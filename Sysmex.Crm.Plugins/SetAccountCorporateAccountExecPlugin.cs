using System;
using System.Linq;
using Microsoft.Xrm.Sdk;
using SonomaPartners.Crm.Toolkit.Plugins;
using Sysmex.Crm.Plugins.Logic;

namespace Sysmex.Crm.Plugins
{
    public class SetAccountCorporateAccountExecPlugin : PluginBase
    {
        public override void OnExecute(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetPluginExecutionContext();
            var orgService = serviceProvider.CreateOrganizationServiceAsCurrentUser();
            var tracer = serviceProvider.GetTracingService();

            var account = context.GetTargetEntity();

            // Kickout if Account Type = GPO or IHN            
            if (account.Attributes.Contains("smx_accounttype") &&
                new[] { 180700004, 180700005 }.Contains(account.GetAttributeValue<OptionSetValue>("smx_accounttype").Value))
            {
                tracer.Trace("Account type  = GPO or IHN");
                return;
            }

            var preImage = context.PreEntityImages.Contains("PreImage") ? context.PreEntityImages["PreImage"] : null;
            var logic = new SetAccountCorporateAccountExecLogic(orgService, tracer);
            tracer.Trace("Run Update Corporate Account Exec");
            logic.UpdateCorporateAccountExec(account, preImage);
        }
    }
}
