using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.Linq;
using System;
using SonomaPartners.Crm.Toolkit;
using System.Reflection;
using Microsoft.Xrm.Sdk.Messages;

namespace Sysmex.Crm.Plugins
{
    class DeactivateInstrumentsOnOppWinLogic
    {
        private IOrganizationService _orgService;
        private ITracingService _tracer;

        public DeactivateInstrumentsOnOppWinLogic(IOrganizationService orgService, ITracingService tracer)
        {
            _orgService = orgService;
            _tracer = tracer;
        }

        public void DeactivateInstruments(Entity opportunity)
        {
            _tracer.Trace("Entering DeactivateInstruments Logic.");
            Guid oppId = opportunity.Id;
            var instruments = RetrieveInstrumentUpdates(oppId);
            if (instruments == null)
            {
                _tracer.Trace("no instruments to update. Exiting plugin logic.");
                return;
            }

            ExecuteMultipleRequest batchRequest = new ExecuteMultipleRequest()
            {
                Settings = new ExecuteMultipleSettings()
                {
                    ContinueOnError = false,
                    ReturnResponses = true
                },
                Requests = new OrganizationRequestCollection()
            };

            foreach (Entity instrument in instruments)
            {
                var record = new Entity("smx_instrument")
                {
                    Id = instrument.Id,
                    ["statecode"] = new OptionSetValue(1)
                };
                batchRequest.Requests.Add(new UpdateRequest() { Target = record });
            }
            try
            {
                var updateResponse = _orgService.Execute(batchRequest);
                _tracer.Trace("Update Batch Request Complete.");
            }
            catch (Exception ex)
            {
                _tracer.Trace(ex.Message);
            }
            _tracer.Trace("Exiting Logic.");
        }

        private DataCollection<Entity> RetrieveInstrumentUpdates(Guid oppId)
        {

            var fetch = new FetchExpression($@"
                <fetch>
                    <entity name='smx_instrument'>
                        <attribute name='smx_instrumentid' />
                            <filter type='and'>
                                <condition attribute = 'statecode' operator= 'eq' value = '0'/>
                            </filter>
                        <link-entity name='smx_instrumentupdate' from='smx_instrumenttoreplaceid' to='smx_instrumentid' link-type='inner' alias='aa'>
      						<link-entity name='smx_opportunitylab' from='smx_opportunitylabid' to='smx_opportunitylabid' link-type='inner' alias='ab'>
						        <link-entity name='opportunity' from='opportunityid' to='smx_opportunityid' link-type='inner' alias='ac'>
						          <filter type='and'>
						            <condition attribute='opportunityid' operator='eq' value='{oppId}' />
                                  </filter>
						        </link-entity>
						    </link-entity>
						</link-entity>
                    </entity>
                </fetch>");
            var result = _orgService.RetrieveMultiple(fetch);

            if (result.Entities.Any())
            {
                return result.Entities;
            }
            else
            {
                return null;
            }
        }
    }
}