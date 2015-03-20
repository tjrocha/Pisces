﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Options;
using Reclamation.TimeSeries;
using Reclamation.Core;
using System.IO;

namespace PiscesWebServices
{
    class Program
    {
        /// <summary>
        /// Examples:
        /// 
        /// --cgi=sites --propertyFilter=program:agrimet --json_required_properties=json_extra
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {

            string propertyFilter="";
            var cgi = "";
            var json_property_stubs="";
            var payload = "";
            var p = new OptionSet();


            p.Add("cgi=","required cgi to execute cgi=sites or cgi=series",x => cgi=x);
            p.Add("json_property_stubs=", "comma separated list of properties (i.e. 'region,url,') to created empty stubs if neeed ",
                              x => json_property_stubs = x);
            p.Add("propertyFilter=", "property filter like program:agrimet", x => propertyFilter = x);
            p.Add("payload=","test query data for a CGI",x => payload =x);
            try
            {
                p.Parse(args);
            }
            catch (OptionException e)
            {
                Console.WriteLine(e.Message);
            }
            if (cgi == "")
            {
                ShowHelp(p);
                return;
            }

            var db = TimeSeriesDatabase.InitDatabase(new Arguments(args));

            db.Inventory();

            if (cgi == "sites")
            {
                SiteDump d = new SiteDump(db);
                d.Execute(json_property_stubs.Split(','), propertyFilter);
            }
            if (cgi == "instant")
            {
                CsvWriter c = new CsvWriter(db);
                c.Run(payload);
            }
            
        }

        static void ShowHelp(OptionSet p)
        {
            Console.WriteLine("PiscesWebServices");
            Console.WriteLine();
            Console.WriteLine("Options:");
            p.WriteOptionDescriptions(Console.Out);
        }

    }
}