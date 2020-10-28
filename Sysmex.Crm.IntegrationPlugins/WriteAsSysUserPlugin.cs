using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SonomaPartners.Crm.Toolkit.Plugins;
using Sysmex.Crm.IntegrationPlugins.Logic;
using Sysmex.Crm.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Sysmex.Crm.IntegrationPlugins
{
	public class WriteAsSysUserPlugin : PluginBase
	{
		private readonly string _unsecureConfig;
		private readonly string _secureConfig;

		public WriteAsSysUserPlugin(string unsecureConfig, string secureConfig)
		{
			_unsecureConfig = unsecureConfig;
			_secureConfig = secureConfig;
		}

		public override void OnExecute(IServiceProvider serviceProvider)
		{
			var context = serviceProvider.GetPluginExecutionContext();
			var traceService = serviceProvider.GetTracingService();

			traceService.Trace($"Start {nameof(WriteAsSysUserPlugin)}");

			if (context.Depth > 1)
			{
				traceService.Trace("Depth > 1; Exit Early.");
				return;
			}

			var orgService = serviceProvider.CreateSystemOrganizationService();

			var implementationProduct = ((Entity)context.InputParameters["Target"]).ToEntity<smx_implementationproduct>();

			List<string> fieldsToCheck = Regex.Replace(_unsecureConfig, @"\s+", "")
						.Trim()
						.Split(',')
						.Where(field => implementationProduct.Attributes.Contains(field))
						.ToList<string>();

			var logic = new WriteAsSysUserLogic(orgService, traceService);
			logic.WriteAsSysUser(implementationProduct, fieldsToCheck);

			traceService.Trace($"End {nameof(WriteAsSysUserPlugin)}");
		}
	}
}