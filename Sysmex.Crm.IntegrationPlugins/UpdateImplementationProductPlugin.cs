using Microsoft.Xrm.Sdk;
using SonomaPartners.Crm.Toolkit.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sysmex.Crm.Model;
using Sysmex.Crm.IntegrationPlugins.Logic;

namespace Sysmex.Crm.IntegrationPlugins
{
	public class UpdateImplementationProductPlugin : PluginBase
	{
		public override void OnExecute(IServiceProvider serviceProvider)
		{
			var context = serviceProvider.GetPluginExecutionContext();
			var trace = serviceProvider.GetTracingService();

			var entity = (Entity)context.InputParameters["Target"];

			if (entity.LogicalName == smx_implementation.EntityLogicalName
				|| entity.LogicalName == new_cpq_lineitem_tmp.EntityLogicalName)
			{
				var orgService = serviceProvider.CreateSystemOrganizationService();
				var logic = new UpdateImplementationProductLogic(orgService, trace);
				if (entity.LogicalName == smx_implementation.EntityLogicalName)
				{
					logic.Update(entity.ToEntity<smx_implementation>());
				}
				else
				{
					var lineItem = entity.ToEntity<new_cpq_lineitem_tmp>();
					if (string.IsNullOrWhiteSpace(lineItem.new_name))
					{
						var preImage = ((Entity)context.PreEntityImages["PreImage"]).ToEntity<new_cpq_lineitem_tmp>();
						lineItem.new_name = preImage.new_name;
					}
					logic.Update(lineItem);
				}
			}
		}
	}
}
