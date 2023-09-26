using System;
using Reclamation.Core;
using System.IO;
using System.Reflection;
using System.Data;
using System.Collections.Generic;
using System.Linq;

namespace Reclamation.TimeSeries.Nrcs
{
    /// <summary>
    /// Karl Tarbet January 2011 - Jon Rocha 2022
    /// Reads SNOTEL data from nrcs web site.
    /// 
    /// see NWCC_Web_Report_Scripting.txt in same directory as this file.
    /// aslo see example test.bat in same directory
    /// 
    /// JR: the 'STAND' report was discontinued late 2021 so code was modified to 
    /// read data from the new data service
    /// </summary>
    public class NrcsSnotelSeries : Series
    {

        string siteNumber, parameterName, nrcsTriplet;
        static DataTable s_snotelSites, s_inventory;
        static string[] SnotelParameters = { "WTEQ.I-1 (in)", "PREC.I-1 (in)", "TOBS.I-1 (degC)", "TMAX.D-1 (degC)", "TMIN.D-1 (degC)", "TAVG.D-1 (degC)", "SNWD.I-1 (in)" };
        static string[] ParameterCode = { "SE", "PC", "OB", "MX", "MN", "MM", "SD" };
      //  string oldURL = @"http://www.wcc.nrcs.usda.gov/nwcc/view?intervalType=Historic&report=STAND&timeseries=Daily&format=copy&sitenum=679&year=2011&month=WY";
        // https://wcc.sc.egov.usda.gov/reportGenerator/view_csv/customSingleStationReport/daily/start_of_period/306:ID:SNTL/2018-01-01,2018-01-10/WTEQ::value,PREC::value,TOBS::value,TMAX::value,TMIN::value,TAVG::value,SNWD::value
        string newURL = @"https://wcc.sc.egov.usda.gov/reportGenerator/view_csv/customSingleStationReport/daily/start_of_period/$NRCSTRIPLET$/$T1$,$T2$/WTEQ::value,PREC::value,TOBS::value,TMAX::value,TMIN::value,TAVG::value,SNWD::value";

        public static bool AutoUpdate = true;


        public NrcsSnotelSeries(string siteNumber, string parameterName)
        {
            this.siteNumber = siteNumber;
            this.parameterName = parameterName;
            this.nrcsTriplet = LookupTriplet(siteNumber);
            this.TimeInterval = TimeInterval.Daily;

            int idx = parameterName.IndexOf("(");
            int idx2 = parameterName.IndexOf(")");
            if (idx >= 0 && idx2 >= 0 && idx2 > idx)
                Units = parameterName.Substring(idx + 1, idx2 - idx - 1);

            if (Units == "degC")
            {
                Units = "degrees C";
            }
            if (Units == "in")
            {
                Units = "inches";
            }

            Name = LookupName(siteNumber) + " " + parameterName;
            this.Table.TableName = Name.Replace(" ", "_");
            base.SiteID = Name;
            // State = LookupState(siteNumber);
            Provider = "NrcsSnotelSeries";
            ConnectionString = "SiteNumber=" + siteNumber + ";ParameterName=" + parameterName;
        }


        public NrcsSnotelSeries(TimeSeriesDatabase db, TimeSeriesDatabaseDataSet.SeriesCatalogRow sr)
             : base(db, sr)
        {
            siteNumber = ConnectionStringUtility.GetToken(ConnectionString, "SiteNumber", "");
            parameterName = ConnectionStringUtility.GetToken(ConnectionString, "ParameterName", "");
            //State = LookupState(siteNumber);
        }


        protected override Series CreateFromConnectionString()
        {
            NrcsSnotelSeries s = new NrcsSnotelSeries(
            ConnectionStringUtility.GetToken(ConnectionString, "SiteNumber", ""),
            ConnectionStringUtility.GetToken(ConnectionString, "ParameterName", ""));

            return s;
        }


        public static DataTable SnotelSites
        {
            get
            {
                var fn = FileUtility.GetFileReference("snotel_site_list2.csv");
                if (File.Exists(fn) && s_snotelSites == null)
                {
                    s_snotelSites = new CsvFile(fn, CsvFile.FieldTypes.AllText);
                }
                return s_snotelSites;
            }
        }


        /// <summary>
        /// Returns number based on station id 
        /// </summary>
        /// <param name="stationID">example:49L10S</param>
        /// <returns></returns>
        public static string LookupSiteID(string stationID)
        {

            if (SnotelSites != null)
            {
                string sql = "[StationID] = '" + stationID + "'";
                DataRow[] rows = SnotelSites.Select(sql);
                if (rows.Length == 1)
                {
                    return rows[0]["SiteID"].ToString();
                }
            }
            return "";
        }


        static string LookupName(string siteNumber)
        {

            if (SnotelSites != null)
            {
                string sql = "[SiteID] = '" + siteNumber + "'";
                DataRow[] rows = SnotelSites.Select(sql);
                if (rows.Length == 1)
                {
                    return rows[0]["SiteName"].ToString();
                }
            }
            return "Snotel Site " + siteNumber;
        }

