using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace CommonEPG
{
    public class RecordingRequest
    {
        public static RecordingRequest FromXML(string theXML)
        {
            RecordingRequest newRR = new RecordingRequest();
            XmlSerializer serializer = new XmlSerializer(newRR.GetType());
            StringReader sr = new StringReader(theXML);
            try
            {
                return (RecordingRequest)serializer.Deserialize(sr);
            }
            catch
            {
                return newRR;
            }
        }

        #region Constructors
        public RecordingRequest() 
        {
            RequestCreationDate = DateTime.Now;
            FirstRunOnly = false;
            KeepUntil = KeepUntilTypes.UntilUserDeletes;
        }
        public RecordingRequest(RecordingRequestType requestType) : this()
        {
            RequestType = requestType;   
        }
        /// <summary>
        /// Create a manual recording request
        /// </summary>
        /// <param name="Title">The prototype title</param>
        /// <param name="startTime"></param>
        /// <param name="channelID"></param>
        /// <param name="duration"></param>
        public RecordingRequest(DateTime startTime, long serviceID, int duration, string protoTitle) : this()
        {
            RequestType = RecordingRequestType.Manual;
            this.ManualRecordingName = protoTitle;
            this.StartTime = startTime;
            this.ServiceID = serviceID;
            this.Duration = duration;
        }

        /// <summary>
        /// Create a one-time request
        /// </summary>
        /// <param name="tvProgrammeID"></param>
        public RecordingRequest(long tvProgrammeID) : this()
        {
            RequestType = RecordingRequestType.OneTime;
            TVProgrammeID = tvProgrammeID;
        }

        /// <summary>
        /// Create a series request
        /// </summary>
        /// <param name="tvProgrammeID"></param>
        /// <param name="channelID">The ID of the service</param>
        /// <param name="seriesRequestSubType">All channels, one channel, etc.</param>
        public RecordingRequest(long tvProgrammeID, SeriesRequestSubTypes seriesRequestSubType ) : this()
        {
            RequestType = RecordingRequestType.Series;
            TVProgrammeID = tvProgrammeID;
            SeriesRequestSubType = seriesRequestSubType;
        }
        #endregion

        #region Public Properties
        public DateTime RequestCreationDate;
        public RecordingRequestType RequestType {get; set;}
        public SeriesRequestSubTypes SeriesRequestSubType { get; set; }
        public bool FirstRunOnly {get; set;}
        public int KeepNumberOfEpisodes { get; set; }
        //public DefaultContentQualityPreferences DefaultContentQualityPreference { get; set; }


        // OneTime / Series
        public long TVProgrammeID { get; set; }

        // Manual
        public DateTime StartTime { get; set; }
        public long ServiceID { get; set; }
        /// <summary>
        /// The duration, for manual recordings only
        /// </summary>
        public int Duration { get; set; }
        public string ManualRecordingName { get; set; }

        // All
        public long MCChannelID {get; set;}

        // Extra settings
        public KeepUntilTypes KeepUntil {get; set;}
        public int Quality {get; set;}       
        public int Prepadding {get; set;}
        public int Postpadding {get; set;}

        #endregion

        // Helper Props
        public string RequestTypeAsString
        {
            get
            {
                switch (RequestType)
                {
                    case RecordingRequestType.Manual:
                        return "Manual recording";

                    case RecordingRequestType.OneTime:
                        return "One-time show recording.";

                    case RecordingRequestType.Series:
                        return "Series recording.";

                    default:
                        return "Unknown recording type.";
                }
            }
        }

        
    }


    // Enums
    public enum RecordingRequestType
    {
        Unknown = 0,
        Manual = 1,
        OneTime = 2,
        Series = 3
    };
    public enum SeriesRequestSubTypes
    {
        ThisChannelThisTime,
        ThisChannelAnyTime,
        AnyChannelAnyTime
    };
    public enum KeepUntilTypes
    {
        NotSet,
        UntilUserWatched,
        UntilUserDeletes,
        UntilEligible,
        OneWeek,
        LatestEpisodes
    }
    /*
    public enum DefaultContentQualityPreferences
    {
        NotSet,
        Any,
        OnlyHD,
        OnlySD,
        PreferHD,
        PreferSD,
        SmartDefault
    }
    */
}
