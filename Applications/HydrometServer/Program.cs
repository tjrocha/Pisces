﻿using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using Reclamation.TimeSeries.Hydromet;
using Reclamation.TimeSeries;
using Reclamation.Core;
using System.Configuration;
using System.IO;
using Mono.Options;
using Reclamation.TimeSeries.AgriMet;
using Reclamation.TimeSeries.Alarms;

namespace HydrometServer
{   
    /// <summary>
    /// HydrometServer is a command line tool with many feature to
    /// manage a time series database.
    /// 
    /// HydrometServer processes incoming data and
    /// stores it in a SQL database. Values are flagged 
    /// are based on high and low limits, alarms (email/phone calls)
    /// are made, and computations are performed. 
    /// 
    /// HydrometServer has features to perform daily,monthly, or 15-minute data calclations,
    /// import rating tables, and calculate crop water use
    /// </summary>
    class Program
    {
        static void Main(string[] argList)
        {
            
            var ver = AssemblyUtility.GetVersion();

            Console.Write("HydrometServer " + ver +" " + AssemblyUtility.CreationDate()+ "   System Time = "+DateTime.Now);
            Console.WriteLine();
            if( argList.Length == 0)
            {
                ShowHelp();
                return;
            }

            Arguments args = new Arguments(argList);


            string errorFileName = "errors.txt";
            Performance perf = new Performance();
         //   try
            {
            if (args.Contains("debug"))  
            {
                Logger.EnableLogger();
                Reclamation.TimeSeries.Parser.SeriesExpressionParser.Debug = true;
            }

            if (args.Contains("import-rating-tables"))
            {// --import-rating-tables=site_list.csv  [--generateNewTables]     : updates usgs,idahopower, and owrd rating tables
                ApplicationTrustPolicy.TrustAll();
                var cfg = args["import-rating-tables"];
                if (File.Exists(cfg))
                {
                    RatingTableDownload.UpdateRatingTables(cfg, args.Contains("generateNewTables"));
                }
                else
                {
                    Console.WriteLine("Error: File not found: " + cfg);
                }
                return;
            }

                if( args.Contains("run-crop-charts"))
                {
                    var str_yr = args["run-crop-charts"].Trim();
                    int year = DateTime.Now.Year;
                    if (str_yr != "")
                        year = Convert.ToInt32(str_yr);

                    var server = PostgreSQL.GetPostgresServer("agrimet", "", "agrimet");
                    CropDatesDataSet.DB = server;
                    string dir = CropDatesDataSet.GetCropOutputDirectory(year);
                    Logger.WriteLine("output dir = " + dir);
                   
                    CropChartGenerator.CreateCropReports(year, dir, HydrometHost.PN);
                    return;
                }


              
           
            var db = TimeSeriesDatabase.InitDatabase(args);

            if (args.Contains("cli"))
            {
              TimeInterval interval = TimeInterval.Irregular;
                    if (args["cli"] == "daily")
                        interval = TimeInterval.Daily;

                    Console.WriteLine();
                    HydrometServer.CommandLine.PiscesCommandLine cmd = new CommandLine.PiscesCommandLine(db,interval);
                    cmd.PiscesPrompt();

                return;
            }


                if ( args.Contains("processAlarms"))
            {
                try
                {
                    Logger.EnableLogger();
                    Logger.WriteLine("Checking for new or unconfirmed Alarms ");
                    var aq = new AlarmManager(db);
                    Logger.WriteLine("Processing Alarms");
                    aq.ProcessAlarms();
                }
                catch (Exception e)
                {
                    Logger.WriteLine(e.Message);
                }
                return;
            }

            
            if (args.Contains("error-log"))
            {
                errorFileName = args["error-log"];
                File.AppendAllText(errorFileName,"HydrometServer.exe:  Started " + DateTime.Now.ToString()+"\n");
            }

                string propertyFilter = "";
                if (args.Contains("property-filter"))
                {
                    propertyFilter = args["property-filter"];
                }

                string filter = "";
                if (args.Contains("filter"))
                {
                    filter = args["filter"];
                }

                if (args.Contains("inventory"))
                {
                    db.Inventory();
                }



                if (args.Contains("import")) // import and process data from files
                {
                    bool computeDependencies = args.Contains("computeDependencies");
                    bool computeDailyOnMidnight = args.Contains("computeDailyOnMidnight");

                     
                    string searchPattern = args["import"];

                    if (searchPattern == "")
                        searchPattern = "*";

                    string incomingPath = ConfigurationManager.AppSettings["incoming"];
                    FileImporter importer = new FileImporter(db, DatabaseSaveOptions.Upsert);
                    importer.Import(incomingPath, computeDependencies,computeDailyOnMidnight,searchPattern);
                }



                DateTime t1;
                DateTime t2;

                SetupDates(args, out t1, out t2);

                if (args.Contains("import-hydromet-instant"))
                {
                    HydrometHost host = HydrometHost.PN;
                    if (args["import-hydromet-instant"] != "")
                        host = (HydrometHost)Enum.Parse(typeof(HydrometHost), args["import-hydromet-instant"]);

                    File.AppendAllText(errorFileName, "begin: import-hydromet-instant " + DateTime.Now.ToString() + "\n");
                    ImportHydrometInstant(host,db, t1.AddDays(-2), t2.AddDays(1), filter, propertyFilter);
                }

                if (args.Contains("import-hydromet-daily"))
                {
                    HydrometHost host = HydrometHost.PN;
                    if (args["import-hydromet-daily"] != "")
                        host = (HydrometHost)Enum.Parse(typeof(HydrometHost), args["import-hydromet-daily"]);

                    File.AppendAllText(errorFileName, "begin: import-hydromet-daily " + DateTime.Now.ToString() + "\n");
                    ImportHydrometDaily(host,db, t1, t2, filter, propertyFilter);
                }

                if (args.Contains("import-hydromet-monthly"))
                {
                    File.AppendAllText(errorFileName, "begin: import-hydromet-monthly " + DateTime.Now.ToString() + "\n");
                    ImportHydrometMonthly(db, t1.AddYears(-5), t2.AddDays(1), filter, propertyFilter);
                }


                if (args.Contains("calculate-daily"))
                {
                    DailyTimeSeriesCalculator calc = new DailyTimeSeriesCalculator(db, TimeInterval.Daily,
                        filter,propertyFilter);
                    File.AppendAllText(errorFileName, "begin: calculate-daily " + DateTime.Now.ToString() + "\n");
                    calc.ComputeDailyValues(t1, t2, errorFileName);
                }

                if (args.Contains("calculate-monthly"))
                {
                    MonthlyTimeSeriesCalculator calc = new MonthlyTimeSeriesCalculator(db, TimeInterval.Monthly,
                        filter, propertyFilter);
                    File.AppendAllText(errorFileName, "begin: calculate-monthly " + DateTime.Now.ToString() + "\n");
                    //calc.ComputeDailyValues(t1, t2, errorFileName);
                    calc.ComputeMonthlyValues(t1, t2, errorFileName);
                }


                if(args.Contains("copy-daily"))
                {
                    var tablename = args["copy-daily"];

                    if (tablename=="" || args["source"] == "")
                    {
                        Console.WriteLine("Error: --copy-daily=tablename requires tablename, and requires --source=connectionString");
                        ShowHelp();
                        return;
                    }
                    bool compare = args.Contains("compare");
                    string connectionString = args["source"];

                    Copy(TimeInterval.Daily, connectionString,tablename,(PostgreSQL) db.Server, t1, t2,compare);
                    return;
                }


                if (args.Contains("update-period-of-record"))
                {
                    var sc = db.GetSeriesCatalog("isfolder=0");

                    var prop = db.GetSeriesProperties(true);
                    for (int i = 0; i < sc.Count; i++)
                    {
                        var s = db.GetSeries(sc[i].id);
                        var por = s.GetPeriodOfRecord();

                        s.Properties.Set("count", por.Count.ToString());

                        if (por.Count == 0)
                        {
                            s.Properties.Set("t1", "");
                            s.Properties.Set("t2", "");
                        }
                        else
                        {
                            s.Properties.Set("t1", por.T1.ToString("yyyy-MM-dd"));
                            s.Properties.Set("t2", por.T2.ToString("yyyy-MM-dd"));
                        }
                        Console.WriteLine(s.Name);
                    }
                    db.Server.SaveTable(prop);
                }
               
                db.Server.Cleanup();
                

                File.AppendAllText(errorFileName, "HydrometServer.exe:  Completed " + DateTime.Now.ToString() + "\n");
            }
            //catch (Exception e )
            //{
            //    Logger.WriteLine(e.Message);
            //    File.AppendAllText(errorFileName, "Error: HydrometServer.exe: \n"+e.Message);
            //    // Console.ReadLine();
            //    throw e;
            //}

            var mem = GC.GetTotalMemory(true);
            long mb = mem / 1024 / 1024;
            Console.WriteLine("Memory Usage: " + mb.ToString() + " Mb");
            perf.Report("HydrometServer: finished ");
        }

        

