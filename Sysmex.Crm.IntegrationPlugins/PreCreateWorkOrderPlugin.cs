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
    public class PreCreateWorkOrderPlugin : PluginBase
    {
        public override void OnExecute(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetPluginExecutionContext();
            var trace = serviceProvider.GetTracingService();

            trace.Trace("BEGIN PreCreateWorkOrderPlugin");

            if (!context.InputParameters.Contains("Target"))
            {
                trace.Trace("Target not found within InputParameters, returning.");
                return;
            }

            var input = context.InputParameters["Target"] as Entity;
            var workOrder = input.ToEntity<msdyn_workorder>();

            if (workOrder.Contains("smx_implementationproductid") == false || workOrder.smx_ImplementationProductID == null)
            {
                trace.Trace("No implementation product; exit early");
                return;
            }

            var implementationProductGuid = ((EntityReference)workOrder.smx_ImplementationProductID).Id;
            var orgService = serviceProvider.CreateSystemOrganizationService();

            trace.Trace("calling logic.WorkOrderAutoname for implementationProductGuid " + implementationProductGuid);
            var logic = new PreCreateWorkOrderLogic(orgService, trace);            
            var workOrderAutoname = logic.WorkOrderAutoname(implementationProductGuid);

            //truncates the autoname to max 100 characters to match entity field max lenght
            workOrder.smx_Name = workOrderAutoname.Length > 100 ? workOrderAutoname.Substring(0, 100) : workOrderAutoname;
			workOrder.msdyn_WorkOrderSummary = workOrder.smx_Name;

            trace.Trace("END PreCreateWorkOrderPlugin");
        }
    }
}
