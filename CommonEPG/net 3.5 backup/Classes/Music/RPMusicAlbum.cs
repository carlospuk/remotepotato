using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace CommonEPG
{
    public class RPMusicAlbum : RPMusicCollection
    {
        public string ID { get; set; }
        public string Title { get; set; }
        public string ArtistID { get; set; }
        public string GenreID { get; set; }
      //  public List<string> SongIDs { get; set; }  Call server instead for songs

        public RPMusicAlbum()
        {
            Title = string.Empty;
            ID = string.Empty;
            ArtistID = string.Empty;
            GenreID = string.Empty;
       //     SongIDs = new List<string>();
        }

    }
}
