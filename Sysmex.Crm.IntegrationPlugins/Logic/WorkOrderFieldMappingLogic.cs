using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Sysmex.Crm.Model;

namespace Sysmex.Crm.IntegrationPlugins.Logic
{
    public class WorkOrderFieldMappingLogic
    {
        private IOrganizationService orgService;
        private ITracingService trace;

        public WorkOrderFieldMappingLogic(IOrganizationService orgService, ITracingService trace)
        {
            this.orgService = orgService;
            this.trace = trace;
        }

		public void UpdateWorkOrder(msdyn_workorder workOrder, WorkOrderFieldMappingOutput workOrderFieldMappingOutput)
		{
			var doUpdate = false;

			trace.Trace("check msdyn_BillingAccount");
			if (workOrderFieldMappingOutput.smx_soldtoid != null &&
				(workOrder.msdyn_BillingAccount == null || workOrder.msdyn_BillingAccount.Id != workOrderFieldMappingOutput.smx_soldtoid.smx_account?.Id))
			{
				workOrder.msdyn_BillingAccount = workOrderFieldMappingOutput.smx_soldtoid.smx_account;
				doUpdate = true;
			}

			if (workOrderFieldMappingOutput.smx_instrumentshiptoid != null)
			{
				trace.Trace("check lab");
				if (workOrderFieldMappingOutput.smx_instrumentshiptoid.smx_lab != null &&
					(workOrder.smx_LabId== null || workOrder.smx_LabId.Id != workOrderFieldMappingOutput.smx_instrumentshiptoid.smx_lab?.Id))
				{
					workOrder.smx_LabId = workOrderFieldMappingOutput.smx_instrumentshiptoid.smx_lab;
					doUpdate = true;
				}

				trace.Trace("check msdyn_ServiceAccount");
				if (workOrderFieldMappingOutput.smx_instrumentshiptoid.smx_account != null &&
					(workOrder.msdyn_ServiceAccount == null || workOrder.msdyn_ServiceAccount.Id != workOrderFieldMappingOutput.smx_instrumentshiptoid.smx_account?.Id))
				{
					workOrder.msdyn_ServiceAccount = workOrderFieldMappingOutput.smx_instrumentshiptoid.smx_account;
					doUpdate = true;
				}

				trace.Trace("check msdyn_Address1");
				if (!string.IsNullOrWhiteSpace(workOrderFieldMappingOutput.smx_instrumentshiptoid.smx_addressstreet1) &&
					(workOrder.msdyn_Address1 == null || workOrder.msdyn_Address1 != workOrderFieldMappingOutput.smx_instrumentshiptoid.smx_addressstreet1))
				{
					workOrder.msdyn_Address1 = workOrderFieldMappingOutput.smx_instrumentshiptoid.smx_addressstreet1;
					doUpdate = true;
				}

				trace.Trace("check msdyn_Address2");
				if (!string.IsNullOrWhiteSpace(workOrderFieldMappingOutput.smx_instrumentshiptoid.smx_addressstreet2) &&
					(workOrder.msdyn_Address2 == null || workOrder.msdyn_Address2 != workOrderFieldMappingOutput.smx_instrumentshiptoid.smx_addressstreet2))
				{
					workOrder.msdyn_Address2 = workOrderFieldMappingOutput.smx_instrumentshiptoid.smx_addressstreet2;
					doUpdate = true;
				}

				trace.Trace("check msdyn_City");
				if (string.IsNullOrWhiteSpace(workOrderFieldMappingOutput.smx_instrumentshiptoid.smx_city) == false &&
					(workOrder.msdyn_City == null || workOrder.msdyn_City != workOrderFieldMappingOutput.smx_instrumentshiptoid.smx_city))
				{
					workOrder.msdyn_City = workOrderFieldMappingOutput.smx_instrumentshiptoid.smx_city;
					doUpdate = true;
				}

				trace.Trace("check .msdyn_PostalCode");
				if (string.IsNullOrWhiteSpace(workOrderFieldMappingOutput.smx_instrumentshiptoid.smx_zippostalcode) == false &&
					(workOrder.msdyn_PostalCode == null || workOrder.msdyn_PostalCode != workOrderFieldMappingOutput.smx_instrumentshiptoid.smx_zippostalcode))
				{
					workOrder.msdyn_PostalCode = workOrderFieldMappingOutput.smx_instrumentshiptoid.smx_zippostalcode;
					doUpdate = true;
				}

				trace.Trace("check msdyn_StateOrProvince");
				if (string.IsNullOrWhiteSpace(workOrderFieldMappingOutput.smx_instrumentshiptoid.smx_statesap?.smx_name) == false &&
					(workOrder.msdyn_StateOrProvince == null || workOrder.msdyn_StateOrProvince != workOrderFieldMappingOutput.smx_instrumentshiptoid.smx_statesap?.smx_name))
				{
					workOrder.msdyn_StateOrProvince = workOrderFieldMappingOutput.smx_instrumentshiptoid.smx_statesap.smx_name;
					doUpdate = true;
				}

				trace.Trace("check msdyn_Country");
				if (string.IsNullOrWhiteSpace(workOrderFieldMappingOutput.smx_instrumentshiptoid.smx_countrysap) == false &&
					(workOrder.msdyn_Country == null || workOrder.msdyn_Country != workOrderFieldMappingOutput.smx_instrumentshiptoid.smx_countrysap))
				{
					workOrder.msdyn_Country = workOrderFieldMappingOutput.smx_instrumentshiptoid.smx_countrysap;
					doUpdate = true;
				}
			}

			trace.Trace("check smx_ShipToSAPNumber");
			if (string.IsNullOrWhiteSpace(workOrderFieldMappingOutput.smx_sapshiptonumber) == false &&
				(workOrder.smx_SAPNumber == null || workOrder.smx_SAPNumber != workOrderFieldMappingOutput.smx_sapshiptonumber))
			{
				workOrder.smx_SAPNumber = workOrderFieldMappingOutput.smx_sapshiptonumber;
				doUpdate = true;
			}

			trace.Trace("check smx_projectmanagerid");
			if (workOrderFieldMappingOutput.smx_projectmanagerid != null &&
				(workOrder.smx_ProjectManagerID == null || workOrder.smx_ProjectManagerID != workOrderFieldMappingOutput.smx_projectmanagerid))
			{
				workOrder.smx_ProjectManagerID = workOrderFieldMappingOutput.smx_projectmanagerid;
				doUpdate = true;
			}

			if (doUpdate)
			{
				trace.Trace("Update workorder entity");
				orgService.Update(workOrder.ToEntity<Entity>());
			}

			trace.Trace("End UpdateWorkOrder. DoUpdate=" + doUpdate.ToString());
		}

