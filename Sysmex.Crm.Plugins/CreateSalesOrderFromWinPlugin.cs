using System;
using Microsoft.Xrm.Sdk;
using SonomaPartners.Crm.Toolkit.Plugins;
using Sysmex.Crm.Model;
using Sysmex.Crm.Plugins.Logic;

namespace Sysmex.Crm.Plugins
{
    public class CreateSalesOrderFromWinPlugin : PluginBase
    {
        public override void OnExecute(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetPluginExecutionContext();
            var orgService = serviceProvider.CreateOrganizationServiceAsCurrentUser();
            var tracer = serviceProvider.GetTracingService();

            tracer.Trace("Start Create Sales Order From Win Plugin");

            var opportunityClose = context.GetInputParameter<Entity>("OpportunityClose").ToEntity<OpportunityClose>();
            var status = context.GetInputParameter<OptionSetValue>("Status");
            if (status == null || status.Value != (int)opportunity_statuscode.Won)
            {
                tracer.Trace("Opportunity not a Win, returning");
                return;
            }

            if (opportunityClose.OpportunityId == null)
            {
                tracer.Trace("Opportunity Close has no Opportunity Id, returning");
                return;
            }

            var logic = new CreateSalesOrderFromWinLogic(orgService, tracer);
            logic.CreateSalesOrdersFromOpportunityLabs(opportunityClose.OpportunityId.Id);

            tracer.Trace("End Create Sales Order From Win Plugin");
        }
    }
}
