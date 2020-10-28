using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.Collections.Generic;
using System;
using Sysmex.Crm.Model;
using System.Linq;

namespace Sysmex.Crm.IntegrationPlugins.Logic
{
	public class MapSalesOrderToImplementationLogic
	{
		private IOrganizationService _orgService;
		private ITracingService _trace;

		public MapSalesOrderToImplementationLogic(IOrganizationService orgService, ITracingService trace)
		{
			_orgService = orgService;
			_trace = trace;
		}

		private enum ContractType
		{
			Purchase = 180700000,
			Lease = 180700001,
			CPR = 180700002
		}

		private enum YesNo
		{
			No = 180700000,
			Yes = 180700001
		}

		private enum InstrumentPickup
		{
			No = 180700000,
			Yes = 180700001
		}

		private enum OrderRush
		{
			No = 180700000,
			Yes = 180700001
		}

		private enum WAMSite
		{
			No = 180700000,
			Yes = 180700001
		}

		private enum ApprovalType
		{
			NoPO_NoShip = 180700000,
			APContactAproval = 180700001,
			InternalApprover_Waiver = 180700002
		}

		private smx_implementation _existingContractMatchImplementation = null;

		private const string _shipToAlias = "address";
		private const string _equipmentLocationAlias = "equipmentLocation";

		public void UpdateImplementation(Entity changeSet)
		{
			_trace.Trace($"Start {nameof(UpdateImplementation)}");
			var implementation = new Entity("smx_implementation");
			var salesOrders = RetrieveSalesOrder(changeSet.Id, changeSet.LogicalName, true, changeSet.Contains("smx_lablocationid"));
			_trace.Trace($"Number of Sales Orders Found {salesOrders.Count()}");

			var salesOrder = salesOrders.FirstOrDefault();
			if (salesOrder == null)
			{
				_trace.Trace("Could not find related implentations");
				return;
			}

			SetImplementationMappings(salesOrder, implementation, false, changeSet, changeSet.LogicalName);

			var implementations = salesOrders
									.Where(w => w.Contains("implementation.smx_implementationid"))
									.Select(s =>
										new ImpStatus
										{
											Name = GetFormatName(s, changeSet),
											Id = (Guid)(s.GetAttributeValue<AliasedValue>("implementation.smx_implementationid").Value),
											Status = (OptionSetValue)(s.GetAttributeValue<AliasedValue>("implementation.statuscode").Value)
										}).ToList();	

			UpdateImplmentations(implementations, implementation);
			_trace.Trace($"End {nameof(UpdateImplementation)}");
		}

		public void CreateImplementationProducts(smx_salesorder salesOrder)
		{
			_trace.Trace($"Start {nameof(CreateImplementationProducts)}");
			if (_existingContractMatchImplementation == null)
			{
				throw new InvalidPluginExecutionException("Missing Existing Implementation Id; Exit Early");
			}

			var newLineItems = GetNewLineItems(salesOrder.Id);
			var logic = new CreateImplementationProductRecordsLogic(_orgService, _trace);
			var activeStage = logic.GetActiveStage(salesOrder.Id);

			foreach (var item in newLineItems)
			{
				if (logic.ValidForCreation(item) == false)
				{
					_trace.Trace($"Skip creation of Quote Line Item {item.Id}");
					continue;
				}

				logic.MapAndCreate(item, _existingContractMatchImplementation.ToEntityReference(),
									_existingContractMatchImplementation.smx_ContractNumber, _existingContractMatchImplementation.OwnerId, 
									_existingContractMatchImplementation.smx_AddressTimeZone, activeStage);
			}

		}

		private IEnumerable<new_cpq_lineitem_tmp> GetNewLineItems(Guid salesOrderId)
		{
			_trace.Trace($"Start {nameof(GetNewLineItems)}");

			var fetch = $@"<fetch>
							  <entity name='new_cpq_lineitem_tmp'>
								<attribute name='new_cpq_lineitem_tmpid' />
								<attribute name='new_name' />
								<attribute name='new_optionid' />
								<attribute name='new_customdescription' />
								<attribute name='new_price' />
								<attribute name='smx_shipdate' />
								<attribute name='smx_lineitemstatus' />
								<attribute name='new_quoteid' />
								<attribute name='smx_salesorderid' />
								<filter type='and'>
								  <condition attribute='smx_lineitemstatus' operator='eq' value='{(int)smx_product_status.New}' />
								  <condition attribute='smx_salesorderid' operator='eq' value='{salesOrderId}' />
								</filter>
								<link-entity name='smx_product' from='smx_productid' to='new_optionid' link-type='inner' alias='product'>
									<attribute name='smx_productid' />
									<attribute name='smx_sow' />
									<attribute name='smx_classification' />
								    <attribute name='smx_seicn' />
								    <attribute name='smx_snap' />
                                    <attribute name='smx_excludefromis' />
									<attribute name='smx_excludefromicn' />
									<attribute name='smx_isnocharge' />
								</link-entity>
							  </entity>
							</fetch>";

			return _orgService.RetrieveMultiple(new FetchExpression(fetch))
					.Entities
					.Where(w => w.Contains("product.smx_excludefromis") == true
									&& ((bool?)w.GetAttributeValue<AliasedValue>("product.smx_excludefromis").Value).HasValue == true
									&& ((bool?)w.GetAttributeValue<AliasedValue>("product.smx_excludefromis").Value).Value == true)
					.Select(s => s.ToEntity<new_cpq_lineitem_tmp>())
					.ToList();
		}

