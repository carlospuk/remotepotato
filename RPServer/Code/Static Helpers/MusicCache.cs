using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using CommonEPG;
namespace RemotePotatoServer
{
    public class MusicCache
    {

        private MusicCache() {
            Framework = null;
            LastAccessed = DateTime.Now;
            
        }

        #region Initialise / Expire
        public RPMusicBlob Framework;
        DateTime LastAccessed;
        const int EXPIRE_CACHE_AFTER_THIS_MANY_MINUTES = 30;

        Timer expireCacheTimer;

        public void RefreshCache()
        {
            FlushCache();
            CheckInitialised();
        }
        public void CheckInitialised()
        {
            if (Framework != null) return;

            using (WMPManager manager = new WMPManager())
            {
                Framework = manager.GetMusicFramework();
                InitCacheTimer();
            }
        }
        void InitCacheTimer()
        {
            if (expireCacheTimer != null)
            {
                try
                {
                    expireCacheTimer.Stop();
                    expireCacheTimer = null;
                }
                catch
                { }
            }

            expireCacheTimer = new Timer(30000);
            expireCacheTimer.AutoReset = true;
            expireCacheTimer.Elapsed += new ElapsedEventHandler(expireCacheTimer_Elapsed);
            expireCacheTimer.Start();
        }
        void expireCacheTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            ExpireCacheIfLastAccessExpired();
        }
        void ExpireCacheIfLastAccessExpired()
        {
            // Check whether to expire the cache
            if (Framework == null) return; // no cache

            TimeSpan timeSinceLastAccessed = DateTime.Now.Subtract(LastAccessed);

            if (timeSinceLastAccessed.TotalMinutes >= EXPIRE_CACHE_AFTER_THIS_MANY_MINUTES)
            {
                FlushCache();
                expireCacheTimer.Stop();
                expireCacheTimer = null;
            }
        }
        void FlushCache()
        {
            Framework.Albums.Clear();
            Framework.Artists.Clear();
            Framework.Genres.Clear();
            Framework = null;
        }
        #endregion

        #region Lookup

        // Pseudo properties (used by extension methods)
        static List<RPMusicArtist> AllArtists
        {
            get
            {
                Default.CheckInitialised();
                return Default.Framework.Artists;
            }
        }
        static List<RPMusicAlbum> AllAlbums
        {
            get
            {
                Default.CheckInitialised();
                return Default.Framework.Albums;
            }
        }
        static List<RPMusicGenre> AllGenres
        {
            get
            {
                Default.CheckInitialised();
                return Default.Framework.Genres;
            }
        }

        public RPMusicArtist artistWithID(string artistID)
        {
            CheckInitialised();

            foreach (RPMusicArtist artist in Framework.Artists)
            {
                if (artist.ID == artistID)
                    return artist;
            }

            return null;
        }
        public RPMusicAlbum albumWithID(string ID)
        {
            CheckInitialised();

            foreach (RPMusicAlbum album in Framework.Albums)
            {
                if (album.ID == ID)
                    return album;
            }

            return null;
        }
        public RPMusicGenre genreWithID(string ID)
        {
            CheckInitialised();

            foreach (RPMusicGenre genre in Framework.Genres)
            {
                if (genre.ID == ID)
                    return genre;
            }

            return null;
        }
        #endregion

        #region Singleton Methods
        static MusicCache instance = null;
        static readonly object padlock = new object();
        internal static MusicCache Default
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new MusicCache();
                    }
                    return instance;
                }
            }
        }
        #endregion

    }


    public static class ExtensionsMusic
    {

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
            foreach (RPMusicArtist ar in MusicCache.Default.Framework.Artists)
            {
                if (ar.ID == sg.ArtistID)
                    return ar;
            }

            return null;
        }
        public static Uri ThumbnailUriOrNull(this RPMusicSong sg, string size)
        {
            if (string.IsNullOrEmpty(size)) size = "medium";

            return new Uri("musicsongthumbnail64?id=" + Uri.EscapeUriString(Functions.EncodeToBase64(sg.ID)) +
                                                        "&size=" + size, UriKind.Absolute);
        }
        public static Uri StreamSourceUri(this RPMusicSong sg)
        {
            return new Uri("streamsong64?id=" + Uri.EscapeUriString(Functions.EncodeToBase64(sg.ID)), UriKind.Absolute);
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
        public static RPMusicArtist Artist(this RPMusicAlbum al)
        {
            foreach (RPMusicArtist ar in MusicCache.Default.Framework.Artists)
            {
                if (ar.ID == al.ArtistID)
                    return ar;
            }

            return null;
        }
        public static string ArtistName(this RPMusicAlbum al)
        {
            RPMusicArtist ar = al.Artist();
            if (ar != null)
                return ar.Name;
            else
                return "Unknown Artist";
        }

        public static Uri ThumbnailUriOrNull(this RPMusicAlbum al, string size)
        {
            // Normal albums - remote thumbnail
            if (string.IsNullOrEmpty(size)) size = "medium";

            return new Uri("musicalbumthumbnail64?id=" + Uri.EscapeUriString(Functions.EncodeToBase64(al.ID)) +
                                                        "&size=" + size, UriKind.Absolute);
        }
        public static bool IsPseudoAlbum(this RPMusicAlbum al)
        {
            return ((al.ID.StartsWith("[ALL")));
        }
        public static RPMusicGenre Genre(this RPMusicAlbum al)
        {
            foreach (RPMusicGenre gn in MusicCache.Default.Framework.Genres)
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
            foreach (RPMusicAlbum al in MusicCache.Default.Framework.Albums)
            {
                if (al.ArtistID == ar.ID)
                    output.Add(al);
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
            foreach (RPMusicAlbum al in MusicCache.Default.Framework.Albums)
            {
                if (al.GenreID == gn.ID)
                    output.Add(al);
            }

            return output;
        }
        #endregion
    }
}
