using Microsoft.Xrm.Sdk;
using SonomaPartners.Crm.Toolkit.Plugins;
using Sysmex.Crm.IntegrationPlugins.Logic;
using Sysmex.Crm.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sysmex.Crm.IntegrationPlugins
{
    public class WorkOrderFieldMappingPlugin : PluginBase
    {
        public override void OnExecute(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetPluginExecutionContext();
            var trace = serviceProvider.GetTracingService();

            trace.Trace("BEGIN WorkOrderFieldMappingPlugin");

            if (!context.InputParameters.Contains("Target"))
            {
                trace.Trace("Target not found within InputParameters, returning.");
                return;
            }

			var orgService = serviceProvider.CreateSystemOrganizationService();
			trace.Trace("Create WorkOrderFieldMappingLogic reference");
			var logic = new WorkOrderFieldMappingLogic(orgService, trace);

			var input = context.InputParameters["Target"] as Entity;
			if (input.LogicalName == msdyn_workorder.EntityLogicalName)
			{
				var workOrder = input.ToEntity<msdyn_workorder>();
				if (workOrder.Contains("smx_ImplementationProductID") == false || workOrder.smx_ImplementationProductID == null)
				{
					trace.Trace("No Implementation Product; exit early");
					return;
				}
				
				logic.UpdateWorkOrder(workOrder);
			}
			else if (input.LogicalName == smx_implementation.EntityLogicalName)
			{
				logic.UpdateAllImplementationWorkOrders(input.ToEntity<smx_implementation>());
			}
			else if (input.LogicalName == smx_address.EntityLogicalName)
			{
				logic.UpdateAllAddressWorkOrders(input.ToEntity<smx_address>());
			}

			trace.Trace("END WorkOrderFieldMappingPlugin");
        }
    }
} 