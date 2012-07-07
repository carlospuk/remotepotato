using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace CommonEPG
{
    public class RPMusicPlaylist : RPMusicCollection
    {

        public string Title { get; set; }

        public RPMusicPlaylist()
        {
            Title = string.Empty;
        }



    }
}
