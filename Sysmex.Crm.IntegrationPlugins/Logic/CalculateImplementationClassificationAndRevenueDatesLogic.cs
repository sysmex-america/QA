using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Sysmex.Crm.Model;

namespace Sysmex.Crm.IntegrationPlugins.Logic
{
	public class CalculateImplementationClassificationAndRevenueDatesLogic
	{
		public smx_implementation theImplmentation;

		private IOrganizationService orgService;
		private ITracingService trace;		
		private IEnumerable<smx_implementationproduct> products;

		public CalculateImplementationClassificationAndRevenueDatesLogic(IOrganizationService orgService, ITracingService trace)
		{
			this.orgService = orgService;
			this.trace = trace;
		}

		public OptionSetValue GetClassification(OptionSetValue classification, OptionSetValue updateAll, Guid productId)
		{
			trace.Trace($"Start {nameof(GetClassification)}");
			var product = new smx_implementationproduct();
			product.Id = productId;

			if (theImplmentation.smx_WAMSite?.Value == (int)smx_yesno.Yes)
			{
				trace.Trace($"WAM Site is Yes");
				if (classification?.Value == (int)smx_classification.SYS || classification?.Value == (int)smx_classification.STD)
				{
					trace.Trace($"SYS or STD");
					classification = new OptionSetValue((int)smx_classification.WR);
					product.smx_Classification = new OptionSetValue((int)smx_classification.WR);				

					orgService.Update(product.ToEntity<Entity>());					
					UpdateProductClassification(product.Id, product.smx_Classification);
				}
				this.GetRevRecDate(classification, new OptionSetValue((int)smx_yesno.No), product.Id);
			}
			else if (GetExisting(smx_classification.WR).Any() || GetExisting(smx_classification.WAC).Any())
			{
				trace.Trace($"Has WR or WAC");
				if (updateAll?.Value == (int)smx_yesno.Yes)
				{
					trace.Trace($"Update All");
					var changeToWRList = new List<smx_implementationproduct>();
					changeToWRList.AddRange(GetExisting(smx_classification.STD));
					changeToWRList.AddRange(GetExisting(smx_classification.SYS));
					trace.Trace($"Record to Change Count {changeToWRList.Count}");

					foreach (var item in changeToWRList)
					{
						product.Id = item.Id;
						product.smx_Classification = new OptionSetValue((int)smx_classification.WR);
						orgService.Update(product.ToEntity<Entity>());

						if (item.Id == productId)
						{
							classification = product.smx_Classification;
						}
						this.GetRevRecDate(product.smx_Classification, new OptionSetValue((int)smx_yesno.No), product.Id);
						UpdateProductClassification(product.Id, product.smx_Classification);
					}
				}
				else if (classification?.Value == (int)smx_classification.STD || classification?.Value == (int)smx_classification.SYS)
				{
					trace.Trace($"Update just current record");
					product.smx_Classification = new OptionSetValue((int)smx_classification.WR);
					orgService.Update(product.ToEntity<Entity>());
					classification = product.smx_Classification;
					this.GetRevRecDate(product.smx_Classification, new OptionSetValue((int)smx_yesno.No), product.Id);
					UpdateProductClassification(product.Id, product.smx_Classification);
				}

				trace.Trace($"Update the Implementation WAM flag");
				var updateImplementation = new smx_implementation();
				updateImplementation.Id = theImplmentation.Id;
				updateImplementation.smx_WAMSite = new OptionSetValue((int)smx_yesno.Yes);
				orgService.Update(updateImplementation.ToEntity<Entity>());

				theImplmentation.smx_WAMSite = new OptionSetValue((int)smx_yesno.Yes);
			}
			else if (GetExisting(smx_classification.SYS).Any())
			{
				trace.Trace($"Has SYS");
				if (updateAll?.Value == (int)smx_yesno.Yes)
				{
					trace.Trace($"Update All");
					var stdas = GetExisting(smx_classification.STD);

					foreach (var stda in stdas)
					{
						stda.smx_Classification = new OptionSetValue((int)smx_classification.SYS);
						orgService.Update(stda.ToEntity<Entity>());
						this.GetRevRecDate(stda.smx_Classification, new OptionSetValue((int)smx_yesno.No), stda.Id);
						UpdateProductClassification(stda.Id, stda.smx_Classification);

						if (stda.Id == productId)
						{
							classification = product.smx_Classification;
						}
					}
				}
				else if (classification?.Value == (int)smx_classification.STD)
				{
					trace.Trace($"Update current record only");
					product.smx_Classification = new OptionSetValue((int)smx_classification.SYS);
					orgService.Update(product.ToEntity<Entity>());
					classification = product.smx_Classification;
					this.GetRevRecDate(product.smx_Classification, new OptionSetValue((int)smx_yesno.No), product.Id);
					UpdateProductClassification(product.Id, product.smx_Classification);
				}
			}

			trace.Trace($"Classification {classification?.Value}");
			trace.Trace($"End {nameof(GetClassification)}");
			return classification;
		}

