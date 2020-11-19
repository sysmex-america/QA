using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using SonomaPartners.Crm.Toolkit;
using Sysmex.Crm.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sysmex.Crm.Plugins.Logic
{
    class CreateSalesOrderFromWinLogic : LogicBase
    {
        public CreateSalesOrderFromWinLogic(IOrganizationService orgService, ITracingService tracer)
            : base(orgService, tracer)
        {
        }

        public void CreateSalesOrdersFromOpportunityLabs(Guid opportunityId)
        {
            _tracer.Trace("Create Sales Orders From Opportunity Labs");

            try
            {
                var cpqRecord = RetrieveFirstCPQQuote(opportunityId);
                bool opportunityIsWon = IsOpportunityWon(opportunityId);
                _tracer.Trace(" and isWon: " + opportunityIsWon.ToString());
                if (cpqRecord != null && cpqRecord.Id != Guid.Empty && cpqRecord.Id != null && opportunityIsWon)
                {
                    var opportunityLabs = RetrieveOpportunityLabsAndRelatedFields(opportunityId);
                    var opportunityLabsFiltered = new List<smx_opportunitylab>();
                    new_clm_agreement agreementRecord = RetrieveFirstAgreement(opportunityId);
                    if (agreementRecord == null)
                    {
                        _tracer.Trace($"Null agreement");
                        agreementRecord = new new_clm_agreement();
                    }
                    _tracer.Trace($"Quote Number {  agreementRecord.new_QuoteNumber }");

                    EntityReference prodConfig = null;

                    var cpqLineItems = RetrieveCPQLineItemsByQuoteNumber(agreementRecord.new_QuoteNumber, out prodConfig);
                    if (cpqLineItems != null && cpqLineItems.Count() > 0)
                    {
                        _tracer.Trace($"total line items: {cpqLineItems.Count()}");
                        IEnumerable<Guid> distinctAddresses = cpqLineItems.Where(x => x.Contains("new_locationid")).Select(x => x.GetAttributeValue<EntityReference>("new_locationid").Id).ToList().Distinct();
                        _tracer.Trace($" distinct Address : { distinctAddresses.Count() }");

                        foreach (Guid a in distinctAddresses)
                        {
                            List<smx_opportunitylab> oppLabs = opportunityLabs.Where(x => x.Contains("smx_shiptoaddressid") && x.GetAttributeValue<EntityReference>("smx_shiptoaddressid").Id == a).Select(x => x).ToList();
                            if (oppLabs != null && oppLabs.Count() > 0)
                            {
                                opportunityLabsFiltered.Add(oppLabs.FirstOrDefault());
                            }
                        }
                    }
                    foreach (var opportunityLab in opportunityLabsFiltered)
                    {
                        CreateSalesOrder(opportunityLab, cpqLineItems, cpqRecord, agreementRecord, prodConfig);
                    }
                }
                else
                {
                    _tracer.Trace("No Quote Founds for this Opportunity");
                }
            }
            catch (Exception ex)
            {
                _tracer.Trace("Exception :" + ex.Message + " , exception trace : " + ex.StackTrace);
            }
        }

        private void CreateSalesOrder(smx_opportunitylab opportunityLab, IEnumerable<new_cpq_lineitem_tmp> cpqLineItems, new_cpq_quote cpqRecord, new_clm_agreement agreementRecord, EntityReference prodConfig)
        {
            _tracer.Trace("Create Sales Order");

            var opportunityId = opportunityLab.smx_OpportunityId.Id;

            //Added by Yash on 23-06-2020 - ticket id 57042
            Entity _opportunityEntity=_orgService.Retrieve("opportunity", opportunityId,new ColumnSet("ownerid", "smx_opportunitytype", "smx_distributionsalesmanager", "smx_distributor", "smx_accountmanager"));
            //end

            //Added by yash on 26-06-2020 - ticket Id 57086
            Entity _opportunityOwner=_orgService.Retrieve("systemuser", _opportunityEntity.GetAttributeValue<EntityReference>("ownerid").Id, new ColumnSet("smx_cpqapprovalrole"));
            //end

            // EntityReference HSAManager = new EntityReference();

            string salesOrderName = string.Empty;
            salesOrderName = $"{opportunityLab.GetAliasedAttributeValue<string>("Opportunity.name")}";
            if (opportunityLab.smx_AccountId != null && opportunityLab.smx_AccountId.Id != Guid.Empty)
            {
                salesOrderName = salesOrderName + " - " + opportunityLab.smx_AccountId.Name;
            }

            //Commented By yash 01-07-2020 ticket id = 57086 
            //HSAManager = opportunityLab.GetAliasedAttributeValue<EntityReference>("account.ownerid");
            //if (opportunityLab.Contains("Opportunity.smx_opportunitytype") && opportunityLab.GetAliasedAttributeValue<OptionSetValue>("Opportunity.smx_opportunitytype").Value == 180700001)
            //{
            //    _tracer.Trace("this is Multisite opp");
            //    if (!opportunityLab.Contains("Opportunity.smx_distributor") || opportunityLab.GetAliasedAttributeValue<EntityReference>("Opportunity.smx_distributor").Id == Guid.Empty)
            //    {
            //        _tracer.Trace("distributor not available");
            //        Entity labDetails = GetLabDetailsFromOpportunity(opportunityLab.Id);
            //        HSAManager = labDetails.Contains("smx_regionalmanager") ? labDetails.GetAttributeValue<EntityReference>("smx_regionalmanager") : null;
            //        _tracer.Trace($"HSAManager: { (HSAManager != null ? HSAManager.Name : "HSAManager is null")}");
            //    }
            //}


            //Added by yash 01-07-2020 ticket id = 57086
            EntityReference _HSAM = new EntityReference();
            Entity labInfo = GetLabDetailsFromOpportunity(opportunityLab.Id);
            _HSAM = labInfo.Contains("smx_regionalmanager") ? labInfo.GetAttributeValue<EntityReference>("smx_regionalmanager") : null;
			//end
			//Added by yash 12-08-2020 ticket id = 57665
			EntityReference opportunityHSAM = new EntityReference();
			opportunityHSAM = _opportunityEntity.Contains("smx_accountmanager") ? _opportunityEntity.GetAttributeValue<EntityReference>("smx_accountmanager") : null;
			//End
			var salesOrder = new smx_salesorder()
            {
                smx_name = salesOrderName,
                smx_ESRTier = opportunityLab.GetAliasedAttributeValue<OptionSetValue>("Opportunity.smx_esrtier"),
                smx_FlowTier = opportunityLab.GetAliasedAttributeValue<OptionSetValue>("Opportunity.smx_flowtier"),
                //smx_LabLocationId = opportunityLab.smx_ShipToAddressId,
                smx_MainContactId = opportunityLab.GetAliasedAttributeValue<EntityReference>("Opportunity.parentcontactid"),
                smx_Account = opportunityLab.smx_AccountId, //opportunityLab.GetAliasedAttributeValue<EntityReference>("Opportunity.parentaccountid"),
                smx_APContactId = opportunityLab.GetAliasedAttributeValue<EntityReference>("Opportunity.smx_apcontact"),
                smx_PurchasingContactId = opportunityLab.GetAliasedAttributeValue<EntityReference>("Opportunity.smx_purchasingcontact"),
                smx_CAEIHNId = opportunityLab.GetAliasedAttributeValue<EntityReference>("Opportunity.smx_corporateaccountexecihn"),
                smx_ESRGPO = opportunityLab.GetAliasedAttributeValue<EntityReference>("Opportunity.smx_gpoesr"),
                smx_FlowGPO = opportunityLab.GetAliasedAttributeValue<EntityReference>("Opportunity.smx_gpoflow"),
                smx_HemeTier = opportunityLab.GetAliasedAttributeValue<OptionSetValue>("Opportunity.smx_hematologytier"),
                smx_HemeGPO = opportunityLab.GetAliasedAttributeValue<EntityReference>("Opportunity.smx_gpoheme"),
                smx_IHN = opportunityLab.GetAliasedAttributeValue<EntityReference>("Opportunity.smx_ihn"),
                smx_PaymentTerms = ConvertPaymentTerms(agreementRecord?.new_ExtendedPaymentTerms),
                smx_UFGPO = opportunityLab.GetAliasedAttributeValue<EntityReference>("Opportunity.smx_gpourinalysis"),
                smx_UFTier = opportunityLab.GetAliasedAttributeValue<OptionSetValue>("Opportunity.smx_urinalysistier"),
                smx_ContractOption = ConvertContractOption(cpqRecord?.new_printtype),
                smx_ContractType = ConvertContractType(cpqRecord?.new_acquisitiontype),
                smx_CAEGPOId = opportunityLab.GetAliasedAttributeValue<EntityReference>("GPOHeme.ownerid"),
                smx_InstrumentShipToIdId = opportunityLab.smx_ShipToAddressId,
                smx_PartsShipToId = opportunityLab.smx_ShipToAddressId,
                smx_ReagentControlShipToId = opportunityLab.smx_ShipToAddressId,
                smx_AnnualTargetTestCount = SumAnnualTargetTestCount(cpqLineItems),
                smx_OpportunityId = opportunityLab.smx_OpportunityId,
				// OwnerId = opportunityLab.OwnerId,

				//Added by Yash on 23-06-2020 - ticket id 57042 Single site==180700000
				// OwnerId = (_opportunityEntity.GetAttributeValue<OptionSetValue>("smx_opportunitytype").Value== 180700000)?_opportunityEntity.GetAttributeValue<EntityReference>("ownerid"): opportunityLab.OwnerId,
				//end

				smx_OpportunityLabID = opportunityLab.ToEntityReference(),
                smx_TerritoryId = opportunityLab.GetAliasedAttributeValue<EntityReference>("account.territoryid"),
                smx_RegionId = opportunityLab.GetAliasedAttributeValue<EntityReference>("territory.smx_region"),
                smx_AccountManagerId = _HSAM,
                smx_clmagreement = agreementRecord?.ToEntityReference(),
                smx_agreementnumber = agreementRecord?.new_name,
                smx_AgreementURL = agreementRecord?.smx_agreementurl,
                smx_DSM = opportunityLab.GetAliasedAttributeValue<EntityReference>("smx_shiptoaddress.smx_dsmid"),
                smx_ProductConfig = prodConfig,
            };
			//Added by Yash on 08-10-2020 - ticket id 58477
			salesOrder.Attributes.Add("smx_pending", new OptionSetValueCollection(new List<OptionSetValue>() { new OptionSetValue(180700000) }));
			//End
			//Added by yash on 26-06-2020 - ticket Id 57086
			//Added by Yash on 14-10-2020 - ticket id 58607
			int opportunityOwnerApprovalRole = _opportunityOwner.Contains("smx_cpqapprovalrole") ? _opportunityOwner.GetAttributeValue<OptionSetValue>("smx_cpqapprovalrole").Value : 0;
			int _opportunityType = _opportunityEntity.GetAttributeValue<OptionSetValue>("smx_opportunitytype").Value;
            if (_opportunityType == 180700000 )  // Single site==180700000  
            {
				if(opportunityOwnerApprovalRole == 180700001 && opportunityHSAM != null) //CAE=180700001
					salesOrder.OwnerId = opportunityHSAM;
				else if (opportunityOwnerApprovalRole == 180700006 && _HSAM != null) //HSAM=180700006
					salesOrder.OwnerId = _HSAM;
				else
				salesOrder.OwnerId = _opportunityEntity.GetAttributeValue<EntityReference>("ownerid");
            }
            else if (_opportunityType == 180700001) // Multi-Site == 180700001
            {
                if (_opportunityOwner.Attributes.Contains("smx_cpqapprovalrole") && _opportunityOwner.GetAttributeValue<OptionSetValue>("smx_cpqapprovalrole").Value == 180700003) // MDS ==180700003
                {
                    salesOrder.OwnerId = _opportunityEntity.GetAttributeValue<EntityReference>("ownerid");
                }
                else if(_HSAM!=null)
                {
                    salesOrder.OwnerId = _HSAM;
                }
                else
                {
                    _tracer.Trace("Owner Doesn't Exist");
                    throw new InvalidPluginExecutionException("Owner Doesn't Exist");
                }
            }
            else
            {
                salesOrder.OwnerId = opportunityLab.OwnerId;
            }

			//end

			//Added by yash on 29-06-2020 - ticket Id 57045
			 
			if ( _opportunityEntity.Attributes.Contains("smx_distributor") && _opportunityEntity.Attributes.Contains("smx_distributionsalesmanager") && _opportunityOwner.Attributes.Contains("smx_cpqapprovalrole") && _opportunityOwner.GetAttributeValue<OptionSetValue>("smx_cpqapprovalrole").Value == 180700003) // MDS ==180700003
				salesOrder.smx_mds= _opportunityEntity.GetAttributeValue<EntityReference>("smx_distributionsalesmanager");

			//end
			
			//Popilate Bill To/Payer Address fields
			var distributorRef = opportunityLab.GetAliasedAttributeValue<EntityReference>("Opportunity.smx_distributor");
            if (distributorRef == null)
            {
                salesOrder.smx_BillToAddressId = opportunityLab.GetAliasedAttributeValue<EntityReference>("Opportunity.smx_contractsoldtoaddress");
                salesOrder.smx_payeraddressid = opportunityLab.GetAliasedAttributeValue<EntityReference>("Opportunity.smx_contractsoldtoaddress");
                salesOrder.smx_soldtoaddressid = opportunityLab.GetAliasedAttributeValue<EntityReference>("Opportunity.smx_contractsoldtoaddress");
            }
            else
            {
                salesOrder.smx_BillToAddressId = opportunityLab.GetAliasedAttributeValue<EntityReference>("DistributorAccount.smx_address");
                salesOrder.smx_payeraddressid = opportunityLab.GetAliasedAttributeValue<EntityReference>("DistributorAccount.smx_address");
                salesOrder.smx_soldtoaddressid = opportunityLab.GetAliasedAttributeValue<EntityReference>("DistributorAccount.smx_address");
            }

            var newSalesOrderId = Guid.Empty;
            try
            {
                newSalesOrderId = _orgService.Create(salesOrder);
            }
            catch (Exception ex)
            {
                _tracer.Trace("Exception 2:" + ex.Message + " , exception trace 2 : " + ex.StackTrace);
                throw new InvalidPluginExecutionException(ex.Message);
            }

            //Update Instrument Update records with new Sales Order
            var instrumentUpdateIds = RetrieveInstrumentUpdates(opportunityLab.Id);
            _tracer.Trace($"Instruments to update: {instrumentUpdateIds.Count()}");
            foreach (var instrumentUpdateId in instrumentUpdateIds)
            {
                var updatedInstrumentUpdate = new smx_instrumentupdate()
                {
                    Id = instrumentUpdateId,
                    smx_SalesOrderId = new EntityReference(smx_salesorder.EntityLogicalName, newSalesOrderId)
                };

                _orgService.Update(updatedInstrumentUpdate);
            }

            var filteredCPQLineItems = cpqLineItems.Where(x => x.new_LocationId?.Id == opportunityLab.smx_ShipToAddressId?.Id).Select(x => x.Id);

            _tracer.Trace($"CPQ Line items to update: {filteredCPQLineItems.Count()}");
            foreach (var cpqLineItemId in filteredCPQLineItems)
            {
                var updatedCPQLineItem = new new_cpq_lineitem_tmp()
                {
                    Id = cpqLineItemId,
                    smx_SalesOrderId = new EntityReference(smx_salesorder.EntityLogicalName, newSalesOrderId)
                };

                _orgService.Update(updatedCPQLineItem);
            }
        }

        private string SumAnnualTargetTestCount(IEnumerable<new_cpq_lineitem_tmp> cpqLineItems)
        {
            int unused;
            var fieldsToSum = cpqLineItems.Where(x => int.TryParse(x.smx_AnnualTargetTestCount, out unused));
            return fieldsToSum.Sum(x => int.Parse(x.smx_AnnualTargetTestCount)).ToString();
        }

        //From new_cpq_quote to smx_salesorder
        private OptionSetValue ConvertContractOption(new_cpq_quote_new_printtype? printType)
        {
            if (printType == null)
            {
                return null;
            }
            _tracer.Trace("Convert Contract Option");

            switch (printType)
            {
            case new_cpq_quote_new_printtype.A:
                return new OptionSetValue((int)smx_contractoption.A);
            case new_cpq_quote_new_printtype.B:
                return new OptionSetValue((int)smx_contractoption.B);
            case new_cpq_quote_new_printtype.C:
                return new OptionSetValue((int)smx_contractoption.C);
            default:
                return null;
            }
        }

        //From new_cpq_quote to smx_salesorder
        private OptionSetValue ConvertContractType(new_cpq_quote_new_acquisitiontype? acquisitionType)
        {
            if (acquisitionType == null)
            {
                return null;
            }
            _tracer.Trace("Convert Contract Type");

            switch (acquisitionType)
            {
            case new_cpq_quote_new_acquisitiontype.CPR:
                return new OptionSetValue((int)smx_contracttype.CPR);
            case new_cpq_quote_new_acquisitiontype.Lease:
                return new OptionSetValue((int)smx_contracttype.Lease);
            case new_cpq_quote_new_acquisitiontype.Purchase:
                return new OptionSetValue((int)smx_contracttype.Purchase);
            default:
                return null;
            }
        }

        //From new_clm_agreement to smx_salesorder
        private OptionSetValue ConvertPaymentTerms(new_clm_agreement_new_extendedpaymentterms? paymentTerms)
        {
            if (paymentTerms == null)
            {
                return null;
            }
            _tracer.Trace("Convert Payment Terms");

            switch (paymentTerms)
            {
            case new_clm_agreement_new_extendedpaymentterms._120:
                return new OptionSetValue((int)smx_paymentterms._120);
            case new_clm_agreement_new_extendedpaymentterms._121180:
                return new OptionSetValue((int)smx_paymentterms._121180);
            case new_clm_agreement_new_extendedpaymentterms._30:
                return new OptionSetValue((int)smx_paymentterms._30);
            case new_clm_agreement_new_extendedpaymentterms._45:
                return new OptionSetValue((int)smx_paymentterms._45);
            case new_clm_agreement_new_extendedpaymentterms._60:
                return new OptionSetValue((int)smx_paymentterms._60);
            case new_clm_agreement_new_extendedpaymentterms._90:
                return new OptionSetValue((int)smx_paymentterms._90);
            default:
                return null;
            }
        }


        private IEnumerable<new_cpq_lineitem_tmp> RetrieveCPQLineItemsByQuoteNumber(String quoteNumber, out EntityReference prodConfig)
        {
            prodConfig = null;
            try
            {
                QueryExpression qeProductConfiguration = new QueryExpression("new_cpq_productconfiguration")
                {
                    ColumnSet = new ColumnSet("new_versionnumber"),
                    Criteria =
                    {
                        Conditions =
                        {
                        new ConditionExpression("new_cpqstatus",ConditionOperator.Equal,100000006)   // 100000006  -Finalized
                        }
                    }
                };
                LinkEntity toQuoteLink = new LinkEntity("new_cpq_productconfiguration", "new_cpq_quote", "new_quoteid", "new_cpq_quoteid", JoinOperator.Inner);
                toQuoteLink.LinkCriteria.AddCondition(new ConditionExpression("statecode", ConditionOperator.Equal, (int)new_cpq_quoteState.Active));
                toQuoteLink.LinkCriteria.AddCondition(new ConditionExpression("new_name", ConditionOperator.Equal, quoteNumber));
                qeProductConfiguration.LinkEntities.Add(toQuoteLink);
                DataCollection<Entity> pcList = _orgService.RetrieveMultiple(qeProductConfiguration).Entities;

                if (pcList != null && pcList.Count() > 0)
                {
                    Entity firstPCHavingMaxVersion = pcList.OrderByDescending(x => x.GetAttributeValue<Int32>("new_versionnumber")).FirstOrDefault();
                    prodConfig = firstPCHavingMaxVersion.ToEntityReference();

                    QueryExpression qe = new QueryExpression("new_cpq_lineitem_tmp")
                    {
                        ColumnSet = new ColumnSet("new_cpq_lineitem_tmpid", "smx_annualtargettestcount", "new_locationid"),
                        Criteria =
                        {
                            Conditions=
                            {
                                new ConditionExpression("new_productconfigurationid",ConditionOperator.Equal,firstPCHavingMaxVersion.Id)
                            }
                        }
                    };
                    EntityCollection quoteLineList = _orgService.RetrieveMultiple(qe);
                    return quoteLineList.Entities.Select(x => x.ToEntity<new_cpq_lineitem_tmp>());
                }
                else
                {
                    _tracer.Trace("No CPQ Line items Found");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _tracer.Trace($"Exception :{ ex.Message }");
                return null;
            }
        }

        private IEnumerable<Guid> RetrieveInstrumentUpdates(Guid opportunityLabId)
        {
            _tracer.Trace("Retrieve Instrument Updates");

            var fetch = $@"
                <fetch>
                  <entity name='smx_instrumentupdate'>
                    <attribute name='smx_instrumentupdateid' />
                    <filter type='and'>
                      <condition attribute='smx_opportunitylabid' operator='eq' value='{opportunityLabId}' />
                    </filter>
                  </entity>
                </fetch>";

            return _orgService.RetrieveMultipleAll(fetch).Entities.Select(x => x.Id);
        }

        private new_clm_agreement RetrieveFirstAgreement(Guid opportunityId)
        {
            _tracer.Trace("Retrieve First Agreement");

            var fetch = $@"
                <fetch top='1'>
                  <entity name='new_clm_agreement'>
                    <attribute name='new_clm_agreementid' />
                    <attribute name='new_extendedpaymentterms' />
                    <attribute name='new_agreementurltoapttus' />
                    <attribute name='new_quotenumber' />
                    <attribute name='smx_agreementurl' />
                    <attribute name='new_name' />
                    <filter type='and'>
                      <condition attribute='new_opportunityid' operator='eq' value='{opportunityId}' />
                      <condition attribute='new_status' operator='eq' value='{(int)new_clm_agreement_new_status.Activated}' />
                    </filter>
                  </entity>
                </fetch>";

            return _orgService.RetrieveMultiple<new_clm_agreement>(new FetchExpression(fetch)).FirstOrDefault();
        }

        private new_cpq_quote RetrieveFirstCPQQuote(Guid opportunityId)
        {
            _tracer.Trace("Retrieve First CPQ Quote");

            var fetch = $@"
                <fetch top='1'>
                  <entity name='new_cpq_quote'>
                    <attribute name='new_cpq_quoteid' />
                    <attribute name='new_printtype' />
                    <attribute name='new_acquisitiontype' />
                    <filter type='and'>
                      <condition attribute='new_opportunityid' operator='eq' value='{opportunityId}' />
                      <condition attribute='statecode' operator='eq' value='{(int)new_cpq_quoteState.Active}' />
                    </filter>
                  </entity>
                </fetch>";

            return _orgService.RetrieveMultiple<new_cpq_quote>(new FetchExpression(fetch)).FirstOrDefault();
        }

        private Boolean IsOpportunityWon(Guid opportunityId)
        {
            Boolean response = false;
            try
            {
                _tracer.Trace("Is Opportunity Won");

                var fetch = $@"
                <fetch top='1'>
                  <entity name='opportunity'>
                    <attribute name='statecode' />
                    <filter type='and'>
                      <condition attribute='opportunityid' operator='eq' value='{opportunityId}' />
                    </filter>
                  </entity>
                </fetch>";

                Entity opportunity = _orgService.RetrieveMultiple<Entity>(new FetchExpression(fetch)).FirstOrDefault();
                if (opportunity != null && opportunity.Id != Guid.Empty)
                {
                    if (opportunity.Contains("statecode"))
                    {
                        OptionSetValue stateCode = opportunity.GetAttributeValue<OptionSetValue>("statecode");
                        if (stateCode != null && stateCode.Value == 1)  // Won
                        {
                            response = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response = false;
                _tracer.Trace("Exception : " + ex.Message);
            }
            _tracer.Trace("Is Opportunity WON ? : " + response.ToString());
            return response;
        }

        private IEnumerable<smx_opportunitylab> RetrieveOpportunityLabsAndRelatedFields(Guid opportunityId)
        {
            _tracer.Trace("Retrieve Opportunity Labs And Related Fields");

            var fetch = $@"
                <fetch>
                  <entity name='smx_opportunitylab'>
                    <attribute name='smx_name' />
                    <attribute name='smx_opportunitylabid' />
                    <attribute name='smx_shiptoaddressid' />
                    <attribute name='smx_opportunityid' />
                    <attribute name='ownerid' />
                    <attribute name='smx_labid' />
                    <attribute name='smx_accountid' />
                    <filter type='and'>
                      <condition attribute='smx_opportunityid' operator='eq' value='{opportunityId}' />
                    </filter>
                    <link-entity name='smx_address' from='smx_addressid' to='smx_shiptoaddressid' link-type='inner' alias='smx_shiptoaddress'>
                       <attribute name='smx_dsmid' />                        
                            <link-entity name='account' from='accountid' to='smx_account' link-type='inner' alias='account'>
                            <attribute name='ownerid' />
                            <attribute name='territoryid' />
                            <link-entity name='territory' from='territoryid' to='territoryid' link-type='inner' alias='territory'>
                                <attribute name='smx_region' />
                            </link-entity>
                        </link-entity>
                    </link-entity>
                    <link-entity name='opportunity' from='opportunityid' to='smx_opportunityid' link-type='inner' alias='Opportunity'>
                      <attribute name='smx_flowtier' />
                      <attribute name='smx_esrtier' />
                      <attribute name='parentaccountid' />
                      <attribute name='parentcontactid' />
                      <attribute name='smx_apcontact' />
                      <attribute name='smx_purchasingcontact' />
                      <attribute name='name' />
                      <attribute name='smx_corporateaccountexecihn' />
                      <attribute name='smx_gpoesr' />
                      <attribute name='smx_gpoflow' />
                      <attribute name='smx_hematologytier' />
                      <attribute name='smx_gpoheme' />
                      <attribute name='smx_ihn' />
                      <attribute name='smx_contractsoldtoaddress' />
                      <attribute name='smx_distributor' />
                      <attribute name='smx_gpourinalysis' />
                      <attribute name='smx_urinalysistier' />
                      <attribute name='smx_opportunitytype' />
                      <link-entity name='account' from='accountid' to='smx_distributor' link-type='outer' alias='DistributorAccount'>
                        <attribute name='smx_address' />
                      </link-entity>
                      <link-entity name='account' from='accountid' to='smx_gpoheme' link-type='outer' alias='GPOHeme'>
                        <attribute name='ownerid' />
                      </link-entity>
                    </link-entity>
                  </entity>
                </fetch>";

            return _orgService.RetrieveMultipleAll(fetch).Entities.Select(x => x.ToEntity<smx_opportunitylab>());
        }

        private Entity GetLabDetailsFromOpportunity(Guid opportunityLabId)
        {
            Entity lab = new Entity();
            try
            {
                var fetch = $@"
                <fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true' >
                        <entity name='smx_lab' >
                            <attribute name='smx_labid' />
                            <attribute name='smx_regionalmanager' />
                            <order attribute='smx_regionalmanager' descending='false' />
                            <link-entity name='smx_opportunitylab' from='smx_labid' to='smx_labid' link-type='inner' alias='ah' >
                                <filter type='and' >
                                    <condition attribute='smx_opportunitylabid' operator='eq' value='{ opportunityLabId }' />
                                </filter>
                            </link-entity>
                        </entity>
                    </fetch>";

                IEnumerable<Entity> opportunity = _orgService.RetrieveMultiple<Entity>(new FetchExpression(fetch));
                if (opportunity != null && opportunity.Count() > 0)
                {
                    lab = opportunity.FirstOrDefault();
                }

            }
            catch (Exception ex)
            {
                _tracer.Trace("Exception : " + ex.Message);
            }
            return lab;
        }
    }
}
