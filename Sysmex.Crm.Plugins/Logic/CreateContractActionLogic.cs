using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Sysmex.Crm.Model;
using SonomaPartners.Crm.Toolkit;
using Sysmex.Crm.Plugins.Common;

namespace Sysmex.Crm.Plugins.Logic
{
    public class CreateContractActionLogic : LogicBase
    {
        private IOrganizationService _systemOrgService;


        public CreateContractActionLogic(IOrganizationService systemOrgService, IOrganizationService orgService, ITracingService tracer)
            : base(orgService, tracer)
        {
            _systemOrgService = systemOrgService;
        }


                  public string CreateContract(Guid salesOrderId)
            {
                var fetch = $@"<fetch top='1'>
                <entity name='smx_salesorder'>
                    <attribute name='smx_contracttype' />
                    <attribute name='smx_purchaseorder' />
                    <attribute name='smx_purchaseorderdate' />
                    <attribute name='smx_minimummonthlybilling' />
                    <attribute name='smx_additionaltermwarranty' />
                    <attribute name='smx_annualtargettestcount' />
                    <attribute name='smx_standardcontractcompliance' />
                    <attribute name='smx_orderreason' />
                    <attribute name='smx_mastercontract' />
                    <attribute name='smx_currentcprrate' />
                    <attribute name='smx_bcqmprogram' />
                    <attribute name='smx_automaticbilling' />
                    <attribute name='smx_dontsendemail' />
                    <link-entity name='opportunity' from='opportunityid' to='smx_opportunityid' link-type='outer' alias='opportunity'>
                        <attribute name='opportunityid' />
                        <link-entity name='new_cpq_quote' from='new_opportunityid' to='opportunityid' link-type='outer' alias='new_cpq_quote'>
                            <attribute name='new_terms' />
                            <filter type='and'>
                                <condition attribute='new_isprimary' operator='eq' value='1' />
                            </filter>
                            <link-entity name='new_clm_agreement' from='new_quote_id' to='new_cpq_quoteid' link-type='outer' alias='clm_agreement'>
                                <attribute name='new_name' />
                            </link-entity>
                        </link-entity>
                    </link-entity>
                    <link-entity name='smx_address' from='smx_addressid' to='smx_soldtoaddressid' link-type='outer' alias='smx_soldtoaddress'>
                        <attribute name='smx_salesorganization' />
                        <attribute name='smx_distributionchannel' />
                        <attribute name='smx_sapnumber' />
                    </link-entity>
                    <link-entity name='smx_address' from='smx_addressid' to='smx_lablocationid' link-type='outer' alias='smx_lablocation'>
                        <attribute name='smx_sapnumber' />
                    </link-entity>
                    <link-entity name='smx_address' from='smx_addressid' to='smx_billtoaddressid' link-type='outer' alias='smx_billtoaddress'>
                        <attribute name='smx_sapnumber' />
                    </link-entity>
                    <link-entity name='smx_address' from='smx_addressid' to='smx_payeraddressid' link-type='outer' alias='smx_payeraddress'>
                        <attribute name='smx_sapnumber' />
                    </link-entity>
                    <filter type='and'>
                        <condition attribute='smx_salesorderid' operator='eq' value='{{{salesOrderId}}}' />
                    </filter>
                </entity>
            </fetch>";

                _tracer.Trace("Retrieving Sales Order");
                var salesOrder = _orgService.RetrieveMultiple<smx_salesorder>(new FetchExpression(fetch)).FirstOrDefault();
                if (salesOrder == null)
                {
                    throw new InvalidPluginExecutionException($"Sales Order {salesOrderId} does not exist");
                }

                var lineItems = GetLineItems(salesOrderId);

                return CreateRequest(salesOrder, lineItems);
            }



            private IEnumerable<new_cpq_lineitem_tmp> GetLineItems(Guid salesOrderId)
            {
                var fetch = $@"<fetch>
                  <entity name='new_cpq_lineitem_tmp'>
                    <attribute name='new_name' />
                    <attribute name='new_quantity' />
                    <attribute name='smx_unitofmeasure' />
                    <attribute name='new_baseprice' />
                    <attribute name='new_optionid' />
                    <link-entity name='smx_product' from='smx_productid' to='new_optionid' alias='smx_product'>
                        <filter type='and'>
                            <condition attribute='smx_excludefromcontract' operator='ne' value='1' />
                        </filter>
                    </link-entity>
                    <filter type='and'>
                        <condition attribute='smx_salesorderid' operator='eq' value='{{{salesOrderId}}}'/>
                         </filter>
                   <filter type='and'>
                          <condition attribute='new_producttype' operator='ne' value='Service' />
                           </filter>
                </entity>
            </fetch>";

                return _orgService.RetrieveMultiple<new_cpq_lineitem_tmp>(new FetchExpression(fetch));
            }

