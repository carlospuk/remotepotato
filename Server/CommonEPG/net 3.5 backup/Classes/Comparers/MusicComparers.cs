using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommonEPG.Comparers
{
    public class RPMusicArtistNameComparer: IComparer<RPMusicArtist>
    {
        public int Compare(RPMusicArtist ar1, RPMusicArtist ar2)
        {
            return string.Compare(ar1.Name, ar2.Name);
        }
    }

    public class RPMusicAlbumNameComparer : IComparer<RPMusicAlbum>
    {
        public int Compare(RPMusicAlbum al1, RPMusicAlbum al2)
        {
            return string.Compare(al1.Title, al2.Title);
        }
    }

    public class RPMusicSongTitleComparer : IComparer<RPMusicSong>
    {
        public int Compare(RPMusicSong sg1, RPMusicSong sg2)
        {
            return string.Compare(sg1.Title, sg2.Title);
        }
    }

    public class RPMusicGenreTitleComparer : IComparer<RPMusicGenre>
    {
        public int Compare(RPMusicGenre sg1, RPMusicGenre sg2)
        {
            return string.Compare(sg1.Name, sg2.Name);
        }
    }
}
