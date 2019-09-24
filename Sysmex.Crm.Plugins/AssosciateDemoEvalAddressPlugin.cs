using Microsoft.Xrm.Sdk;
using SonomaPartners.Crm.Toolkit.Plugins;
using Sysmex.Crm.Plugins.Logic;
using System;

namespace Sysmex.Crm.Plugins
{
    public class AssosciateDemoEvalAddressPlugin : PluginBase
    {
        public override void OnExecute(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetPluginExecutionContext();
            var orgService = serviceProvider.CreateOrganizationServiceAsCurrentUser();
            var trace = serviceProvider.GetTracingService();

            trace.Trace("BEGIN AssosciateDemoEvalAddressPlugin");

            if (!context.InputParameters.Contains("Target"))
            {
                trace.Trace("Target not found within InputParameters, returning.");
                return;
            }

            var input = context.InputParameters["Target"] as Entity;
            var logic = new AssosciateDemoEvalAddressLogic(orgService, trace);
            logic.CopyAddressFields(input);

            trace.Trace("END AssosciateDemoEvalAddressPlugin");
        }
    }
}