            private string RetrieveSAPEndpointURL()
            {
                var fetch = @"
                <fetch top='1'>
                  <entity name='smx_sysmexconfig'>
                    <attribute name='smx_sysmexconfigid' />
                    <attribute name='smx_sapendpointurl' />
                  </entity>
                </fetch>";

                var record = _systemOrgService.RetrieveMultiple<smx_sysmexconfig>(new FetchExpression(fetch)).FirstOrDefault();

                return record != null ? record.smx_SAPEndpointURL : String.Empty;
            }

            private string CreateRequest(smx_salesorder salesOrder, IEnumerable<new_cpq_lineitem_tmp> lineItems)
            {
                var requestHeader = GetRequestHeader(salesOrder);
                var requestLineItems = lineItems.Select(x => GetRequestLineItem(x)).ToArray();

                var contractRequest = new Z_BIZ_BAPI_CONTRACT_CREATE1()
                {
                    CONTRACT_HEADER = requestHeader,
                    CONTRACT_ITEM = requestLineItems

                };





            var serializerRequest = new XmlSerializer(typeof(Z_BIZ_BAPI_CONTRACT_CREATE1));
                string serializedRequest;


            using (var sww = new StringWriter())

            {
                using (XmlWriter writer = XmlWriter.Create(sww))

                {
                    serializerRequest.Serialize(writer, contractRequest);
                    serializedRequest = sww.ToString(); // Your XML
                }
                }

                var sapEndpointUrl = RetrieveSAPEndpointURL();

                if (String.IsNullOrWhiteSpace(sapEndpointUrl))
                {
                    throw new InvalidPluginExecutionException("Configuration record does not exist or SAP Endpoint URL not specified. Please contact an administrator.");
                }


                var client = new HttpClient();
                var content = new StringContent(serializedRequest, Encoding.UTF8, "text/xml");


                var response = client.PostAsync(sapEndpointUrl, content).Result;

                string contractId = String.Empty;
                if (response.IsSuccessStatusCode)
                {
                    //XML received from BAPI
                    var serializerResponse = new XmlSerializer(typeof(Z_BIZ_BAPI_CONTRACT_CREATE1Response));
                    var byteArray = Encoding.UTF8.GetBytes(response.Content.ReadAsStringAsync().Result);

                    var xmlResponse = (Z_BIZ_BAPI_CONTRACT_CREATE1Response)serializerResponse.Deserialize(new MemoryStream(byteArray));

                    //Check XML BAPI response for it's own error
                    if (!String.IsNullOrWhiteSpace(xmlResponse?.RETURN.TYPE) && xmlResponse?.RETURN.TYPE.ToUpper() == "E")
                    {
                        throw new InvalidPluginExecutionException($"Error returned in XML: {xmlResponse?.RETURN.MESSAGE} - Request XML: {serializedRequest}");
                    }

                    contractId = xmlResponse.SALESDOCUMENT;
                    UpdateSalesOrder(salesOrder.Id, contractId, serializedRequest);
                }
                else
                {
                    //BAPI or endpoint threw a hard error
                    throw new InvalidPluginExecutionException($"Error returned from endpoint: {response.Content.ReadAsStringAsync().Result} - Request XML: {serializedRequest}");
                }

                return contractId;
            }

            private void UpdateSalesOrder(Guid salesOrderId, string contractId, string contractXml)
            {
                if (String.IsNullOrWhiteSpace(contractId))
                {
                    return;
                }

                var update = new smx_salesorder() { Id = salesOrderId };
                update.smx_ContractNumber = contractId;
                update.smx_ContractXML = contractXml;

                _orgService.Update(update);
            }

