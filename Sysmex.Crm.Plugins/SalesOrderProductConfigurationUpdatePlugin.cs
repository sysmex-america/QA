using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;

namespace Sysmex.Crm.Plugins
{
    public class SalesOrderProductConfigurationUpdatePlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var entity = context.InputParameters["Target"] as Entity;

            if (entity == null || !entity.Contains("smx_productconfig"))
            {
                return;
            }

            var productConfig = entity.GetAttributeValue<EntityReference>("smx_productconfig");
            if (productConfig == null)
            {
                return;
            }

            var factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var service = factory.CreateOrganizationService(null);

            var qe = new QueryExpression("new_cpq_lineitem_tmp");
            qe.Criteria = new FilterExpression();
            qe.Criteria.AddCondition("smx_salesorderid", ConditionOperator.Equal, entity.Id);
            qe.NoLock = true;
            qe.ColumnSet = new ColumnSet(false);

            var qlines = service.RetrieveMultiple(qe);

            foreach (var qline in qlines.Entities)
            {
                qline["smx_salesorderid"] = null;
                qline.EntityState = EntityState.Changed;
                service.Update(qline);
            }

            qe = new QueryExpression("new_cpq_lineitem_tmp");
            qe.Criteria = new FilterExpression();
            qe.Criteria.AddCondition("smx_salesorderid", ConditionOperator.Null);
            qe.Criteria.AddCondition("new_productconfigurationid", ConditionOperator.Equal, productConfig.Id);
            qe.ColumnSet = new ColumnSet(false);
            qe.NoLock = true;

            qlines = service.RetrieveMultiple(qe);

            var salesOrderRef = new EntityReference(entity.LogicalName, entity.Id);

            foreach (var qline in qlines.Entities)
            {
                qline["smx_salesorderid"] = salesOrderRef;
                service.Update(qline);
            }
        }
    }
}
