using System;
using SonomaPartners.Crm.Toolkit.Plugins;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.Linq;

namespace Sysmex.Crm.Plugins
{
    public class PopulateOpportunityFieldsPlugin : PluginBase
    {
        public override void OnExecute(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetPluginExecutionContext();
            var orgService = serviceProvider.CreateOrganizationServiceAsCurrentUser();
            var tracer = serviceProvider.GetTracingService();

            tracer.Trace("Starting PopulateOpportunityFieldsPlugin Execution.");

            var target = context.InputParameters.Contains("Target") ? context.GetTargetEntity() : null;

            if (target == null)
            {
                tracer.Trace("No target Opportunity in context. Exiting...");
                return;
            }
            
            var logic = new PopulateOpportunityFieldsLogic(orgService, tracer);
            logic.PopulateOpportunityFields(target);
            tracer.Trace("Ending Plugin Execution.");
        }
    }
}