		public void UpdateWorkOrder(msdyn_workorder workOrder)
        {
            trace.Trace("Begin UpdateWorkOrder");

            var logic = new GetWorkOrderFieldMappingActionLogic(orgService, trace);
            var WorkOrderFieldMappingOutput = logic.GetWorkOrderFieldMapping(workOrder.smx_ImplementationProductID.Id);

			UpdateWorkOrder(workOrder, WorkOrderFieldMappingOutput);
        }

		public void UpdateAllImplementationWorkOrders(smx_implementation implementation)
		{
			trace.Trace($"Start {nameof(UpdateAllImplementationWorkOrders)}");
			var workOrders = GetImplementationWorkOrders(implementation.Id);
			foreach (var workOrder in workOrders)
			{
				if (implementation.smx_InstrumentShipToId != null 
					|| implementation.smx_SoldToId != null 
					|| implementation.smx_LabLocationId != null)
				{
					UpdateWorkOrder(workOrder);
				}
				else
				{
					var mapping = new WorkOrderFieldMappingOutput()
					{
						smx_projectmanagerid = implementation.smx_ProjectManagerId,
						smx_sapshiptonumber = implementation.smx_SAPShipToNumber
					};

					UpdateWorkOrder(workOrder, mapping);
				}
			}

			trace.Trace($"End {nameof(UpdateAllImplementationWorkOrders)}");
		}

