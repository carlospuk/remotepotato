using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace CommonEPG
{
    public class RPMusicArtist : RPMusicCollection
    {

        public string Name { get; set; }

        public RPMusicArtist()
        {
            // ID = string.Empty;  ID is currently the artist name
            Name = string.Empty;
        }
        public RPMusicArtist(string _name)
        {
            // ID = string.Empty;  ID is currently the artist name
            Name = _name;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is RPMusicArtist)) return false;

            RPMusicArtist objArtist = (RPMusicArtist)obj;
            return (this.GetHashCode() == objArtist.GetHashCode());
        }

        public override int GetHashCode()
        {
            int hc = 0;
            if (this.Name != null)
                hc += this.Name.GetHashCode();

            if (hc == 0)
                return base.GetHashCode();
            else
                return hc;
        }

        [XmlIgnore]
        public string ID
        {
            get
            {
                return Name;
            }
        }

    }
}
