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
	//Added by Yash on 21-10-2020 - 58646
	public class CASSyncPlugin:PluginBase
	{
		public override void OnExecute(IServiceProvider serviceProvider)
		{
			var context = serviceProvider.GetPluginExecutionContext();
			var orgService = serviceProvider.CreateOrganizationServiceAsCurrentUser();
			var tracer = serviceProvider.GetTracingService();

			var lab = context.GetTargetEntity();

			if (context.MessageName.ToLower() == "update")
			{
				var logic = new CASSyncPluginLogic(orgService, tracer);
				logic.UpdateSaleOrder(lab);
			}
		}
	}
}
