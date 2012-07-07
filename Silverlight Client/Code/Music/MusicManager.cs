using System;
using System.Net;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using CommonEPG;
using CommonEPG.Comparers;

namespace SilverPotato
{
    public static class MusicManager
    {
        // Lists
        static List<RPMusicSong> AllSongs;
        static List<RPMusicArtist> AllArtists;
        static List<RPMusicAlbum> AllAlbums;
        public static List<RPMusicGenre> AllGenres;

        // Constructor
        static MusicManager()
        {
            AllSongs = new List<RPMusicSong>();
            AllArtists = new List<RPMusicArtist>();
            AllAlbums = new List<RPMusicAlbum>();

            MusicImporter.ImportMusicFrameworkCompleted += new EventHandler<GenericEventArgs<RPMusicBlob>>(MusicImporter_ImportMusicFrameworkCompleted);
        }


        #region Retrieval / Webget
        public static event EventHandler WebGetMusicFramework_Completed;
        public static bool GettingMusicFramework;
        public static bool GotMusicFramework;
        public static void WebGetMusicFramework()
        {
            if (GettingMusicFramework) return;

            GettingMusicFramework = true;
            MusicImporter.ImportMusicFramework();
        }
        static void MusicImporter_ImportMusicFrameworkCompleted(object sender, GenericEventArgs<RPMusicBlob> e)
        {
            GotMusicFramework = true;
            GettingMusicFramework = false;

            AllAlbums = e.Value.Albums;
            AllAlbums.Sort(new RPMusicAlbumNameComparer());

            AllArtists = e.Value.Artists;
            AllArtists.Sort(new RPMusicArtistNameComparer());
            
            AllGenres = e.Value.Genres;
            AllGenres.Sort(new RPMusicGenreTitleComparer());

            if (WebGetMusicFramework_Completed != null) WebGetMusicFramework_Completed(new object(), new EventArgs());
        }
        #endregion


        #region Filters / Lists
        public static Dictionary<string, List<RPMusicAlbum>> AllAlbumsGroupedByArtist()
        {
            Dictionary<string, List<RPMusicAlbum>> output = new Dictionary<string, List<RPMusicAlbum>>();

            foreach (RPMusicArtist ar in AllArtists)
            {
                List<RPMusicAlbum> ArtistAlbums = ar.Albums();
                if (ArtistAlbums.Count > 0)
                {
                    output.Add(ar.Name, ArtistAlbums);
                }
            }

            // assume AllAlbums list is already alpha sorted

            return output;
        }

