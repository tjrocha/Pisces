using System.Reflection.Metadata;
using Usda.gov.usda.egov.sc;
using Usda.gov.usda.egov.sc.wcc;

namespace UsdaTester
{
  internal class Program
  {
    static void Main(string[] args)
    {

      var ws = new AwdbWebServiceClient();
     
      System.Net.ServicePointManager.Expect100Continue = false;


      DateTime t1 = DateTime.Now;
      DateTime t2 = DateTime.Now;

     // var data = ws.getData(new string[] {"a","b","c" }, "", 1, null,
       //   dur, true, t1.ToString("yyyy-MM-dd"), t2.ToString("yyyy-MM-dd"), false, false);

      Console.WriteLine("Hello, World!");
    }
  }
}