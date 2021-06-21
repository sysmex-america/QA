using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sysmex.Crm.Plugins.Logic
{
	public class OpportunityRevenueUpdateLogic
	{
		private IOrganizationService _orgService;
		private ITracingService _tracer;

		public OpportunityRevenueUpdateLogic(IOrganizationService orgService, ITracingService tracer)
		{
			_orgService = orgService;
			_tracer = tracer;
		}
		public void UpdateOpportunityRevenue(Entity productConfig)
		{
			EntityReference cpqQuote = productConfig.Contains("new_quoteid") ? productConfig.GetAttributeValue<EntityReference>("new_quoteid") : null;
			if(cpqQuote!=null)
			{
				Entity finalizedProductConfiguration = GetCpqQuoteFinalizedProductConfigurations(cpqQuote.Id);
				if (finalizedProductConfiguration != null)
					UpdateOpportunityRevenueField(finalizedProductConfiguration);
				else
				{
					Entity maxVersionNumberProductConfiguration = GetCpqQuoteMaxVersionNumberProductConfigurations(cpqQuote.Id);
					if (maxVersionNumberProductConfiguration != null)
						UpdateOpportunityRevenueField(maxVersionNumberProductConfiguration);
				}
			}
		}
		public bool IsPrimaryQuote(Entity productConfig)
		{
			bool isPrimary = false;
			EntityReference cpqQuote = productConfig.Contains("new_quoteid")?productConfig.GetAttributeValue<EntityReference>("new_quoteid"):null;
			if(cpqQuote!=null)
			{
				Entity enCpqQuote = _orgService.Retrieve(cpqQuote.LogicalName, cpqQuote.Id, new ColumnSet("new_isprimary"));
				if(enCpqQuote.Contains("new_isprimary"))
					isPrimary = enCpqQuote.GetAttributeValue<bool>("new_isprimary");
			}
			return isPrimary;
		}
		private Entity GetCpqQuoteFinalizedProductConfigurations(Guid quoteId)
		{
			Entity productConfiguration = null;
			try
			{ 
			var fetch = $@"<fetch>
                             <entity name='new_cpq_productconfiguration'>
                             <attribute name='new_name' />
                             <attribute name='new_cpqstatus' />
                             <attribute name='new_quoteid' />
                             <attribute name='new_versionnumber' />
                             <attribute name='smx_revenue' />
                             <attribute name='new_cpq_productconfigurationid' /> 
							 <attribute name='createdon' />
							   <filter type='and'>
                                   <condition attribute='new_quoteid' operator='eq' value='{quoteId}' />
                                   <condition attribute='new_cpqstatus' operator='eq' value='100000006' />
                              </filter>
                                <link-entity name='new_cpq_quote' from='new_cpq_quoteid' to='new_quoteid' link-type='outer' alias='opportunity'>
                                    <attribute name='new_opportunityid'/>
                                </link-entity>
                            </entity>
                          </fetch>";
				EntityCollection productConfigurationList = _orgService.RetrieveMultiple(new FetchExpression(fetch));
				if (productConfigurationList.Entities.Count() > 0)
				{
					_tracer.Trace("Finalyzed productConfigurations " + productConfigurationList.Entities.Count());
					//Added by Yash on 28-04-2021--Ticket No 60439
					var recentPC =productConfigurationList.Entities?.OrderByDescending(n => n.GetAttributeValue<DateTime>("createdon")).Take(1);
					//productConfiguration = productConfigurationList.Entities.FirstOrDefault();
					productConfiguration = recentPC.ToList().FirstOrDefault();

				}
			}
			catch (Exception)
			{
				return productConfiguration;
			}
			return productConfiguration;
		}
		private Entity GetCpqQuoteMaxVersionNumberProductConfigurations(Guid quoteId)
		{
			Entity productConfiguration = null;
			try
			{
				var fetch = $@"<fetch>
                             <entity name='new_cpq_productconfiguration'>
                             <attribute name='new_name' />
                             <attribute name='new_cpqstatus' />
                             <attribute name='new_quoteid' />
                             <attribute name='new_versionnumber' />
                             <attribute name='smx_revenue' />
							 <attribute name='new_cpq_productconfigurationid' /> 
                               <filter type='and'>
                                   <condition attribute='new_quoteid' operator='eq' value='{quoteId}' />
                              </filter>
                                <link-entity name='new_cpq_quote' from='new_cpq_quoteid' to='new_quoteid' link-type='outer' alias='opportunity'>
                                    <attribute name='new_opportunityid'/>
                                </link-entity>
                            </entity>
                          </fetch>";
				EntityCollection productConfigurationList = _orgService.RetrieveMultiple(new FetchExpression(fetch));
				if (productConfigurationList.Entities.Count() > 0)
				{
					_tracer.Trace("Max Version productConfigurations " + productConfigurationList.Entities.Count());
					var maxRec = productConfigurationList.Entities?.OrderByDescending(n => n.GetAttributeValue<int>("new_versionnumber")).Take(1);
					productConfiguration= maxRec.ToList().FirstOrDefault();
				}

			}
			catch (Exception)
			{
				return productConfiguration;
			}
			return productConfiguration;
		}
		private void UpdateOpportunityRevenueField(Entity productConfiguration)
		{
			AliasedValue pcOpportunity =productConfiguration.Contains("opportunity.new_opportunityid") ? productConfiguration.GetAttributeValue<AliasedValue>("opportunity.new_opportunityid"):null;
			Money quoteRevenueTotal = productConfiguration.Contains("smx_revenue") ? productConfiguration.GetAttributeValue<Money>("smx_revenue"):null;
			if (pcOpportunity != null && quoteRevenueTotal!=null)
			{
				Entity opportunity = new Entity("opportunity",((EntityReference)pcOpportunity.Value).Id);
				opportunity.Attributes.Add("smx_quoterevenue", quoteRevenueTotal.Value);
				_orgService.Update(opportunity);
				_tracer.Trace("Opportunity Quote Revenue field is updated");
			}
		}
	}
}