        public static Dictionary<string, List<RPMusicAlbum>> AllAlbumsGroupedByAlpha()
        {
            Dictionary<string, List<RPMusicAlbum>> output = new Dictionary<string, List<RPMusicAlbum>>();

            foreach (RPMusicAlbum al in AllAlbums)
            {
                if ((string.IsNullOrEmpty(al.Title))) continue;

                // Group by first letter
                string Alpha = al.Title.Substring(0, 1).ToUpper();
                Functions.ProcessAlphaForNumbers(ref Alpha);

                if (output.ContainsKey(Alpha))
                    output[Alpha].Add(al);
                else
                {
                    List<RPMusicAlbum> newAlphaList = new List<RPMusicAlbum>();
                    newAlphaList.Add(al);
                    output.Add(Alpha, newAlphaList);
                }
            }

            // assume AllAlbums list is already alpha sorted

            return output;
        }
        public static Dictionary<string, List<RPMusicArtist>> AllArtistsGroupedByAlpha(bool limitToAlbumArtists)
        {
            Dictionary<string, List<RPMusicArtist>> output = new Dictionary<string, List<RPMusicArtist>>();

            foreach (RPMusicArtist ar in AllArtists)
            {
                if (string.IsNullOrEmpty(ar.Name)) continue;
                if (limitToAlbumArtists)
                    if (ar.Albums().Count < 1) continue;

                // Group by first letter
                string Alpha = ar.Name.Substring(0, 1).ToUpper();
                Functions.ProcessAlphaForNumbers(ref Alpha);

                if (output.ContainsKey(Alpha))
                    output[Alpha].Add(ar);
                else
                {
                    List<RPMusicArtist> newAlphaList = new List<RPMusicArtist>();
                    newAlphaList.Add(ar);
                    output.Add(Alpha, newAlphaList);
                }
            }


            // ASSUME list of artists is already alpha sorted when retrieved / merged


            return output;
        }
        public static Dictionary<string, List<RPMusicAlbum>> AlbumsForArtist(string ArtistID, bool addPseudoCollections)
        {
            Dictionary<string, List<RPMusicAlbum>> output = new Dictionary<string, List<RPMusicAlbum>>();

           
            foreach (RPMusicArtist ar in AllArtists)
            {
                if (ar.ID != ArtistID) continue;  // match artist

                List<RPMusicAlbum> ArtistAlbums = ar.Albums();
                if (ArtistAlbums.Count > 0)
                {
                    output.Add("[" + ar.Name + "]", ArtistAlbums);
                }
            }

            if (addPseudoCollections)
            {
                // Add pseudo links (at bottom)
                List<RPMusicAlbum> specialAlbums = new List<RPMusicAlbum>();

                /*
                RPMusicAlbum albAllAlbums = new RPMusicAlbum();
                albAllAlbums.ArtistID = ArtistID;
                albAllAlbums.ID = "[ALL_ALBUMS]";
                albAllAlbums.Title = "All Albums";
                
                specialAlbums.Add(albAllAlbums);
                */

                RPMusicAlbum albNoAlbums = new RPMusicAlbum();
                albNoAlbums.ArtistID = ArtistID;
                albNoAlbums.ID = "[ALL_SONGS_BY_ARTIST]";
                albNoAlbums.Title = "All Songs";

                specialAlbums.Add(albNoAlbums);
                output.Add("[PSEUDO_ALBUMS]", specialAlbums);
            }


            // assume AllAlbums list is already alpha sorted



            return output;
        }
        public static Dictionary<string, List<RPMusicAlbum>> AlbumsForGenre(RPMusicGenre genre, bool addPseudoCollection)
        {
            Dictionary<string, List<RPMusicAlbum>> output = new Dictionary<string, List<RPMusicAlbum>>();
            output.Add("[GENRE:" + genre.ID + "]", genre.Albums());

            if (addPseudoCollection)
            {
                // Add pseudo links (at bottom)
                List<RPMusicAlbum> specialAlbums = new List<RPMusicAlbum>();

                RPMusicAlbum albAllSongs = new RPMusicAlbum();
                albAllSongs.GenreID = genre.ID;
                albAllSongs.ID = "[ALL_SONGS_BY_GENRE]";
                albAllSongs.Title = "All Songs";

                specialAlbums.Add(albAllSongs);
                output.Add("[PSEUDO_ALBUMS]", specialAlbums);
            }

            return output;
        }
        
        // Helper
        public static string AlbumTitleFromID(string AlbumID)
        {
            foreach (RPMusicAlbum al in AllAlbums)
            {
                if (al.ID == AlbumID)
                {
                    return al.Title;
                }
            }

            return "Untitled Album";
        }
        #endregion



