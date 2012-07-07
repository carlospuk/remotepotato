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



        public override bool Equals(object obj)
        {
            if (!(obj is RPMusicGenre)) return false;

            RPMusicGenre objGenre = (RPMusicGenre)obj;
            return (this.GetHashCode() == objGenre.GetHashCode());
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

    }
}
