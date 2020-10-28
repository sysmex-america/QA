using Microsoft.Xrm.Sdk;
using SonomaPartners.Crm.Toolkit.Plugins;
using Sysmex.Crm.Model;
using Sysmex.Crm.IntegrationPlugins.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sysmex.Crm.IntegrationPlugins
{
	public class CalculateImplementationClassificationAndRevenueDatesPlugin : PluginBase
	{
		public override void OnExecute(IServiceProvider serviceProvider)
		{
			var context = serviceProvider.GetPluginExecutionContext();
			var trace = serviceProvider.GetTracingService();

			trace.Trace($"BEGIN {nameof(CalculateImplementationClassificationAndRevenueDatesPlugin)}");

			var orgService = serviceProvider.CreateSystemOrganizationService();
			var logic = new CalculateImplementationClassificationAndRevenueDatesLogic(orgService, trace);

			var product = new smx_implementationproduct();
			var implementation = new smx_implementation();
			var target = ((Entity)context.InputParameters["Target"]);			

			if (target.LogicalName == product.LogicalName)
			{
				product = ((Entity)context.InputParameters["Target"]).ToEntity<smx_implementationproduct>();

				if (context.MessageName.ToLower() == "create")
				{
					logic.SetImplementation(product.smx_ImplementationId);
					if (logic.theImplmentation == null)
					{
						trace.Trace("Missing the implmentation id; nothing can be claculated; exit early;");
						return;
					}

					logic.SetProducts();

					trace.Trace("Start Get Classifications");
					product.smx_Classification = logic.GetClassification(product.smx_Classification, product.smx_EligibleforRevenueDateCalculations, product.Id);

					trace.Trace("Start CalcRevRecDate");
					logic.GetRevRecDate(product.smx_Classification, product.smx_EligibleforRevenueDateCalculations, product.Id);
				}
				else if (context.MessageName.ToLower() == "update" && product.Contains("smx_potentialrevenuedate"))
				{
					trace.Trace("Start Update");
					trace.Trace("Get Potential Rev Date");
					product.smx_PotentialRevenueDate = logic.GetPotentialRevenueDate(product.smx_PotentialRevenueDate);
				}									
			}
		}
	}
}
