using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Sysmex.Crm.Model;

namespace Sysmex.Crm.IntegrationPlugins.Logic
{
	public class CreateImplementationProductRecordsLogic
	{
		private IOrganizationService orgService;
		private ITracingService trace;

		public CreateImplementationProductRecordsLogic(IOrganizationService orgService, ITracingService trace)
		{
			this.orgService = orgService;
			this.trace = trace;
		}

		public void CreateImplementationProducts(smx_implementation implementation)
		{
			if (DoesNotHaveExistingImplementationProducts(implementation))
			{
				var activeStage = GetActiveStage(implementation.smx_SalesOrderId.Id);
				var quoteItems = GetQuoteItems(implementation.smx_SalesOrderId);
				trace.Trace($"Number of Quote Items found: {quoteItems.Count()}");
				foreach (var item in quoteItems)
				{
					MapAndCreate(item, implementation.ToEntityReference(), implementation.smx_ContractNumber, implementation.OwnerId, implementation.smx_AddressTimeZone, activeStage);
				}
			}
			else
			{
				trace.Trace("Has Existing Implmentation Products.  Do not create new ones.");
			}
		}

		public void MapAndCreate(Entity quoteItem, EntityReference implementation, string contractNumber, 
			EntityReference ownerId, string timeZone, EntityReference activeStage)
		{
			trace.Trace($"Start MapAndCreate for Quote Item: {quoteItem.Id}"); //this Method is called from a loop, do not use Reflextion to get method name for performance reasons
			var implementationProduct = new smx_implementationproduct();
			var isNoCharge = quoteItem.Contains("product.smx_isnocharge")
								? ((bool?)(quoteItem.GetAttributeValue<AliasedValue>("product.smx_isnocharge").Value))
								: null;

			foreach (var mapping in fieldMappings)
			{
				var data = quoteItem.Contains(mapping.Key) ? quoteItem[mapping.Key] : null;
				if (data != null)
				{
					trace.Trace($"Mapping {mapping.Key} to {mapping.Value}");
					implementationProduct.Attributes.Add(mapping.Value, FormatData(data, mapping.Value, isNoCharge));
				}
			}

			if (implementation == null)
			{
				trace.Trace("Missing Implementation");
			}
			if (activeStage == null)
			{
				trace.Trace("Missing Active Stage");
			}
			implementationProduct.Attributes.Add("smx_salesorderactivestageid", activeStage);
			implementationProduct.Attributes.Add("smx_implementationid", implementation);
			implementationProduct.Attributes.Add("ownerid", ownerId);
			implementationProduct.Attributes.Add("smx_addresstimezone", timeZone);
			implementationProduct.Attributes.Add("smx_eligibleforrevenuedatecalculations", GetRecalcFlag(implementationProduct.smx_lineitemstatus));

			orgService.Create(implementationProduct.ToEntity<Entity>());
		}

		private static OptionSetValue GetRecalcFlag(OptionSetValue lineItemStatus)
		{
			if (lineItemStatus != null && lineItemStatus.Value == (int)smx_product_status.New)
			{
				return new OptionSetValue((int)smx_yesno.No);
			}
			return new OptionSetValue((int)smx_yesno.Yes);
		}

		public void CreateImplementationProduct(new_cpq_lineitem_tmp lineItem)
		{
			trace.Trace($"Start {nameof(CreateImplementationProduct)}");
			if (ValidForCreation(lineItem))
			{
				var fullLineItem = GetQuoteItem(lineItem.ToEntityReference());
				if (fullLineItem == null)
				{
					trace.Trace("Could not find Quote Line Item; Exit Early");
					return;
				}

				var implementation = GetImplementation(lineItem);
				if (implementation == null)
				{
					trace.Trace("Could not find a Implementation that is related to this Sales Order or any Sales Order with a matching Contract Number.  Please contact a System Admin to correct the data and try again");
					return;
				}

				MapAndCreate(fullLineItem, implementation.ToEntityReference(), implementation.smx_ContractNumber, implementation.OwnerId, implementation.smx_AddressTimeZone, null);
			}
			else
			{
				trace.Trace("Does not meet creation criteria.  Do not crete a new record.");
			}
			trace.Trace($"End {nameof(CreateImplementationProduct)}");
		}