		public void UpdateAllAddressWorkOrders(smx_address address)
		{
			trace.Trace($"Start {nameof(UpdateAllAddressWorkOrders)}");

			var workOrders = GetServiceAccountWorkOrders(address.Id);

			if ((address.smx_CountrySAP != null && string.IsNullOrWhiteSpace(address.smx_CountrySAP?.Name))
				|| (address.smx_StateSAP != null && string.IsNullOrWhiteSpace(address.smx_StateSAP?.Name)))
			{
				var tempAddress = orgService.Retrieve(address.LogicalName, address.Id, new ColumnSet("smx_countrysap", "smx_statesap"))
										.ToEntity<smx_address>();

				if (address.smx_CountrySAP != null)
				{
					address.smx_CountrySAP.Name = tempAddress.smx_CountrySAP.Name;
				}
				if (address.smx_StateSAP != null)
				{
					address.smx_StateSAP.Name = tempAddress.smx_StateSAP.Name;
				}
			}

			var shipTo = new WorkOrderFieldMappingOutput.ImplementationShipTo
			{
				smx_lab = address.smx_lab,
				smx_account = address.smx_Account,
				smx_addressstreet1 = address.smx_addressstreet1,
				smx_addressstreet2 = address.smx_addressstreet2,
				smx_city = address.smx_city,
				smx_zippostalcode = address.smx_zippostalcode,
				smx_countrysap = address.smx_CountrySAP?.Name
			};
			shipTo.smx_statesap = new WorkOrderFieldMappingOutput.State()
			{
				smx_name = address.smx_StateSAP?.Name
			};
				
			foreach (var workOrder in workOrders)
			{
				var mapping = new WorkOrderFieldMappingOutput
				{
					smx_instrumentshiptoid = shipTo					
				};				

				UpdateWorkOrder(workOrder, mapping);
			}

			workOrders = GetBillingAccountWorkOrders(address.Id);
			var soldTo = new WorkOrderFieldMappingOutput.ImplementationSoldTo
			{
				smx_account = address.smx_Account
			};

			foreach (var workOrder in workOrders)
			{
				var mapping = new WorkOrderFieldMappingOutput()
				{
					smx_soldtoid = soldTo
				};

				UpdateWorkOrder(workOrder, mapping);
			}

			trace.Trace($"End {nameof(UpdateAllAddressWorkOrders)}");
		}

		private IEnumerable<msdyn_workorder> GetServiceAccountWorkOrders(Guid serviceAccountId)
		{
			var fetch = $@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>
							  <entity name='msdyn_workorder'>
								<attribute name='msdyn_serviceaccount' />
								<attribute name='msdyn_workorderid' />
								<attribute name='smx_projectmanagerid' />
								<attribute name='msdyn_country' />
								<attribute name='msdyn_address2' />
								<attribute name='msdyn_address1' />
								<attribute name='msdyn_stateorprovince' />
								<attribute name='smx_sapnumber' />
								<attribute name='smx_labid' />
								<attribute name='msdyn_city' />
								<attribute name='msdyn_billingaccount' />
								<attribute name='msdyn_postalcode' />
								<order attribute='msdyn_serviceaccount' descending='false' />
								<filter type='and'>
								  <condition attribute='statecode' operator='eq' value='0' />
								</filter>
								<link-entity name='smx_implementationproduct' from='smx_implementationproductid' to='smx_implementationproductid' link-type='inner' alias='ae'>
								  <link-entity name='smx_implementation' from='smx_implementationid' to='smx_implementationid' link-type='inner' alias='implementation'>
									<attribute name='smx_instrumentshiptoid' />
									<attribute name='smx_lablocationid' />
									<filter type='or'>
									  <condition attribute='smx_instrumentshiptoid' operator='eq' value='{serviceAccountId}'/>
									  <condition attribute='smx_lablocationid' operator='eq' value='{serviceAccountId}'/>
									</filter>
								  </link-entity>
								</link-entity>
							  </entity>
							</fetch>";

			return orgService.RetrieveMultiple(new FetchExpression(fetch))
								.Entities
								.Select(s => s.ToEntity<msdyn_workorder>())
								.Where(w => w.Contains("implementation.smx_lablocationid") == false
											|| ((EntityReference)(w.GetAttributeValue<AliasedValue>("implementation.smx_lablocationid").Value)).Id == serviceAccountId);
								
								
		}

