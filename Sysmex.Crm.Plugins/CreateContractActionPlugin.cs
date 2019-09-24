using System;
using Microsoft.Xrm.Sdk;
using SonomaPartners.Crm.Toolkit.Plugins;
using Sysmex.Crm.Plugins.Logic;

namespace Sysmex.Crm.Plugins
{
    public class CreateContractActionPlugin : PluginBase
    {
        public override void OnExecute(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetPluginExecutionContext();
            var orgService = serviceProvider.CreateOrganizationServiceAsCurrentUser();
            var systemOrgService = serviceProvider.CreateSystemOrganizationService();
            var tracer = serviceProvider.GetTracingService();

            tracer.Trace("Start Create Contract Action Plugin");

            Guid salesOrderId;
            var inputIsGuid = Guid.TryParse(context.GetInputParameter<string>("Input"), out salesOrderId);
            if (inputIsGuid == false)
            {
                throw new InvalidPluginExecutionException("The ID is not a valid GUID.");
            }

            var logic = new CreateContractActionLogic(systemOrgService, orgService, tracer);
            var contractId = logic.CreateContract(salesOrderId);
            context.OutputParameters["Output"] = contractId;

            tracer.Trace("End Create Contract Action Plugin");
        }
    }
}
