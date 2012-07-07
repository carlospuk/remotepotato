using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.IO.IsolatedStorage;
using CommonEPG;
using CommonEPG.Comparers;
using System.Text;

namespace SilverPotato
{



    public class ScheduleSliceInfo
    {
        public DateTime TheLocalDate { get; set; }
        public List<string> TVServiceIDs { get; set; }

        public ScheduleSliceInfo()
        {
            TheLocalDate = DateTime.Now.Date;
            TVServiceIDs = new List<string>();
        }

        public ScheduleSliceInfo(DateTime _date, ChannelFilterTypes _channelFilterType)
        {
            TheLocalDate = _date;
            TVServiceIDs = new List<string>();
        }

        public ScheduleSliceInfo(DateTime _date, List<string> _tvServiceIDs)
        {
            TheLocalDate = _date;
            TVServiceIDs = _tvServiceIDs;
        }


        public string CacheUniqueFilename
        {
            get
            {
                StringBuilder sbCacheFN = new StringBuilder(11);
                sbCacheFN.Append(TheLocalDate.ToString("yyyy-MM-dd") + "#");

                if (TVServiceIDs.Count > 0)
                    sbCacheFN.Append(TVServiceIDs[0]);

                sbCacheFN.Append(".slice");

                return sbCacheFN.ToString();
            }
        }
        public override bool Equals(object obj)
        {
            // If parameter is null return false.
            if (obj == null)
            {
                return false;
            }

            // If parameter cannot be cast to ScheduleDay return false.
            ScheduleSliceInfo s = obj as ScheduleSliceInfo;
            if ((System.Object)s == null)
            {
                return false;
            }

            // Return true if all the IDs match
            foreach (string sID in s.TVServiceIDs)
            {
                if (!TVServiceIDs.Contains(sID))
                    return false;
            }

            return true;
        }

        public bool Equals(ScheduleSliceInfo s)
        {
            // If parameter is null return false:
            if ((object)s == null)
            {
                return false;
            }

            // Return true if all the IDs match
            foreach (string sID in s.TVServiceIDs)
            {
                if (!TVServiceIDs.Contains(sID))
                    return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            int hCode = TheLocalDate.Day ^ TheLocalDate.Month ^ TheLocalDate.Year;
            return hCode.GetHashCode();
        }

    }


    public class ScheduleSliceIdentical : EqualityComparer<ScheduleSliceInfo>
    {
        public override bool Equals(ScheduleSliceInfo sd1, ScheduleSliceInfo sd2)
        {
            return
                (sd1.Equals(sd2));  // see overriden equals operator
        }


        public override int GetHashCode(ScheduleSliceInfo sd1)
        {
            int hCode = sd1.TheLocalDate.Day ^ sd1.TheLocalDate.Month ^ sd1.TheLocalDate.Year;
            return hCode.GetHashCode();
        }

    }

}
