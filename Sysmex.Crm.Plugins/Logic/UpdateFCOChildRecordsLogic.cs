using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using SonomaPartners.Crm.Toolkit;
using Sysmex.Crm.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Sysmex.Crm.Plugins.Logic
{
    public class UpdateFCOChildRecordsLogic : LogicBase
    {
        public UpdateFCOChildRecordsLogic(IOrganizationService orgService, ITracingService tracer)
            : base(orgService, tracer)
        {
        }

        public void UpdateChildRecords(smx_fcsopportunity fcsOpportunity)
        {
            _tracer.Trace("Update Child Records");

            if (fcsOpportunity.Contains("smx_consumablediscount"))
            {
                var products = RetrieveFCSalesProductRecords(smx_producttype.Consumables, fcsOpportunity.Id);

                foreach(var productId in products)
                {
                    UpdateDiscountPercentage(productId, fcsOpportunity.smx_ConsumableDiscount);
                }
            }

            if (fcsOpportunity.Contains("smx_discountpercentage"))
            {
                var products = RetrieveFCSalesProductRecords(smx_producttype.Reagents, fcsOpportunity.Id);

                foreach (var productId in products)
                {
                    UpdateDiscountPercentage(productId, fcsOpportunity.smx_DiscountPercentage);
                }
            }

            if (fcsOpportunity.Contains("smx_instdiscountpercentage"))
            {
                var products = RetrieveFCSalesProductRecords(smx_producttype.Instrument, fcsOpportunity.Id);

                foreach (var productId in products)
                {
                    UpdateDiscountPercentage(productId, fcsOpportunity.smx_InstDiscountPercentage);
                }
            }
        }

        private void UpdateDiscountPercentage(Guid productId, decimal? discountValue)
        {
            _tracer.Trace("Update Discount Percentage");

            var updatedRecord = new smx_flowcytometryproduct()
            {
                Id = productId,
                smx_DiscountPercentage = discountValue != null ? discountValue / 100 : null
            };

            _orgService.Update(updatedRecord);
        }

        private IEnumerable<Guid> RetrieveFCSalesProductRecords(smx_producttype productType, Guid fcsOpportunityId)
        {
            _tracer.Trace($"Retrieve FC Sales Product Records, fcsOpportunity {fcsOpportunityId}, productType {(int)productType}");

            var fetch = $@"
                <fetch>
                  <entity name='smx_flowcytometryproduct'>
                    <attribute name='smx_flowcytometryproductid' />
                    <filter type='and'>
                      <condition attribute='smx_flowcytometryopportunity' operator='eq' value='{fcsOpportunityId}' />
                      <condition attribute='smx_discountoverride' operator='ne' value='1' />
                      <condition attribute='smx_producttype' operator='eq' value='{(int)productType}' />
                    </filter>
                  </entity>
                </fetch>";

            return _orgService.RetrieveMultipleAll(fetch).Entities.Select(x => x.Id);
        }
    }
}
