using SonomaPartners.Crm.Toolkit.Plugins;
using Sysmex.Crm.Model;
using Sysmex.Crm.Plugins.Logic;
using System;

namespace Sysmex.Crm.Plugins
{
    public class CreateCommissionRecordsPlugin : PluginBase
    {
        public override void OnExecute(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetPluginExecutionContext();
            var orgService = serviceProvider.CreateOrganizationServiceAsCurrentUser();
            var tracer = serviceProvider.GetTracingService();
                                                                                                                                                    
            tracer.Trace("Start Create Commission Records Plugin");


            var salesOrder = context.GetTargetEntity().ToEntity<smx_salesorder>();

            if (String.IsNullOrWhiteSpace(salesOrder.smx_ContractNumber))
            {
                tracer.Trace("smx_contractnumber empty, returning");
                return;
            }

            var logic = new CreateCommissionRecordsLogic(orgService, tracer);
            logic.CreateCommissionRecordsFromSalesOrder(salesOrder.Id);

            tracer.Trace("End Create Commission Records Plugin");
        }
    }
}