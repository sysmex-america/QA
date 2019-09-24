using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sysmex.Crm.Plugins.Logic
{
    public class CalculateLabInstrumentRollupsPluginLogic
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

        public CalculateLabInstrumentRollupsPluginLogic(IOrganizationService orgService, ITracingService tracer)
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
                if (preImage.Contains("smx_lab"))
                {
                    preLab = (EntityReference)preImage["smx_lab"];
                }

                if (postImage.Contains("smx_lab"))
                {
                    postLab = (EntityReference)postImage["smx_lab"];
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
                if (targetEntity.Contains("smx_lab"))
                {
                    var lab = (EntityReference)targetEntity["smx_lab"];
                    CalculateLabRollups(lab);
                }
            }
        }

        public void CalculateLabRollups(EntityReference lab)
        {
            QueryExpression query = new QueryExpression("smx_instrument");
            query.Criteria.AddCondition(new ConditionExpression("smx_lab", ConditionOperator.Equal, lab.Id));
            query.Criteria.AddCondition(new ConditionExpression("statecode", ConditionOperator.Equal, 0));
            query.Criteria.AddCondition(new ConditionExpression("smx_metrixinstrument", ConditionOperator.NotEqual, true));
            query.LinkEntities.Add(new LinkEntity("smx_instrument", "smx_productline", "smx_productline", "smx_productlineid", JoinOperator.LeftOuter));
            query.LinkEntities[0].Columns.AddColumn("smx_name");
            query.LinkEntities[0].EntityAlias = "smx_productline";
            
            var instruments = orgService.RetrieveMultiple(query).Entities;

            Entity updateLab = new Entity("smx_lab");
            updateLab.Id = lab.Id;

            foreach (var productField in productLineFieldMap)
            {
                updateLab[productField.Value] = instruments.Where(t => t.Contains("smx_productline.smx_name") &&
                         ((AliasedValue)t["smx_productline.smx_name"]).Value.ToString().ToLower() == productField.Key).Count();
            }

            orgService.Update(updateLab);
        }
    }
}
