using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Drawing;
using System.ComponentModel;
using System.Threading;
using RemotePotatoServer.Properties;
using CommonEPG;
using FatAttitude;
using System.Collections.Specialized;

namespace RemotePotatoServer
{
    public class MusicManager : IDisposable
    {
        const int BATCH_SIZE = 500;

        public MusicManager()
        {
            
        }
        public void Dispose()
        {
            
        }

        // SHARED objects used by multiple STA threads
        List<RPMusicSong> HelperReturnSongs;
        string HelperReturnString;

        #region Pictures
        /*
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
                string folderArtFN = Path.Combine(itemFolder, "folder.jpg");
                if (File.Exists(folderArtFN))
                    picFileName = folderArtFN;
            }


            FatAttitude.ShellHelper shellHelper = new FatAttitude.ShellHelper();
            MimeType = "image/jpeg";
            switch (size)
            {
                case Thumbnail_Sizes.Small:
                    return shellHelper.ThumbnailForFile(picFileName, ThumbnailSizes.Small);

                case Thumbnail_Sizes.Medium:
                    return shellHelper.ThumbnailForFile(picFileName, ThumbnailSizes.Medium);

                case Thumbnail_Sizes.Large:
                    return shellHelper.ThumbnailForFile(picFileName, ThumbnailSizes.Large);

                case Thumbnail_Sizes.ExtraLarge:
                    return shellHelper.ThumbnailForFile(picFileName, ThumbnailSizes.ExtraLarge);

                default:
                    return shellHelper.ThumbnailForFile(picFileName, ThumbnailSizes.Small);
            }

        }


        */

