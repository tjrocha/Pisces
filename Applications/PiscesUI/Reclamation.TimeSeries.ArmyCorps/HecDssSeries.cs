using System;
using Reclamation.Core;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Hec.Dss;

namespace Reclamation.TimeSeries.Hec
{
    public class HecDssSeries: Series
    {
        DssPath path;
        string m_filename;
        string m_dssPath;
        public HecDssSeries(string filename, string dssPath)
        {
            ExternalDataSource = true;
            path = new DssPath(dssPath);
            m_filename = filename;
            m_dssPath = dssPath;

            FileInfo fi = new FileInfo(filename);
            Name = path.Apart+" " +path.Bpart + " " + path.Cpart + " " + path.Epart + " " + path.Fpart;
            SiteID = path.Bpart;
            this.ConnectionString = "FileName=" + filename
               + ";LastWriteTime=" + fi.LastWriteTime.ToString(DateTimeFormatInstantaneous)
               + ";DssPath="+dssPath;
            Provider = "HecDssSeries";
            EstimateInterval();
            Source = "hecdss"; // for icon name
            ReadOnly = true;
            HasFlags = true;
        }

        

        public HecDssSeries(TimeSeriesDatabase db,Reclamation.TimeSeries.TimeSeriesDatabaseDataSet.SeriesCatalogRow sr)
            : base(db,sr)
        {
            ExternalDataSource = true;
            m_filename = ConnectionStringToken("FileName");
            if (!Path.IsPathRooted(m_filename))
            {
                string dir = Path.GetDirectoryName(m_db.DataSource);
                m_filename = Path.Combine(dir, m_filename);
            }
            m_dssPath = ConnectionStringToken("DssPath");
            path = new DssPath(m_dssPath);
            ReadOnly = true;
            ScenarioName = Path.GetFileNameWithoutExtension(m_filename);
            InitTimeSeries(null, this.Units, this.TimeInterval, this.ReadOnly, true, true);
            Appearance.LegendText = Name;
        }
        /// <summary>
        /// Creates Scenario based on scenaroName as dss filename without extension (.dss)
        /// </summary>
        public override Series CreateScenario(TimeSeriesDatabaseDataSet.ScenarioRow scenario)
        {

            if (scenario.Name == ScenarioName)
            {
                this.Appearance.LegendText = scenario.Name + " " + Name;
                return this;
            }

            string path = Path.GetDirectoryName(m_filename);
            string fn = Path.Combine(path, scenario.Name + ".dss");
            Logger.WriteLine("Reading series from " + fn);
            if (!File.Exists(fn))
            {
                Logger.WriteLine("Error: Can't create scenario");
                return new Series();
            }


            var rval = new HecDssSeries(m_filename, m_dssPath);
            rval.Name = this.Name;
            rval.Appearance.LegendText = scenario.Name + " " + Name;

            rval.SiteID = this.SiteID;
            rval.TimeInterval = this.TimeInterval;
            return rval;
        }



        private void EstimateInterval()
        {
            this.TimeInterval = TimeInterval.Irregular; // default.
            string e = path.Epart.ToUpper();

            if (e.IndexOf("IR-") == 0)
                this.TimeInterval = TimeInterval.Irregular;
            else if (e.IndexOf("1DAY") == 0)
                this.TimeInterval = TimeInterval.Daily;
            else if (e.IndexOf("1HOUR") == 0)
                this.TimeInterval = TimeInterval.Hourly;
            else if (e.IndexOf("1MON") == 0)
                this.TimeInterval = TimeInterval.Monthly;
            else if (e.IndexOf("1WEEK") == 0)
                this.TimeInterval = TimeInterval.Weekly;
            else if (e.IndexOf("1YEAR") == 0)
                this.TimeInterval = TimeInterval.Yearly;
        }

     
        private string PathToFile
        {
            get { return Path.GetDirectoryName(m_filename); }
        }

        protected override void ReadCore(DateTime t1, DateTime t2)
        {
            Clear();
            using (DssReader r = new DssReader(m_filename))
            {
             var s = r.GetTimeSeries(path,t1,t2);
             this.Table = s.ToDataTable(false,false);
             this.Units = s.Units;
             this.Parameter = path.Cpart;
             this.Table.AcceptChanges();
            }
        }


    }
}
