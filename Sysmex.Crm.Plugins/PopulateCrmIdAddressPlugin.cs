using System;
using SonomaPartners.Crm.Toolkit.Plugins;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.Linq;

namespace Sysmex.Crm.Plugins
{
    public class PopulateCrmIdAddressPlugin : PluginBase
    {
        public override void OnExecute(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetPluginExecutionContext();
            var orgService = serviceProvider.CreateOrganizationServiceAsCurrentUser();
            var tracer = serviceProvider.GetTracingService();

            tracer.Trace("Starting PopulateCrmIdAddressPlugin Execution.");

            var address = context.InputParameters.Contains("Target") ? context.GetTargetEntity() : null;

            if (address == null)
            {
                tracer.Trace("No target in context. Exiting...");
                return;
            }

            var logic = new PopulateCrmIdAddressLogic(orgService, tracer);
            logic.PopulateCrmIdField(address);
            tracer.Trace("Ending Plugin Execution.");
        }
    }
}
