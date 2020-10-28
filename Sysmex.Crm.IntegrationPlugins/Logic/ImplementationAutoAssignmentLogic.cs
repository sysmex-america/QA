using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Sysmex.Crm.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sysmex.Crm.IntegrationPlugins.Logic
{
	public class ImplementationAutoAssignmentLogic
	{
		private IOrganizationService _orgService;
		private ITracingService _trace;

		public ImplementationAutoAssignmentLogic(IOrganizationService orgService, ITracingService trace)
		{
			_orgService = orgService;
			_trace = trace;
		}

		public void SetOvRep(smx_implementation implementation)
		{
			var implementationProducts = GetImplmentationProducts(implementation.Id);
			var flow = GetFlow(implementationProducts);
			var wam = GetWAM(implementation.smx_WAMSite, implementation.smx_WAMConnects);
			var ovRep = EvaluateRule(implementation.smx_StandalongXPPochiOrder, wam, flow, implementation.smx_StateId);

			UpdateRecords(implementation.Id, ovRep, flow, implementation.OwnerId, implementationProducts);
		}

		private IEnumerable<Entity> GetImplmentationProducts(Guid implementationId)
		{
			var fetch = $@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
							  <entity name='smx_implementationproduct'>
								<attribute name='smx_implementationproductid' />
								<filter type='and'>
								  <condition attribute='smx_implementationid' operator='eq' value='{implementationId}' />
								  <condition attribute='smx_classification' operator='eq' value='180700008' />
								</filter>
							  </entity>
							</fetch>";

			return _orgService.RetrieveMultiple(new FetchExpression(fetch)).Entities;
		}

		public void SetOvRep(smx_implementationproduct implmentationProduct)
		{
			var implementation = GetImplementation(implmentationProduct.smx_ImplementationId);
			SetOvRep(implementation);
		}

		private void UpdateRecords(Guid id, EntityReference ovRep, bool flow, EntityReference currentOVRep, IEnumerable<Entity> implementationProducts)
		{
			if (ovRep == null)
			{
				ovRep = GetImplementationTeam();
			}

			if (currentOVRep == null || ovRep.Id != currentOVRep.Id)
			{
				var implementation = new smx_implementation();
				implementation.Id = id;
				implementation.OwnerId = ovRep;
				_orgService.Update(implementation.ToEntity<Entity>());

				if (flow)
				{
					foreach (var product in implementationProducts)
					{
						var implementationProduct = new smx_implementationproduct();
						implementationProduct.Id = product.Id;
						implementationProduct.OwnerId = ovRep;
						_orgService.Update(implementation.ToEntity<Entity>());
					}
				}
			}
		}

		private EntityReference GetImplementationTeam()
		{
			_trace.Trace("* BEGIN GetImplementationTeam method *");

			EntityReference implementationTeam;

			var teamNameToSearchFor = "Implementation Team";

			var fetch = $@"
                    <fetch>
                      <entity name='team'>
                        <attribute name='teamid' />
                        <order attribute='name' descending='false' />
                        <filter type='and'>
                          <condition attribute='name' operator='eq' value='{teamNameToSearchFor}' />
                        </filter>
                      </entity>
                    </fetch>";

			var result = _orgService.RetrieveMultiple(new FetchExpression(fetch));

			if (result.Entities.Count == 1)
			{
				_trace.Trace($"GetImplementationTeam method: team {result.Entities[0].Id} found");
				implementationTeam = new EntityReference("team", result.Entities[0].Id);
			}
			else
			{
				_trace.Trace("*GetImplementationTeam method: team not found");
				implementationTeam = null;
			}

			_trace.Trace("* END/RETURN GetImplementationTeam method");
			return implementationTeam;
		}

		private bool? GetWAM(OptionSetValue wamSite, OptionSetValue wamConnects)
		{
			return (wamSite?.Value == (int)smx_yesno.Yes || wamConnects?.Value == (int)smx_yesno.Yes);
		}

		private smx_implementation GetImplementation(EntityReference implementation)
		{
			if (implementation != null)
			{
				var columnSet = new ColumnSet("smx_stateid", "smx_wamsite", "smx_wamconnects", "smx_standalongxppochiorder", "ownerid");
				return _orgService.Retrieve(implementation.LogicalName, implementation.Id, columnSet).ToEntity<smx_implementation>();
			}

			return null;
		}

		private EntityReference EvaluateRule(bool? standAlone, bool? wam, bool? flow, EntityReference state)
		{
			string standAloneClause = "", wamClause = "", flowClause = "", stateClause = "";
			var ignoreState = false;
			if (flow.HasValue) {
				ignoreState = flow.Value;
				flowClause = $@"<condition attribute='smx_flowcytometry' operator='eq' value='{(flow.Value ? '1': '0')}' />";
			}
			if (standAlone.HasValue)
			{
				ignoreState = ignoreState ? ignoreState : standAlone.Value;
				standAloneClause = $@"<condition attribute='smx_standalonexppochiorder' operator='eq' value='{(standAlone.Value ? '1' : '0')}' />";
			}
			if (wam.HasValue)
			{
				ignoreState = ignoreState ? ignoreState : wam.Value;
				wamClause = $@"<condition attribute='smx_wamsite' operator='eq' value='{(wam.Value ? '1' : '0')}' />";
			}
			if (state != null && ignoreState == false)
			{
				stateClause = $@"<link-entity name='smx_smx_implementationassignmentrule_smx_st' from='smx_implementationassignmentruleid' to='smx_implementationassignmentruleid' visible='false' intersect='true'>
									  <filter type='and'>
										<condition attribute='smx_stateid' operator='eq' value= '{state.Id}' />
									  </filter>
									</link-entity>";
			}
			else
			{
				stateClause = $@"<link-entity name='smx_smx_implementationassignmentrule_smx_st' from='smx_implementationassignmentruleid' to='smx_implementationassignmentruleid' visible='false' intersect='true' link-type='outer'>
									  <link-entity name='smx_state' from='smx_stateid' to='smx_stateid' alias='state' link-type='outer'>
										<attribute name='smx_stateid' />
									  </link-entity>                                      
									</link-entity>
									<filter type = 'and'> 
										 <condition attribute = 'smx_stateid' operator= 'null' entityname = 'state' />
									</filter>";
			}

			var fetch = $@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
							  <entity name='smx_implementationassignmentrule'>
								<attribute name='smx_implementationassignmentruleid' />
								<attribute name='smx_ovrepid' />
								<attribute name='smx_sortorder' />
								<order attribute='smx_sortorder' descending='false' />
								<filter type='and'>
								  {flowClause}
								  {standAloneClause}
								  {wamClause}
								  <condition attribute='statecode' operator='eq' value='0' />
								</filter>
                                {stateClause}
							  </entity>
							</fetch>";

			return _orgService.RetrieveMultiple(new FetchExpression(fetch))
								.Entities
								.Select(s => s.GetAttributeValue<EntityReference>("smx_ovrepid"))
								.FirstOrDefault();
		}

		private bool GetFlow(IEnumerable<Entity> implementationProducts)
		{
			return implementationProducts.Count() > 0;
		}
	}
}