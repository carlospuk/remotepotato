using System;
using System.Collections.Generic;
using System.Text;

namespace CommonEPG
{
    public class RPRecording
    {
        public long Id { get; set; } // MC internal ID
        public long RPRequestID {get; set; }  // link to RPRequest
        public long TVProgrammeID {get; set; }  // Link to TVProgramme  (actually a scheduleEntry)
        public RPRecordingStates State { get; set; }
        public long SeriesID { get; set; } // Track any series this is part of

        // helpers for convenience
        public RPRequestTypes RequestType { get; set; }
        public string Title { get; set; }

        public int KeepUntil { get; set; }
        public int Quality { get; set; }
        public bool Partial { get; set; }

        // Manual Recordings
        public DateTime ManualRecordingStartTime { get; set; }
        public double ManualRecordingDuration { get; set; }
        public long ManualRecordingServiceID { get; set; }

        // Create a blank recording event
        public RPRecording()
        {
            
        }

    }

}
