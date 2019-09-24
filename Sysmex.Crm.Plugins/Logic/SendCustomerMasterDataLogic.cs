using Microsoft.Xrm.Sdk;

namespace Sysmex.Crm.Plugins.Logic
{
    class SendCustomerMasterDataLogic
    {
        private IOrganizationService _orgService;
        private ITracingService _trace;

        public SendCustomerMasterDataLogic(IOrganizationService orgService, ITracingService trace)
        {
            _orgService = orgService;
            _trace = trace;
        }
    }
}
