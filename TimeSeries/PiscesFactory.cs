﻿using Reclamation.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SeriesCatalogRow = Reclamation.TimeSeries.TimeSeriesDatabaseDataSet.SeriesCatalogRow;

namespace Reclamation.TimeSeries
{
    /// <summary>
    /// TimeSeriesFactory creates objects stored in the TimeSeriesDatabase
    /// </summary>
    class PiscesFactory
    {
        TimeSeriesDatabase db;
        public PiscesFactory(TimeSeriesDatabase db)
        {
            this.db = db;
        }

        public IEnumerable<Series> GetSeries(TimeInterval interval, string filter = "",string propertyFilter="")
        {
            string sql = " timeinterval = '" + interval.ToString() + "'";
            if (filter != "")
                sql += " AND " + filter;

            var sc = db.GetSeriesCatalog(sql,propertyFilter);

            foreach (var sr in sc)
            {
                var s = GetSeries(sr);

                yield return s;
            }
        }

        /// <summary>
        ///  returns a list of CalculationSeries
        /// </summary>
        /// <param name="interval"></param>
        /// <param name="filter">supplement for the SQL where clause</param>
        /// <param name="propertyFilter">two part filter for the siteproperty table i.e.  'program:agrimet'</param>
        /// <returns></returns>
        public List<CalculationSeries> GetCalculationSeries(TimeInterval interval, string filter="", string propertyFilter="")
        {
            string sql = "provider = 'CalculationSeries' AND "
                + " timeinterval = '" + interval.ToString() + "'";
            if (filter != "")
                sql += " AND "+filter;

            var sc = db.GetSeriesCatalog(sql,propertyFilter);

            List<CalculationSeries> list1 = new List<CalculationSeries>();
            foreach (var sr in sc)
            {
                var s = GetSeries(sr) as CalculationSeries;
                if( s.Enabled == 1)
                  list1.Add(s);
            }
            return list1;
        }


        public PiscesFolder GetFolder(int id)
        {
            TimeSeriesDatabaseDataSet.SeriesCatalogRow sr = db.GetSeriesRow(id);
           PiscesObject o = CreateObject(sr);

           if (!(o is PiscesFolder))
           {
               throw new ArgumentException("this object is not a PiscesFolder "+id);
           }
           return o as PiscesFolder;
        }


        private static List<Type> seriesTypeList = null;

        public Series GetSeries(TimeSeriesDatabaseDataSet.SeriesCatalogRow sr)
        {
         
            Series s = null;// = new Series(sr, db);
            int sdi = sr.id;
            try
            {
                // backwards compatibility with VAX/Linux HydrometHost options
                if (sr.ConnectionString.Contains("PNLinux")
                    || sr.ConnectionString.Contains("YakimaLinux")
                    || sr.ConnectionString.Contains("Yakima"))
                {
                    var entries = sr.ConnectionString.Split(';');
                    foreach (var entry in entries)
                    {
                        var svr = "server=";
                        if (entry.Contains(svr))
                        {
                            sr.ConnectionString = sr.ConnectionString.Replace(entry, $"{svr}PN");
                            break;
                        }
                    }
                }
                
                if (sr.Provider.Trim() == "")
                    sr.Provider = "Series";

                // most common cases -- avoid reflection
                if (sr.Provider == "Series")
                { 
                    s = new Series(db, sr);
                    s.TimeSeriesDatabase = this.db;
                    return s;
                }
                // most common cases -- avoid reflection
                if (sr.Provider == "CalculationSeries")
                {
                    s = new CalculationSeries(db, sr);
                    s.TimeSeriesDatabase = this.db;
                    return s;
                }


                if (seriesTypeList == null)
                {
                    seriesTypeList = new List<Type>();
                    var asmList = AppDomain.CurrentDomain.GetAssemblies();

                    foreach (Assembly item in asmList)
                    {
                        if (item.FullName.IndexOf("Reclamation.") <0
                            && item.FullName.IndexOf("Pisces") <0
                            && item.FullName.IndexOf("HDB") <0 )
                            continue;

                        var types = item.GetTypes().Where(x => x.BaseType == typeof(Series));
                        seriesTypeList.AddRange(types);
                    }
                }

                for (int i = 0; i < seriesTypeList.Count; i++)
                {
                    Type t = seriesTypeList[i];
                    if (t.Name == sr.Provider)
                    {
                        Type[] parmFaster = new Type[] {  typeof(TimeSeriesDatabase), typeof(SeriesCatalogRow) };
                        var cInfoFaster = t.GetConstructor(parmFaster);

                        if (cInfoFaster != null)
                        {
                            object o = cInfoFaster.Invoke(new object[] {  db, sr });
                            if (o is Series)
                                s = o as Series;
                            else
                                throw new InvalidOperationException("Provider '" + sr.Provider + "' is not a Series");
                        }
                       
                            else
                            {
                                throw new InvalidOperationException("Can't find constructor for '" + sr.Provider + "'");
                            }

                        break;
                    }

                }
            }
            catch(Exception excep)
            {
                if (excep.InnerException != null)
                {
                    Logger.WriteLine(excep.InnerException.Message);
                    throw excep.InnerException;
                }
                var msg = excep.Message + "\n" + sr.Provider;
                Logger.WriteLine(msg);
                throw new Exception(msg);
            }

            if (s == null)
            {
//                Logger.WriteLine("No Class found for '"+sr.Provider +"'  ID= "+sr.id+" Name = "+sr.Name);
                s = new Series( db, sr);
            }
            s.TimeSeriesDatabase = this.db;
            return s;
        }


        public Series GetSeries(int id)
        {
            SeriesCatalogRow si = db.GetSeriesRow(id);
            return GetSeries(si);
        }

        public PiscesObject CreateObject(SeriesCatalogRow sr)
        {
            PiscesObject rval = null;
            if (sr.IsFolder == 1)
            {
                rval = new PiscesFolder(db, sr);
            }
            else
            {
                return GetSeries(sr); //11.53125 seconds elapsed.
            }

        //    rval.Icon = AssignIcon(sr.iconname);
            return rval;
        }





    }
}
