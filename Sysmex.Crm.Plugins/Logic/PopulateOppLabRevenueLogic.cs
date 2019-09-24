using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.Linq;
using System;
using SonomaPartners.Crm.Toolkit;
using System.Reflection;
using Microsoft.Xrm.Sdk.Messages;
using System.Collections.Generic;

namespace Sysmex.Crm.Plugins
{
    class PopulateOppLabRevenueLogic
    {
        private IOrganizationService _orgService;
        private ITracingService _tracer;

        public PopulateOppLabRevenueLogic(IOrganizationService orgService, ITracingService tracer)
        {
            _orgService = orgService;
            _tracer = tracer;
        }

        public void PopulateRevenues(Entity quoteLine, Entity image)
        {
            _tracer.Trace("Entering PopulateRevenues Logic.");

            if (quoteLine != null && quoteLine.Contains("new_quoteid"))
            {
                Guid quoteId = quoteLine.GetAttributeValue<EntityReference>("new_quoteid").Id;
                UpdateOpportunityLabRevenue(quoteId);
            }

            if (image != null && image.Contains("new_quoteid"))
            {
                Guid oldQuoteId = image.GetAttributeValue<EntityReference>("new_quoteid").Id;
                if ((quoteLine == null || !quoteLine.Contains("new_quoteid")) || (oldQuoteId != null && oldQuoteId != quoteLine.GetAttributeValue<EntityReference>("new_quoteid").Id))
                {
                    UpdateOpportunityLabRevenue(oldQuoteId);
                }
            }
            _tracer.Trace("Exiting Logic.");

        }
        private void UpdateOpportunityLabRevenue(Guid quoteId)
        {
            var oppLabs = RetrieveOpportunityLabs(quoteId);
            if (oppLabs == null)
            {
                _tracer.Trace("No active opportunity labs found. Exiting...");
                return;
            }

            foreach (Entity oppLab in oppLabs)
            {
                EntityReference addressReference = oppLab.GetAttributeValue<EntityReference>("smx_shiptoaddressid");
                EntityReference oppReference = oppLab.GetAliasedAttributeValue<EntityReference>("ab.new_opportunityid");
                if (addressReference == null || oppReference == null) { continue; }

                Guid addressId = addressReference.Id;
                Guid oppId = oppReference.Id;

                Money revenue = null;
                var lineItemSum = RetrieveQuoteLineSum(quoteId, addressId, oppId);
                if (lineItemSum != null && lineItemSum.Contains("pricesum"))
                {
                    revenue = lineItemSum.GetAliasedAttributeValue<Money>("pricesum");
                }
                //if (revenue == null) revenue = new Money(0);
                _tracer.Trace($"linesum:{lineItemSum}, rev:{revenue}");

                var opportunityLab = new Entity("smx_opportunitylab")
                {
                    Id = oppLab.Id,
                    ["smx_opportunitylabrevenue"] = revenue,
                };
                _tracer.Trace($"Updating opportunity lab with revenue {revenue}.");
                _orgService.Update(opportunityLab);
            }
        }
        private DataCollection<Entity> RetrieveOpportunityLabs(Guid quoteId)
        {
            var fetch = new FetchExpression($@"
                <fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>
                  <entity name='smx_opportunitylab'>
                    <attribute name='smx_opportunitylabid' />
                    <attribute name='smx_shiptoaddressid' />
                    <filter type='and'>
                      <condition attribute='smx_shiptoaddressid' operator='not-null' />
                    </filter>
                    <link-entity name='opportunity' from='opportunityid' to='smx_opportunityid' link-type='inner' alias='aa'>
                      <link-entity name='new_cpq_quote' from='new_opportunityid' to='opportunityid' link-type='inner' alias='ab'>
                        <attribute name='new_opportunityid' />
                        <filter type='and'>
                          <condition attribute='new_cpq_quoteid' operator='eq' value='{quoteId}'/>
                          <condition attribute='new_isprimary' operator='eq' value='1' />
                          <condition attribute='statecode' operator='eq' value='0' />
                        </filter>
                        <link-entity name='new_cpq_lineitem_tmp' from='new_quoteid' to='new_cpq_quoteid' link-type='outer' alias='ac'>
                        </link-entity>
                      </link-entity>
                    </link-entity>
                  </entity>
                </fetch>");
            var result = _orgService.RetrieveMultiple(fetch);

            if (result.Entities.Any())
            {
                return result.Entities;
            }
            else
            {
                return null;
            }
        }

        private Entity RetrieveQuoteLineSum(Guid quoteId, Guid addressId, Guid opportunityId)
        {

            var fetch = new FetchExpression($@"
                <fetch count='1' aggregate='true' distinct='false' mapping='logical'>
                  <entity name='new_cpq_lineitem_tmp'>
                    <attribute name='new_baseprice' aggregate='sum' alias='pricesum'/>
                    <filter type='and'>
                      <condition attribute='new_locationid' operator='eq' value='{addressId}' />
                      <condition attribute='new_quoteid' operator='eq' value='{quoteId}'/>
                      <condition attribute='new_baseprice' operator='not-null'/>
                      <condition attribute='statecode' operator='eq' value='0' />
                    </filter>
                    <link-entity name='new_cpq_productconfiguration' from='new_cpq_productconfigurationid' to='new_productconfigurationid' link-type='inner' alias='aa'>
                      <filter type='and'>
                        <condition attribute='statecode' operator='eq' value='0' />
                      </filter>
                      <link-entity name='new_cpq_quote' from='new_cpq_quoteid' to='new_quoteid' link-type='inner' alias='ab'>
                        <filter type='and'>
                          <condition attribute='new_isprimary' operator='eq' value='1' />
                          <condition attribute='new_opportunityid' operator='eq' value='{opportunityId}'/>
                        </filter>
                      </link-entity>
                    </link-entity>
                  </entity>
                </fetch>");
            var result = _orgService.RetrieveMultiple(fetch);

            return result.Entities.FirstOrDefault();

        }
    }
}
