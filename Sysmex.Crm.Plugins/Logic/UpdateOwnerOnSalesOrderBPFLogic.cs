using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Sysmex.Crm.Model;
using Sysmex.Crm.Plugins.Logic;
using SonomaPartners.Crm.Toolkit;
using System.Linq;
using System;

namespace Sysmex.Crm.Plugins
{
    class UpdateOwnerOnSalesOrderBPFLogic : LogicBase
    {
        public UpdateOwnerOnSalesOrderBPFLogic(IOrganizationService orgService, ITracingService tracer)
            : base(orgService, tracer)
        { }

        public void UpdateOwner(smx_salesorderbusinessprocess salesOrderBP)
        {
            if (salesOrderBP == null)
            {
                _tracer.Trace("salesOrderBP is NULL!!!");
            }

            if (salesOrderBP.bpf_smx_salesorderid == null)
            {
                _tracer.Trace("salesOrderBP.bpf_smx_salesorderid is NULL!!!");
            }

            if (salesOrderBP.ActiveStageId == null)
            {
                _tracer.Trace("salesOrderBP.ActiveStageId is NULL!!!");
            }

            var updatedSalesOrder = new smx_salesorder()
            {
                Id = salesOrderBP.bpf_smx_salesorderid.Id
            };

            var stage = _orgService.Retrieve<ProcessStage>(ProcessStage.EntityLogicalName, salesOrderBP.ActiveStageId.Id, new ColumnSet("stagename"));
            switch (stage.StageName)
            {
                case "Customer Master":
                    {
                        var teamRef = FindTeamByName("Customer Master");
                        if(teamRef != null)
                        {
                            updatedSalesOrder.OwnerId = teamRef;
                            _orgService.Update(updatedSalesOrder);
                        }
                        break;
                    }
                case "Sales Contract Analyst Review":
                    {
                        var salesContractAnalysisRef = RetrieveSalesContractAnalysis(salesOrderBP.bpf_smx_salesorderid.Id);
                        if (salesContractAnalysisRef != null)
                        {
                            updatedSalesOrder.OwnerId = salesContractAnalysisRef;
                            _orgService.Update(updatedSalesOrder);
                        }
                        break;
                    }
                case "Contract Team Review - Waiting For Assignment":
                    {
                        var teamRef = FindTeamByName("Contract Team");
                        if (teamRef != null)
                        {
                            updatedSalesOrder.OwnerId = teamRef;
                            _orgService.Update(updatedSalesOrder);
                        }
                        break;
                    }
            }
        }

        private EntityReference RetrieveSalesContractAnalysis(Guid salesOrderId)
        {
            var fetch = $@"
              <fetch top='1'>
                <entity name='smx_salesorder'>
                <attribute name='smx_salesorderid' />
                <filter type='and'>
                    <condition attribute='smx_salesorderid' operator='eq' value='{salesOrderId}' />
                </filter>
                <link-entity name='smx_address' from='smx_addressid' to='smx_lablocationid' link-type='inner' alias='ar'>
                    <link-entity name='account' from='accountid' to='smx_account' link-type='inner' alias='as'>
                    <link-entity name='territory' from='territoryid' to='territoryid' link-type='inner' alias='at'>
                        <link-entity name='territory' from='territoryid' to='smx_region' link-type='inner' alias='Territory'>
                        <attribute name='smx_salescontractanalystid' />
                        </link-entity>
                    </link-entity>
                    </link-entity>
                </link-entity>
                </entity>
              </fetch>";

            var results = _orgService.RetrieveMultiple<smx_salesorder>(new FetchExpression(fetch));
            return results.FirstOrDefault()?.GetAliasedAttributeValue<EntityReference>("Territory.smx_salescontractanalystid");
        }

        private EntityReference FindTeamByName(string name)
        {
            var fetch = $@"
                <fetch top='1'>
                  <entity name='team'>
                    <attribute name='teamid' />
                    <filter type='and'>
                      <condition attribute='name' operator='eq' value='{name}' />
                    </filter>
                  </entity>
                </fetch>";

            return _orgService.RetrieveMultiple<Team>(new FetchExpression(fetch)).FirstOrDefault()?.ToEntityReference();
        }
    }
}
