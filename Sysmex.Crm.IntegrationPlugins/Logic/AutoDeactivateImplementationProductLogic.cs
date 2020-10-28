using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.Collections.Generic;
using SonomaPartners.Crm.Toolkit;
using System;
using System.Text;
using System.Linq;
using Sysmex.Crm.Model;

namespace Sysmex.Crm.IntegrationPlugins.Logic
{
    class AutoDeactivateImplementationProductLogic
    {
        private IOrganizationService _orgService;
        private ITracingService _trace;

        public AutoDeactivateImplementationProductLogic(IOrganizationService orgService, ITracingService trace)
        {
            _orgService = orgService;
            _trace = trace;
        }

        public void UpdateImplementationProductOnOvItemStatusUpdate(smx_implementationproduct product)
        {
            _trace.Trace($"Start {nameof(UpdateImplementationProductOnOvItemStatusUpdate)}");

            if (product.smx_ovitemstatus == smx_implementationproduct_smx_ovitemstatus.Delete)
            {
                UpdateImplementationProductField(product.Id);
            }
            _trace.Trace($"End {nameof(UpdateImplementationProductOnOvItemStatusUpdate)}");

        }

        private void UpdateImplementationProductField(Guid entityId)
        {
            _trace.Trace($"Start {nameof(UpdateImplementationProductField)}");
            smx_implementationproduct updateEntity = new smx_implementationproduct();
            updateEntity.Id = entityId;
            updateEntity.statecode = smx_implementationproductState.Inactive;

            _trace.Trace($"Id {updateEntity.Id}");

            _orgService.Update(updateEntity.ToEntity<Entity>());

            _trace.Trace($"End {nameof(UpdateImplementationProductField)}");
        }
		
		public void ClearImplementationProduct(smx_implementationproduct product)
		{
			_trace.Trace($"Start {nameof(ClearImplementationProduct)}");
			if (product.statecode.HasValue && product.statuscode.Value == smx_implementationproduct_statuscode.Inactive)
			{
				_trace.Trace("Implementation is deactivated; exit early");
				return;
			}

			smx_implementationproduct updateEntity = new smx_implementationproduct();
			updateEntity.Id = product.Id;
			updateEntity.smx_ovitemstatus = null;
			_orgService.Update(updateEntity.ToEntity<Entity>());

			_trace.Trace($"End {nameof(ClearImplementationProduct)}");
		}
	}
}
