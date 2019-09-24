using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using SonomaPartners.Crm.Toolkit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Sysmex.Crm.Plugins.Logic
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
            _trace.Trace($"Entered: {MethodBase.GetCurrentMethod().Name}");
            var processStageRef = entity.GetAttributeValue<EntityReference>("activestageid");
            if (processStageRef == null)
            {
                _trace.Trace("No Process Stage was found, returning.");
                return;
            }

            var processStage = _orgService.Retrieve(processStageRef.LogicalName, processStageRef.Id, new ColumnSet("processstageid", "stagename"));
            var stageId = processStage.GetAttributeValue<Guid>("processstageid");
            var stageName = processStage.GetAttributeValue<string>("stagename");

            var opportunityRef = entity.GetAttributeValue<EntityReference>("bpf_opportunityid");
            if (opportunityRef == null)
            {
                _trace.Trace("No Opportunity was found, returning.");
                return;
            }
            
            UpdateCloseProbability(opportunityRef, stageName);

            var opportunityStage = new Entity("smx_opportunitystage")
            {
                ["smx_opportunity"] = opportunityRef,
                ["smx_name"] = stageName,
                ["smx_stagestartdate"] = DateTime.UtcNow,
                ["smx_stageid"] = stageId.ToString()
            };

            _orgService.Create(opportunityStage);
        }

        public void UpdateOpportunityStageDuration(Entity entity, Entity preEntity)
        {
            _trace.Trace($"Entered: {MethodBase.GetCurrentMethod().Name}");
            var opportunityStageRecords = RetrieveOpportunityStageRecords(preEntity);

            // Update the records associated with the previous stage
            UpdatePreviousStageRecord(opportunityStageRecords);
            
            // Create new record associated with the new(current) stage
            entity.MergeAttributes(preEntity);
            CreateOpportunityStageDuration(entity);

            // If there are multiple records associated with previous stage, rollup duration and deactivate older records
            if (opportunityStageRecords != null && opportunityStageRecords.Count > 1)
            {
                CombineStageRecords(opportunityStageRecords);
            }
        }

        private void UpdateCloseProbability(EntityReference opportunityRef, string stageName)
        {
            _trace.Trace($"Entered: {MethodBase.GetCurrentMethod().Name}");
            var updateOpportunity = new Entity(opportunityRef.LogicalName)
            {
                Id = opportunityRef.Id
            };

            switch (stageName.ToLower())
            {
                case "opportunity":
                    updateOpportunity["closeprobability"] = 20;
                    break;
                case "competing with only 1 vendor":
                    updateOpportunity["closeprobability"] = 50;
                    break;
                case "High Business Influencer Commits":
                    updateOpportunity["closeprobability"] = 70;
                    break;
                case "High Business Influencer Confirms":
                    updateOpportunity["closeprobability"] = 80;
                    break;
                case "final paperwork":
                    updateOpportunity["closeprobability"] = 90;
                    break;
                default:
                    updateOpportunity["closeprobability"] = 0;
                    break;
            }

            _orgService.Update(updateOpportunity);
        }

        public void UpdatePreviousStageRecord(List<Entity> opportunityStageRecords)
        {
            _trace.Trace($"Entered: {MethodBase.GetCurrentMethod().Name}");
            if (opportunityStageRecords == null || opportunityStageRecords.Count == 0)
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
            _trace.Trace($"Entered: {MethodBase.GetCurrentMethod().Name}");
            var processStageRef = entity.GetAttributeValue<EntityReference>("activestageid");
            if (processStageRef == null)
            {
                _trace.Trace("No Process Stage was found, returning null.");
                return null;
            }

            var processStage = _orgService.Retrieve(processStageRef.LogicalName, processStageRef.Id, new ColumnSet("processstageid", "stagename"));
            var stageName = processStage.GetAttributeValue<string>("stagename");

            var opportunityRef = entity.GetAttributeValue<EntityReference>("bpf_opportunityid");
            if (opportunityRef == null)
            {
                _trace.Trace("No Opportunity was found, returning null.");
                return null;
            }

            var fetchXml = $@"
                <fetch aggregate='false' distinct='false' mapping='logical'>
                  <entity name='smx_opportunitystage'>
                    <attribute name='smx_stagestartdate' />
                    <attribute name='smx_stageduration' />
                    <order attribute='createdon' descending='true' />
                    <filter type='and'>
                        <condition attribute='smx_opportunity' operator='eq' value='{opportunityRef.Id}' />
                        <condition attribute='smx_name' operator='eq' value='{stageName}' />
                        <condition attribute='statecode' operator='eq' value='0' />
                    </filter>
                  </entity>
                </fetch>";

            return _orgService.RetrieveMultiple(new FetchExpression(fetchXml)).Entities.ToList();
        }

        public void CombineStageRecords(List<Entity> opportunityStageRecords)
        {
            _trace.Trace($"Entered: {MethodBase.GetCurrentMethod().Name}");
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
    }
}
