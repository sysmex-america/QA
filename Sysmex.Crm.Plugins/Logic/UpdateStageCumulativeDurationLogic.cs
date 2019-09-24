using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using SonomaPartners.Crm.Toolkit;
using Sysmex.Crm.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Sysmex.Crm.Plugins.Logic
{
    public class UpdateStageCumulativeDurationLogic : LogicBase
    {
        public UpdateStageCumulativeDurationLogic(IOrganizationService orgService, ITracingService tracer)
            : base(orgService, tracer)
        {
        }

        public void ProcessCumulativeDurations(smx_stageduration stageDuration, smx_stageduration stageDurationPreimage)
        {
            _tracer.Trace("Process Cumulative Durations");
            var stageName = stageDuration.Contains("smx_stagename") || stageDurationPreimage == null
                ? stageDuration.smx_StageName
                : stageDurationPreimage.smx_StageName;

            var salesOrderRef = stageDuration.Contains("regardingobjectid") || stageDurationPreimage == null
                ? stageDuration.RegardingObjectId
                : stageDurationPreimage.RegardingObjectId;

            var actualStart = stageDuration.Contains("actualstart") || stageDurationPreimage == null
                ? stageDuration.ActualStart
                : stageDurationPreimage.ActualStart;

            var actualEnd = stageDuration.ActualEnd;

            if (!actualStart.HasValue || !actualEnd.HasValue)
            {
                _tracer.Trace("ActualStart and/or ActualEnd not provided, returning");
                return;
            }
            else if (salesOrderRef?.LogicalName != smx_salesorder.EntityLogicalName || String.IsNullOrWhiteSpace(stageName))
            {
                _tracer.Trace("Stage name is blank, or regarding field is not type Sales order, returning");
                return;
            }

            var actualStartTrimmed = TrimDateTime(actualStart.Value, TimeSpan.TicksPerMinute);
            var actualEndTrimmed = TrimDateTime(actualEnd.Value, TimeSpan.TicksPerMinute);

            var duration = new decimal((actualEndTrimmed - actualStartTrimmed).TotalMinutes);

            var lastCumulativeDuration = RetrieveLastCumulativeDuration(salesOrderRef.Id, stageDuration.Id, stageName);

            stageDuration.smx_StageCumulativeDuration = lastCumulativeDuration + duration;
        }

        private decimal RetrieveLastCumulativeDuration(Guid salesOrderId, Guid stageDurationId, string stageName)
        {
            _tracer.Trace($"Retrieve Last Cumulative Duration from Sales Order {salesOrderId}, Stage Duration {stageDurationId}, Stage name {stageName}");

            var fetch = $@"
                <fetch top='1'>
                  <entity name='smx_stageduration'>
                    <attribute name='activityid' />
                    <attribute name='createdon' />
                    <attribute name='smx_stagecumulativeduration' />
                    <order attribute='createdon' descending='false' />
                    <filter type='and'>
                      <condition attribute='smx_stagename' operator='eq' value='{stageName}' />
                      <condition attribute='activityid' operator='ne' value='{stageDurationId}' />
                    </filter>
                    <link-entity name='smx_salesorder' from='smx_salesorderid' to='regardingobjectid' link-type='inner' alias='ac'>
                      <filter type='and'>
                        <condition attribute='smx_salesorderid' operator='eq' value='{salesOrderId}' />
                      </filter>
                    </link-entity>
                  </entity>
                </fetch>";

            var results = _orgService.RetrieveMultiple<smx_stageduration>(new FetchExpression(fetch));

            return results.FirstOrDefault()?.smx_StageCumulativeDuration ?? 0;
        }

        private static DateTime TrimDateTime(DateTime date, long ticks)
        {
            return new DateTime(date.Ticks - (date.Ticks % ticks), date.Kind);
        }
    }
}
