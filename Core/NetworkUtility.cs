using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Net.NetworkInformation;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;

namespace Reclamation.Core
{
    public class NetworkUtility
    {
        private static bool s_intranet = false;
        private static bool knowMyIP = false;
        private static bool alreadyInitialized = false;

        // loop through all machine ip's to determine if the machine is connected to the internal network
        //https://superuser.com/questions/1034471/how-do-i-extract-the-ipv4-ip-address-from-the-output-of-ipconfig
        //https://stackoverflow.com/questions/8529181/which-terminal-command-to-get-just-ip-address-and-nothing-else
        public static bool Intranet
        {
            get
            {
                string prefix = System.Configuration.ConfigurationManager.AppSettings["InternalNetworkPrefix"];
                if (!knowMyIP)
                {
                    s_intranet = MyIpStartsWith(prefix);
                    knowMyIP = true;
                }

                try
                {
                    if (!alreadyInitialized)
                    {
                        var tempFile = FileUtility.GetTempFileName(".csv");

                        var cmd = new Process();
                        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        {
                            cmd.StartInfo.FileName = "cmd.exe";
                            cmd.StartInfo.Arguments = $"/C for /F \"tokens=14\" %A in ('\"ipconfig | findstr IPv4\"') do echo %A >> {tempFile}";
                        }
                        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                        {
                            cmd.StartInfo.FileName = "/bin/bash";
                            cmd.StartInfo.Arguments = $"ip -4 addr | grep -oP '(?<=inet\\s)\\d+(\\.\\d+){{3}}' >> {tempFile}";
                        }
                        else
                        {
                            throw new PlatformNotSupportedException($"error: unsupported platform: {RuntimeInformation.OSDescription}");
                        }

                        cmd.StartInfo.UseShellExecute = false;
                        cmd.StartInfo.CreateNoWindow = true;
                        cmd.StartInfo.RedirectStandardOutput = true;
                        cmd.Start();
                        cmd.WaitForExit();
                        cmd.Close();

                        var result = File.ReadAllLines(tempFile);
                        foreach (var item in result)
                        {
                            if (item.StartsWith(prefix))
                            {
                                alreadyInitialized = true;
                                s_intranet = true;
                                knowMyIP = true;
                                break;
                            }
                        }
                        File.Delete(tempFile);
                    }
                }
                catch
                {

                    s_intranet = false;
                }
                return s_intranet;
            }
        }

        public static bool MyIpStartsWith(string prefix)
        {
            IPHostEntry s_IPHost = Dns.GetHostEntry(Dns.GetHostName());
           
            foreach (var item in s_IPHost.AddressList)
            {
                string ip = item.ToString();
                if (prefix != null && ip.IndexOf(prefix) == 0)
                {
                    Logger.WriteLine("Yes, my Ip address starts with "+prefix);
                    return true;
                }
                Logger.WriteLine(ip);
            }
            return false;
        }

        /// <summary>
        /// Check if there is a public internet connection 
        /// https://stackoverflow.com/questions/2031824/what-is-the-best-way-to-check-for-internet-connectivity-using-net
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private bool checkInternetConnection()
        {
            try
            {
                using (var client = new WebClient())
                {
                    using (var stream = client.OpenRead("http://www.google.com"))
                    {
                        return true;
                    }
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
