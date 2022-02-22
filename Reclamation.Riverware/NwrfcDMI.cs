using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Reclamation.Core;

namespace Reclamation.Riverware
{
    public class NwrfcDMI
    {
        ControlFile controlFile1;
        List<string> objectSlot = new List<string>();
        List<string> cbtt = new List<string>();
        string host;
        DateTime startDate;
        DateTime endDate;

        public NwrfcDMI(string host, string controlFilename, DateTime startDate, DateTime endDate)
        {
            controlFile1 = new ControlFile(controlFilename);
            this.host = host;
            this.startDate = startDate;
            this.endDate = endDate.AddDays(1);
        }

        public void GenerateFilesTrace(string format, string outdirectory)
        {
            ParseControlFile();

            for (int i = 0; i < cbtt.Count; i++)
            {
                var data = GetEspData(cbtt[i]);
                WriteRiverwareFilesTrace(data, format, outdirectory, objectSlot[i]);
            }
        }

        private string[] GetEspData(string cbtt)
        {
            /* 
             * https://www.nwrfc.noaa.gov/chpsesp/ensemble/natural/LUCI1N_SQIN.ESPF0.csv
             * 
             * PATTERN:
             * https://www.nwrfc.noaa.gov/chpsesp/ensemble/x/????#y_SQIN.ESPFz.csv
             * x = {natural, watersupply}
             * y = station alpha(?)-numeric(#) code
             * z = {0,10,M}
             */

            string url = "https://www.nwrfc.noaa.gov/chpsesp/ensemble/natural/LUCI1N_SQIN.ESPF0.csv";

            var lookAhead = host.Substring(5);

            url = url.Replace("LUCI1N", cbtt + "N");
            url = url.Replace("ESPF0", "ESPF" + lookAhead);

            return Web.GetPage(url);
        }


        private void WriteRiverwareFilesTrace(string[] data, string format, string outdirectory, string objectSlot)
        {
            if (format == "txt")
            {
                WriteRiverwareTraceText(data, outdirectory, objectSlot);
            }
            else if (format == "xls")
            {
                WriteRiverwareTraceExcel(data, outdirectory, objectSlot);
            }
            else
            {
                throw new NotImplementedException($"Error: unknown output format: '{format}' only txt or xls");
            }

        }

        private void WriteRiverwareTraceExcel(string[] data, string outdirectory, string objectSlot)
        {
            throw new NotImplementedException("Error: xls output currently unsupported");
        }

        private void WriteRiverwareTraceText(string[] data, string outdirectory, string objectSlot)
        {
            // Rather than loop through dates foreach trace I decided to loop once and store the
            // data in Dictionary<DateTime, List<string>>, then loop through the dictionary to
            // write data foreach trace which should be faster.
            var results = GetTraceDataFromString(data);

            for (int i = 0; i < results.First().Value.Count; i++)
            {
                var today = DateTime.Now;
                var wy = today.Month > endDate.Month ? 2000 : 2001;

                var lines = new StringBuilder();
                lines.AppendLine($"# this data was imported from Northwest River Forecast Center {today}");
                lines.AppendLine($"# {data[0]}.csv");
                lines.AppendLine($"start_date: {new DateTime(wy, results.First().Key.Month, results.First().Key.Day):yyyy-MM-dd 24:00}");
                lines.AppendLine($"end_date: {new DateTime(wy, results.Last().Key.Month, results.Last().Key.Day):yyyy-MM-dd 24:00}");

                foreach (var item in results.Keys)
                {
                    lines.AppendLine(results[item][i]);
                }

                var outputpath = Path.Combine(outdirectory, $"trace{i + 1}");
                Directory.CreateDirectory(outputpath);
                File.WriteAllText(Path.Combine(outputpath, $"{objectSlot}.txt"), lines.ToString());
            }

        }

        private Dictionary<DateTime, List<string>> GetTraceDataFromString(string[] data)
        {
            var rval = new Dictionary<DateTime, List<string>>();

            var indices = GetIndices(data);
            var traceIndex = indices.Item1;
            var traceCount = indices.Item2;
            var dataIndex = indices.Item3;

            if (traceIndex * traceCount * dataIndex < 0)
            {
                Console.WriteLine($"unable to parse data -- {data[0]}.csv");
                return rval;
            }

            DateTime date;
            double val;
            for (int i = dataIndex; i < data.Length; i++)
            {
                if (data[i].Trim() == "")
                    continue;

                var lineItems = data[i].Split(',');


                DateTime.TryParse(lineItems[0], out date);

                // skip leap year data, Feb 29
                if (date.Month == 2 && date.Day == 29)
                    continue;

                if (date.Hour == 0)
                {
                    // riverware defines this as the previous day's midnight 24:00
                    date = date.AddDays(-1);

                    rval.Add(date, new List<string>());

                    for (int j = 1; j <= traceCount; j++)
                    {
                        if (Double.TryParse(lineItems[j], out val))
                            rval[date].Add($"{val * 1000}");
                        else
                            rval[date].Add("NaN");
                    }
                }

                if (date.Month == endDate.Month && date.Day == endDate.Day)
                    break;
            }

            return rval;
        }

        private Tuple<int, int, int> GetIndices(string[] data)
        {
            int traceIndex = -1;
            int traceCount = -1;
            int dataIndex = -1;

            for (int i = 0; i < data.Length; i++)
            {
                var lineItems = data[i].Split(',');

                if (lineItems.Length == 1 || lineItems[0].StartsWith("QPFDAYS"))
                    continue;

                if (lineItems[0] == "FCST_VALID_TIME_GMT")
                {
                    for (int j = 1; j < lineItems.Length; j++)
                    {
                        if (lineItems[j] == startDate.Year.ToString())
                        {
                            traceIndex = j;
                            dataIndex = i + 1;
                        }

                        if (lineItems[j] == endDate.Year.ToString())
                        {
                            traceCount = j;
                            return new Tuple<int, int, int>(traceIndex, traceCount, dataIndex);
                        }
                    }
                }
            }

            return new Tuple<int, int, int>(-1, -1, -1);
        }

        private void ParseControlFile()
        {
            string objectSlot;
            string cbtt;

            for (int i = 0; i < controlFile1.Length; i++)
            {
                if (controlFile1[i].StartsWith("#") || controlFile1[i].Trim() == "")
                    continue;

                controlFile1.TryParseObjectSlot(i, out objectSlot);
                controlFile1.TryParse(i, "cbtt", out cbtt);

                this.objectSlot.Add(objectSlot);
                this.cbtt.Add(cbtt);
            }
        }
    }
}