		private void UpdateProductClassification(Guid productId, OptionSetValue value)
		{
			var thisRecord = products.Where(w => w.Id == productId).FirstOrDefault();
			thisRecord.smx_Classification = value;
		}

		private IEnumerable<smx_implementationproduct> GetExisting(smx_classification value)
		{
			return products.Where(w => w.smx_Classification?.Value == (int)value);
		}

		public void SetProducts()
		{
			var fetch = $@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
							  <entity name='smx_implementationproduct'>
								<attribute name='smx_implementationproductid' />
								<attribute name='smx_classification' />
								<attribute name='smx_calculatedrevrecdate' />
								<filter type='and'>
                                  <filter type='and'>
									<condition attribute='statecode' operator='ne' value='{(int)smx_implementationproductState.Inactive}' />
									<filter type='or'>
									  <condition attribute='smx_lineitemstatus' operator='ne' value='{(int)smx_product_status.Remove}' />
									  <filter type='and'>
										<condition attribute='smx_ovitemstatus' operator='not-null' />
										<condition attribute='smx_ovitemstatus' operator='ne' value='{(int)smx_implementationproduct_smx_ovitemstatus.Delete}' />
									  </filter>
									</filter>
								  </filter>
								  <condition attribute='smx_implementationid' operator='eq' value='{theImplmentation.Id}' />
								</filter>
							  </entity>
							</fetch>";

			products = orgService.RetrieveMultiple(new FetchExpression(fetch))
								.Entities
								.Select(s => s.ToEntity<smx_implementationproduct>());

			trace.Trace($"Products found: {products.Count()}");
		}

