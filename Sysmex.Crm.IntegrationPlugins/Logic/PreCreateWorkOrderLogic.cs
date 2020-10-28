using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.Collections.Generic;
using System;
using Sysmex.Crm.Model;
using System.Linq;
using System.Text;

namespace Sysmex.Crm.IntegrationPlugins.Logic
{
    public class PreCreateWorkOrderLogic
    {
        private IOrganizationService _orgService;
        private ITracingService _trace;

		private WorkOrderNameInfo _workOrderName = new WorkOrderNameInfo();


		public PreCreateWorkOrderLogic(IOrganizationService orgService, ITracingService trace)
        {
            _orgService = orgService;
            _trace = trace;
        }

        public string WorkOrderAutoname(Guid implementationProductGuid)
		{
			_trace.Trace("* BEGIN WorkOrderAutoname method *");

			_trace.Trace("Get Equipment Location Data");
			SetEquipmentLocationData(implementationProductGuid);
			if (_workOrderName.IsEquipmentLocationData == false)
			{
				_trace.Trace("Not Equipment Location; Get ShipTo Data");
				SetShipToData(implementationProductGuid);
			}

			_trace.Trace($"implementationName:{_workOrderName.ImplementationName}");
			_trace.Trace($"implementationCity:{_workOrderName.ImplementationCity}");
			_trace.Trace($"implementationState:{_workOrderName.ImplementationState}");
			_trace.Trace($"productMaterialNumber:{_workOrderName.ProductMaterialNumber}");

			//smx_implementation_smx_instrumentshiptoid> - < smx_implementationproduct_smx_city >, < smx_implementation_ smx_stateid > - <smx_implementationproduct_smx_materialnumber>
			string workOrderName = $@"{_workOrderName.ImplementationName} - {_workOrderName.ImplementationCity}, {_workOrderName.ImplementationState} - {_workOrderName.ProductMaterialNumber}";
			_trace.Trace($"workOrderName:{workOrderName}");

			_trace.Trace("* RETURN WorkOrderAutoname method *");
			return workOrderName;
		}

		private void SetEquipmentLocationData(Guid implementationProductGuid)
		{
			Entity equipmentLocationInfo = RetrieveEquipmentLocation(implementationProductGuid);
			if (equipmentLocationInfo == null) return;

			var address = equipmentLocationInfo.GetAttributeValue<AliasedValue>("address.smx_name") != null ? equipmentLocationInfo.GetAttributeValue<AliasedValue>("address.smx_name").Value?.ToString() : "";
			var number = equipmentLocationInfo.GetAttributeValue<AliasedValue>("implementation.smx_contractnumber") != null ? equipmentLocationInfo.GetAttributeValue<AliasedValue>("implementation.smx_contractnumber").Value?.ToString() : "";
			var name = address + " - " + number;

			_workOrderName.ImplementationName = name;
			_workOrderName.ImplementationCity = equipmentLocationInfo.GetAttributeValue<AliasedValue>("address.smx_city") != null ? equipmentLocationInfo.GetAttributeValue<AliasedValue>("address.smx_city").Value?.ToString() : "";
			_workOrderName.ImplementationState = equipmentLocationInfo.GetAttributeValue<AliasedValue>("state.smx_name") != null ? equipmentLocationInfo.GetAttributeValue<AliasedValue>("state.smx_name").Value?.ToString() : "";
			_workOrderName.ProductMaterialNumber = equipmentLocationInfo.GetAttributeValue<AliasedValue>("product.smx_name") != null ? equipmentLocationInfo.GetAttributeValue<AliasedValue>("product.smx_name").Value?.ToString() : "";
			_workOrderName.IsEquipmentLocationData = equipmentLocationInfo.GetAttributeValue<AliasedValue>("implementation.smx_lablocationid") != null;
		}

