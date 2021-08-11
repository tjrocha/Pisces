using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Reclamation.Core;
using Reclamation.TimeSeries;
using System.IO;
using Math = Reclamation.TimeSeries.Math;

namespace Pisces.NunitTests.Estimation
{
    [TestFixture]
    public class TestScaleDailyToMonthlyVolume
    {

        public static void Main()
        {
            TestScaleDailyToMonthlyVolume t = new TestScaleDailyToMonthlyVolume();
            t.ScaleDailyToMonthlyVolume();
        }

        static DateTime t1 = new DateTime(1927, 10, 1);
        static DateTime t2 = new DateTime(1949, 9, 30);

        string path = "";
        public TestScaleDailyToMonthlyVolume()
        {
            string zipFile = Path.Combine(TestData.DataPath, "ScaleDailyToMonthlyVolumeTest.zip");
             path = FileUtility.GetTempPath() + @"\ScaleDailyToMonthlyVolumeTest.pdb";
            ZipFileUtility.UnzipFile(zipFile,path);
        }

        [Test]
        public void ScaleDailyToMonthlyVolume()
        {
            SQLiteServer pDB = new SQLiteServer(path);
            TimeSeriesDatabase DB = new TimeSeriesDatabase(pDB,false);

            // Reads input data required by the calculation
            Series partialDaily = DB.GetSeriesFromName("PALI_QD");
            partialDaily.Read();// Series with gaps
            Series completeDaily = DB.GetSeriesFromName("PALI_UpstreamGain");
            completeDaily.Read();// Complete upstream gain series
            Series monthly = DB.GetSeriesFromName("Monthly13032500:Snake River Nr Irwin");
            monthly.Read();// Monthly data
            Series known= DB.GetSeriesFromName("PALI_test");
            known.Read(t1,t2);

            Series infilled = Math.ScaleDailyToMonthlyVolume(partialDaily, completeDaily, monthly);
            var s = infilled.Subset(t1, t2);
            s.TimeInterval = TimeInterval.Daily;
            
            var diff = Math.Sum(known - s);
            Assert.IsTrue(System.Math.Abs(diff) < 0.01,"Error");
        }        
    }
}