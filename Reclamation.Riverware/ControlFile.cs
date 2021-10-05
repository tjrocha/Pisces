using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Reclamation.Core;

namespace Reclamation.Riverware
{
    public class ControlFile:Reclamation.Core.TextFile
    {
        public ControlFile(string filename): base(filename)
        {
        }

        public bool TryParse(int index, string name, out string value)
        {
            return TryParse(index, name, out value, "");
        }
        public bool TryParse(int index, string name, out int value, int defaultValue)
        {
            return TryParse(index, name, out value, defaultValue, false);
        }
        public bool TryParse(int index, string name, out int value, int defaultValue, bool suppressWarnings)
        {
            value = defaultValue;
            string s="";
            if (!TryParse(index, name, out s, "0", suppressWarnings))
                return false;

            if (!int.TryParse(s, out value))
            {
                Console.WriteLine("Error: could not convert '" + s + "' to a integer ["+name+"]");
                return false;
            }
            return true;
        }
        public bool TryParse(int index, string name, out double value, double defaultValue)
        {
            value = defaultValue;
            string s = "";
            if (!TryParse(index, name, out s, "0"))
                return false;

            if (!double.TryParse(s, out value))
            {
                Console.WriteLine("Error: could not convert '" + s + "' to a double [" + name + "]");
                return false;
            }
            return true;
        }

        public bool TryParse(int index, string name, out bool value, bool defaultValue)
        {
            value = defaultValue;

            string s = "";
            if (!TryParse(index, name, out s, "false"))
                return false;

            if (!bool.TryParse(s, out value))
            {
                Console.WriteLine(string.Format("Error: could not convert '{0}' to a boolean [{1}]", s, name));
                return false;
            }
            return true;
        }

        public bool TryParse(int index, string name, out string value, string defaultValue)
        {
            return TryParse(index, name, out value, defaultValue, false);
        }
        public bool TryParse(int index, string name, out string value, string defaultValue, bool suppressWarnings)
        {
            value = defaultValue;
            if (index >= this.Length)
            {
                return false;
            }

            if (name == "file") // special case..
            {
                value = ParseFilename(this[index]);
                return true;
            }

            string pattern = name + "=" + @"(\S+)";
            var match = Regex.Match(this[index], pattern);
            if (!match.Success)
            {
                if (!suppressWarnings)
                {
                    Console.WriteLine("Warning: The control file parameter " + name + " was not specified. Example !" + name + "=abc123" + " in control file\n" + this[index]);
                }
                return false;
            }

            value = match.Groups[1].Value;

            return true;
        }

        public bool TryParseObjectSlot(int index, out string value)
        {
            value = "";

            int idx = this[index].IndexOf(":");
            if (idx > 0)
            {
                value = this[index].Substring(0, idx);
                return true;
            }
            else
            {
                return false;
            }
        }

        // filenames with spaces will be quoted
        private static string ParseFilename(string line)
        {
            string rval = "";
            int idx = line.IndexOf("file=\"");
            if (idx > 0)
            {
                int idx1 = idx + 6;
                int idx2 = line.IndexOf('\"', idx1);
                rval = line.Substring(idx1, idx2 - idx1);
            }
            else
            {
                rval = Regex.Match(line, @"file=(\S+)").Groups[1].Value;
            }

            return rval;
        }




        internal bool OptionExists(int index,string name)
        {
            string s="";
            return TryParse(index, name, out s,"",true);
        }
    }
}
