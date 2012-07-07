using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Drawing;
//using System.Reflection;
using System.ComponentModel;
using RemotePotatoServer.Properties;
using WMPLib;
using CommonEPG;
using FatAttitude;
using System.Collections.Specialized;

namespace RemotePotatoServer
{
    public class WMPManager : IDisposable
    {
        const int BATCH_SIZE = 500;
        //Tools.Impersonator userImpersonator;

        public WMPManager()
        {
        //    userImpersonator = new Tools.Impersonator("Carl", string.Empty, "[PASSWORD_REMOVED]");
        }
        public void Dispose()
        {
       //     userImpersonator.Dispose();
       //     userImpersonator = null;
        }

        #region Pictures
        public byte[] ThumbnailForWMPItemAsByte(string WMPMatchAttribute, string itemID, bool useFolderArtIfFound, Thumbnail_Sizes size, out string MimeType)
        {
            Bitmap bmp = ThumbnailForWMPItem(WMPMatchAttribute, itemID, useFolderArtIfFound, size, out MimeType);
            if (bmp == null) return null;
            try
            {
                byte[] bytes = (byte[])TypeDescriptor.GetConverter(bmp).ConvertTo(bmp, typeof(byte[]));
                return bytes;
            }
            catch (Exception ex)
            {
                Functions.WriteLineToLogFile("Exception converting WMP item thumbnail to bytes:");
                Functions.WriteExceptionToLogFile(ex);
            }

            return null;
        }
        public Bitmap ThumbnailForWMPItem(string WMPMatchAttribute, string itemID, bool useFolderArtIfFound, Thumbnail_Sizes size, out string MimeType)
        {
            // Get URL
            MimeType = "";
            string picFileName;
            WindowsMediaPlayer WMPlayer = new WindowsMediaPlayer();

            IWMPPlaylist pl = WMPlayer.mediaCollection.getByAttribute(WMPMatchAttribute, itemID);
            if (pl.count == 0)
            {
                Functions.WriteLineToLogFile("Warning - no items found in library with ID " + itemID);
                return null;
            }

            //if (pl.count != 1) Functions.WriteLineToLogFile("Warning - more than one item found in library with ID " + itemID);

            IWMPMedia pic = pl.get_Item(0);

            picFileName = pic.sourceURL;

            WMPlayer.close();

            if (useFolderArtIfFound)
            {
                string itemFolder = Path.GetDirectoryName(picFileName);
                string folderArtFN = Path.Combine(itemFolder,"folder.jpg");
                if (File.Exists(folderArtFN))
                    picFileName = folderArtFN;
            }
            
                
            ShellHelper shellHelper = new ShellHelper();
            MimeType = "image/jpeg";
            string strLog = ""; // ignore
            switch (size)
            {
                case Thumbnail_Sizes.Small:
                    return shellHelper.ThumbnailForFile(picFileName, ThumbnailSizes.Small, ref strLog);

                case Thumbnail_Sizes.Medium:
                    return shellHelper.ThumbnailForFile(picFileName, ThumbnailSizes.Medium, ref strLog);

                case Thumbnail_Sizes.Large:
                    return shellHelper.ThumbnailForFile(picFileName, ThumbnailSizes.Large, ref strLog);

                case Thumbnail_Sizes.ExtraLarge:
                    return shellHelper.ThumbnailForFile(picFileName, ThumbnailSizes.ExtraLarge, ref strLog);

                default:
                    return shellHelper.ThumbnailForFile(picFileName, ThumbnailSizes.Small, ref strLog);
            }
            
        }
        public string WMPItemFileExistsAsXML(string itemTrackingID)
        {
            bool itemExists = WMPItemFileExists(itemTrackingID);

            return XMLHelper.Serialize<bool>(itemExists);
        }
        public bool WMPItemFileExists(string itemTrackingID)
        {
            string FN = FileNameForWMPItem(itemTrackingID);
            return (File.Exists(FN));
        }
        public string FileNameForWMPItem(string itemTrackingID)
        {
            WindowsMediaPlayer WMPlayer = new WindowsMediaPlayer();
            IWMPPlaylist pl = WMPlayer.mediaCollection.getByAttribute("TrackingID", itemTrackingID);
            if (pl.count == 0)
            {
                Functions.WriteLineToLogFile("Warning - no item found in library with ID " + itemTrackingID);
                return null;
            }

            if (pl.count != 1) Functions.WriteLineToLogFile("Warning - more than one item found in library with ID " + itemTrackingID);

            IWMPMedia mItem = pl.get_Item(0);


            WMPlayer.close();
            return mItem.sourceURL;            
        }
        
        public enum Thumbnail_Sizes
        {
            Small,
            Medium,
            Large,
            ExtraLarge
        }
        #endregion

