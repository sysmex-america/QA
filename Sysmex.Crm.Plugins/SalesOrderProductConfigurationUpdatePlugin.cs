using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.ObjectModel;


namespace Sysmex.Crm.Plugins
{
    public class SalesOrderProductConfigurationUpdatePlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            Entity inputParameter = ((IExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext))).InputParameters["Target"] as Entity;
            if (inputParameter == null || !inputParameter.Contains("smx_productconfig"))
            {
                return;
            }

            EntityReference attributeValue = inputParameter.GetAttributeValue<EntityReference>("smx_productconfig");
            if (attributeValue == null)
            {
                return;
            }

            IOrganizationService organizationService = ((IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory))).CreateOrganizationService(new Guid?());
            QueryExpression queryExpression1 = new QueryExpression("new_cpq_lineitem_tmp");
            queryExpression1.Criteria = new FilterExpression();
            queryExpression1.Criteria.AddCondition("smx_salesorderid", ConditionOperator.Equal, (object)inputParameter.Id);
            queryExpression1.NoLock = true;
            queryExpression1.ColumnSet = new ColumnSet(false);
            foreach (Entity entity in (Collection<Entity>)organizationService.RetrieveMultiple((QueryBase)queryExpression1).Entities)
            {
                entity["smx_salesorderid"] = (object)null;
                entity.EntityState = new EntityState?(EntityState.Changed);
                organizationService.Update(entity);
            }
            QueryExpression queryExpression2 = new QueryExpression("new_cpq_lineitem_tmp");
            queryExpression2.Criteria = new FilterExpression();
            queryExpression2.Criteria.AddCondition("smx_salesorderid", ConditionOperator.Null);
            queryExpression2.Criteria.AddCondition("new_productconfigurationid", ConditionOperator.Equal, (object)attributeValue.Id);
            queryExpression2.ColumnSet = new ColumnSet(false);
            queryExpression2.NoLock = true;
            EntityCollection entityCollection = organizationService.RetrieveMultiple((QueryBase)queryExpression2);
            EntityReference entityReference = new EntityReference(inputParameter.LogicalName, inputParameter.Id);
            foreach (Entity entity in (Collection<Entity>)entityCollection.Entities)
            {
                entity["smx_salesorderid"] = (object)entityReference;
                organizationService.Update(entity);
            }
        }
    }
}





