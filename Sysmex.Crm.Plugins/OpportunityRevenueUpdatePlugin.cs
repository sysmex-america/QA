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
	public class OpportunityRevenueUpdatePlugin : PluginBase
	{
		public override void OnExecute(IServiceProvider serviceProvider)
		{
			var context = serviceProvider.GetPluginExecutionContext();
			var orgService = serviceProvider.CreateOrganizationServiceAsCurrentUser();
			var tracer = serviceProvider.GetTracingService();
			if (context.MessageName.ToLower() == "update")
			{
				var productConfig = context.GetPostEntityImage("ProductConfigPostImage");
				var logic=new OpportunityRevenueUpdateLogic(orgService, tracer);
				bool IsPrimaryQuote = logic.IsPrimaryQuote(productConfig);
				if(IsPrimaryQuote==true)
				{
					tracer.Trace("Calling the UpdateOpportunityRevenue method");
					logic.UpdateOpportunityRevenue(productConfig);
					tracer.Trace("Logic Executed");
				}
			}
		}
	}
}
