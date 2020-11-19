using Microsoft.Xrm.Sdk;
using SonomaPartners.Crm.Toolkit.Plugins;
using Sysmex.Crm.Model;
using Sysmex.Crm.Plugins.Logic;
using System;

namespace Sysmex.Crm.Plugins
{
    public class CreateInstrumentRecordsPlugin : PluginBase
    {
        public override void OnExecute(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetPluginExecutionContext();
            var tracer = serviceProvider.GetTracingService();
			//Added by Yash on 09-10-2020--Ticket No 58546
			var orgService = serviceProvider.CreateOrganizationService(context.UserId);
			//End
			tracer.Trace("Start Create Instrument Records Plugin");

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
			//Added by Yash on 09-10-2020--Ticket No 58546
			//var logic = new CreateInstrumentRecordsLogic(serviceProvider, tracer);
			var logic = new CreateInstrumentRecordsLogic(serviceProvider, tracer, orgService);
			//End
			logic.CreateInstrumentRecordsFromCPQLineItems(opportunityClose.OpportunityId.Id);

            tracer.Trace("End Create Instrument Records Plugin");
        }
    }
}