		public void UpdateImplementationProducts(EntityReference salesOrder, EntityReference activeStage)
		{
			_trace.Trace($"Start {nameof(UpdateImplementationProducts)}");
			var records = RetrievenSalesOrderForImplementationProducts(salesOrder.Id);
			var record = records.FirstOrDefault();
			if (record == null)
			{
				_trace.Trace("Could not find related implentation product");
				return;
			}

			foreach (var s in records)
			{
				UpdateImplementationProductsActivestageidField(s.Id, activeStage);
			}
			_trace.Trace($"End {nameof(UpdateImplementationProducts)}");
		}

		private void UpdateImplementationProductsActivestageidField(Guid entityId, EntityReference attributevalue)
		{
			smx_implementationproduct updateEntity = new smx_implementationproduct();
			updateEntity.Id = entityId;
			updateEntity.smx_SalesOrderActiveStageId = attributevalue;
			_orgService.Update(updateEntity.ToEntity<Entity>());
			_trace.Trace("Record Updated for : implementation product");
		}

		/// <summary>
		/// Creates an implementation entity record for a sales order
		/// </summary>
		/// <param name="salesOrder"></param>
		public void CreateImplementation(Entity changeSet)
		{
			_trace.Trace($"***** BEGIN CreateImplementation method - sales order {changeSet.Id} - {DateTime.Now.ToString()} *****");

			var data = RetrieveSalesOrder(changeSet.Id, changeSet.LogicalName, false).FirstOrDefault();
			if (data != null)
			{
				var implementation = new Entity("smx_implementation");

				_trace.Trace("Set implementation name");
				implementation.Attributes.Add("smx_name", FormatName(data));

				_trace.Trace("Get implementation team");
				var implementationTeam = GetImplementationTeam();
				if (implementationTeam != null)
				{
					_trace.Trace("Set implementation team");
					implementation.Attributes.Add("ownerid", implementationTeam);

					_trace.Trace("Set implementation field mappings");
					SetImplementationMappings(data, implementation, true, changeSet, changeSet.LogicalName);

					_trace.Trace("Creating implementation record");
					_orgService.Create(implementation);
					_trace.Trace("Implementation record created");
				}
				else
				{
					throw new InvalidPluginExecutionException("Exception: Implementation team not found.");
				}
			}
			else
			{
				throw new InvalidPluginExecutionException("Exception: Sales order not found/could not be retrieved.");
			}

			_trace.Trace("***** END CreateImplementation method *****");
		}

		/// <summary>
		/// Check for required criteria to be met before an implementation record is created
		/// </summary>
		/// <param name="salesOrder"></param>
		/// <returns></returns>
		public ImplmentationCreationOptions ValidForImplementationCreation(Entity salesOrder)
		{
			_trace.Trace("* BEGIN  ValidForImplementationCreation method *");

			_trace.Trace("Evaluate Contract #");
			if (string.IsNullOrWhiteSpace(salesOrder.GetAttributeValue<string>("smx_contractnumber")))
			{
				_trace.Trace("Missing Contract Number; not valid for Creation");
				return ImplmentationCreationOptions.Update;
			}

			if (HasExistingImplementationRelatedToSalesContract(salesOrder.ToEntity<smx_salesorder>()))
			{
				return ImplmentationCreationOptions.CreateImplementationProducts;
			}

			_trace.Trace("Evaluate condition 1");
			var condition1 = salesOrder.GetAttributeValue<OptionSetValue>("smx_poreceived")?.Value == (int)YesNo.Yes && !String.IsNullOrWhiteSpace(salesOrder.GetAttributeValue<string>("smx_purchaseorder"));

			_trace.Trace("Evaluate condition 2");
			var condition2 = (salesOrder.GetAttributeValue<OptionSetValue>("smx_poreceived")?.Value == (int)YesNo.No
							&& ((salesOrder.GetAttributeValue<OptionSetValue>("smx_approvaltype")?.Value == (int)ApprovalType.APContactAproval)
									|| salesOrder.GetAttributeValue<OptionSetValue>("smx_approvaltype")?.Value == (int)ApprovalType.InternalApprover_Waiver));

			if ((condition1 || condition2))
			{
				_trace.Trace($"At least one condition has passed [condition1={condition1},condition2={condition2}] and now it will check for an existing implementation");
				if (HasExistingImplementation(salesOrder.Id))
				{
					_trace.Trace("Sales Order already has existing implementation: invalid for implementation ");
					return ImplmentationCreationOptions.Update;
				}
				else
				{
					_trace.Trace("Sales Order doesn't have existing implementation. All requirements passed: sales order valid for implementation ");
					return ImplmentationCreationOptions.Create;
				}
			}
			else
			{
				_trace.Trace("Sales Order failed both conditions 1 & 2: invalid for implementation  ");
				return ImplmentationCreationOptions.Update;
			}
		}

		private bool HasExistingImplementationRelatedToSalesContract(smx_salesorder salesOrder)
		{
			_trace.Trace($"* BEGIN {nameof(HasExistingImplementationRelatedToSalesContract)} method *");

			var fetch = $@"
                    <fetch>	
                      <entity name='smx_implementation'>
						<attribute name='smx_contractnumber' />
						<attribute name='ownerid' />
						<attribute name='smx_implementationid' />
						<attribute name='smx_addresstimezone' />
                        <filter type='and'>
                          <condition attribute='smx_salesorderid' operator='ne' value='{salesOrder.Id}' />
						  <condition attribute='smx_contractnumber' operator='eq' value='{salesOrder.smx_ContractNumber}' />
							<filter type='or'>
								<condition attribute='statecode' operator='eq' value='{(int)smx_implementationState.Active}' />
								<filter type='and'>
									<condition attribute='statecode' operator='eq' value='{(int)smx_implementationState.Inactive}' />
									<condition attribute='statuscode' operator='eq' value='{(int)smx_implementation_statuscode.OnHold}' />
								</filter>
							</filter>
                        </filter>
                      </entity>
                    </fetch>";

			var result = _orgService.RetrieveMultiple(new FetchExpression(fetch));
			if (result.Entities.Count > 0)
			{
				_existingContractMatchImplementation = result
														.Entities
														.Select(s => s.ToEntity<smx_implementation>())
														.First();
			}

			_trace.Trace($"* End {nameof(HasExistingImplementationRelatedToSalesContract)} method *");
			return _existingContractMatchImplementation != null;
		}

