using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Net.NetworkInformation;

namespace Reclamation.Core
{
    public class NetworkUtility
    {
        private static bool s_intranet = false;
        private static bool knowMyIP = false;
        private static bool alreadyInitialized = false;

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

                // Check if we can see the lrgs1 server
                try
                {
                    if (!alreadyInitialized)
                    {
                        alreadyInitialized = true;
                        var lrgs1Host = Dns.GetHostEntry("IBRSACDC01.bor.doi.net");
                        s_intranet = true;
                        knowMyIP = true;
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
