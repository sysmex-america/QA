using System.Activities;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using Sysmex.Crm.WorkflowActivities.Logic;

namespace Sysmex.Crm.WorkflowActivities
{
    public sealed class UpdateOpportunityStageDuration : CodeActivity
    {
        [RequiredArgument]
        [Input("Process Stage Name")]
        public InArgument<string> ProcessStage { get; set; }

        protected override void Execute(CodeActivityContext executionContext)
        {
            // Create the tracing service 
            ITracingService tracingService = executionContext.GetExtension<ITracingService>();

            if (tracingService == null)
            {
                throw new InvalidPluginExecutionException("Failed to retrieve tracing service.");
            }

            tracingService.Trace("Entered UpdateOpportunityStageDuration.Execute(), Activity Instance Id: {0}, Workflow Instance Id: {1}",
                executionContext.ActivityInstanceId,
                executionContext.WorkflowInstanceId);

            // Create the context 
            IWorkflowContext context = executionContext.GetExtension<IWorkflowContext>();

            if (context == null)
            {
                throw new InvalidPluginExecutionException("Failed to retrieve workflow context.");
            }

            tracingService.Trace("ChangeProcessStageActivity.Execute(), Correlation Id: {0}, Initiating User: {1}",
                context.CorrelationId,
                context.InitiatingUserId);

            IOrganizationServiceFactory serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            var logic = new UpdateOpportunityStageDurationLogic(service, tracingService);
            var entity = service.Retrieve(context.PrimaryEntityName, context.PrimaryEntityId, new ColumnSet("stageid"));
            if (context.MessageName == "Create")
            {
                logic.CreateOpportunityStageDuration(entity);
            }
            else if (context.MessageName == "Update")
            {
                var preImage = context.PreEntityImages.Values.FirstOrDefault();
                logic.UpdateOpportunityStageDuration(entity, preImage);
            }

            tracingService.Trace("Exiting UpdateOpportunityStageDuration.Execute(), Correlation Id: {0}", context.CorrelationId);
        }
    }
}
