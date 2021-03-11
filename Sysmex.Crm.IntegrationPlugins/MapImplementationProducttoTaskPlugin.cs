using SonomaPartners.Crm.Toolkit.Plugins;
using System;
using Sysmex.Crm.IntegrationPlugins.Logic;
using Microsoft.Xrm.Sdk;


namespace Sysmex.Crm.IntegrationPlugins
{
	//Added by Yash on 26-02-2021 Ticket No 60085
	public class MapImplementationProducttoTaskPlugin :PluginBase
	{
		public override void OnExecute(IServiceProvider serviceProvider)
		{
			var context = serviceProvider.GetPluginExecutionContext();
			var trace = serviceProvider.GetTracingService();
			var orgService = serviceProvider.CreateSystemOrganizationService();

			trace.Trace("Begin MapImplementationProducttoTaskPlugin");
			var task = ((Entity)context.InputParameters["Target"]);
			trace.Trace("Entity Name :"+task.LogicalName);

			var logic = new MapImplementationProducttoTaskLogic(orgService, trace);
			logic.UpdateTask(task);

			trace.Trace("End MapImplementationProducttoTaskPlugin");
		}
	}
}
