using System;
using System.IO;
using System.Xml.Serialization;
using System.Text;

namespace FatAttitude.WTVTranscoder
{
    public class WTVStreamingVideoResult
    {
        public DSStreamResultCodes ResultCode;
        public string ResultString;
        public string StreamerID { get; set; }
        public string Port { get; set; }

        public WTVStreamingVideoResult() { }
        public WTVStreamingVideoResult(DSStreamResultCodes resultCode, string resultString)
            : this()
        {
            ResultCode = resultCode;
            ResultString = resultString;
            StreamerID = "000000";
            Port = "9081";
        }
        public WTVStreamingVideoResult(DSStreamResultCodes resultCode)
            : this(resultCode, "") { }


    }
}
