using System;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Cellbi;

namespace SilverPotato
{
    public static class ZipManager
    {

        static object UnzipOneStringAtATimeLock = new object();

        public static bool UnzipString(ref string theString)
        {
           // if (String.IsNullOrEmpty(theString)) return true;

            // theString is a base64 string encoded string
            // which decodes to a byte[] array 
            // which unzips to a UTF-8 encoded string which deserialises

            try
            {
                lock (UnzipOneStringAtATimeLock)
                {

                    byte[] input = Convert.FromBase64String(theString);
                    byte[] output = Cellbi.SvZLib.Utils.Decompress(input);
                    theString = Encoding.UTF8.GetString(output, 0, output.Length);

                    return true;

                }
            }
            catch (Exception e)
            {
                Functions.WriteExceptionToLogFile(e);
                return false;
            }
        }
    }
}
