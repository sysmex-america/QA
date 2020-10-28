using SonomaPartners.Crm.Toolkit.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sysmex.Crm.IntegrationPlugins.Logic;
using Sysmex.Crm.Model;
using Microsoft.Xrm.Sdk;

namespace Sysmex.Crm.IntegrationPlugins
{
	public class CalculateActualRevenueDatePlugin : PluginBase
	{
		public override void OnExecute(IServiceProvider serviceProvider)
		{
			var context = serviceProvider.GetPluginExecutionContext();
			var trace = serviceProvider.GetTracingService();
			trace.Trace($"BEGIN {nameof(CalculateActualRevenueDatePlugin)}");

			var product = ((Entity)context.InputParameters["Target"]).ToEntity<smx_implementationproduct>();
			
			if (product.smx_ActualRevenueDate.HasValue)
			{
				trace.Trace($"Date is set");
				var orgService = serviceProvider.CreateOrganizationServiceAsCurrentUser();

				var logic = new CalculateActualRevenueDateLogic(orgService, trace);
				product.smx_ActualRevenueDate = logic.CalculateActualRevenueDate(product.smx_ActualRevenueDate.Value);  
			}
			else
			{
				trace.Trace($"Date not set");
			}

			trace.Trace($"END {nameof(CalculateActualRevenueDatePlugin)}");
		}
	}
}
