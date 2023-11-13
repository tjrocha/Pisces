using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.IO;
using Reclamation.Core;

namespace Reclamation.Core.Tests
{
    [TestFixture]
    public class TestCredentialsFile
    {

        [Test]
        public void SaveManyCredentials()
        {
            var fn = FileUtility.GetTempFileName(".txt");

            TextFileCredentials c = new TextFileCredentials(fn);

            for (int i = 0; i < 15; i++)
            {
                var pw = "a".PadRight(i + 1,'B');
                var svr = "server" + i;
                Logger.WriteLine(svr);
                Logger.WriteLine(pw);
                c.Save(svr);
            }

        }
    }
}
