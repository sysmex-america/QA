using Microsoft.Xrm.Sdk;
using SonomaPartners.Crm.Toolkit.Plugins;
using Sysmex.Crm.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sysmex.Crm.IntegrationPlugins
{
	public class AutoPopulateMissingICNData : PluginBase
	{
		public override void OnExecute(IServiceProvider serviceProvider)
		{
			var context = serviceProvider.GetPluginExecutionContext();
			var trace = serviceProvider.GetTracingService();
			trace.Trace($"BEGIN {nameof(AutoPopulateICNDatePlugin)}");

			var product = ((Entity)context.InputParameters["Target"]).ToEntity<smx_implementationproduct>();
			if (product.smx_ICNComplete.HasValue && product.smx_ICNComplete.Value == true)
			{
				trace.Trace("ICN Complete set to yes");
				if (product.Contains("smx_icndate") == false)
				{
					trace.Trace("Set ICN Date");
					product.smx_ICNDate = DateTime.Now;
				}
				if (product.Contains("smx_icncompletedbyid") == false)
				{
					trace.Trace("Set ICN Completed By");
					product.smx_ICNCompletedById = new EntityReference(SystemUser.EntityLogicalName, context.InitiatingUserId);
				}
			}
			else if (product.smx_ICNComplete.HasValue && product.smx_ICNComplete.Value == false)
			{
				trace.Trace("ICN Complete set to no");
				if (product.Contains("smx_icndate") == false)
				{
					trace.Trace("Set ICN Date");
					product.smx_ICNDate = null;
				}
				if (product.Contains("smx_icncompletedbyid") == false)
				{
					trace.Trace("Set ICN Completed By");
					product.smx_ICNCompletedById = null;
				}
			}
			else if (product.Contains("smx_icndate"))
			{
				trace.Trace("ICN Date Changed");
				if (product.smx_ICNDate.HasValue)
				{
					trace.Trace("ICN Date Has Value");
					if (product.Contains("smx_icncompletedbyid") == false)
					{
						trace.Trace("Set ICN Completed By");
						product.smx_ICNCompletedById = new EntityReference(SystemUser.EntityLogicalName, context.InitiatingUserId); 
					}
					if (product.Contains("smx_icncomplete") == false)
					{
						trace.Trace("Set ICN Complete");
						product.smx_ICNComplete = true; 
					}
				}
				else
				{
					trace.Trace("ICN Date is cleared");
					if (product.Contains("smx_icncompletedbyid") == false)
					{
						trace.Trace("Clear ICN Completed By");
						product.smx_ICNCompletedById = null;
					}
					if (product.Contains("smx_icncomplete") == false)
					{
						trace.Trace("Clear ICN Complete");
						product.smx_ICNComplete = false;
					}
				}
			}

			trace.Trace($"End {nameof(AutoPopulateICNDatePlugin)}");
		}
	}
}
