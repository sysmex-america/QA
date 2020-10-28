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
    class AutoPopulateICNDateLogic
    {
        private IOrganizationService _orgService;
        private ITracingService _trace;

        public AutoPopulateICNDateLogic(IOrganizationService orgService, ITracingService trace)
        {
            _orgService = orgService;
            _trace = trace;
        }

        public void UpdateImplementationProductOnShipdateUpdate(smx_instrument instrument)
        {
            _trace.Trace($"Start {nameof(UpdateImplementationProductOnShipdateUpdate)}");
            var records = GetRelatedImplementationProductForShipdateUpdate(instrument.Id);
            if (records.Count() == 0)
            {
                _trace.Trace("Could not find related implentation product for the instrument");
                return;
            }

            foreach (smx_implementationproduct s in records)
            {
                UpdateImplementationProductFields(s.Id, instrument.smx_ShipDate, s.smx_StandardofWork, s.smx_ServiceConfirmedInstallDate);
            }
            _trace.Trace($"End {nameof(UpdateImplementationProductOnShipdateUpdate)}");

        }

        private IEnumerable<smx_implementationproduct> GetRelatedImplementationProductForShipdateUpdate(Guid instrumentId)
        {
			_trace.Trace($"Start {nameof(GetRelatedImplementationProductForShipdateUpdate)}");
            var fetch = $@"
                <fetch version='1.0' output-format='xml - platform' mapping='logical' distinct='true'>
					<entity name='smx_implementationproduct'>
						<attribute name = 'smx_implementationproductid' />
						<attribute name = 'smx_serviceconfirmedinstalldate' />
						<attribute name = 'smx_standardofwork' />
						<link-entity name='smx_instrument' from='smx_instrumentid' to='smx_instrumentid' link-type='inner' alias='ab'>
							<filter type = 'and'>
									<condition attribute = 'smx_instrumentid' operator= 'eq' value = '{instrumentId}' />
							</filter>
						</link-entity>
					</entity>
                </fetch>";
            return _orgService.RetrieveMultiple(new FetchExpression(fetch))
                    .Entities
                    .Select(s => s.ToEntity<smx_implementationproduct>())
                    .ToList();
        }

        private void UpdateImplementationProductFields(Guid entityId, DateTime? shipDate, string standardOfWork, DateTime? serviceConfirmedInstalldate)
        {
			_trace.Trace($"Start {nameof(UpdateImplementationProductFields)}");
			var doUpdate = false;
            smx_implementationproduct updateEntity = new smx_implementationproduct();
			updateEntity.Id = entityId;
			updateEntity.smx_ICNComplete = true;

			_trace.Trace($"Id {updateEntity.Id}");
			_trace.Trace($"SOW: {standardOfWork}");
			_trace.Trace($"Ship Date: {shipDate}");
			_trace.Trace($"Install Date: {serviceConfirmedInstalldate}");

			if ((standardOfWork == "Upon Shipment" || standardOfWork == "Upon Shipment.") && shipDate.HasValue)
            {
				_trace.Trace("Set date to Ship Date");
				doUpdate = true;
                updateEntity.smx_ICNDate = shipDate;
            }
            else if ((standardOfWork == "Upon Delivery" || standardOfWork == "Upon Delivery.") && shipDate.HasValue && serviceConfirmedInstalldate.HasValue)
            {
				_trace.Trace("Set date to Serv. Confirmed Install");
				doUpdate = true;
                updateEntity.smx_ICNDate = serviceConfirmedInstalldate;
            }

			if (doUpdate)
			{
				_orgService.Update(updateEntity.ToEntity<Entity>());
			}

			_trace.Trace($"End {nameof(UpdateImplementationProductFields)}");
		}

        public void UpdateImplementationProduct(smx_implementationproduct implementationProduct)
        {
            _trace.Trace($"Start {nameof(UpdateImplementationProductFields)}");
            var records = GetRelatedInstrument(implementationProduct.Id);
            if (records.Count() == 0)
            {
                _trace.Trace("Could not find related implentation product for the instrument");
                return;
            }

            foreach (var instrument in records)
            {
                UpdateImplementationProductFields(implementationProduct.Id, instrument.smx_ShipDate,
                    implementationProduct.smx_StandardofWork, implementationProduct.smx_ServiceConfirmedInstallDate);
            }

            _trace.Trace($"End {nameof(UpdateImplementationProductFields)}");

        }

        private IEnumerable<smx_instrument> GetRelatedInstrument(Guid instrumentId)
        {
            var fetch = $@"
                <fetch version='1.0' output-format='xml - platform' mapping='logical' distinct='true'>
					<entity name='smx_instrument'>
						<attribute name = 'smx_instrumentid' />
						<attribute name = 'smx_shipdate' />
						  <link-entity name='smx_implementationproduct' from='smx_instrumentid' to='smx_instrumentid' link-type='inner' alias='improd'>
							<attribute name = 'smx_serviceconfirmedinstalldate' />
							<filter type = 'and'>
								<condition attribute = 'smx_implementationproductid' operator= 'eq' value = '{instrumentId}' />
							</filter>
						 </link-entity>
						<filter type = 'and'>
							<condition attribute = 'smx_shipdate' operator='not-null' />
						</filter>
					</entity>
                </fetch>";
            return _orgService.RetrieveMultiple(new FetchExpression(fetch))
                    .Entities.Select(s => s.ToEntity<smx_instrument>());
        }


    }
}
