using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace SilverPotato
{
    public class QueryString
    {

        Dictionary<string, string> data;

        public QueryString()
        {
            data = new Dictionary<string, string>();
        }
        public override string ToString()
        {
            StringBuilder sbOutput = new StringBuilder(10);

            bool HaveOutputAnything = false;
            foreach (KeyValuePair<string, string> kvp in data)
            {
                if (HaveOutputAnything)
                    sbOutput.Append("&");
                else
                    sbOutput.Append("?");

                sbOutput.Append(kvp.Key);
                sbOutput.Append("=");
                sbOutput.Append(kvp.Value);

                if (!HaveOutputAnything)
                    HaveOutputAnything = true;
            }

            return sbOutput.ToString();
        }
        public void AddKeyValuePair(string key, string value)
        {
            data.Add(key, value);
        }

    }
}
