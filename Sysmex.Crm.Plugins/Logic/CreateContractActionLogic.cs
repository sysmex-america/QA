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
                    <link-entity name='smx_address' from='smx_addressid' to='smx_instrumentshiptoidid' link-type='outer' alias='smx_instrumentshipto'>
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

            var lineItems = GetLineItems(salesOrder);

            return CreateRequest(salesOrder, lineItems);
        }


        
        private IEnumerable<new_cpq_lineitem_tmp> GetLineItems(smx_salesorder salesOrder)
        {
			//Added by Yash on 24-08-2020--Ticket No 57839---><condition attribute='new_producttype' operator='ne' value='Reagents' />
			var fetch = $@"<fetch>
                  <entity name='new_cpq_lineitem_tmp'>
                    <attribute name='new_name' />
                    <attribute name='new_quantity' />
                    <attribute name='smx_unitofmeasure' />
                    <attribute name='new_baseprice' />
                    <attribute name='new_optionid' />
                    <attribute name='smx_ncmonths' />
                    <attribute name='smx_combinedwarranty' />
					<link-entity name='smx_product' from='smx_productid' to='new_optionid' alias='smx_product'>
                        <filter type='and'>
                            <condition attribute='smx_excludefromcontract' operator='ne' value='1' />
						</filter>
                    </link-entity>
                    <filter type='and'>
                        <condition attribute='smx_salesorderid' operator='eq' value='{{{salesOrder.Id}}}'/>
                        <condition attribute='new_producttype' operator='ne' value='Reagents' />
                    </filter>
                   <filter type='and'>
                          <condition attribute='new_producttype' operator='ne' value='Service' />
                          {GetProductTypeFiltersByOrderReason(salesOrder.smx_OrderReason)}
                           </filter>
                </entity>
            </fetch>";

            return _orgService.RetrieveMultiple<new_cpq_lineitem_tmp>(new FetchExpression(fetch));
        }

        string GetProductTypeFiltersByOrderReason(OptionSetValue orderReason)
        {
            if (orderReason != null)
            {
                switch ((smx_orderreason)orderReason.Value)
                {
                case smx_orderreason.COPurchaseAInstrumentOnly:
                case smx_orderreason.COPurchaseBInstrmtOptService:
                case smx_orderreason.COLeaseAInstrumentOnly:
                case smx_orderreason.COLeaseBInstrumentService:
                case smx_orderreason.COPurchasedInstCPRSR:
                case smx_orderreason.COPurchasedInstCPRR:
                case smx_orderreason.COLeaseACPRSR:
                case smx_orderreason.COLeaseBCPRR:
                case smx_orderreason.COLabPurchaseAInstrumentOnly:
                case smx_orderreason.COLABPurchaseBInstrmtOptService:
                case smx_orderreason.COPurchaseADistributor:
                case smx_orderreason.COPurchaseBDistributor:
                case smx_orderreason.CODonation:
                    return @"<condition attribute='new_producttype' operator='ne' value='Reagents' />
                                 <condition attribute='new_producttype' operator='ne' value='Consumables' />";
                }
            }

            return string.Empty;
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
			//Added by Yash on 28-10-2020--Ticket No 58798
			//_orgService.Update(update);

			//Added by Yash on 13-08-2020--Ticket No 57589
			Entity saleorderDetails = GetSaleOrderDetails(salesOrderId);
			AliasedValue opportunityLabLab = saleorderDetails.Contains("OpportunityLab.smx_labid") ? saleorderDetails.GetAttributeValue<AliasedValue>("OpportunityLab.smx_labid") : null;
			//Added by Yash on 28-10-2020--Ticket No 58798
			if(opportunityLabLab!=null)
			{
				EntityReference labDetails = (EntityReference)opportunityLabLab.Value;
				Entity labEntity = _orgService.Retrieve(labDetails.LogicalName, labDetails.Id, new ColumnSet("smx_casprimary"));
				if(labEntity.Contains("smx_casprimary"))
				{
					string labCASPrimary = labEntity.Contains("smx_casprimary") ? labEntity.GetAttributeValue<string>("smx_casprimary") : string.Empty;
					if(labCASPrimary!=string.Empty)
					{
						Entity user = GetUserfromCASPrimery(labCASPrimary);
						if(user!=null)
						{
							update["smx_cas"] = new EntityReference(user.LogicalName, user.Id);
						}
					}
					
				}
			}
			_orgService.Update(update);
			//End
			AliasedValue sapNumber = saleorderDetails.Contains("InstrumentShipTo.smx_sapnumber") ? saleorderDetails.GetAttributeValue<AliasedValue>("InstrumentShipTo.smx_sapnumber") : null;
			if (opportunityLabLab != null && sapNumber != null)
			{
				UpdateLabandLabAddressSapNumber((EntityReference)opportunityLabLab.Value, sapNumber.Value.ToString(), "smx_sapid",saleorderDetails);
				Entity labAddress = _orgService.Retrieve(((EntityReference)opportunityLabLab.Value).LogicalName, ((EntityReference)opportunityLabLab.Value).Id, new ColumnSet("smx_labaddress"));
				if (labAddress.Contains("smx_labaddress"))
					UpdateLabandLabAddressSapNumber((EntityReference)labAddress.Attributes["smx_labaddress"], sapNumber.Value.ToString(), "smx_sapnumber",saleorderDetails);
				//Added by Yash on 27-08-2020--Ticket No 57589
				AliasedValue opportunityLabAccount = saleorderDetails.Contains("OpportunityLab.smx_accountid") ? saleorderDetails.GetAttributeValue<AliasedValue>("OpportunityLab.smx_accountid") : null;
				if(opportunityLabAccount!=null && opportunityLabLab!=null)
				{
					EntityReference instrumentShipTo = saleorderDetails.GetAttributeValue<EntityReference>("smx_instrumentshiptoidid");
					UpdateSoldToandShipToAddresses(instrumentShipTo,(EntityReference) opportunityLabAccount.Value, (EntityReference)opportunityLabLab.Value, "shipto");
				}
				if(opportunityLabAccount!=null)
				{
					EntityReference soldToAddress = saleorderDetails.Contains("smx_soldtoaddressid") ? saleorderDetails.GetAttributeValue<EntityReference>("smx_soldtoaddressid") : null;
					if(soldToAddress!=null)
						UpdateSoldToandShipToAddresses(soldToAddress, (EntityReference)opportunityLabAccount.Value,null, "soldto");
				}
			}
			//End
			

		}

		private ZBAPI_CON_HEADER GetRequestHeader(smx_salesorder salesOrder)
        {
            var requestHeader = new ZBAPI_CON_HEADER()
            {
                DOC_TYPE = ParseContractType(salesOrder.GetAttributeValue<OptionSetValue>("smx_contracttype")),
                SALES_ORG = salesOrder.GetAliasedAttributeValue<string>("smx_soldtoaddress.smx_salesorganization"),
                DISTR_CHAN = "10",
                SOLD_TO_PARTY = salesOrder.GetAliasedAttributeValue<string>("smx_soldtoaddress.smx_sapnumber"),
                SHIP_TO_PARTY = salesOrder.GetAliasedAttributeValue<string>("smx_instrumentshipto.smx_sapnumber"),
                BILL_TO_PARTY = salesOrder.GetAliasedAttributeValue<string>("smx_billtoaddress.smx_sapnumber"),
                PAYER = salesOrder.GetAliasedAttributeValue<string>("smx_payeraddress.smx_sapnumber"),
                PURCH_NO_C = salesOrder.GetAttributeValue<string>("smx_purchaseorder"),
                PURCH_DATE = salesOrder.GetAttributeValue<DateTime?>("smx_purchaseorderdate")?.ToString("yyyy-MM-dd"),
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
                PRICE = lineItem.new_BasePrice?.Value,
				//ADD_VAL_DY = lineItem.Attributes.Contains("smx_ncmonths")?(lineItem.GetAttributeValue<int>("smx_ncmonths")).ToString():string.Empty  //Added by Yash on 30-06-2020 ticket id:57130   
				ADD_VAL_DY = lineItem.Attributes.Contains("smx_combinedwarranty") ?(lineItem.GetAttributeValue<int>("smx_combinedwarranty")).ToString():string.Empty  //Added by Yash on 23-09-2020 ticket id:57130 

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
		//Added by Yash on 13-08-2020--Ticket No 57589
		//Added by Yash on 19-10-2020--Ticket No 58434
		private Entity GetSaleOrderDetails(Guid saleOrderId)
		{
			Entity saleOrder = null;
			try
			{
				var fetch = $@"<fetch>
                             <entity name='smx_salesorder'>
                             <attribute name='smx_salesorderid' />
                             <attribute name='smx_name' />
                             <attribute name='smx_opportunitylabid' />
                             <attribute name='smx_instrumentshiptoidid' />
                             <attribute name='smx_wamsite' />
                             <attribute name='smx_wamconnects' />
                             <attribute name='smx_soldtoaddressid' />
							  <filter type='and'>
                                <condition attribute='smx_salesorderid' operator='eq' value='{saleOrderId}' />
                              </filter>
                           <link-entity name='smx_opportunitylab' from='smx_opportunitylabid' to='smx_opportunitylabid' link-type='outer' alias='OpportunityLab'>
                             <attribute name='smx_labid' />
                             <attribute name='smx_accountid' />
						   </link-entity>
                          <link-entity name='smx_address' from='smx_addressid' to='smx_instrumentshiptoidid' link-type='outer' alias='InstrumentShipTo'>
                            <attribute name='smx_sapnumber' />
                         </link-entity>
                        </entity>
                       </fetch>";
				EntityCollection ecSaleorders = _orgService.RetrieveMultiple(new FetchExpression(fetch));
				if (ecSaleorders.Entities.Count() > 0)
				{
					_tracer.Trace("Saleorders Count" + ecSaleorders.Entities.Count());
					saleOrder = ecSaleorders.Entities.FirstOrDefault();
				}
			}
			catch (Exception ex)
			{
				return saleOrder;
			}
			return saleOrder;
		}

		private void UpdateLabandLabAddressSapNumber(EntityReference labandlabAddress, string sapNumber, string sapNumberFieldName, Entity saleOrderDetails)
		{
			_tracer.Trace("Entered UpdateLabandLabAddressSapNumber Method");
			Entity enLabandlabAddress = new Entity(labandlabAddress.LogicalName, labandlabAddress.Id);
			enLabandlabAddress.Attributes.Add(sapNumberFieldName, sapNumber);
			//Added by Yash on 08-10-2020--Ticket No 58434
			if (saleOrderDetails.Contains("smx_wamsite") && labandlabAddress.LogicalName == "smx_lab")
				enLabandlabAddress.Attributes.Add("smx_wamsite", saleOrderDetails.GetAttributeValue<OptionSetValue>("smx_wamsite"));
			if (saleOrderDetails.Contains("smx_wamconnects") && labandlabAddress.LogicalName == "smx_lab")
				enLabandlabAddress.Attributes.Add("smx_wamconnects", saleOrderDetails.GetAttributeValue<OptionSetValue>("smx_wamconnects"));
			//End
			_orgService.Update(enLabandlabAddress);
			_tracer.Trace("Lab or Lab Address Updated");
		}
		//End
		//Added by Yash on 27-08-2020--Ticket No 57589
		private void UpdateSoldToandShipToAddresses(EntityReference SoldToandShipTo, EntityReference account,EntityReference lab,string SoldToorShipTo)
		{
			_tracer.Trace("Entered UpdateSoldToandShipToAddresses Method");
			Entity enSoldToandShipTo = new Entity(SoldToandShipTo.LogicalName, SoldToandShipTo.Id);
			if (SoldToorShipTo == "soldto")
				enSoldToandShipTo.Attributes.Add("smx_account", new EntityReference(account.LogicalName, account.Id));
			else if (SoldToorShipTo == "shipto")
			{
				enSoldToandShipTo.Attributes.Add("smx_account", new EntityReference(account.LogicalName, account.Id));
				enSoldToandShipTo.Attributes.Add("smx_lab", new EntityReference(lab.LogicalName, lab.Id));
			}
			_orgService.Update(enSoldToandShipTo);
			_tracer.Trace("ShipTo or SoldTo Address Updated");
		}

		//End
		//Added by Yash on 28-10-2020--Ticket No 57589
		private Entity GetUserfromCASPrimery(string CASPrimary)
		{
			Entity user = null;
			string userName = "%" + CASPrimary + "%";
			try
			{
				var fetch = $@"<fetch>
                             <entity name='systemuser'>
                               <attribute name='fullname' />
                               <attribute name='systemuserid' />
                               <attribute name='domainname' />
                                  <filter type='and'>
                                       <condition attribute='domainname' operator='like' value='{userName}' />
                                  </filter>
                             </entity>
                       </fetch>";
				EntityCollection ecUsers = _orgService.RetrieveMultiple(new FetchExpression(fetch));
				if (ecUsers.Entities.Count() > 0)
				{
					_tracer.Trace("users Count" + ecUsers.Entities.Count());
					user = ecUsers.Entities.FirstOrDefault();
				}
			}
			catch (Exception ex)
			{
				_tracer.Trace("users not found");
				return user;
			}
			return user;

		}
		//End
	}
}



