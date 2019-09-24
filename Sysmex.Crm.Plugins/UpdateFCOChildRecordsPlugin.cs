using System;
using Microsoft.Xrm.Sdk;
using SonomaPartners.Crm.Toolkit.Plugins;
using Sysmex.Crm.Model;
using Sysmex.Crm.Plugins.Logic;

namespace Sysmex.Crm.Plugins
{
    public class UpdateFCOChildRecordsPlugin : PluginBase
    {
        public override void OnExecute(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetPluginExecutionContext();
            var orgService = serviceProvider.CreateOrganizationServiceAsCurrentUser();
            var tracer = serviceProvider.GetTracingService();

            tracer.Trace("Start Update FCO Child Records Plugin");

            var fcsOpportunity = context.GetInputParameter<Entity>("Target").ToEntity<smx_fcsopportunity>();

            var logic = new UpdateFCOChildRecordsLogic(orgService, tracer);
            logic.UpdateChildRecords(fcsOpportunity);

            tracer.Trace("End Update FCO Child Records Plugin");
        }
    }
}