		private IEnumerable<msdyn_workorder> GetBillingAccountWorkOrders(Guid billingAccountId)
		{
			var fetch = $@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
							  <entity name='msdyn_workorder'>
								<attribute name='msdyn_serviceaccount' />
								<attribute name='msdyn_workorderid' />
								<attribute name='smx_projectmanagerid' />
								<attribute name='msdyn_country' />
								<attribute name='msdyn_address2' />
								<attribute name='msdyn_address1' />
								<attribute name='msdyn_stateorprovince' />
								<attribute name='smx_sapnumber' />
								<attribute name='smx_labid' />
								<attribute name='msdyn_city' />
								<attribute name='msdyn_billingaccount' />
								<attribute name='msdyn_postalcode' />
								<order attribute='msdyn_serviceaccount' descending='false' />
								<filter type='and'>
								  <condition attribute='statecode' operator='eq' value='0' />
								</filter>
								<link-entity name='smx_implementationproduct' from='smx_implementationproductid' to='smx_implementationproductid' link-type='inner' alias='ag'>
								  <link-entity name='smx_implementation' from='smx_implementationid' to='smx_implementationid' link-type='inner' alias='ah'>
									<filter type='and'>
									  <condition attribute='smx_soldtoid' operator='eq' value='{billingAccountId}' />
									</filter>
								  </link-entity>
								</link-entity>
							  </entity>
							</fetch>";

			return orgService.RetrieveMultiple(new FetchExpression(fetch))
								.Entities
								.Select(s => s.ToEntity<msdyn_workorder>());
		}

		private IEnumerable<msdyn_workorder> GetImplementationWorkOrders(Guid implementationId)
		{
			var fetch = $@"<fetch version='1.0' distinct='true'>
							  <entity name='msdyn_workorder'>
								<attribute name='msdyn_serviceaccount' />
								<attribute name='msdyn_workorderid' />
								<attribute name='smx_projectmanagerid' />
								<attribute name='msdyn_country' />
								<attribute name='msdyn_address2' />
								<attribute name='msdyn_address1' />
								<attribute name='msdyn_stateorprovince' />
								<attribute name='smx_sapnumber' />
								<attribute name='smx_labid' />
								<attribute name='msdyn_city' />
								<attribute name='msdyn_billingaccount' />
								<attribute name='msdyn_postalcode' />
								<attribute name='smx_implementationproductid' />
								<filter type='and'>
								  <condition attribute = 'statecode' operator= 'eq' value = '{msdyn_workorderState.Active}' />
								</filter>	
								<link-entity name='smx_implementationproduct' from='smx_implementationproductid' to='smx_implementationproductid' link-type='inner' alias='aa'>
								  <link-entity name='smx_implementation' from='smx_implementationid' to='smx_implementationid' link-type='inner' alias='ab'>
									<filter type='and'>
									  <condition attribute='smx_implementationid' operator='eq' value='{implementationId}' />
									</filter>
								  </link-entity>
								</link-entity>
							  </entity>
							</fetch>";

			return orgService.RetrieveMultiple(new FetchExpression(fetch))
								.Entities
								.Select(s => s.ToEntity<msdyn_workorder>());
		}
	}
}