		/// <summary>
		/// Based on the sales order fields, it returns the name to be assigned to the implementation record
		/// </summary>
		/// <param name="salesOrder"></param>
		/// <returns></returns>
		private string FormatName(Entity salesOrder)
		{
			_trace.Trace("* BEGIN FormatName method *");

			var part2OfName = salesOrder.GetAttributeValue<string>("smx_contractnumber") != null ? salesOrder.GetAttributeValue<string>("smx_contractnumber") : "";
			var part1OfName = salesOrder.GetAttributeValue<AliasedValue>("address.smx_name") != null ? salesOrder.GetAttributeValue<AliasedValue>("address.smx_name").Value?.ToString() : "";

			return FormatName(part1OfName, part2OfName);
		}

		private string FormatName(string part1OfName, string part2OfName)
		{
			_trace.Trace("* RETURN FormatName method *");
			return (part1OfName + " - " + part2OfName);
		}

		private string GetFormatName(Entity thisSalesOrder, Entity changeSet)
		{
			string part2OfName = "", part1OfName = "";
			if (changeSet.LogicalName == smx_salesorder.EntityLogicalName && changeSet.Contains("smx_contractnumber"))
			{
				part2OfName = changeSet.GetAttributeValue<string>("smx_contractnumber") != null ? changeSet.GetAttributeValue<string>("smx_contractnumber") : "";
				part1OfName = thisSalesOrder.GetAttributeValue<AliasedValue>("address.smx_name") != null ? thisSalesOrder.GetAttributeValue<AliasedValue>("address.smx_name").Value?.ToString() : "";
				return FormatName(part1OfName, part2OfName);
			}
			else if (changeSet.LogicalName == smx_address.EntityLogicalName && changeSet.Contains("smx_name"))
			{
				part2OfName = thisSalesOrder.GetAttributeValue<string>("smx_contractnumber") != null ? thisSalesOrder.GetAttributeValue<string>("smx_contractnumber") : "";
				part1OfName = changeSet.GetAttributeValue<string>("smx_name") != null ? changeSet.GetAttributeValue<string>("smx_name")?.ToString() : "";
				return FormatName(part1OfName, part2OfName);
			}

			return "";
		}

		/// <summary>
		/// Set fields in the implementation entity, using the sales order fields as source
		/// </summary>
		/// <param name="salesOrder"></param>
		/// <param name="implementation"></param>
		private void SetImplementationMappings(Entity salesOrder, Entity implementation, bool mapAllFields, Entity changeSet, string logicalName)
		{
			_trace.Trace($"* BEGIN {nameof(SetImplementationMappings)} method *");
			var hasEquipmentLocation = (salesOrder.Contains($"{_equipmentLocationAlias}.smx_addressid"));
			var entityAlias = GetEntityAlias(logicalName, hasEquipmentLocation);

			if (mapAllFields)
			{
				implementation.Attributes.Add("smx_salesorderid", new EntityReference("smx_salesorder", salesOrder.Id));
				CreateStyleMappings(salesOrder, implementation, entityAlias);
			}
			else
			{				
				UpdateStyleMappings(implementation, changeSet, entityAlias);
				AddAddressFieldChangeFields(salesOrder, implementation, changeSet);
			}

			if (entityAlias == "") //create imp or update imp from sales order
			{
				SetContractType(salesOrder, implementation);
			}
			SetXpPochi(salesOrder, implementation);

			_trace.Trace($"* End {nameof(SetImplementationMappings)} method *");
		}

		private void AddAddressFieldChangeFields(Entity souceData, Entity destination, Entity changeSet)
		{
			_trace.Trace($"Start {nameof(AddAddressFieldChangeFields)}");
			_trace.Trace($"Contains Location Id: {souceData.Contains("smx_lablocationid")}");
			_trace.Trace($"Contains Ship To Id: {souceData.Contains("smx_instrumentshiptoidid")}");
			_trace.Trace($"Sales Data has Equip Location: {destination.Contains("smx_lablocationid")}");

			Dictionary<string, string> mappingDictionary = new Dictionary<string, string>();
			if (changeSet.Contains("smx_instrumentshiptoidid"))
			{
				_trace.Trace($"Map Ship To Fields");
				mappingDictionary = _shipToChangedFields;
			}
			if (changeSet.Contains("smx_lablocationid"))
			{
				foreach (var element in _equipmentLocationChangedFields)
				{
					mappingDictionary.Add(element.Key, element.Value);
				}
			}

			foreach (var mapping in mappingDictionary)
			{
				_trace.Trace($"Source Contains Mapping Value: {mapping.Value} - {souceData.Contains(mapping.Value)}");
				_trace.Trace($"Destination Contains Mapping Key: {mapping.Key} - {destination.Contains(mapping.Key)}");

				_trace.Trace($"Map {mapping.Value}");
				if (souceData.Contains(mapping.Key))
				{
					CopyMappedAliasedValues(mapping.Key, souceData, mapping.Value, destination);
				}
				else
				{
					destination.Attributes[mapping.Value] = null;
				}
			}
			_trace.Trace($"End {nameof(AddAddressFieldChangeFields)}");
		}

