using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommonEPG
{
    public class TVRecordingEvent
    {
        public string ScheduleEventId { get; set; }
        public string ScheduleRequestId { get; set; }
        public string ScheduleRequestType { get; set; }
        public RecordingEventStates State { get; set; }
        public long StartTime { get; set; }
        public long StopTime { get; set; }
        public string ChannelCallsign { get; set; }
        public string Title { get; set; }
        public int KeepUntil { get; set; }
        public int Quality { get; set; }
        public bool Partial { get; set; }
        public bool Repeat { get; set; }
        public string ProviderCopyright { get; set; }
        public long OriginalAirDate { get; set; }
        public string Genre { get; set; }
        public string FileName { get; set; }
        public string Description { get; set; } // always initially blank
        public bool HasWatched { get; set; }
        //public TVProgramme LinkedTVProgramme { get; set; }

        // Create a blank recording event
        public TVRecordingEvent()
        {
            Description = ""; // always initially blank
        }


        
    }

    public enum RecordingEventStates
    {
        All,
        Alternate,
        Canceled,
        Conflict,
        Deleted,
        Error,
        HasOccurred,
        IsOccurring,
        None,
        WillOccur   
    };
}
