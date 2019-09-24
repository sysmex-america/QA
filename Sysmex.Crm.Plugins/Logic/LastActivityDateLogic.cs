using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using SonomaPartners.Crm.Toolkit;
using System;
using System.Reflection;

namespace Sysmex.Crm.Plugins.Logic
{
    public class LastActivityDateLogic
    {

        private readonly IOrganizationService _orgService;
        private readonly ITracingService _trace;

        public LastActivityDateLogic(IOrganizationService orgService, ITracingService trace)
        {
            _orgService = orgService;
            _trace = trace;
        }

        public void UpdateLastActivityDate(Entity input, Entity preImage)
        {
            _trace.Trace($"Entered {MethodBase.GetCurrentMethod().Name}");
            if (input == null)
            {
                input = preImage;
            }
            else if (preImage != null)
            {
                input.MergeAttributes(preImage);
            }

            var regardingObject = input.GetAttributeValue<EntityReference>("regardingobjectid");
            if (regardingObject == null || (regardingObject.LogicalName != "account" && regardingObject.LogicalName != "contact" && regardingObject.LogicalName != "opportunity"))
            {
                _trace.Trace("Regarding field was not referencing the correct type of record, returning.");
                return;
            }

            ValidateAndUpdateDate(regardingObject);
        }

        private void ValidateAndUpdateDate(EntityReference regardingObject)
        {
            _trace.Trace($"Entered {MethodBase.GetCurrentMethod().Name}");

            EntityReference parentRecordRef = RetrieveParentAccountRecord(regardingObject);
            if (parentRecordRef == null)
            {
                _trace.Trace("Could not determine parent record, returning.");
                return;
            }

            _trace.Trace("Updating Account's Last Activity Date");
            var updateAccount = new Entity(parentRecordRef.LogicalName)
            {
                Id = parentRecordRef.Id,
                ["smx_lastactivitydate"] = DateTime.UtcNow
            };

            _orgService.Update(updateAccount);
        }

        private EntityReference RetrieveParentAccountRecord(EntityReference regardingObject)
        {
            if (regardingObject.LogicalName == "account")
            {
                _trace.Trace("Regarding was referencing an account.");
                return regardingObject;
            }
            else if (regardingObject.LogicalName == "contact")
            {
                _trace.Trace("Regarding was referencing a contact, retreiving parent account.");
                var contact = _orgService.Retrieve(regardingObject.LogicalName, regardingObject.Id, new ColumnSet("parentcustomerid"));
                var parentCustomerRef = contact.GetAttributeValue<EntityReference>("parentcustomerid");
                if (parentCustomerRef == null || parentCustomerRef.LogicalName != "account")
                {
                    _trace.Trace("Contact's Company field was unpopulated or not referencing an account, returning null.");
                    return null;
                }

                return parentCustomerRef;
            }
            else if (regardingObject.LogicalName == "opportunity")
            {
                _trace.Trace("Regarding was referencing an opportunity, retreiving parent account.");
                var opportunity = _orgService.Retrieve(regardingObject.LogicalName, regardingObject.Id, new ColumnSet("parentaccountid"));
                var parentAccountRef = opportunity.GetAttributeValue<EntityReference>("parentaccountid");
                if (parentAccountRef == null)
                {
                    _trace.Trace("Opportunity's Account field was not populated, returning null.");
                    return null;
                }

                return parentAccountRef;
            }

            return null;
        }
    }
}