		private string GetEntityAlias(string logicalName, bool hasEquipmentLocation)
		{
			_trace.Trace($"Start {nameof(GetEntityAlias)}");
			switch (logicalName)
			{
				case smx_lab.EntityLogicalName:
					if (hasEquipmentLocation)
					{
						return $"{_equipmentLocationAlias}lab";
					}
					else
					{
						return $"{_shipToAlias}lab";
					}
				case smx_address.EntityLogicalName:
					return "address";
				case Territory.EntityLogicalName:
					return "territory";
				default:
					return "";
			}
		}

		private void CreateStyleMappings(Entity sourceData, Entity implementation, string entityAlias)
		{
			_trace.Trace($"Start {nameof(CreateStyleMappings)}");

			foreach (var mapping in fieldMappings)
			{
				//map wam fields from sales order; skip the wam fields from the lab entity on create of Implmentation
				var isWAMLabMapping = mapping.Key.Contains("lab.smx_wam");
				if (implementation.Attributes.Contains(mapping.Value) == false && isWAMLabMapping == false)
				{
					if (mapping.Key.Contains("."))
					{
						CopyMappedAliasedValues(mapping.Key, sourceData, mapping.Value, implementation);
					}
					else
					{
						CopyMappedField(mapping.Key, sourceData, mapping.Value, implementation);
					}
				}
			}

			_trace.Trace($"End {nameof(CreateStyleMappings)}");
		}

		private void UpdateStyleMappings(Entity implementation, Entity changeSet, string entityAlias)
		{
			_trace.Trace($"Start {nameof(UpdateStyleMappings)}");
			foreach (var field in changeSet.Attributes)
			{
				if (entityAlias != "" && entityAlias.Contains(".") == false)
				{
					entityAlias = $"{entityAlias}.";
				}
				_trace.Trace($"Entity Alias: {entityAlias}");
				_trace.Trace($"Field Key: {field.Key}");

				var mapping = fieldMappings.Where(w => w.Key == $"{entityAlias}{field.Key}").FirstOrDefault();
				if (string.IsNullOrWhiteSpace(mapping.Value) == false) //<-- not sure if this will work
				{
					CopyMappedField(field.Key, changeSet, mapping.Value, implementation);
				}
				else
				{
					_trace.Trace($"Could not find mapping for {entityAlias}{field.Key}");
				}
			}
			_trace.Trace($"End {nameof(UpdateStyleMappings)}");
		}

		private void SetXpPochi(Entity salesOrder, Entity implementation)
		{
			_trace.Trace($"Start {nameof(SetXpPochi)}");
			var flag = false;
			//When a Sales Order that have one or more Line Items that have products with classifications of either POC or XP 
			//	If Sales Order’s other Line Items are related to a Product with a classification of SYS, STD or WR then set the Implementation’s Pochi/ Stand Alone XP flag to false
			//   Otherwise set the flag to true
			//If the Sales Order has zero line items related to a Product with a classification of POC or XP then the Implementation’s Pochi/ Stand Alone XP flag is set to false

			var items = GetQuoteItems(salesOrder.ToEntityReference());
			if (items.Where(w => w.smx_Classification?.Value == (int)smx_classification.POC || w.smx_Classification?.Value == (int)smx_classification.XP).Any())
			{
				_trace.Trace("Xp or Pochi Found");
				var execptions = items.Where(w => w.smx_Classification?.Value == (int)smx_classification.SYS
											 || w.smx_Classification?.Value == (int)smx_classification.STD
											 || w.smx_Classification?.Value == (int)smx_classification.WR);

				if (execptions.Any() == false)
				{
					_trace.Trace("No Sys, Std or Wr found, set flag to true");
					flag = true;
				}
			}

			implementation.Attributes.Add("smx_standalongxppochiorder", flag);
			_trace.Trace($"End {nameof(SetXpPochi)}");
		}

		private IEnumerable<smx_product> GetQuoteItems(EntityReference smx_SalesOrderId)
		{
			_trace.Trace($"Start {nameof(GetQuoteItems)}");

			var fetch = $@"<fetch>
							  <entity name='smx_product'>
								<attribute name='smx_productid' />
								<attribute name='smx_classification' />								
								<link-entity name='new_cpq_lineitem_tmp' from='new_optionid' to='smx_productid' link-type='inner' alias='aa'>
									<filter type='and'>
										<condition attribute='smx_salesorderid' operator='eq' value='{smx_SalesOrderId.Id}' />						   
									</filter>
								</link-entity>
							  </entity>
							</fetch>";

			return _orgService.RetrieveMultiple(new FetchExpression(fetch))
					.Entities
					.Select(s => s.ToEntity<smx_product>())
					.ToList();
		}

		private void SetContractType(Entity salesOrder, Entity implementation)
		{
			var sales = salesOrder.ToEntity<smx_salesorder>();
			const string contractTypeFieldName = "smx_contracttype";

			if (sales.smx_ContractType?.Value == (int)smx_contracttype.CPR && sales.smx_minmonthlybilling?.Value == (int)smx_yesno.Yes)
			{
				implementation.Attributes.Add(contractTypeFieldName, new OptionSetValue((int)smx_contracttype.CPR_New));
			}
			else if (sales.smx_ContractType?.Value == (int)smx_contracttype.CPR && sales.smx_OrderReason?.Value == (int)smx_orderreason.COLABCRHandling)
			{
				implementation.Attributes.Add(contractTypeFieldName, new OptionSetValue((int)smx_contracttype.LabCPR));
			}
			else if (sales.smx_ContractType?.Value == (int)smx_contracttype.Lease && sales.smx_OrderReason?.Value == (int)smx_orderreason.COReagentPlanCInstrmtServiceReagt)
			{
				implementation.Attributes.Add(contractTypeFieldName, new OptionSetValue((int)smx_contracttype.Reagent));
			}
			else
			{
				implementation.Attributes.Add(contractTypeFieldName, sales.smx_ContractType);
			}
		}

