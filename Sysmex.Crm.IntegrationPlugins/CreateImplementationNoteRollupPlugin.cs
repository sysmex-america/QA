using Microsoft.Xrm.Sdk;
using SonomaPartners.Crm.Toolkit.Plugins;
using Sysmex.Crm.IntegrationPlugins.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sysmex.Crm.Model;

namespace Sysmex.Crm.IntegrationPlugins
{
    public class CreateImplementationNoteRollupPlugin : PluginBase
    {
		public override void OnExecute(IServiceProvider serviceProvider)
		{            
            var context = serviceProvider.GetPluginExecutionContext();
			var orgService = serviceProvider.CreateSystemOrganizationService();
            var trace = serviceProvider.GetTracingService();

            trace.Trace("*** BEGIN CreateImplementationNoteRollupPlugin ***");


			Entity input = new Entity();

			if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is EntityReference)
            {
                trace.Trace("Target found within InputParameters.");

                if (!context.PreEntityImages.Contains("PreImage"))
                {
                    trace.Trace("PreImage not found within PreEntityImages, returning.");
                    return;
                }

                input = context.GetPreEntityImage("PreImage");
            }
			else
			{
                trace.Trace("Target not found within InputParameters.");

                if (!context.PostEntityImages.Contains("PostImage"))
				{
					trace.Trace("PostImage not found within PostEntityImages, returning.");
					return;
				}

				input = context.GetPostEntityImage("PostImage");
			}


            if (input.GetAttributeValue<EntityReference>("objectid") != null)
            {
                trace.Trace("Valid ObjectID");

                if (input.GetAttributeValue<EntityReference>("objectid").LogicalName.Equals(smx_implementation.EntityLogicalName))
                {
                    trace.Trace("Valid Target: annotation: " + input.Id.ToString());

                    var logic = new CreateImplementationNoteRollupLogic(orgService, trace);
                    logic.CreateImplementationNoteRollup(input);
                }
            }

			trace.Trace("*** END CreateImplementationNoteRollupPlugin ***");
		}
	}
}
