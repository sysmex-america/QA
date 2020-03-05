using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using SonomaPartners.Crm.Toolkit.Plugins;
using System;
using System.Linq;

namespace Sysmex.Crm.Plugins
{
    public class LabNameAssignmentPlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetPluginExecutionContext();
            
            var target = context.GetTargetEntity();

            string accountName = null;
            string labTypeName = null;

            var account = target.GetAttributeValue<EntityReference>("smx_account");
            var labType = target.GetAttributeValue<OptionSetValue>("smx_labtype");

            if (labType != null)
            {
                labTypeName = target.FormattedValues.Contains("smx_labtype") ? target.FormattedValues["smx_labtype"] : null;
            }

            if (account == null || labType == null)
            {
                var preImage = context.PreEntityImages.Contains("PreImage") ? context.PreEntityImages["PreImage"] : null;
                if (preImage != null)
                {
                    if (account == null)
                    {
                        account = preImage.GetAttributeValue<EntityReference>("smx_account");
                    }

                    if (labType == null)
                    {
                        labType = preImage.GetAttributeValue<OptionSetValue>("smx_labtype");

                        if (preImage.FormattedValues.Contains("smx_labtype"))
                        {
                            labTypeName = preImage.FormattedValues["smx_labtype"];
                        }
                    }
                }
            }

            if (account != null || labTypeName == null && labType != null)
            {
                var orgService = serviceProvider.CreateOrganizationServiceAsCurrentUser();

                if (account != null)
                {
                    var acc = orgService.Retrieve(account.LogicalName, account.Id, new Microsoft.Xrm.Sdk.Query.ColumnSet("name"));
                    accountName = acc.GetAttributeValue<string>("name");
                }

                if (labTypeName == null && labType != null)
                {
                    RetrieveAttributeRequest retrieveAttributeRequest = new RetrieveAttributeRequest
                    {
                        EntityLogicalName = "smx_lab",
                        LogicalName = "smx_labtype",
                        RetrieveAsIfPublished = true
                    };

                    var retrieveAttributeResponse = (RetrieveAttributeResponse)orgService.Execute(retrieveAttributeRequest);
                    var retrievedPicklistAttributeMetadata = (Microsoft.Xrm.Sdk.Metadata.PicklistAttributeMetadata) retrieveAttributeResponse.AttributeMetadata;
                    labTypeName = retrievedPicklistAttributeMetadata.OptionSet.Options.FirstOrDefault(p => p.Value == labType.Value)?.Label?.UserLocalizedLabel?.Label;
                }
            }

            target["smx_name"] = $"{labTypeName} - {accountName}";
        }
    }
}
