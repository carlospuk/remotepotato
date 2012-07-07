using System;
using System.Collections.Generic;
using System.Text;
using DirectShowLib;
using DirectShowLib.SBE;
using WindowsMediaLib;

namespace FatAttitude.WTVTranscoder.FilterDefinitions
{    
    // Common / required filters and codecs
    public static class Audio  // Audio Filters
    {
        public static FilterDefinition AudioDecoderMSDTV = new FilterDefinition(@"@device:sw:{083863F1-70DE-11D0-BD40-00A0C911CE86}\{E1F1A0B8-BEEE-490D-BA7C-066C40B5E2B9}", "Microsoft DTV-DVD Audio Decoder");
        public static FilterDefinition AudioDecoderMPCHC = new FilterDefinition(new Guid("{3D446B6F-71DE-4437-BE15-8CE47174340F}"), "MPC-HC Audio Decoder");
        public static FilterDefinition AudioDecoderMPCHCMod = new FilterDefinition(new Guid("{E82D9138-BA00-43FD-BCF1-A69424DE5426}"), "MPC-HC Audio Decoder (babgvant)");
        public static FilterDefinition AudioDecoderFFDShow = new FilterDefinition(new Guid("{0F40E1E5-4F79-4988-B1A9-CC98794E6B55}"), "FFDSHOW Audio Decoder");
    }

    public static class Video // Video Filters
    {
        public static FilterDefinition VideoDecoderMSDTV = new FilterDefinition(@"@device:sw:{083863F1-70DE-11D0-BD40-00A0C911CE86}\{212690FB-83E5-4526-8FD7-74478B7939CD}", "Microsoft DTV-DVD Video Decoder");
        public static FilterDefinition VideoDecoderMpeg = new FilterDefinition(new Guid("{212690FB-83E5-4526-8FD7-74478B7939CD}"), "MS MPEG Decoder");
    }

    public static class Decrypt // // Decrypt
    {
        public static FilterDefinition DTFilterPBDA = new FilterDefinition(@"@device:sw:{4A56AF32-C21F-11DB-96FA-005056C00008}\PBDA DTFilter", "PBDA DTFilter");
        public static FilterDefinition DTFilterBDA = new FilterDefinition(new Guid("{C4C4C4F2-0049-4E2B-98FB-9537F6CE516D}"), "BDA DTFilter");
    }

    public static class Other
    {
        public static FilterDefinition ACMWrapperFilter = new FilterDefinition(new Guid("{6A08CF80-0E18-11CF-A24D-0020AFD79767}"), "ACM Wrapper");
    }

}