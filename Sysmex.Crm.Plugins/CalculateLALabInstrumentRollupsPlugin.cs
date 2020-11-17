using Microsoft.Xrm.Sdk;
using SonomaPartners.Crm.Toolkit.Plugins;
using Sysmex.Crm.Plugins.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sysmex.Crm.Plugins
{
	//Added by Yash on 22-10-2020 - 58717
	public class CalculateLALabInstrumentRollupsPlugin : PluginBase
	{
		public override void OnExecute(IServiceProvider serviceProvider)
		{
			var context = serviceProvider.GetPluginExecutionContext();
			var orgService = serviceProvider.CreateOrganizationServiceAsCurrentUser();
			var tracer = serviceProvider.GetTracingService();

			Entity LAinstrument;
			if (context.MessageName.ToLower() != "delete")
			{
				LAinstrument = context.GetTargetEntity();
			}
			else
			{
				if (context.PreEntityImages.Count > 0)
				{
					LAinstrument = context.PreEntityImages.FirstOrDefault().Value;
				}
				else
				{
					return;
				}
			}

			Entity preImage = null, postImage = null;
			if (context.MessageName.ToLower() == "update")
			{
				if (context.PreEntityImages.Count > 0 && context.PostEntityImages.Count > 0)
				{
					preImage = context.PreEntityImages.FirstOrDefault().Value;
					postImage = context.PostEntityImages.FirstOrDefault().Value;
				}
				else
				{
					return;
				}
			}

			try
			{
				var logic = new CalculateLALabInstrumentRollupsPluginLogic(orgService, tracer);
				logic.Execute(context.MessageName, preImage, postImage, LAinstrument);
			}
			catch (Exception ex)
			{
				throw new InvalidPluginExecutionException(ex.Message, ex);
			}
		}
	}
}
