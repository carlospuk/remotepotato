using System;
using System.IO;
using System.Xml.Serialization;
using System.Text;
using System.ComponentModel;

namespace FatAttitude.WTVTranscoder
{
    public class WTVStreamingVideoCommand
    {
        public static WTVStreamingVideoCommand FromXML(string theXML)
        {
            WTVStreamingVideoCommand newRR = new WTVStreamingVideoCommand();
            XmlSerializer serializer = new XmlSerializer(newRR.GetType());
            StringReader sr = new StringReader(theXML);
            try
            {
                return (WTVStreamingVideoCommand)serializer.Deserialize(sr);
            }
            catch
            {
                return newRR;
            }
        }

        // Class members
        [XmlIgnore]
        public TimeSpan SeekTo { get; set; }
        public string CommandName { get; set; }

        public WTVStreamingVideoCommand() { }
        public WTVStreamingVideoCommand(string _commandName, TimeSpan _seekTo)
        {
            CommandName = _commandName;
            SeekTo = _seekTo;
        }

        // Pretend property for serialization
        [XmlElement("SeekToTicks")]
#if !SILVERLIGHT
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
#endif
        public long SeekToTicks
        {
            get { return SeekTo.Ticks; }
            set { SeekTo = TimeSpan.FromTicks(value); }
        }

    }


}
