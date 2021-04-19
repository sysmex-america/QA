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
                "new_customlineitem",
				"new_producttype"
			});
            _tracer.Trace("before retrive product data");
			//Added by Yash on 08-02-2021--Ticket No 60353
			var productData = RetrieveCRMRecord<smx_product>(smx_product.EntityLogicalName, cpqLineItem.new_optionid.Id, new string[] {
                "smx_revenue",
                "smx_quota",
				"smx_family"
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
                "smx_distributor",
                "smx_regionid",
                "smx_territoryid",
				"smx_clmagreement"
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
			//var state = RetrieveCRMRecord<smx_state>(smx_state.EntityLogicalName, cpqLineItemLocationAddress?.smx_StateSAP?.Id, new string[] {
			//    "smx_region"
			//});
			//Added by Yash on 14-07-2020--Ticket No 57289
			var instrumentShipToAddress = RetrieveCRMRecord<smx_address>(smx_address.EntityLogicalName, salesOrder?.smx_InstrumentShipToIdId?.Id, new string[] { "smx_account", "smx_statesap" });
			var state = RetrieveCRMRecord<smx_state>(smx_state.EntityLogicalName, instrumentShipToAddress?.smx_StateSAP?.Id, new string[] {
				"smx_region"
			});
			//End
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
			//Added by Yash on 01-07-2020--Ticket No 57149
			Guid agreementId = salesOrder.Attributes.Contains("smx_clmagreement") ? ((EntityReference)salesOrder.Attributes["smx_clmagreement"]).Id : Guid.Empty;
			Entity enAgreement = agreementId != Guid.Empty ? _orgService.Retrieve(new_clm_agreement.EntityLogicalName, agreementId, new ColumnSet("new_quotenumber")) : null;
			string quoteNumber = enAgreement != null && enAgreement.Attributes.Contains("new_quotenumber") ? enAgreement.Attributes["new_quotenumber"].ToString() : string.Empty;
			var primaryQuote = RetrieveQuoteFromAgreement(quoteNumber);
			_tracer.Trace($"new_quotes found :{ primaryQuote }");
			//End
			//var primaryQuote = RetrievePrimaryQuoteFromOpportunity(salesOrder.smx_OpportunityId?.Id);

			var product = RetrieveProductByName(cpqLineItem.new_optionidId?.Name);
            var crmModel = RetrieveCRMRecord<smx_model>(smx_model.EntityLogicalName, product?.smx_CRMModelID?.Id, new string[] {
                "smx_productline"
            });
            var salesOrderSoldToAddress = RetrieveCRMRecord<smx_address>(smx_address.EntityLogicalName, salesOrder.smx_soldtoaddressid?.Id, new string[] {
                "smx_salesorganization"
            });
            var salesAppOptionMetadata = RetrieveGlobalOptionSet("smx_salesapps");
			//Added by Yash on 31-08-2020--Ticket No 57863
			string productType = cpqLineItem.Contains("new_producttype") ? cpqLineItem.GetAttributeValue<string>("new_producttype") : null;
			Money productEVEPrice = null;
			bool overrideEve = false;
		    EntityReference lineItemProduct = cpqLineItem.Contains("new_optionid") ? cpqLineItem.GetAttributeValue<EntityReference>("new_optionid") : null;
			if (lineItemProduct != null)
			{
				Entity enproduct = GetProduct(lineItemProduct.Id);
				productEVEPrice = enproduct.Contains("smx_eveprice") ? enproduct.GetAttributeValue<Money>("smx_eveprice") : null;
				overrideEve= enproduct.Contains("smx_eveoverride") ? (enproduct.GetAttributeValue<bool>("smx_eveoverride")) : false;
				_tracer.Trace("productEVEPrice" + productEVEPrice);
			}
					
			
			//End
			//Added by Yash on 12-08-2020--Ticket No 57667
			Entity saleOrder = GetOpenTerritory(salesOrderId);
			int? openTerriory = null;// (180700001=Yes)
			string territoryId = null;
			AliasedValue openTerritoryValue = saleOrder.Contains("territory.smx_openterritory") ? saleOrder.GetAttributeValue<AliasedValue>("territory.smx_openterritory") : null;
			AliasedValue openTeritoryId = saleOrder.Contains("territory.smx_territoryid") ? saleOrder.GetAttributeValue<AliasedValue>("territory.smx_territoryid") : null;
			if (openTerritoryValue != null)
				openTerriory = ((OptionSetValue)openTerritoryValue.Value).Value;
			if (openTeritoryId != null)
				territoryId = openTeritoryId.Value.ToString();
			
			//End
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
			//Added by Yash on 31-08-2020--Ticket No 57863
			//Money smx_Quotaprice = cpqLineItem.Contains("new_price") && cpqLineItem.GetAttributeValue<Money>("new_price") != null ? cpqLineItem.new_Price : null;
			 Money quotaprice = cpqLineItem.Contains("new_price") && cpqLineItem.GetAttributeValue<Money>("new_price") != null ? cpqLineItem.new_Price : null;			
			//End
			_tracer.Trace("sdiPartName : " + smx_sdiPartName+ " smxQuotaPrice : " + quotaprice);
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
					//Added by Yash on 08-02-2021--Ticket No 60353
					//smx_GreenBonus = (salesOrder.Contains("smx_distributor") && salesOrder.GetAttributeValue<EntityReference>("smx_distributor").Id != Guid.Empty) ? "No" : "Yes";
					smx_GreenBonus = ((salesOrder.Contains("smx_distributor") && salesOrder.GetAttributeValue<EntityReference>("smx_distributor").Id != Guid.Empty) || productData.GetAttributeValue<OptionSetValue>("smx_family").Value == 180700006) ? "No" : "Yes";

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

				//smx_Region = instrumentShipToAccount?.TerritoryId?.Name?.Substring(0, 2),

				//Added by Yash on 22-06-2020
				smx_Region = salesOrder.Contains("smx_territoryid") ? (salesOrder.GetAttributeValue<EntityReference>("smx_territoryid").Name).Substring(0, 2) : string.Empty,

				smx_PartnerAcute = oppLabAccount?.smx_ClassType?.Value == (int)smx_companyclasstype.Hospital_Acute ? "Acute" : "Other",
				smx_SalesAPPS = salesOrder.FormattedValues.Contains("smx_salesapps") ? salesOrder.FormattedValues["smx_salesapps"] : String.Empty,
				smx_SalesAPPSEmpN = smx_SalesAPPSEmpN,
				smx_APPSQ = salesOrder.smx_APPSQ == true ? "Yes" : "No",
				smx_APPSR = salesOrder.smx_APPSR == true ? "Yes" : "No",
				smx_GreenBonus = smx_GreenBonus,
				smx_DealSourceID = smx_DealSourceID,
				smx_sdiPartName = cpqLineItem.Contains("new_customlineitem") && !string.IsNullOrEmpty(cpqLineItem.GetAttributeValue<string>("new_customlineitem")) ? cpqLineItem.new_CustomLineItem : string.Empty,
				//Added by Yash on 31-08-2020--Ticket No 57863smx_Quotaprice = (overrideEve == true ||  saleOrder.Contains("smx_distributor")) ? quotaprice : productEVEPrice,
				//smx_Quotaprice = cpqLineItem.Contains("new_price") && cpqLineItem.GetAttributeValue<Money>("new_price") != null ? cpqLineItem.new_Price : null,
				//Added by Yash on 02-11-2020--Ticket No 58905
				//smx_Quotaprice = (overrideEve == false ) ? quotaprice : productEVEPrice,
				//Added by Yash on 19-11-2020--Ticket No 58905
				//smx_Quotaprice = (overrideEve == true || salesOrder.Contains("smx_distributor")) ? productEVEPrice  : quotaprice,
				smx_Quotaprice = ((overrideEve == true || salesOrder.Contains("smx_distributor")) && productEVEPrice != null) ? productEVEPrice : quotaprice,
				//End
				smx_sdiOrderDDate = DateTime.Now.ToString(),
				smx_ContractType = smx_ContractType,
				smx_cOption = smx_cOption,
				smx_ItemNetPrice = cpqLineItem.new_Price?.Value.ToString(),
				//Added by Yash on 21-09-2020--Ticket No 57863
				//smx_ItemEVEPrice = product?.smx_EVEPrice?.Value.ToString(),
				//Added by Yash on 02-11-2020--Ticket No 58905
				//smx_ItemEVEPrice = overrideEve == false ? string.Empty : productEVEPrice.Value.ToString(),
				smx_ItemEVEPrice = (overrideEve == true || salesOrder.Contains("smx_distributor")) ? productEVEPrice?.Value.ToString() : string.Empty,
				//End
				smx_AMPBonusMiles1 = "0",
				smx_AMPBonusMiles2 = "0",
				smx_TotalAMP = "0",
				smx_sdiPONumber = salesOrder.smx_PurchaseOrder,
				//Added by Yash on 08-02-2021--Ticket No 60353
				//smx_sdiCompetDisp = salesOrder.smx_CompetitiveDisplacement == true  ? "CPT" : "STD",
				smx_sdiCompetDisp = (salesOrder.smx_CompetitiveDisplacement == true && productData.GetAttributeValue<OptionSetValue>("smx_family").Value != 180700006) ? "CPT" : "STD",
				smx_Competitor = salesOrder.smx_CompetitiveDisplacement == true ? salesOrder.smx_Competitor?.Name : "SYSMEX",
				smx_ItemCmpDspEQP = model?.smx_name,
				smx_ItemShipDate = String.Empty,
				smx_IHNifdifferentthanGPO = ihn?.Name,
				smx_GPODescription = (string.IsNullOrEmpty(translateGPOdesc) ? "" : translateGPOdesc),
				smx_sdiReagent = primaryQuote?.new_TotalReagentPrice?.Value.ToString(),
				smx_sdiNetService = primaryQuote?.new_TotalServicePrice?.Value.ToString(),
				smx_GPpercentP = primaryQuote?.new_GrossProfit?.ToString(),
				//smx_GPpercentLRC = primaryQuote?.smx_GrossProfitLRC?.ToString(),
				//Added by Yash on 01-07-2020--Ticket No 57147
				smx_GPpercentLRC = primaryQuote?.new_GrossProfit?.ToString(),
				smx_SalesOrganization = salesOrderSoldToAddress?.smx_SalesOrganization,
				smx_SalesOrderID = salesOrder.ToEntityReference(),
				smx_LineItemID = cpqLineItem.ToEntityReference(),
				//Added by Yash on 13-11-2020--Ticket No 58839
				smx_HSAMQ = (salesOrder.Contains("smx_accountmanagerid") && salesOrder.GetAttributeValue<EntityReference>("smx_accountmanagerid").Id != Guid.Empty) ? isQutaStr : null,
				smx_HSAMR = (salesOrder.Contains("smx_accountmanagerid") && salesOrder.GetAttributeValue<EntityReference>("smx_accountmanagerid").Id != Guid.Empty) ? isRevenueStr : null,
				//smx_HSAMQ = (salesOrder.Contains("smx_accountmanagerid") && salesOrder.GetAttributeValue<EntityReference>("smx_accountmanagerid").Id != Guid.Empty && openTerriory != 180700001) ? isQutaStr : null,
				//smx_HSAMR = (salesOrder.Contains("smx_accountmanagerid") && salesOrder.GetAttributeValue<EntityReference>("smx_accountmanagerid").Id != Guid.Empty && openTerriory != 180700001) ? isRevenueStr : null,
				smx_MDSQ = (salesOrder.Contains("smx_mds") && salesOrder.GetAttributeValue<EntityReference>("smx_mds").Id != Guid.Empty) ? isQutaStr : null,
				smx_MDSR = (salesOrder.Contains("smx_mds") && salesOrder.GetAttributeValue<EntityReference>("smx_mds").Id != Guid.Empty) ? isRevenueStr : null,
				smx_CAEQ = (salesOrder.Contains("smx_caeihnid") && salesOrder.GetAttributeValue<EntityReference>("smx_caeihnid").Id != Guid.Empty) ? isQutaStr : null,
				smx_CAER = (salesOrder.Contains("smx_caeihnid") && salesOrder.GetAttributeValue<EntityReference>("smx_caeihnid").Id != Guid.Empty) ? isRevenueStr : null,
				smx_lscq = ((salesOrder.Contains("smx_lsc") && salesOrder.GetAttributeValue<EntityReference>("smx_lsc").Id != Guid.Empty) && isQutaStr != null) ? new OptionSetValue(isQutaStr == "Yes" ? 180700001 : 180700000) : null,
				smx_lscr = ((salesOrder.Contains("smx_lsc") && salesOrder.GetAttributeValue<EntityReference>("smx_lsc").Id != Guid.Empty) && isRevenueStr != null) ? new OptionSetValue(isRevenueStr == "Yes" ? 180700001 : 180700000) : null,
				smx_caegpoq = (salesOrder.Contains("smx_caegpoid") && salesOrder.GetAttributeValue<EntityReference>("smx_caegpoid").Id != Guid.Empty) ? boolIsQuote : (bool?)null,
				smx_caegpor = (salesOrder.Contains("smx_caegpoid") && salesOrder.GetAttributeValue<EntityReference>("smx_caegpoid").Id != Guid.Empty) ? boolIsRevenue : (bool?)null,
				//Added by Yash on 23-09-2020--Ticket No 58244
				smx_FCAMQ = ((salesOrder.Contains("smx_fcam") && salesOrder.GetAttributeValue<EntityReference>("smx_fcam").Id != Guid.Empty) && isQutaStr != null) ? new OptionSetValue(isQutaStr == "Yes" ? 180700001 : 180700000) : null,
				smx_FCAMR = ((salesOrder.Contains("smx_fcam") && salesOrder.GetAttributeValue<EntityReference>("smx_fcam").Id != Guid.Empty) && isRevenueStr != null) ? new OptionSetValue(isRevenueStr == "Yes" ? 180700001 : 180700000) : null,
				//End
				//Added by Yash on 12-08-2020--Ticket No 57667
				//smx_HSAM =salesOrder.GetAttributeValue<EntityReference>("smx_accountmanagerid"),
				smx_HSAM = openTerriory != 180700001 ? salesOrder.GetAttributeValue<EntityReference>("smx_accountmanagerid") : null,
				smx_Sales_MDS_User = salesOrder.GetAttributeValue<EntityReference>("smx_mds"),
				smx_CAE = salesOrder.GetAttributeValue<EntityReference>("smx_caeihnid"),
				//Added by Yash on 23-09-2020--Ticket No 58244
				smx_FCAM = salesOrder.GetAttributeValue<EntityReference>("smx_fcam"),
				//End
				smx_LSC = salesOrder.GetAttributeValue<EntityReference>("smx_lsc"),
				smx_caegpo = salesOrder.GetAttributeValue<EntityReference>("smx_caegpoid"),
				//Added by Yash on 12-08-2020--Ticket No 57667
				//smx_SalesHSAMEmpN = hsamEmpDetails.empNo,
				//smx_SalesHSAMEmpN = openTerriory != 180700001 ? hsamEmpDetails.empNo:null,
				smx_SalesHSAMEmpN = openTerriory != 180700001 ? hsamEmpDetails.empNo : territoryId,
				smx_SalesMDSEmpN = mdsEmpDetails.empNo,
				smx_SalesCAEEmpN = caeEmpDetails.empNo,
				smx_SalesLSCEmpN = lscEmpDetails.empNo,
				//Added by Yash on 23-09-2020--Ticket No 58244
				smx_FCAMEmpN = fcaEmpDetails.empNo,
				//End
				smx_caegpoempn = caegpoEmpDetails.empNo,
				//Added by Yash on 12-08-2020--Ticket No 57667
				//smx_SalesHSAM = hsamEmpDetails.nameDerivedFromEmail,
				//Added by Yash on 02-11-2020--Ticket No 58839
				//smx_SalesHSAM = openTerriory != 180700001 ? hsamEmpDetails.nameDerivedFromEmail:null,
				//Added by Yash on 24-11-2020--Ticket No 58839
				smx_SalesHSAM = openTerriory == 180700001 ? "OPEN Territory" + "-" + instrumentShipToAccount?.TerritoryId?.Name : hsamEmpDetails.nameDerivedFromEmail,
				smx_SalesMDS = mdsEmpDetails.nameDerivedFromEmail,
				smx_SalesCAE = caeEmpDetails.nameDerivedFromEmail,
				smx_SalesLSC = lscEmpDetails.nameDerivedFromEmail,
				//Added by Yash on 23-09-2020--Ticket No 58244
				smx_SalesFCAM = fcaEmpDetails.nameDerivedFromEmail,
				//End
				//smx_Green_bonus = (salesOrder.Contains("smx_distributor") && salesOrder.GetAttributeValue<EntityReference>("smx_distributor").Id != Guid.Empty) ? new OptionSetValue(180700001) : new OptionSetValue(180700000),
				
			};
            _tracer.Trace("quota entry obj created");
			//These must take place after initial creation above, since it uses fields from that creation
			//Added by Yash on 31-08-2020--Ticket No 57863
			//commissionRecord.smx_iQuotaPrice = (product?.smx_EVEPrice == null || product?.smx_EVEPrice.Value == 0) ? commissionRecord.smx_ItemNetPrice : commissionRecord.smx_ItemEVEPrice;
			//Added by Yash on 02-11-2020--Ticket No 58905
			//commissionRecord.smx_iQuotaPrice = overrideEve == false ? commissionRecord.smx_ItemNetPrice : productEVEPrice?.Value.ToString();
			//Added by Yash on 19-11-2020--Ticket No 58905
			//commissionRecord.smx_iQuotaPrice = (overrideEve == true || salesOrder.Contains("smx_distributor")) ? productEVEPrice?.Value.ToString()  : commissionRecord.smx_ItemNetPrice;
			commissionRecord.smx_iQuotaPrice = ((overrideEve == true || salesOrder.Contains("smx_distributor")) && productEVEPrice != null) ? productEVEPrice?.Value.ToString() : commissionRecord.smx_ItemNetPrice;
			//End
			commissionRecord.smx_CPTBonusMiles = commissionRecord.smx_sdiCompetDisp == "CPT" ? ((product?.smx_EVEPrice?.Value ?? 0) * (decimal)0.25).ToString() : "0";
            commissionRecord.smx_Distrib3PartBonusMiles = (product?.smx_EVEPrice != null && product?.smx_EVEPrice.Value != 0
                && !String.IsNullOrWhiteSpace(cpqLineItem.new_name) && (cpqLineItem.new_name.StartsWith("pocH") || cpqLineItem.new_name.StartsWith("XP-")))
                ? commissionRecord.smx_ItemEVEPrice
                : commissionRecord.smx_ItemNetPrice;

            _tracer.Trace("before quota creation");
            try
            {
                _orgService.Create(commissionRecord);
				_tracer.Trace("quote is created");
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
			//var fetch = $@"
			//    <fetch>
			//      <entity name='new_cpq_lineitem_tmp'>
			//        <attribute name='new_cpq_lineitem_tmpid' />
			//        <filter type='and'>
			//          <condition attribute='smx_salesorderid' operator='eq' value='{salesOrderId}' />                      
			//        </filter>
			//        <link-entity name='smx_product' from='smx_productid' to='new_optionid' link-type='inner' alias='aa'>
			//            <attribute name='smx_producttype' />
			//            <filter type='and'>
			//                <condition attribute='smx_producttype' operator='not-in'>
			//                <value>180700003</value>
			//                <value>180700002</value>
			//                </condition>
			//            </filter>
			//        </link-entity>
			//      </entity>
			//    </fetch>";
			//Added by Yash on 23-07-2020 - 57416
			var fetch = $@"      
               <fetch>
                  <entity name='new_cpq_lineitem_tmp'>
                    <attribute name='new_cpq_lineitem_tmpid' />
                    <filter type='and'>
                     <condition attribute='smx_lineitemstatus' operator='not-in'>
                      <value>180700002</value>
                      <value>180700003</value>
                     </condition>
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
			//End
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
		private new_cpq_quote RetrieveQuoteFromAgreement(string quoteNumber)
		{
			var fetch = $@"
             <fetch version='1.0' output-format='xml - platform' mapping='logical' distinct='false'>
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
	                     <condition attribute='new_name' operator='eq' value='{quoteNumber}' />
		                </filter >
					  </entity>
                </fetch>";

			return quoteNumber != null
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
		//Added by Yash on 12-08-2020--Ticket No 57667
		private Entity GetOpenTerritory(Guid saleorderId)
		{
			_tracer.Trace("Entered GetOpenTerritory Method");
			Entity saleOrder = new Entity();
			try
			{
				var fetch = $@"<fetch>
                                 <entity name='smx_salesorder'>
                                 <attribute name='smx_salesorderid' />
                                 <attribute name='smx_name' />
                                   <filter type='and'>
                                      <condition attribute='smx_salesorderid' operator='eq' value='{saleorderId}' />
                                     <condition attribute='smx_territoryid' operator='not-null' />
                                  </filter>
                               <link-entity name='territory' from='territoryid' to='smx_territoryid'  link-type='outer' alias='territory'>
                               <attribute name='smx_openterritory' />
                              <attribute name='smx_territoryid' />
							   </link-entity>
                               </entity>
                             </fetch>";
				EntityCollection saleOrders = _orgService.RetrieveMultiple(new FetchExpression(fetch));
				if (saleOrders.Entities.Count() > 0)
				{
					_tracer.Trace("Saleorders " + saleOrders.Entities.Count());
				    saleOrder = saleOrders.Entities.FirstOrDefault();
				}
			}
			catch (Exception)
			{
				_tracer.Trace("Exit GetOpenTerritory Method");
				return saleOrder;
			}
			_tracer.Trace("Exit GetOpenTerritory Method");
			return saleOrder;
		}
		//End
		//Added by Yash on 31-08-2020--Ticket No 57863
		//private Money GetProductEVEValue(Guid lineItemProductId)
		//{
		//	Money EVEPrice = null;
		//	try
		//	{
		//		var fetch = $@"<fetch>
  //                               <entity name='smx_product'>
  //                                  <attribute name='smx_productid' />
  //                                  <attribute name='smx_name' />
  //                                  <attribute name='smx_eveprice' /> 
  //                                  <filter type='and'>
  //                                    <condition attribute='smx_productid' operator='eq' value='{lineItemProductId}' />
  //                                  </filter>
  //                                </entity>
  //                           </fetch>";
		//		EntityCollection lineItemProduct = _orgService.RetrieveMultiple(new FetchExpression(fetch));
		//		if (lineItemProduct.Entities.Count() > 0)
		//		{
		//			_tracer.Trace("Line Item Products " + lineItemProduct.Entities.Count());
		//		     Entity	product = lineItemProduct.Entities.FirstOrDefault();
		//			EVEPrice = product.Contains("smx_eveprice") ? product.GetAttributeValue<Money>("smx_eveprice") : null;
		//			_tracer.Trace("EVEPrice" +EVEPrice);
		//		}
		//	}
		//	catch (Exception ex)
		//	{
		//		return EVEPrice;
		//	}
		//	return EVEPrice;
		//}
		//Added by Yash on 10-09-2020--Ticket No 57863
		private Entity GetProduct(Guid lineItemProductId)
		{
			Entity product = new Entity();
			try
			{
				var fetch = $@"<fetch>
                                 <entity name='smx_product'>
                                    <attribute name='smx_productid' />
                                    <attribute name='smx_name' />
                                    <attribute name='smx_eveprice' /> 
                                    <attribute name='smx_eveoverride' />
									<filter type='and'>
                                      <condition attribute='smx_productid' operator='eq' value='{lineItemProductId}' />
                                    </filter>
                                  </entity>
                             </fetch>";
				EntityCollection lineItemProduct = _orgService.RetrieveMultiple(new FetchExpression(fetch));
				if (lineItemProduct.Entities.Count() > 0)
				{
					_tracer.Trace("Line Item Products " + lineItemProduct.Entities.Count());
					product = lineItemProduct.Entities.FirstOrDefault();
					
				}
			}
			catch (Exception ex)
			{
				_tracer.Trace("Get Product Menthod Exception" + ex.Message);
				return product;
			}
			return product;
		}

		//End
	}

	public class EmpDetails
    {
        public string empNo { get; set; }
        public string nameDerivedFromEmail { get; set; }
    }
}
