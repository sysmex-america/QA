using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using SonomaPartners.Crm.Toolkit;
using Sysmex.Crm.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Sysmex.Crm.Plugins.Logic
{
	public class CPQPricingMGTPluginLogic
	{
		private IOrganizationService _orgService;
		private ITracingService _tracer;
		public CPQPricingMGTPluginLogic(IOrganizationService orgService, ITracingService tracer)
		{
			_orgService = orgService;
			_tracer = tracer;
		}
		public void CaluculateQuoteRevenueTotal(Entity cpqLineitem)
		{
			_tracer.Trace("Entered CaluculateQuoteRevenueTotal Method");
			if (cpqLineitem.Contains("new_productconfigurationid"))
			{
				_tracer.Trace("productconfiguration and quotarevenue is not-null");
				EntityReference productConfiguration = cpqLineitem.GetAttributeValue<EntityReference>("new_productconfigurationid");
				_tracer.Trace("ProductConfiguration :"+ productConfiguration);
				decimal quoteRevenueTotal = 0;
				if (productConfiguration != null)
				{
					quoteRevenueTotal = GetTotalLineItemsQuoteRevenue(productConfiguration.Id);
					_tracer.Trace("quoteRevenueTotal :" + quoteRevenueTotal);
					Entity enProductConfiguration = new Entity(productConfiguration.LogicalName, productConfiguration.Id);
					enProductConfiguration["smx_revenue"] = new Money(quoteRevenueTotal);
					_orgService.Update(enProductConfiguration);
					_tracer.Trace("Updated Product Configuration QuoteRevenueFieldValue");
				}
			}
			_tracer.Trace("Exit CaluculateQuoteRevenueTotal Method");
		}
		
		private decimal GetTotalLineItemsQuoteRevenue(Guid ProductConfigurationId)
		{
			_tracer.Trace("Entered GetProductConfigurationDetails Method");
			decimal TotalQuoteRevenue = 0;
			try
			{
				var fetch = $@"<fetch>
									  <entity name='new_cpq_lineitem_tmp'>
										<attribute name='new_name' />
										<attribute name='new_cpq_lineitem_tmpid' />
										<attribute name='smx_quotarevenue' />
										<attribute name='new_productconfigurationid' />
										<order attribute='new_name' descending='false' />
										<filter type='and'>
										  <condition attribute='new_productconfigurationid' operator='eq' value='{ProductConfigurationId}' />
										  <condition attribute='smx_quotarevenue' operator='not-null' />
										</filter>
									  </entity>
									</fetch>";
				EntityCollection lineItems = _orgService.RetrieveMultiple(new FetchExpression(fetch));
				if (lineItems.Entities.Count() > 0)
				{
					_tracer.Trace("LineItems " + lineItems.Entities.Count());
					foreach (Entity lineItem in lineItems.Entities)
					{
						TotalQuoteRevenue = TotalQuoteRevenue + lineItem.GetAttributeValue<Money>("smx_quotarevenue").Value;
					}
				}
			}
			catch (Exception)
			{
				_tracer.Trace("Exit GetTotalLineItemsQuoteRevenue Method");
				return TotalQuoteRevenue;
			}
			_tracer.Trace("Exit GetTotalLineItemsQuoteRevenue Method");
			return TotalQuoteRevenue;
		}


	}
}
