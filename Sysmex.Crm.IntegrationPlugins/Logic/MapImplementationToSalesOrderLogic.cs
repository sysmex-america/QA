using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Sysmex.Crm.Model;
using System;
using System.Linq;

namespace Sysmex.Crm.IntegrationPlugins.Logic
{
    public class MapImplementationToSalesOrderLogic
    {
        private IOrganizationService _orgService;
        private ITracingService _trace;

        public MapImplementationToSalesOrderLogic(IOrganizationService orgService, ITracingService trace)
        {
            _orgService = orgService;
            _trace = trace;
        }

        public void UpdateLab(smx_implementation implementation)
        {
            _trace.Trace("** BEGIN UpdateLab **");

            var labGuids = RetrieveSalesOrderLabGuids(implementation.Id, implementation.smx_LabLocationId);
            
            foreach(Entity record in labGuids.Entities)
            {
                var lab = new smx_lab();
				var update = false;
                lab.Id = (Guid)record.GetAttributeValue<Microsoft.Xrm.Sdk.AliasedValue>("lab.smx_labid").Value;
                _trace.Trace($"* update lab {lab.Id} *");
				if (implementation.smx_WAMSite != null)
				{
					update = true;
					lab.smx_WAMSite = implementation.smx_WAMSite;
				}
				if (implementation.smx_WAMConnects != null)
				{
					update = true;
					lab.smx_WAMConnects = implementation.smx_WAMConnects;
				}

				if (update)
				{
					_orgService.Update(lab.ToEntity<Entity>());
				}
            }

            _trace.Trace("** END UpdateLab **");
        }

        private EntityCollection RetrieveSalesOrderLabGuids(Guid implementationGuid, EntityReference labLocation)
        {
            _trace.Trace("** BEGIN RetrieveSalesOrderLab method **");
			var addressJoinField = "smx_instrumentshiptoid";
			if (labLocation != null)
			{
				addressJoinField = "smx_lablocationid";
			}

            var fetchXml = $@"
                    <fetch top='50' >
                        <entity name='smx_implementation' >
                            <filter>
                                <condition attribute='smx_implementationid' operator='eq' value='{implementationGuid}' />
                            </filter>
                            <link-entity name='smx_address' from='smx_addressid' to='{addressJoinField}' link-type='inner' alias='aa'>
								<link-entity name='smx_lab' from='smx_labid' to='smx_lab' link-type='inner' alias='lab'>
									<attribute name='smx_labid' />
									<filter type='and'>
										<condition attribute='statecode' operator='eq' value='{(int)smx_labState.Active}' />
									</filter>
								</link-entity>
							</link-entity>
                        </entity>
                    </fetch>";

            var result = _orgService.RetrieveMultiple(new FetchExpression(fetchXml));

            _trace.Trace("** END/RETURN RetrieveSalesOrderLab method **");
            return result;
        }
    }
}