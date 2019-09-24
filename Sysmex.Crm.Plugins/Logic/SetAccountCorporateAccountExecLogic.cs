using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Sysmex.Crm.Plugins.Logic
{
    class SetAccountCorporateAccountExecLogic
    {
        private IOrganizationService _orgService;
        private ITracingService _tracer;

        public SetAccountCorporateAccountExecLogic(IOrganizationService orgService, ITracingService tracer)
        {
            _orgService = orgService;
            _tracer = tracer;
        }

        public void UpdateCorporateAccountExec(Entity account, Entity preImage)
        {
            // We want to make sure that when an account is created (or when updated) 
            // we autopopulate account.smx_ihncae with the ownerid field of record that account.smx_ihn is pointing to
            Trace("UpdateCorporateAccountExec");
            var ihnRef = account.GetAttributeValue<EntityReference>("smx_ihn");

            if (preImage != null)
            {
                var preIhnRef = preImage?.GetAttributeValue<EntityReference>("smx_ihn");
                if (preIhnRef != null && ihnRef != null && preIhnRef.Id.Equals(ihnRef.Id))
                {
                    _tracer.Trace("IHN was not updated, returning.");
                    return;
                }
            }

            if (ihnRef == null)
            {
                Trace("IHN cleared, clearing the IHN CAE on the account.");
                account["smx_ihncae"] = null;
            }
            else
            {
                Trace("IHN set to {0}, setting the IHN CAE.", ihnRef.Id);
                var ihn = _orgService.Retrieve(ihnRef.LogicalName, ihnRef.Id, new ColumnSet("ownerid"));
                account["smx_ihncae"] = ihn.GetAttributeValue<EntityReference>("ownerid");
            }
        }

        private void Trace(string message, params object[] args)
        {
            if (_tracer != null)
            {
                _tracer.Trace(message, args);
            }
        }
    }
}
