using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Sysmex.Crm.Plugins.Logic
{
    public class AssosciateDemoEvalAddressLogic
    {
        private readonly IOrganizationService _orgService;
        private readonly ITracingService _trace;

        // { smx_address, smx_demoeval }
        private readonly Dictionary<string, string> AddressMappings = new Dictionary<string, string>
        {
            { "smx_addressstreet1", "smx_street1" },
            { "smx_addressstreet2", "smx_street2" },
            { "smx_addressstreet3", "smx_street3" },
            { "smx_city", "smx_city" },
            { "smx_statesap", "smx_statesap" },
            { "smx_zippostalcode", "smx_zippostalcode" },
            { "smx_countrysap", "smx_countrysap" },
        };

        public AssosciateDemoEvalAddressLogic(IOrganizationService orgService, ITracingService trace)
        {
            _orgService = orgService;
            _trace = trace;
        }

        public void CopyAddressFields(Entity demoEval)
        {
            _trace.Trace($"Entered: {MethodBase.GetCurrentMethod().Name}");

            var addressRef = demoEval.GetAttributeValue<EntityReference>("smx_address");
            if (addressRef == null)
            {
                _trace.Trace("Address field was not populated, returning.");
                return;
            }

            var address = _orgService.Retrieve(addressRef.LogicalName, addressRef.Id, new ColumnSet(AddressMappings.Keys.ToArray()));
            foreach(var mapping in AddressMappings)
            {
                if (!address.Contains(mapping.Key))
                {
                    continue;
                }

                demoEval[mapping.Value] = address[mapping.Key]; 
            }
        }
    }
}
