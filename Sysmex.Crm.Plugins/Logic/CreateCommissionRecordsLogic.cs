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
    public class CreateCommissionRecordsLogic : LogicBase
    {
        public CreateCommissionRecordsLogic(IOrganizationService orgService, ITracingService _tracer)
            : base(orgService, _tracer)
        {
        }

        public void CreateCommissionRecordsFromSalesOrder(Guid salesOrderId)
        {
            _tracer.Trace(MethodBase.GetCurrentMethod().Name);

            var cpqLineItems = RetrieveCPQLineItems(salesOrderId);

            _tracer.Trace("total line item founds :" + cpqLineItems.Count());
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
                "new_optionid",
                "new_price",
                "new_customlineitem"
            });
            _tracer.Trace("before retrive product data");
            var productData = RetrieveCRMRecord<smx_product>(smx_product.EntityLogicalName, cpqLineItem.new_optionid.Id, new string[] {
                "smx_revenue",
                "smx_quota"
            });

            _tracer.Trace("after retrive product data");
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
                "smx_accountmanagerid",
                "smx_mds",
                "smx_caeihnid",
                "smx_fcam",
                "smx_lsc",
                "smx_caegpoid",
                "smx_distributor"
            });
            _tracer.Trace("after so");
            var salesOrderBillToAddress = RetrieveCRMRecord<smx_address>(smx_address.EntityLogicalName, salesOrder.smx_BillToAddressId?.Id, new string[] {
                "smx_name"
            });
            //var cpqLineItemQuote = RetrieveCRMRecord<new_cpq_quote>(new_cpq_quote.EntityLogicalName, cpqLineItem.new_quoteid?.Id, new string[] {
            //     "new_name",
            //     "new_quoteid",
            //     "new_dealcolor"
            //});
            var cpqLineItemQuote = GetQuoteFromLineItem(cpqLineItem.Id); 
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
                "smx_directordistributorsales",
                "smx_distributor",
                "smx_corporateaccountexecihn"
            });
            var directorDistSales = RetrieveCRMRecord<SystemUser>(SystemUser.EntityLogicalName, opportunity?.smx_DirectorDistributorSales?.Id, new string[] {
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

            string isQutaStr = string.Empty;
            string isRevenueStr = string.Empty;
            try
            {

                string q = productData.FormattedValues.Contains("smx_quota") ? productData.FormattedValues["smx_quota"] : String.Empty;
                string r = productData.FormattedValues.Contains("smx_revenue") ? productData.FormattedValues["smx_revenue"] : String.Empty;

                isQutaStr = !string.IsNullOrEmpty(q) ? q : null;
                isRevenueStr = !string.IsNullOrEmpty(r) ? r : null;
            }
            catch (Exception ex)
            {
                _tracer.Trace("Eception: " + ex.Message);
            }
            _tracer.Trace("q=" + isQutaStr + " , r=" + isRevenueStr);
            bool? boolIsQuote = isQutaStr != null ? isQutaStr == "Yes" ? true : false : (bool?)null;
            bool? boolIsRevenue = isRevenueStr != null ? isRevenueStr == "Yes" ? true : false : (bool?)null;

            _tracer.Trace("q=" + boolIsQuote.ToString() + " ,r=" + boolIsRevenue.ToString() + ", qStr=" + isQutaStr + ", rStr =" + isRevenueStr);

            EmpDetails hsamEmpDetails = new EmpDetails();
            EmpDetails mdsEmpDetails = new EmpDetails();
            EmpDetails caeEmpDetails = new EmpDetails();
            EmpDetails lscEmpDetails = new EmpDetails();
            EmpDetails fcaEmpDetails = new EmpDetails();
            EmpDetails caegpoEmpDetails = new EmpDetails();

            hsamEmpDetails = RetrieveUserEmpDetailsByUserId(salesOrder.smx_AccountManagerId != null ? salesOrder.smx_AccountManagerId.Id : Guid.Empty);
            mdsEmpDetails = RetrieveUserEmpDetailsByUserId(salesOrder.smx_mds != null ? salesOrder.smx_mds.Id : Guid.Empty);
            caeEmpDetails = RetrieveUserEmpDetailsByUserId(salesOrder.smx_CAEIHNId != null ? salesOrder.smx_CAEIHNId.Id : Guid.Empty);
            lscEmpDetails = RetrieveUserEmpDetailsByUserId(salesOrder.smx_LSC != null ? salesOrder.smx_LSC.Id : Guid.Empty);
            fcaEmpDetails = RetrieveUserEmpDetailsByUserId(salesOrder.smx_fcam != null ? salesOrder.smx_fcam.Id : Guid.Empty);
            caegpoEmpDetails = RetrieveUserEmpDetailsByUserId(salesOrder.smx_CAEGPOId != null ? salesOrder.smx_CAEGPOId.Id : Guid.Empty);

            string smx_sdiPartName = cpqLineItem.Contains("new_customlineitem") && !string.IsNullOrEmpty(cpqLineItem.GetAttributeValue<string>("new_customlineitem")) ? cpqLineItem.new_CustomLineItem : string.Empty;
            Money smx_Quotaprice = cpqLineItem.Contains("new_price") && cpqLineItem.GetAttributeValue<Money>("new_price") != null ? cpqLineItem.new_Price : null;

            _tracer.Trace("sdiPartName : " + smx_sdiPartName.ToString() + " smxQuotaPrice : " + smx_Quotaprice.Value.ToString());
            string translateGPOdesc = TranslateGPODescription(salesOrder, crmModel?.smx_ProductLine);
            string smx_SalesAPPSEmpN = string.Empty;
            string smx_ContractType = string.Empty;
            string smx_cOption = string.Empty;
            string smx_GreenBonusFromLineItem = string.Empty;
            string smx_GreenBonus = string.Empty;
            string smx_DealSourceID = string.Empty;
            try
            {
                smx_SalesAPPSEmpN = salesOrder.smx_SalesAPPS != null && salesAppOptionMetadata.FirstOrDefault(x => x.Value == salesOrder.smx_SalesAPPS.Value) != null
                  ? salesAppOptionMetadata.First(x => x.Value == salesOrder.smx_SalesAPPS.Value).ExternalValue
                  : String.Empty;

                smx_ContractType = primaryQuote?.new_acquisitiontype == new_cpq_quote_new_acquisitiontype.CPR && primaryQuote?.new_Financing == new_cpq_quote_new_financing.LAB
                    ? "CPR-Lab"
                    : primaryQuote != null && primaryQuote.FormattedValues.Contains("new_acquisitiontype")
                        ? primaryQuote?.FormattedValues["new_acquisitiontype"]
                        : String.Empty;
                smx_cOption = primaryQuote != null && primaryQuote.FormattedValues.Contains("new_printtype")
                    ? primaryQuote.FormattedValues["new_printtype"]
                    : String.Empty;

                smx_GreenBonusFromLineItem = cpqLineItemQuote?.new_DealColor?.ToUpper() == "GREEN" ? "Yes" : "No";
                if (smx_GreenBonusFromLineItem == "Yes")
                {
                    _tracer.Trace("smx_GreenBonusFrom Quote line = YES, So Checking from Sales Order");
                    smx_GreenBonus = (salesOrder.Contains("smx_distributor") && salesOrder.GetAttributeValue<EntityReference>("smx_distributor").Id != Guid.Empty) ? "No" : "Yes";
                }
                else
                {
                    _tracer.Trace("smx_GreenBonusFrom Quote line = NO");
                    smx_GreenBonus = smx_GreenBonusFromLineItem;
                }
                smx_DealSourceID = opportunity?.smx_Distributor != null ? opportunity?.smx_Distributor.Name : "Sysmex";

            }
            catch (Exception ex)
            {
                _tracer.Trace("ex:" + ex.Message);
            }

            _tracer.Trace("green Bonus :" + smx_GreenBonus.ToString());
            var commissionRecord = new smx_commission()
            {

                smx_LineCreationDate = DateTime.Now.ToString(),
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
                smx_PartnerAcute = oppLabAccount?.smx_ClassType?.Value == (int)smx_companyclasstype.Hospital_Acute ? "Acute" : "Other",
                smx_SalesAPPS = salesOrder.FormattedValues.Contains("smx_salesapps") ? salesOrder.FormattedValues["smx_salesapps"] : String.Empty,
                smx_SalesAPPSEmpN = smx_SalesAPPSEmpN,
                smx_APPSQ = salesOrder.smx_APPSQ == true ? "Yes" : "No",
                smx_APPSR = salesOrder.smx_APPSR == true ? "Yes" : "No",
                smx_GreenBonus = smx_GreenBonus,
                smx_DealSourceID = smx_DealSourceID,
                smx_sdiPartName = cpqLineItem.Contains("new_customlineitem") && !string.IsNullOrEmpty(cpqLineItem.GetAttributeValue<string>("new_customlineitem")) ? cpqLineItem.new_CustomLineItem : string.Empty,
                smx_Quotaprice = cpqLineItem.Contains("new_price") && cpqLineItem.GetAttributeValue<Money>("new_price") != null ? cpqLineItem.new_Price : null,
                smx_sdiOrderDDate = DateTime.Now.ToString(),
                smx_ContractType = smx_ContractType,
                smx_cOption = smx_cOption,
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
                smx_GPODescription = (string.IsNullOrEmpty(translateGPOdesc) ? "" : translateGPOdesc),
                smx_sdiReagent = primaryQuote?.new_TotalReagentPrice?.Value.ToString(),
                smx_sdiNetService = primaryQuote?.new_TotalServicePrice?.Value.ToString(),
                smx_GPpercentP = primaryQuote?.new_GrossProfit?.ToString(),
                smx_GPpercentLRC = primaryQuote?.smx_GrossProfitLRC?.ToString(),
                smx_SalesOrganization = salesOrderSoldToAddress?.smx_SalesOrganization,
                smx_SalesOrderID = salesOrder.ToEntityReference(),
                smx_LineItemID = cpqLineItem.ToEntityReference(),

                smx_HSAMQ = (salesOrder.Contains("smx_accountmanagerid") && salesOrder.GetAttributeValue<EntityReference>("smx_accountmanagerid").Id != Guid.Empty) ? isQutaStr : null,
                smx_HSAMR = (salesOrder.Contains("smx_accountmanagerid") && salesOrder.GetAttributeValue<EntityReference>("smx_accountmanagerid").Id != Guid.Empty) ? isRevenueStr : null,
                smx_MDSQ = (salesOrder.Contains("smx_mds") && salesOrder.GetAttributeValue<EntityReference>("smx_mds").Id != Guid.Empty) ? isQutaStr : null,
                smx_MDSR = (salesOrder.Contains("smx_mds") && salesOrder.GetAttributeValue<EntityReference>("smx_mds").Id != Guid.Empty) ? isRevenueStr : null,
                smx_CAEQ = (salesOrder.Contains("smx_caeihnid") && salesOrder.GetAttributeValue<EntityReference>("smx_caeihnid").Id != Guid.Empty) ? isQutaStr : null,
                smx_CAER = (salesOrder.Contains("smx_caeihnid") && salesOrder.GetAttributeValue<EntityReference>("smx_caeihnid").Id != Guid.Empty) ? isRevenueStr : null,
                smx_lscq = ((salesOrder.Contains("smx_lsc") && salesOrder.GetAttributeValue<EntityReference>("smx_lsc").Id != Guid.Empty) && isQutaStr != null) ? new OptionSetValue(isQutaStr == "Yes" ? 180700001 : 180700000) : null,
                smx_lscr = ((salesOrder.Contains("smx_lsc") && salesOrder.GetAttributeValue<EntityReference>("smx_lsc").Id != Guid.Empty) && isRevenueStr != null) ? new OptionSetValue(isRevenueStr == "Yes" ? 180700001 : 180700000) : null,
                smx_caegpoq = (salesOrder.Contains("smx_caegpoid") && salesOrder.GetAttributeValue<EntityReference>("smx_caegpoid").Id != Guid.Empty) ? boolIsQuote : (bool?)null,
                smx_caegpor = (salesOrder.Contains("smx_caegpoid") && salesOrder.GetAttributeValue<EntityReference>("smx_caegpoid").Id != Guid.Empty) ? boolIsRevenue : (bool?)null,
                //smx_FCAMQ = ((salesOrder.Contains("smx_fcam") && salesOrder.GetAttributeValue<EntityReference>("smx_fcam").Id != Guid.Empty) && isQutaStr != null) ? new OptionSetValue(isQutaStr == "Yes" ? 180700001 : 180700000) : null,
                //smx_FCAMR = ((salesOrder.Contains("smx_fcam") && salesOrder.GetAttributeValue<EntityReference>("smx_fcam").Id != Guid.Empty) && isRevenueStr != null) ? new OptionSetValue(isRevenueStr == "Yes" ? 180700001 : 180700000) : null,

                smx_HSAM = salesOrder.GetAttributeValue<EntityReference>("smx_accountmanagerid"),
                smx_Sales_MDS_User = salesOrder.GetAttributeValue<EntityReference>("smx_mds"),
                smx_CAE = salesOrder.GetAttributeValue<EntityReference>("smx_caeihnid"),
                //smx_FCAM = salesOrder.GetAttributeValue<EntityReference>("smx_fcam"),
                smx_LSC = salesOrder.GetAttributeValue<EntityReference>("smx_lsc"),
                smx_caegpo = salesOrder.GetAttributeValue<EntityReference>("smx_caegpoid"),

                smx_SalesHSAMEmpN = hsamEmpDetails.empNo,
                smx_SalesMDSEmpN = mdsEmpDetails.empNo,
                smx_SalesCAEEmpN = caeEmpDetails.empNo,
                smx_SalesLSCEmpN = lscEmpDetails.empNo,
                //smx_FCAMEmpN = fcaEmpDetails.empNo,
                smx_caegpoempn = caegpoEmpDetails.empNo,

                smx_SalesHSAM = hsamEmpDetails.nameDerivedFromEmail,
                smx_SalesMDS = mdsEmpDetails.nameDerivedFromEmail,
                smx_SalesCAE = caeEmpDetails.nameDerivedFromEmail,
                smx_SalesLSC = lscEmpDetails.nameDerivedFromEmail,
                //smx_SalesFCAM = fcaEmpDetails.nameDerivedFromEmail,
                //smx_Green_bonus = (salesOrder.Contains("smx_distributor") && salesOrder.GetAttributeValue<EntityReference>("smx_distributor").Id != Guid.Empty) ? new OptionSetValue(180700001) : new OptionSetValue(180700000),
            };
            _tracer.Trace("quota entry obj created");
            //These must take place after initial creation above, since it uses fields from that creation
            commissionRecord.smx_iQuotaPrice = (product?.smx_EVEPrice == null || product?.smx_EVEPrice.Value == 0) ? commissionRecord.smx_ItemNetPrice : commissionRecord.smx_ItemEVEPrice;
            commissionRecord.smx_CPTBonusMiles = commissionRecord.smx_sdiCompetDisp == "CPT" ? ((product?.smx_EVEPrice?.Value ?? 0) * (decimal)0.25).ToString() : "0";
            commissionRecord.smx_Distrib3PartBonusMiles = (product?.smx_EVEPrice != null && product?.smx_EVEPrice.Value != 0
                && !String.IsNullOrWhiteSpace(cpqLineItem.new_name) && (cpqLineItem.new_name.StartsWith("pocH") || cpqLineItem.new_name.StartsWith("XP-")))
                ? commissionRecord.smx_ItemEVEPrice
                : commissionRecord.smx_ItemNetPrice;

            _tracer.Trace("before quota creation");
            try
            {
                _orgService.Create(commissionRecord);
            }
            catch (Exception ex)
            {
                _tracer.Trace("Exception :" + ex.Message);
                if (ex.StackTrace != null)
                {
                    _tracer.Trace("StackTrace :" + ex.StackTrace);
                }
            }
            _tracer.Trace("quota created");
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

            switch (productLineRef.Name)
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

        private T RetrieveCRMRecord<T>(string recordLogicalName, Guid? recordId, IEnumerable<string> columns) where T : Entity
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
                    </filter>
                    <link-entity name='smx_product' from='smx_productid' to='new_optionid' link-type='inner' alias='aa'>
                        <attribute name='smx_producttype' />
                        <filter type='and'>
                            <condition attribute='smx_producttype' operator='not-in'>
                            <value>180700003</value>
                            <value>180700002</value>
                            </condition>
                        </filter>
                    </link-entity>
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

        private EmpDetails RetrieveUserEmpDetailsByUserId(Guid userId)
        {
            EmpDetails empDetails = new EmpDetails();
            _tracer.Trace(MethodBase.GetCurrentMethod().Name);
            try
            {
                var fetch = $@"
                <fetch top='1'>
                  <entity name='systemuser'>
                    <attribute name='smx_sapidnumber1' />
                    <attribute name='domainname' />
                    <filter type='and'>
                      <condition attribute='systemuserid' operator='eq' value='{userId}' />
                    </filter>
                  </entity>
                </fetch>";

                if (userId != null && userId != Guid.Empty)
                {
                    var result = _orgService.RetrieveMultiple<SystemUser>(new FetchExpression(fetch)).FirstOrDefault();
                    if (result != null && result.Id != Guid.Empty)
                    {
                        if (result.Attributes.Contains("smx_sapidnumber1"))
                        {
                            empDetails.empNo = result.GetAttributeValue<string>("smx_sapidnumber1");
                        }
                        if (result.Attributes.Contains("domainname"))
                        {
                            empDetails.nameDerivedFromEmail = ParseFirstHalfOfEmail(result.GetAttributeValue<string>("domainname"));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _tracer.Trace("Exception from RetriveEmpNoByUserID method :" + ex.Message);
            }
            _tracer.Trace("userid :" + userId.ToString() + ", returned emp number :" + empDetails.empNo + ", name =  " + empDetails.nameDerivedFromEmail);
            return empDetails;
        }

        private new_cpq_quote GetQuoteFromLineItem(Guid lineItemId)
        {
            new_cpq_quote quote = null;
            try
            {
                QueryExpression qe = new QueryExpression("new_cpq_quote")
                {
                    ColumnSet = new ColumnSet("new_name", "new_quoteid", "new_dealcolor")

                };
                LinkEntity pc = qe.AddLink("new_cpq_productconfiguration", "new_cpq_quoteid", "new_quoteid");
                LinkEntity lineitem = pc.AddLink("new_cpq_lineitem_tmp", "new_cpq_productconfigurationid", "new_productconfigurationid");
                lineitem.LinkCriteria.AddCondition(new ConditionExpression("new_cpq_lineitem_tmpid", ConditionOperator.Equal, lineItemId));

                IEnumerable<new_cpq_quote> quotes = _orgService.RetrieveMultiple<new_cpq_quote>(qe);
                _tracer.Trace($"total number of quotes found :{ quotes.Count() }");
                if (quotes.Count() > 0)
                {
                    quote = quotes.FirstOrDefault();
                }

            }
            catch (Exception ex)
            {
                quote = null;
                _tracer.Trace($"Exception is { ex.Message }");
            }
            return quote;
        }
    }

    public class EmpDetails
    {
        public string empNo { get; set; }
        public string nameDerivedFromEmail { get; set; }
    }
}
