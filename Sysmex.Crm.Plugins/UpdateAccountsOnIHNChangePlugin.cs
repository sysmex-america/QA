using System;
using Microsoft.Xrm.Sdk;
using SonomaPartners.Crm.Toolkit.Plugins;
using Sysmex.Crm.Plugins.Logic;

namespace Sysmex.Crm.Plugins
{
    public class UpdateAccountsOnIHNChangePlugin : PluginBase
    {
        public override void OnExecute(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetPluginExecutionContext();
            var orgService = serviceProvider.CreateOrganizationServiceAsCurrentUser();
            var tracer = serviceProvider.GetTracingService();

            //Account owner has changed, check if account type = IHN and update all accounts assocated to this IHN account
            tracer.Trace("Starting UpdateAccountsOnIHNChangeLogic");
            if (!context.PostEntityImages.Contains("PostImage"))
            {
                return;
            }

            var account = context.GetPostEntityImage("PostImage");
            tracer.Trace("Using Post Image");
            
            if (account.Attributes.Contains("smx_accounttype") &&
                (account.GetAttributeValue<OptionSetValue>("smx_accounttype").Value) == 180700005) //Account Type = IHN
            {
                var logic = new UpdateAccountsOnIHNChangeLogic(orgService, tracer);
                logic.UpdateAssociatedAccounts(account);
            }

            tracer.Trace("End of UpdateAccountsOnIHNChangeLogic");
        }
    }
}
