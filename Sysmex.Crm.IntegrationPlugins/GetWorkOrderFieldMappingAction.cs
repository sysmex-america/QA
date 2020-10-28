using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SonomaPartners.Crm.Toolkit.Plugins;
using Sysmex.Crm.IntegrationPlugins.Logic;
using Sysmex.Crm.Model;
using System;
using System.IO;

namespace Sysmex.Crm.IntegrationPlugins
{
    public class GetWorkOrderFieldMappingAction: PluginBase
    {
        public override void OnExecute(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetPluginExecutionContext();
            var orgService = serviceProvider.CreateOrganizationServiceAsCurrentUser();
            var traceService = serviceProvider.GetTracingService();

            string implementationProductGuid = context.InputParameters["ImplementationProductId"] as string;

            if (string.IsNullOrEmpty(implementationProductGuid))
            {
                traceService.Trace("No implementation product; exit early");
                return;
            }

            traceService.Trace("Implementation Product Guid = {0}", implementationProductGuid);

            var logic = new GetWorkOrderFieldMappingActionLogic(orgService, traceService);
            WorkOrderFieldMappingOutput result = logic.GetWorkOrderFieldMapping(Guid.Parse(implementationProductGuid));

            using (var sw = new StringWriter())
            {
                var settings = new Newtonsoft.Json.JsonSerializerSettings();
                settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                var s = JsonSerializer.Create(settings);
                s.Serialize(sw, result);
                context.OutputParameters["Output"] = sw.ToString();
            }   
        }
    }
}
