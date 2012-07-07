using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommonEPG.Comparers
{

    public class RPRequestTitleComparer : IComparer<RPRequest>
    {
        public int Compare(RPRequest req1, RPRequest req2)
        {
            return String.Compare(req1.Title, req2.Title);
        }
    }
    public class RPRequestPriorityComparer : IComparer<RPRequest>
    {
        public int Compare(RPRequest req1, RPRequest req2)
        {
            if (req1.Priority > req2.Priority) return 1;
            if (req1.Priority < req2.Priority) return -1;
            return 0;
        }
    }

}