		private void CopyMappedAliasedValues(string salesOrderAttribute, Entity sourceData, string implementationAttribute, Entity updateRecord)
		{
			var salesData = sourceData.GetAttributeValue<AliasedValue>(salesOrderAttribute);
			if (salesData != null)
			{
				if (sourceData.GetAttributeValue<AliasedValue>(salesOrderAttribute).Value != null)
				{
					_trace.Trace($"Copying mapping for {salesOrderAttribute}");
					updateRecord[implementationAttribute] = sourceData.GetAttributeValue<AliasedValue>(salesOrderAttribute).Value;
				}
				else
				{
					_trace.Trace($"Source Data doesn't have {salesOrderAttribute}");
				}
			}
			else
			{
				_trace.Trace($"Source Data doesn't have {salesOrderAttribute}");
			}
		}

		/// <summary>
		/// Copy "salesOrderAttribute" from "salesOrder" entity to the field "implementationAttribute" in the "implementation" entity
		/// </summary>
		/// <param name="salesOrderAttribute"></param>
		/// <param name="salesOrder"></param>
		/// <param name="implementationAttribute"></param>
		/// <param name="implementation"></param>
		private void CopyMappedField(string salesOrderAttribute, Entity salesOrder, string implementationAttribute, Entity implementation)
		{
			_trace.Trace($"Start {nameof(CopyMappedField)}");
			var salesData = salesOrder.Contains(salesOrderAttribute) ? salesOrder[salesOrderAttribute] : null;
			if (salesData != null)
			{
				_trace.Trace($"Copying mapping for {salesOrderAttribute}");
				if (implementationAttribute == "smx_orderrushreason")
				{
					var record = _orgService.Retrieve(salesOrder.LogicalName, salesOrder.Id, new ColumnSet("smx_rushreason"));
					var formatedRushReason = record.FormattedValues.Contains("smx_rushreason") ? record.FormattedValues["smx_rushreason"] : "";
					_trace.Trace($"Formated Rush Reason {formatedRushReason}");
					implementation[implementationAttribute] = formatedRushReason;
				}
				else
				{
					implementation[implementationAttribute] = salesData;
				}
			}
			else
			{
				_trace.Trace($"Sales order doesn't have {salesOrderAttribute}");
			}
			_trace.Trace($"End {nameof(CopyMappedField)}");
		}

		/// <summary>
		/// Returns whether or not there is an implementation already associated with the sales order guid
		/// </summary>
		/// <param name="salesGuid"></param>
		/// <returns></returns>
		private bool HasExistingImplementation(Guid salesGuid)
		{
			_trace.Trace("* BEGIN HasExistingImplementation method *");

			var fetch = $@"
                    <fetch>
                      <entity name='smx_implementation'>
                        <attribute name='smx_implementationid' />
                        <filter type='and'>
                          <condition attribute='smx_salesorderid' operator='eq' value='{salesGuid}' />
							<filter type='or'>
								<condition attribute='statecode' value='0' operator='eq'/>
								<filter type='and'>
									<condition attribute='statecode' value='1' operator='eq'/>
									<condition attribute='statuscode' value='2' operator='eq'/>
								</filter>
							</filter>
                        </filter>
                      </entity>
                    </fetch>";

			var result = _orgService.RetrieveMultiple(new FetchExpression(fetch));

			_trace.Trace("* END/RETURN HasExistingImplementation method *");
			return result.Entities.Count >= 1;
		}

		/// <summary>
		/// Retrives the "Implementation Team"  record from entity Teams.
		/// </summary>
		/// <returns></returns>
		private EntityReference GetImplementationTeam()
		{
			_trace.Trace("* BEGIN GetImplementationTeam method *");

			EntityReference implementationTeam;

			var teamNameToSearchFor = "Implementation Team";

			var fetch = $@"
                    <fetch>
                      <entity name='team'>
                        <attribute name='teamid' />
                        <order attribute='name' descending='false' />
                        <filter type='and'>
                          <condition attribute='name' operator='eq' value='{teamNameToSearchFor}' />
                        </filter>
                      </entity>
                    </fetch>";

			var result = _orgService.RetrieveMultiple(new FetchExpression(fetch));

			if (result.Entities.Count == 1)
			{
				_trace.Trace($"GetImplementationTeam method: team {result.Entities[0].Id} found");
				implementationTeam = new EntityReference("team", result.Entities[0].Id);
			}
			else
			{
				_trace.Trace("*GetImplementationTeam method: team not found");
				implementationTeam = null;
			}

			_trace.Trace("* END/RETURN GetImplementationTeam method");
			return implementationTeam;
		}

