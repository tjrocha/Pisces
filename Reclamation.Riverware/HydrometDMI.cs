using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Reclamation.TimeSeries;
using Reclamation.TimeSeries.Hydromet;

namespace Reclamation.Riverware
{
    /// <summary>
    /// Imports daily excel time-series data
    /// into riverware as a dmi
    /// Either imports all data in your spreadsheet
    /// or imports a single year for multiple year trace type run 
    /// Karl Tarbet October 2006
    /// </summary>
    public class HydrometDMI
    {
        ControlFile controlFile1;
        List<string> objectSlot = new List<string>();
        List<string> filename = new List<string>();
        List<string> pcode = new List<string>();
        List<string> cbtt = new List<string>();
        List<int> daysOffset = new List<int>();
        List<int> dayCount = new List<int>();
        List<int> slot_offset = new List<int>();
        List<bool> hasCount = new List<bool>();
        DateTime startDate;
        DateTime endDate;
        HydrometHost server;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="server">either pnhyd or yakhyd</param>
        /// <param name="controlFilename">riverware control file</param>
        /// <param name="startDate">riverware simulation start date</param>
        public HydrometDMI(string server, string controlFilename, 
            DateTime startDate, DateTime endDate)
        {
            controlFile1 = new ControlFile(controlFilename);
            this.startDate = startDate;
            this.endDate = endDate;

            if (server.ToLower() == "pnhyd")
            {
                this.server = HydrometHost.PNLinux;
            }
            else if (server.ToLower() == "yakhyd")
            {
                this.server = HydrometHost.Yakima;
            }
            else
            {
                var msg = string.Format("unknown server string: {0} only pnhyd or yakhyd supported", server);
                Console.WriteLine(msg);
            }
        }

        public void ExportTextFilesDMI()
        {
            ParseControlFile();
            WriteRiverwareFilesDMI(ReadFromHydromet());
        }

        public void GenerateFilesTrace(string format, string outdirectory)
        {
            ParseControlFile();
            WriteRiverwareFilesTrace(ReadFromHydromet(), format, outdirectory);
        }

        private SeriesList ReadFromHydromet()
        {
            SeriesList rval = new SeriesList();
            for (int i = 0; i < cbtt.Count; i++)
            {
                HydrometDailySeries s =
                    new HydrometDailySeries(cbtt[i], pcode[i], this.server);
                DateTime t1 = startDate.AddDays(daysOffset[i]);
                DateTime t2 = t1.AddDays(dayCount[i] - 1);

                if (!hasCount[i])
                {
                    t2 = endDate;
                }
                
                if (dayCount[i] < 1 && hasCount[i])
                {
                    Console.WriteLine("Warning: The number of days requested was " + dayCount[i] + "from hydromet");
                }

                s.Read(t1, t2);
                if (s.Count < dayCount[i] && hasCount[0])
                {
                    Console.WriteLine("Warning: the requested hydromet data is missing.");
                }

                rval.Add(s);
            }

            return rval;
        }

        private void WriteRiverwareFilesDMI(SeriesList list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                Series s = list[i];

                if (s.Count <= 0)
                    continue;

                var lines = new StringBuilder();
                lines.AppendLine("# this data was imported from Hydromet " + DateTime.Now.ToString());
                lines.AppendLine("# " + cbtt[i] + " " + pcode[i]);
                lines.AppendLine("start_date: " + s[0].DateTime.AddDays(slot_offset[i]).ToString("yyyy-MM-dd") + " 24:00");

                for (int j = 0; j < s.Count; j++)
                {
                    if (s[j].IsMissing)
                    {
                        Console.WriteLine(string.Format("{0} {1} Error: missing data {2}", cbtt[i], pcode[i], s[j]));
                        lines.AppendLine("NaN");
                    }
                    else
                    {
                        lines.AppendLine(string.Format("{0}", s[j].Value));
                    }
                }

                File.WriteAllText(filename[i], lines.ToString());
            }
        }

        private void WriteRiverwareFilesTrace(SeriesList list, string format, string outdirectory)
        {
            if (format == "txt")
            {
                WriteRiverwareTraceText(list, outdirectory);
            }
            else if (format == "xls")
            {
                WriteRiverwareTraceExcel(list, outdirectory);
            }
            else
            {
                var msg = string.Format("unknown output format: {0} only txt or xls", format);
                Console.WriteLine(msg);
            }
        }

        private void WriteRiverwareTraceExcel(SeriesList list, string outdirectory)
        {
            throw new NotImplementedException();
        }

        private void WriteRiverwareTraceText(SeriesList list, string outdirectory)
        {
            for (int i = 0; i < list.Count; i++)
            {
                Series s = list[i];
                
                if (s.Count <= 0)
                    continue;

                var header = new StringBuilder();
                header.AppendLine("# this data was imported from Hydromet " + DateTime.Now.ToString());
                header.AppendLine("# " + cbtt[i] + " " + pcode[i]);
                header.AppendLine("start_date: " + new DateTime(2000, startDate.Month, startDate.Day).ToString("yyyy-MM-dd") + " 24:00");

                var trace = 1;
                var lines = new StringBuilder();
                foreach (var pt in s)
                {
                    // skip leap year data, Feb 29
                    if (pt.DateTime.Month == 2 && pt.DateTime.Day == 29)
                        continue;

                    if (pt.DateTime.Month == startDate.Month && pt.DateTime.Day == startDate.Day)
                    {
                        lines.Clear();
                        lines.Append(header);
                    }
                    if (pt.IsMissing)
                    {
                        Console.WriteLine(string.Format("{0} {1} Error: missing data {2}", cbtt[i], pcode[i], pt));
                        lines.AppendLine("NaN");
                    }
                    else
                    {
                        lines.AppendLine(string.Format("{0}", pt.Value));
                    }

                    if (pt.DateTime.Month == endDate.Month && pt.DateTime.Day == endDate.Day)
                    {
                        var outputpath = Path.Combine(outdirectory, "trace" + trace);
                        Directory.CreateDirectory(outputpath);
                        File.WriteAllText(Path.Combine(outputpath, objectSlot[i] + ".txt"), lines.ToString());
                        trace += 1;
                    }
                }
            }
        }

        private void ParseControlFile()
        {
            for (int i = 0; i < controlFile1.Length; i++)
            {
                if (controlFile1[i].StartsWith("#") || controlFile1[i].Trim() == "")
                    continue;

                controlFile1.TryParseObjectSlot(i, out string objectSlot);
                controlFile1.TryParse(i, "file", out string filename);
                controlFile1.TryParse(i, "cbtt", out string cbtt);
                controlFile1.TryParse(i, "pcode", out string pcode);
                controlFile1.TryParse(i, "days_offset", out int daysOffset, 0, true);
                controlFile1.TryParse(i, "slot_offset", out int slot_offset, 0, true);
                var hasCount = controlFile1.TryParse(i, "count", out int dayCount, -1, true);

                this.objectSlot.Add(objectSlot);
                this.filename.Add(filename);
                this.cbtt.Add(cbtt);
                this.pcode.Add(pcode);
                this.daysOffset.Add(daysOffset);
                this.slot_offset.Add(slot_offset);
                this.dayCount.Add(dayCount);
                this.hasCount.Add(hasCount);
            }
        }

    }
}
