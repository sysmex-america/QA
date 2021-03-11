using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;


namespace Sysmex.Crm.IntegrationPlugins.Logic
{
	class MapImplementationProducttoTaskLogic
	{
		private IOrganizationService orgService;
		private ITracingService trace;

		public MapImplementationProducttoTaskLogic(IOrganizationService orgService, ITracingService trace)
		{
			this.orgService = orgService;
			this.trace = trace;
		}
		public void UpdateTask(Entity task)
		{
			trace.Trace("Begin UpdateTask Method");
			EntityReference workOrder =task.Contains("msdyn_workorder") ? task.GetAttributeValue<EntityReference>("msdyn_workorder") : null;
			if(workOrder !=null)
			{
				trace.Trace("Work Order Found");
				Entity enWorkOrder = GetWorkOrder(workOrder);
				EntityReference implementationProduct = enWorkOrder.Contains("smx_implementationproductid") ? enWorkOrder.GetAttributeValue<EntityReference>("smx_implementationproductid") : null;
				trace.Trace("Implementation Product Found");
				if (implementationProduct != null)
					task["smx_implementationproductid"] = new EntityReference(implementationProduct.LogicalName, implementationProduct.Id);
			}
			trace.Trace("End UpdateTask Method");
		}
		private Entity GetWorkOrder(EntityReference workOrder)
		{
			trace.Trace("Begin GetWorkOrder Method");
			var fetch = $@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                               <entity name='msdyn_workorder'>
                               <attribute name='msdyn_name' />
                               <attribute name='msdyn_workorderid' />
                               <attribute name='smx_implementationproductid' />
                                  <filter type='and'>
                                     <condition attribute='msdyn_workorderid' operator='eq'  value='{workOrder.Id}' />
                                 </filter>
                              </entity>
                          </fetch>";

			return orgService.RetrieveMultiple(new FetchExpression(fetch))
									.Entities.Count > 0 ? orgService.RetrieveMultiple(new FetchExpression(fetch))
									.Entities
									.FirstOrDefault() : null;
		}
	}
}
