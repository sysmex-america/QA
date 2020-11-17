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
	//Added by Yash on 21-10-2020 - 58646
	public class CASSyncPluginLogic
	{
		private IOrganizationService _orgService;
		private ITracingService _tracer;

		public CASSyncPluginLogic(IOrganizationService orgService, ITracingService tracer)
		{
			_orgService = orgService;
			_tracer = tracer;
		}
		public void UpdateSaleOrder(Entity lab)
		{
			var saleOrders = GetSaleOrderfromLab(lab.Id);
			string labCASPrimary = lab.Contains("smx_casprimary") ? lab.GetAttributeValue<string>("smx_casprimary") : string.Empty;
			if (labCASPrimary == string.Empty)
			{
				_tracer.Trace("no CASPrimery found. Exiting plugin logic.");
				return;
			}
			Entity user = GetUserfromCASPrimery(labCASPrimary);
			if (user == null)
			{
				_tracer.Trace("no user found. Exiting plugin logic.");
				return;
			}
			if (saleOrders == null)
			{
				_tracer.Trace("no saleorder to update. Exiting plugin logic.");
				return;
			}
			
			ExecuteMultipleRequest batchRequest = new ExecuteMultipleRequest()
			{
				Settings = new ExecuteMultipleSettings()
				{
					ContinueOnError = false,
					ReturnResponses = true
				},
				Requests = new OrganizationRequestCollection()
			};

			foreach (Entity saleOrder in saleOrders)
			{
				var record = new Entity(saleOrder.LogicalName)
				{
					Id = saleOrder.Id,
					["smx_cas"] = new EntityReference(user.LogicalName,user.Id)
				};
				batchRequest.Requests.Add(new UpdateRequest() { Target = record });
			}
			try
			{
				var updateResponse = _orgService.Execute(batchRequest);
				_tracer.Trace("Update Batch Request Complete.");
			}
			catch (Exception ex)
			{
				_tracer.Trace(ex.Message);
			}
			_tracer.Trace("Exiting Logic.");

		}
		private DataCollection<Entity> GetSaleOrderfromLab(Guid? labId)
		{
			_tracer.Trace("Started GetSaleOrderfromLab Method");
			EntityCollection saleOrders = new EntityCollection();

			var fetch = $@"
                 <fetch>
                    <entity name='smx_salesorder'>
                      <attribute name='smx_activestage' />
                      <attribute name='smx_salesorderid' />
                       <filter type='and'>
                          <condition attribute='statecode' operator='eq' value='0' />
                          <condition attribute='smx_activestage' operator='ne' value='Order Review Complete' />
                      </filter>
                      <link-entity name='opportunity' from='opportunityid' to='smx_opportunityid' visible='false' link-type='outer' alias='a_659603ffc2f1e811a96c000d3a1d51b4'>
                         <attribute name='estimatedclosedate' />
                     </link-entity>
                     <link-entity name='smx_opportunitylab' from='smx_opportunitylabid' to='smx_opportunitylabid' link-type='inner' alias='aa'>
                         <filter type='and'>
                             <condition attribute='statecode' operator='eq' value='0' />
                        </filter>
                     <link-entity name='smx_lab' from='smx_labid' to='smx_labid' link-type='inner' alias='ab'>
                          <filter type='and'>
                               <condition attribute='smx_casprimary' operator='not-null' />
                               <condition attribute='statecode' operator='eq' value='0' />
                               <condition attribute='smx_labid' operator='eq' value='{labId}' />
                          </filter>
                      </link-entity>
                     </link-entity>
                 </entity>
           </fetch>";
			var result=_orgService.RetrieveMultiple(new FetchExpression(fetch));
			if (result.Entities.Any())
			{
				_tracer.Trace("IS Saleorders Available" + result.Entities.Any());
				return result.Entities;
			}
			else
			{
				_tracer.Trace("No Sale Odres");
				return null;
			}

		}
		private Entity GetUserfromCASPrimery(string CASPrimary)
		{
			Entity user = null;
			string userName = "%" + CASPrimary + "%";
			try
			{
				var fetch = $@"<fetch>
                             <entity name='systemuser'>
                               <attribute name='fullname' />
                               <attribute name='systemuserid' />
                               <attribute name='domainname' />
                                  <filter type='and'>
                                       <condition attribute='domainname' operator='like' value='{userName}' />
                                  </filter>
                             </entity>
                       </fetch>";
				EntityCollection ecUsers = _orgService.RetrieveMultiple(new FetchExpression(fetch));
				if (ecUsers.Entities.Count() > 0)
				{
					_tracer.Trace("users Count" + ecUsers.Entities.Count());
					user = ecUsers.Entities.FirstOrDefault();
				}
			}
			catch (Exception ex)
			{
				_tracer.Trace("users not found");
				return user;
			}
			return user;
			
		}
	}
}
