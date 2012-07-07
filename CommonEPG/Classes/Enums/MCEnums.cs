using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommonEPG
{

    public enum RPRequestTypes
    {
        Unknown,
        Manual,
        OneTime,
        Series,
        Keyword
    };

    public enum RPRecordingStates
    {
        None,
        Deleted,
        Initializing,
        Recorded,
        Recording,
        Scheduled
    };

    public enum RPRequestStates
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
