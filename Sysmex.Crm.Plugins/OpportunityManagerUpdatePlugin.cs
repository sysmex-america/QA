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
	public class OpportunityManagerUpdatePlugin : PluginBase
	{
		public override void OnExecute(IServiceProvider serviceProvider)
		{
			var context = serviceProvider.GetPluginExecutionContext();
			var orgService = serviceProvider.CreateOrganizationServiceAsCurrentUser();
			var tracer = serviceProvider.GetTracingService();

			var account = context.GetTargetEntity();

			if (context.MessageName.ToLower() == "update") 
			{
				//Added by Yash on 29-09-2020 - 58345
				tracer.Trace("Depth Is "+context.Depth);
				if (context.Depth == 3 || context.Depth == 1)
				{
					var logic = new OpportunityManagerUpdateLogic(orgService, tracer);
					if (logic.IsHSAMRole(account))
					{
						tracer.Trace("HSAM Role");
						logic.UpdateOpportunityManager(account);
					}
				}
			}
		}
			
	}
}
