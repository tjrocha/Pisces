using Reclamation.Core;
using Reclamation.TimeSeries.Parser;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Reclamation.TimeSeries
{
    public static partial class Math
    {
        /// <summary>
        /// Entry point for disaggregating monthly streamflow using flow data from upstream gain
        /// </summary>
        /// <param name="completeDaily">Observed complete daily data from upstream to scale (cfs)</param>
        /// <param name="monthly">Observed monthly data to be disaggregated (cfs or acre-feet)</param>
        /// <returns></returns>
        [FunctionAttribute("Just a test.",
        "Test(CompleteDailySeries,MonthlySeries,bool merge = true)")]
        public static Series Test(Series completeDaily, Series monthly,
            bool merge = true)
        {

            var monthly_subset = monthly.Subset(completeDaily.First().DateTime, completeDaily.Last().DateTime);

            return monthly_subset;
        }



    }
}
