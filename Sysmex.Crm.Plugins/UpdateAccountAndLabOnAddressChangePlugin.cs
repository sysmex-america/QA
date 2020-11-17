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

            string businessUnitName = string.Empty;

            if (context.Depth > 2)
            {
                tracer.Trace("UpdateAccountsLabsOnAddressChangePlugin running twice, exit out.");
                return;
            }

            var address = context.GetPostEntityImage("Target");
            var logic = new SyncLabAccountAddressLogic(orgService, tracer);

            //Added by Yash on 19-06-2020 
            businessUnitName = logic.getUserBusinessUnit(context.InitiatingUserId);
            tracer.Trace("Business Unit Name " + businessUnitName);
            

            logic.UpdateAssociatedAccountsAndLabs(address, businessUnitName);
        }
    }
}
