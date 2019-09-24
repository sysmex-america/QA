using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using SonomaPartners.Crm.Toolkit;
using Sysmex.Crm.Model;

namespace Sysmex.Crm.Plugins.Logic
{
    class PopulatePriceListOnProductLogic : LogicBase
    {
        //Miles and Tom D: Hard-coded as this is something that will only be temporary.
        //Safe to assume country GUID will stay the same between orgs, but not currency, so currency code is used.
        private readonly Dictionary<Guid, string> _countryIdToCurrencyCode = new Dictionary<Guid, string>()
        {
            { new Guid("509252F6-E2E1-E711-812F-E0071B6A3101"), "USD" }, //United States of America
            { new Guid("D89052F6-E2E1-E711-812F-E0071B6A3101"), "CAD" }  //Canada
        };

        public PopulatePriceListOnProductLogic(IOrganizationService orgService, ITracingService tracer)
            :base(orgService, tracer)
        {
        }

        public void PopulatePriceListAndListPrice(smx_flowcytometryproduct flowProduct, smx_flowcytometryproduct flowProductPreimage)
        {
            _tracer.Trace("PopulatePriceListAndListPrice");

            if (!flowProduct.Contains("smx_salesproduct"))
            {
                _tracer.Trace("No incoming sales product change, returning");
                return;
            }
            var salesProductRef = flowProduct.smx_SalesProduct;

            var opportunityRef = flowProduct.Contains("smx_flowcytometryopportunity") 
                ? flowProduct.smx_FlowCytometryOpportunity 
                : flowProductPreimage.smx_FlowCytometryOpportunity;
            if (opportunityRef == null)
            {
                _tracer.Trace("No Opportunity on record, returning");
                return;
            }

            var countryRef = RetrieveCountry(opportunityRef);
            var priceList = RetrievePriceList(salesProductRef.Id, countryRef);

            if (priceList == null)
            {
                _tracer.Trace("No Price List found, returning");
                return;
            }

            _tracer.Trace("Updating fields on entity");
            flowProduct.smx_PriceList = priceList.ToEntityReference();
            flowProduct.smx_ListPrice = priceList.smx_ListPrice;
        }

        private EntityReference RetrieveCountry(EntityReference opportunityRef)
        {
            _tracer.Trace("RetrieveCountry");

            var fetch = $@"
            <fetch top='1'>
              <entity name='smx_fcsopportunity'>
                <attribute name='smx_fcsopportunityid' />
                <filter type='and'>
                  <condition attribute='smx_fcsopportunityid' operator='eq' value='{opportunityRef.Id}' />
                </filter>
                <link-entity name='account' from='accountid' to='smx_customeraccount' link-type='inner' alias='acc'>
                  <attribute name='smx_countrysap' />
                </link-entity>
              </entity>
            </fetch>";

            var results = _orgService.RetrieveMultiple(new FetchExpression(fetch));
            return results.Entities.FirstOrDefault()?.GetAliasedAttributeValue<EntityReference>("acc.smx_countrysap");
        }

        private smx_pricelist RetrievePriceList(Guid salesProductId, EntityReference countryRef)
        {
            _tracer.Trace("RetrievePriceList");

            var now = DateTime.Now;
            if (countryRef == null)
            {
                _tracer.Trace("No Country found, returning");
                return null;
            }
            
            var currencyCode = _countryIdToCurrencyCode.ContainsKey(countryRef.Id) ? _countryIdToCurrencyCode[countryRef.Id] : null;
            if(String.IsNullOrWhiteSpace(currencyCode))
            {
                _tracer.Trace("No Currency Code found, returning");
                return null;
            }

            var fetch = $@"
            <fetch top='1'>
              <entity name='smx_pricelist'>
                <attribute name='smx_pricelistid' />
                <attribute name='smx_listprice' />
                <filter type='and'>
                  <condition attribute='smx_effectivedate' operator='on-or-before' value='{now}' />
                  <filter type='or'>
                    <condition attribute='smx_expirationdate' operator='on-or-after' value='{now}' />
                    <condition attribute='smx_expirationdate' operator='null' />
                  </filter>
                  <condition attribute='smx_productnameid' operator='eq' value='{salesProductId}' />
                </filter>
                <link-entity name='transactioncurrency' from='transactioncurrencyid' to='transactioncurrencyid' link-type='inner' alias='con'>
                  <filter type='and'>
                    <condition attribute='isocurrencycode' operator='eq' value='{currencyCode}' />
                  </filter>
                </link-entity>
              </entity>
            </fetch>";

            var results = _orgService.RetrieveMultiple<smx_pricelist>(new FetchExpression(fetch));
            return results.FirstOrDefault();
        }
    }
}
