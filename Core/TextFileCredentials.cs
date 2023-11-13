using Reclamation.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Reclamation.Core
{
    /// <summary>
    /// Manage  TextFile of server names and credientals
    /// </summary>
    public class TextFileCredentials
    {
        TextFile tf;
        string m_fileName = "credentials.txt";
        public TextFileCredentials(string filename)
        {
            m_fileName = filename;
            if( !File.Exists(filename))
            {
                File.Create(filename).Dispose();
            }
            
            tf = new TextFile(m_fileName);
        }

        public void Save(string server)
        {
           int idx =  tf.IndexOf(server);    
            if( idx <0)
            {
                tf.Add(server);
            }
            tf.SaveAs(tf.FileName);
        }

        public bool Contains(string server)
        {
            return tf.IndexOf(server.Trim()) >= 0;
        }

    }
}
