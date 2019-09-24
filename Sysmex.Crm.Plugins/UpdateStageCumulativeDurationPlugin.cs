using System;
using Microsoft.Xrm.Sdk;
using SonomaPartners.Crm.Toolkit.Plugins;
using Sysmex.Crm.Model;
using Sysmex.Crm.Plugins.Logic;

namespace Sysmex.Crm.Plugins
{
    public class UpdateStageCumulativeDurationPlugin : PluginBase
    {
        public override void OnExecute(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetPluginExecutionContext();
            var orgService = serviceProvider.CreateOrganizationServiceAsCurrentUser();
            var tracer = serviceProvider.GetTracingService();

            tracer.Trace("Start Update Stage Cumulative Duration Plugin");

            var stageDuration = context.GetInputParameter<Entity>("Target").ToEntity<smx_stageduration>();
            var stageDurationPreimage = context.PreEntityImages.Contains("Target")
                ? context.GetPreEntityImage("Target").ToEntity<smx_stageduration>()
                : null;

            var logic = new UpdateStageCumulativeDurationLogic(orgService, tracer);
            logic.ProcessCumulativeDurations(stageDuration, stageDurationPreimage);

            tracer.Trace("End Update Stage Cumulative Duration Plugin");
        }
    }
}
