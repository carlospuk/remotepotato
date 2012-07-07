using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FatAttitude.MediaStreamer
{
    public class VideoEncodingParameters
    {
        public string Description { get; set; } // Optional
        public string CustomFFMpegTemplate { get; set; }
        public int FrameWidth { get; set; }
        public int FrameHeight { get; set; }
        public string AspectRatio { get; set; }
        public string BitRateDeviation { get; set; }
        public AudioCodecTypes AudioCodec { get; set; }
        public string AudioBitRate { get; set; }
        public string AudioSampleRate { get; set; }
        public int AudioVolumePercent { get; set; }
        public int X264SubQ { get; set; }
        public int X264Level { get; set; } // If zero, this is ignored.  Can be set, e.g. to 30 to constrain for iPhone 3G
        public string X264Profile { get; set; } // If blank/null, this is ignored.  Can be set, e.g. to "baseline" to constrain for iPhone 3G
        public string VideoBitRate { get; set; }
        public int MotionSearchRange { get; set; } // between 4 and 16
        public bool Partition_I4x4 { get; set; }
        public bool Partition_I8x8 { get; set; }
        public bool Partition_P8x8 { get; set; }
        public int SegmentDuration { get; set; }
        public bool OutputSquarePixels { get; set; }
        public bool DeInterlace { get; set; }

        public VideoEncodingParameters()
        {
            // Defaults
            Description = "Unknown";
            FrameWidth = 320;
            FrameHeight = 240;
            AspectRatio = null;  // Important not to have a default, as if null, file is probed at time of streaming to find the actual aspect ratio
            VideoBitRate = "128k";
            BitRateDeviation = "120k";
            X264SubQ = 5;
            X264Level = 0;
            X264Profile = "";

            AudioCodec = AudioCodecTypes.MP3;
            AudioBitRate = "64k";
            AudioSampleRate = "48000"; 
            MotionSearchRange = 14;

            SegmentDuration = 4;

            Partition_I4x4 = true;
            Partition_I8x8 = true;
            Partition_P8x8 = false; // Air Video has +partp8x8 on partitions

            DeInterlace = false;
            CustomFFMpegTemplate = "";
            AudioVolumePercent = 100;
        }

        // Derived
        public string ConstrainedSize
        {
            get
            {
                double AR = AspectRatioAsDouble;

                double newFrameWidth;
                double newFrameHeight;

                
                // Fit within frame, respecting the display aspect ratio
                if (AR >= 1)
                {
                    newFrameWidth = FrameWidth;
                    newFrameHeight = FrameWidth * (1 / AR);
                }
                else
                {
                    newFrameHeight = FrameHeight;
                    newFrameWidth = FrameHeight * AR;
                }


                // Convert to integer and ensure an even number
                int iNewFrameWidth = Convert.ToInt32(newFrameWidth);
                int iNewFrameHeight = Convert.ToInt32( newFrameHeight );

                if (IsOdd(iNewFrameWidth)) iNewFrameWidth++;
                if (IsOdd(iNewFrameHeight)) iNewFrameHeight++;

                return string.Format("{0}x{1}", iNewFrameWidth, iNewFrameHeight);

            }
        }
        static bool IsOdd(int intValue)
        {
            return ((intValue & 1) == 1);
        }
        double AspectRatioAsDouble
        {
            get
            {
                double output = 1;
                double num = 0;
                double den = 0;

                string[] arParts = AspectRatio.Split(':');
                if (arParts.Count() < 2) return output;
                if (!double.TryParse(arParts[0], out num)) return output;
                if (!double.TryParse(arParts[1], out den)) return output;

                return (num / den);
            }
        }
        public string FrameSize
        {
            get
            {
                return FrameWidth.ToString() + "x" + FrameHeight.ToString();
            }
        }
        public string PartitionsFlags
        {
            get
            {
                string sParts = "";
                if (Partition_I4x4) sParts += "+parti4x4";
                if (Partition_I8x8) sParts += "+parti8x8";
                if (Partition_P8x8) sParts += "+partp8x8";

                return sParts;
            }
        }
        public override string ToString()
        {
            return string.Format("{0} ({1}x{2} @{3}bps)",
                Description,
                FrameWidth.ToString(),
                FrameHeight.ToString(),
                VideoBitRate );
        }


        public enum AudioCodecTypes { MP3, AAC };
    }

    

}
