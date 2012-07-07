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
            string Name1 = ar1.Name;
            string Name2 = ar2.Name;

            if (Name1.ToUpper().StartsWith("THE "))
                if (Name1.Length > 4)
                    Name1 = Name1.Substring(4, Name1.Length - 4);

            if (Name2.ToUpper().StartsWith("THE "))
                if (Name2.Length > 4)
                    Name2 = Name2.Substring(4, Name2.Length - 4);

            return string.Compare(Name1, Name2);
        }
    }

    


    public class RPMusicAlbumNameComparer : IComparer<RPMusicAlbum>
    {
        public int Compare(RPMusicAlbum al1, RPMusicAlbum al2)
        {
            string Name1 = al1.Title;
            string Name2 = al2.Title;

            if (Name1.ToUpper().StartsWith("THE "))
                if (Name1.Length > 4)
                    Name1 = Name1.Substring(4, Name1.Length - 4);

            if (Name2.ToUpper().StartsWith("THE "))
                if (Name2.Length > 4)
                    Name2 = Name2.Substring(4, Name2.Length - 4);


            return string.Compare(Name1, Name2);
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
