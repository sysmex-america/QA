using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sysmex.Crm.Plugins
{
	public class InstrumentUpdateTotalValueCalculationPlugin : IPlugin
	{
		public void Execute(IServiceProvider serviceProvider)
		{
			IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
			if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
			{
				Entity inputParameter = context.InputParameters["Target"] as Entity;
				IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
				IOrganizationService service = serviceFactory.CreateOrganizationService(null);
				if (inputParameter == null || (!inputParameter.Contains("smx_equivalentvalue") && !inputParameter.Contains("smx_opportunitylabid")) || !context.PostEntityImages.Contains("post"))
					return;

				Entity postImageEntity = (Entity)context.PostEntityImages["post"];
				EntityReference attributeValue1 = null;
				if (postImageEntity.Contains("smx_opportunitylabid"))
				{
					attributeValue1 = context.PostEntityImages["post"].GetAttributeValue<EntityReference>("smx_opportunitylabid");
				}

				if (attributeValue1 != null)
				{				
					this.RecalculateLabInstrumentUpdateTotalValue(attributeValue1.Id, service);
				}

				if (context.PreEntityImages.Contains("pre"))
				{
					EntityReference attributeValue2 = null;
					Entity preImageEntity = (Entity)context.PreEntityImages["pre"];
					if (preImageEntity.Contains("smx_opportunitylabid"))
					{
						attributeValue2 = preImageEntity.GetAttributeValue<EntityReference>("smx_opportunitylabid");
					}
					if (attributeValue2 != null && (attributeValue1 == null || attributeValue1.Id != attributeValue2.Id))
					{
						this.RecalculateLabInstrumentUpdateTotalValue(attributeValue2.Id, service);
					}
				}
			}
		}
		private void RecalculateLabInstrumentUpdateTotalValue(Guid labId, IOrganizationService service)
		{
			QueryExpression queryExpression = new QueryExpression("smx_instrumentupdate");
			queryExpression.Criteria = new FilterExpression();
			queryExpression.Criteria.AddCondition("smx_opportunitylabid", ConditionOperator.Equal, labId);
			queryExpression.Criteria.AddCondition("smx_equivalentvalue", ConditionOperator.NotNull);
			queryExpression.ColumnSet = new ColumnSet("smx_equivalentvalue");
			//queryExpression.NoLock = true;
			EntityCollection entityCollection = service.RetrieveMultiple(queryExpression);
			decimal num = 0;
			foreach (Entity entity in entityCollection.Entities)
			{
				decimal value = (entity.Contains("smx_equivalentvalue") ? entity.GetAttributeValue<Money>("smx_equivalentvalue").Value : 0);
				num += value;
			}
			service.Update(new Entity("smx_opportunitylab")
			{
				Id = labId,
				["smx_instupdatevalue"] = new Money(num)
			});
		}
	}
}
