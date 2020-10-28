using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Sysmex.Crm.Model;

namespace Sysmex.Crm.IntegrationPlugins.Logic
{
	internal class MapCASToImplementationProductLogic
	{
		private IOrganizationService orgService;
		private ITracingService trace;

		public MapCASToImplementationProductLogic(IOrganizationService orgService, ITracingService trace)
		{
			this.orgService = orgService;
			this.trace = trace;
		}

		public void Map(EntityReference entityReference)
		{
			var implementationProducts = new List<smx_implementationproduct>();
			if (entityReference.LogicalName == BookableResourceBooking.EntityLogicalName)
			{
				implementationProducts.AddRange(GetImplementationProducts(entityReference.Id));
			}
			else if (entityReference.LogicalName == msdyn_workorder.EntityLogicalName)
			{
				implementationProducts.AddRange(GetImplementationProductsByWorkOrder(entityReference.Id));
			}

			foreach (var product in implementationProducts)
			{
				var doUpdate = false;
				var updateProduct = new smx_implementationproduct();
				updateProduct.Id = product.Id;

				var bookings = GetRelatedBookings(product.Id);
				if (bookings.Count() == 0)
				{
					trace.Trace("Could not find related bookings");
				}
				else
				{
					var minStartTime = bookings.Min(m => m.StartTime);
					var booking = bookings.Where(w => w.StartTime == minStartTime).OrderByDescending(o => o.CreatedOn).FirstOrDefault();
					var resource = GetResource(booking.Resource);

					if (resource?.LogicalName == SystemUser.EntityLogicalName && resource?.Id != product.smx_CASId?.Id)
					{
						doUpdate = true;
						updateProduct.smx_CASId = resource;
						updateProduct.smx_ImplementationRep = null;
					}
					else if (resource?.LogicalName == Contact.EntityLogicalName && resource?.Id != product.smx_ImplementationRep?.Id)
					{
						doUpdate = true;
						updateProduct.smx_ImplementationRep = resource;
						updateProduct.smx_CASId = null;
					}

					if (booking.StartTime.HasValue && (product.smx_ImplementationDate == null || product.smx_ImplementationDate != minStartTime))
					{
						doUpdate = true;
						updateProduct.smx_ImplementationDate = booking.StartTime;
					}
				}

				if (doUpdate)
				{
					orgService.Update(updateProduct.ToEntity<Entity>());
				}
				else
				{
					updateProduct.smx_ImplementationRep = null;
					updateProduct.smx_CASId = null;
					updateProduct.smx_ImplementationDate = null;
					orgService.Update(updateProduct.ToEntity<Entity>());
				}
			}				
		}

		private IEnumerable<smx_implementationproduct> GetImplementationProducts(Guid bookingId)
		{
			var fetch = $@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>
							  <entity name='smx_implementationproduct'>
								<attribute name='smx_implementationproductid' />
								<attribute name='smx_implementationrep' />								
								<attribute name='smx_casid' />
								<attribute name='smx_implementationdate' />								
								<link-entity name='msdyn_workorder' from='smx_implementationproductid' to='smx_implementationproductid' link-type='inner' alias='ac'>
								  <link-entity name='bookableresourcebooking' from='msdyn_workorder' to='msdyn_workorderid' link-type='inner' alias='booking'>
									<attribute name='starttime' />
									<filter type='and'>
									  <condition attribute='bookableresourcebookingid' operator='eq' value='{bookingId}' />
									</filter>
								  </link-entity>
								</link-entity>
							  </entity>
							</fetch>";

			return orgService.RetrieveMultiple(new FetchExpression(fetch))
						.Entities
						.Select(s => s.ToEntity<smx_implementationproduct>());
		}

