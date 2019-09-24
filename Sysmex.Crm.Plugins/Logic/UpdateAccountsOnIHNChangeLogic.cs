using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;

namespace Sysmex.Crm.Plugins.Logic
{
    class UpdateAccountsOnIHNChangeLogic
    {
        private IOrganizationService _orgService;
        private ITracingService _tracer;

        public UpdateAccountsOnIHNChangeLogic(IOrganizationService orgService, ITracingService tracer)
        {
            _orgService = orgService;
            _tracer = tracer;
        }

        public void UpdateAssociatedAccounts(Entity accountihn)
        {
            // We want to find any accounts who's smx_ihn == target ihn, and update their smx_ihncae field to that of the ihn.ownerid field
            Trace("Retrieve accounts associated with IHN");
            var accounts = RetrieveAccountsAssociatedToIHN(accountihn);
            var newAccountExec = accountihn.GetAttributeValue<EntityReference>("ownerid");

            Trace("Updating {0} accounts.", accounts.Count());
            if (!accounts.Any()) { return; }

            var accountUpdates = new ExecuteMultipleRequest
            {
                Requests = new OrganizationRequestCollection(),
                Settings = new ExecuteMultipleSettings
                {
                    ContinueOnError = false,
                    ReturnResponses = false
                }
            };

            foreach (var account in accounts)
            {
                if (accountUpdates.Requests.Count == 1000)
                {
                    Trace("Hit max amount of possible requests in a single ExecuteMultiple for Accounts. Exeucting and clearing.");
                    _orgService.Execute(accountUpdates);
                    accountUpdates.Requests.Clear();
                }

                var updatedAccount = new Entity(account.LogicalName, account.Id);
                updatedAccount["smx_ihncae"] = newAccountExec;

                var updateRequest = new UpdateRequest()
                {
                    Target = updatedAccount
                };

                accountUpdates.Requests.Add(updateRequest);
            }
            _orgService.Execute(accountUpdates);
        }

        private IEnumerable<Entity> RetrieveAccountsAssociatedToIHN(Entity accountihn)
        {
            Trace("RetrieveAccountsAssociatedToIHN");

            var query = new QueryExpression("account");
            query.Criteria.Conditions.Add(new ConditionExpression("statecode", ConditionOperator.Equal, 0));
            query.Criteria.Conditions.Add(new ConditionExpression("smx_accountihn", ConditionOperator.Equal, accountihn.Id));
            query.ColumnSet.AddColumns("accountid");

            return _orgService.RetrieveMultiple(query).Entities;
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
