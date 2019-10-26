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
    class CreateCommissionRecordsLogic : LogicBase
    {
        public CreateCommissionRecordsLogic(IOrganizationService orgService, ITracingService _tracer)
            : base(orgService, _tracer)
        {
        }

        public void CreateCommissionRecordsFromSalesOrder(Guid salesOrderId)
        {
            _tracer.Trace(MethodBase.GetCurrentMethod().Name);

            var cpqLineItems = RetrieveCPQLineItems(salesOrderId);

            foreach (var cpqLineItem in cpqLineItems)
            {
                CreateCommissionRecord(salesOrderId, cpqLineItem.Id);
            }
        }

    

    private void CreateCommissionRecord(Guid salesOrderId, Guid cpqLineItemId)
        {
            _tracer.Trace(MethodBase.GetCurrentMethod().Name);

            var cpqLineItem = RetrieveCRMRecord<new_cpq_lineitem_tmp>(new_cpq_lineitem_tmp.EntityLogicalName, cpqLineItemId, new string[] {
                "new_name",
                "new_quoteid",
                "new_locationid",
                "new_optionidid",
                "new_price"
            });

            var salesOrder = RetrieveCRMRecord<smx_salesorder>(smx_salesorder.EntityLogicalName, salesOrderId, new string[] {
                "smx_contractnumber",
                "smx_instrumentshiptoidid",
                "smx_opportunitylabid",
                "smx_opportunityid",
                "smx_salesapps",
                "smx_appsq",
                "smx_appsr",
                "smx_hemegpo",
                "smx_ufgpo",
                "smx_esrgpo",
                "smx_flowgpo",
                "smx_model",
                "smx_ihn",
                "smx_soldtoaddressid",
                "smx_billtoaddressid",
                "smx_purchaseorder",
                "smx_competitivedisplacement",
                "smx_competitor",
                "smx_executiondate"
            });
            var salesOrderBillToAddress = RetrieveCRMRecord<smx_address>(smx_address.EntityLogicalName, salesOrder.smx_BillToAddressId?.Id, new string[] {
                "smx_name"
            });
            var cpqLineItemQuote = RetrieveCRMRecord<new_cpq_quote>(new_cpq_quote.EntityLogicalName, cpqLineItem.new_quoteid?.Id, new string[] {
                 "new_name",
                 "new_quoteid",
                 "new_dealcolor"
            });
            var cpqLineItemLocationAddress = RetrieveCRMRecord<smx_address>(smx_address.EntityLogicalName, cpqLineItem?.new_LocationId?.Id, new string[] {
                "smx_name",
                "smx_statesap",
                "smx_city",
                "smx_zippostalcode"
            });
            var state = RetrieveCRMRecord<smx_state>(smx_state.EntityLogicalName, cpqLineItemLocationAddress?.smx_StateSAP?.Id, new string[] {
                "smx_region"
            });
            var instrumentShipToAddress = RetrieveCRMRecord<smx_address>(smx_address.EntityLogicalName, salesOrder?.smx_InstrumentShipToIdId?.Id, new string[] { "smx_account" });
            var instrumentShipToAccount = RetrieveCRMRecord<Account>(Account.EntityLogicalName, instrumentShipToAddress?.smx_Account?.Id, new string[] {
                "territoryid"
            });
            var oppLabOwner = RetrieveUserDataFromOpportunityLab(salesOrder.smx_OpportunityLabID?.Id);
            var opportunity = RetrieveCRMRecord<Opportunity>(Opportunity.EntityLogicalName, salesOrder.smx_OpportunityId?.Id, new string[] {
                "smx_distributor",
                "smx_corporateaccountexecihn",
                "smx_fcam",
                "smx_lsc",
                "smx_distributionsalesmanager"
            });
            var distributionSalesManager = RetrieveCRMRecord<SystemUser>(SystemUser.EntityLogicalName, opportunity?.smx_DistributionSalesManager?.Id, new string[] {
                "internalemailaddress",
                "smx_sapidnumber1"
            });

            var fcamRecord = RetrieveCRMRecord<SystemUser>(SystemUser.EntityLogicalName, opportunity?.GetAttributeValue<EntityReference>("smx_fcam")?.Id, new string[] {
                "internalemailaddress",
                "smx_sapidnumber1"
            });

            var lscRecord = RetrieveCRMRecord<SystemUser>(SystemUser.EntityLogicalName, opportunity?.GetAttributeValue<EntityReference>("smx_lsc")?.Id, new string[] {
                "internalemailaddress",
                "smx_sapidnumber1"
            });

            var oppLabAccount = RetrieveAccountFromOpportunityLab(salesOrder?.smx_OpportunityLabID?.Id);
            var corpAccountExecUser = RetrieveCRMRecord<SystemUser>(SystemUser.EntityLogicalName, opportunity?.smx_CorporateAccountExecIHN?.Id, new string[] {
                "internalemailaddress",
                "smx_sapidnumber1"
            });
            var model = RetrieveCRMRecord<smx_model>(smx_model.EntityLogicalName, salesOrder.smx_Model?.Id, new string[] {
                "smx_name"
            });
            var ihn = RetrieveCRMRecord<Account>(Account.EntityLogicalName, salesOrder.smx_IHN?.Id, new string[] {
                "name"
            });
            var primaryQuote = RetrievePrimaryQuoteFromOpportunity(salesOrder.smx_OpportunityId?.Id);
            var product = RetrieveProductByName(cpqLineItem.new_optionidId?.Name);
            var crmModel = RetrieveCRMRecord<smx_model>(smx_model.EntityLogicalName, product?.smx_CRMModelID?.Id, new string[] {
                "smx_productline"
            });
            var salesOrderSoldToAddress = RetrieveCRMRecord<smx_address>(smx_address.EntityLogicalName, salesOrder.smx_soldtoaddressid?.Id, new string[] {
                "smx_salesorganization"
            });
            var salesAppOptionMetadata = RetrieveGlobalOptionSet("smx_salesapps");

            var commissionRecord = new smx_commission()
            {
                smx_LineCreationDate = salesOrder.GetAttributeValue<DateTime?>("smx_executiondate")?.ToString("MM/dd/yyyy"),
                smx_DIID = cpqLineItem.new_name,
                smx_ProposalID = cpqLineItemQuote?.new_name,
                smx_SAPcontractID = salesOrder.smx_ContractNumber,
                smx_STSiteName = cpqLineItemLocationAddress?.smx_name,
                smx_sdiCity = cpqLineItemLocationAddress?.smx_city,
                smx_sdiState = state?.smx_Region,
                smx_sdiZip = cpqLineItemLocationAddress?.smx_zippostalcode,
                smx_BTSiteName = salesOrderBillToAddress?.smx_name,
                smx_ShipToTerr = instrumentShipToAccount?.TerritoryId?.Name,
                smx_Region = instrumentShipToAccount?.TerritoryId?.Name?.Substring(0, 2),
                smx_SalesHSAM = ParseFirstHalfOfEmail(oppLabOwner?.InternalEMailAddress),
                smx_SalesHSAMEmpN = oppLabOwner?.smx_SAPIDNumber1,
                smx_HSAMQ = "Yes",
                smx_HSAMR = "Yes",
                smx_SalesMDS = ParseFirstHalfOfEmail(distributionSalesManager?.InternalEMailAddress),
                smx_SalesMDSEmpN = distributionSalesManager?.smx_SAPIDNumber1,
                smx_MDSQ = "Yes",
                smx_MDSR = "Yes",
                smx_PartnerAcute = oppLabAccount?.smx_ClassType?.Value == (int)smx_companyclasstype.Hospital_Acute ? "Acute" : "Other",
                smx_SalesCAE = ParseFirstHalfOfEmail(corpAccountExecUser?.InternalEMailAddress),
                smx_SalesCAEEmpN = corpAccountExecUser?.smx_SAPIDNumber1,
                smx_CAEQ = opportunity?.smx_CorporateAccountExecIHN != null ? "Yes" : String.Empty,
                smx_CAER = opportunity?.smx_CorporateAccountExecIHN != null ? "Yes" : String.Empty,
                smx_SalesAPPS = salesOrder.FormattedValues.Contains("smx_salesapps") ? salesOrder.FormattedValues["smx_salesapps"] : String.Empty,
                smx_SalesAPPSEmpN = salesOrder.smx_SalesAPPS != null && salesAppOptionMetadata.FirstOrDefault(x => x.Value == salesOrder.smx_SalesAPPS.Value) != null
                    ? salesAppOptionMetadata.First(x => x.Value == salesOrder.smx_SalesAPPS.Value).ExternalValue
                    : String.Empty,
                smx_APPSQ = salesOrder.smx_APPSQ == true ? "Yes" : "No",
                smx_APPSR = salesOrder.smx_APPSR == true ? "Yes" : "No",
                smx_Green_bonus = cpqLineItemQuote?.new_DealColor?.ToUpper() == "GREEN" ? new OptionSetValue(180700001) : null,
                //smx_GreenBonus = cpqLineItemQuote?.new_DealColor?.ToUpper() == "GREEN" ? "Yes" : String.Empty,
                smx_DealSourceID = opportunity?.smx_Distributor != null ? opportunity?.smx_Distributor.Name : "Sysmex",
                smx_sdiPartName = cpqLineItem.new_optionidId?.Name,
                smx_sdiOrderDDate = salesOrder.GetAttributeValue<DateTime?>("smx_executiondate")?.ToString("MM/dd/yyyy"),
                smx_ContractType = primaryQuote?.new_acquisitiontype == new_cpq_quote_new_acquisitiontype.CPR && primaryQuote?.new_Financing == new_cpq_quote_new_financing.LAB 
                    ? "CPR-Lab" 
                    : primaryQuote != null && primaryQuote.FormattedValues.Contains("new_acquisitiontype")
                        ? primaryQuote?.FormattedValues["new_acquisitiontype"]
                        : String.Empty,
                smx_cOption = primaryQuote != null && primaryQuote.FormattedValues.Contains("new_printtype") 
                    ? primaryQuote.FormattedValues["new_printtype"] 
                    : String.Empty,
                smx_ItemNetPrice = cpqLineItem.new_Price?.Value.ToString(),
                smx_ItemEVEPrice = product?.smx_EVEPrice?.Value.ToString(),
                smx_AMPBonusMiles1 = "0",
                smx_AMPBonusMiles2 = "0",
                smx_TotalAMP = "0",
                smx_sdiPONumber = salesOrder.smx_PurchaseOrder,
                smx_sdiCompetDisp = salesOrder.smx_CompetitiveDisplacement == true ? "CPT" : "STD",
                smx_Competitor = salesOrder.smx_CompetitiveDisplacement == true ? salesOrder.smx_Competitor?.Name : "SYSMEX",
                smx_ItemCmpDspEQP = model?.smx_name,
                smx_ItemShipDate = String.Empty,
                smx_IHNifdifferentthanGPO = ihn?.Name,
                smx_GPODescription = TranslateGPODescription(salesOrder, crmModel?.smx_ProductLine),
                smx_sdiReagent = primaryQuote?.new_TotalReagentPrice?.Value.ToString(),
                smx_sdiNetService = primaryQuote?.new_TotalServicePrice?.Value.ToString(),
                smx_GPpercentP = primaryQuote?.new_GrossProfit?.ToString(),
                smx_GPpercentLRC = primaryQuote?.smx_GrossProfitLRC?.ToString(),
                smx_SalesOrganization = salesOrderSoldToAddress?.smx_SalesOrganization,
                smx_SalesOrderID = salesOrder.ToEntityReference(),
                smx_LineItemID = cpqLineItem.ToEntityReference()
            };

            commissionRecord["smx_quotaprice"] = cpqLineItem.new_Price;

            if (fcamRecord != null)
            {
                commissionRecord["smx_fcam"] = opportunity?.GetAttributeValue<EntityReference>("smx_fcam");
                commissionRecord["smx_fcamq"] = new OptionSetValue(180700001); //Yes;
                commissionRecord["smx_fcamr"] = new OptionSetValue(180700001); //Yes;
                commissionRecord["smx_fcamempn"] = fcamRecord.smx_SAPIDNumber1;
                commissionRecord["smx_salesfcam"] = ParseFirstHalfOfEmail(fcamRecord.InternalEMailAddress);
            }
            else
            {
                commissionRecord["smx_fcamq"] = new OptionSetValue(180700000); //No;
                commissionRecord["smx_fcamr"] = new OptionSetValue(180700000); //No;
            }

            if (lscRecord != null)
            {
                commissionRecord["smx_lsc"] = opportunity?.GetAttributeValue<EntityReference>("smx_lsc");
                commissionRecord["smx_lscq"] = new OptionSetValue(180700001); //Yes;
                commissionRecord["smx_lscr"] = new OptionSetValue(180700001); //Yes;
                commissionRecord["smx_saleslscempn"] = lscRecord.smx_SAPIDNumber1;
                commissionRecord["smx_saleslsc"] = ParseFirstHalfOfEmail(lscRecord.InternalEMailAddress);
            }
            else
            {
                commissionRecord["smx_lscq"] = new OptionSetValue(180700000); //No;
                commissionRecord["smx_lscr"] = new OptionSetValue(180700000); //No;
            }

            if (string.Compare(commissionRecord.smx_PartnerAcute, "Acute", true) == 0 &&
                string.Compare(commissionRecord.smx_DealSourceID, "Sysmex", true) != 0)
            {
                commissionRecord["smx_mdsq"] = "No";
                commissionRecord["smx_mdsr"] = "No";
            }
            
            commissionRecord["smx_hsam"] = salesOrder.smx_OpportunityLabID;
            commissionRecord["smx_sales_mds_user"] = opportunity?.smx_DistributionSalesManager;
            commissionRecord["smx_cae"] = opportunity?.smx_CorporateAccountExecIHN;

            //These must take place after initial creation above, since it uses fields from that creation
            commissionRecord.smx_iQuotaPrice = (product?.smx_EVEPrice == null || product?.smx_EVEPrice.Value == 0) ? commissionRecord.smx_ItemNetPrice : commissionRecord.smx_ItemEVEPrice;
            commissionRecord.smx_CPTBonusMiles = commissionRecord.smx_sdiCompetDisp == "CPT" ? ((product?.smx_EVEPrice?.Value ?? 0) * (decimal)0.25).ToString() : "0";
            commissionRecord.smx_Distrib3PartBonusMiles = (product?.smx_EVEPrice != null && product?.smx_EVEPrice.Value != 0
                && !String.IsNullOrWhiteSpace(cpqLineItem.new_name) && (cpqLineItem.new_name.StartsWith("pocH") || cpqLineItem.new_name.StartsWith("XP-")))
                ? commissionRecord.smx_ItemEVEPrice
                : commissionRecord.smx_ItemNetPrice;

            _orgService.Create(commissionRecord);
        }

        private OptionMetadataCollection RetrieveGlobalOptionSet(string name)
        {
            _tracer.Trace(MethodBase.GetCurrentMethod().Name);

            var retrieveOptionSetRequest = new RetrieveOptionSetRequest
            {
                Name = name
            };

            var retrieveOptionSetResponse = (RetrieveOptionSetResponse)_orgService.Execute(retrieveOptionSetRequest);
            return ((OptionSetMetadata)retrieveOptionSetResponse.OptionSetMetadata).Options;
        }

        private string TranslateGPODescription(smx_salesorder salesOrder, EntityReference productLineRef)
        {
            _tracer.Trace(MethodBase.GetCurrentMethod().Name);

            if (productLineRef == null)
            {
                return String.Empty;
            }

            switch(productLineRef.Name)
            {
                case "Hematology":
                    {
                        return salesOrder.smx_HemeGPO?.Name;
                    }
                case "Urinalysis":
                    {
                        return salesOrder.smx_UFGPO?.Name;
                    }
                case "ESR":
                    {
                        return salesOrder.smx_ESRGPO?.Name;
                    }
                case "Flow Cytometry":
                    {
                        return salesOrder.smx_FlowGPO?.Name;
                    }
                default:
                    {
                        return String.Empty;
                    }
            }
        }

        private T RetrieveCRMRecord<T>(string recordLogicalName, Guid? recordId, IEnumerable<string> columns) where T: Entity
        {
            _tracer.Trace($"{MethodBase.GetCurrentMethod().Name}: {recordLogicalName} - {recordId}");

            if (recordId == null)
            {
                return null;
            }

            var record = _orgService.Retrieve(recordLogicalName, recordId.Value, new ColumnSet(columns.ToArray()));
            return record.ToEntity<T>();
        }

        private string ParseFirstHalfOfEmail(string email)
        {
            _tracer.Trace(MethodBase.GetCurrentMethod().Name);

            return !String.IsNullOrWhiteSpace(email) ? email.Split('@')[0] : String.Empty;
        }

        private SystemUser RetrieveUserDataFromOpportunityLab(Guid? opportunityLabId)
        {
            _tracer.Trace(MethodBase.GetCurrentMethod().Name);

            var fetch = $@"
                <fetch top='1'>
                  <entity name='systemuser'>
                    <attribute name='systemuserid' />
                    <attribute name='internalemailaddress' />
                    <attribute name='smx_sapidnumber1' />
                    <link-entity name='smx_opportunitylab' from='owninguser' to='systemuserid' link-type='inner' alias='ol'>
                      <link-entity name='smx_salesorder' from='smx_opportunitylabid' to='smx_opportunitylabid' link-type='inner' alias='so'>
                        <filter type='and'>
                          <condition attribute='smx_opportunitylabid' operator='eq' value='{opportunityLabId}' />
                        </filter>
                      </link-entity>
                    </link-entity>
                  </entity>
                </fetch>";

            return opportunityLabId != null
                ? _orgService.RetrieveMultiple<SystemUser>(new FetchExpression(fetch)).FirstOrDefault()
                : null;
        }

        private Account RetrieveAccountFromOpportunityLab(Guid? opportunityLabId)
        {
            _tracer.Trace(MethodBase.GetCurrentMethod().Name);

            var fetch = $@"
                <fetch top='1'>
                  <entity name='account'>
                    <attribute name='accountid' />
                    <attribute name='smx_classtype' />
                    <link-entity name='smx_opportunitylab' from='smx_accountid' to='accountid' link-type='inner' alias='ol'>
                      <filter type='and'>
                        <condition attribute='smx_opportunitylabid' operator='eq' value='{opportunityLabId}' />
                      </filter>
                    </link-entity>
                  </entity>
                </fetch>";

            return opportunityLabId != null
                ? _orgService.RetrieveMultiple<Account>(new FetchExpression(fetch)).FirstOrDefault()
                : null;
        }

        private IEnumerable<new_cpq_lineitem_tmp> RetrieveCPQLineItems(Guid salesOrderId)
        {
            _tracer.Trace(MethodBase.GetCurrentMethod().Name);


            var fetch = $@"
                <fetch>
                  <entity name='new_cpq_lineitem_tmp'>
                    <attribute name='new_cpq_lineitem_tmpid' />
                    <filter type='and'>
                      <condition attribute='smx_salesorderid' operator='eq' value='{salesOrderId}' />
                      <condition attribute='new_producttype' operator='ne' value='Service' />
                      <condition attribute='new_producttype' operator='ne' value='Reagents' />
                    </filter>
                  </entity>
                </fetch>";

            return _orgService.RetrieveMultipleAll(fetch).Entities.Select(x => x.ToEntity<new_cpq_lineitem_tmp>());
        }

        private new_cpq_quote RetrievePrimaryQuoteFromOpportunity(Guid? opportunityId)
        {
            _tracer.Trace(MethodBase.GetCurrentMethod().Name);

            var fetch = $@"
                <fetch top='1'>
                  <entity name='new_cpq_quote'>
                    <attribute name='new_cpq_quoteid' />
                    <attribute name='new_acquisitiontype' />
                    <attribute name='new_financing' />
                    <attribute name='new_printtype' />
                    <attribute name='new_totalreagentprice' />
                    <attribute name='new_totalserviceprice' />
                    <attribute name='new_grossprofit' />
                    <attribute name='smx_grossprofitlrc' />
                    <filter type='and'>
                      <condition attribute='new_isprimary' operator='eq' value='1' />
                      <condition attribute='new_opportunityid' operator='eq' value='{opportunityId}' />
                    </filter>
                  </entity>
                </fetch>";

            return opportunityId != null
                ? _orgService.RetrieveMultiple<new_cpq_quote>(new FetchExpression(fetch)).FirstOrDefault()
                : null;
        }
        private smx_product RetrieveProductByName(string name)
        {
            _tracer.Trace(MethodBase.GetCurrentMethod().Name);

            var fetch = $@"
                <fetch top='1'>
                  <entity name='smx_product'>
                    <attribute name='smx_productid' />
                    <attribute name='smx_eveprice' />
                    <attribute name='smx_crmmodelid' />
                    <filter type='and'>
                      <condition attribute='smx_name' operator='eq' value='{name}' />
                    </filter>
                  </entity>
                </fetch>";

            return !String.IsNullOrWhiteSpace(name)
                ? _orgService.RetrieveMultiple<smx_product>(new FetchExpression(fetch)).FirstOrDefault()
                : null;
        }
    }
}
