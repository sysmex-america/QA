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
	public class SendImplementationDatesToSAPPlugin : PluginBase
	{
		public override void OnExecute(IServiceProvider serviceProvider)
		{
			var context = serviceProvider.GetPluginExecutionContext();
			var trace = serviceProvider.GetTracingService();

			trace.Trace("BEGIN SendImplmentationDatesToSAPPlugin");

			if (!context.InputParameters.Contains("Target"))
			{
				trace.Trace("Target not found within InputParameters, returning.");
				return;
			}

			var product = ((Entity)context.InputParameters["Target"]).ToEntity<smx_implementationproduct>();

			trace.Trace($@"Changed Dates -- ICN:{product.smx_ICNDate}; Go Live: {product.smx_GoLiveDate}; 
						Install: {product.smx_ServiceConfirmedInstallDate}; Potential Rev Date: {product.smx_PotentialRevenueDate}");

			if (product.smx_ICNDate.HasValue == false && product.smx_GoLiveDate.HasValue == false 
				&& product.smx_ServiceConfirmedInstallDate.HasValue == false && product.smx_PotentialRevenueDate.HasValue == false)
			{
				trace.Trace("None of the fields that need to be sent to SAP have changed; exit early");
				return;
			}

			if (context.PreEntityImages.Contains("PreImage"))
			{
				var preImage = ((Entity)context.GetPreEntityImage("PreImage")).ToEntity<smx_implementationproduct>();
				if (product.Contains("smx_cpqlineitemid") == false)
				{
					product.smx_CPQLineItemId = preImage.smx_CPQLineItemId;
				}
				if (product.Contains("smx_implementationid") == false)
				{
					product.smx_ImplementationId = preImage.smx_ImplementationId;
				}
			}

			if (ValidData(product, trace))
			{
				var orgService = serviceProvider.CreateSystemOrganizationService();
				var logic = new SendImplementationDatesToSAPLogic(orgService, trace);
				logic.SendToSAP(product.smx_ICNDate, product.smx_GoLiveDate, product.smx_ServiceConfirmedInstallDate, product.smx_PotentialRevenueDate,
									product.smx_CPQLineItemId, product.smx_ImplementationId);
			}

			trace.Trace("END SendImplmentationDatesToSAPPlugin");
		}

		private bool ValidData(smx_implementationproduct product, ITracingService trace)
		{
			if (product == null)
			{
				trace.Trace("PreImage is null; Please contact a System Admin.");
				return false;
			}

			if (string.IsNullOrWhiteSpace(product.smx_CPQLineItemId))
			{
				trace.Trace("Missing CPQ Line Item Id; Please contact a System Admin.");
				return false;
			}

			if (product.smx_ImplementationId == null)
			{
				trace.Trace("Missing Implementation Id; Please contact a System Admin.");
				return false;
			}

			return true;
		}
	}
}

