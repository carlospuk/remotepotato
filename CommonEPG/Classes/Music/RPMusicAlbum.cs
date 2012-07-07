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

        public RPMusicAlbum()
        {
            Title = string.Empty;
            ID = string.Empty;
            ArtistID = string.Empty;
            GenreID = string.Empty;
        }


        public override bool Equals(object obj)
        {
            if (!(obj is RPMusicAlbum)) return false;

            RPMusicAlbum objAlbum = (RPMusicAlbum)obj;

            return (this.GetHashCode() == objAlbum.GetHashCode() ) ;
        }
        public override int GetHashCode()
        {
            int hc = 0;
            if (this.ID != null)
                hc += this.ID.GetHashCode();

            if (hc == 0)
                return base.GetHashCode();
            else
                return hc;
        }

    }
}
