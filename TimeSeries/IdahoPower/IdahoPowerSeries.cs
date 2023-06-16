using Reclamation.Core;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.XPath;

namespace Reclamation.TimeSeries.IdahoPower
{
    public class IdahoPowerSeries:Series
    {

        public IdahoPowerSeries(string stationID, TimeInterval interval)
        {
            this.TimeInterval = interval;
            this.SiteID = stationID;
        }


        protected override void ReadCore(DateTime t1, DateTime t2)
        {
            Clear();
            var s = ReadFromIdahoPower(SiteID, t1, t2);
            this.Add(s);
        }

/*
#Bulk Export - Points as recorded	
13227000
Bully Crk nr Vale, OR
Flow HR.15
Timestamp (UTC-07:00)	Value (ft^3/s)
6/15/2023 0:00	11.3
6/15/2023 0:15	11.6
6/15/2023 0:30	11.9
6/15/2023 0:45	11.9
6/15/2023 1:00	12.3
6/15/2023 1:15	12.3
...
 */
        private static Series ReadFromIdahoPower(string id, DateTime t1, DateTime t2)
        {
            //var url = "https://idastream.idahopower.com/Export/DataSet?DataSet=Flow+HR.15@13227000&DateRange=Custom&StartTime=2017-05-12&EndTime=2017-05-26&Compressed=false&ExportFormat=csv ";
            var url = "https://idastream.idahopower.com/Export/DataSet?DataSet="
                + id + "&DateRange=Custom&StartTime=" + t1.Date.ToString("yyyy-MM-dd")
                + "&EndTime=" + t2.AddDays(1).ToString("yyyy-MM-dd") 
                + "&Compressed=false&ExportFormat=csv";

            //var fn = DownloadAndUnzip(url);
            var csvFile = FileUtility.GetTempFileName(".csv");
            Console.WriteLine("Downloading: " + csvFile);
            Web.GetFile(url, csvFile);

            TextSeries s = new TextSeries(csvFile);
            s.Read();
            s.Trim(t1, t2);
            return s;
        }


        private static string DownloadAndUnzip(string url)
        {
            var zip = FileUtility.GetTempFileName(".zip");
            Console.WriteLine("Downloading: " + url);
            Web.GetFile(url, zip);


            var csv = FileUtility.GetTempFileName(".csv");
            Console.WriteLine("Unzipping to-> " + csv);
            ZipFileUtility.UnzipFile(zip, csv);
            return csv;
        }
    }
}
