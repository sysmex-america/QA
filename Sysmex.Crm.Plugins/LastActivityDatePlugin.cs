using Microsoft.Xrm.Sdk;
using SonomaPartners.Crm.Toolkit.Plugins;
using Sysmex.Crm.Plugins.Logic;
using System;

namespace Sysmex.Crm.Plugins
{
    public class LastActivityDatePlugin : PluginBase
    {
        public override void OnExecute(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetPluginExecutionContext();
            var orgService = serviceProvider.CreateOrganizationServiceAsCurrentUser();
            var trace = serviceProvider.GetTracingService();

            trace.Trace("BEGIN LastActivityDatePlugin");

            var input = context.InputParameters.Contains("Target") ? context.InputParameters["Target"] as Entity : null;
            var preImage = context.PreEntityImages.Contains("PreImage") ? context.PreEntityImages["PreImage"] : null;

            var logic = new LastActivityDateLogic(orgService, trace);
            logic.UpdateLastActivityDate(input, preImage);

            trace.Trace("END LastActivityDatePlugin");
        }
    }
}
