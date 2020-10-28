using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Sysmex.Crm.Model;

namespace Sysmex.Crm.IntegrationPlugins.Logic
{
    public class CalculateActualRevenueDateLogic
    {
        private IOrganizationService orgService;
        private ITracingService trace;

        public CalculateActualRevenueDateLogic(IOrganizationService orgService, ITracingService trace)
        {
            this.orgService = orgService;
            this.trace = trace;
        }

        /// <summary>
        /// Given the original smx_ActualRevenueDate, takes its month and year, and returns a new date set to the last day of that month and year.
        /// </summary>
        /// <param name="smx_ActualRevenueDate"></param>
        public DateTime CalculateActualRevenueDate(DateTime smx_ActualRevenueDate)
        {
            this.trace.Trace("* BEGIN/RETURN CalculateActualRevenueDate method *");
            return new DateTime(smx_ActualRevenueDate.Year,
                                               smx_ActualRevenueDate.Month,
                                               DateTime.DaysInMonth(smx_ActualRevenueDate.Year,
                                               smx_ActualRevenueDate.Month));
        }
    }
}
