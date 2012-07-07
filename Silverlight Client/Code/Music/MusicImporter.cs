using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Text;
using System.Windows;
using CommonEPG;
using System.Xml.Serialization;
using System.Xml;


namespace SilverPotato
{
    public static class MusicImporter
    {
        public static event EventHandler<GenericEventArgs<RPMusicBlob>> ImportMusicFrameworkCompleted;

        // The music framework
        public static void ImportMusicFramework()
        {
            RPWebClient client = new RPWebClient();
            client.GetStringByGettingCompleted += new EventHandler<UploadStringCompletedEventArgs>(ImportMusicFramework_GetStringByGettingCompleted);
            client.GetStringByGetting("xml/music/framework" + Settings.ZipDataStreamsAddendum);
        }
        static void ImportMusicFramework_GetStringByGettingCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            if ((e.Error != null) || (string.IsNullOrEmpty(e.Result)))
            {
                ErrorManager.DisplayAndLogError("Could not get music framework from server.");
                Functions.WriteExceptionToLogFile(e.Error);
                ImportMusicFrameworkCompleted(new object(), new GenericEventArgs<RPMusicBlob>(new RPMusicBlob()));
                return;
            }

            string strOut = e.Result;
            if (Settings.ZipDataStreams)
            {
                if (!ZipManager.UnzipString(ref strOut))
                {
                    ErrorManager.DisplayAndLogError("Could not unzip downloaded music framework blob from server.");
                    ImportMusicFrameworkCompleted(new object(), new GenericEventArgs<RPMusicBlob>(new RPMusicBlob()));
                    return;
                }
            }

            // Prepare to deserialize
            RPMusicBlob blob = new RPMusicBlob();
            XmlSerializer serializer = new XmlSerializer(blob.GetType());

            // Replace nulls - cannot be deserialized
            StringReader sr = new StringReader(strOut);

            // Dont check characters
            XmlReaderSettings xset = new XmlReaderSettings();
            xset.CheckCharacters = false;
            XmlReader xread = XmlReader.Create(sr, xset);

            // Deserialize
            blob = (RPMusicBlob)serializer.Deserialize(xread);
            strOut = null;

            // Success
            ImportMusicFrameworkCompleted(new object(), new GenericEventArgs<RPMusicBlob>(blob));
        }

        // Songs for an artist
        public static event EventHandler<GenericEventArgs<List<RPMusicSong>>> ImportSongsForArtistCompleted;
        public static void ImportSongsForArtist(string ArtistID)
        {
            RPWebClient client = new RPWebClient();
            client.GetStringByGettingCompleted += new EventHandler<UploadStringCompletedEventArgs>(ImportSongsForArtist_GetStringByGettingCompleted);
            client.GetStringByGetting("xml/music/songs/artist64/" + Uri.EscapeUriString( Functions.EncodeToBase64( ArtistID) ) + Settings.ZipDataStreamsAddendum);
        }
        static void ImportSongsForArtist_GetStringByGettingCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            if ((e.Error != null) || (string.IsNullOrEmpty(e.Result)))
            {
                ErrorManager.DisplayAndLogError("Could not get songs from server.");
                Functions.WriteExceptionToLogFile(e.Error);
                ImportSongsForArtistCompleted(new object(), new GenericEventArgs<List<RPMusicSong>>(new List<RPMusicSong>()));
                return;
            }

            string strOut = e.Result;
            if (Settings.ZipDataStreams)
            {
                if (!ZipManager.UnzipString(ref strOut))
                {
                    ErrorManager.DisplayAndLogError("Could not unzip downloaded songs from server.");
                    ImportSongsForArtistCompleted(new object(), new GenericEventArgs<List<RPMusicSong>>(new List<RPMusicSong>()));
                    return;
                }
            }

            // Prepare to deserialize
            List<RPMusicSong> songs = new List<RPMusicSong>();
            XmlSerializer serializer = new XmlSerializer(songs.GetType());

            // Replace nulls - cannot be deserialized
            StringReader sr = new StringReader(strOut);

