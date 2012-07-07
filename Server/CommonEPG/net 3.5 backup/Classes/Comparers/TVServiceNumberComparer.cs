using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommonEPG.Comparers
{
    public class TVCServiceNumComparer : IComparer<TVService>
    {
        public int Compare(TVService tvc1, TVService tvc2)
        {
            // Zero channel number comes after a channel number
            if ((tvc1.MCChannelNumber == 0) & (tvc2.MCChannelNumber != 0)) return 1;
            if ((tvc1.MCChannelNumber != 0) & (tvc2.MCChannelNumber == 0)) return -1;

            double bigNum1 = (tvc1.MCChannelNumber * 1000) + tvc1.MCSubChannelNumber;
            double bigNum2 = (tvc2.MCChannelNumber * 1000) + tvc2.MCSubChannelNumber;

            if (bigNum1 > bigNum2) return 1;
            if (bigNum1 < bigNum2) return -1;

            // Equal, compare by callsign now.
            return String.Compare(tvc1.Callsign, tvc2.Callsign);
        }
    }

    public class TVCServiceCallsignComparer : IComparer<TVService>
    {
        public int Compare(TVService tvc1, TVService tvc2)
        {
            // Zero channel number comes after a channel number
            return string.Compare(tvc1.Callsign, tvc2.Callsign);
        }
    }

}
