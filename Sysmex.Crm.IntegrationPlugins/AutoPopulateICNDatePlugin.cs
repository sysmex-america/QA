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
    class AutoPopulateICNDatePlugin : PluginBase
    {
        public override void OnExecute(IServiceProvider serviceProvider) 
        {
            var context = serviceProvider.GetPluginExecutionContext();
            var trace = serviceProvider.GetTracingService();
            var orgService = serviceProvider.CreateSystemOrganizationService();
            trace.Trace($"BEGIN {nameof(AutoPopulateICNDatePlugin)}");

            var target = context.InputParameters["Target"] as Entity;
          
            var logic = new AutoPopulateICNDateLogic(orgService, trace);

            if (target.LogicalName == smx_instrument.EntityLogicalName)
            {
                var instrument = target.ToEntity<smx_instrument>();

                if (target.Contains("smx_shipdate"))
                {
                    logic.UpdateImplementationProductOnShipdateUpdate(instrument);
                }
            }
            else if (target.LogicalName == smx_implementationproduct.EntityLogicalName)
            {
                var product = target.ToEntity<smx_implementationproduct>();
                var preImage = ((Entity)context.PreEntityImages["PreImage"]).ToEntity<smx_implementationproduct>();

				if (product.smx_ServiceConfirmedInstallDate == null)
				{
					product.smx_ServiceConfirmedInstallDate = preImage.smx_ServiceConfirmedInstallDate;
				}
				if (string.IsNullOrWhiteSpace(product.smx_StandardofWork))
				{
					product.smx_StandardofWork = preImage.smx_StandardofWork;
				}

                if (target.Contains("smx_serviceconfirmedinstalldate") || target.Contains("smx_standardofwork"))
                {
                    logic.UpdateImplementationProduct(product);
                }
            }
            else
            {
                trace.Trace("Logical name did not match");
            }

            trace.Trace($"END {nameof(AutoPopulateICNDatePlugin)}");

        }
    }
}
