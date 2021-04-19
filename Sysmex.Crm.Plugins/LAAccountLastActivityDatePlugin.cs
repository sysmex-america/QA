using Microsoft.Xrm.Sdk;
using SonomaPartners.Crm.Toolkit.Plugins;
using Sysmex.Crm.Plugins.Logic;
using System;

namespace Sysmex.Crm.Plugins
{
	public class LAAccountLastActivityDatePlugin:PluginBase
	{
		public override void OnExecute(IServiceProvider serviceProvider)
		{
			var context = serviceProvider.GetPluginExecutionContext();
			var orgService = serviceProvider.CreateOrganizationServiceAsCurrentUser();
			var trace = serviceProvider.GetTracingService();

			trace.Trace("BEGIN LAAccountLastActivityDatePlugin");

			var input = context.InputParameters.Contains("Target") ? context.InputParameters["Target"] as Entity : null;
			var preImage = context.PreEntityImages.Contains("PreImage") ? context.PreEntityImages["PreImage"] : null;

			var logic = new LAAccountLastActivityDateLogic(orgService, trace);
			logic.UpdateLAAccountLastActivityDate(input, preImage);

			trace.Trace("END LAAccountLastActivityDatePlugin");
		}
	}
}