		private Entity RetrieveEquipmentLocation(Guid implementationProductGuid)
		{
			_trace.Trace($"* BEGIN {nameof(RetrieveEquipmentLocation)} *");

			var fetchXml = $@"<fetch>
								  <entity name='smx_implementationproduct'>
									<attribute name='smx_implementationproductid' />
									<filter type='and'>
									  <condition attribute='smx_implementationproductid' operator='eq' value='{implementationProductGuid}' />
									</filter>
									<link-entity name='smx_product' from='smx_productid' to='smx_materialnumberid' link-type='outer' alias='product' >
									  <attribute name='smx_name' />
									</link-entity>
									<link-entity name='smx_implementation' from='smx_implementationid' to='smx_implementationid'  link-type='inner' alias='implementation'>
									  <attribute name='smx_name' />
									  <attribute name='smx_lablocationid' />
									  <attribute name='smx_contractnumber' />
									  <link-entity name='smx_address' from='smx_addressid' to='smx_lablocationid' link-type='inner' alias='address'>
										<attribute name='smx_name' />
										<attribute name='smx_city' />
										<link-entity name='smx_state' from='smx_stateid' to='smx_statesap' link-type='outer' alias='state'>
										  <attribute name='smx_name' />
										</link-entity>
									  </link-entity>
									</link-entity>
								  </entity>
								</fetch>";

			var result = _orgService.RetrieveMultiple(new FetchExpression(fetchXml));

			_trace.Trace($"* END/RETURN {nameof(RetrieveEquipmentLocation)}  *");
			return result.Entities.FirstOrDefault();
		}

		private void SetShipToData(Guid implementationProductGuid)
		{
			Entity implementationProductInfo = RetrieveShipToData(implementationProductGuid);
			_workOrderName.ImplementationName = implementationProductInfo.GetAttributeValue<AliasedValue>("implementation.smx_name") != null ? implementationProductInfo.GetAttributeValue<AliasedValue>("implementation.smx_name").Value?.ToString() : "";
			_workOrderName.ImplementationCity = implementationProductInfo.GetAttributeValue<AliasedValue>("implementation.smx_city") != null ? implementationProductInfo.GetAttributeValue<AliasedValue>("implementation.smx_city").Value?.ToString() : "";
			_workOrderName.ImplementationState = implementationProductInfo.GetAttributeValue<AliasedValue>("state.smx_name") != null ? implementationProductInfo.GetAttributeValue<AliasedValue>("state.smx_name").Value?.ToString() : "";
			_workOrderName.ProductMaterialNumber = implementationProductInfo.GetAttributeValue<AliasedValue>("product.smx_name") != null ? implementationProductInfo.GetAttributeValue<AliasedValue>("product.smx_name").Value?.ToString() : "";
		}

		private Entity RetrieveShipToData(Guid implementationProductGuid)
        {
            _trace.Trace("* BEGIN RetrieveImplementationProduct method *");

            var fetchXml = $@"
                <fetch>
                  <entity name='smx_implementationproduct' >
                    <attribute name='smx_implementationproductid' />
                    <filter>
                      <condition attribute='smx_implementationproductid' operator='eq' value='{implementationProductGuid}' />
                    </filter>
                    <link-entity name='smx_product' from='smx_productid' to='smx_materialnumberid' link-type='outer' alias='product' >
                      <attribute name='smx_name' />
                    </link-entity>
                    <link-entity name='smx_implementation' from='smx_implementationid' to='smx_implementationid' link-type='outer' alias='implementation' >
                      <attribute name='smx_city' />
                      <attribute name='smx_name' />
                      <link-entity name='smx_state' from='smx_stateid' to='smx_stateid' link-type='outer' alias='state' >
                        <attribute name='smx_name' />
                      </link-entity>
                    </link-entity> 
                  </entity>
                </fetch>";

            var result = _orgService.RetrieveMultiple(new FetchExpression(fetchXml));

            _trace.Trace("* END/RETURN RetrieveImplementationProduct method *");
			return result.Entities.FirstOrDefault();
        }

		private class WorkOrderNameInfo
		{
			public string ImplementationName = "";
			public string ImplementationCity = "";
			public string ImplementationState = "";
			public string ProductMaterialNumber = "";
			public bool IsEquipmentLocationData = false;
		}
	}
}