        static string LookupTriplet(string siteNumber)
        {
            if (SnotelSites != null)
            {
                string sql = "[SiteID] = '" + siteNumber + "'";
                DataRow[] rows = SnotelSites.Select(sql);
                if (rows.Length == 1)
                {
                    return siteNumber.ToString() + ":" + rows[0]["State"].ToString().ToUpper() + ":SNTL";
                }
            }
            return "0:X:SNTL";
        }


        public override PeriodOfRecord GetPeriodOfRecord()
        {
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            string fn = Path.Combine(path, "nwcc_inventory.csv");



            if (File.Exists(fn) && s_inventory == null)
            {
                s_inventory = new CsvFile(fn, CsvFile.FieldTypes.AllText);
            }
            if (s_inventory != null)
            {
                string sql = "[station id] = '" + siteNumber + "'";
                DataRow[] rows = s_inventory.Select(sql);
                if (rows.Length == 1)
                {
                    DateTime t1, t2;

                    if (DateTime.TryParse(rows[0]["start_date"].ToString(), out t1)
                        && DateTime.TryParse(rows[0]["end_date"].ToString(), out t2))
                    {
                        if (t2 > DateTime.Now.Date)
                            t2 = DateTime.Now.Date;

                        return new PeriodOfRecord(t1, t2, 0);
                    }
                }
            }

            return new PeriodOfRecord(DateTime.Now.AddYears(-10), DateTime.Now, 0);

        }


        protected override void ReadCore()
        {
            var por = GetPeriodOfRecord();
            ReadCore(por.T1, por.T2);
        }

        protected override void ReadCore(DateTime t1, DateTime t2)
        {
            Clear();
            if (m_db == null)
            {
                ReadFromWeb(t1, t2);
            }
            else
            {
                if (NrcsSnotelSeries.AutoUpdate)
                {
                    if (t2 >= DateTime.Now.Date.AddDays(1))
                    { // don't waste time looking to the future
                        // snotel includes today
                        t2 = DateTime.Now.Date;
                    }
                    base.UpdateCore(t1, t2, true);
                }
                base.ReadCore(t1, t2);

            }

        }


        private void ReadFromWeb(DateTime t1, DateTime t2)
        {
            if (t2 < t1)
            {
                Logger.WriteLine("Warning invalid date range");
                return;
            }

            string queryUrl = newURL.Replace("$NRCSTRIPLET$", nrcsTriplet);
            queryUrl = queryUrl.Replace("$T1$", t1.ToString("yyyy-MM-dd"));
            queryUrl = queryUrl.Replace("$T2$", t2.ToString("yyyy-MM-dd"));
            var queryLines = Web.GetPage(queryUrl, true);
            ProcessQuery(queryLines);


            // Old code that worked with the 'STAND' report format
            //int wy1 = t1.WaterYear();
            //int wy2 = t2.WaterYear();
            //for (int wy = wy1; wy <=wy2 ; wy++)
            //{
            //    string address = url.Replace("sitenum=679", "sitenum=" + siteNumber);
            //    address = address.Replace("year=2011", "year=" + wy);
            //    var lines = Web.GetPage(address,true);
            //    ProcessPage(lines,t1,t2);
            //}

        }


        private void ProcessQuery(string[] lines)
        {
            /*
              # ...
              # Atlanta Summit (306) 
              # Idaho  SNOTEL Site - 7580 ft
              # Reporting Frequency: Daily; Date Range: 2018-01-01 to 2018-01-10
              #
              # As of: Jan 21, 2022 10:52:50 AM GMT-08:00
              #
              Date,Snow Water Equivalent (in) Start of Day Values,Precipitation Accumulation (in) Start of Day Values,Air Temperature Observed (degF) Start of Day Values,Air Temperature Maximum (degF),Air Temperature Minimum (degF),Air Temperature Average (degF),Snow Depth (in) Start of Day Values
              2018-01-01,7.7,12.9,25,37,23,29,32
              2018-01-02,7.7,12.9,29,50,28,35,31
              ...            
             */
            var data = new List<string>(lines);
            data.RemoveAll(x => x.StartsWith("#"));
            int dataIdx = Array.IndexOf(SnotelParameters, this.parameterName) + 1;

            for (int i = 0; i < data.Count; i++)
            {
                var rowItems = data[i].Split(',');
                try
                {
                    DateTime t = DateTime.Parse(rowItems[0]);
                    double v = double.Parse(rowItems[dataIdx].ToString());
                    Add(t, v);
                }
                catch
                {

                }
            }
        }


        public static string SnotelParameterFromHydrometPcode(string pcode)
        {
            int idx = Array.IndexOf(ParameterCode, pcode.ToUpper());
            if (idx >= 0)
                return SnotelParameters[idx];
            return "";
        }


    }
}