		private IEnumerable<Entity> RetrieveSalesOrder(Guid entityId, string logicalName, bool isAnUpdate, bool runEquipmentLocationQuery = false)
		{
			_trace.Trace("* BEGIN RetrieveSalesOrder method *");
			string salesOrderFilter = "", labFilter = "", addressFilter = "", territoryFilter = "", implementationJoin = "";
			string labJoinType = "outer", addressJoinType = "outer", territoryJoinType = "outer";

			switch (logicalName)
			{
				case smx_salesorder.EntityLogicalName:
					salesOrderFilter = $@"<filter type='and'> 
											<condition attribute = 'smx_salesorderid' operator= 'eq' value = '{entityId}' />
										</filter>";

					runEquipmentLocationQuery = true;

					break;
				case smx_lab.EntityLogicalName:
					runEquipmentLocationQuery = true;
					labFilter = $@"<filter type='and'> 
											<condition attribute = 'smx_labid' operator= 'eq' value = '{entityId}' />
										</filter>";

					addressJoinType = "inner";
					labJoinType = "inner";

					break;
				case smx_address.EntityLogicalName:
					runEquipmentLocationQuery = true;
					addressFilter = $@"<filter type='and'> 
											<condition attribute = 'smx_addressid' operator= 'eq' value = '{entityId}' />
										</filter>";

					addressJoinType = "inner";

					break;
				case Territory.EntityLogicalName:
					territoryFilter = $@"<filter type='and'> 
											<condition attribute = 'territoryid' operator= 'eq' value = '{entityId}' />
										</filter>";
					territoryJoinType = "inner";
					break;
				default:
					return new List<Entity>();
			}

			if (isAnUpdate)
			{
				implementationJoin = $@"<link-entity name='smx_implementation' from='smx_salesorderid' to='smx_salesorderid' link-type='inner' alias='implementation'>
											<attribute name='smx_implementationid' />
											<attribute name='statuscode' />
											<attribute name='smx_lablocationid' />
											<filter type='or'>
												<condition attribute='statuscode' operator='eq' value='{(int)smx_implementation_statuscode.OnHold}' />
												<condition attribute='statecode' operator='eq' value='{(int)smx_implementationState.Active}' />
											 </filter>
										</link-entity>";
			}

			List<Entity> results = new List<Entity>();
			results.AddRange(GetSalesOrderRecords(salesOrderFilter, labFilter, addressFilter, territoryFilter, implementationJoin, labJoinType, addressJoinType, territoryJoinType));

			if (runEquipmentLocationQuery)
			{
				var addresses = GetEquipmentLocationRecords(salesOrderFilter, labFilter, addressFilter, implementationJoin, labJoinType, addressJoinType);
				if (logicalName == smx_salesorder.EntityLogicalName && results.Count == 1 && addresses.Count == 1)
				{
					if (isAnUpdate)
					{
						var attributesToAdd = addresses[0].Attributes.Where(w => w.Key.Contains($"{_equipmentLocationAlias}"));
						results[0].Attributes.AddRange(attributesToAdd);
					}
					else
					{
						var attributesToAdd = new List<KeyValuePair<string, object>>();
						if (addresses[0].Attributes.Contains($"{_equipmentLocationAlias}lab.smx_crcid"))
						{
							attributesToAdd.Add(new KeyValuePair<string, object>($"{_equipmentLocationAlias}lab.smx_crcid", addresses[0].GetAttributeValue<AliasedValue>($"{_equipmentLocationAlias}lab.smx_crcid")));
						}
						if (addresses[0].Attributes.Contains($"{_equipmentLocationAlias}.smx_sapnumber"))
						{
							attributesToAdd.Add(new KeyValuePair<string, object>($"{_equipmentLocationAlias}.smx_sapnumber", addresses[0].GetAttributeValue<AliasedValue>($"{_equipmentLocationAlias}.smx_sapnumber")));
						}

						if (attributesToAdd.Count > 0)
						{
							results[0].Attributes.AddRange(attributesToAdd);
						}
					}
				}
				else
				{
					results.AddRange(addresses);
				}
			}

			_trace.Trace("* END/RETURN RetrieveSalesOrder method *");
			return results;
		}

		private List<Entity> GetSalesOrderRecords(string salesOrderFilter, string labFilter, string addressFilter, string territoryFilter, string implementationJoin, string labJoinType, string addressJoinType, string territoryJoinType)
		{
			var fetchXml = $@"
                <fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'> 
	                <entity name='smx_salesorder'> 
							  <attribute name='smx_salesorderid' /> 
                              <attribute name='smx_competitor' />
                              <attribute name='smx_maincontactid' />
                              <attribute name='smx_customernotes' />
                              <attribute name='smx_purchaseorder' />
                              <attribute name='smx_distributor' />
                              <attribute name='smx_hemegpo' />
                              <attribute name='smx_accountmanagerid' />
                              <attribute name='smx_ihn' />
                              <attribute name='smx_internalnotes' />
                              <attribute name='smx_orderdetaildist' />
                              <attribute name='smx_instrumentpickup' />
                              <attribute name='smx_orderrush' />
                              <attribute name='smx_rushreason' />
                              <attribute name='smx_contractnumber' />
                              <attribute name='smx_territoryid' />
                              <attribute name='smx_instrumentshiptoidid' />
                              <attribute name='smx_tentativeinstalldate' />
                              <attribute name='smx_wamsite' />
							  <attribute name='smx_poreceived' />
							  <attribute name='smx_approvaltype' />
                              <attribute name='smx_contracttype' />
                              <attribute name='smx_equipmenttradein' />
							  <attribute name='smx_soldtoaddressid' />
                              <attribute name='smx_caeihnid' />
                              <attribute name='smx_wamconnects' />
                              <attribute name='smx_competitivedisplacement' />
							  <attribute name='smx_masteragreement' />
							  <attribute name='smx_revenuetype' />
							  <attribute name='smx_orderreason' />
							  <attribute name='smx_contracttype' />
							  <attribute name='smx_minmonthlybilling' />	
                              <attribute name='smx_contractreleasedate' />	
                              <attribute name='smx_lis' />	
                              <attribute name='smx_mds' />	
							  <attribute name='smx_lablocationid' />
						{salesOrderFilter}
		                <link-entity name='smx_address' from='smx_addressid' to='smx_instrumentshiptoidid' link-type='{addressJoinType}' alias='{_shipToAlias}'>  
                             <attribute name='smx_city' /> 
                             <attribute name='smx_countrysap' /> 
                             <attribute name='smx_statesap' /> 
                             <attribute name='smx_zippostalcode' /> 
                             <attribute name='smx_name' /> 
                             <attribute name='smx_addresstimezone' />
							 <attribute name='smx_servicedistrict' />
							 <attribute name='smx_dsmid' />
							 <attribute name='smx_sapnumber' />
                             <attribute name='smx_addressstreet1' />
                             <attribute name='smx_addressstreet2' />
							 <attribute name='smx_addressid' />
							 <link-entity name='smx_lab' from='smx_labid' to='smx_lab' link-type='{labJoinType}' alias='{_shipToAlias}lab'>
								<attribute name='smx_crcid' />
								<attribute name='smx_wamconnects' />
								<attribute name='smx_wamsite' />
								{labFilter}
							 </link-entity>
							 {addressFilter}
						</link-entity>
						<link-entity name='territory' from='territoryid' to='smx_territoryid' link-type='{territoryJoinType}' alias='territory'>
							<attribute name='smx_regionalmanager' />
							{territoryFilter}
						</link-entity>                        
						{implementationJoin}
					</entity> 
                </fetch>";

			return _orgService.RetrieveMultiple(new FetchExpression(fetchXml)).Entities.ToList();
		}

