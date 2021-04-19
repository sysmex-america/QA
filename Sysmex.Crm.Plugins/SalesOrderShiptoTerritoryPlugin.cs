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
	//Added by Yash on 16-02-2021 - 60379
	public class SalesOrderShiptoTerritoryPlugin:PluginBase
	{
		public override void OnExecute(IServiceProvider serviceProvider)
		{
			var context = serviceProvider.GetPluginExecutionContext();
			var orgService = serviceProvider.CreateOrganizationServiceAsCurrentUser();
			var tracer = serviceProvider.GetTracingService();

			var saleOrder = context.GetPostEntityImage("SaleOrderPostImage");

			if (context.MessageName.ToLower() == "update")
			{
				var logic = new SalesOrderShiptoTerritoryPluginLogic(orgService, tracer);
				tracer.Trace("Calling UpdateSaleOrder Method");
				logic.UpdateSaleOrder(saleOrder);
				tracer.Trace("UpdateSaleOrder executed");
			}
		}
	}
}
