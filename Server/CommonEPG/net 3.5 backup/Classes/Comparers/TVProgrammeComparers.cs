using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommonEPG.Comparers
{
    public class TVProgrammeStartTimeComparer : IComparer<TVProgramme>
    {
        public int Compare(TVProgramme tvp1, TVProgramme tvp2)
        {
            if (tvp1.StartTime > tvp2.StartTime) return 1;
            if (tvp1.StartTime < tvp2.StartTime) return -1;
            return 0;

        }
    }

    public class TVProgrammeTitleComparer : IComparer<TVProgramme>
    {
        public int Compare(TVProgramme tvp1, TVProgramme tvp2)
        {
            return string.Compare(tvp1.Title, tvp2.Title);
        }
    }

    public class TVProgrammeStartTimeComparerDescending : IComparer<TVProgramme>
    {
        public int Compare(TVProgramme tvp1, TVProgramme tvp2)
        {
            if (tvp1.StartTime < tvp2.StartTime) return 1;
            if (tvp1.StartTime > tvp2.StartTime) return -1;
            return 0;

        }
    }

    public class TVProgrammeServiceComparer : IComparer<TVProgramme>
    {
        public int Compare(TVProgramme tvp1, TVProgramme tvp2)
        {
            long svcID1, svcID2;
            if (! long.TryParse(tvp1.ServiceID, out svcID1) || (! long.TryParse(tvp2.ServiceID, out svcID2)))
            {
                return 0;
            }

            if (svcID1 > svcID2) return 1;
            if (svcID1 < svcID2) return -1;
            return 0;

        }
    }
}
