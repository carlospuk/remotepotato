using System;
using System.IO;
using System.Xml.Serialization;
using System.Text;

namespace FatAttitude.WTVTranscoder
{
    public class WTVInitResult
    {
        public readonly WTVResultCodes ResultCode;
        public readonly string ResultString;

        public WTVInitResult() { }
        public WTVInitResult(WTVResultCodes resultCode, string resultString)
            : this()
        {
            ResultCode = resultCode;
            ResultString = resultString;
        }
        public WTVInitResult(WTVResultCodes resultCode)
            : this(resultCode, "") { }

    }
}
