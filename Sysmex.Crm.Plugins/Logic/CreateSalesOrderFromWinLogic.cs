using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using SonomaPartners.Crm.Toolkit;
using Sysmex.Crm.Model;

namespace Sysmex.Crm.Plugins.Logic
{
    class CreateSalesOrderFromWinLogic : LogicBase
    {
        public CreateSalesOrderFromWinLogic(IOrganizationService orgService, ITracingService tracer)
            :base(orgService, tracer)
        {
        }

        public void CreateSalesOrdersFromOpportunityLabs(Guid opportunityId)
        {
            _tracer.Trace("Create Sales Orders From Opportunity Labs");

            var opportunityLabs = RetrieveOpportunityLabsAndRelatedFields(opportunityId);
            var cpqLineItems = RetrieveCPQLineItems(opportunityId);

            foreach (var opportunityLab in opportunityLabs)
            {
                CreateSalesOrder(opportunityLab, cpqLineItems);
            }
        }

        private void CreateSalesOrder(smx_opportunitylab opportunityLab, IEnumerable<new_cpq_lineitem_tmp> cpqLineItems)
        {
            _tracer.Trace("Create Sales Order");

            var opportunityId = opportunityLab.smx_OpportunityId.Id;
            var agreementRecord = RetrieveFirstAgreement(opportunityId);
            var cpqRecord = RetrieveFirstCPQQuote(opportunityId);

            var salesOrder = new smx_salesorder()
            {
                smx_name = $"{opportunityLab.GetAliasedAttributeValue<string>("Opportunity.name")} - {opportunityLab.smx_name}",
                smx_ESRTier = opportunityLab.GetAliasedAttributeValue<OptionSetValue>("Opportunity.smx_esrtier"),
                smx_FlowTier = opportunityLab.GetAliasedAttributeValue<OptionSetValue>("Opportunity.smx_flowtier"),
                smx_LabLocationId = opportunityLab.smx_ShipToAddressId,
                smx_MainContactId = opportunityLab.GetAliasedAttributeValue<EntityReference>("Opportunity.parentcontactid"),
                smx_Account = opportunityLab.GetAliasedAttributeValue<EntityReference>("Opportunity.parentaccountid"),
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
                OwnerId = opportunityLab.OwnerId,
                smx_OpportunityLabID = opportunityLab.ToEntityReference(),
                smx_AgreementURL = agreementRecord?.new_agreementurltoapttus,
                smx_TerritoryId = opportunityLab.GetAliasedAttributeValue<EntityReference>("account.territoryid"),
                smx_RegionId = opportunityLab.GetAliasedAttributeValue<EntityReference>("territory.smx_region"),
                smx_AccountManagerId = opportunityLab.GetAliasedAttributeValue<EntityReference>("account.ownerid")
            };

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

            var newSalesOrderId = _orgService.Create(salesOrder);

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

            //Update CPQ Line Items with new Sales Order
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

        private IEnumerable<new_cpq_lineitem_tmp> RetrieveCPQLineItems(Guid opportunityId)
        {
            _tracer.Trace("Retrieve CPQ Line Items");

            var fetch = $@"
                <fetch>
                  <entity name='new_cpq_lineitem_tmp'>
                    <attribute name='new_cpq_lineitem_tmpid' />
                    <attribute name='new_locationid' />
                    <attribute name='smx_annualtargettestcount' />
                    <link-entity name='new_cpq_productconfiguration' from='new_cpq_productconfigurationid' to='new_productconfigurationid' link-type='inner' alias='ad'>
                      <link-entity name='new_cpq_quote' from='new_cpq_quoteid' to='new_quoteid' link-type='inner' alias='ae'>
                        <filter type='and'>
                          <condition attribute='statecode' operator='eq' value='{(int)new_cpq_quoteState.Active}' />
                        </filter>
                        <link-entity name='opportunity' from='opportunityid' to='new_opportunityid' link-type='inner' alias='af'>
                          <filter type='and'>
                            <condition attribute='opportunityid' operator='eq' value='{opportunityId}' />
                          </filter>
                        </link-entity>
                      </link-entity>
                    </link-entity>
                  </entity>
                </fetch>";

            return _orgService.RetrieveMultipleAll(fetch).Entities.Select(x => x.ToEntity<new_cpq_lineitem_tmp>());
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
                    <filter type='and'>
                      <condition attribute='smx_opportunityid' operator='eq' value='{opportunityId}' />
                    </filter>
                    <link-entity name='smx_address' from='smx_addressid' to='smx_shiptoaddressid' link-type='inner' alias='smx_shiptoaddress'>
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
    }
}
