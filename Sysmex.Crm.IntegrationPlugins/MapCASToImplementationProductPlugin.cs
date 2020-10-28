using SonomaPartners.Crm.Toolkit.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sysmex.Crm.IntegrationPlugins.Logic;
using Microsoft.Xrm.Sdk;
using Sysmex.Crm.Model;

namespace Sysmex.Crm.IntegrationPlugins
{
	public class MapCASToImplementationProductPlugin : PluginBase
	{
		public override void OnExecute(IServiceProvider serviceProvider)
		{
			var context = serviceProvider.GetPluginExecutionContext();
			var trace = serviceProvider.GetTracingService();

			trace.Trace($"Start {nameof(MapCASToImplementationProductPlugin)}");
			EntityReference entityReference;

			if (context.MessageName.ToLower() == "delete")
			{
				entityReference = ((EntityReference)context.InputParameters["Target"]);
			}
			else
			{
				var entity = ((Entity)context.InputParameters["Target"]);
				entityReference = entity.ToEntityReference();
			}
			
			var orgService = serviceProvider.CreateSystemOrganizationService();
			var logic = new MapCASToImplementationProductLogic(orgService, trace);

			logic.Map(entityReference);

			trace.Trace($"End {nameof(MapCASToImplementationProductPlugin)}");
		}
	}
}
