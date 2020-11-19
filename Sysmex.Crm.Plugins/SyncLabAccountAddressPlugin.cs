using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using SonomaPartners.Crm.Toolkit.Plugins;
using Sysmex.Crm.Plugins.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sysmex.Crm.Plugins
{
    public class SyncLabAccountAddressPlugin : PluginBase
    {
        public override void OnExecute(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetPluginExecutionContext();
            var orgService = serviceProvider.CreateOrganizationServiceAsCurrentUser();
            var tracer = serviceProvider.GetTracingService();

            string businessUnitName = string.Empty;

            if (context.Depth > 1)
            {
                tracer.Trace("SyncLabAccountAddressPlugin running twice, exit out.");
                return;
            }

            var target = context.GetTargetEntity();

            var logic = new SyncLabAccountAddressLogic(orgService, tracer);
            var preImage = context.PreEntityImages.Contains("PreImage") ? context.PreEntityImages["PreImage"] : null;
            if (target.LogicalName.ToLower() == "smx_lab")
            {
                logic.SetAddressTypeToShipTo(target);
            }
            if (context.MessageName.ToLower() == "create")
            {
                //two cases has address/doesn't have address
                if ((target.LogicalName.ToLower() == "smx_lab" && target.GetAttributeValue<EntityReference>("smx_labaddress") != null) || (target.LogicalName.ToLower() == "account" && target.GetAttributeValue<EntityReference>("smx_address") != null))
                {
                    //copy
                    logic.CopyAddressFields(target, null);
                }
                else if ((target.LogicalName.ToLower() == "smx_lab" && target.GetAttributeValue<EntityReference>("smx_labaddress") == null) || (target.LogicalName.ToLower() == "account" && target.GetAttributeValue<EntityReference>("smx_address") == null))
                {
                    //Added by Yash on 19-06-2020                     
                    if (target.LogicalName.ToLower() == "account")
                    {
                        businessUnitName = logic.getUserBusinessUnit(context.InitiatingUserId);
                        tracer.Trace("Business Unit Name " + businessUnitName);
                    }
                    //End

                    //create                    
                    logic.CreateAddressRecord(target, businessUnitName);
                }
            }
            else if (preImage == null){
                tracer.Trace("No image associated with context. Exiting.");
                return;
            }
            else if (context.MessageName.ToLower() == "update")
            {
                //two cases changing address, changing address fields,
                logic.CopyAddressFields(target, preImage);

                //Added by Yash on 19-06-2020  
                businessUnitName = logic.getUserBusinessUnit(context.InitiatingUserId);
                tracer.Trace("Business Unit Name " + businessUnitName);
				//if (target.LogicalName.ToLower() == "account" && businessUnitName == "Latin America")
				//{
				//	return;
				//}
				//End

				tracer.Trace("calling PopulateTerritoryAndRegionalManager");
				logic.PopulateTerritoryAndRegionalManager(target, orgService, businessUnitName);
				tracer.Trace(" PopulateTerritoryAndRegionalManager executed");
            }
            if (target.LogicalName.ToLower() == "account" && (target.Contains("address1_postalcode") || target.Contains("smx_countrysap")))
            {
                //Added by Yash on 19-06-2020  
                businessUnitName = logic.getUserBusinessUnit(context.InitiatingUserId);
                tracer.Trace("Business Unit Name " + businessUnitName);
                if (businessUnitName == "Latin America")
                {
                    return;
                }
                //End

                logic.PopulateFieldsBasedOnAddressZipCode(target, context.MessageName.ToLower());
            }
        }
    }
}