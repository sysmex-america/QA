using System;
using SonomaPartners.Crm.Toolkit.Plugins;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.Linq;

namespace Sysmex.Crm.Plugins
{
    public class PopulateOppEstimatedValuePlugin : PluginBase
    {
        public override void OnExecute(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetPluginExecutionContext();
            var orgService = serviceProvider.CreateOrganizationServiceAsCurrentUser();
            var tracer = serviceProvider.GetTracingService();

            tracer.Trace("Starting PopulateOppEstimatedValuePlugin Execution.");

            var target = context.InputParameters.Contains("Target") ? context.GetTargetEntity() : null;

            if (target == null && context.MessageName == "Create")
            {
                tracer.Trace("No target in context. Exiting...");
                return;
            }

            var preImage = context.PreEntityImages.Contains("PreImage") ? context.PreEntityImages["PreImage"] : null;
            if (preImage == null && (context.MessageName == "Update" || context.MessageName == "Delete"))
            {
                tracer.Trace("No pre image in context. Exiting...");
                return;
            }

            var logic = new PopulateOppEstimatedValueLogic(orgService, tracer);
            logic.PopulateEstimatedValue(target, preImage);

            tracer.Trace("Ending Plugin Execution.");
        }
    }
}
