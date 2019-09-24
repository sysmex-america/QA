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
    public class UpdateDemoEvalDirectorsLogic
    {
        private readonly IPluginExecutionContext _context;
        private readonly IOrganizationService _orgService;
        private readonly ITracingService _trace;

        public UpdateDemoEvalDirectorsLogic(IPluginExecutionContext context, IOrganizationService orgService, ITracingService trace)
        {
            _context = context;
            _orgService = orgService;
            _trace = trace;
        }

        public void ExecuteDemoEvalLogic(Entity demoEval, Entity preImage)
        {
            _trace.Trace($"Entered: {MethodBase.GetCurrentMethod().Name}");
            OptionSetValue evaluationLevel = null;

            var status = demoEval.Contains("statuscode") ? demoEval.GetAttributeValue<OptionSetValue>("statuscode") : null;
            if (status != null)
            {
                _trace.Trace("Status Reason was updated, checking value...");
                if (status.Value == (int)smx_demoeval_statuscode.Approved)
                {
                    _trace.Trace("Status Reason was set to Approved, skipping logic.");
                    return;
                }
            }

            if (_context.MessageName.ToLower() == "update")
            {
                evaluationLevel = demoEval.Contains("smx_evaluationlevel") ? demoEval.GetAttributeValue<OptionSetValue>("smx_evaluationlevel") : null;
                UpdateEvaluationLevelLogic(demoEval, preImage, evaluationLevel);
                return;
            }

            var territoryRef = RetrieveTerritoryReference(demoEval);
            if (_context.MessageName.ToLower() == "create")
            {
                PopulateConfigurationFields(demoEval);

                evaluationLevel = demoEval.GetAttributeValue<OptionSetValue>("smx_evaluationlevel");
                if (evaluationLevel == null || evaluationLevel.Value == (int)smx_demoeval_smx_evaluationlevel.Low)
                {
                    _trace.Trace("Evaluation Level was unpopulated or Low, not populating National Sales Director or Area Zone Directory.");
                }
                else
                {
                    demoEval["smx_nationalsalesdirector"] = RetrieveNationalSalesDirector();
                    demoEval["smx_areazonedirector"] = RetrieveAreaZoneDirector(territoryRef.Id);
                }
            }
            
            demoEval["smx_regionalsalesdirector"] = RetrieveRegionalSalesDirector(territoryRef.Id);
        }

        public void ExecuteAccountLogic(Entity account, Entity preImage)
        {
            // Kickout if Account Type = GPO or IHN
            if (account.Attributes.Contains("smx_accounttype") &&
                new[] { 180700004, 180700005 }.Contains(account.GetAttributeValue<OptionSetValue>("smx_accounttype").Value))
            {
                return;
            }

            _trace.Trace($"Entered: {MethodBase.GetCurrentMethod().Name}");
            var preTerritoryRef = preImage.GetAttributeValue<EntityReference>("territoryid");
            var territoryRef = account.GetAttributeValue<EntityReference>("territoryid");

            if (preTerritoryRef != null && territoryRef != null && preTerritoryRef.Id.Equals(territoryRef.Id))
            {
                _trace.Trace("Territory was not updated, returning.");
                return;
            }

            if (territoryRef == null)
            {
                _trace.Trace("Territory was null, returning.");
                return;
            }
            
            var areaZoneDirector = RetrieveAreaZoneDirector(territoryRef.Id);
            var regionalSalesDirector = RetrieveRegionalSalesDirector(territoryRef.Id);

            var assosciatedDemoEvals = RetrieveAssociatedDemoEvals(account.Id);
            foreach(var demoEval in assosciatedDemoEvals)
            {
                var updateDemoEval = new Entity("smx_demoeval")
                {
                    Id = demoEval.Id,
                    ["smx_areazonedirector"] = areaZoneDirector,
                    ["smx_regionalsalesdirector"] = regionalSalesDirector
                };

                _orgService.Update(updateDemoEval);
            }
        }

        private EntityReference RetrieveTerritoryReference(Entity demoEval)
        {
            _trace.Trace($"Entered: {MethodBase.GetCurrentMethod().Name}");

            _trace.Trace("Retrieving Territory record from Account and populating Area Zone Director and Regional Sales Director.");
            var accountRef = demoEval.GetAttributeValue<EntityReference>("smx_account");
            if (accountRef == null)
            {
                _trace.Trace("Account field was unpopulated, returning.");
                return null;
            }

            var territoryRef = _orgService.Retrieve(accountRef.LogicalName, accountRef.Id, new ColumnSet("territoryid")).GetAttributeValue<EntityReference>("territoryid");
            if (territoryRef == null)
            {
                _trace.Trace("Territory was not populated on the Account record, returning.");
                return null;
            }

            return territoryRef;
        }

        private void UpdateEvaluationLevelLogic(Entity demoEval, Entity preImage, OptionSetValue evaluationLevel)
        {
            _trace.Trace($"Entered: {MethodBase.GetCurrentMethod().Name}");
            if (evaluationLevel == null)
            {
                _trace.Trace("Evaluation Level was unpopulated, unpopulating Area Zone Director and National Sales Director.");
                demoEval["smx_areazonedirector"] = null;
                demoEval["smx_nationalsalesdirector"] = null;
                return;
            }

            _trace.Trace("Evaluation Level was updated, checking value...");
            var preEvaluationLevel = preImage.GetAttributeValue<OptionSetValue>("smx_evaluationlevel");
            if (preEvaluationLevel != null && preEvaluationLevel.Value == evaluationLevel.Value)
            {
                _trace.Trace("Evaluation Level was not changed, returning.");
                return;
            }
            else if (evaluationLevel.Value == (int)smx_demoeval_smx_evaluationlevel.High)
            {
                _trace.Trace("Evaluation Level was set to High, populating Area Zone Director and National Sales Director.");
                demoEval.MergeAttributes(preImage);

                var territory = RetrieveTerritoryReference(demoEval);
                demoEval["smx_areazonedirector"] = RetrieveAreaZoneDirector(territory.Id);
                demoEval["smx_nationalsalesdirector"] = RetrieveNationalSalesDirector();
            }
            else if (evaluationLevel.Value == (int)smx_demoeval_smx_evaluationlevel.Low)
            {
                _trace.Trace("Evaluation Level was set to Low, unpopulating Area Zone Director and National Sales Director.");
                demoEval["smx_areazonedirector"] = null;
                demoEval["smx_nationalsalesdirector"] = null;
            }
        }

        private void PopulateConfigurationFields(Entity demoEval)
        {
            _trace.Trace("Create step entered, retrieving configuration record and populating Nationsal Sales Director, TSG, and SVC Ops.");
            var configuration = RetrieveConfigurationRecord();
            if (configuration == null)
            {
                _trace.Trace("No configuration record was found, returning.");
                return;
            }
            
            _trace.Trace("Retrieving user's business unit to determine which fields to pull TSG and SVC Ops from.");
            if(_context.BusinessUnitId == null || _context.BusinessUnitId.Equals(Guid.Empty))
            {
                _trace.Trace("No Business Unit was found for current user, returning.");
                return;
            }

            var businessUnit = _orgService.Retrieve("businessunit", _context.BusinessUnitId, new ColumnSet("name"));
            if (businessUnit == null)
            {
                _trace.Trace("No Business Unit was found for current user, returning.");
                return;
            }

            var businessUnitName = businessUnit.GetAttributeValue<string>("name");
            switch (businessUnitName)
            {
                case "United States":
                    demoEval["smx_tsg"] = configuration.GetAttributeValue<EntityReference>("smx_tsg");
                    demoEval["smx_svcops"] = configuration.GetAttributeValue<EntityReference>("smx_svcops");
                    break;
                case "Canada":
                    demoEval["smx_tsg"] = configuration.GetAttributeValue<EntityReference>("smx_tsgcanada");
                    demoEval["smx_svcops"] = configuration.GetAttributeValue<EntityReference>("smx_svcopscanada");
                    break;
                case "Latin America":
                    demoEval["smx_tsg"] = configuration.GetAttributeValue<EntityReference>("smx_tsgla");
                    demoEval["smx_svcops"] = configuration.GetAttributeValue<EntityReference>("smx_svcopsla");
                    break;
                default:
                    _trace.Trace($"No case was found for Business Unit Name {businessUnitName}. Defaulting to US fields.");
                    demoEval["smx_tsg"] = configuration.GetAttributeValue<EntityReference>("smx_tsg");
                    demoEval["smx_svcops"] = configuration.GetAttributeValue<EntityReference>("smx_svcops");
                    break;
            }
        }

        private EntityReference RetrieveNationalSalesDirector()
        {
            _trace.Trace($"Entered: {MethodBase.GetCurrentMethod().Name}");

            var configuration = RetrieveConfigurationRecord();
            if (configuration == null)
            {
                _trace.Trace("No configuration record was found, returning.");
                return null;
            }

            return configuration.GetAttributeValue<EntityReference>("smx_ussalesdirector");
        }

        private Entity RetrieveConfigurationRecord()
        {
            _trace.Trace($"Entered: {MethodBase.GetCurrentMethod().Name}");
            var fetchXml = $@"
                <fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                    <entity name='smx_sysmexconfig'>
                        <attribute name='smx_ussalesdirector' />
                        <attribute name='smx_svcops' />
                        <attribute name='smx_tsg' />
                        <attribute name='smx_svcopscanada' />
                        <attribute name='smx_tsgcanada' />
                        <attribute name='smx_svcopsla' />
                        <attribute name='smx_tsgla' />
                    </entity>
                </fetch>";

            var results = _orgService.RetrieveMultiple(new FetchExpression(fetchXml)).Entities;
            if (results.Count == 0)
            {
                return null;
            }

            return results.First();
        }

        private EntityReference RetrieveAreaZoneDirector(Guid id)
        {
            _trace.Trace($"Entered: {MethodBase.GetCurrentMethod().Name}");
            var name = _orgService.Retrieve("territory", id, new ColumnSet("smx_region")).GetAttributeValue<EntityReference>("smx_region").Name; 

            var fetchXml = $@"
                <fetch>
                    <entity name='systemuser'>
                        <link-entity name='territory' from='smx_regionalmanager' to='systemuserid' alias='ac'>
                            <filter type='and'>
                                <condition attribute='name' operator='eq' value='{name}' />
                            </filter>
                        </link-entity>
                    </entity>
                </fetch>";

            var results = _orgService.RetrieveMultiple(new FetchExpression(fetchXml)).Entities;
            if (results.Count == 0)
            {
                return null;
            }

            return results.First().ToEntityReference();
        }

        private EntityReference RetrieveRegionalSalesDirector(Guid id)
        {
            _trace.Trace($"Entered: {MethodBase.GetCurrentMethod().Name}");
            var fetchXml = $@"
                <fetch>
                    <entity name='systemuser'>
                        <link-entity name='territory' from='smx_regionalmanager' to='systemuserid' alias='ac'>
                            <filter type='and'>
                                <condition attribute='smx_type' operator='eq' value='180700000' />
                                <condition attribute='territoryid' operator='eq' value='{id}' />
                            </filter>
                        </link-entity>
                    </entity>
                </fetch>";

            var results = _orgService.RetrieveMultiple(new FetchExpression(fetchXml)).Entities;
            if(results.Count == 0)
            {
                return null;
            }

            return results.First().ToEntityReference();
        }

        private IEnumerable<Entity> RetrieveAssociatedDemoEvals(Guid id)
        {
            _trace.Trace($"Entered: {MethodBase.GetCurrentMethod().Name}");
            var fetchXml = $@"
                <fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                    <entity name='smx_demoeval'>
                        <filter type='and'>
                            <condition attribute='smx_account' operator='eq' value='{id}' />
                            <condition attribute='statuscode' operator='ne' value='180700001' />
                        </filter>
                    </entity>
                </fetch>";

            return _orgService.RetrieveMultiple(new FetchExpression(fetchXml)).Entities;
        }
    }
}
