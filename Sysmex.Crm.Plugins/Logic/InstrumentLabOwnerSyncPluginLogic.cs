using Microsoft.Xrm.Sdk;
using Sysmex.Crm.Model;
using Microsoft.Xrm.Sdk.Query;

namespace Sysmex.Crm.Plugins.Logic
{
   public class InstrumentLabOwnerSyncPluginLogic
   {
      private IPluginExecutionContext context;
      private IOrganizationService orgService;
      private ITracingService trace;

      public InstrumentLabOwnerSyncPluginLogic(IOrganizationService orgService, ITracingService trace)
      {
         this.orgService = orgService;
         this.trace = trace;
      }

      public void SyncOwnerToLab(smx_instrument instrument)
      {
         if(instrument.smx_Lab == null)
         {
            trace.Trace("Lab is null on Instrument, returning");
            return;
         }
         var labId = instrument.smx_Lab.Id;
         smx_lab lab = orgService.Retrieve("smx_lab", labId, new ColumnSet("ownerid")).ToEntity<smx_lab>();
         if(instrument.OwnerId.Id != lab.OwnerId.Id)
         {
            instrument.OwnerId = lab.OwnerId;
            orgService.Update(instrument);
         }
      }
   }
}
