using Microsoft.Xrm.Sdk;
using SonomaPartners.Crm.Toolkit.Plugins;
using Sysmex.Crm.Plugins.Logic;
using System;

namespace Sysmex.Crm.Plugins
{
    public class UpdateOpportunityStageDurationPlugin : PluginBase
    {
        public override void OnExecute(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetPluginExecutionContext();
            var orgService = serviceProvider.CreateOrganizationServiceAsCurrentUser();
            var trace = serviceProvider.GetTracingService();

            trace.Trace("BEGIN UpdateDemoEvalDirectorsPlugin");

            if (!context.InputParameters.Contains("Target"))
            {
                trace.Trace("Target not found within InputParameters, returning.");
                return;
            }

            var input = context.InputParameters["Target"] as Entity;
            var preImage = context.PreEntityImages.Contains("PreImage") ? context.PreEntityImages["PreImage"] : null;
            var logic = new UpdateOpportunityStageDurationLogic(orgService, trace);

            switch (context.MessageName.ToLower())
            {
                case "create":
                    logic.CreateOpportunityStageDuration(input);
                    break;
                case "update":
                    logic.UpdateOpportunityStageDuration(input, preImage);
                    break;
            }

            trace.Trace("END UpdateDemoEvalDirectorsPlugin");
        }
    }
}
