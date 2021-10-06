using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Reclamation.TimeSeries;
using Reclamation.TimeSeries.Hydromet;

namespace Reclamation.Riverware
{
    /// <summary>
    /// Imports daily hydromet time-series data
    /// into riverware as a dmi
    /// Either creates timeseries of all data in hydromet
    /// or creates single year traces for multiple year 
    /// trace type run Karl Tarbet October 2006
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
        List<bool> hasDayCount = new List<bool>();
        List<bool> mrm_init = new List<bool>();
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
                throw new NotImplementedException($"unknown server string: '{server}' only pnhyd or yakhyd supported");
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
                DateTime t1 = mrm_init[i] ? DateTime.Now.AddDays(daysOffset[i] - 1) : startDate.AddDays(daysOffset[i]);
                DateTime t2 = hasDayCount[i] ? t1.AddDays(dayCount[i] - 1) : endDate;

                if (dayCount[i] < 1 && hasDayCount[i])
                {
                    Console.WriteLine($"Warning: The number of days requested was '{dayCount[i]}' days from hydromet");
                }

                s.Read(t1, t2);
                if (s.Count < dayCount[i] && hasDayCount[0])
                {
                    Console.WriteLine("Warning: the requested hydromet data is missing.");
                }

                if (mrm_init[i])
                {
                    t1 = startDate.AddDays(daysOffset[i]);
                    for (int j = 0; j < s.Count; j++)
                    {
                        var pt = s[j];
                        pt.DateTime = t1;
                        s[j] = pt;
                        t1 = t1.AddDays(1);
                    }
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
                lines.AppendLine($"# this data was imported from Hydromet {DateTime.Now}");
                lines.AppendLine($"# {cbtt[i]} {pcode[i]}");
                lines.AppendLine($"start_date: {s[0].DateTime.AddDays(slot_offset[i])}:yyyy-MM-dd 24:00");

                for (int j = 0; j < s.Count; j++)
                {
                    if (s[j].IsMissing)
                    {
                        Console.WriteLine($"{cbtt[i]} {pcode[i]} Error: missing data {s[j]}");
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
                throw new NotImplementedException($"Error: unknown output format: '{format}' only txt or xls");
            }
        }

        private void WriteRiverwareTraceExcel(SeriesList list, string outdirectory)
        {
            throw new NotImplementedException("Error: xls output currently unsupported");
        }

        private void WriteRiverwareTraceText(SeriesList list, string outdirectory)
        {
            for (int i = 0; i < list.Count; i++)
            {
                Series s = list[i];
                
                if (s.Count <= 0)
                    continue;

                var trace = 1;
                for (int j = startDate.Year; j < endDate.Year; j++)
                {
                    var dt1 = new DateTime(j, startDate.Month, startDate.Day);
                    var dt2 = new DateTime(j + 1, endDate.Month, endDate.Day);

                    var wySeries = s.Subset(dt1, dt2);

                    var lines = new StringBuilder();
                    lines.AppendLine($"# this data was imported from Hydromet {DateTime.Now}");
                    lines.AppendLine($"# {cbtt[i]} {pcode[i]}");
                    lines.AppendLine($"start_date: {new DateTime(2000, startDate.Month, startDate.Day)}:yyyy-MM-dd 24:00");

                    foreach (var pt in wySeries)
                    {
                        // skip leap year data, Feb 29
                        if (pt.DateTime.Month == 2 && pt.DateTime.Day == 29)
                            continue;

                        if (pt.IsMissing)
                        {
                            Console.WriteLine($"{cbtt[i]} {pcode[i]} Error: missing data {pt}");
                            lines.AppendLine("NaN");
                        }
                        else
                        {
                            lines.AppendLine($"{pt.Value}");
                        }
                    }

                    var outputpath = Path.Combine(outdirectory, $"trace{trace}");
                    Directory.CreateDirectory(outputpath);
                    File.WriteAllText(Path.Combine(outputpath, $"{objectSlot[i]}.txt"), lines.ToString());
                    trace += 1;
                }
            }
        }

        private void ParseControlFile()
        {
            string objectSlot;
            string filename;
            string cbtt;
            string pcode;
            int daysOffset;
            int slot_offset;
            int dayCount;
            bool mrm_init;

            for (int i = 0; i < controlFile1.Length; i++)
            {
                if (controlFile1[i].StartsWith("#") || controlFile1[i].Trim() == "")
                    continue;

                controlFile1.TryParseObjectSlot(i, out objectSlot);
                controlFile1.TryParse(i, "file", out filename);
                controlFile1.TryParse(i, "cbtt", out cbtt);
                controlFile1.TryParse(i, "pcode", out pcode);
                controlFile1.TryParse(i, "days_offset", out daysOffset, 0, true);
                controlFile1.TryParse(i, "slot_offset", out slot_offset, 0, true);
                var hasCount = controlFile1.TryParse(i, "count", out dayCount, -1, true);
                controlFile1.TryParse(i, "mrm_init", out mrm_init, false);

                this.objectSlot.Add(objectSlot);
                this.filename.Add(filename);
                this.cbtt.Add(cbtt);
                this.pcode.Add(pcode);
                this.daysOffset.Add(daysOffset);
                this.slot_offset.Add(slot_offset);
                this.dayCount.Add(dayCount);
                this.hasDayCount.Add(hasCount);
                this.mrm_init.Add(mrm_init);
            }
        }

    }
}
