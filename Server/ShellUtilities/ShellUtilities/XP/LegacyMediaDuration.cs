using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
namespace FatAttitude
{
public class LegacyMediaDuration
{
        [DllImport("winmm.dll")]
        static extern Int32 mciSendString(string command, StringBuilder buffer, int bufferSize, IntPtr hwndCallback);

        [DllImport("winmm.dll")]
        private static extern int mciGetErrorString(int l1, StringBuilder s1, int l2);

        public TimeSpan GetMediaDuration(string file)
        {
            string strLength = MCIFindLength(file);
            if (!(string.IsNullOrWhiteSpace(strLength)))
            {

                try
                {
                    int iTime = Convert.ToInt32(strLength);
                    TimeSpan ts = TimeSpan.FromMilliseconds(iTime);

                    return ts;
                }
                catch
                {
                    // Not a number; try processing it as a HH:MM:SS string

                    TimeSpan ts = new TimeSpan();
                    if (TimeSpan.TryParse(strLength, out ts))
                        return ts;
                }
            }

            return TimeSpan.FromSeconds(0);
        }

        private string MCIFindLength(string file)
        {
            string strShortFile = FatAttitude.GetShortFilenames.GetShortFileName(file);

            string cmd = "open " + strShortFile + " alias voice1";
            StringBuilder mssg = new StringBuilder(255);
            int h = mciSendString(cmd, null, 0, IntPtr.Zero);
            int i = mciSendString("set voice1 time format ms", null, 0, IntPtr.Zero);
            int j = mciSendString("status voice1 length", mssg, mssg.Capacity, IntPtr.Zero);

            return mssg.ToString();
        }

}

}
