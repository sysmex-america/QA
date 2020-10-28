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
    public class AutoDeactivateImplementationProductPlugin : PluginBase
    {
        public override void OnExecute(IServiceProvider serviceProvider) 
        {
            var context = serviceProvider.GetPluginExecutionContext();
            var trace = serviceProvider.GetTracingService();
           
            trace.Trace($"BEGIN {nameof(AutoDeactivateImplementationProductPlugin)}");

            var target = context.InputParameters["Target"] as Entity;

           if (target.LogicalName == smx_implementationproduct.EntityLogicalName)
           {
                var product = target.ToEntity<smx_implementationproduct>();
				var orgService = serviceProvider.CreateSystemOrganizationService();
				var logic = new AutoDeactivateImplementationProductLogic(orgService, trace);
				if (target.Contains("smx_ovitemstatus"))
				{
					logic.UpdateImplementationProductOnOvItemStatusUpdate(product);
				}
				else if (target.Contains("statuscode"))
				{
					logic.ClearImplementationProduct(product);
				}
            }
            else
            {
                trace.Trace("Logical name did not match");
            }

            trace.Trace($"END {nameof(AutoDeactivateImplementationProductPlugin)}");

        }
    }
}