		public bool ValidForCreation(new_cpq_lineitem_tmp lineItem)
		{
			trace.Trace($"Start {nameof(ValidForCreation)}");
			if (lineItem.smx_SalesOrderId == null)
			{
				trace.Trace("Quote Line Item is missing Sales Order Id");
				return false;
			}
			else if (lineItem.smx_LineItemStatus == null || lineItem.smx_LineItemStatus.Value != (int)smx_product_status.New)
			{
				trace.Trace("Line Item Status does not equal New");
				return false;
			}
			if (LineIdAlreadyExists(lineItem.new_name))
			{
				trace.Trace("Implementation Product Already Exists");
				return false;
			}

			trace.Trace($"End {nameof(ValidForCreation)}");
			return true;
		}

		private bool LineIdAlreadyExists(string lineItemId)
		{
			trace.Trace($"Start {nameof(LineIdAlreadyExists)}");

			var fetch = $@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
							  <entity name='smx_implementationproduct'>
								<attribute name='smx_implementationproductid' />
								<filter type='and'>
								  <condition attribute='smx_cpqlineitemid' operator='eq' value='{lineItemId}' />
								  <condition attribute='statecode' operator='eq' value='{(int)smx_implementationproductState.Active}' />
								</filter>
							  </entity>
							</fetch>";

			return orgService.RetrieveMultiple(new FetchExpression(fetch))
										  .Entities.Count() > 0;							  
		}

		private smx_implementation GetImplementation(new_cpq_lineitem_tmp lineItem)
		{
			trace.Trace($"Start {nameof(GetImplementation)}");

			if (lineItem.smx_SalesOrderId == null)
			{
				trace.Trace("Missing Sales Order; Exit Early");
				return null;
			}

			var implmentation = GetSalesOrderImplementation(lineItem.smx_SalesOrderId);
			if (implmentation == null)
			{
				trace.Trace("Sales Order Does not have a related Implementation");
				implmentation = GetImplementationByContractNumber(lineItem.smx_SalesOrderId);
			}
			return implmentation;
		}

		private smx_implementation GetImplementationByContractNumber(EntityReference salesOrderId)
		{
			trace.Trace($"Start {nameof(GetImplementationByContractNumber)}");

			var salesOrder = orgService.Retrieve(salesOrderId.LogicalName, salesOrderId.Id, new ColumnSet("smx_contractnumber")).ToEntity<smx_salesorder>();
			if (string.IsNullOrWhiteSpace(salesOrder.smx_ContractNumber))
			{
				trace.Trace("Missing Contract Number; Exit Early");
				return null;
			}

			var fetch = $@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
							  <entity name='smx_implementation'>
								<attribute name='smx_name' />
								<attribute name='smx_contractnumber' />
								<attribute name='ownerid' />
								<attribute name='smx_implementationid' />
								<attribute name='smx_addresstimezone' />
								<filter type='and'>
									<condition attribute='statecode' operator='eq' value='{(int)smx_implementationproductState.Active}' />
								 </filter>
								<link-entity name='smx_salesorder' from='smx_salesorderid' to='smx_salesorderid' link-type='inner' alias='aa'>
								  <filter type='and'>
									<condition attribute='smx_contractnumber' operator='eq' value='{salesOrder.smx_ContractNumber}' />
								  </filter>
								</link-entity>
							  </entity>
							</fetch>";

			return orgService.RetrieveMultiple(new FetchExpression(fetch))
										  .Entities
										  .Select(s => s.ToEntity<smx_implementation>())
										  .FirstOrDefault();
		}

		private smx_implementation GetSalesOrderImplementation(EntityReference smx_SalesOrderId)
		{
			trace.Trace($"Start {nameof(GetSalesOrderImplementation)}");
			var fetch = $@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
							  <entity name='smx_implementation'>
								<attribute name='smx_name' />
								<attribute name='smx_contractnumber' />
								<attribute name='ownerid' />
								<attribute name='smx_implementationid' />
								<attribute name='smx_addresstimezone' />
								<filter type='and'>
									<condition attribute='statecode' operator='eq' value='{(int)smx_implementationproductState.Active}' />
								</filter>
								<filter type='and'>
								  <condition attribute='smx_salesorderid' operator='eq' value='{smx_SalesOrderId.Id}' />
								</filter>
							  </entity>
							</fetch>";

			return orgService.RetrieveMultiple(new FetchExpression(fetch))
										  .Entities
										  .Select(s => s.ToEntity<smx_implementation>())
										  .FirstOrDefault();
		}