		private List<Entity> GetEquipmentLocationRecords(string salesOrderFilter, string labFilter, string addressFilter, string implementationJoin, string labJoinType, string addressJoinType)
		{
			_trace.Trace($"Start {nameof(GetEquipmentLocationRecords)}");
			var fetch = $@"
                <fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'> 
	                <entity name='smx_salesorder'> 
						<attribute name='smx_salesorderid' /> 
						<attribute name='smx_lablocationid' />
						<attribute name='smx_contractnumber' />
						{salesOrderFilter}
						<link-entity name='smx_address' from='smx_addressid' to='smx_lablocationid' link-type='{addressJoinType}' alias='{_equipmentLocationAlias}'>  
                             <attribute name='smx_city' /> 
                             <attribute name='smx_countrysap' /> 
                             <attribute name='smx_statesap' /> 
                             <attribute name='smx_zippostalcode' /> 
                             <attribute name='smx_name' /> 
                             <attribute name='smx_addresstimezone' />
							 <attribute name='smx_servicedistrict' />
							 <attribute name='smx_dsmid' />
							 <attribute name='smx_sapnumber' />
                             <attribute name='smx_addressstreet1' />
                             <attribute name='smx_addressstreet2' />
							 <attribute name='smx_addressid' />
							 <link-entity name='smx_lab' from='smx_labid' to='smx_lab' link-type='{labJoinType}' alias='{_equipmentLocationAlias}lab'>
								<attribute name='smx_crcid' />
								<attribute name='smx_wamconnects' />
								<attribute name='smx_wamsite' />
								{labFilter}
							 </link-entity>
							 {addressFilter}
						</link-entity>		                                     
						{implementationJoin}
					</entity> 
                </fetch>";

			return _orgService.RetrieveMultiple(new FetchExpression(fetch)).Entities.ToList();
		}

		private IEnumerable<Entity> RetrievenSalesOrderForImplementationProducts(Guid id)
		{
			_trace.Trace("* BEGIN RetrievenSalesOrderForImplementationProducts method *");
			var fetchXml = $@"
                            <fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'> 
								<entity name='smx_implementationproduct'>
									<attribute name='smx_implementationproductid' /> 
									<link-entity name='smx_implementation' from='smx_implementationid' to='smx_implementationid' link-type='inner' alias='implementation'>
										<filter type='and'> 
											<condition attribute='smx_salesorderid' operator='eq' value='{id}' /> 
										</filter> 
									</link-entity> 
								</entity> 
                            </fetch>";

			_trace.Trace("* END/RETURN RetrievenSalesOrderForImplementationProducts method *");
			return _orgService.RetrieveMultiple(new FetchExpression(fetchXml))
					.Entities;
		}
		private void UpdateImplmentations(IEnumerable<ImpStatus> records, Entity implementation)
		{
			_trace.Trace($"Start {nameof(UpdateImplmentations)}");

			foreach (var record in records)
			{
				var id = record.Id;
				var status = record.Status;
				var statusImplementation = new smx_implementation();
				if (status.Value == (int)smx_implementation_statuscode.OnHold)
				{
					statusImplementation.statecode = smx_implementationState.Active;
					statusImplementation.Id = id;
					_orgService.Update(statusImplementation.ToEntity<Entity>());
				}

				_trace.Trace($"Update {id}");
				implementation.Id = id;
				if (string.IsNullOrWhiteSpace(record.Name) == false)
				{
					implementation.Attributes["smx_name"] = record.Name;
				}
				
				_orgService.Update(implementation.ToEntity<Entity>());

				if (status.Value == (int)smx_implementation_statuscode.OnHold)
				{
					statusImplementation.statecode = smx_implementationState.Inactive;
					statusImplementation.statuscode = smx_implementation_statuscode.OnHold;
					_orgService.Update(statusImplementation.ToEntity<Entity>());
				}
			}

			_trace.Trace($"End {nameof(UpdateImplmentations)}");
		}