		private string SubStatusName(EntityReference subStatus)
		{
			var fetch = $@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
							  <entity name='msdyn_workordersubstatus'>
								<attribute name='msdyn_name' />
								<filter type='and'>
								  <condition attribute='msdyn_workordersubstatusid' operator='eq' value='{subStatus.Id}' />
								</filter>
							  </entity>
							</fetch>";

			var result = orgService.RetrieveMultiple(new FetchExpression(fetch))
									.Entities
									.FirstOrDefault();

			return result.Contains("msdyn_name") ? result.GetAttributeValue<string>("msdyn_name") : "";
		}

		private IEnumerable<BookableResourceBooking> GetRelatedBookings(Guid productId)
		{
			trace.Trace($"Start {nameof(GetRelatedBookings)}");
			var fetch = $@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
							<entity name='bookableresourcebooking'>
								<attribute name='starttime' />  
								<attribute name='resource' /> 
								<attribute name='bookableresourcebookingid' />
								<filter type='and'>
									<condition attribute='statecode' operator='eq' value='0' />
								</filter>
								<link-entity name='msdyn_workorder' from='msdyn_workorderid' to='msdyn_workorder' link-type='inner' alias='aa'>
									<filter type='and'>
										<condition attribute='smx_implementationproductid' operator='eq' value='{productId}' />
										<condition attribute='statecode' operator='eq' value='{(int)msdyn_workorderState.Active}' />
									</filter>
								    <link-entity name='msdyn_workordersubstatus' from='msdyn_workordersubstatusid' to='msdyn_substatus' link-type='inner' alias='ae'>
										<filter type='and'>
											<condition attribute='msdyn_name' operator='ne' value='Cancelled' />
										</filter>
									</link-entity>
								</link-entity>
								<link-entity name='bookingstatus' from='bookingstatusid' to='bookingstatus' link-type='inner' alias='ab'>
								  <filter type='and'>
									<condition attribute='name' operator='ne' value='Cancelled' />
								  </filter>
								</link-entity>
								</entity>
							</fetch>";

			trace.Trace($"End {nameof(GetRelatedBookings)}");
			return orgService.RetrieveMultiple(new FetchExpression(fetch))
						.Entities
						.Select(s => s.ToEntity<BookableResourceBooking>());
		}

		private EntityReference GetResource(EntityReference resource)
		{
			if (resource == null)
			{
				return null;
			}

			var fetch = $@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
							  <entity name='bookableresource'>
								<attribute name='bookableresourceid' />
								<attribute name='userid' />
								<attribute name='contactid' />
								<order attribute='name' descending='false' />
								<filter type='and'>
								  <condition attribute='bookableresourceid' operator='eq' value='{resource.Id}' />
								</filter>
							  </entity>
							</fetch>";

			var bookableResource = orgService.RetrieveMultiple(new FetchExpression(fetch))
												.Entities
												.Select(s => s.ToEntity<BookableResource>())
												.FirstOrDefault();

			if (bookableResource?.UserId != null)
			{
				return bookableResource.UserId;
			}

			return bookableResource?.ContactId;
		}

		private IEnumerable<smx_implementationproduct> GetImplementationProductsByWorkOrder(Guid workOrderId)
		{
			trace.Trace($"Start {nameof(GetImplementationProductsByWorkOrder)}");
			var fetch = $@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>
							  <entity name='smx_implementationproduct'>
								<attribute name='smx_implementationproductid' />
								<attribute name='smx_implementationrep' />								
								<attribute name='smx_casid' />
								<attribute name='smx_implementationdate' />	
								<link-entity name='msdyn_workorder' from='smx_implementationproductid' to='smx_implementationproductid' link-type='inner' alias='aa'>
								  <filter type='and'>
									<condition attribute='msdyn_workorderid' operator='eq' value='{workOrderId}'/>
								  </filter>
								</link-entity>
							  </entity>
							</fetch>";

			trace.Trace($"End {nameof(GetImplementationProductsByWorkOrder)}");
			return orgService.RetrieveMultiple(new FetchExpression(fetch))
						.Entities
						.Select(s => s.ToEntity<smx_implementationproduct>());
		}
	}
}