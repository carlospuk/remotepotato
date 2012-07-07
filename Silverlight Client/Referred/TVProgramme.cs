using System;
using System.Xml.Serialization;

namespace CommonEPG
{
    public class TVProgramme
    {
        // Constructors
        public TVProgramme()
        { }

        // Private Members
        public string Id { get; set; }  // scheduleentry ID
        public string MCProgramID { get; set; }  // media center Program ID  (a TVProgramme object more closely corresponds to a scheduleEntry)
        public string Title { get; set; }
        public string ServiceID { get; set; }
        public string EpisodeTitle { get; set; }
        public String Description { get; set; }
        public long StartTime { get; set; }
        public long StopTime { get; set; }
        public bool IsHD { get; set; }
        public bool IsFirstShowing { get; set; }
        public bool IsSeries { get; set; }
        public bool IsDRMProtected { get; set; }
        public TVProgrammeType ProgramType { get; set; }
        public int StarRating { get; set; }
        public bool HasSubtitles { get; set; }
        public string TVRating { get; set; }
        public string MPAARating { get; set; }
        public long OriginalAirDate { get; set; }
        public string Filename { get; set; }
        public long SeriesID { get; set; }
        public string GuideImageUri { get; set; }

        // Optional Members - filled on request
        public TVProgrammeCrew Crew { get; set; }   // TODO: Delete this soon if you never end up storing this info

        // Silverlight Store
        public event EventHandler Updated;
        [XmlIgnore]
        public bool IsLongTermTenant { get; set; }
        [XmlIgnore]
        public bool IsNotDTV { get; set; }

        public bool isGeneratedFromFile { get; set; }
        public string WTVCallsign {get; set;}

        // Methods
        public void FireUpdated()
        {
            if (Updated != null) Updated(this, new EventArgs());
        }
    }

    public enum TVProgrammeType
    {
        None,
        Sport,
        News,
        Movie,
        Documentary,
        Kids,
        All  // added for use in searches
    } 




}
