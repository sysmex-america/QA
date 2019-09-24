using System;
using Microsoft.Xrm.Sdk;
using SonomaPartners.Crm.Toolkit.Plugins;
using Sysmex.Crm.Model;
using Sysmex.Crm.Plugins.Logic;

namespace Sysmex.Crm.Plugins
{
    public class PopulatePriceListOnProductPlugin : PluginBase
    {
        public override void OnExecute(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetPluginExecutionContext();
            var orgService = serviceProvider.CreateOrganizationServiceAsCurrentUser();
            var tracer = serviceProvider.GetTracingService();

            tracer.Trace("Start PopulatePriceListOnProductPlugin Execution.");

            var flowProduct = context.GetInputParameter<Entity>("Target").ToEntity<smx_flowcytometryproduct>();
            var flowProductPreimage = context.PreEntityImages.Contains("Target") ?
                context.GetPreEntityImage("Target").ToEntity<smx_flowcytometryproduct>()
                : null;

            var logic = new PopulatePriceListOnProductLogic(orgService, tracer);
            logic.PopulatePriceListAndListPrice(flowProduct, flowProductPreimage);

            tracer.Trace("End PopulatePriceListOnProductPlugin Execution.");
        }
    }
}