            private ZBAPI_CON_HEADER GetRequestHeader(smx_salesorder salesOrder)
            {
                var requestHeader = new ZBAPI_CON_HEADER()
                {
                    DOC_TYPE = ParseContractType(salesOrder.GetAttributeValue<OptionSetValue>("smx_contracttype")),
                    SALES_ORG = salesOrder.GetAliasedAttributeValue<string>("smx_soldtoaddress.smx_salesorganization"),
                    DISTR_CHAN = "10",
                    SOLD_TO_PARTY = salesOrder.GetAliasedAttributeValue<string>("smx_soldtoaddress.smx_sapnumber"),
                    SHIP_TO_PARTY = salesOrder.GetAliasedAttributeValue<string>("smx_lablocation.smx_sapnumber"),
                    BILL_TO_PARTY = salesOrder.GetAliasedAttributeValue<string>("smx_billtoaddress.smx_sapnumber"),
                    PAYER = salesOrder.GetAliasedAttributeValue<string>("smx_payeraddress.smx_sapnumber"),
                    PURCH_NO_C = salesOrder.GetAttributeValue<string>("smx_purchaseorder"),
                    PURCH_DATE = salesOrder.GetAttributeValue<DateTime?>("smx_purchaseorderdate")?.ToString("yyyyMMdd"),
                    ZMINMBILL = salesOrder.GetAttributeValue<string>("smx_minimummonthlybilling"),
                    ZSTCOM = salesOrder.GetAttributeValue<string>("smx_standardcontractcompliance"),
                    ZSITES = CountOpportunityLabs(salesOrder.GetAliasedAttributeValue<Guid?>("opportunity.opportunityid")),
                    ADD_VAL_DY = salesOrder.GetAttributeValue<string>("smx_additionaltermwarranty"),
                    ZTCOUNT = salesOrder.GetAttributeValue<string>("smx_annualtargettestcount"),
                    ZCONTRACT_TERM = salesOrder.GetAliasedAttributeValue<int?>("new_cpq_quote.new_terms")?.ToString(),
                    ORD_REASON = ParseOrderReason(salesOrder.GetAttributeValue<OptionSetValue>("smx_orderreason")),
                    VBELN_GRP = salesOrder.GetAttributeValue<string>("smx_mastercontract"),
                    ZCRM_ID = salesOrder.GetAliasedAttributeValue<string>("clm_agreement.new_name"),
                    ZKBETR = salesOrder.GetAttributeValue<decimal?>("smx_currentcprrate"),
                    ZCONIN5 = salesOrder.GetAttributeValue<string>("bcqmprogram"),
                    ZCONIN4 = salesOrder.GetAttributeValue<string>("smx_automaticbilling"),
                    ZCONIN6 = salesOrder.GetAttributeValue<string>("smx_dontsendemail")
                };

                return requestHeader;
            }

            private ZBAPI_CON_ITEM GetRequestLineItem(new_cpq_lineitem_tmp lineItem)
            {
                var requestItem = new ZBAPI_CON_ITEM()
                {
                    //ARK 190610 change for itemline to sap
                    LINEITEM_ID = lineItem.new_name,
                    MATERIAL_NUMBER = lineItem.new_optionid?.Name,
                    TARGET_QUANTITY = lineItem.new_quantity,
                    UOM = lineItem.smx_UnitofMeasure,
                    PRICE = lineItem.new_BasePrice?.Value

                };

                return requestItem;
            }

            private string ParseContractType(OptionSetValue contractType)
            {
                if (contractType == null)
                {
                    return null;
                }

                switch (contractType.Value)
                {
                    case (int)smx_contracttype.Purchase:
                        return "ZCON";
                    case (int)smx_contracttype.Lease:
                        return "ZLEA";
                    case (int)smx_contracttype.CPR:
                        return "ZCPR";
                    default:
                        return null;
                }
            }

            private string ParseOrderReason(OptionSetValue orderReason)
            {
                if (orderReason == null)
                {
                    return null;
                }
                var value = orderReason.Value.ToString();

                return value.Substring(value.Length - 3);
            }

            private string CountOpportunityLabs(Guid? opportunityId)
            {
                if (opportunityId == null)
                {
                    return null;
                }

                var fetch = $@"<fetch top='1' aggregate='true'>
                <entity name='smx_opportunitylab'>
					<attribute name='smx_opportunitylabid' alias='opportunitylab_count' aggregate='count' />
                    <filter type='and'>
                        <condition attribute='smx_opportunityid' operator='eq' value='{{{opportunityId}}}' />
                    </filter>
                </entity>
            </fetch>";

                var labs = _orgService.RetrieveMultiple(new FetchExpression(fetch)).Entities.FirstOrDefault();

                return labs.GetAliasedAttributeValue<int?>("opportunitylab_count")?.ToString();
            }

            }
        }
 
   

