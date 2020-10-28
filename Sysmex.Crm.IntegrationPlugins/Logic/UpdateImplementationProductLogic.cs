using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Sysmex.Crm.Model;

namespace Sysmex.Crm.IntegrationPlugins.Logic
{
	internal class UpdateImplementationProductLogic
	{
		private IOrganizationService orgService;
		private ITracingService trace;

		public UpdateImplementationProductLogic(IOrganizationService orgService, ITracingService trace)
		{
			this.orgService = orgService;
			this.trace = trace;
		}

		public void Update(smx_implementation implementation)
		{
			trace.Trace($"Start {nameof(Update)}");

			var workflowId = GetWorkflow("Set Implementation Product Name");

			var implementationProducts = GetImplementationProducts(implementation.Id);
			trace.Trace($"Products count:  {implementationProducts.Count()}");
			foreach (var product in implementationProducts)
			{
				var doUpdate = false;

				foreach (var mapping in _implementationMapping)
				{
					if (implementation.Contains(mapping.Key))
					{
						trace.Trace($"Map: {mapping.Key}");
						product.Attributes.Add(mapping.Value, implementation[mapping.Key]);
						doUpdate = true;
					}
				}

				if (implementation.Contains("smx_instrumentshiptoid"))
				{
					RunRenameWorkflow(product.ToEntityReference(), workflowId);
				}

				if (doUpdate)
				{
					trace.Trace($"Update {product.Id}");
					orgService.Update(product.ToEntity<Entity>());
				}
			}

			trace.Trace($"End {nameof(Update)}");
		}

		private void RunRenameWorkflow(EntityReference implementationProductId, Guid workflowId)
		{
			trace.Trace($"Try to start Workflow ID: {workflowId}");
			if (workflowId == Guid.Empty)
			{
				trace.Trace("Empty Guid; Exit Early");
				return;
			}

			var workflowRequest = new ExecuteWorkflowRequest()
			{
				WorkflowId = workflowId,
				EntityId = implementationProductId.Id
			};

			ExecuteWorkflowResponse response = (ExecuteWorkflowResponse)orgService.Execute(workflowRequest);
		}

		private Guid GetWorkflow(string workflowName)
		{
			var fetch = $@"<fetch>
				  <entity name='workflow'>
					<attribute name='workflowid' />
					<filter type='and'>
					  <condition attribute='name' operator='eq' value='{workflowName}' />
					  <condition attribute='activeworkflowid' operator='not-null' />
					  <condition attribute='statecode' operator='eq' value='1' />
					</filter>
				  </entity>
				</fetch>";

			var workflowResult = orgService.RetrieveMultiple(new FetchExpression(fetch)).Entities.FirstOrDefault();
			if (workflowResult == null)
			{
				trace.Trace("Could not find Workflow; exit early.");
				return Guid.Empty;
			}
			return workflowResult.Id;
		}

		public void Update(new_cpq_lineitem_tmp lineItem)
		{
			trace.Trace($"Start {nameof(Update)}");
			if (lineItem.smx_LineItemStatus != null && ValidLineItemStatus(lineItem) == false)
			{
				return;
			}

			var impProd = GetImpProd(lineItem);
			if (impProd == null)
			{
				trace.Trace("Could not find related Implmentation Product.  Exit Early");
			}

			var doUpdate = false;
			foreach (var mapping in _quoteLineItemMapping)
			{
				if (lineItem.Contains(mapping.Key))
				{
					trace.Trace($"Map: {mapping.Key}");
					impProd.Attributes.Add(mapping.Value, lineItem[mapping.Key]);
					doUpdate = true;
				}
			}

			if (doUpdate)
			{
				trace.Trace("Do Update");
				orgService.Update(impProd.ToEntity<Entity>());
			}
			trace.Trace($"End {nameof(Update)}");
		}

		private bool ValidLineItemStatus(new_cpq_lineitem_tmp lineItem)
		{
			trace.Trace($"Start {nameof(ValidLineItemStatus)}");
			if (lineItem.smx_LineItemStatus == null
							|| lineItem.smx_LineItemStatus.Value == (int)smx_product_status.New
							|| lineItem.smx_LineItemStatus.Value == (int)smx_product_status.Existing)
			{
				trace.Trace("Only map Line Item Status values that are NOT equal to New Or Existing; Exit Early");
				return false;
			}
			return true;
		}

		private smx_implementationproduct GetImpProd(new_cpq_lineitem_tmp lineItem)
		{
			trace.Trace($"Start {nameof(GetImpProd)}");
			if (string.IsNullOrWhiteSpace(lineItem.new_name))
			{
				trace.Trace("Missing Apttus Line Item Id; Exit Early");
				return null;
			}

			var fetch = $@"<fetch>
							  <entity name='smx_implementationproduct'>
								<attribute name='smx_implementationproductid' />
								<filter type='and'>
								  <condition attribute='smx_cpqlineitemid' operator='eq' value='{lineItem.new_name}' />
								  <condition attribute='statecode' operator='eq' value='{(int)smx_implementationproductState.Active}' />
								</filter>
							  </entity>
							</fetch>";

			return orgService.RetrieveMultiple(new FetchExpression(fetch))
					.Entities
					.Select(s => s.ToEntity<smx_implementationproduct>())
					.FirstOrDefault();
		}

		private IEnumerable<smx_implementationproduct> GetImplementationProducts(Guid implementationId)
		{
			trace.Trace($"Start {nameof(GetImplementationProducts)}");
			var fetch = $@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
							  <entity name='smx_implementationproduct'>
								<attribute name='smx_implementationproductid' />
								<filter type='and'>
								  <condition attribute='smx_implementationid' operator='eq' value='{implementationId}' />
								  <condition attribute='statecode' operator='eq' value='{(int)smx_implementationproductState.Active}' />
								</filter>
							  </entity>
							</fetch>";

			return orgService.RetrieveMultiple(new FetchExpression(fetch))
					.Entities
					.Select(s => s.ToEntity<smx_implementationproduct>());
		}

		private Dictionary<string, string> _quoteLineItemMapping = new Dictionary<string, string>()
		{
			{"smx_lineitemstatus","smx_lineitemstatus"}
		};

		private Dictionary<string, string> _implementationMapping = new Dictionary<string, string>()
		{
			{"smx_addresstimezone","smx_addresstimezone"}
		};
	}
}