        static void ShowHelp()
        {
            Console.WriteLine("HydrometServer");
            Console.WriteLine();
            Console.WriteLine("Options:");

            Console.WriteLine("--cli=instant|daily|monthly");
            Console.WriteLine("             command line interface");
            Console.WriteLine("--processAlarms");
            Console.WriteLine("             process alarms in the table 'alarm_phone_queue'");
            Console.WriteLine("--import[=searchPattern]   [--computeDependencies] [--computeDailyOnMidnight]");
            Console.WriteLine("            imports (and processes) data in incoming directory");
            Console.WriteLine("            supports DMS3, and LoggerNet files");
            Console.WriteLine("            example: --import=*.txt");
            Console.WriteLine("--debug");
            Console.WriteLine("           prints debugging messages to console");
            Console.WriteLine("--inventory");
            Console.WriteLine("           prints summary inventory of database");
            Console.WriteLine("--calculate-daily");
            Console.WriteLine("           computes daily data");
            Console.WriteLine("--calculate-monthly");
            Console.WriteLine("           evaluates monthly equations");
            Console.WriteLine("--computeDependencies");
            Console.WriteLine("           computes dependencies at the same level");
            Console.WriteLine("--t1=1-31-2013|yesterday|lastyear");
            Console.WriteLine("           starting date: default is yesterday");
            Console.WriteLine("--t2=1-31-2013|yesterday|lastyear");
            Console.WriteLine("           ending date: default is yesterday");
            Console.WriteLine("--property-filter=program:agrimet");
            Console.WriteLine("           filtering based on series properties (key:value)");
            Console.WriteLine("--filter=\"siteid='boii'\"");
            Console.WriteLine("           raw sql filter against seriescatalog");
            Console.WriteLine("--error-log=errors.txt");
            Console.WriteLine("           file to log error messages");

            Console.WriteLine("--copy-daily=tablename --source=connectionString [--compare]");
            Console.WriteLine("           copy daily data from an external time series database");
            Console.WriteLine("           if --compare then compare data in databases without copying");
            Console.WriteLine("     example:  --copy-daily=daily_ahti_etos --source=\"Server=127.0.0.1;Database=timeseries;User id=me;password=[^543}9].*;\"");
            
            Console.WriteLine("--import-hydromet-instant[=PN|Yakima|GreatPlains]");
            Console.WriteLine("           imports hydromet (vms) instant data default (t1-3 days)");
            Console.WriteLine("--import-hydromet-daily[=PN|Yakima|GreatPlains]");
            Console.WriteLine("           imports hydromet (vms) daily data default ( t1-100 days)");
            Console.WriteLine("--import-hydromet-monthly");
            Console.WriteLine("           imports hydromet monthly data ( last 5 years)");
            Console.WriteLine("--import-rating-tables=site_list.csv  [--generateNewTables]");
            Console.WriteLine("          updates usgs,idahopower, and owrd rating tables");
            Console.WriteLine("--update-period-of-record");
            Console.WriteLine("          updates series properties with t1 and t2 for the data");
            Console.WriteLine("--run-crop-charts=2016   --run-crop-charts (defaults to current calendar year)");
            Console.WriteLine("        runs crop charts");
            Console.WriteLine(@"--sqlite=c:\data\mydata.pdb");
            Console.WriteLine("       use sqlite database, and provide filename");
        //    Console.WriteLine("--perfTest");
          //  Console.WriteLine("        measure performance importing test data with calculations ");

        }
        
        