		public DateTime? GetRevRecDate(OptionSetValue classification, OptionSetValue updateAll, Guid productId)
		{
			trace.Trace($"Start {nameof(GetRevRecDate)}");
			trace.Trace($"Classification {classification?.Value}");
			trace.Trace($"Update All is Yes {updateAll?.Value}");

			DateTime? revRecDate = null, normalDate = null;
			if (theImplmentation.smx_OrderDistributedDate.HasValue)
			{
				trace.Trace($"Order Distribution Date {theImplmentation.smx_OrderDistributedDate}");

				int convertedClassification;
				if (updateAll?.Value == (int)smx_yesno.Yes)
				{
					var maxDateProduct = FindProperProduct();
					convertedClassification = maxDateProduct?.smx_Classification == null ? 0 : maxDateProduct.smx_Classification.Value;
				}
				else
				{
					convertedClassification = classification == null ?  0 : classification.Value;
				}
				
				var productsToUpdate = new List<smx_implementationproduct>();

				if (theImplmentation.smx_WAMSite?.Value == (int)smx_yesno.Yes
					|| theImplmentation.smx_WAMConnects?.Value == (int)smx_yesno.Yes
					|| convertedClassification == (int)smx_classification.WR
					|| convertedClassification == (int)smx_classification.WAC)
				{
					revRecDate = theImplmentation.smx_OrderDistributedDate.Value.Date.AddDays(365);
					normalDate = theImplmentation.smx_OrderDistributedDate.Value.Date.AddDays(120);					
				}
				else if (convertedClassification == (int)smx_classification.SYS)
				{
					revRecDate = theImplmentation.smx_OrderDistributedDate.Value.Date.AddDays(275);
					normalDate = theImplmentation.smx_OrderDistributedDate.Value.Date.AddDays(90);
				}
				else if (convertedClassification == (int)smx_classification.STD)
				{
					revRecDate = theImplmentation.smx_OrderDistributedDate.Value.Date.AddDays(185);
					normalDate = theImplmentation.smx_OrderDistributedDate.Value.Date.AddDays(60);
				}
				else if (convertedClassification == (int)smx_classification.FLO)
				{
					revRecDate = theImplmentation.smx_OrderDistributedDate.Value.Date.AddDays(185);
					normalDate = theImplmentation.smx_OrderDistributedDate.Value.Date.AddDays(90);
				}
				else if (convertedClassification == (int)smx_classification.POC
						|| convertedClassification == (int)smx_classification.SED
						|| convertedClassification == (int)smx_classification.XP
						|| convertedClassification == (int)smx_classification.GLO)
				{
					revRecDate = theImplmentation.smx_OrderDistributedDate.Value.Date.AddDays(60);
					normalDate = theImplmentation.smx_OrderDistributedDate.Value.Date.AddDays(60);
				}
				else
				{
					revRecDate = theImplmentation.smx_OrderDistributedDate.Value.Date.AddDays(185);
					normalDate = theImplmentation.smx_OrderDistributedDate.Value.Date.AddDays(60);
				}

				if (updateAll?.Value == (int)smx_yesno.Yes)
				{
					productsToUpdate.AddRange(products.Where(w =>
																(w.smx_CalculatedRevRecDate.HasValue == false ||
																w.smx_CalculatedRevRecDate.Value.Date < revRecDate ||
																w.smx_NormalProcessDate.HasValue == false || 
																w.smx_NormalProcessDate.Value.Date < normalDate)));
				}
				else
				{
					productsToUpdate.Add(new smx_implementationproduct { Id = productId });
				}

				trace.Trace($"Rev Rec Date {revRecDate}");
				trace.Trace($"Normal Process Date {normalDate}");
				trace.Trace($"Products to Update count {productsToUpdate.Count()}");
				foreach (var product in productsToUpdate)
				{
					if (revRecDate.HasValue && normalDate.HasValue)
					{
						trace.Trace($"Update Product {product.Id}");
						var potRevDate = this.GetPotentialRevenueDate(revRecDate);
						trace.Trace($"Pot Rev Date {potRevDate}");

						var updateProduct = new smx_implementationproduct();
						updateProduct.Id = product.Id;
						updateProduct.smx_CalculatedRevRecDate = revRecDate;
						updateProduct.smx_NormalProcessDate = normalDate;
							updateProduct.smx_PotentialRevenueDate = potRevDate;
						orgService.Update(updateProduct.ToEntity<Entity>());
					}
					else
					{
						trace.Trace($"Skip Update of Product {product.Id}; Rev Rec Date: {product.smx_CalculatedRevRecDate.Value}; Nominal Date {product.smx_NormalProcessDate.Value}");
					}
				}
			}
			else
			{
				trace.Trace("No Order Dist Date Found.");
			}

			return revRecDate;
		}

		private smx_implementationproduct FindProperProduct()
		{
			var tempProduct = products.Where(w => w.smx_Classification?.Value == (int)smx_classification.WR
											|| w.smx_Classification?.Value == (int)smx_classification.WAC).FirstOrDefault();

			if (tempProduct == null)
			{
				tempProduct = products.Where(w => w.smx_Classification?.Value == (int)smx_classification.SYS).FirstOrDefault();
				if (tempProduct == null)
				{
					tempProduct = products.Where(w => w.smx_Classification?.Value == (int)smx_classification.FLO || w.smx_Classification?.Value == (int)smx_classification.STD).FirstOrDefault();
					if (tempProduct == null)
					{
						tempProduct = products.Where(w => w.smx_Classification?.Value == (int)smx_classification.POC
													|| w.smx_Classification?.Value == (int)smx_classification.SED
													|| w.smx_Classification?.Value == (int)smx_classification.XP
													|| w.smx_Classification?.Value == (int)smx_classification.GLO).FirstOrDefault();

						if (tempProduct == null)
						{
							tempProduct = products.FirstOrDefault();
						}

					}
				}

			}
			return tempProduct;
		}

		public DateTime? GetPotentialRevenueDate(DateTime? input)
		{
			if (input.HasValue == false)
			{
				return null;
			}

			return new DateTime(input.Value.Year,
								   input.Value.Month,
								   DateTime.DaysInMonth(input.Value.Year, input.Value.Month), 12, 0, 0);
		}

		public void SetImplementation(EntityReference implementationId)
		{
			if (implementationId == null)
			{
				trace.Trace("Missing Implementation Id");
				return;
			}

			theImplmentation = orgService.Retrieve(implementationId.LogicalName, implementationId.Id, new ColumnSet("smx_orderdistributeddate", "smx_wamsite", "smx_wamconnects")).ToEntity<smx_implementation>();
		}
	}
}