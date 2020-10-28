using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Sysmex.Crm.Model;

namespace Sysmex.Crm.IntegrationPlugins.Logic
{
    public class SetImplementationRollupFieldsLogic
    {
        private IOrganizationService orgService;
        private ITracingService trace;

        private EntityCollection implementationProducts;
		private smx_implementation implementation;
        private Guid implementationId;
		private IOrganizationService service;
		private ITracingService tracer;

		public SetImplementationRollupFieldsLogic(IOrganizationService orgService, ITracingService trace, Guid implementationId)
        {
            this.orgService = orgService;
            this.trace = trace;
            this.implementationId = implementationId;
			this.implementation = RetriveImplementation();
            this.implementationProducts = RetrieveImplementationProducts();
        }

		public SetImplementationRollupFieldsLogic(IOrganizationService service, ITracingService tracer)
		{
			this.service = service;
			this.tracer = tracer;
		}

		public void SetImplementationRollupFields()
        {
			this.trace.Trace($"**Begin Logic {nameof(SetImplementationRollupFields)} **");

            var hasChanges = false;

			var totalRevenue = CalculatedTotalRevenue();
            if  ((this.implementation.smx_CalculatedTotalRevenueRollup == null) || (this.implementation.smx_CalculatedTotalRevenueRollup.Value != totalRevenue.Value))
            {
                this.trace.Trace($"smx_CalculatedTotalRevenueRollup changed to: {totalRevenue.Value}");
                implementation.smx_CalculatedTotalRevenueRollup = totalRevenue;
                hasChanges = true;
            }

            var totalGovtLeaseBackAmount = TotalGovtLeaseBackAmount();
            if ((this.implementation.smx_TotalGovtLeasebackAmountRollup == null) || (this.implementation.smx_TotalGovtLeasebackAmountRollup.Value != totalGovtLeaseBackAmount.Value))
            {
                this.trace.Trace($"smx_TotalGovtLeasebackAmountRollup changed to: {totalGovtLeaseBackAmount.Value}");
                implementation.smx_TotalGovtLeasebackAmountRollup = totalGovtLeaseBackAmount;
                hasChanges = true;
            }

            var unrecognizedRevenue = UnrecognizedRevenue();
            if ((this.implementation.smx_UnrecognizedRevenueRollup == null) || (this.implementation.smx_UnrecognizedRevenueRollup.Value != unrecognizedRevenue.Value))
            {
                this.trace.Trace($"smx_unrecognizedrevenuerollup changed to: {unrecognizedRevenue.Value}");
                implementation.smx_UnrecognizedRevenueRollup = unrecognizedRevenue;
                hasChanges = true;
            }

            var totalProducts = TotalProducts();
            if (this.implementation.smx_TotalProductsRollup != totalProducts)
            {
                this.trace.Trace($"smx_TotalProductsRollup changed to: {totalProducts}");
                implementation.smx_TotalProductsRollup = totalProducts;
                hasChanges = true;
            }

            var totalProductsICNRemaining = TotalProductsICNRemaining();
            if (this.implementation.smx_TotalProductsICNRemainingRollup != totalProductsICNRemaining)
            {
                this.trace.Trace($"smx_TotalProductsICNRemainingRollup changed to: {totalProductsICNRemaining}");
                implementation.smx_TotalProductsICNRemainingRollup = totalProductsICNRemaining;
                hasChanges = true;
            }

            var potentialRevDate = PotentialRevenueDate();
            if (this.implementation.smx_PotentialRevenueDateRollup != potentialRevDate)
            {
                this.trace.Trace($"smx_PotentialRevenueDateRollup changed to: {potentialRevDate}");
                implementation.smx_PotentialRevenueDateRollup = potentialRevDate;
                hasChanges = true;
            }

			var targetGoLiveDate = TargetGoLiveDate();
			if (this.implementation.smx_TargetGoLiveDateRollUp != targetGoLiveDate)
			{
				this.trace.Trace($"smx_TargetGoLiveDateRollUp changed to: {targetGoLiveDate}");
				implementation.smx_TargetGoLiveDateRollUp = targetGoLiveDate;
				hasChanges = true;
			}

			if (hasChanges)
			{
				this.trace.Trace("Update implementation record with changes");
				this.orgService.Update(implementation.ToEntity<Entity>());
			}
            this.trace.Trace($"** END {nameof(SetImplementationRollupFields)} **");
        }

		private DateTime? TargetGoLiveDate()
		{
			this.trace.Trace($"Calculate {nameof(TargetGoLiveDate)} ");
			List<DateTime> dateList = new List<DateTime>();

			foreach (var implementationProduct in this.implementationProducts.Entities.Select(s => s.ToEntity<smx_implementationproduct>()))
			{
				if (implementationProduct.Contains("smx_icncomplete") && implementationProduct.smx_ICNComplete.HasValue
						&& implementationProduct.smx_ICNComplete.Value == false && implementationProduct.smx_GoLiveDate.HasValue)
				{
					dateList.Add(implementationProduct.smx_GoLiveDate.Value);
				}
			}

			if (dateList.Count > 0)
			{
				return dateList.Min();
			}
			else
			{
				return null;
			}
		}

