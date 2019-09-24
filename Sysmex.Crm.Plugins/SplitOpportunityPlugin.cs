using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using SonomaPartners.Crm.Toolkit.Plugins;
using Sysmex.Crm.Plugins.Logic;

namespace Sysmex.Crm.Plugins
{
    public class SplitOpportunityPlugin : PluginBase
    {
        public override void OnExecute(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetPluginExecutionContext();
            var orgService = serviceProvider.CreateOrganizationServiceAsCurrentUser();
            var trace = serviceProvider.GetTracingService();

            if (!context.InputParameters.Contains("Target"))
            {
                trace.Trace("Target not found within InputParameters, returning.");
                return;
            }

            var entityReference = context.InputParameters["Target"] as EntityReference;
            if (entityReference == null)
            {
                trace.Trace("Retrieved EntityReference was null, returning.");
                return;
            }

            var entity = orgService.Retrieve(entityReference.LogicalName, entityReference.Id, new ColumnSet(true));
            var logic = new SplitOpportunityLogic(context, orgService, trace);
            logic.SplitOpportunity(entity);
        }
    }
}
