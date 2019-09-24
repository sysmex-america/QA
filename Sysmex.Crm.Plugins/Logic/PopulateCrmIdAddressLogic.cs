using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.Linq;
using System;
using SonomaPartners.Crm.Toolkit;
using System.Reflection;
using Microsoft.Xrm.Sdk.Messages;

namespace Sysmex.Crm.Plugins
{
    class PopulateCrmIdAddressLogic
    {
        private IOrganizationService _orgService;
        private ITracingService _tracer;

        public PopulateCrmIdAddressLogic(IOrganizationService orgService, ITracingService tracer)
        {
            _orgService = orgService;
            _tracer = tracer;
        }

        public void PopulateCrmIdField(Entity address)
        {
            _tracer.Trace("Entering PopulateCrmIdField Logic.");

            Guid smx_addId = address.Id;

            _tracer.Trace($"Populating Crm Id field with {smx_addId}.");

            address["smx_crmid"] = smx_addId.ToString();

            _tracer.Trace("Exiting Logic.");
        }
    }
}
