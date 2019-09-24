using System;
using SonomaPartners.Crm.Toolkit.Plugins;
using Sysmex.Crm.Plugins.Logic;

namespace Sysmex.Crm.Plugins
{
    public class UpdateAccountsLabsOnAddressChangePlugin : PluginBase
    {
        public override void OnExecute(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetPluginExecutionContext();
            var orgService = serviceProvider.CreateOrganizationServiceAsCurrentUser();
            var tracer = serviceProvider.GetTracingService();

            if (context.Depth > 2)
            {
                tracer.Trace("UpdateAccountsLabsOnAddressChangePlugin running twice, exit out.");
                return;
            }

            var address = context.GetPostEntityImage("Target");
            var logic = new SyncLabAccountAddressLogic(orgService, tracer);
            logic.UpdateAssociatedAccountsAndLabs(address);
        }
    }
}
