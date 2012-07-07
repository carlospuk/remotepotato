using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FatAttitude.MediaStreamer
{
    public class MediaStreamingRequest
    {
        public StreamingTypes StreamingType { get; set; }
        public string InputFile { get; set; }
        public int Quality { get; set; }
        public int StartAt { get; set; }
        public int UseAudioStreamIndex { get; set; }
        public bool UseCustomParameters { get; set; }
        public string AspectRatio { get; set; } // For older iOS clients
        public string ClientID { get; set; }
        public string ClientDevice { get; set; }
        public VideoEncodingParameters CustomParameters { get; set; }
        

        public MediaStreamingRequest()
        {
            UseAudioStreamIndex = -1;
            ClientID = "";
            ClientDevice = "";
        }

        // Derived Property - depends on whether there are Custom Parameters.  
        const int DEFAULT_SEGMENT_DURATION = 4;  // **DEFAULT SEGMENT DURATION FOR IOS OR ANY CLIENTS NOT USING CUSTOM PARAMS **
        public int ActualSegmentDuration
        {
            get
            {
                if (!UseCustomParameters) return DEFAULT_SEGMENT_DURATION;
                return CustomParameters.SegmentDuration;
            }
        }   

        public static MediaStreamingRequest RequestWithDesktopProfileLevel(int desktopProfileLevel)
        {
            MediaStreamingRequest newRequest = new MediaStreamingRequest();
            
            newRequest.UseCustomParameters = true;
            newRequest.CustomParameters = videoEncodingParametersFromDesktopLevel(desktopProfileLevel); 

            return newRequest;
        }

        const int NUMBER_OF_DESKTOP_PROFILE_LEVELS = 5;
        public static VideoEncodingParameters[] desktopVideoEncodingProfiles
        {
            get
            {
                VideoEncodingParameters[] desktopParams = new VideoEncodingParameters[NUMBER_OF_DESKTOP_PROFILE_LEVELS];
                for (int i = 0; i < NUMBER_OF_DESKTOP_PROFILE_LEVELS; i++)
                {
                    desktopParams[i] = MediaStreamingRequest.videoEncodingParametersFromDesktopLevel(i);
                }

                return desktopParams;
            }
        }

        static VideoEncodingParameters videoEncodingParametersFromDesktopLevel(int desktopProfileLevel)
        {
            VideoEncodingParameters vParams = new VideoEncodingParameters();

            // By default, Silverlight allows square pixels  (ios is more picky)
            vParams.OutputSquarePixels = true;

            /*                  COMMENTS ON PARAMETERS
             
             X264SubQ:   6 takes twice the time of 5
             
             */
            if (desktopProfileLevel == 0)
            {
                vParams.Description = "Low";
                vParams.FrameWidth = 256;
                vParams.FrameHeight = 192;
                vParams.VideoBitRate = "360k";
                vParams.BitRateDeviation = "60k";
                vParams.X264SubQ = 6;
                vParams.AudioBitRate = "32k";
                vParams.AudioSampleRate = "48000";
                vParams.MotionSearchRange = 12;

                vParams.Partition_I4x4 = true;
                vParams.Partition_I8x8 = true;
                vParams.Partition_P8x8 = true;
            }
            else if (desktopProfileLevel == 1)
            {
                vParams.Description = "Normal";
                vParams.FrameWidth = 320;
                vParams.FrameHeight = 240;
                vParams.VideoBitRate = "600k";
                vParams.BitRateDeviation = "80k";
                vParams.X264SubQ = 6;
                vParams.AudioBitRate = "64k";
                vParams.AudioSampleRate = "48000";
                vParams.MotionSearchRange = 14;

                vParams.Partition_I4x4 = true;
                vParams.Partition_I8x8 = true;
                vParams.Partition_P8x8 = true;
            }
            else if (desktopProfileLevel == 2)
            {
                vParams.Description = "High";
                vParams.FrameWidth = 512;
                vParams.FrameHeight = 384;
                vParams.VideoBitRate = "1200k";
                vParams.BitRateDeviation = "100k";
                vParams.X264SubQ = 6;
                vParams.AudioBitRate = "64k";
                vParams.AudioSampleRate = "48000";
                vParams.MotionSearchRange = 16;

                vParams.Partition_I4x4 = true;
                vParams.Partition_I8x8 = true;
                vParams.Partition_P8x8 = true;
            }
            else if (desktopProfileLevel == 3)
            {
                vParams.Description = "Higher";
                vParams.FrameWidth = 576;
                vParams.FrameHeight = 432;
                vParams.VideoBitRate = "1400k";
                vParams.BitRateDeviation = "100k";
                vParams.X264SubQ = 6;
                vParams.AudioBitRate = "64k";
                vParams.AudioSampleRate = "48000";
                vParams.MotionSearchRange = 16;

                vParams.Partition_I4x4 = true;
                vParams.Partition_I8x8 = true;
                vParams.Partition_P8x8 = true;
            }
            else if (desktopProfileLevel == 4)
            {
                vParams.Description = "Very High";
                vParams.FrameWidth = 640;
                vParams.FrameHeight = 480;
                vParams.VideoBitRate = "1800k";
                vParams.BitRateDeviation = "100k";
                vParams.X264SubQ = 6;
                vParams.AudioBitRate = "64k";
                vParams.AudioSampleRate = "48000";
                vParams.MotionSearchRange = 16;

                vParams.Partition_I4x4 = true;
                vParams.Partition_I8x8 = true;
                vParams.Partition_P8x8 = true;
            }
            else 
            {
                vParams.Description = "Highest";
                vParams.FrameWidth = 800;
                vParams.FrameHeight = 600;
                vParams.VideoBitRate = "2400k";
                vParams.BitRateDeviation = "120k";
                vParams.X264SubQ = 8;
                vParams.AudioBitRate = "96k";
                vParams.AudioSampleRate = "48000";
                vParams.MotionSearchRange = 16;

                vParams.Partition_I4x4 = true;
                vParams.Partition_I8x8 = true;
                vParams.Partition_P8x8 = true;
            }
            return vParams;
        }
        
        public static void AddVideoEncodingParametersUsingiOSQuality(ref MediaStreamingRequest rq)
        {
            CreateOrUpdateVideoEncodingParametersFromiOSQuality(ref rq);

            // Override aspect ratio -> insert into custom parameters 
            rq.CustomParameters.AspectRatio = rq.AspectRatio;
        }
        private static void CreateOrUpdateVideoEncodingParametersFromiOSQuality(ref MediaStreamingRequest rq)
        {
            rq.UseCustomParameters = true;
            int iOSQualityIndex = rq.Quality;

            // Work on the existing Customparameters object, if there is one, else create a new one
            if (rq.CustomParameters == null)
                rq.CustomParameters = new VideoEncodingParameters();
            VideoEncodingParameters vParams = rq.CustomParameters;

            switch (iOSQualityIndex)
            {
                    
#if ACCESSIBILITY_AUDIO
                    /*  ACCESSIBILITY VERSION :
                case 0:
                    vParams.VideoBitRate = "96k";
                    vParams.BitRateDeviation = "64k";
                    vParams.FrameWidth = 192;
                    vParams.FrameHeight = 128;
                    vParams.X264SubQ = 2;
                    vParams.AudioBitRate = "96k";
                    vParams.AudioSampleRate = "48000";
                    vParams.MotionSearchRange = 8;
                    break;

                case 1:
                    vParams.VideoBitRate = "96k";
                    vParams.BitRateDeviation = "64k";
                    vParams.FrameWidth = 192;
                    vParams.FrameHeight = 128;
                    vParams.X264SubQ = 2;
                    vParams.AudioBitRate = "96k";
                    vParams.AudioSampleRate = "48000";
                    vParams.MotionSearchRange = 8;
                    break;

                case 2:
                    vParams.VideoBitRate = "96k";
                    vParams.BitRateDeviation = "64k";
                    vParams.FrameWidth = 192;
                    vParams.FrameHeight = 128;
                    vParams.X264SubQ = 2;
                    vParams.AudioBitRate = "128k";
                    vParams.AudioSampleRate = "48000";
                    vParams.MotionSearchRange = 8;
                    break;

                case 3:
                    vParams.VideoBitRate = "164k";
                    vParams.BitRateDeviation = "64k";
                    vParams.FrameWidth = 192;
                    vParams.FrameHeight = 128;
                    vParams.X264SubQ = 2;
                    vParams.AudioBitRate = "148k";
                    vParams.AudioSampleRate = "48000";
                    vParams.MotionSearchRange = 8;
                    break;

                case 4:
                    vParams.VideoBitRate = "256k";
                    vParams.BitRateDeviation = "64k";
                    vParams.FrameWidth = 192;
                    vParams.FrameHeight = 128;
                    vParams.X264SubQ = 2;
                    vParams.AudioBitRate = "256k";
                    vParams.AudioSampleRate = "48000";
                    vParams.MotionSearchRange = 8;
                    break;

                case 5:  // ipad lo
                    vParams.VideoBitRate = "256k";
                    vParams.BitRateDeviation = "64k";
                    vParams.FrameWidth = 192;
                    vParams.FrameHeight = 128;
                    vParams.X264SubQ = 2;
                    vParams.AudioBitRate = "256k";
                    vParams.AudioSampleRate = "48000";
                    vParams.MotionSearchRange = 8;
                    break;


                case 6:  // ipad hi
                    vParams.VideoBitRate = "1400k";
                    vParams.BitRateDeviation = "120k";
                    vParams.FrameWidth = 1024;
                    vParams.FrameHeight = 768;
                    vParams.X264SubQ = 6;
                    vParams.AudioBitRate = "64k";
                    vParams.AudioSampleRate = "48000";
                    vParams.MotionSearchRange = 16;
                    break;
#else
                case 0:
                    vParams.VideoBitRate = "48k";
                    vParams.BitRateDeviation = "24k";
                    vParams.FrameWidth = 192;
                    vParams.FrameHeight = 128;
                    vParams.X264SubQ = 2;
                    vParams.AudioBitRate = "48k";
                    vParams.AudioSampleRate = "48000";
                    vParams.MotionSearchRange = 8;
                    break;

                case 1:
                    vParams.VideoBitRate = "56k";
                    vParams.BitRateDeviation = "32k";
                    vParams.FrameWidth = 192;
                    vParams.FrameHeight = 128;
                    vParams.X264SubQ = 2;
                    vParams.AudioBitRate = "48k";
                    vParams.AudioSampleRate = "48000";
                    vParams.MotionSearchRange = 8;
                    break;

                case 2:
                    vParams.VideoBitRate = "96k";
                    vParams.BitRateDeviation = "64k";
                    vParams.FrameWidth = 240;
                    vParams.FrameHeight = 160;
                    vParams.X264SubQ = 4;
                    vParams.AudioBitRate = "64k";
                    vParams.AudioSampleRate = "48000";
                    vParams.MotionSearchRange = 12;
                    break;

                case 3:
                    vParams.VideoBitRate = "192k";
                    vParams.BitRateDeviation = "120k";
                    vParams.FrameWidth = 240;
                    vParams.FrameHeight = 160;
                    vParams.X264SubQ = 5;
                    vParams.AudioBitRate = "64k";
                    vParams.AudioSampleRate = "48000";
                    vParams.MotionSearchRange = 16;
                    break;

                case 4:
                    vParams.VideoBitRate = "320k";
                    vParams.BitRateDeviation = "120k";
                    vParams.FrameWidth = 480;
                    vParams.FrameHeight = 320;
                    vParams.X264SubQ = 6;
                    vParams.AudioBitRate = "64k";
                    vParams.AudioSampleRate = "48000";
                    vParams.MotionSearchRange = 16;
                    break;

                case 5:  // ipad lo
                    vParams.VideoBitRate = "860k";
                    vParams.BitRateDeviation = "120k";
                    vParams.FrameWidth = 614;
                    vParams.FrameHeight = 460;
                    vParams.X264SubQ = 6;
                    vParams.AudioBitRate = "64k";
                    vParams.AudioSampleRate = "48000";
                    vParams.MotionSearchRange = 16;
                    break;


                case 6:  // ipad hi
                    vParams.VideoBitRate = "1400k";
                    vParams.BitRateDeviation = "120k";
                    vParams.FrameWidth = 1024;
                    vParams.FrameHeight = 768;
                    vParams.X264SubQ = 6;
                    vParams.AudioBitRate = "64k";
                    vParams.AudioSampleRate = "48000";
                    vParams.MotionSearchRange = 16;
                    break;

#endif
              
            }
        }


        // Enums
        public enum StreamingTypes
        {
            Unset,
            HttpLiveStreaming
        }


    }


}
