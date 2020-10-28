using Microsoft.Xrm.Sdk;
using SonomaPartners.Crm.Toolkit.Plugins;
using Sysmex.Crm.IntegrationPlugins.Logic;
using Sysmex.Crm.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sysmex.Crm.IntegrationPlugins
{
    public class MapSalesOrderToImplementationPlugin : PluginBase
    {
		public override void OnExecute(IServiceProvider serviceProvider)
		{
			var context = serviceProvider.GetPluginExecutionContext();			
			var trace = serviceProvider.GetTracingService();

			trace.Trace($"BEGIN {nameof(MapSalesOrderToImplementationPlugin)}");

			if (!context.InputParameters.Contains("Target"))
			{
				trace.Trace("Target not found within InputParameters, returning.");
				return;
			}

			var input = context.InputParameters["Target"] as Entity;		
			var orgService = serviceProvider.CreateSystemOrganizationService();
			var logic = new MapSalesOrderToImplementationLogic(orgService, trace);

			if (input.LogicalName == smx_salesorder.EntityLogicalName)
			{
				var salesOrder = input.ToEntity<smx_salesorder>();
				var preImage = context.PreEntityImages["PreImage"] as Entity;
				Merge(input, preImage);
				var nextStep = logic.ValidForImplementationCreation(salesOrder);

				if (nextStep == ImplmentationCreationOptions.CreateImplementationProducts)
				{
					logic.CreateImplementationProducts(salesOrder);
				}
				else if (nextStep == ImplmentationCreationOptions.Create)
				{
					logic.CreateImplementation(input);
				}
				else
				{
					logic.UpdateImplementation(input);
				}
			}
			else if (input.LogicalName == smx_salesorderbusinessprocess.EntityLogicalName)
			{
				var process = input.ToEntity<smx_salesorderbusinessprocess>();
				var postImage = ((Entity)context.PostEntityImages["PostImage"]).ToEntity<smx_salesorderbusinessprocess>();
				logic.UpdateImplementationProducts(postImage.bpf_smx_salesorderid, process.ActiveStageId);
			}
			else
			{
				logic.UpdateImplementation(input);
			}

			trace.Trace("END CreateImplmentationRecordPlugin");
		}

		private void Merge(Entity input, Entity preImage)
		{
			var fieldNames = new string[] { "smx_approvaltype", "smx_contractnumber", "smx_poreceived", "smx_purchaseorder" };

			foreach (var field in fieldNames)
			{
				if ((input.Contains(field) == false || input.Attributes[field] == null) && preImage.Contains(field))
				{
					input[field] = preImage[field];
				}
			}
			
		}
	}

	public enum ImplmentationCreationOptions
	{
		Create = 0,
		Update = 1,
		CreateImplementationProducts = 2
	}
}
