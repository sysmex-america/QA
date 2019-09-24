using Microsoft.Xrm.Sdk;
using SonomaPartners.Crm.Toolkit.Plugins;
using Sysmex.Crm.Model;
using Sysmex.Crm.Plugins.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sysmex.Crm.Plugins
{
   public class InstrumentLabOwnerSyncPlugin : PluginBase
   {
      public override void OnExecute(IServiceProvider serviceProvider)
      {
         var context = serviceProvider.GetPluginExecutionContext();
         var orgService = serviceProvider.CreateOrganizationServiceAsCurrentUser();
         var trace = serviceProvider.GetTracingService();

         var entity = context.GetPostEntityImage<smx_instrument>("PostImage");
         
         var logic = new InstrumentLabOwnerSyncPluginLogic(orgService, trace);
         logic.SyncOwnerToLab(entity);
      }
   }
}