        static string[] s_quality_parameters = new string[] { 
            "parity","power","msglen","lenerr","timeerr","batvolt","bay","demod",
            "charcnt",
            "channel","freq","signal","modul","illchrs","maxpos","timesec","rmsgcnt","eotchar"};
        public static IEnumerable<String> GetBlockOfQueries(TimeSeriesDatabase db,
            TimeInterval interval, string filter,string propertyFilter="", int blockSize=75,
            bool ignoreQuality=true)
        {
            var rval = new List<string>();
            foreach (Series s in db.GetSeries(interval, filter,propertyFilter).ToArray())
            {
                TimeSeriesName tn = new TimeSeriesName(s.Table.TableName);
                //rval.Add(s.SiteID + " " + s.Parameter);
                if (Array.IndexOf(s_quality_parameters, tn.pcode.ToLower() ) >=0 )
                    continue; // skip quality parameters
                rval.Add(tn.siteid + " " + tn.pcode);
                if (rval.Count >= blockSize)
                {
                    yield return String.Join(",",rval.ToArray());
                    rval.Clear();
                }
            }
            yield return String.Join(",", rval.ToArray());
        }


        /// <summary>
        /// Imports daily data from Hydromet into TimeSeriesDatabase
        /// </summary>
        /// <param name="db"></param>
        private static void ImportHydrometDaily(HydrometHost host, TimeSeriesDatabase db, DateTime t1, DateTime t2, string filter, string propertyFilter)
        {
            Performance perf = new Performance();
            Console.WriteLine("ImportHydrometDaily");
            int block = 1;
            foreach (string query in GetBlockOfQueries(db,TimeInterval.Daily,filter,propertyFilter))
            {
                if (query == "")
                    continue;

                var table = HydrometDataUtility.ArchiveTable(host, query, t1, t2, 0);
                Console.WriteLine("Block " + block + " has " + table.Rows.Count + " rows ");
                Console.WriteLine(query);
                SaveTableToSeries(db, table, TimeInterval.Daily);
                block++;
            }
            perf.Report("Finished importing daily data"); // 15 seconds
        }

