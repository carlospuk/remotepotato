using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Text;

namespace FatAttitude.MediaStreamer
{
    public class AVStream
    {
        public int StreamIndex;
        public string CodecTag;
        public string CodecName;
        public int Channels;
        public AVCodecType CodecType;
        public AudioStreamTypes AudioCodecSubType;
        public int Width;
        public int Height;
        public double DurationSeconds;
        public string SampleAspectRatio;
        public string DisplayAspectRatio;
        public string Language;

        public AVStream()
        {
            CodecType = AVCodecType.Unset;
            AudioCodecSubType = AudioStreamTypes.Unset;
        }

        // Calculated property
        public TimeSpan Duration
        {
            get
            {
                return TimeSpan.FromSeconds(DurationSeconds);
            }
        }

        public override string ToString()
        {
            StringBuilder sbOutput = new StringBuilder(20);
            //sbOutput.Append( StreamIndex.ToString() + ": ");
            sbOutput.Append(CodecName.ToUpper() + ", ");

            if (CodecType == AVCodecType.Audio)
            {
                if (Channels > 2)
                    sbOutput.Append(Channels.ToString() + " channels");
                else
                    sbOutput.Append(Channels > 1 ? "Stereo" : "Mono");

                if (!string.IsNullOrEmpty(Language))
                    sbOutput.Append(", " + Language.ToUpper());
                else
                    sbOutput.Append(" ");

                if (AudioCodecSubType == AudioStreamTypes.Commentary)
                    sbOutput.Append(" (commentary)");

            }



            if (CodecType == AVCodecType.Video)
            {
                sbOutput.Append(string.Format("{0}x{1}", Width.ToString(), Height.ToString() ));
            }


            return sbOutput.ToString();
        }



        /*
         * [STREAM]
index=2
codec_name=mp2
codec_long_name=MP2 (MPEG audio layer 2)
codec_type=audio
codec_time_base=0/1
codec_tag_string=P[0][0][0]
codec_tag=0x0050
sample_rate=48000.000000
channels=1
bits_per_sample=0
r_frame_rate=0/0
avg_frame_rate=125/3
time_base=1/10000000
start_time=1.364370
duration=4.191059
TAG:language=eng
[/STREAM]*/

    }

    public enum AVCodecType
    {
        Unset,
        Unknown,
        Audio,
        Video,
        Subtitle
    }
    public enum AudioStreamTypes
    {
        Unset,
        Commentary
    }
}
