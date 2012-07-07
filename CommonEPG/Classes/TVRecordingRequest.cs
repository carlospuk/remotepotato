using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommonEPG
{
    public class TVRecordingRequest
    {
        public string RequestId { get; set; }
        public string OriginatorId { get; set; }
        public string RequestType { get; set; }
        public string ChannelCallsign { get; set; }
        public string Title { get; set; }
        public int KeepUntil { get; set; }
        public int Quality { get; set; }
        public int Priority { get; set; }

        // Create a blank recording event
        public TVRecordingRequest()
        {}


    }
}