		private DateTime? PotentialRevenueDate()
        {
            this.trace.Trace($"Calculate {nameof(PotentialRevenueDate)} ");
            List<DateTime> dateList = new List<DateTime>();

            foreach (var implementationProduct in this.implementationProducts.Entities)
            {
                if (!implementationProduct.Contains("smx_actualrevenuedate") & implementationProduct.Contains("smx_potentialrevenuedate"))
                    dateList.Add(implementationProduct.GetAttributeValue<DateTime?>("smx_potentialrevenuedate").Value);
            }

            if (dateList.Count > 0)
                return dateList.Min();
            else
                return null;
        }

        /// <summary>
        /// The total count (number) of related Implementation Product records where the ICN Complete = No/unchecked.
        /// </summary>
        /// <returns></returns>
        private int TotalProductsICNRemaining()
        {
            this.trace.Trace($"Calculate {nameof(TotalProductsICNRemaining)} ");
            int calculatedValue = 0;
            foreach (var implementationProduct in this.implementationProducts.Entities)
            {
                if (!implementationProduct.GetAttributeValue<bool>("smx_icncomplete"))
                    calculatedValue += 1;
            }
            return calculatedValue;
        }

        /// <summary>
        /// The count (number) of related Implementation Products records.
        /// </summary>
        /// <returns></returns>
        private int TotalProducts()
        {
            this.trace.Trace($"Calculate {nameof(TotalProducts)} ");
            return this.implementationProducts.Entities.Count();
        }

        /// <summary>
        /// The addition of any implementation products on the Implementation that do not have an actual revenue date.
        /// </summary>
        /// <returns></returns>
        private Money UnrecognizedRevenue()
        {
            this.trace.Trace($"Calculate {nameof(UnrecognizedRevenue)} ");
            decimal calculatedValue = 0;
            foreach (var implementationProduct in this.implementationProducts.Entities)
            {
                if (!implementationProduct.Contains("smx_actualrevenuedate") && implementationProduct.GetAttributeValue<Money>("smx_price") != null)
                    calculatedValue += implementationProduct.GetAttributeValue<Money>("smx_price").Value;
            }
            return new Money(calculatedValue);
        }

        /// <summary>
        /// Total of Gov Lease Backs in the related Implementation Products
        /// </summary>
        /// <returns></returns>
        private Money TotalGovtLeaseBackAmount()
        {
            this.trace.Trace($"Calculate {nameof(TotalGovtLeaseBackAmount)} ");
            decimal calculatedValue = 0;
            foreach (var implementationProduct in this.implementationProducts.Entities)
            {
                if (implementationProduct.GetAttributeValue<OptionSetValue>("statuscode")?.Value == (int)smx_implementationproduct_statuscode.Active && implementationProduct.GetAttributeValue<Money>("smx_governmentleasebackamount") != null)
                    calculatedValue += implementationProduct.GetAttributeValue<Money>("smx_governmentleasebackamount").Value;
            }
            return new Money(calculatedValue);
        }

        /// <summary>
        /// The sum of the Price for all related active Implementation Product records.
        /// </summary>
        /// <returns></returns>
        private Money CalculatedTotalRevenue()
        {
            this.trace.Trace($"Calculate {nameof(CalculatedTotalRevenue)} ");
            decimal calculatedValue = 0;
            foreach(var implementationProduct in this.implementationProducts.Entities)
            {
                if (implementationProduct.GetAttributeValue<OptionSetValue>("statuscode")?.Value == (int) smx_implementationproduct_statuscode.Active && implementationProduct.GetAttributeValue<Money>("smx_price") != null)
                    calculatedValue += implementationProduct.GetAttributeValue<Money>("smx_price").Value;
            }
            return new Money(calculatedValue);
        }

        private smx_implementation RetriveImplementation()
        {
            this.trace.Trace($"Begin {nameof(RetriveImplementation)}");
            var columnSet = new ColumnSet("smx_unrecognizedrevenuerollup",
                "smx_calculatedtotalrevenuerollup",
                "smx_totalproductsicnremainingrollup",
                "smx_totalgovtleasebackamountrollup",
                "smx_potentialrevenuedaterollup",
                "smx_totalproductsrollup",
				"smx_targetgolivedaterollup");
            return orgService.Retrieve(smx_implementation.EntityLogicalName, this.implementationId, columnSet).ToEntity<smx_implementation>();
        }

        private EntityCollection RetrieveImplementationProducts()
        {
            this.trace.Trace($"Begin {nameof(RetrieveImplementationProducts)}:{this.implementationId.ToString()}");

			var fetch = $@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
							  <entity name='smx_implementationproduct'>
								<attribute name='smx_price' />
								<attribute name='smx_potentialrevenuedate' />
								<attribute name='smx_governmentleasebackamount' />
								<attribute name='smx_implementationproductid' />
								<attribute name='statuscode' />
								<attribute name='smx_icncomplete' />
								<attribute name='smx_golivedate' />
								<attribute name='smx_actualrevenuedate' />
								<order attribute='smx_potentialrevenuedate' descending='false' />
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
								  <condition attribute='smx_implementationid' operator='eq' value='{implementationId}' />
								</filter>
							  </entity>
							</fetch>";

			this.trace.Trace($"End {nameof(RetrieveImplementationProducts)}");
            return this.orgService.RetrieveMultiple(new FetchExpression(fetch));
        }
    }
}
