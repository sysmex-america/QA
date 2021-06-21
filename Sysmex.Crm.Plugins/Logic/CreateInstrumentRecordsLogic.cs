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
		//Added by Yash on 09-10-2020--Ticket No 58546
		private IOrganizationService _orgService;
		//End
		public CreateInstrumentRecordsLogic(IServiceProvider serviceProvider, ITracingService tracer, IOrganizationService orgService)
        {
            _systemOrgService = serviceProvider.CreateSystemOrganizationService();
            _serviceProvider = serviceProvider;
            _tracer = tracer;
			_orgService = orgService;
        }

        public void CreateInstrumentRecordsFromCPQLineItems(Guid opportunityId)
        {
			// _tracer.Trace(MethodBase.GetCurrentMethod().Name);
			_tracer.Trace("Started CreateInstrumentRecordsFromCPQLineItems Method");
			var cpqLineItems = RetrieveCPQLineItems(opportunityId);
            foreach (var cpqLineItem in cpqLineItems)
            {
                CreateInstrumentRecord(cpqLineItem.Id, opportunityId);
            }
			
		}

        private void CreateInstrumentRecord(Guid cpqLineItemId, Guid opportunityId)
        {
            var cpqLineItem = RetrieveCRMRecord<new_cpq_lineitem_tmp>(new_cpq_lineitem_tmp.EntityLogicalName, cpqLineItemId, new string[]
            {
                "new_name",
                "new_locationid",
                "new_optionid",
				"new_productconfigurationid"
			});
			//var opportunity = RetrieveOpportunityFromCPQLineItem(cpqLineItem.Id);
			//var opportunityLab = RetrieveOpportunityLabByShipToAddressAndOpportunity(cpqLineItem.new_LocationId?.Id, opportunity?.Id);
		    var opportunityLab = RetrieveOpportunityLabByShipToAddressAndOpportunity(cpqLineItem.new_LocationId?.Id, opportunityId);
			var lab = RetrieveCRMRecord<smx_lab>(smx_lab.EntityLogicalName, opportunityLab?.smx_LabId?.Id, new string[] { //TODO: Where to get smx_lab from?
                "ownerid"
            });
            var product = cpqLineItem.new_optionid;

			//Added by Yash on 17-08-2020--Ticket No 57672

			//var model = RetrieveCRMRecord<smx_model>(smx_model.EntityLogicalName, product?.Id, new string[]
			//         {
			//             "smx_productline"
			//         });
			//Added by Yash on 09-10-2020--Ticket No 58546
			if (product != null)
			{
				Entity enProduct = GetProduct(product?.Id);
				EntityReference model = enProduct.Contains("smx_crmmodelid") ? enProduct.GetAttributeValue<EntityReference>("smx_crmmodelid") : null;
				if (model != null)
				{
					AliasedValue productLine = enProduct.Contains("Model.smx_productline") ? enProduct.GetAttributeValue<AliasedValue>("Model.smx_productline") : null;
					//Added by Yash on 15-09-2020--Ticket No 57672
					OptionSetValue acquisitionType = null;
					EntityReference productConfigId = cpqLineItem.new_ProductConfigurationId;
					if (productConfigId != null)
					{
						Entity quote = GetProductConfigQoute(productConfigId?.Id);
						acquisitionType = quote.Contains("new_acquisitiontype") ? quote.GetAttributeValue<OptionSetValue>("new_acquisitiontype") : null;
						if (acquisitionType != null)
						{
							switch (acquisitionType.Value)
							{
								case 100000000:
									acquisitionType = new OptionSetValue(180700002);
									break;
								case 100000001:
									acquisitionType = new OptionSetValue(180700001);
									break;
								case 100000002:
									acquisitionType = new OptionSetValue(180700000);
									break;
							}
						}

					}
					//End
					var manufacturer = RetrieveManufacturerByName("Sysmex");



					var instrument = new smx_instrument()
					{
						//Created by requires an orgservice created under that user, see create task
						//smx_ProductLine = model?.smx_ProductLine,
						smx_ProductLine = productLine != null ? (EntityReference)productLine.Value : null,
						//smx_Model = model?.ToEntityReference(),
						smx_Model = model,
						smx_Manufacturer = manufacturer?.ToEntityReference(),
						smx_Lab = opportunityLab?.smx_LabId,
						smx_YearofAcquisition = DateTime.Now.Year.ToString(),
						smx_frequencyofuse = false, //Primary
						smx_MetrixInstrument = false,
						smx_Account = opportunityLab?.smx_AccountId,
						smx_ModeofAcquisition = acquisitionType
					};

					if (lab != null)
					{
						instrument.OwnerId = lab.OwnerId;
						//Added by Yash on 09-10-2020--Ticket No 58546
						//var impersonatedOrgService = _serviceProvider.CreateOrganizationService(lab.OwnerId.Id);
						//impersonatedOrgService.Create(instrument);
						_orgService.Create(instrument);//SVC-Dunamic-User
						//End
						
						_tracer.Trace("Instrument Created");
						//Added by Yash on 02-02-2021--Ticket No 59844
						Entity opportunity = new Entity("opportunity", opportunityId);
						opportunity["smx_createinstruments"] = false;
						_orgService.Update(opportunity);
						_tracer.Trace("Opportunty createinstruments set to no");
						//End

					}
					else
					{
						// _systemOrgService.Create(instrument);
						_tracer.Trace("Lab Not Available");
					}
				}

			}
        }

        private T RetrieveCRMRecord<T>(string recordLogicalName, Guid? recordId, IEnumerable<string> columns) where T : Entity
        {
           // _tracer.Trace($"{MethodBase.GetCurrentMethod().Name}: {recordLogicalName} - {recordId}");

            if (recordId == null)
            {
                return null;
            }

            var record = _systemOrgService.Retrieve(recordLogicalName, recordId.Value, new ColumnSet(columns.ToArray()));
            return record.ToEntity<T>();
        }
		
		private IEnumerable<new_cpq_lineitem_tmp> RetrieveCPQLineItems(Guid opportunityId)
        {
			// _tracer.Trace(MethodBase.GetCurrentMethod().Name);
			_tracer.Trace("Started RetrieveCPQLineItems Method");
			//Added by Yash on 28-08-2020--Ticket No 57672
			//var fetch = $@"
			//             <fetch>
			//               <entity name='new_cpq_lineitem_tmp'>
			//                 <attribute name='new_cpq_lineitem_tmpid' />
			//                 <filter type='and'>
			//                   <condition attribute='new_producttype' operator='eq' value='Instrument' />
			//                 </filter>
			//                 <link-entity name='new_cpq_quote' from='new_cpq_quoteid' to='new_quoteid' link-type='inner' alias='ncl'>
			//                   <filter type='and'>
			//                     <condition attribute='new_isprimary' operator='eq' value='1' />
			//                   </filter>
			//                   <link-entity name='opportunity' from='opportunityid' to='new_opportunityid' link-type='inner' alias='opp'>
			//                     <filter type='and'>
			//                       <condition attribute='opportunityid' operator='eq' value='{opportunityId}' />
			//                     </filter>
			//                   </link-entity>
			//                 </link-entity>
			//               </entity>
			//             </fetch>";
			//Added by Yash on 09-10-2020--Ticket No 58546
			//var fetch = $@"
			//              <fetch>
			//                 <entity name='new_cpq_lineitem_tmp'>
			//                   <attribute name='new_cpq_lineitem_tmpid' />
			//                   <filter type='and'>
			//                     <condition attribute='new_producttype' operator='eq' value='Instrument' />
			//                   </filter>
			//                    <link-entity name='new_cpq_productconfiguration' from='new_cpq_productconfigurationid' to='new_productconfigurationid' link-type='inner' alias='PC'>
			//                       <link-entity name='new_cpq_quote' from='new_cpq_quoteid' to='new_quoteid' link-type='inner' alias='ncl'>
			//						  <filter type='and'>
			//                              <condition attribute='new_isprimary' operator='eq' value='1' />
			//                          </filter>
			//                       <link-entity name='opportunity' from='opportunityid' to='new_opportunityid' link-type='inner' alias='opp'>
			//                         <filter type='and'>
			//                             <condition attribute='opportunityid' operator='eq' value='{opportunityId}' />
			//                         </filter>
			//                     </link-entity>
			//                     </link-entity>
			//                     </link-entity>
			//                   </entity>
			//              </fetch>";

			var fetch = $@"
                          <fetch>
                               <entity name='new_cpq_lineitem_tmp'>
                                     <attribute name='new_cpq_lineitem_tmpid' />
                                    <filter type='and'>
                                    <condition attribute='statecode' operator='eq' value='0' />
                                    <condition attribute='new_producttype' operator='eq' value='Instrument' />
                                   </filter>
                                  <link-entity name='smx_product' from='smx_productid' to='new_optionid' link-type='inner' alias='Product'>
                                     <filter type='and'>
                                          <condition attribute='smx_crmmodelid' operator='not-null' />
                                     </filter>
                                 </link-entity>
                                 <link-entity name='new_cpq_productconfiguration' from='new_cpq_productconfigurationid' to='new_productconfigurationid' link-type='inner' alias='PC'>
                                    <link-entity name='new_cpq_quote' from='new_cpq_quoteid' to='new_quoteid' link-type='inner' alias='ncl'>
                                    <link-entity name='opportunity' from='opportunityid' to='new_opportunityid' link-type='inner' alias='opp'>
                                     <filter type='and'>
                                        <condition attribute='statecode' operator='eq' value='1' />
                                        <condition attribute='opportunityid' operator='eq' value='{opportunityId}' />
                                     </filter>
                                    </link-entity>
                                    </link-entity>
                                 </link-entity>
                              <link-entity name='smx_salesorder' from='smx_salesorderid' to='smx_salesorderid' link-type='inner' alias='SO' />
                           </entity>
                        </fetch>";

			return _systemOrgService.RetrieveMultipleAll(fetch).Entities.Select(x => x.ToEntity<new_cpq_lineitem_tmp>());
        }

        private smx_product RetrieveProductByName(string name)
        {
			//_tracer.Trace(MethodBase.GetCurrentMethod().Name);
			_tracer.Trace("Started RetrieveProductByName Method");
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
			// _tracer.Trace(MethodBase.GetCurrentMethod().Name);
			_tracer.Trace("Started RetrieveManufacturerByName Method");
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
			// _tracer.Trace(MethodBase.GetCurrentMethod().Name);
			_tracer.Trace("Started RetrieveOpportunityLabByShipToAddressAndOpportunity Method");
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
			//  _tracer.Trace(MethodBase.GetCurrentMethod().Name);
			_tracer.Trace("Started RetrieveOpportunityFromCPQLineItem Method");
			var fetch = $@"
                <fetch top='1'>
                  <entity name='opportunity'>
                    <attribute name='opportunityid' />
                    <link-entity name='new_cpq_quote' from='new_opportunityid' to='opportunityid' link-type='inner' alias='ncq'>
                      <link-entity name='new_cpq_lineitem_tmp' from='new_quoteid' to='new_cpq_quoteid' link-type='inner' alias='nclt'>
                        <filter type='and'>
                          <condition attribute='new_cpq_lineitem_tmpid' operator='eq' value='{cpqLineItemId}' />
                        </filter>
                      </link-entity>
                    </link-entity>
                  </entity>
                </fetch>";

            return _systemOrgService.RetrieveMultiple<Opportunity>(new FetchExpression(fetch)).FirstOrDefault();
        }
		private Entity GetProduct(Guid? productId)
		{
			_tracer.Trace("Started GetProduct Method");
			Entity model = new Entity();
			//Added by Yash on 01-03-2021--Ticket No 60569--Added Model Condition.
			var fetch = $@"
                      <fetch>
                        <entity name='smx_product'>
                        <attribute name='smx_productid' />
                        <attribute name='smx_name' />
                        <attribute name='smx_crmmodelid' />
                       <filter type='and'>
                           <condition attribute='smx_productid' operator='eq' value='{productId}' />
                       </filter>
                       <link-entity name='smx_model' from='smx_modelid' to='smx_crmmodelid' link-type='inner' alias='Model'>
                         <attribute name='smx_productline' />
                         <filter type='and'>
                             <condition attribute='statecode' operator='eq' value='0' />
                        </filter>
                       </link-entity>
                       </entity>
                     </fetch>";
			EntityCollection ecProducts = _systemOrgService.RetrieveMultiple(new FetchExpression(fetch));
			if(ecProducts.Entities.Count>0)
			{
				_tracer.Trace("Prducts Count:" + ecProducts.Entities.Count);
				model = ecProducts.Entities.FirstOrDefault();
			}
			return model;
		}
		private Entity GetProductConfigQoute(Guid? productConfigId)
		{
			_tracer.Trace("Started GetProductConfiguration Method");
			Entity quote = new Entity();

			var fetch = $@"
                      <fetch>
                          <entity name='new_cpq_quote'>
                          <attribute name='new_name' />
                          <attribute name='new_acquisitiontype' />
                          <attribute name='new_cpq_quoteid' />
                           <link-entity name='new_cpq_productconfiguration' from='new_quoteid' to='new_cpq_quoteid' link-type='inner' alias='ProductConfig'>
                              <filter type='and'>
                                  <condition attribute='new_cpq_productconfigurationid' operator='eq' value='{productConfigId}' />
                               </filter>
                          </link-entity>
                       </entity>
                     </fetch>";
			EntityCollection ecQuotes = _systemOrgService.RetrieveMultiple(new FetchExpression(fetch));
			if (ecQuotes.Entities.Count > 0)
			{
				_tracer.Trace("Quotes Count:" + ecQuotes.Entities.Count);
				quote = ecQuotes.Entities.FirstOrDefault();
			}
			return quote;
		}
		//Added by Yash on 14-01-2021--Ticket No 59844
		public Entity getOpportunity(Guid opportunityId)
		{
			Entity opportunity = _orgService.Retrieve("opportunity", opportunityId, new ColumnSet("ownerid", "smx_createinstruments"));
			return opportunity;
		}
		public string getUserBusinessUnit(Guid opportunityManagerId)
		{
			Entity _businessUnitEntity = _orgService.Retrieve("systemuser", opportunityManagerId, new ColumnSet("businessunitid"));
			return (_businessUnitEntity.GetAttributeValue<EntityReference>("businessunitid").Name);
		}
		//End
	}
}
