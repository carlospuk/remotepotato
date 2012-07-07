using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FatAttitude.WTVTranscoder
{
    public enum DSStreamResultCodes
    {
        Error,
        ErrorExceptionOccurred,
        ErrorAlreadyStreaming,
        ErrorFileNotFound,
        ErrorInvalidFileType,
        ErrorCodecNotFound,
        ErrorAC3CodecNotFound,
        ErrorInStreamRequest,
        ErrorTooManyStreamers,
        OK
    }
}
