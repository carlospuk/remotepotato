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
