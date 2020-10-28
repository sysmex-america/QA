using Microsoft.Xrm.Sdk;
using SonomaPartners.Crm.Toolkit.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sysmex.Crm.Model;
using Sysmex.Crm.IntegrationPlugins.Logic;

namespace Sysmex.Crm.IntegrationPlugins
{
	public class MapImplementationToSalesOrderPlugin : PluginBase
	{
        public override void OnExecute(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetPluginExecutionContext();
            var trace = serviceProvider.GetTracingService();
            var orgService = serviceProvider.CreateSystemOrganizationService();

            trace.Trace($"* BEGIN {nameof(MapImplementationToSalesOrderPlugin)} *");

            if (context.Depth > 1)
            {
                trace.Trace("Depth > 1; Triggered by another plugin; Exit early.");
            }
            else
            {
                var entity = ((Entity)context.InputParameters["Target"]);
                var logic = new MapImplementationToSalesOrderLogic(orgService, trace);
             
                if (entity.Attributes.Contains("smx_wamsite"))
                    trace.Trace($"* smx_wamsite:{entity.GetAttributeValue<OptionSetValue>("smx_wamsite").Value} *");
                if (entity.Attributes.Contains("smx_wamconnects"))
                    trace.Trace($"* smx_connects{entity.GetAttributeValue<OptionSetValue>("smx_wamconnects").Value} *");

                if (entity.LogicalName == smx_implementation.EntityLogicalName)
                {
                    trace.Trace($"* Update Lab - Call Logic UpdateLab() - implementation guid:{entity.Id}  *");
                    var implementation = entity.ToEntity<smx_implementation>();
					var preImage = ((Entity)context.PreEntityImages["PreImage"]).ToEntity<smx_implementation>();
					if (implementation.smx_LabLocationId == null)
					{
						implementation.smx_LabLocationId = preImage.smx_LabLocationId;
					}
					logic.UpdateLab(implementation);
                }                
            }

            trace.Trace($"* End {nameof(MapImplementationToSalesOrderPlugin)} *");
        }
	}
}