        /// <summary>
        /// Copy data from another database
        /// if compare=true  compare tables without copying data
        /// </summary>
        /// <param name="db"></param>
        private static void Copy(TimeInterval interval, string connectionString, string tablename,
            PostgreSQL destination, DateTime t1, DateTime t2, bool compare=false)
        {
            Logger.WriteLine("--copy-daily="+tablename);
            
            var source = new PostgreSQL(connectionString);
            var sql = "select * from "+tablename;
                sql += " WHERE datetime >= " + source.PortableDateString(t1, TimeSeriesDatabase.dateTimeFormat)
                + " AND "
                + " datetime <= " + source.PortableDateString(t2, TimeSeriesDatabase.dateTimeFormat);
            sql += " order by datetime ";
            var sourceTable = source.Table(tablename,sql);
            Logger.WriteLine("found "+sourceTable.Rows.Count +" records in source ");
            if (compare) // show differences
            {
                var destTable = destination.Table(tablename, sql);
                Logger.WriteLine("found " + destTable.Rows.Count + " records in destination ");
                Series dest = new Series(tablename);
                dest.Table = destTable;

                var src = new Series("source_" + tablename);
                src.Table = sourceTable;

                var diff = src - dest;
                for (int i = 0; i < diff.Count; i++)
                {
                    var pt = diff[i];
                    if (System.Math.Abs(pt.Value) > 0.01)
                    {
                        Console.WriteLine(tablename +"difference " + pt);
                    }
                }

            }
            else
            {
                foreach (DataRow row in sourceTable.Rows)
                {
                    row.SetAdded();
                }
                destination.InsertTimeSeriesTable(sourceTable);
            }
        }

        private static void SaveTableToSeries(TimeSeriesDatabase db, DataTable table, TimeInterval interval)
        {
            int i = 1;
            string tablePrefix="daily";
            if (interval == TimeInterval.Irregular)
                tablePrefix = "instant";
            if (interval == TimeInterval.Monthly)
                tablePrefix = "monthly";
            while( i < table.Columns.Count )
            {
                string tn = table.Columns[i].ColumnName.Trim().ToLower();
                tn = tn.Replace(" ", "_");
                TimeSeriesName tsn = new TimeSeriesName(tn,interval.ToString().ToLower());
                var series1 = db.GetSeriesFromTableName(tn, tablePrefix);
                Console.Write(tn+ " ");
                for (int r = 0; r < table.Rows.Count; r++)
                {
                    var row = table.Rows[r];
                    object o = row[i];
                    double val = Point.MissingValueFlag;
                    if (o != DBNull.Value)
                        val = Convert.ToDouble(row[i]);
                    else
                    {
                        continue; // mixing 5 and 15-minute data can cause gaps
                    }

                    string flag = "hmet-import";
                    if (interval == TimeInterval.Irregular|| interval == TimeInterval.Monthly)
                        flag = row[i + 1].ToString();


                    DateTime t = Convert.ToDateTime(row[0]);
                    if (interval == TimeInterval.Monthly)
                    {
                        //if( tsn.pcode.ToLower() == "fc" || tsn.pcode.ToLower() == "se" || tsn.pcode.ToLower() == "fcm")
                        t = t.FirstOfMonth();
                        //if (val != Point.MissingValueFlag && HydrometMonthlySeries.LookupUnits(tsn.pcode) == "1000 acre-feet")
                          //  val = val * 1000;
                    
                    }
                   var pt = new Point(t, val, flag);
                    series1.Add(pt);
                }

                if (interval == TimeInterval.Irregular || interval == TimeInterval.Monthly)
                {
                    i += 2;// flag column
                }
                else
                {
                    i++;
                }

                int rc = series1.Count;
                if( rc>0)
                   rc = db.SaveTimeSeriesTable(series1.ID, series1, DatabaseSaveOptions.UpdateExisting);
                Console.WriteLine(rc + " records saved "+POR(series1.Table));
            }
        }

