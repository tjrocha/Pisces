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
        /// <param name="partialDaily">Observed partial daily data to infill (cfs)</param>
        /// <param name="completeDaily">Observed complete daily data from upstream to scale (cfs)</param>
        /// <param name="monthly">Observed monthly data to be disaggregated (cfs or acre-feet)</param>
        /// <returns></returns>
        [FunctionAttribute("Fills partial daily series by scaling complete daily series to a known monthly volume.",
        "ScaleDailyToMonthlyVolume(PartialDailySeries,CompleteDailySeries,MonthlySeries,bool merge = true)")]
        public static Series ScaleDailyToMonthlyVolume(Series partialDaily, Series completeDaily, Series monthly,
            bool merge = true)
        {
            // Check TimeIntervals of input series
            if (partialDaily.TimeInterval != TimeInterval.Daily)
            {
                throw new ArgumentException("Invalid time interval for series: " + partialDaily.Name);
            }
            if (completeDaily.TimeInterval != TimeInterval.Daily)
            {
                throw new ArgumentException("Invalid time interval for series: " + completeDaily.Name);
            }
            if (monthly.TimeInterval != TimeInterval.Monthly)
            {
                throw new ArgumentException("Invalid time interval for series: " + monthly.Name);
            }

            var scaledDaily = CreateScaledSeries(completeDaily, monthly);

            if (merge)
            {
                return Merge(partialDaily, scaledDaily);
            }

            return scaledDaily;
        }

        /// <summary>
        /// Scale daily series to a monthly volume.
        /// </summary>
        /// <param name="daily">daily series (cfs)</param>
        /// <param name="monthly">monthly series (cfs or acre-feet)</param>
        /// <returns></returns>
        public static Series CreateScaledSeries(Series daily, Series monthly)
        {
            // Check series units
            if (daily.Units.ToLower() != "cfs")
            {
                var msg = string.Format("Invalid units provided on series '{0}' must be 'cfs'", daily.Name);
                throw new FormatException(msg);
            }
            if (monthly.Units.ToLower() != "cfs" && monthly.Units.ToLower() != "acre-feet")
            {
                var msg = string.Format("Invalid units provided on series '{0}' must be 'cfs' or 'acre-feet'", monthly.Name);
                throw new FormatException(msg);
            }

            // Series to be filled with the scaled daily data
            var rval = new Series("acre-feet", TimeInterval.Daily);

            // Convert daily series to af to compute ratio of daily volume to monthly volume
            var daily_af = Math.ConvertUnits(daily, "acre-feet");

            // Make sure monthly series is in acre-feet and aligns with daily_af timeframe
            var monthly_af = Math.ConvertUnits(monthly, "acre-feet").Subset(daily_af.First().DateTime.FirstOfMonth(), daily_af.Last().DateTime);

            for (int i = 0; i < monthly_af.Count; i++)
            {
                var pt = monthly_af[i];

                var startDate = (pt.DateTime.FirstOfMonth() < daily_af.First().DateTime) ? daily_af.First().DateTime : pt.DateTime.FirstOfMonth();
                var endDate = (pt.DateTime.EndOfMonth() > daily_af.Last().DateTime) ? daily_af.Last().DateTime : pt.DateTime.EndOfMonth();

                var daily_af_subset = daily_af.Subset(startDate, endDate);
                var daily_af_volume = daily_af_subset.Values.Sum();
                var monthly_af_volume = pt.Value * (Convert.ToDouble(daily_af_subset.Count) / Convert.ToDouble(pt.DateTime.EndOfMonth().Day));

                foreach (var p in daily_af_subset)
                {
                    if (p.IsMissing)
                    {
                        rval.AddMissing(p.DateTime);
                    }
                    else
                    {
                        var vRatio = p.Value / daily_af_volume;
                        rval.Add(p.DateTime, monthly_af_volume * vRatio, PointFlag.Computed);
                    } 
                }
            }

            return Math.ConvertUnits(rval, "cfs");
        }

    }
}
