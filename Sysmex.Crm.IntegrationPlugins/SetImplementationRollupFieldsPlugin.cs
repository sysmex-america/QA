using Microsoft.Xrm.Sdk;
using SonomaPartners.Crm.Toolkit.Plugins;
using Sysmex.Crm.Model;
using Sysmex.Crm.IntegrationPlugins.Logic;
using System;

namespace Sysmex.Crm.IntegrationPlugins
{
    public class SetImplementationRollupFieldsPlugin : PluginBase
    {
        public override void OnExecute(IServiceProvider serviceProvider)
        {
            var trace = serviceProvider.GetTracingService();
            trace.Trace($"*** Begin Plugin {nameof(SetImplementationRollupFieldsPlugin)} ***");

            var context = serviceProvider.GetPluginExecutionContext();
            var orgService = serviceProvider.CreateSystemOrganizationService();

            if (context.InputParameters.Contains("Target")) 
            {
                trace.Trace("Target found within InputParameters.");

                Entity targetEntity;

                if (context.InputParameters["Target"] is Entity) //update or create message
                {
                    trace.Trace("Target Entity found within input parameters");
                    if (context.PostEntityImages.Contains("PostImage"))
                    {
                        trace.Trace("Retrieving PostImage");
                        targetEntity = (Entity)context.GetPostEntityImage("PostImage");
                    }
                    else
                    {
                        trace.Trace("Retrieving Target Image");
                        targetEntity = ((Entity)context.InputParameters["Target"]);
                    }
                }
                else //delete message
                {
                    trace.Trace("Target Entity Reference found within input parameters");
                    if (!context.PreEntityImages.Contains("PreImage"))
                    {
                        trace.Trace("PreImage not found, returning.");
                        return;
                    }
                    targetEntity = context.GetPreEntityImage("PreImage");
                }

                if (targetEntity.LogicalName.Equals(smx_implementationproduct.EntityLogicalName))
                {
                    trace.Trace($"Valid logicalname: {targetEntity.LogicalName}:{targetEntity.Id}");
                    var implProduct = targetEntity.ToEntity<smx_implementationproduct>();
                    if (implProduct.smx_ImplementationId != null)
                    {
                        trace.Trace($"Call logic execution for implementation guid: {implProduct.smx_ImplementationId.Id}");
                        var logic = new SetImplementationRollupFieldsLogic(orgService, trace, implProduct.smx_ImplementationId.Id);
                        logic.SetImplementationRollupFields();
                    }
                    else
                    {
                        trace.Trace("Invalid implProduct.smx_ImplementationId");
                    }
                }
            }

            trace.Trace($"*** End Plugin {nameof(SetImplementationRollupFieldsPlugin)} ***");
        }
    }
}
