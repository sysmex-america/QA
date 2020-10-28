using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sysmex.Crm.IntegrationPlugins.Logic
{
    public class GetWorkOrderFieldMappingActionLogic
    {
        private IOrganizationService _orgService;
        private ITracingService _trace;

		private const string _shipToAlias = "ImplementationShipTo";
		private const string _equipmentLocationAlias = "EquipmentLocation";


		public GetWorkOrderFieldMappingActionLogic(IOrganizationService orgService, ITracingService trace)
        {
            _orgService = orgService;
            _trace = trace;
        }

        public WorkOrderFieldMappingOutput GetWorkOrderFieldMapping(Guid implementationProductGuid)
        {
            _trace.Trace("* BEGIN GetWorkOrderFieldMapping method *");

            Entity implementationInfo = RetrieveImplementationInfo(implementationProductGuid);
			return SetWorkOrderFieldMapping(implementationInfo);
        }

		public WorkOrderFieldMappingOutput SetWorkOrderFieldMapping(Entity implementation)
		{
			_trace.Trace("* BEGIN SetWorkOrderFieldMapping method *");

			var addressAlias = "";
			if (implementation.Contains($"{_equipmentLocationAlias}.smx_addressid"))
			{
				addressAlias = _equipmentLocationAlias;
			}
			else if (implementation.Contains($"{_shipToAlias}.smx_addressid"))
			{
				addressAlias = _shipToAlias;
			}

			WorkOrderFieldMappingOutput.ImplementationSoldTo implementatonSoldTo = new WorkOrderFieldMappingOutput.ImplementationSoldTo();
			implementatonSoldTo.smx_account = getEntityReference(implementation, $"ImplementationSoldTo.smx_account");

			WorkOrderFieldMappingOutput.ImplementationShipTo implementationShipTo = new WorkOrderFieldMappingOutput.ImplementationShipTo();
			implementationShipTo.smx_lab = getEntityReference(implementation, $"{addressAlias}.smx_lab");
			implementationShipTo.smx_account = getEntityReference(implementation, $"{addressAlias}.smx_account");
			implementationShipTo.smx_addressstreet1 = getAttributeValue(implementation, $"{addressAlias}.smx_addressstreet1");
			implementationShipTo.smx_addressstreet2 = getAttributeValue(implementation, $"{addressAlias}.smx_addressstreet2");
			implementationShipTo.smx_city = getAttributeValue(implementation, $"{addressAlias}.smx_city");
			implementationShipTo.smx_zippostalcode = getAttributeValue(implementation, $"{addressAlias}.smx_zippostalcode");
			WorkOrderFieldMappingOutput.State state = new WorkOrderFieldMappingOutput.State();
			state.smx_name = getAttributeValue(implementation, $"{addressAlias}.State.smx_name");
			implementationShipTo.smx_statesap = state;
			var countryName = implementation.GetAttributeValue<AliasedValue>($"{addressAlias}.smx_countrysap") != null ? (EntityReference)implementation.GetAttributeValue<AliasedValue>($"{addressAlias}.smx_countrysap").Value: null;     
			implementationShipTo.smx_countrysap = countryName != null ? countryName.Name : "";

			_trace.Trace("setting up object WorkOrderFieldMappingOutput");
			WorkOrderFieldMappingOutput workOrderFieldMappingOutput = new WorkOrderFieldMappingOutput();
			workOrderFieldMappingOutput.smx_instrumentshiptoid = implementationShipTo;
			workOrderFieldMappingOutput.smx_soldtoid = implementatonSoldTo;
			workOrderFieldMappingOutput.smx_sapshiptonumber = getAttributeValue(implementation, "Implementation.smx_sapshiptonumber");
			workOrderFieldMappingOutput.smx_projectmanagerid = (EntityReference)(implementation.GetAttributeValue<AliasedValue>("Implementation.smx_projectmanagerid"))?.Value;

			_trace.Trace("* END/RETURN SetWorkOrderFieldMapping method *");
			return workOrderFieldMappingOutput;
		}

        private EntityReference getEntityReference(Entity implementationInfo, string attribute)
        {
            _trace.Trace($"retrieving entity reference for {attribute}");
            return (EntityReference)(implementationInfo.GetAttributeValue<AliasedValue>(attribute) != null ? implementationInfo.GetAttributeValue<AliasedValue>(attribute).Value : null);
        }

        private string getAttributeValue(Entity implementationInfo, string attribute)
        {
            _trace.Trace($"retrieving attribute value for {attribute}");
            return implementationInfo.GetAttributeValue<AliasedValue>(attribute) != null ? implementationInfo.GetAttributeValue<AliasedValue>(attribute).Value?.ToString() : "";
        }

        private Entity RetrieveImplementationInfo(Guid implementationProductGuid)
        {
            _trace.Trace("* BEGIN RetrieveImplementationInfo method *");

            var fetchXml = $@"
                <fetch>
                  <entity name='smx_implementationproduct' >
                    <filter>
                      <condition attribute='smx_implementationproductid' operator='eq' value='{implementationProductGuid}' />
                    </filter>
                    <link-entity name='smx_implementation' from='smx_implementationid' to='smx_implementationid' link-type='outer' alias='Implementation' >
                      <attribute name='smx_sapshiptonumber' />
					  <attribute name='smx_projectmanagerid' />
                      <link-entity name='smx_address' from='smx_addressid' to='smx_instrumentshiptoid' link-type='outer' alias='{_shipToAlias}' >
                        <attribute name='smx_addressid' />
						<attribute name='smx_lab' />
                        <attribute name='smx_account' />
                        <attribute name='smx_addressstreet1' />
                        <attribute name='smx_addressstreet2' />
                        <attribute name='smx_city' />
                        <attribute name='smx_zippostalcode' />
                        <attribute name='smx_countrysap' />						
                        <link-entity name='smx_state' from='smx_stateid' to='smx_statesap' link-type='outer' alias='{_shipToAlias}.State' >
                            <attribute name='smx_name' />
                        </link-entity>
                       </link-entity>
					   <link-entity name='smx_address' from='smx_addressid' to='smx_lablocationid' link-type='outer' alias='{_equipmentLocationAlias}' >
                        <attribute name='smx_addressid' />
						<attribute name='smx_lab' />
                        <attribute name='smx_account' />
                        <attribute name='smx_addressstreet1' />
                        <attribute name='smx_addressstreet2' />
                        <attribute name='smx_city' />
                        <attribute name='smx_zippostalcode' />
                        <attribute name='smx_countrysap' />						
                        <link-entity name='smx_state' from='smx_stateid' to='smx_statesap' link-type='outer' alias='{_equipmentLocationAlias}.State' >
                            <attribute name='smx_name' />
                        </link-entity>
                       </link-entity>
                      <link-entity name='smx_address' from='smx_addressid' to='smx_soldtoid' link-type='outer' alias='ImplementationSoldTo' >
                        <attribute name='smx_account' />
                      </link-entity>
                    </link-entity>
                  </entity>
                </fetch>";

            var result = _orgService.RetrieveMultiple(new FetchExpression(fetchXml));

            _trace.Trace("* END/RETURN RetrieveImplementationInfo method *");
            return result.Entities[0];
        }
    }

    public class WorkOrderFieldMappingOutput
    {
        public string smx_sapshiptonumber { get; set; }
        public ImplementationShipTo smx_instrumentshiptoid { get; set; }
        public ImplementationSoldTo smx_soldtoid { get; set; }
		public EntityReference smx_projectmanagerid { get; set; }

        public class ImplementationShipTo
        {
            public EntityReference smx_lab { get; set; }
            public EntityReference smx_account { get; set; }
            public string smx_addressstreet1 { get; set; }
            public string smx_addressstreet2 { get; set; }
            public string smx_city { get; set; }
            public string smx_zippostalcode { get; set; }
            public State smx_statesap { get; set; }
            public string smx_countrysap { get; set; }
        }

        public class ImplementationSoldTo
        {
            public EntityReference smx_account { get; set; }
        }

        public class State
        {
            public string smx_name { get; set; }
        }
    }
}