            // Dont check characters
            XmlReaderSettings xset = new XmlReaderSettings();
            xset.CheckCharacters = false;
            XmlReader xread = XmlReader.Create(sr, xset);

            // Deserialize
            songs = (List<RPMusicSong>)serializer.Deserialize(xread);
            strOut = null;

            // Success
            ImportSongsForArtistCompleted(new object(), new GenericEventArgs<List<RPMusicSong>>(songs));
        }

        // Songs for an album
        public static event EventHandler<GenericEventArgs<List<RPMusicSong>>> ImportSongsForAlbumCompleted;
        public static void ImportSongsForAlbum(string AlbumID)
        {
            RPWebClient client = new RPWebClient();
            client.GetStringByGettingCompleted += new EventHandler<UploadStringCompletedEventArgs>(ImportSongsForAlbum_GetStringByGettingCompleted);
            client.GetStringByGetting("xml/music/songs/album64/" + Uri.EscapeUriString( Functions.EncodeToBase64(AlbumID)) + Settings.ZipDataStreamsAddendum);
        }
        static void ImportSongsForAlbum_GetStringByGettingCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            if ((e.Error != null) || (string.IsNullOrEmpty(e.Result)))
            {
                ErrorManager.DisplayAndLogError("Could not get songs from server.");
                Functions.WriteExceptionToLogFile(e.Error);
                ImportSongsForAlbumCompleted(new object(), new GenericEventArgs<List<RPMusicSong>>(new List<RPMusicSong>()));
                return;
            }

            string strOut = e.Result;
            if (Settings.ZipDataStreams)
            {
                if (!ZipManager.UnzipString(ref strOut))
                {
                    ErrorManager.DisplayAndLogError("Could not unzip downloaded songs from server.");
                    ImportSongsForAlbumCompleted(new object(), new GenericEventArgs<List<RPMusicSong>>(new List<RPMusicSong>()));
                    return;
                }
            }

            // Prepare to deserialize
            List<RPMusicSong> songs = new List<RPMusicSong>();
            XmlSerializer serializer = new XmlSerializer(songs.GetType());

            // Replace nulls - cannot be deserialized
            StringReader sr = new StringReader(strOut);

            // Dont check characters
            XmlReaderSettings xset = new XmlReaderSettings();
            xset.CheckCharacters = false;
            XmlReader xread = XmlReader.Create(sr, xset);

            // Deserialize
            songs = (List<RPMusicSong>)serializer.Deserialize(xread);
            strOut = null;

            // Success
            ImportSongsForAlbumCompleted(new object(), new GenericEventArgs<List<RPMusicSong>>(songs));
        }

        // Songs of a given genre
        public static event EventHandler<GenericEventArgs<List<RPMusicSong>>> ImportSongsForGenreCompleted;
        public static void ImportSongsForGenre(string GenreID)
        {
            RPWebClient client = new RPWebClient();
            client.GetStringByGettingCompleted += new EventHandler<UploadStringCompletedEventArgs>(ImportSongsForGenre_GetStringByGettingCompleted);
            client.GetStringByGetting("xml/music/songs/genre64/" + Uri.EscapeUriString( Functions.EncodeToBase64(GenreID)) + Settings.ZipDataStreamsAddendum);
        }
        static void ImportSongsForGenre_GetStringByGettingCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            if ((e.Error != null) || (string.IsNullOrEmpty(e.Result)))
            {
                ErrorManager.DisplayAndLogError("Could not get songs from server.");
                Functions.WriteExceptionToLogFile(e.Error);
                ImportSongsForGenreCompleted(new object(), new GenericEventArgs<List<RPMusicSong>>(new List<RPMusicSong>()));
                return;
            }

            string strOut = e.Result;
            if (Settings.ZipDataStreams)
            {
                if (!ZipManager.UnzipString(ref strOut))
                {
                    ErrorManager.DisplayAndLogError("Could not unzip downloaded songs from server.");
                    ImportSongsForGenreCompleted(new object(), new GenericEventArgs<List<RPMusicSong>>(new List<RPMusicSong>()));
                    return;
                }
            }

            // Prepare to deserialize
            List<RPMusicSong> songs = new List<RPMusicSong>();
            XmlSerializer serializer = new XmlSerializer(songs.GetType());

            // Replace nulls - cannot be deserialized
            StringReader sr = new StringReader(strOut);

            // Dont check characters
            XmlReaderSettings xset = new XmlReaderSettings();
            xset.CheckCharacters = false;
            XmlReader xread = XmlReader.Create(sr, xset);

            // Deserialize
            songs = (List<RPMusicSong>)serializer.Deserialize(xread);
            strOut = null;

            // Success
            if (ImportSongsForGenreCompleted != null) ImportSongsForGenreCompleted(new object(), new GenericEventArgs<List<RPMusicSong>>(songs));
        }

        // All songs
        public static event EventHandler<GenericEventArgs<List<RPMusicSong>>> ImportAllSongsCompleted;
        public static void ImportAllSongs()
        {
            RPWebClient client = new RPWebClient();
            client.GetStringByGettingCompleted += new EventHandler<UploadStringCompletedEventArgs>(ImportAllSongs_GetStringByGettingCompleted);
            client.GetStringByGetting("xml/music/songs/all" + Settings.ZipDataStreamsAddendum);
        }
        static void ImportAllSongs_GetStringByGettingCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            if ((e.Error != null) || (string.IsNullOrEmpty(e.Result)))
            {
                ErrorManager.DisplayAndLogError("Could not get songs from server.");
                Functions.WriteExceptionToLogFile(e.Error);
                ImportAllSongsCompleted(new object(), new GenericEventArgs<List<RPMusicSong>>(new List<RPMusicSong>()));
                return;
            }

            string strOut = e.Result;
            if (Settings.ZipDataStreams)
            {
                if (!ZipManager.UnzipString(ref strOut))
                {
                    ErrorManager.DisplayAndLogError("Could not unzip downloaded songs from server.");
                    ImportAllSongsCompleted(new object(), new GenericEventArgs<List<RPMusicSong>>(new List<RPMusicSong>()));
                    return;
                }
            }

            // Prepare to deserialize
            List<RPMusicSong> songs = new List<RPMusicSong>();
            XmlSerializer serializer = new XmlSerializer(songs.GetType());

            // Replace nulls - cannot be deserialized
            StringReader sr = new StringReader(strOut);

            // Dont check characters
            XmlReaderSettings xset = new XmlReaderSettings();
            xset.CheckCharacters = false;
            XmlReader xread = XmlReader.Create(sr, xset);

            // Deserialize
            songs = (List<RPMusicSong>)serializer.Deserialize(xread);
            strOut = null;

            // Success
            ImportAllSongsCompleted(new object(), new GenericEventArgs<List<RPMusicSong>>(songs));
        }

        public static event EventHandler<GenericEventArgs<bool>> CheckSongCanStreamCompleted;
        public static void CheckIfSongCanStream(RPMusicSong Song)
        {
            RPWebClient client = new RPWebClient();
            client.GetStringByGettingCompleted += new EventHandler<UploadStringCompletedEventArgs>(CheckIfSongCanStream_GetStringByGettingCompleted);
            client.GetStringByGetting("xml/music/songs/checkexists64/" + Uri.EscapeUriString( Functions.EncodeToBase64( Song.ID) ) );
        }
        static void CheckIfSongCanStream_GetStringByGettingCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            if ((e.Error != null) || (string.IsNullOrEmpty(e.Result)))
            {
                ErrorManager.DisplayAndLogError("Could not determine if song could stream - no response from server.");
                Functions.WriteExceptionToLogFile(e.Error);
                CheckSongCanStreamCompleted(new object(), new GenericEventArgs<bool>(false));
                return;
            }
            
            
            // Deserialize (a bool - extravagent!)
            bool canStream = e.Result.ToUpper().Contains("TRUE");            

            // Success
            CheckSongCanStreamCompleted(new object(), new GenericEventArgs<bool>(canStream));
        }

    }
}