        private static string POR(DataTable t)
        {
            var rval = "";
            if( t.Rows.Count > 0)
            {
                rval = Convert.ToDateTime(t.Rows[0][0]).ToShortDateString()+ " "
                + Convert.ToDateTime(t.Rows[t.Rows.Count -1][0]).ToShortDateString();
            }

            return rval;
        }

        /// <summary>
        /// Imports instant data from Hydromet into TimeSeriesDatabase
        /// </summary>
        /// <param name="db"></param>
        private static void ImportHydrometInstant(HydrometHost host, TimeSeriesDatabase db,DateTime start,
            DateTime end, string filter,string propertyFilter)
        {
            // TO DO.. the outer loop of Date ranges  (t,t3) could
            // be generated as a separate task.
            Console.WriteLine("ImportHydrometInstant");
            TimeRange timeRange = new TimeRange(start, end);
            foreach (TimeRange item in timeRange.Split(365))
            {
                int block = 1;
                foreach (string query in GetBlockOfQueries(db, TimeInterval.Irregular, filter, propertyFilter))
                {
                    Console.WriteLine("Reading " + item.StartDate + " to " + item.EndDate);
                    var table = HydrometDataUtility.DayFilesTable(host, query, item.StartDate, item.EndDate, 0);
                    Console.WriteLine("Block " + block + " has " + table.Rows.Count + " rows ");
                    Console.WriteLine(query);
                    SaveTableToSeries(db, table, TimeInterval.Irregular);
                    block++;
                }
            }
            
            Console.WriteLine("Finished importing 15-minute data");
        }

        /// <summary>
        /// Imports instant data from Hydromet into TimeSeriesDatabase
        /// </summary>
        /// <param name="db"></param>
        private static void ImportHydrometMonthly(TimeSeriesDatabase db, DateTime t1, DateTime t2, string filter,string propertyFilter)
        {
            Console.WriteLine("ImportHydrometMonthly");
            int block = 1;
            foreach (string query in GetBlockOfQueries(db, TimeInterval.Monthly, filter, propertyFilter))
            {
                var table = HydrometDataUtility.MPollTable(HydrometHost.PN, query, t1, t2);
                Console.WriteLine("Block "+block + " has "+table.Rows.Count     +" rows ");
                Console.WriteLine(query);
                SaveTableToSeries(db, table, TimeInterval.Monthly);
                block++;
            }
            Console.WriteLine("Finished importing monthly data");
        }



        private static void SetupDates(Arguments args, out DateTime t1, out DateTime t2)
        {
            t1 = DateTime.Now.Date.AddDays(-1);
            t2 = DateTime.Now.Date.AddDays(-1);

            if (args.Contains("t1"))
                t1 = ParseArgumentDate(args["t1"]);
            if (args.Contains("t2"))
                t2 = ParseArgumentDate(args["t2"]);

            Logger.WriteLine("t1= "+t1.ToShortDateString());
            Logger.WriteLine("t2= "+t2.ToShortDateString());
            
        }

        private static DateTime ParseArgumentDate(string dateString)
        {
            dateString = dateString.Trim();
            if( dateString.ToLower() == "yesterday")
            return DateTime.Now.AddDays(-1);

            if (dateString.ToLower() == "lastweek")
                return DateTime.Now.AddDays(-7);

            if (dateString.ToLower() == "lastyear")
                return DateTime.Now.AddDays(-365);

            var t1 = DateTime.Parse(dateString);
            return t1;
        }


       
    }
}