        #region Music

        public string MusicFrameworkAsXML()
        {
            RPMusicBlob blob = GetMusicFramework();
            return XMLHelper.Serialize<RPMusicBlob>(blob);
        }
        public RPMusicBlob GetMusicFramework() 
        {
            RPMusicBlob blob = new RPMusicBlob();

            blob.Artists = GetAllArtists();
            blob.Albums = GetAllAlbums();
            blob.Genres = GetAllGenres();

            return blob;
        }
        public List<RPMusicArtist> GetAllArtists()
        {
            if (!Settings.Default.EnableMusicLibrary) return new List<RPMusicArtist>();

            WindowsMediaPlayer WMPlayer = new WindowsMediaPlayer();

            List<RPMusicArtist> output = new List<RPMusicArtist>();
            List<RPMusicArtist> outputList = new List<RPMusicArtist>();
            IWMPStringCollection scArtists = WMPlayer.mediaCollection.getAttributeStringCollection("Author", "Audio");

            for (int i = 0; i < scArtists.count; i++)
            {
                string strArtistName = scArtists.Item(i);
                if (string.IsNullOrEmpty(strArtistName)) continue; // Believe it or not WMP sometimes returns an empty artist name

                RPMusicArtist artist = new RPMusicArtist(strArtistName);
                output.Add(artist);
            }

            WMPlayer.close();

            // Sort output A-Z
            CommonEPG.Comparers.RPMusicArtistNameComparer myComparer = new CommonEPG.Comparers.RPMusicArtistNameComparer();
            output.Sort(myComparer);

            return output;
        }
        public List<RPMusicAlbum> GetAllAlbums()
        {
            if (!Settings.Default.EnableMusicLibrary) return new List<RPMusicAlbum>();

            WindowsMediaPlayer WMPlayer = new WindowsMediaPlayer();
            List<RPMusicAlbum> output = new List<RPMusicAlbum>();
            IWMPStringCollection scAlbums = WMPlayer.mediaCollection.getAttributeStringCollection("AlbumID", "Audio");

            for (int i = 0; i < scAlbums.count; i++)
            {
                if (string.IsNullOrEmpty(scAlbums.Item(i))) continue; // avoid null strings

                RPMusicAlbum album = new RPMusicAlbum();
                album.ID = scAlbums.Item(i);

                // Find a song in this album
                IWMPPlaylist pl = WMPlayer.mediaCollection.getByAttribute("AlbumID", album.ID);
                if (pl.count < 1) continue; // don't add the album; no matching media items  (must be an error, shouldn't happen)

                

                IWMPMedia song = pl.get_Item(0); // just use the first song to get the additional album info
                
                // ALBUM ARTIST: Try to use the song property's "album artist", if this doesn't work, use the first song's author
                album.ArtistID = song.getItemInfo("WM/AlbumArtist");
                if (string.IsNullOrEmpty(album.ArtistID))
                    album.ArtistID = song.getItemInfo("Author");

                album.Title = song.getItemInfo("WM/AlbumTitle");
                album.GenreID = song.getItemInfo("Genre");

                output.Add(album);
            }

            WMPlayer.close();

            // Sort output A-Z
            CommonEPG.Comparers.RPMusicAlbumNameComparer myComparer = new CommonEPG.Comparers.RPMusicAlbumNameComparer();
            output.Sort(myComparer);
            
            return output;
        }
        public List<RPMusicGenre> GetAllGenres()
        {
            if (!Settings.Default.EnableMusicLibrary) return new List<RPMusicGenre>();

            WindowsMediaPlayer WMPlayer = new WindowsMediaPlayer();
            List<RPMusicGenre> output = new List<RPMusicGenre>();

            IWMPStringCollection scGenres = WMPlayer.mediaCollection.getAttributeStringCollection("Genre", "Audio");

            for (int i = 0; i < scGenres.count; i++)
            {
                RPMusicGenre genre = new RPMusicGenre(scGenres.Item(i));
                output.Add(genre);
            }

            WMPlayer.close();

            // Sort output A-Z
            CommonEPG.Comparers.RPMusicGenreTitleComparer myComparer = new CommonEPG.Comparers.RPMusicGenreTitleComparer();
            output.Sort(myComparer);

            return output;
        }
        public string GetSongsForArtistAsXML(string artistID)
        {
            List<RPMusicSong> songs = GetSongsForArtist(artistID);
            return XMLHelper.Serialize<List<RPMusicSong>>(songs);
        }
        public string GetSongsForAlbumAsXML(string albumID)
        {
            List<RPMusicSong> songs = GetSongsForAlbum(albumID);
            return XMLHelper.Serialize<List<RPMusicSong>>(songs);
        }
        public string GetSongsForGenreAsXML(string genreID)
        {
            List<RPMusicSong> songs = GetSongsForGenre(genreID);
            return XMLHelper.Serialize<List<RPMusicSong>>(songs);
        }
        public string GetAllSongsAsXML()
        {
            List<RPMusicSong> songs = GetAllSongs();
            return XMLHelper.Serialize<List<RPMusicSong>>(songs);
        }
        public List<RPMusicSong> GetSongsForArtist(string artistID)
        {
            if (!Settings.Default.EnableMusicLibrary) return new List<RPMusicSong>();

            WindowsMediaPlayer WMPlayer = new WindowsMediaPlayer();
            List<RPMusicSong> output = new List<RPMusicSong>();
            
            // Find all songs by this artist
            IWMPPlaylist pl = WMPlayer.mediaCollection.getByAttribute("Author", artistID);
            for (int s = 0; s < pl.count; s++)
            {
                IWMPMedia song = pl.get_Item(s);

                RPMusicSong RPsong = CreateRPMusicSongFromIWMPMediaSong(song);
                if (RPsong != null) output.Add(RPsong);
            }

            WMPlayer.close();
            return output;
        }
        public List<RPMusicSong> GetSongsForAlbum(string albumID)
        {
            if (!Settings.Default.EnableMusicLibrary) return new List<RPMusicSong>();

            WindowsMediaPlayer WMPlayer = new WindowsMediaPlayer();
            List<RPMusicSong> output = new List<RPMusicSong>();

            // Find all songs by this artist
            IWMPPlaylist pl = WMPlayer.mediaCollection.getByAttribute("AlbumID", albumID);
            for (int s = 0; s < pl.count; s++)
            {
                IWMPMedia song = pl.get_Item(s);

                RPMusicSong RPsong = CreateRPMusicSongFromIWMPMediaSong(song);
                if (RPsong != null) output.Add(RPsong);
            }

            WMPlayer.close();
            return output;
        }
        public List<RPMusicSong> GetSongsForGenre(string genreID)
        {
            if (!Settings.Default.EnableMusicLibrary) return new List<RPMusicSong>();

            WindowsMediaPlayer WMPlayer = new WindowsMediaPlayer();
            List<RPMusicSong> output = new List<RPMusicSong>();

            // Find all songs by this artist
            IWMPPlaylist pl = WMPlayer.mediaCollection.getByAttribute("Genre", genreID);
            for (int s = 0; s < pl.count; s++)
            {
                IWMPMedia song = pl.get_Item(s);

                RPMusicSong RPsong = CreateRPMusicSongFromIWMPMediaSong(song);
                if (RPsong != null) output.Add(RPsong);
            }

            WMPlayer.close();
            return output;
        }
        public List<RPMusicSong> GetAllSongs()
        {
            if (!Settings.Default.EnableMusicLibrary) return new List<RPMusicSong>();

            WindowsMediaPlayer WMPlayer = new WindowsMediaPlayer();
            List<RPMusicSong> output = new List<RPMusicSong>();

            // Find all songs
            IWMPPlaylist pl = WMPlayer.mediaCollection.getByAttribute("MediaType", "Audio");
            for (int s = 0; s < pl.count; s++)
            {
                IWMPMedia song = pl.get_Item(s);

                RPMusicSong RPsong = CreateRPMusicSongFromIWMPMediaSong(song);
                if (RPsong != null) output.Add(RPsong);
            }

            WMPlayer.close();
            return output;
        }

        public RPMusicSong CreateRPMusicSongFromIWMPMediaSong(IWMPMedia song)
        {

            string FN = song.sourceURL;
            if (!File.Exists(FN)) return null;

            RPMusicSong RPsong = new RPMusicSong();
            RPsong.ArtistID = song.getItemInfo("Author");
            //RPsong.FileName = song.sourceURL;
            RPsong.ID = song.getItemInfo("TrackingID");
            string userRating = song.getItemInfo("UserRating");
            int intRating;
            if (int.TryParse(userRating, out intRating))
                RPsong.UserRating = intRating;
            RPsong.Title = song.getItemInfo("Title");
            RPsong.Duration = (long)song.duration;

            // Just store extension for now so client knows what type of music file this is
            RPsong.FileExtension = Path.GetExtension(FN);
            FileInfo fi = new FileInfo(FN);
            RPsong.FileSizeBytes = fi.Length;

            int intTrackNumber;
            string strTrackNumber = song.getItemInfo("WM/TrackNumber");

            if ((! string.IsNullOrEmpty(strTrackNumber)) && (int.TryParse(strTrackNumber, out intTrackNumber)))
                RPsong.TrackNumber = intTrackNumber;
            return RPsong;
        }
        #endregion

        

    }

}

