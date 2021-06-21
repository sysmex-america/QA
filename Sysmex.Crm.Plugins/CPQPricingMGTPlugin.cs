using Microsoft.Xrm.Sdk;
using SonomaPartners.Crm.Toolkit.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sysmex.Crm.Plugins.Logic;

namespace Sysmex.Crm.Plugins
{
	//Added by Yash on 07-06-2021 - 60439
	public class CPQPricingMGTPlugin : PluginBase
	{
		public override void OnExecute(IServiceProvider serviceProvider)
		{
			var context = serviceProvider.GetPluginExecutionContext();
			var orgService = serviceProvider.CreateOrganizationServiceAsCurrentUser();
			var tracer = serviceProvider.GetTracingService();

			Entity cpqLineItem = null;
			if (context.MessageName.ToLower() == "create")
				cpqLineItem = context.InputParameters.Contains("Target") ? context.InputParameters["Target"] as Entity : null;
			else if (context.MessageName.ToLower() == "update")
				cpqLineItem = context.GetPostEntityImage("CPQLineItemPostImage");
			else
				return;
			tracer.Trace("Calling the CaluculateQuoteRevenueTotal method");
			var logic = new CPQPricingMGTPluginLogic(orgService, tracer);
			logic.CaluculateQuoteRevenueTotal(cpqLineItem);
			tracer.Trace("Logic Executed");
		}
	}
}
