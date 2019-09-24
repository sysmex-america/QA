using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Sysmex.Crm.Plugins.Logic
{
    public class SplitOpportunityLogic
    {
        private readonly IPluginExecutionContext _context;
        private readonly IOrganizationService _orgService;
        private readonly ITracingService _trace;

        public SplitOpportunityLogic(IPluginExecutionContext context, IOrganizationService orgService, ITracingService trace)
        {
            _context = context;
            _orgService = orgService;
            _trace = trace;
        }

        public void SplitOpportunity(Entity entity)
        {
            // Retrieve Product Id String
            var productString = _context.InputParameters.Contains("ProductString") ? _context.InputParameters["ProductString"] as string : null;
            var allProductsSelected = _context.InputParameters.Contains("AllProductsSelected") ? _context.InputParameters["AllProductsSelected"] as string : null;

            // Retrieve Lost Reason if exists;
            var status = _context.InputParameters.Contains("Status") ? _context.InputParameters["Status"] as string : null;
            var lostReason = _context.InputParameters.Contains("Reason") ? _context.InputParameters["Reason"] as string : null;
            
            var opportunityProducts = RetrieveFlaggedOrderProducts(productString);

            if (allProductsSelected == "true") // Receiving errors when attempting to pass Boolean Input into Custom Action so I set the type to String
            {
                _trace.Trace("All Opportunity Products were flagged for selection, updating the original opportunity.");

                UpdateOpportunity(entity, status, lostReason);
                _context.OutputParameters["ClonedEntity"] = null;
            }
            else
            {
                _trace.Trace("Not all Opportunity Products were flagged for selection, cloning the original opportunity.");

                var clonedEntity = CloneOpportunity(entity, status);

                UpdateOpportunityProducts(clonedEntity, opportunityProducts);
                UpdateOpportunity(clonedEntity, status, lostReason);

                _context.OutputParameters["ClonedEntity"] = clonedEntity.ToEntityReference();
            }
        }

        public List<Entity> RetrieveFlaggedOrderProducts(string productString)
        {
            _trace.Trace("Entered: RetrieveFlaggedOrderProducts");

            var productIds = productString.Split(',').ToList();
            var conditionSet = CreateConditionSet(productIds);

            var fetchXml = $@"
                <fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                    <entity name='opportunityproduct'>
                        <attribute name='productid' />
                        <attribute name='productdescription' />
                        <attribute name='priceperunit' />
                        <attribute name='quantity' />
                        <attribute name='extendedamount' />
                        <attribute name='opportunityproductid' />
                        <filter type='and'>
                            {conditionSet}
                        </filter>
                    </entity>
                </fetch>";

            return _orgService.RetrieveMultiple(new FetchExpression(fetchXml)).Entities.ToList();
        }

        public string CreateConditionSet(List<string> productIds)
        {
            var conditionSet = "<condition attribute='opportunityproductid' operator='in'>";
            foreach (var productId in productIds)
            {
                conditionSet = conditionSet + $"<value>{productId}</value>";
            }
            conditionSet = conditionSet + "</condition>";

            return conditionSet;
        }

        public Entity CloneOpportunity(Entity entity, string status)
        {
            _trace.Trace("Entered: CloneOpportunity");

            var outputEntity = new Entity(entity.LogicalName);
            
            foreach (var key in entity.Attributes.Keys)
            {
                _trace.Trace($"Cloning attribute: {key}");
                if (key == "statuscode" || key == "statecode")
                {
                    _trace.Trace("Key is statecode or statuscode, skipping.");
                    continue;
                }

                switch (entity.Attributes[key].GetType().ToString())
                {
                    case "Microsoft.Xrm.Sdk.EntityReference":
                        outputEntity.Attributes[key] = entity.GetAttributeValue<EntityReference>(key);
                        break;
                    case "Microsoft.Xrm.Sdk.OptionSetValue":
                        outputEntity.Attributes[key] = entity.GetAttributeValue<OptionSetValue>(key);
                        break;
                    case "Microsoft.Xrm.Sdk.Money":
                        outputEntity.Attributes[key] = entity.GetAttributeValue<Money>(key);
                        break;
                    case "System.Guid":
                        break;
                    default:
                        outputEntity.Attributes[key] = entity.Attributes[key];
                        break;
                }
            }

            if (status == "sendToContract")
            {
                _trace.Trace("Opportunity Products being sent to contract, setting processid and stageid");

                var processId = RetrieveProcessId("Sales Process - Primary");
                var finalPaperworkStage = RetrieveStageId("Final Paperwork", processId);
                outputEntity.Attributes["processid"] = processId;
                outputEntity.Attributes["stageid"] = finalPaperworkStage;
            }
            
            Guid clonedEntityGuid = _orgService.Create(outputEntity);
            var clonedEntityReference = new EntityReference(outputEntity.LogicalName, clonedEntityGuid);

            return _orgService.Retrieve(clonedEntityReference.LogicalName, clonedEntityReference.Id, new ColumnSet(false));
        }

        public void UpdateOpportunityProducts(Entity clonedEntity, List<Entity> opportunityProducts)
        {
            _trace.Trace("Entered: UpdateOpportunityProducts");

            foreach (var oppProduct in opportunityProducts)
            {
                _trace.Trace($"Updating opportunity product: {oppProduct.Id}");
                oppProduct["opportunityid"] = clonedEntity.ToEntityReference();
                _orgService.Update(oppProduct);
            }
        }

        public void UpdateOpportunity(Entity entity, string status, string lostReason)
        {
            _trace.Trace("Entered: UpdateOpportunity");

            if (status != "lostProducts")
            {
                return;
            }

            Entity opportunityClose = new Entity("opportunityclose");
            opportunityClose.Attributes.Add("opportunityid", entity.ToEntityReference());
            opportunityClose.Attributes.Add("subject", lostReason);

            LoseOpportunityRequest request = new LoseOpportunityRequest
            {
                OpportunityClose = opportunityClose,
                Status = new OptionSetValue(4)
            };

            _orgService.Execute(request);
        }

        private Guid RetrieveProcessId(string processName)
        {
            _trace.Trace("Entered: RetrieveProcessId");

            // Category of 4 is Business Process Flow
            var process = _orgService.RetrieveMultiple(new FetchExpression($@"
                <fetch count='1' aggregate='false' distinct='false' mapping='logical'>
                  <entity name='workflow'>
                    <attribute name='workflowid' />
                    <filter>
                      <condition attribute='name' operator='eq' value='{processName}' />
                      <condition attribute='category' operator='eq' value='4' />
                      <condition attribute='type' operator='eq' value='1' />
                    </filter>
                    <order attribute='processorder' />
                  </entity>
                </fetch>")).Entities.FirstOrDefault();

            return process?.GetAttributeValue<Guid>("workflowid") ?? Guid.Empty;
        }

        private Guid RetrieveStageId(string stageName, Guid? processId)
        {
            _trace.Trace("Entered: RetrieveStageId");

            var stage = _orgService.RetrieveMultiple(new FetchExpression($@"
                <fetch count='1' aggregate='false' distinct='false' mapping='logical'>
                  <entity name='processstage'>
                    <attribute name='processstageid' />
                    <filter>
                      <condition attribute='stagename' operator='eq' value='{stageName}' />
                    </filter>
                    <link-entity name='workflow' from='workflowid' to='processid' link-type='inner'>
                        <filter>
                            <condition attribute='workflowid' operator='eq' value='{processId}' />
                        </filter>
                    </link-entity>
                  </entity>
                </fetch>")).Entities.FirstOrDefault();

            return stage?.GetAttributeValue<Guid>("processstageid") ?? Guid.Empty;
        }
    }
}
