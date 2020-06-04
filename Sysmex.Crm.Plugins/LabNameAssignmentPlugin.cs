using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using Sysmex.Crm.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sysmex.Crm.Plugins
{
    public class LabNameAssignmentPlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            IPluginExecutionContext executionContext = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(null);

            if (executionContext.InputParameters.Contains("Target") && executionContext.InputParameters["Target"] is Entity)
            {
                Entity targetEntity = executionContext.InputParameters["Target"] as Entity;
                Entity preEntity = null;
                if (executionContext.PreEntityImages.Contains("PreImage"))
                {
                    preEntity = executionContext.PreEntityImages["PreImage"];
                }

                string accountName = string.Empty;
                string optionsetText = string.Empty;
                EntityReference attributeValue = null;
                OptionSetValue labType = null;
                string zipCode = string.Empty;


                Entity account = null;



                if (targetEntity.Contains("smx_account"))
                {
                    attributeValue = targetEntity.GetAttributeValue<EntityReference>("smx_account");
                }
                else if (preEntity != null && preEntity.Contains("smx_account"))
                {
                    attributeValue = preEntity.GetAttributeValue<EntityReference>("smx_account");
                }

                //if (targetEntity.Contains("smx_zippostalcode"))
                //{
                //    zipCode = targetEntity.GetAttributeValue<string>("smx_zippostalcode");
                //}
                //else if (preEntity != null && preEntity.Contains("smx_zippostalcode"))
                //{
                //    zipCode = preEntity.GetAttributeValue<string>("smx_zippostalcode");
                //}




                account = GetAccountDetails(service, attributeValue);
                accountName = account.GetAttributeValue<string>("name");

                //Entity territoryDetails = new Entity();
                //EntityReference territory = new EntityReference();
                //EntityReference regionalmanager = new EntityReference();

                //if (!string.IsNullOrEmpty(zipCode))
                //{
                //    territoryDetails = GetTerritoryDetails(service, zipCode);
                //    territory = new EntityReference("territory", territoryDetails.Id);
                //    regionalmanager = territoryDetails.Contains("smx_accountmanager") ? territoryDetails.GetAttributeValue<EntityReference>("smx_accountmanager") : null;
                //}
                //else
                //{
                //    territory = account.Contains("territoryid") ? account.GetAttributeValue<EntityReference>("territoryid") : null;
                //    regionalmanager = account.Contains("territory.smx_accountmanager") ? ((EntityReference)account.GetAttributeValue<AliasedValue>("territory.smx_accountmanager").Value) : null;
                //}

                if (targetEntity.Contains("smx_labtype"))
                {
                    labType = targetEntity.GetAttributeValue<OptionSetValue>("smx_labtype");
                    optionsetText = getOptionsetText(service, "smx_lab", "smx_labtype", labType.Value);
                }
                else if (preEntity != null && preEntity.Contains("smx_labtype"))
                {
                    labType = preEntity.GetAttributeValue<OptionSetValue>("smx_labtype");
                    optionsetText = getOptionsetText(service, "smx_lab", "smx_labtype", labType.Value);
                }
                targetEntity["smx_name"] = (optionsetText + " - " + accountName);
                //targetEntity["smx_territory"] = territory;
                //targetEntity["smx_regionalmanager"] = regionalmanager;
            }
        }

        public string getOptionsetText(IOrganizationService service, string entityName, string fieldName, int value)
        {
            try
            {
                RetrieveAttributeRequest attributeRequest = new RetrieveAttributeRequest()
                {
                    EntityLogicalName = entityName,
                    LogicalName = fieldName,
                    RetrieveAsIfPublished = true
                };
                RetrieveAttributeResponse response = (RetrieveAttributeResponse)service.Execute(attributeRequest);
                return ((EnumAttributeMetadata)response.AttributeMetadata).OptionSet.Options.Where(x => x.Value == value).FirstOrDefault().Label.UserLocalizedLabel.Label;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        //public string getAccountName(IOrganizationService service, EntityReference accountref)
        //{
        //    Account accountEntity = service.Retrieve<Account>(accountref.LogicalName, accountref.Id, new Microsoft.Xrm.Sdk.Query.ColumnSet("name"));
        //    if (accountEntity.Contains("name"))
        //    {
        //        return Convert.ToString(accountEntity["name"]);
        //    }
        //    return string.Empty;
        //}

        private Entity GetAccountDetails(IOrganizationService service, EntityReference accountref)
        {
            Entity account = new Entity();
            try
            {
                var qe = $@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                    <entity name='account'>
                                    <attribute name='name' />
                                    <attribute name='territoryid' />
                                    <attribute name='accountid' />
                                    <order attribute='name' descending='false' />
                                    <filter type='and'>
                                        <condition attribute='accountid' operator='eq' uitype='account' value='{accountref.Id}' />
                                    </filter>
                                        <link-entity name='territory' from='territoryid' to='territoryid' visible='false' link-type='outer' alias='territory'>
                                            <attribute name='smx_accountmanager' />
                                        </link-entity>
                                    </entity>
                               </fetch>";

                EntityCollection accountList = service.RetrieveMultiple(new FetchExpression(qe));
                if (accountList.Entities.Count() > 0)
                {
                    account = accountList.Entities.FirstOrDefault();
                }

            }
            catch (Exception)
            {
                return account;
            }
            return account;
        }

        private Entity GetTerritoryDetails(IOrganizationService service, string zipCode)
        {
            Entity territory = new Entity();
            try
            {
                var qe = $@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>
                              <entity name='territory'>
                                <attribute name='name' />
                                <attribute name='territoryid' />
                                <attribute name='smx_accountmanager' />
                                <order attribute='name' descending='false' />
                                <link-entity name='smx_zippostalcode' from='smx_territory' to='territoryid' link-type='inner' alias='ad'>
                                  <filter type='and'>
                                    <condition attribute='smx_name' operator='eq' value='{ zipCode }' />
                                  </filter>
                                </link-entity>
                              </entity>
                            </fetch>";

                EntityCollection accountList = service.RetrieveMultiple(new FetchExpression(qe));
                if (accountList.Entities.Count() > 0)
                {
                    territory = accountList.Entities.FirstOrDefault();
                }

            }
            catch (Exception)
            {
                return territory;
            }
            return territory;
        }

    }
}
