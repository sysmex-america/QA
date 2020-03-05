using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;

namespace Sysmex.Crm.Plugins
{
    public class InstrumentUpdateTotalValueCalculationPlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var entity = context.InputParameters["Target"] as Entity;

            if (entity == null || (!entity.Contains("smx_equivalentvalue") && !entity.Contains("smx_opportunitylabid")))
            {
                return;
            }

            if (!context.PostEntityImages.Contains("post"))
            {
                return;
            }

            var post = context.PostEntityImages["post"];
            var labId = post.GetAttributeValue<EntityReference>("smx_opportunitylabid");

            IOrganizationService service = null;

            if (labId != null)
            {
                var factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                service = factory.CreateOrganizationService(null);

                RecalculateLabInstrumentUpdateTotalValue(labId.Id, service);
            }

            if (context.PreEntityImages.Contains("pre"))
            {
                var pre = context.PreEntityImages["pre"];
                var pre_labId = pre.GetAttributeValue<EntityReference>("smx_opportunitylabid");

                if (pre_labId != null && (labId == null || labId.Id != pre_labId.Id))
                {
                   if (service == null)
                    {
                        var factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                        service = factory.CreateOrganizationService(null);
                    }

                    RecalculateLabInstrumentUpdateTotalValue(pre_labId.Id, service);
                }
            }
        }

        void RecalculateLabInstrumentUpdateTotalValue(Guid labId, IOrganizationService service)
        {
            var qe = new QueryExpression("smx_instrumentupdate");
            qe.Criteria = new FilterExpression();
            qe.Criteria.AddCondition("smx_opportunitylabid", ConditionOperator.Equal, labId);
            qe.Criteria.AddCondition("smx_equivalentvalue", ConditionOperator.NotNull);
            qe.ColumnSet = new ColumnSet("smx_equivalentvalue");
            qe.NoLock = true;

            var data = service.RetrieveMultiple(qe);
            decimal total = 0;
            foreach (var d in data.Entities)
            {
                var value = d.GetAttributeValue<Money>("smx_equivalentvalue")?.Value;
                if (value.HasValue)
                {
                    total += value.Value;
                }
            }

            var updated = new Entity("smx_opportunitylab");
            updated.Id = labId;
            updated["smx_instupdatevalue"] = new Money(total);
            service.Update(updated);
        }
    }
}
