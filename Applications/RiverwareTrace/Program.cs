using System;
using Reclamation.Core;
using Reclamation.Riverware;

namespace Reclamation.RiverwareTrace
{
    internal enum Host { pnhyd, yakhyd, nwrfc0, nwrfc10, nwrfcM };

    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Arguments arguments = new Arguments(args);

                DateTime dt1 = DateTime.Parse(args[0]);
                DateTime dt2 = DateTime.Parse(args[1]);
                string controlFilename = args[2];
                var host = (Host)Enum.Parse(typeof(Host), args[3]);
                string outputType = args[4];
                string outputDirectory = args[5];

                ProcessArguments(dt1, dt2, controlFilename, host, outputType, outputDirectory);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Usage();
            }
        }

        private static void Usage()
        {
            Console.WriteLine();
            Console.WriteLine("Usage: RiverwareTrace.exe dt1 dt2 controlfile host txt outputdirectory");
            Console.WriteLine("Where:");
            Console.WriteLine("    dt1 is the start of the water for the start year of interest");
            Console.WriteLine("    dt2 is the end of the water for the end year of interest");
            Console.WriteLine("    controlfile is a RiverWare DMI control file with !cbtt/!pcode for Hydromet and !cbtt for NWRFC");
            Console.WriteLine("    host is pnhyd|yakhyd|nwrfc0|nwrfc10|nwrfcM server data to read");
            Console.WriteLine("    txt is txt|xls output type requested");
            Console.WriteLine("    outputdirectory is directory to write program output");
        }

        private static void ProcessArguments(DateTime dt1, DateTime dt2, string controlFilename, 
            Host host, string outputType, string outputDirectory)
        {
            switch (host)
            {
                case Host.pnhyd:
                case Host.yakhyd:
                    var hydrometDMI = new HydrometDMI(host.ToString(), controlFilename, dt1, dt2);
                    hydrometDMI.GenerateFilesTrace(outputType, outputDirectory);
                    break;
                case Host.nwrfc0:
                case Host.nwrfc10:
                case Host.nwrfcM:
                    var nwrfcDMI = new NwrfcDMI(host.ToString(), controlFilename, dt1, dt2);
                    nwrfcDMI.GenerateFilesTrace(outputType, outputDirectory);
                    break;
                default:
                    break;
            }
        }
    }
}