        public string MusicFileExistsAsXML(string itemTrackingID)
        {
            bool itemExists = MusicFileExists(itemTrackingID);

            return XMLHelper.Serialize<bool>(itemExists);
        }
        public bool MusicFileExists(string itemTrackingID)
        {
            string FN = PathForMusicFile(itemTrackingID);
            return (File.Exists(FN));
        }
        public string PathForMusicFile(string itemTrackingID)
        {
            Thread t = new Thread(SafePathForMusicFile);
            return GetStringUsingSTAThread(t, itemTrackingID);
        }
        public void SafePathForMusicFile(object itemTrackingID)
        {
            MusicHelper helper = new MusicHelper();
            bool failed = false;
            string txtError = "";
            HelperReturnString = helper.RetrieveSongPathForSongID((string)itemTrackingID, ref failed, ref txtError);

            if (failed)
                Functions.WriteLineToLogFile("Couldn't get path for song with ID " + itemTrackingID);
        }
        public string PathForAlbumThumbnail(string albumID, bool useFolderArtIfFound)
        {
            List<RPMusicSong> songs = GetSongsForAlbum(albumID);
            if (songs.Count < 1)
            {
                Functions.WriteLineToLogFile("Warning - no album found with ID " + albumID + " when trying to retrieve thumbnail.");
                return "";  // no songs
            }

            RPMusicSong firstSong = songs[0];

            string filePath = PathForMusicFile(firstSong.ID);

            if (useFolderArtIfFound)
            {
                string itemFolder = Path.GetDirectoryName(filePath);
                string folderArtFN = Path.Combine(itemFolder, "folder.jpg");
                if (File.Exists(folderArtFN))
                    return folderArtFN;
            }

            // No folder art, use first song file
            return filePath;
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
            MusicHelper helper = new MusicHelper();
            bool failed = false;
            string txtError = "";
            List<RPMusicArtist> output = helper.RetrieveAllArtists(ref failed, ref txtError);
            if (failed)            
                Functions.WriteLineToLogFile("Failed to get artists from music library: " + txtError);
            else
            {
                // Sort output A-Z
                CommonEPG.Comparers.RPMusicArtistNameComparer myComparer = new CommonEPG.Comparers.RPMusicArtistNameComparer();
                output.Sort(myComparer);
            }

            return output;
        }
        public List<RPMusicAlbum> GetAllAlbums()
        {
            MusicHelper helper = new MusicHelper();
            bool failed = false;
            string txtError = "";
            List<RPMusicAlbum> output = helper.RetrieveAllAlbums(ref failed, ref txtError);
            if (failed)
                Functions.WriteLineToLogFile("Failed to get albums from music library: " + txtError);
            else
            {
                // Sort output A-Z
                CommonEPG.Comparers.RPMusicAlbumNameComparer myComparer = new CommonEPG.Comparers.RPMusicAlbumNameComparer();
                output.Sort(myComparer);
            }

            return output;
        }
        public List<RPMusicGenre> GetAllGenres()
        {
            MusicHelper helper = new MusicHelper();
            bool failed = false;
            string txtError = "";
            List<RPMusicGenre> output = helper.RetrieveAllGenres(ref failed, ref txtError);
            if (failed)
                Functions.WriteLineToLogFile("Failed to get genres from music library: " + txtError);
            else
            {
                // Sort output A-Z
                CommonEPG.Comparers.RPMusicGenreTitleComparer myComparer = new CommonEPG.Comparers.RPMusicGenreTitleComparer();
                output.Sort(myComparer);
            }

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
        object ExclusiveMusicHelper = new object();
        public List<RPMusicSong> GetSongsForAlbum(string albumID)
        {
            Thread t = new Thread(SafeGetSongsForAlbum);
            return GetSongsUsingSTAThread(t, albumID);
        }
        public List<RPMusicSong> GetSongsForArtist(string artistID)
        {
            Thread t = new Thread(SafeGetSongsForArtist);
            return GetSongsUsingSTAThread(t, artistID);
        }
        public List<RPMusicSong> GetSongsForGenre(string genreID)
        {
            Thread t = new Thread(SafeGetSongsForGenre);
            return GetSongsUsingSTAThread(t, genreID);
        }
        void SafeGetSongsForAlbum(object albumID)
        {
            MusicHelper helper = new MusicHelper();
            bool failed = false;
            string txtError = "";
            HelperReturnSongs = helper.RetrieveSongsForAlbum((string)albumID, ref failed, ref txtError);
            if (failed)
                Functions.WriteLineToLogFile("Failed to get songs for album: " + albumID + ", error: " + txtError);
        }
        void SafeGetSongsForArtist(object artistID)
        {
            MusicHelper helper = new MusicHelper();
            bool failed = false;
            string txtError = "";
            HelperReturnSongs = helper.RetrieveSongsForArtist((string)artistID, ref failed, ref txtError);
            if (failed)
                Functions.WriteLineToLogFile("Failed to get songs for artist: " + artistID + ", error: " + txtError);
        }
        void SafeGetSongsForGenre(object genreID)
        {
            MusicHelper helper = new MusicHelper();
            bool failed = false;
            string txtError = "";
            HelperReturnSongs = helper.RetrieveSongsForGenre((string)genreID, ref failed, ref txtError);
            if (failed)
                Functions.WriteLineToLogFile("Failed to get songs for genre: " + genreID + ", error: " + txtError);
        }
        public List<RPMusicSong> GetAllSongs()
        {
            return new List<RPMusicSong>();
        }

        #endregion

        #region STA Threaded Methods
        List<RPMusicSong> GetSongsUsingSTAThread(Thread t, object strID)
        {
            t.SetApartmentState(ApartmentState.STA);

            // LOCK
            Monitor.Enter(ExclusiveMusicHelper);
            t.Start(strID);
            t.Join(); // wait for thread
            // While we still have exclusivity, grab the shared object and copy it
            List<RPMusicSong> output = (List<RPMusicSong>)Functions.DeepClone(HelperReturnSongs);
            Monitor.Exit(ExclusiveMusicHelper);

            return output;
        }
        string GetStringUsingSTAThread(Thread t, object strID)
        {
            t.SetApartmentState(ApartmentState.STA);

            // LOCK
            Monitor.Enter(ExclusiveMusicHelper);
                t.Start(strID);
                t.Join(); // wait for thread
                // While we still have exclusivity, grab the shared object and copy it
                string output = (string)HelperReturnString.Clone();
            Monitor.Exit(ExclusiveMusicHelper);

            return output;
        }
        #endregion
    }
}
