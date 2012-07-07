using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FatAttitude.MediaStreamer
{
    public class MediaStreamingResult
    {
        public bool Completed;
        public bool Success;
        public string ErrorText;
        public string LiveStreamingIndexPath; // legacy
        public MediaStreamingResultCodes ResultCode;
        public int StreamerID;
        public int FrameWidth;
        public int FrameHeight;

        public MediaStreamingResult()
        {
        }
        public MediaStreamingResult(MediaStreamingResultCodes _ResultCode, string _ErrorText)
        {
            ResultCode = _ResultCode;
            ErrorText = _ErrorText;
        }


    }

    public enum MediaStreamingResultCodes
    {
        NamedError,
        FileNotFound,
        OK
    }
}
