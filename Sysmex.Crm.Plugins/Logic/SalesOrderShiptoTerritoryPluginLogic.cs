using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sysmex.Crm.Plugins.Logic
{
	class SalesOrderShiptoTerritoryPluginLogic
	{
		private IOrganizationService _orgService;
		private ITracingService _tracer;

		public SalesOrderShiptoTerritoryPluginLogic(IOrganizationService orgService, ITracingService tracer)
		{
			_orgService = orgService;
			_tracer = tracer;
		}
		public void UpdateSaleOrder(Entity saleOrder)
		{
			string zipPostalCode = string.Empty;
			EntityReference equipmentLocation = saleOrder.Contains("smx_lablocationid") ? saleOrder.GetAttributeValue<EntityReference>("smx_lablocationid") : null;
			if(equipmentLocation ==null)
			{
				_tracer.Trace("equipmentLocation is Not exist");
				EntityReference instrumentShipTo = saleOrder.Contains("smx_instrumentshiptoidid") ? saleOrder.GetAttributeValue<EntityReference>("smx_instrumentshiptoidid") : null;
				zipPostalCode = instrumentShipTo != null ? GetZipPostalCode(instrumentShipTo) : string.Empty;
			}
			else
				zipPostalCode = GetZipPostalCode(equipmentLocation);
			_tracer.Trace("Zip Postal Code :" +zipPostalCode);

			Entity territory = zipPostalCode != string.Empty ? GetTerritory(_orgService, zipPostalCode) : null;
			Entity enSaleOrder = new Entity(saleOrder.LogicalName, saleOrder.Id);
			if (territory != null)
			{
				enSaleOrder["smx_shiptoterritory"] = new EntityReference(territory.LogicalName, territory.Id);
				_orgService.Update(enSaleOrder);
				_tracer.Trace("saleOrder is Updated");
			}
			else
			{
				enSaleOrder["smx_shiptoterritory"] = null;
				_orgService.Update(enSaleOrder);
				_tracer.Trace("Territory is Not Exist");
			}
				
		}
		private string GetZipPostalCode(EntityReference address)
		{
			_tracer.Trace("Entered GetZipPostalCode Method");
			Entity enAddress = _orgService.Retrieve(address.LogicalName, address.Id, new ColumnSet("smx_zippostalcode"));
			return enAddress.Contains("smx_zippostalcode") ? enAddress.GetAttributeValue<string>("smx_zippostalcode") : string.Empty;
		}
		private Entity GetTerritory(IOrganizationService service, string zipCode)
		{
			_tracer.Trace("Entered GetTerritory Method");
			Entity territory = null;
			try
			{
				var qe = $@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>
                              <entity name='territory'>
                                <attribute name='name' />
                                <attribute name='territoryid' />
                                <link-entity name='smx_zippostalcode' from='smx_territory' to='territoryid' link-type='inner' alias='ad'>
                                  <filter type='and'>
                                    <condition attribute='smx_name' operator='eq' value='{ zipCode }' />
                                  </filter>
                                </link-entity>
                              </entity>
                            </fetch>";

				EntityCollection territoryList = service.RetrieveMultiple(new FetchExpression(qe));
				if (territoryList.Entities.Count() > 0)
				{
					territory = territoryList.Entities.FirstOrDefault();
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
