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
            var context = serviceProvider.GetPluginExecutionContext();
            var orgService = serviceProvider.CreateOrganizationServiceAsCurrentUser();
            var tracer = serviceProvider.GetTracingService();
                                                                                                                                                    
            tracer.Trace("Start Create Commission Records Plugin");
            
            var targetRef = context.GetInputParameter<EntityReference>("Target"); //salesorder
            
            var logic = new CreateCommissionRecordsLogic(orgService, tracer);
            logic.CreateCommissionRecordsFromSalesOrder(targetRef.Id);

            tracer.Trace("End Create Commission Records Plugin");
        }
    }
}