using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using SonomaPartners.Crm.Toolkit;
using SonomaPartners.Crm.Toolkit.Plugins;
using Sysmex.Crm.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Sysmex.Crm.Plugins.Logic
{
    class CreateInstrumentRecordsLogic
    {
        private IOrganizationService _systemOrgService;
        private IServiceProvider _serviceProvider;
        private ITracingService _tracer;

        public CreateInstrumentRecordsLogic(IServiceProvider serviceProvider, ITracingService tracer)
        {
            _systemOrgService = serviceProvider.CreateSystemOrganizationService();
            _serviceProvider = serviceProvider;
            _tracer = tracer;
        }

        public void CreateInstrumentRecordsFromCPQLineItems(Guid opportunityId)
        {
            _tracer.Trace(MethodBase.GetCurrentMethod().Name);

            var cpqLineItems = RetrieveCPQLineItems2(opportunityId);

            foreach (var cpqLineItem in cpqLineItems)
            {
                CreateInstrumentRecord(cpqLineItem.Id);
            }
        }

        private void CreateInstrumentRecord(Guid cpqLineItemId)
        {
            var cpqLineItem = RetrieveCRMRecord<new_cpq_lineitem_tmp>(new_cpq_lineitem_tmp.EntityLogicalName, cpqLineItemId, new string[]
            {
                "new_name",
                "new_locationid",
                "new_optionid"
            });
            var opportunity = RetrieveOpportunityFromCPQLineItem(cpqLineItem.Id);
            var opportunityLab = RetrieveOpportunityLabByShipToAddressAndOpportunity(cpqLineItem.new_LocationId?.Id, opportunity?.Id);
            var lab = RetrieveCRMRecord<smx_lab>(smx_lab.EntityLogicalName, opportunityLab?.smx_LabId?.Id, new string[] { //TODO: Where to get smx_lab from?
                "ownerid"
            });
            
            smx_model model = null;
            if (cpqLineItem.new_optionid != null)
            {
                var product = RetrieveCRMRecord<smx_product>(smx_product.EntityLogicalName, cpqLineItem.new_optionid?.Id, new string[] { "smx_crmmodelid" });
                
                model = RetrieveCRMRecord<smx_model>(smx_model.EntityLogicalName, product?.smx_CRMModelID?.Id, new string[]
                {
                "smx_productline"
                });
            }
            var manufacturer = RetrieveManufacturerByName("Sysmex");

            var instrument = new smx_instrument()
            {
                //Created by requires an orgservice created under that user, see create task
                smx_ProductLine = model?.smx_ProductLine,
                smx_Model = model?.ToEntityReference(),
                smx_Manufacturer = manufacturer?.ToEntityReference(),
                smx_Lab = opportunityLab?.smx_LabId,
                smx_YearofAcquisition = DateTime.Now.Year.ToString(),
                smx_frequencyofuse = false, //Primary
                smx_MetrixInstrument = false,
                smx_Account = opportunityLab?.smx_AccountId
            };

            _tracer.Trace($"Address - {cpqLineItem.new_LocationId?.Id} id - {cpqLineItemId.ToString()} OppId - {opportunity?.Id} OppLab is Null - {opportunityLab == null}");

            if (lab != null)
            {
                instrument.OwnerId = lab.OwnerId;
                var impersonatedOrgService = _serviceProvider.CreateOrganizationService(lab.OwnerId.Id);
                impersonatedOrgService.Create(instrument);
            }
            else
            {
                _systemOrgService.Create(instrument);
            }
        }

        private T RetrieveCRMRecord<T>(string recordLogicalName, Guid? recordId, IEnumerable<string> columns) where T : Entity
        {
            _tracer.Trace($"{MethodBase.GetCurrentMethod().Name}: {recordLogicalName} - {recordId}");

            if (recordId == null)
            {
                return null;
            }

            var record = _systemOrgService.Retrieve(recordLogicalName, recordId.Value, new ColumnSet(columns.ToArray()));
            return record.ToEntity<T>();
        }

        private IEnumerable<new_cpq_lineitem_tmp> RetrieveCPQLineItems2(Guid opportunityId)
        {
            _tracer.Trace("Retrieve CPQ Line Items");
            var agreementQe = new QueryExpression("new_clm_agreement");
            agreementQe.Criteria = new FilterExpression();
            agreementQe.Criteria.AddCondition("new_opportunityid", ConditionOperator.Equal, opportunityId);
            agreementQe.Criteria.AddCondition("new_status", ConditionOperator.Equal, 100000011);
            agreementQe.TopCount = 1;
            agreementQe.ColumnSet = new ColumnSet("new_quotenumber");

            var agreements = _systemOrgService.RetrieveMultiple(agreementQe);
            if (agreements.Entities.Count == 0)
            {
                return new List<new_cpq_lineitem_tmp>();
            }

            var quoteNumber = agreements[0].GetAttributeValue<string>("new_quotenumber");
            if (string.IsNullOrEmpty(quoteNumber))
            {
                return new List<new_cpq_lineitem_tmp>();
            }

            var qe = new QueryExpression("new_cpq_productconfiguration");
            qe.ColumnSet = new ColumnSet("new_cpqstatus");
            qe.AddOrder("new_versionnumber", OrderType.Descending);
            qe.NoLock = true;

            var le = qe.AddLink("new_cpq_quote", "new_quoteid", "new_cpq_quoteid", JoinOperator.Inner);
            le.LinkCriteria = new FilterExpression();
            le.LinkCriteria.AddCondition("new_name", ConditionOperator.Equal, quoteNumber);

            var configs = _systemOrgService.RetrieveMultiple(qe);

            var config = configs.Entities.FirstOrDefault(c => c.GetAttributeValue<OptionSetValue>("new_cpqstatus")?.Value == 100000006); // finalized
            if (config == null)
            {
                config = configs.Entities.FirstOrDefault();
            }

            if (config == null)
            {
                return new List<new_cpq_lineitem_tmp>();
            }

            var fetch = $@"
                <fetch>
                  <entity name='new_cpq_lineitem_tmp'>
                    <attribute name='new_cpq_lineitem_tmpid' />
                    <attribute name='new_locationid' />
                    <attribute name='smx_annualtargettestcount' />
                    <filter type='and'>
                          <condition attribute='new_productconfigurationid' operator='eq' value='{config.Id}' />
                          <condition attribute='new_producttype' operator='eq' value='Instrument' />
                    </filter>
                  </entity>
                </fetch>";

            return _systemOrgService.RetrieveMultipleAll(fetch).Entities.Select(x => x.ToEntity<new_cpq_lineitem_tmp>());
        }

        //Deprecated
        private IEnumerable<new_cpq_lineitem_tmp> RetrieveCPQLineItems(Guid opportunityId)
        {
            _tracer.Trace(MethodBase.GetCurrentMethod().Name);

            var fetch = $@"
                <fetch>
                  <entity name='new_cpq_lineitem_tmp'>
                    <attribute name='new_cpq_lineitem_tmpid' />
                    <filter type='and'>
                      <condition attribute='new_producttype' operator='eq' value='Instrument' />
                    </filter>
                    <link-entity name='new_cpq_productconfiguration' to='new_productconfigurationid' from='new_cpq_productconfigurationid' link-type='inner' alias='pconf'>
                        <link-entity name='new_cpq_quote' from='new_cpq_quoteid' to='new_quoteid' link-type='inner' alias='ncl'>
                          <filter type='and'>
                            <condition attribute='new_isprimary' operator='eq' value='1' />
                          </filter>
                          <link-entity name='opportunity' from='opportunityid' to='new_opportunityid' link-type='inner' alias='opp'>
                            <filter type='and'>
                              <condition attribute='opportunityid' operator='eq' value='{opportunityId}' />
                            </filter>
                          </link-entity>
                        </link-entity>
                    </link-entity>
                  </entity>
                </fetch>";

            return _systemOrgService.RetrieveMultipleAll(fetch).Entities.Select(x => x.ToEntity<new_cpq_lineitem_tmp>());
        }

        private smx_product RetrieveProductByName(string name)
        {
            _tracer.Trace(MethodBase.GetCurrentMethod().Name);

            var fetch = $@"
                <fetch top='1'>
                  <entity name='smx_product'>
                    <attribute name='smx_productid' />
                    <attribute name='smx_crmmodelid' />
                    <filter type='and'>
                      <condition attribute='smx_name' operator='eq' value='{name}' />
                    </filter>
                  </entity>
                </fetch>";

            return !String.IsNullOrWhiteSpace(name)
                ? _systemOrgService.RetrieveMultiple<smx_product>(new FetchExpression(fetch)).FirstOrDefault()
                : null;
        }

        private smx_manufacturer RetrieveManufacturerByName(string name)
        {
            _tracer.Trace(MethodBase.GetCurrentMethod().Name);

            var fetch = $@"
                <fetch top='1'>
                  <entity name='smx_manufacturer'>
                    <attribute name='smx_manufacturerid' />
                    <filter type='and'>
                      <condition attribute='smx_name' operator='eq' value='{name}' />
                    </filter>
                  </entity>
                </fetch>";

            return !String.IsNullOrWhiteSpace(name)
                ? _systemOrgService.RetrieveMultiple<smx_manufacturer>(new FetchExpression(fetch)).FirstOrDefault()
                : null;
        }

        private smx_opportunitylab RetrieveOpportunityLabByShipToAddressAndOpportunity(Guid? shipToAddressId, Guid? opportunityId)
        {
            _tracer.Trace(MethodBase.GetCurrentMethod().Name);

            var fetch = $@"
                <fetch top='1'>
                  <entity name='smx_opportunitylab'>
                    <attribute name='smx_opportunitylabid' />
                    <attribute name='smx_labid' />
                    <attribute name='smx_accountid' />
                    <filter type='and'>
                      <condition attribute='smx_shiptoaddressid' operator='eq' value='{shipToAddressId}' />
                      <condition attribute='smx_opportunityid' operator='eq' value='{opportunityId}' />
                    </filter>
                  </entity>
                </fetch>";

            return shipToAddressId != null && opportunityId != null
                ? _systemOrgService.RetrieveMultiple<smx_opportunitylab>(new FetchExpression(fetch)).FirstOrDefault()
                : null;
        }

        private Opportunity RetrieveOpportunityFromCPQLineItem(Guid cpqLineItemId)
        {
            _tracer.Trace(MethodBase.GetCurrentMethod().Name);

            var fetch = $@"
                <fetch top='1'>
                  <entity name='opportunity'>
                    <attribute name='opportunityid' />
                    <link-entity name='new_cpq_quote' from='new_opportunityid' to='opportunityid' link-type='inner' alias='ncq'>
                      <link-entity name='new_cpq_productconfiguration' from='new_quoteid' to='new_cpq_quoteid' link-type='inner' alias='pconf'>
                        <link-entity name='new_cpq_lineitem_tmp' from='new_productconfigurationid' to='new_cpq_productconfigurationid' link-type='inner' alias='nclt'>
                            <filter type='and'>
                              <condition attribute='new_cpq_lineitem_tmpid' operator='eq' value='{cpqLineItemId}' />
                            </filter>
                        </link-entity>
                      </link-entity>
                    </link-entity>
                  </entity>
                </fetch>";

            return _systemOrgService.RetrieveMultiple<Opportunity>(new FetchExpression(fetch)).FirstOrDefault();
        }
    }
}
