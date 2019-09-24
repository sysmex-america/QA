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
    class PopulateOppEstimatedValueLogic
    {
        private IOrganizationService _orgService;
        private ITracingService _tracer;

        public PopulateOppEstimatedValueLogic(IOrganizationService orgService, ITracingService tracer)
        {
            _orgService = orgService;
            _tracer = tracer;
        }

        public void PopulateEstimatedValue(Entity oppLab, Entity preImage)
        {
            _tracer.Trace("Entering Populate Estimated Value Logic.");
            if (oppLab != null)
            {
                EntityReference opportunity = oppLab.Contains("smx_opportunityid") ? oppLab.GetAttributeValue<EntityReference>("smx_opportunityid") : null;
                UpdateTargetOpportunity(opportunity);
            }

            if (preImage != null && preImage.Contains("smx_opportunityid"))
            {
                EntityReference oldOpportunity = preImage.GetAttributeValue<EntityReference>("smx_opportunityid");
                UpdateTargetOpportunity(oldOpportunity);
            }
            
            _tracer.Trace("Exiting Logic.");

        }
        private void UpdateTargetOpportunity(EntityReference opportunity)
        {
            if (opportunity == null)
            {
                _tracer.Trace("No active opportunity found on quote line. Exiting...");
                return;
            }

            var oppSum = RetrieveOpportunityLabSum(opportunity.Id);
            if (oppSum == null)
            {
                _tracer.Trace("No active opportunity labs found. Exiting...");
                return;
            }

            _tracer.Trace($"Updating opportunity({opportunity.Id}).");
            var updateOpp = new Entity("opportunity")
            {
                Id = opportunity.Id,
                ["estimatedvalue"] = oppSum.GetAliasedAttributeValue<Money>("estimatedvalue")
            };
            _orgService.Update(updateOpp);
        }
        private Entity RetrieveOpportunityLabSum(Guid oppId)
        {
            var fetch = new FetchExpression($@"
                <fetch count='1' aggregate='true' distinct='false' mapping='logical'>
                  <entity name='smx_opportunitylab'>
                    <attribute name='smx_opportunitylabrevenue' aggregate='sum' alias='estimatedvalue'/>
                    <filter type='and'>
                      <condition attribute='statecode' operator='eq' value='0'/>
                      <condition attribute='smx_opportunityid' operator='eq' value='{oppId}'/>
                    </filter>
                  </entity>
                </fetch>");
            var result = _orgService.RetrieveMultiple(fetch);

            if (result.Entities.Any())
            {
                return result.Entities.FirstOrDefault();
            }
            else
            {
                return null;
            }
        }
    }
}
