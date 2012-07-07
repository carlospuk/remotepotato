using System;
using System.Collections.Generic;
using System.Text;

namespace CommonEPG
{
    public class RPMusicBlob
    {
        
        public List<RPMusicAlbum> Albums {get; set;}
        public List<RPMusicArtist> Artists {get; set;}
        public List<RPMusicGenre> Genres { get; set; }

        public RPMusicBlob()
        {
            Albums = new List<RPMusicAlbum>();
            Artists = new List<RPMusicArtist>();
            Genres = new List<RPMusicGenre>();
        }
        
    }
}
