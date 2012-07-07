using System;
using System.IO;
using System.Xml.Serialization;
using System.Text;
using System.ComponentModel;

namespace FatAttitude.WTVTranscoder
{
    public class WTVStreamingVideoRequest
    {
        public static WTVStreamingVideoRequest FromXML(string theXML)
        {
            WTVStreamingVideoRequest newRR = new WTVStreamingVideoRequest();
            XmlSerializer serializer = new XmlSerializer(newRR.GetType());
            StringReader sr = new StringReader(theXML);
            try
            {
                return (WTVStreamingVideoRequest)serializer.Deserialize(sr);
            }
            catch
            {
                return newRR;
            }
        }

        // Class members
        public  WTVProfileQuality Quality;
        public string FileName;
        public int DeInterlaceMode;
        public int CustomFrameWidth;
        public int CustomFrameHeight;
        public int CustomVideoBitrate;
        public int CustomEncoderSmoothness;
        public double CustomEncoderFPS;

        [XmlIgnore]
        public  TimeSpan StartAt;
        /*[Obsolete]
        public double FrameSizeMultiplier; */

        public WTVStreamingVideoRequest() { }
        public WTVStreamingVideoRequest(string filename, WTVProfileQuality quality, TimeSpan startat)
        {
            FileName = filename;
            Quality = quality;
            StartAt = startat;
        }
        public WTVStreamingVideoRequest(string filename, WTVProfileQuality quality, int interlaceMode, TimeSpan startat) : this(filename, quality, startat)
        {
            DeInterlaceMode = interlaceMode;
        }

        // Pretend property for serialization
        [XmlElement("StartAtTicks")]
#if !SILVERLIGHT
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
#endif
        public long StartAtTicks
        {
            get { return StartAt.Ticks; }
            set { StartAt = TimeSpan.FromTicks(value); }
        }

    }


}
