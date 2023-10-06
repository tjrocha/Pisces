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
        private static bool alreadyInitialized = false;

        public static bool Intranet
        {
            get 
            {
                if (!alreadyInitialized)
                {
                    string prefix = System.Configuration.ConfigurationManager.AppSettings["InternalNetworkPrefix"];
                    var ipList = Dns.GetHostEntry(Dns.GetHostName()).AddressList;
                    foreach (var ip in ipList)
                    {
                        if (ip.MapToIPv4().ToString().StartsWith(prefix))
                        {
                            alreadyInitialized = true;
                            s_intranet = true;
                            break;
                        }
                    } 
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
