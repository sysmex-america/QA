using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Sysmex.Crm.WorkflowActivities.Logic
{
    public class UpdateOpportunityStageDurationLogic
    {
        private readonly IOrganizationService _orgService;
        private readonly ITracingService _trace;

        public UpdateOpportunityStageDurationLogic(IOrganizationService orgService, ITracingService trace)
        {
            _orgService = orgService;
            _trace = trace;
        }

        public void CreateOpportunityStageDuration(Entity entity)
        {
            _trace.Trace("Entered: CreateOpportunityStageDuration");
            UpdateCloseProbability(entity);

            var stageName = RetrieveStageName(entity.GetAttributeValue<Guid>("stageid"));
            var opportunityStage = new Entity("smx_opportunitystage")
            {
                ["smx_opportunity"] = entity.ToEntityReference(),
                ["smx_name"] = stageName,
                ["smx_stagestartdate"] = DateTime.UtcNow,
                ["smx_stageid"] = entity.GetAttributeValue<Guid>("stageid").ToString()
            };

            _orgService.Create(opportunityStage);
        }

        public void UpdateOpportunityStageDuration(Entity entity, Entity preEntity)
        {
            _trace.Trace("Entered: UpdateOpportunityStageDuration");

            var opportunityStageRecords = RetrieveOpportunityStageRecords(preEntity);

            // Update the records associated with the previous stage
            UpdatePreviousStageRecord(preEntity, opportunityStageRecords);

            // Create new record associated with the new(current) stage
            CreateOpportunityStageDuration(entity);

            // If there are multiple records associated with previous stage, rollup duration and deactivate older records
            if (opportunityStageRecords.Count > 1)
            {
                CombineStageRecords(entity, opportunityStageRecords);
            }
        }

        private void UpdateCloseProbability(Entity entity)
        {
            var stageName = RetrieveStageName(entity.GetAttributeValue<Guid>("stageid"));
            var updateEntity = new Entity(entity.LogicalName);
            updateEntity.Id = entity.Id;

            switch (stageName.ToLower())
            {
                case "opportunity":
                    updateEntity["closeprobability"] = 20;
                    break;
                case "competing with only 1 vendor":
                    updateEntity["closeprobability"] = 50;
                    break;
                case "high business influencer in tab commits to sysmex":
                    updateEntity["closeprobability"] = 70;
                    break;
                case "admin/ihg high business influencer confirm":
                    updateEntity["closeprobability"] = 80;
                    break;
                case "final paperwork":
                    updateEntity["closeprobability"] = 90;
                    break;
                default:
                    updateEntity["closeprobability"] = 0;
                    break;
            }

            _orgService.Update(updateEntity);
        }

        public void UpdatePreviousStageRecord(Entity entity, List<Entity> opportunityStageRecords)
        {
            _trace.Trace("Entered: UpdatePreviousStageRecord");

            if (opportunityStageRecords.Count == 0)
            {
                _trace.Trace("No existing Opportunity Stage record was found.");
                return;
            }

            var opportunityStageRecord = opportunityStageRecords[0];

            var opportunityStageStart = opportunityStageRecord.GetAttributeValue<DateTime>("smx_stagestartdate");
            var duration = Convert.ToInt32(Math.Ceiling((DateTime.UtcNow - opportunityStageStart).TotalMinutes));

            opportunityStageRecord["smx_stageenddate"] = DateTime.UtcNow;
            opportunityStageRecord["smx_stageduration"] = duration;
            _orgService.Update(opportunityStageRecord);
        }

        public List<Entity> RetrieveOpportunityStageRecords(Entity entity)
        {
            _trace.Trace("Entered: RetrieveOpportunityStageRecords");

            var stageName = RetrieveStageName(entity.GetAttributeValue<Guid>("stageid"));
            var fetchXml = $@"
                <fetch aggregate='false' distinct='false' mapping='logical'>
                  <entity name='smx_opportunitystage'>
                    <attribute name='smx_stagestartdate' />
                    <attribute name='smx_stageduration' />
                    <order attribute='createdon' descending='true' />
                    <filter type='and'>
                        <condition attribute='smx_opportunity' operator='eq' value='{entity.Id}' />
                        <condition attribute='smx_name' operator='eq' value='{stageName}' />
                        <condition attribute='statecode' operator='eq' value='0' />
                    </filter>
                  </entity>
                </fetch>";

            return _orgService.RetrieveMultiple(new FetchExpression(fetchXml)).Entities.ToList();
        }

        public void CombineStageRecords(Entity entity, List<Entity> opportunityStageRecords)
        {
            _trace.Trace("Entered: CombineStageRecords");

            // Cache the most recently created record, we will rollup duration to this record and deactivate the rest
            var currentRecord = opportunityStageRecords[0];

            // Deactivate other active records
            for (var i = 1; i < opportunityStageRecords.Count; i++)
            {
                SetStateRequest setStateRequest = new SetStateRequest
                {
                    EntityMoniker = new EntityReference
                    {
                        Id = opportunityStageRecords[i].Id,
                        LogicalName = opportunityStageRecords[i].LogicalName
                    },
                    State = new OptionSetValue(1),
                    Status = new OptionSetValue(2)
                };

                _orgService.Execute(setStateRequest);
            }

            // Rollup duration and update record
            var duration = opportunityStageRecords.Sum(o => o.GetAttributeValue<int>("smx_stageduration"));
            currentRecord["smx_stageduration"] = duration;
            currentRecord["smx_returnedto"] = true;
            _orgService.Update(currentRecord);
        }

        public string RetrieveStageName(Guid stageId)
        {
            _trace.Trace("Entered: RetrieveStageName");

            var stage = _orgService.RetrieveMultiple(new FetchExpression($@"
                <fetch aggregate='false' distinct='false' mapping='logical'>
                  <entity name='processstage'>
                    <attribute name='processstageid' />
                    <attribute name='stagename' />
                    <filter>
                      <condition attribute='processstageid' operator='eq' value='{stageId}' />
                    </filter>
                  </entity>
                </fetch>")).Entities.FirstOrDefault();

            return stage?.GetAttributeValue<string>("stagename");
        }
    }
}

