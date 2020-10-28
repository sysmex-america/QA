using Microsoft.Xrm.Sdk;
using SonomaPartners.Crm.Toolkit.Plugins;
using Sysmex.Crm.IntegrationPlugins.Logic;
using Sysmex.Crm.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sysmex.Crm.IntegrationPlugins
{
	public class SetImplementationFieldsOnUpdatePlugin : PluginBase
	{
		public override void OnExecute(IServiceProvider serviceProvider)
		{
			var context = serviceProvider.GetPluginExecutionContext();			
			var tracer = serviceProvider.GetTracingService();

			if (context.Depth > 1)
			{
				tracer.Trace("Depth > 1; Triggered by another plugin; Exit early.");
				return;
			}

			var entity = (Entity)context.InputParameters["Target"];
			if (!entity.Contains("ownerid") && !entity.Contains("statecode") && !entity.Contains("smx_projectmanagerid") && !entity.Contains("smx_totalproductsicnremainingrollup"))
			{
				tracer.Trace("No changes require feild updates.  Exit Early.");
				return;
			}

			var preImage = (Entity)context.PreEntityImages["PreImage"];
			var service = serviceProvider.CreateOrganizationServiceAsCurrentUser();
			var logic = new SetImplementationFieldsOnUpdateLogic(service, tracer);
			var implementation = mergeImplementation(entity.ToEntity<smx_implementation>(), preImage?.ToEntity<smx_implementation>());
			logic.UpdateFields(implementation);
		}

		private smx_implementation mergeImplementation(smx_implementation entity, smx_implementation preImage)
		{
			if (preImage != null)
			{
				var fieldNames = new string[]{ "statecode", "ownerid", "smx_projectmanagerid", "smx_totalproductsicnremainingrollup" };

				foreach(var field in fieldNames)
				{
					if (preImage.Contains(field) && preImage[field] != null)
					{
						if (entity.Contains(field) == false)
						{
							entity.Attributes.Add(field, preImage[field]);
						}
						else if (entity[field] == null)
						{
							entity[field] = preImage[field];
						}
					}
				}
			}

			return entity;
		}
	}
}
