using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sysmex.Crm.Plugins.Logic
{
	//Added by Yash on 22-10-2020 - 58717
	public class CalculateLALabInstrumentRollupsPluginLogic
	{
		IOrganizationService orgService;
		ITracingService tracer;

		Dictionary<String, String> productLineFieldMap = new Dictionary<string, string>()
		{
			{ "hematology", "smx_hemeinstruments" },
			{ "flow cytometry", "smx_flowinstruments" },
			{ "urinalysis", "smx_urinalysisinstruments" },
			{ "esr", "smx_esrinstruments" },
			{ "coagulation", "smx_coagulationinstruments" },
			{ "chemistry/ia", "smx_chemistryiainstruments" }
		};

		public CalculateLALabInstrumentRollupsPluginLogic(IOrganizationService orgService, ITracingService tracer)
		{
			this.orgService = orgService;
			this.tracer = tracer;
		}

		public void Execute(string messageName, Entity preImage, Entity postImage, Entity targetEntity)
		{
			// if != update rollup for lab of targetEntity
			if (messageName.ToLower() == "update")
			{
				EntityReference preLab = null, postLab = null;
				if (preImage.Contains("smx_lalabid"))
				{
					preLab = (EntityReference)preImage["smx_lalabid"];
				}

				if (postImage.Contains("smx_lalabid"))
				{
					postLab = (EntityReference)postImage["smx_lalabid"];
				}

				if (preLab != null)
				{
					CalculateLabRollups(preLab);
				}

				if ((preLab == null && postLab != null)
					|| (preLab != null && postLab != null && preLab.Id != postLab.Id))
				{
					// only do this if the lab changed during the update
					CalculateLabRollups(postLab);
				}
			}
			else
			{
				if (targetEntity.Contains("smx_lalabid"))
				{
					var lab = (EntityReference)targetEntity["smx_lalabid"];
					CalculateLabRollups(lab);
				}
			}
		}

		public void CalculateLabRollups(EntityReference lab)
		{
			QueryExpression query = new QueryExpression("smx_lainstrument");
			query.Criteria.AddCondition(new ConditionExpression("smx_lalabid", ConditionOperator.Equal, lab.Id));
			query.Criteria.AddCondition(new ConditionExpression("statecode", ConditionOperator.Equal, 0));
			query.Criteria.AddCondition(new ConditionExpression("smx_metrixinstrument", ConditionOperator.NotEqual, true));
			query.LinkEntities.Add(new LinkEntity("smx_lainstrument", "smx_laproductline", "smx_laproductline", "smx_laproductlineid", JoinOperator.LeftOuter));
			query.LinkEntities[0].Columns.AddColumn("smx_name");
			query.LinkEntities[0].EntityAlias = "smx_laproductline";

			var instruments = orgService.RetrieveMultiple(query).Entities;

			Entity updateLab = new Entity("smx_lalab");
			updateLab.Id = lab.Id;

			foreach (var productField in productLineFieldMap)
			{
				updateLab[productField.Value] = instruments.Where(t => t.Contains("smx_laproductline.smx_name") &&
						 ((AliasedValue)t["smx_laproductline.smx_name"]).Value.ToString().ToLower() == productField.Key).Count();
			}

			orgService.Update(updateLab);
		}
	}
}