		private Dictionary<string, string> fieldMappings = new Dictionary<string, string>
		{
			{$"{_equipmentLocationAlias}.smx_city","smx_city"},
			{$"{_equipmentLocationAlias}.smx_zippostalcode","smx_zip"},
			{$"{_equipmentLocationAlias}.smx_statesap", "smx_stateid"},
			{$"{_equipmentLocationAlias}.smx_countrysap","smx_countryid"},
			{$"{_equipmentLocationAlias}.smx_addresstimezone","smx_addresstimezone"},
			{$"{_equipmentLocationAlias}.smx_servicedistrict", "smx_servicedistrict"},
			{$"{_equipmentLocationAlias}.smx_dsmid","smx_dsmid"},
			{$"{_equipmentLocationAlias}.smx_sapnumber","smx_sapshiptonumber"},
			{$"{_equipmentLocationAlias}.smx_addressstreet1","smx_street1"},
			{$"{_equipmentLocationAlias}.smx_addressstreet2","smx_street2"},
			{$"{_shipToAlias}.smx_city","smx_city"},
			{$"{_shipToAlias}.smx_zippostalcode","smx_zip"},
			{$"{_shipToAlias}.smx_statesap", "smx_stateid"},
			{$"{_shipToAlias}.smx_countrysap","smx_countryid"},
			{$"{_shipToAlias}.smx_addresstimezone","smx_addresstimezone"},
			{$"{_shipToAlias}.smx_servicedistrict", "smx_servicedistrict"},
			{$"{_shipToAlias}.smx_dsmid","smx_dsmid"},
			{$"{_shipToAlias}.smx_sapnumber","smx_sapshiptonumber"},
			{$"{_shipToAlias}.smx_addressstreet1","smx_street1"},
			{$"{_shipToAlias}.smx_addressstreet2","smx_street2"},
			{"smx_wamconnects","smx_wamconnects"},
			{"smx_wamsite", "smx_wamsite"},
			{$"{_equipmentLocationAlias}lab.smx_crcid", "smx_crcid"},
			{$"{_equipmentLocationAlias}lab.smx_wamconnects","smx_wamconnects"},
			{$"{_equipmentLocationAlias}lab.smx_wamsite", "smx_wamsite"},
			{$"{_shipToAlias}lab.smx_crcid", "smx_crcid"},
			{$"{_shipToAlias}lab.smx_wamconnects","smx_wamconnects"},
			{$"{_shipToAlias}lab.smx_wamsite", "smx_wamsite"},
			{"territory.smx_regionalmanager", "smx_regionalmanagerid"},
			{"smx_maincontactid", "smx_customercontactid"},
			{"smx_customernotes","smx_customernotes"},
			{"smx_purchaseorder","smx_purchaseorder"},
			{"smx_distributor", "smx_distributorid"},
			{"smx_hemegpo", "smx_gpopartnerid"},
			{"smx_accountmanagerid", "smx_hsamid"},
			{"smx_ihn", "smx_ihnpartnerid"},
			{"smx_internalnotes", "smx_internalnotes"},
			{"smx_orderdetaildist", "smx_orderdistributeddate"},
			{"smx_instrumentpickup", "smx_pickupflag"},
			{"smx_orderrush", "smx_orderrush"},
			{"smx_rushreason", "smx_orderrushreason"},
			{"smx_contractnumber", "smx_contractnumber"},
			{"smx_territoryid", "smx_salesterritoryid"},
			{"smx_instrumentshiptoidid", "smx_instrumentshiptoid"},
			{"smx_tentativeinstalldate", "smx_tentativeinstalldate"},
			{"smx_soldtoaddressid", "smx_soldtoid"},
			{"smx_equipmenttradein", "smx_equipmenttradein"},
			{"smx_caeihnid","smx_caeihnid"},
			{"smx_competitivedisplacement","smx_competitivedisplacement"},
			{"smx_masteragreement", "smx_masteragreement"},
			{"smx_revenuetype", "smx_revenuetype"},
			{"smx_contractreleasedate", "smx_contractreleasedate"},
			{"smx_lis","smx_lis"},
			{"smx_mds","smx_mds"},
			{"smx_lablocationid", "smx_lablocationid"}
		};

		private Dictionary<string, string> _shipToChangedFields = new Dictionary<string, string>()
		{
			{$"{_shipToAlias}.smx_city","smx_city"},
			{$"{_shipToAlias}.smx_zippostalcode","smx_zip"},
			{$"{_shipToAlias}.smx_statesap", "smx_stateid"},
			{$"{_shipToAlias}.smx_countrysap","smx_countryid"},
			{$"{_shipToAlias}.smx_addresstimezone","smx_addresstimezone"},
			{$"{_shipToAlias}.smx_servicedistrict", "smx_servicedistrict"},
			{$"{_shipToAlias}.smx_dsmid","smx_dsmid"},
			{$"{_shipToAlias}.smx_sapnumber","smx_sapshiptonumber"},
			{$"{_shipToAlias}.smx_addressstreet1","smx_street1"},
			{$"{_shipToAlias}.smx_addressstreet2","smx_street2"},
		};

		private Dictionary<string, string> _equipmentLocationChangedFields = new Dictionary<string, string>()
		{
			{$"{_equipmentLocationAlias}lab.smx_crcid", "smx_crcid"},
			{$"{_equipmentLocationAlias}lab.smx_wamconnects","smx_wamconnects"},
			{$"{_equipmentLocationAlias}lab.smx_wamsite", "smx_wamsite"},
		};

		private class ImpStatus
		{
			public Guid Id;
			public OptionSetValue Status;
			public string Name;
		}
	}
}

