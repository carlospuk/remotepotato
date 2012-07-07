using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommonEPG
{
#if !SILVERLIGHT
    [Serializable]
#endif
    public class RPMusicSong
    {
        public string ID { get; set; }
        public string Title { get; set; }
        public string ArtistID { get; set; }
        public string FileExtension { get; set; }
        public int UserRating { get; set; }
        public long Duration { get; set; }
        public int TrackNumber { get; set; }
        public long FileSizeBytes { get; set; }
      //  public string FileName { get; set; }
        
        public RPMusicSong()
        {
            ID = string.Empty;
            Title = string.Empty;
       //     FileName = string.Empty;
            ArtistID = string.Empty;
            FileExtension = string.Empty;
            UserRating = 50;
            TrackNumber = 0;
        }

    }
}
