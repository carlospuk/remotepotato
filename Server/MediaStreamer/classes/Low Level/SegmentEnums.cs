using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FatAttitude.MediaStreamer.HLS
{


    public enum SegmentAvailabilities
    {
        IsAvailable,
        IsError,
        RequiresSeek,
        Cancelled
    }
}