        #region Extension Methods
        // Song => 
        public static string ArtistName(this RPMusicSong sg)
        {
            RPMusicArtist ar = sg.Artist();
            if (ar != null)
                return ar.Name;
            else
                return "Unknown Artist";
        }
        public static RPMusicArtist Artist(this RPMusicSong sg)
        {
            foreach (RPMusicArtist ar in AllArtists)
            {
                if (ar.ID == sg.ArtistID)
                    return ar;
            }

            return null;
        }
        public static Uri ThumbnailUriOrNull(this RPMusicSong sg, string size)
        {
            if (string.IsNullOrEmpty(size)) size = "medium";

            return new Uri(NetworkManager.hostURL + "musicsongthumbnail64?id=" +  Uri.EscapeUriString( Functions.EncodeToBase64( sg.ID) ) +
                                                        "&size=" + size, UriKind.Absolute);
        }
        public static Uri StreamSourceUri(this RPMusicSong sg)
        {
            return new Uri(NetworkManager.hostURL + "streamsong64?id=" + Uri.EscapeUriString( Functions.EncodeToBase64( sg.ID) ) , UriKind.Absolute);
        }
        public static Uri DownloadSourceUri(this RPMusicSong sg)
        {
            return new Uri(NetworkManager.hostURL + "downloadsong64.mp3?id=" + Uri.EscapeUriString(Functions.EncodeToBase64(sg.ID)), UriKind.Absolute);
        }
        public static TimeSpan DurationTS(this RPMusicSong sg)
        {
            return TimeSpan.FromSeconds(sg.Duration);
        }
        public static string ToPrettyDuration(this RPMusicSong sg)
        {
            TimeSpan ts = sg.DurationTS();
            if (ts.Hours > 0)
                return String.Format("{0:00}:{1:00}:{2:00}", ts.Hours, ts.Minutes, ts.Seconds);
            else
                return String.Format("{0:00}:{1:00}", ts.Minutes, ts.Seconds);
        }
        // Album => 
        public static string ArtistName(this RPMusicAlbum al)
        {
            RPMusicArtist ar = al.Artist();
            if (ar != null)
                return ar.Name;
            else
                return "Unknown Artist";
        }
        public static RPMusicArtist Artist(this RPMusicAlbum al)
        {
            foreach (RPMusicArtist ar in AllArtists)
            {
                if (ar.ID == al.ArtistID)
                    return ar;
            }

            return null;
        }
        public static List<RPMusicSong> Songs(this RPMusicAlbum al)
        {
            List<RPMusicSong> output = new List<RPMusicSong>();

            foreach (RPMusicSong sg in AllSongs)
            {
                // NOT USED AT THE MOMENT
            }

            return output;
        }
        public static Uri ThumbnailUriOrNull(this RPMusicAlbum al, string size)
        {
            // Pseudo albums - local thumbnail
            if (al.IsPseudoAlbum())
            {
                if (al.ID == "[ALL_ALBUMS]")
                    return new Uri("/Images/imgAllAlbums.png", UriKind.Relative);

                if ( (al.ID == "[ALL_SONGS_BY_ARTIST]") || (al.ID == "[ALL_SONGS_BY_GENRE]"))
                    return new Uri("/Images/imgAllSongs.png", UriKind.Relative);
            }

            // Normal albums - remote thumbnail
            if (string.IsNullOrEmpty(size)) size = "medium";

            return new Uri(NetworkManager.hostURL + "musicalbumthumbnail64?id=" + Uri.EscapeUriString( Functions.EncodeToBase64( al.ID) ) +
                                                        "&size=" + size, UriKind.Absolute);
        }
        public static bool IsPseudoAlbum(this RPMusicAlbum al)
        {
            return ((al.ID.StartsWith("[ALL")));
        }
        public static RPMusicGenre Genre(this RPMusicAlbum al)
        {
            foreach (RPMusicGenre gn in AllGenres)
            {
                if (gn.ID == al.GenreID)
                    return gn;
            }

            return null;
        }

        // Artist =>
        public static List<RPMusicAlbum> Albums(this RPMusicArtist ar)
        {
            List<RPMusicAlbum> output = new List<RPMusicAlbum>();
            foreach (RPMusicAlbum al in AllAlbums)
            {
                if (al.ArtistID == ar.ID)
                    output.Add(al);
            }

            return output;
        }
        public static List<RPMusicSong> Songs(this RPMusicArtist ar)
        {
            List<RPMusicSong> output = new List<RPMusicSong>();

            foreach (RPMusicSong sg in AllSongs)
            {
                if (sg.ArtistID == ar.ID)
                    output.Add(sg);
            }

            return output;
        }
        public static Uri ThumbnailUriOrNull(this RPMusicArtist ar)
        {
            return null;
        }

        // Genre =>
        public static List<RPMusicAlbum> Albums(this RPMusicGenre gn)
        {
            List<RPMusicAlbum> output = new List<RPMusicAlbum>();
            foreach (RPMusicAlbum al in AllAlbums)
            {
                if (al.GenreID == gn.ID)
                    output.Add(al);
            }

            return output;
        }
        #endregion
    }
}

