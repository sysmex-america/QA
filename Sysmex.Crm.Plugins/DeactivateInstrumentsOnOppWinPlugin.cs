using System;
using SonomaPartners.Crm.Toolkit.Plugins;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.Linq;

namespace Sysmex.Crm.Plugins
{
    public class DeactivateInstrumentsOnOppWinPlugin : PluginBase
    {
        public override void OnExecute(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetPluginExecutionContext();
            var orgService = serviceProvider.CreateOrganizationServiceAsCurrentUser();
            var tracer = serviceProvider.GetTracingService();

            tracer.Trace("Starting Plugin Execution.");

            var opportunity = context.InputParameters.Contains("Target") ? context.GetTargetEntity() : null;

            if (opportunity == null)
            {
                tracer.Trace("No opportunity in context. Exiting...");
                return;
            }

            var status = opportunity.Contains("statecode") ? opportunity.GetAttributeValue<OptionSetValue>("statecode") : null;
            
            if (status == null || status.Value.ToString() != "1")
            {
                tracer.Trace("Opportunity not closed as won. Exiting...");
                return;
            }
            

            var logic = new DeactivateInstrumentsOnOppWinLogic(orgService, tracer);
            logic.DeactivateInstruments(opportunity);
            tracer.Trace("Ending Plugin Execution.");
        }
    }
}