		public EntityReference GetActiveStage(Guid salesOrderId)
		{
			trace.Trace($"Start {nameof(GetActiveStage)}");
			var fetch = $@"<fetch>
                                <entity name='smx_salesorderbusinessprocess'>
                                  <attribute name='bpf_smx_salesorderid' />
                                  <attribute name='activestageid' />
                                  <filter type='and'>
                                    <condition attribute='bpf_smx_salesorderid' operator='eq' value='{salesOrderId}' />
                                  </filter>
                                </entity>
                              </fetch>";

			return orgService.RetrieveMultiple(new FetchExpression(fetch))
										  .Entities
										  .Select(s => s.ToEntity<smx_salesorderbusinessprocess>())
										  .FirstOrDefault()
										  ?.ActiveStageId;

		}

		private object FormatData(object data, string fieldName, bool? isNoCharge = null)
		{
			if (data is AliasedValue)
			{
				if (fieldName == "smx_snapyesno")
				{
					return GetSnapValue(data);
				}				
				return ((AliasedValue)data).Value;
			}

			if (fieldName == "smx_price" && isNoCharge.HasValue && isNoCharge.Value == true)
			{
				return new Money(0);
			}

			return data;
		}

		private static object GetSnapValue(object data)
		{
			var value = ((AliasedValue)data).Value != null
									? (bool)((AliasedValue)data).Value
									: (bool?)null;

			if (value != null)
			{
				return value == true ? new OptionSetValue((int)smx_yesno.Yes) : new OptionSetValue((int)smx_yesno.No);
			}

			return null;
		}

		private IEnumerable<Entity> GetQuoteItems(EntityReference smx_SalesOrderId)
		{
			trace.Trace($"Start {nameof(GetQuoteItems)}");
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
								<order attribute='new_name' descending='false' />
								<filter type='and'>
								  <condition attribute='smx_salesorderid' operator='eq' value='{smx_SalesOrderId.Id}' />						   
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

			return orgService.RetrieveMultiple(new FetchExpression(fetch))
					.Entities
					.Where(w => w.Contains("product.smx_excludefromis") == true
									&& ((bool?)w.GetAttributeValue<AliasedValue>("product.smx_excludefromis").Value).HasValue == true 
									&& ((bool?)w.GetAttributeValue<AliasedValue>("product.smx_excludefromis").Value).Value == true)
					.ToList();
		}

		private Entity GetQuoteItem(EntityReference quoteId)
		{
			trace.Trace($"Start {nameof(GetQuoteItems)}");
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
								<order attribute='new_name' descending='false' />
								<filter type='and'>
								  <condition attribute='new_cpq_lineitem_tmpid' operator='eq' value='{quoteId.Id}' />						   
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

			return orgService.RetrieveMultiple(new FetchExpression(fetch))
					.Entities
					.Where(w => w.Contains("product.smx_excludefromis") == true
									&& ((bool?)w.GetAttributeValue<AliasedValue>("product.smx_excludefromis").Value).HasValue == true
									&& ((bool?)w.GetAttributeValue<AliasedValue>("product.smx_excludefromis").Value).Value == true)
					.FirstOrDefault();
		}

		private bool DoesNotHaveExistingImplementationProducts(smx_implementation implementation)
		{
			trace.Trace($"Start {nameof(DoesNotHaveExistingImplementationProducts)}");
			var fetch = $@"<fetch>
							  <entity name='smx_implementationproduct'>
								<attribute name='smx_implementationproductid' />
								<attribute name='smx_name' />
								<attribute name='createdon' />
								<order attribute='smx_name' descending='false' />
								<filter type='and'>
								  <condition attribute='smx_implementationid' operator='eq' value='{implementation.Id}' />
								  <condition attribute='statecode' operator='eq' value='0' />
								</filter>
							  </entity>
							</fetch>";

			return orgService.RetrieveMultiple(new FetchExpression(fetch)).Entities.Count == 0;
		}

		private Dictionary<string, string> fieldMappings = new Dictionary<string, string>
		{
			{ "new_name","smx_cpqlineitemid"},
			{ "product.smx_sow", "smx_standardofwork"},
			{ "product.smx_classification", "smx_classification"},
			{ "product.smx_seicn", "smx_seicn"},
			{ "product.smx_snap", "smx_snapyesno"},
			{ "product.smx_excludefromicn", "smx_excludefromicn"},			
			{ "new_optionid", "smx_materialnumberid"},
			{ "new_customdescription", "smx_modeldesc"},
			{ "new_price", "smx_price"},
			{ "smx_shipdate", "smx_shipdate"},
			{ "smx_lineitemstatus", "smx_lineitemstatus"}
		};
	}	
}