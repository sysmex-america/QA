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
	public class CreateImplementationProductRecordsPlugin : PluginBase
	{
		public override void OnExecute(IServiceProvider serviceProvider)
		{
			var context = serviceProvider.GetPluginExecutionContext();
			var trace = serviceProvider.GetTracingService();	

			if (!context.InputParameters.Contains("Target"))
			{
				trace.Trace("Target not found within InputParameters, returning.");
				return;
			}

			var orgService = serviceProvider.CreateSystemOrganizationService();
			var logic = new CreateImplementationProductRecordsLogic(orgService, trace);
			var entity = (Entity)context.InputParameters["Target"];

			if (entity.LogicalName == smx_implementation.EntityLogicalName)
			{
				trace.Trace("Create from Implmentation");
				var implementation = entity.ToEntity<smx_implementation>();

				smx_implementation preImage;
				if (context.PreEntityImages.Contains("PreImage"))
				{
					preImage = ((Entity)context.PreEntityImages["PreImage"]).ToEntity<smx_implementation>();
					if (implementation.smx_SalesOrderId == null)
					{
						implementation.smx_SalesOrderId = preImage.smx_SalesOrderId;
					}
					if (implementation.smx_ContractNumber == null)
					{
						implementation.smx_ContractNumber = preImage.smx_ContractNumber;
					}
					if (implementation.OwnerId == null)
					{
						implementation.OwnerId = preImage.OwnerId;
					}
					if (implementation.smx_AddressTimeZone == null)
					{
						implementation.smx_AddressTimeZone = preImage.smx_AddressTimeZone;
					}
				}

				if (implementation.smx_SalesOrderId == null)
				{
					trace.Trace("The Sales Order is missing from the Implementation record.Please correct and try again.");
					return;
				}

				logic.CreateImplementationProducts(implementation);
			}
			else if (entity.LogicalName == new_cpq_lineitem_tmp.EntityLogicalName)
			{
				trace.Trace("Create from Quote Line Item");
				var lineItem = entity.ToEntity<new_cpq_lineitem_tmp>();
				if (context.PreEntityImages.Contains("PreImage"))
				{
					var preImage = ((Entity)context.PreEntityImages["PreImage"]).ToEntity<new_cpq_lineitem_tmp>();
					if (lineItem.smx_SalesOrderId == null)
					{
						lineItem.smx_SalesOrderId = preImage.smx_SalesOrderId;
					}
					if (string.IsNullOrWhiteSpace(lineItem.new_name))
					{
						lineItem.new_name = preImage.new_name;
					}
				}

				logic.CreateImplementationProduct(lineItem);
			}
		}
	}
}

