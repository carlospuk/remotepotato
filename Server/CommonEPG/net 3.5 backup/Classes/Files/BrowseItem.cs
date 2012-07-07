using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommonEPG
{
    public class BrowseItem
    {
        public bool IsDirectory { get; set; }
        public string Name { get; set; }
        public double Size { get; set; }

        public BrowseItem()
        {
            IsDirectory = false;
            Name = @"";
        }
        


    }
}
