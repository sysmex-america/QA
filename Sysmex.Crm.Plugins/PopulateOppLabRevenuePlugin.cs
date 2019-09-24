using System;
using SonomaPartners.Crm.Toolkit.Plugins;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.Linq;

namespace Sysmex.Crm.Plugins
{
    public class PopulateOppLabRevenuePlugin : PluginBase
    {
        public override void OnExecute(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetPluginExecutionContext();
            var orgService = serviceProvider.CreateOrganizationServiceAsCurrentUser();
            var tracer = serviceProvider.GetTracingService();

            tracer.Trace("Starting PopulateOppLabRevenuePlugin Execution.");

            var target = context.InputParameters.Contains("Target") ? context.GetTargetEntity() : null;

            if (target == null && context.MessageName == "Create")
            {
                tracer.Trace("No target in context. Exiting...");
                return;
            }

            var preImage = context.PreEntityImages.Contains("PreImage") ? context.PreEntityImages["PreImage"] : null;
            if (preImage == null && (context.MessageName == "Delete" || context.MessageName == "Update"))
            {
                tracer.Trace("No pre image in context. Exiting...");
                return;
            }

            var logic = new PopulateOppLabRevenueLogic(orgService, tracer);
            logic.PopulateRevenues(target, preImage);

            tracer.Trace("Ending Plugin Execution.");
        }
    }
}
