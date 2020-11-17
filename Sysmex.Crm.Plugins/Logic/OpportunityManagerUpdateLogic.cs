using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sysmex.Crm.Plugins.Logic
{
	public class OpportunityManagerUpdateLogic
	{
		private IOrganizationService _orgService;
		private ITracingService _tracer;

		public OpportunityManagerUpdateLogic(IOrganizationService orgService, ITracingService tracer)
		{
			_orgService = orgService;
			_tracer = tracer;
		}
		public void UpdateOpportunityManager(Entity account)
		{

			EntityCollection opportunities = GetRelatedOppprtunities(account);
			EntityReference accountOwner =(EntityReference) account.Attributes["ownerid"];
			if (opportunities == null)
			{
				_tracer.Trace("no opportunities to update. Exiting plugin logic.");
				return;
			}
			try
			{
				_tracer.Trace("opportity count -" + opportunities.Entities.Count);
				foreach (Entity opportinity in opportunities.Entities)
				{
					//Added by Yash on 14-10-2020 - 58616
					int oppManagerApprovalRole = opportunityManagerApprovalRole(opportinity);
					_tracer.Trace("Opportunity Manager Approval Role is :"+oppManagerApprovalRole);
					if (oppManagerApprovalRole != 180700003 && oppManagerApprovalRole != 100000002 && oppManagerApprovalRole != 180700001)//MDS=180700003--FCAM=100000002--CAE=180700001
					{
						AssignRequest assign = new AssignRequest
						{
							Assignee = new EntityReference(accountOwner.LogicalName, accountOwner.Id),
							Target = new EntityReference(opportinity.LogicalName, opportinity.Id)
						};
						_orgService.Execute(assign);
					}
				}

			}
			catch (Exception ex)
			{
				_tracer.Trace(ex.Message);
			}

			_tracer.Trace("Exiting Logic.");
		}
		private EntityCollection GetRelatedOppprtunities(Entity account)
		{
			QueryExpression query = new QueryExpression("opportunity");
			query.ColumnSet = new ColumnSet("ownerid");
			query.Criteria.AddCondition("parentaccountid", ConditionOperator.Equal,account.Id);
			query.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);
			return _orgService.RetrieveMultiple(query);
		}
		public bool IsHSAMRole(Entity account)
		{
			EntityReference accountOwner = (EntityReference)account.Attributes["ownerid"];
			Entity enAccount=_orgService.Retrieve(accountOwner.LogicalName, accountOwner.Id, new ColumnSet("smx_cpqapprovalrole"));
			int approvalrRoleValue =enAccount.Contains("smx_cpqapprovalrole") ? enAccount.GetAttributeValue<OptionSetValue>("smx_cpqapprovalrole").Value : 0;
			if(approvalrRoleValue== 180700006)// HSAM ==180700006
			{
				return true;
			}
			return false;
		}
		//Added by Yash on 14-10-2020 - 58616
		private int opportunityManagerApprovalRole(Entity opportunity)
		{
			EntityReference opportunityOwner = (EntityReference)opportunity.Attributes["ownerid"];
			Entity enUser = _orgService.Retrieve(opportunityOwner.LogicalName, opportunityOwner.Id, new ColumnSet("smx_cpqapprovalrole"));
		    return enUser.Contains("smx_cpqapprovalrole") ? enUser.GetAttributeValue<OptionSetValue>("smx_cpqapprovalrole").Value : 0;
		}
		//End
	}
}
