using Microsoft.Xrm.Sdk;
using SonomaPartners.Crm.Toolkit.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sysmex.Crm.Model;
using Sysmex.Crm.IntegrationPlugins.Logic;

namespace Sysmex.Crm.IntegrationPlugins
{
	public class ImplementationAutoAssignmentPlugin : PluginBase
	{
		public override void OnExecute(IServiceProvider serviceProvider)
		{
			var context = serviceProvider.GetPluginExecutionContext();
			var trace = serviceProvider.GetTracingService();
			var orgService = serviceProvider.CreateSystemOrganizationService();
			trace.Trace($"BEGIN {nameof(ImplementationAutoAssignmentPlugin)}");

			var entity = ((Entity)context.InputParameters["Target"]);
			var logic = new ImplementationAutoAssignmentLogic(orgService, trace);
			
			if (entity.LogicalName == smx_implementation.EntityLogicalName)
			{
				logic.SetOvRep(entity.ToEntity<smx_implementation>());
			}
			else if (entity.LogicalName == smx_implementationproduct.EntityLogicalName)
			{
				logic.SetOvRep(entity.ToEntity<smx_implementationproduct>());
			}

			trace.Trace($"END {nameof(ImplementationAutoAssignmentPlugin)}");
		}
	}
}
