using Microsoft.Xrm.Sdk;
using SonomaPartners.Crm.Toolkit.Plugins;
using Sysmex.Crm.Plugins.Logic;
using System;

namespace Sysmex.Crm.Plugins
{
    public class CreateCommissionRecordsPlugin : PluginBase
    {
        public override void OnExecute(IServiceProvider serviceProvider)
        {

            IPluginExecutionContext executionContext = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            var context = serviceProvider.GetPluginExecutionContext();
            var orgService = serviceProvider.CreateOrganizationServiceAsCurrentUser();
            var tracer = serviceProvider.GetTracingService();

            tracer.Trace("Start Create Commission Records Plugin");
            var entityRef = executionContext.InputParameters["Target"] as EntityReference;
            tracer.Trace("1");

            //var salesOrder = context.GetTargetEntity().ToEntity<smx_salesorder>();
            //Entity commisionEntity = orgService.Retrieve(entityRef.LogicalName, entityRef.Id, new Microsoft.Xrm.Sdk.Query.ColumnSet("smx_contractnumber"));
            //if (!commisionEntity.Contains("smx_contractnumber"))
            //{
            //    tracer.Trace("smx_contractnumber empty, returning");
            //    return;
            //}

            var logic = new CreateCommissionRecordsLogic(orgService, tracer);
            tracer.Trace("guid id=" + entityRef.Id);
            //logic.CreateCommissionRecordsFromSalesOrder(salesOrder.Id);

            logic.CreateCommissionRecordsFromSalesOrder(entityRef.Id);
            tracer.Trace("3");
            tracer.Trace("End Create Commission Records Plugin");
        }
    }
}