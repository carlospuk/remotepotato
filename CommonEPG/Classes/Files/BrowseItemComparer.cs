using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CommonEPG.Comparers
{

    public class BrowseItemComparer : IComparer<BrowseItem>
    {
        public int Compare(BrowseItem bi1, BrowseItem bi2)
        {
            //return String.Compare(bi1.Name, bi2.Name);

            string p1 = (Path.GetFileName(bi1.Name)) ;
            string p2 = (Path.GetFileName(bi2.Name));

            return string.Compare(p1, p2);
        }
    }


}
