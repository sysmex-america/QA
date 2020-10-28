using SonomaPartners.Crm.Toolkit.Plugins;
using System;
using Sysmex.Crm.Model;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace Sysmex.Crm.IntegrationPlugins.Logic 
{
	public class SendImplementationDatesToSAPLogic
	{
		private IOrganizationService orgService;
		private ITracingService trace;

		public SendImplementationDatesToSAPLogic(IOrganizationService orgService, ITracingService trace)
		{
			this.orgService = orgService;
			this.trace = trace;
		}

		public void SendToSAP(DateTime? icnDate, DateTime? goLiveDate, DateTime? serviceDate, DateTime? potentialRevenueDate, string cpqLineItemId, EntityReference implementationId)
		{
			trace.Trace($"CPQ/Apttus Line Item Id: {cpqLineItemId}");

			var contractNumber = GetContractNumber(implementationId);
			if (string.IsNullOrWhiteSpace(contractNumber))
			{
				throw new InvalidPluginExecutionException("Could not find a Contract Number.  Please update the Sales Order record with this data (if available) or contact a System Admin for assistance.");
			}
			trace.Trace($"Contract Number: {contractNumber}");

			var endPoint = RetrieveSAPEndpointURL();
			if (string.IsNullOrWhiteSpace(endPoint))
			{
				throw new InvalidPluginExecutionException("Could not find the SAP Endpoint.  Please contact a System Admin to have them resolve the issue.");
			}
			trace.Trace($"EndPoint: {endPoint}");

			var soap = GetSoap(icnDate, goLiveDate, serviceDate, potentialRevenueDate, cpqLineItemId, contractNumber);
			trace.Trace($"SOAP: {soap}");

			var logRecord = CreateLogRecord(endPoint, soap, cpqLineItemId);
			var logId = logRecord != null ? logRecord.Id.ToString() : "null";
			trace.Trace($"Log Id: {logId}");

			SendSoap(endPoint, soap, logRecord);
		}

		private void SendSoap(string endPoint, string soap, EntityReference logRecord)
		{
			trace.Trace($"Start {nameof(SendSoap)}");

			bool success = false;
			bool crashed = false;
			string resultMessage;
			try
			{
				trace.Trace($"Start Send");
				var client = new HttpClient();
				var content = new StringContent(soap, Encoding.UTF8, "text/xml");

				var result = client.PostAsync(endPoint, content).Result;

				success = result.IsSuccessStatusCode;
				trace.Trace($"Sucess: {success}");

				resultMessage = result.Content.ReadAsStringAsync().Result;
				trace.Trace($"Message: {resultMessage}");
			}
			catch (Exception ex)
			{
				trace.Trace($"Start Error");
				resultMessage = ex.Message;
				trace.Trace($"Error Message: {ex.Message}");

				crashed = true;
				trace.Trace($"End Error");
			}

			UpdateLogWithResponse(logRecord, success, crashed, resultMessage);
			trace.Trace($"End {nameof(SendSoap)}");
		}

		private void UpdateLogWithResponse(EntityReference logRecord, bool success, bool crashed, string resultMessage)
		{
			trace.Trace($"Start {nameof(UpdateLogWithResponse)}");
			if (logRecord != null)
			{
				trace.Trace($"Start Update Log");
				var log = new Entity(smx_ImplementationSAPDatesLog.EntityLogicalName);
				log.Id = logRecord.Id;
				log.Attributes.Add("smx_response", resultMessage);
				log.Attributes.Add("smx_success", GetSuccess(success, crashed));
				orgService.Update(log);
				trace.Trace($"End Update Log");
			}
			trace.Trace($"End {nameof(UpdateLogWithResponse)}");
		}

		private OptionSetValue GetSuccess(bool success, bool crashed)
		{
			trace.Trace($"Start {nameof(GetSuccess)}");
			if (crashed)
			{
				return new OptionSetValue((int)smx_implementationsapdateslog_smx_success.Crashed);
			}
			else if (success)
			{
				return new OptionSetValue((int)smx_implementationsapdateslog_smx_success.Yes);
			}

			return new OptionSetValue((int)smx_implementationsapdateslog_smx_success.No);			
		}

		private EntityReference CreateLogRecord(string endPoint, string soap, string lineItemId)
		{
			trace.Trace($"Start {nameof(CreateLogRecord)}");

			var log = new Entity(smx_ImplementationSAPDatesLog.EntityLogicalName);
			log.Attributes.Add("smx_outboundsoap", soap);
			log.Attributes.Add("smx_outboundendpoint", endPoint);
			log.Attributes.Add("smx_cpqlineitemid", lineItemId);

			var id = orgService.Create(log);
			trace.Trace($"Log Id: {id}");

			trace.Trace($"End {nameof(CreateLogRecord)}");
			return new EntityReference(smx_ImplementationSAPDatesLog.EntityLogicalName, id);
		}

		private string GetSoap(DateTime? icnDate, DateTime? goLiveDate, DateTime? serviceDate, DateTime? potentialRevenueDate, string cpqLineItemId, string contractNumber)
		{
			trace.Trace($"Start {nameof(GetSoap)}");
			return $@"<Z_BIZ_BAPI_CONTRACT_CHANGE xmlns='http://Microsoft.LobServices.Sap/2007/03/Rfc/' xmlns:xsd='http://www.w3.org/2001/XMLSchema' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'>
						<CONTRACT_HEADER>
							<CONTRACT_NUMBER xmlns='http://Microsoft.LobServices.Sap/2007/03/Types/Rfc/'>{contractNumber}</CONTRACT_NUMBER>
							<LINEITEM_ID xmlns='http://Microsoft.LobServices.Sap/2007/03/Types/Rfc/'>{cpqLineItemId}</LINEITEM_ID>
							{ConvertDate(serviceDate, "SERVICE_DATE")}{ConvertDate(potentialRevenueDate, "POTREV_DATE")}{ConvertDate(icnDate,"ICN_DATE")}{ConvertDate(goLiveDate, "GOLIVE_DATE")}
						</CONTRACT_HEADER>
					</Z_BIZ_BAPI_CONTRACT_CHANGE>";
		}

		private string ConvertDate(DateTime? date, string tagName)
		{
			trace.Trace($"Start {nameof(ConvertDate)}");
			if (date.HasValue == false)
			{
				return $"";
			}
			else
			{
				trace.Trace($"Date: {date.Value}");
				return $"<{tagName} xmlns = 'http://Microsoft.LobServices.Sap/2007/03/Types/Rfc/'>{date.Value.ToString("yyyy-MM-dd")}</{tagName}>";
			}
		}

		private string GetContractNumber(EntityReference implementationId)
		{
			trace.Trace("Get Contract number");

			return orgService.Retrieve(implementationId.LogicalName, implementationId.Id, new ColumnSet("smx_contractnumber"))
						.ToEntity<smx_implementation>()
						.smx_ContractNumber;
		}

		private string RetrieveSAPEndpointURL()
		{
			trace.Trace("Get SAP End Point");
			var fetch = @"
                <fetch top='1'>
                  <entity name='smx_sysmexconfig'>
                    <attribute name='smx_implementationdatesendpointurl' />
                    <attribute name='smx_sapendpointurl' />
                  </entity>
                </fetch>";

			var record = orgService.RetrieveMultiple(new FetchExpression(fetch)).Entities.FirstOrDefault();

			return record != null ? record.ToEntity<smx_sysmexconfig>().smx_ImplementationDatesEndpointUrl : String.Empty;
		}
	}
}