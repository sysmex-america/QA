using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using SonomaPartners.Crm.Toolkit;
using System;
using System.Reflection;

namespace Sysmex.Crm.Plugins.Logic
{
	class LAAccountLastActivityDateLogic
	{
		private readonly IOrganizationService _orgService;
		private readonly ITracingService _trace;

		public LAAccountLastActivityDateLogic(IOrganizationService orgService, ITracingService trace)
		{
			_orgService = orgService;
			_trace = trace;
		}
		public void UpdateLAAccountLastActivityDate(Entity input, Entity preImage)
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
			if (regardingObject == null || (regardingObject.LogicalName != "smx_laaccount" ))
			{
				_trace.Trace("Regarding field was not referencing the correct type of record, returning.");
				return;
			}

			LAAccountValidateAndUpdateDate(regardingObject);
		}

		private void LAAccountValidateAndUpdateDate(EntityReference regardingObject)
		{
			_trace.Trace($"Entered {MethodBase.GetCurrentMethod().Name}");

			EntityReference parentRecordRef = RetrieveParentAccountRecord(regardingObject);
			if (parentRecordRef == null)
			{
				_trace.Trace("Could not determine parent record, returning.");
				return;
			}

			_trace.Trace("Updating LAAccount's Last Activity Date");
			var updateAccount = new Entity(parentRecordRef.LogicalName)
			{
				Id = parentRecordRef.Id,
				["smx_lastactivitydate"] = DateTime.UtcNow
			};

			_orgService.Update(updateAccount);
		}

		private EntityReference RetrieveParentAccountRecord(EntityReference regardingObject)
		{
			if (regardingObject.LogicalName == "smx_laaccount")
			{
				_trace.Trace("Regarding was referencing an LAAccount.");
				return regardingObject;
			}
			return null;
		}
	}
}
