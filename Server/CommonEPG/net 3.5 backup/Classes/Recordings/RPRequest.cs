using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommonEPG
{
    public class RPRequest
    {
        public long ID { get; set; }  // The same as MCRequest's ID
        public RPRequestTypes RequestType { get; set; }
        public string Title { get; set; }
        public long ServiceID { get; set; } // convenience helper (also present in MCData)
        public int Priority { get; set; }
        public long SeriesID { get; set; }

        // Constructor
        public RPRequest() { }

    }
}
