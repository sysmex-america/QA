using System;
using Microsoft.Xrm.Sdk;
using Sysmex.Crm.Model;

namespace Sysmex.Crm.IntegrationPlugins.Logic
{
	internal class SetImplementationFieldsOnUpdateLogic
	{
		private IOrganizationService service;
		private ITracingService tracer;

		public SetImplementationFieldsOnUpdateLogic(IOrganizationService service, ITracingService tracer)
		{
			this.service = service;
			this.tracer = tracer;
		}

		public void UpdateFields(smx_implementation implementation)
		{
			if (ValidStatus(implementation.statecode))
			{
				smx_implementation_statuscode statusReason  = smx_implementation_statuscode.Unassigned;
				bool flagIsSet = false;
				if (implementation.smx_ProjectManagerId == null && (implementation.OwnerId != null && implementation.OwnerId.LogicalName.ToLower() == Team.EntityLogicalName))
				{
					flagIsSet = true;
				}
				else if (implementation.smx_TotalProductsICNRemainingRollup.HasValue == true && implementation.smx_TotalProductsICNRemainingRollup.Value > 0)
				{
					statusReason = smx_implementation_statuscode.InProcess;
					flagIsSet = true;
				}
				else if (implementation.smx_TotalProductsICNRemainingRollup.HasValue == true && implementation.smx_TotalProductsICNRemainingRollup.Value == 0)
				{
					statusReason = smx_implementation_statuscode.ICNComplete;
					flagIsSet = true;
				}

				if(flagIsSet)
				{
					var record = new smx_implementation();
					record.Id = implementation.Id;
					record.statuscode = statusReason;
					service.Update(record.ToEntity<Entity>());
				}
			}
		}

	private bool ValidStatus(smx_implementationState? implementationState)
	{
		if (implementationState == smx_implementationState.Inactive)
		{
			tracer.Trace("Inactive Status.  Exit Early");
			return false;
		}

		return true;
	}
}
}