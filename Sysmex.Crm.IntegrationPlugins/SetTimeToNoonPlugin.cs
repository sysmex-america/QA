using Microsoft.Xrm.Sdk;
using SonomaPartners.Crm.Toolkit.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Sysmex.Crm.IntegrationPlugins.Logic;

namespace Sysmex.Crm.IntegrationPlugins
{
	public class SetTimeToNoonPlugin : PluginBase
	{
		private readonly string _unsecureConfig;
		private readonly string _secureConfig;

		public SetTimeToNoonPlugin(string unsecureConfig, string secureConfig)
		{
			_unsecureConfig = unsecureConfig;
			_secureConfig = secureConfig;
		}

		public override void OnExecute(IServiceProvider serviceProvider)
		{
			var context = serviceProvider.GetPluginExecutionContext();	
			var trace = serviceProvider.GetTracingService();
			var target = (Entity)context.InputParameters["Target"];

			if (context.Depth > 2)
			{
				trace.Trace("Depth is > 2");
				return;
			}

			List<string> fieldsToCheck = Regex.Replace(_unsecureConfig, @"\s+", "")
					   .Trim()
					   .Split(',')
					   .Where(field => target.Attributes.Contains(field))
					   .ToList<string>();

			var logic = new SetTimeToNoonLogic(trace);
			logic.SetToNoon(target, fieldsToCheck);
		}
	}
}
