using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace CommonEPG
{
    public class RPMusicGenre : RPMusicCollection
    {
        public string Name { get; set; }

        // Constructors
        public RPMusicGenre()
        {
            Name = string.Empty;
        }
        public RPMusicGenre(string _name)
        {
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
