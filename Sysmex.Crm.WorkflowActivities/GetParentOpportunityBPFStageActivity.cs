using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace Sysmex.Crm.WorkflowActivities
{
    public class GetParentOpportunityBPFStageActivity : CodeActivity
    {
        [Input("Opportunity")]
        [RequiredArgument]
        [ReferenceTarget("opportunity")]
        public InArgument<EntityReference> Opportunity { get; set; }

        [Output("US Process Stage")]
        [ReferenceTarget("processstage")]
        public OutArgument<EntityReference> USStage { get; set; }

        [Output("CA Process Stage")]
        [ReferenceTarget("processstage")]
        public OutArgument<EntityReference> CAStage { get; set; }

        protected override void Execute(CodeActivityContext activityContext)
        {
            var context = activityContext.GetExtension<IWorkflowContext>();
            var factory = activityContext.GetExtension<IOrganizationServiceFactory>();
            var service = factory.CreateOrganizationService(null);

            var oppId = Opportunity.Get(activityContext);

            var qe = new QueryExpression("smx_salesprocessca");
            qe.Criteria = new FilterExpression();
            qe.Criteria.AddCondition("bpf_opportunityid", ConditionOperator.Equal, oppId.Id);
            qe.ColumnSet = new ColumnSet("activestageid");
            qe.TopCount = 1;
            qe.NoLock = true;

            var caProcess = service.RetrieveMultiple(qe);
            if (caProcess.Entities.Count > 0)
            {
                CAStage.Set(activityContext, caProcess[0].GetAttributeValue<EntityReference>("activestageid"));
            }

            qe.EntityName = "smx_salesprocess_primary";
            var usProcess = service.RetrieveMultiple(qe);
            if (usProcess.Entities.Count > 0)
            {
                USStage.Set(activityContext, usProcess[0].GetAttributeValue<EntityReference>("activestageid"));
            }
        }
    }
}
