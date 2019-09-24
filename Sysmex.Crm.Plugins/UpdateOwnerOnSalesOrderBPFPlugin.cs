using System;
using Microsoft.Xrm.Sdk;
using SonomaPartners.Crm.Toolkit.Plugins;
using Sysmex.Crm.Model;

namespace Sysmex.Crm.Plugins
{
    public class UpdateOwnerOnSalesOrderBPFPlugin : PluginBase
    {
        public override void OnExecute(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetPluginExecutionContext();
            var orgService = serviceProvider.CreateOrganizationServiceAsCurrentUser();
            var tracer = serviceProvider.GetTracingService();

            tracer.Trace("Start Update Owner On Sales Order BPF Plugin");

            var salesOrderBP = context.GetInputParameter<Entity>("Target").ToEntity<smx_salesorderbusinessprocess>();
            var logic = new UpdateOwnerOnSalesOrderBPFLogic(orgService, tracer);
            logic.UpdateOwner(salesOrderBP);

            tracer.Trace("End Update Owner On Sales Order BPF Plugin");
        }
    }
}
