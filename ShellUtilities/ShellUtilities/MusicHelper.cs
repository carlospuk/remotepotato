using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
using System.Diagnostics;
using System.ComponentModel;
using CommonEPG;

/*
 * An abandoned branch to obtain MP3 tag information using native Windows API calls
 * It was slow and unreliable
 * 
 */

namespace FatAttitude
{
    public class MusicHelper
    {
        public enum ShellFileTypes { MusicFiles }


        public MusicHelper()
        {

        }
        /*

        enum RetrievableCollections {AllArtists, AllGenres, AllAlbums, AllSongs, NamedArtist, NamedAlbum, NamedGenre}
        enum MusicHelperActions { RetrieveData, WriteIDs }
        MusicHelperActions currentAction;
        RetrievableCollections retrievingCollection;
        List<RPMusicArtist> AllArtistsList;
        List<RPMusicAlbum> AllAlbumsList;
        List<RPMusicGenre> AllGenresList;
        List<RPMusicSong> AllSongsList;
        public List<RPMusicArtist> RetrieveAllArtists(ref bool failed, ref string txtError)
        {
            retrievingCollection = RetrievableCollections.AllArtists;
            currentAction = MusicHelperActions.RetrieveData;
            AllArtistsList = new List<RPMusicArtist>();
            TraverseLibrary(ref failed, ref txtError);
            
            return AllArtistsList.Distinct().ToList();
        }
        public List<RPMusicAlbum> RetrieveAllAlbums(ref bool failed, ref string txtError)
        {
            retrievingCollection = RetrievableCollections.AllAlbums;
            currentAction = MusicHelperActions.RetrieveData;
            AllAlbumsList = new List<RPMusicAlbum>();
            TraverseLibrary(ref failed, ref txtError);

            return AllAlbumsList.Distinct().ToList();
        }
        public List<RPMusicGenre> RetrieveAllGenres(ref bool failed, ref string txtError)
        {
            retrievingCollection = RetrievableCollections.AllGenres;
            currentAction = MusicHelperActions.RetrieveData;
            AllGenresList = new List<RPMusicGenre>();
            TraverseLibrary(ref failed, ref txtError);

            return AllGenresList.Distinct().ToList();
        }
        public string RetrieveSongPathForSongID(string songID, ref bool failed, ref string txtError)
        {
            Debug.Print("RetrieveSongPathForID: " + songID);
            List<ShellFile> files = RetrieveShellFilesForSearch("System.Media.UniqueFileIdentifier", "RPID=" + songID, SearchConditionOperation.ValueContains, ref failed, ref txtError);
            if (files.Count() < 1)
            {
                failed = true;
                txtError = "No song with the ID " + songID + " was found.";
                return "";
            }

            if (files.Count() > 1)
                Debug.Print("Warning: multiple (" + files.Count.ToString() + ") songs found with ID " + songID );

            ShellFile file = files[0];
            return file.Path;
        }
        public List<RPMusicSong> RetrieveSongsForAlbum(string albumID, ref bool failed, ref string txtError)
        {
            return RetrieveSongsForSearch("System.Music.AlbumID", albumID, SearchConditionOperation.Equal, ref failed, ref txtError);
        }
        public List<RPMusicSong> RetrieveSongsForArtist(string artistID, ref bool failed, ref string txtError)
        {
            return RetrieveSongsForSearch("System.Music.DisplayArtist", artistID, SearchConditionOperation.Equal, ref failed, ref txtError);
        }
        public List<RPMusicSong> RetrieveSongsForGenre(string genreID, ref bool failed, ref string txtError)
        {
            return RetrieveSongsForSearch("System.Music.Genre", genreID, SearchConditionOperation.ValueContains, ref failed, ref txtError);
        }
        public List<RPMusicSong> RetrieveSongsForSearch(string propertyCanonicalName, string propertyValue, SearchConditionOperation matchType, ref bool failed, ref string txtError)
        {
            if (!ShellLibrary.IsPlatformSupported)
            {
                failed = true;
                txtError = "This version of Windows does not support libraries.";
                return new List<RPMusicSong>();
            }

            // TODO: Check if music library exists
            

            SearchCondition sc = SearchConditionFactory.CreateLeafCondition(propertyCanonicalName, propertyValue, matchType);
            ShellContainer[] searchScope = musicLibraryFolders();
            Debug.Print("RetrieveSongsForSearch:  scope is " + searchScope.Count().ToString() + " folders.");
            foreach (ShellContainer container in searchScope)
            {
                Debug.Print("ParseName:" + container.ParsingName);
            }

            ShellFileSystemFolder myFolder = ShellFileSystemFolder.FromFolderPath(@"C:\Users\carl\Music");

            //using (ShellSearchFolder searchFolder = new ShellSearchFolder(sc, searchScope) )
            using (ShellSearchFolder searchFolder = new ShellSearchFolder(sc, myFolder))
            {
                retrievingCollection = RetrievableCollections.AllSongs;
                AllSongsList = new List<RPMusicSong>();

                Debug.Print("SearchFolder: found " + searchFolder.Count().ToString() + " items.");

                foreach (ShellObject obj in searchFolder)
                {
                    if (obj is ShellFile)
                        processFile((ShellFile)obj, ref failed, ref txtError);
                }
            }

            return AllSongsList;
        }
        public List<ShellFile> RetrieveShellFilesForSearch(string propertyCanonicalName, string propertyValue, SearchConditionOperation matchType, ref bool failed, ref string txtError)
        {
            List<ShellFile> output = new List<ShellFile>();

            if (!ShellLibrary.IsPlatformSupported)
            {
                failed = true;
                txtError = "This version of Windows does not support libraries.";
                return output;
            }

            SearchCondition sc = SearchConditionFactory.CreateLeafCondition(propertyCanonicalName, propertyValue, matchType);
            using (ShellSearchFolder searchFolder = new ShellSearchFolder(sc, musicLibraryFolders() ))
            {
                foreach (ShellObject obj in searchFolder)
                {
                    if (obj is ShellFile)
                        output.Add((ShellFile)obj);
                }
            }

            return output;
        }
        ShellContainer[] musicLibraryFolders()
        {
            List<ShellContainer> output = new List<ShellContainer>();

      
            using (ShellLibrary musicLibrary = ShellLibrary.Load(KnownFolders.MusicLibrary, true))
            {
                foreach (ShellContainer folder in musicLibrary)
                {
                    if (folder is ShellFileSystemFolder)
                        output.Add(folder);
                }
            }

            return output.ToArray();
            

        }

        #region Library IDs
        public event EventHandler<GenericEventArgs<int>> SetMusicLibraryIDProgressReport;
        public event EventHandler<GenericEventArgs<bool>> SetMusicLibraryIDsASync_Complete;
        int musicFileCounter = 0;
        bool SetMusicLibraryIDsFailed;
        public string SetMusicLibraryIDsTxtError;
        public void SetMusicLibraryIDsASync(ref bool failed, ref string txtError)
        {
            musicFileCounter = 0;
            currentAction = MusicHelperActions.WriteIDs;

            SetMusicLibraryIDsTxtError = "";
            SetMusicLibraryIDsFailed = false;

            BackgroundWorker worker = new BackgroundWorker();
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
            worker.DoWork += new DoWorkEventHandler(worker_DoWork);
            worker.RunWorkerAsync();
        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            TraverseLibrary(ref SetMusicLibraryIDsFailed, ref SetMusicLibraryIDsTxtError);
        }

        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (SetMusicLibraryIDsASync_Complete != null)
                SetMusicLibraryIDsASync_Complete(this, new GenericEventArgs<bool>(!SetMusicLibraryIDsFailed));
        }
        #endregion

        #region Get Folders and Files from Library
        void TraverseLibrary(ref bool failed, ref string txtError)
        {
            IKnownFolder f = KnownFolders.MusicLibrary;

            List<string> output = new List<string>();
            bool needWriteAccess = (currentAction == MusicHelperActions.WriteIDs);
            using (ShellLibrary musicLibrary = ShellLibrary.Load(f, needWriteAccess))
            {
                foreach (ShellObject obj in musicLibrary)
                {
                    if (obj is ShellFileSystemFolder)
                        traverseFolder((ShellFileSystemFolder)obj, ref failed, ref txtError);
                }
            }

        }
        void traverseFolder(ShellFileSystemFolder folder, ref bool failed, ref string txtError)
        {
            foreach (ShellObject obj in folder)
            {
                if (obj is ShellFileSystemFolder)
                    traverseFolder((ShellFileSystemFolder)obj, ref failed, ref txtError);

                if (obj is ShellFile)
                    processFile((ShellFile)obj, ref failed, ref txtError);
            }
        }
        void processFile(ShellFile file, ref bool failed, ref string txtError)
        {
            // Show?
            if (file == null) return;
            if (!isExtensionValid(file.Path)) return;

            ShellProperties props = file.Properties;
            ShellProperties.PropertySystem psys = props.System;

            switch (currentAction)
            {
                case MusicHelperActions.RetrieveData:
                    if (retrievingCollection == RetrievableCollections.AllArtists)
                    {
                        string[] Artists = (psys.Music.Artist.ValueType == typeof(string[])) ? psys.Music.Artist.Value : new string[] { };
                        if (Artists == null) return;
                        if (Artists.Count() < 1) return;
                        string Artist = Artists[0];
                        if (string.IsNullOrWhiteSpace(Artist)) return;

                        RPMusicArtist newArtist = new RPMusicArtist(Artist);
                        AllArtistsList.Add(newArtist);                
                    }

                    if (retrievingCollection == RetrievableCollections.AllAlbums)
                    {
                        string AlbumTitle = (psys.Music.AlbumTitle.ValueType == typeof(string))  ? psys.Music.AlbumTitle.Value : "Unknown Album" ;
                        if (string.IsNullOrWhiteSpace(AlbumTitle)) AlbumTitle = "Unknown Album";
                        string AlbumArtist = (psys.Music.AlbumArtist.ValueType == typeof(string)) ? psys.Music.AlbumArtist.Value : "Unknown Artist";  //>("System.Music.Artist").Value;    System.Music.DisplayArtist").Value;
                        if (string.IsNullOrWhiteSpace(AlbumArtist)) AlbumArtist = "Unknown Artist";

                        string AlbumID = (psys.Music.AlbumID.ValueType == typeof(string)) ? psys.Music.AlbumID.Value : "";
                        if (string.IsNullOrWhiteSpace(AlbumID)) return;

                        string[] AlbumGenres = (psys.Music.Genre.ValueType == typeof(string[])) ? psys.Music.Genre.Value : new string[] { };
                        if (AlbumGenres == null) AlbumGenres = new string[] { };
                        string AlbumGenre = (AlbumGenres.Count() > 0) ? AlbumGenres[0] : "Blank";
                        if (string.IsNullOrWhiteSpace(AlbumGenre)) AlbumGenre = "Blank";

                        RPMusicAlbum newAlbum = new RPMusicAlbum();
                        newAlbum.ArtistID = AlbumArtist;
                        newAlbum.Title = AlbumTitle;
                        newAlbum.GenreID = AlbumGenre;
                        newAlbum.ID = AlbumID;

                        // Only add if not found, otherwise list grows to same length as all songs in library
                        if (AllAlbumsList != null) // safety test
                        if (! AllAlbumsList.Contains(newAlbum))
                            AllAlbumsList.Add(newAlbum);
                    }
                    if (retrievingCollection == RetrievableCollections.AllGenres)
                    {
                        string[] AlbumGenres = (psys.Music.Genre.ValueType == typeof(string[])) ? psys.Music.Genre.Value : new string[] { };
                        string AlbumGenre = (AlbumGenres.Count() > 0) ? AlbumGenres[0] : "Blank";
                        if (string.IsNullOrWhiteSpace(AlbumGenre)) return;

                        RPMusicGenre newGenre = new RPMusicGenre(AlbumGenre);
                        if (! AllGenresList.Contains(newGenre))
                            AllGenresList.Add(newGenre);
                    }

                    if (retrievingCollection == RetrievableCollections.AllSongs)
                    {
                        // ID
                        string ID = "";
                        int failCounter = 0;
                        while (!TryGetRPIDForFile(file, ref ID))
                        {
                            Debug.Print("No RPID found in file " + file + " - adding RPID.");
                            AddOrReplaceRPIDInFile(file);
                            failCounter++;
                            if (failCounter > 5) 
                            {
                                failed = true;
                                txtError = "Could not set RPID in file " + file.Path;
                                return;
                            }
                        }
                        
                        if (String.IsNullOrWhiteSpace(ID))
                        {
                            failed = true;
                            txtError = "No Unique ID property was found, then could be set, for the file at " + file.Path;
                            return;
                        }
                        // Title
                        string Title = (psys.Title.ValueType == typeof(string)) ? psys.Title.Value : "Untitled Song";
                        if (String.IsNullOrWhiteSpace(Title)) Title = "Untitled Song";
                        // Artist
                        string Artist = (psys.Music.AlbumArtist.ValueType == typeof(string)) ? psys.Music.DisplayArtist.Value : "Unknown Artist";
                        if (String.IsNullOrWhiteSpace(Artist)) Artist = "Unknown Artist";
                        // Track Number
                        uint? TrackNumber = (psys.Music.TrackNumber.ValueType == typeof(uint?)) ? psys.Music.TrackNumber.Value : 0;
                        // Duration
                        ulong? Duration = (psys.Media.Duration.ValueType == typeof(ulong?)) ? psys.Media.Duration.Value : 0;
                        // File Extension
                        string FileExtension = (psys.FileExtension.ValueType == typeof(string)) ? psys.FileExtension.Value : "";
                        if (String.IsNullOrWhiteSpace(FileExtension)) FileExtension = "";
                        // File Size Bytes
                        ulong? FileSize = (psys.Size.ValueType == typeof(ulong?)) ? psys.Size.Value : 0;
                        // User Rating
                        uint? SimpleRating = (psys.SimpleRating.ValueType == typeof(uint?)) ? psys.SimpleRating.Value : 0;

                        RPMusicSong newSong = new RPMusicSong();
                        newSong.Title = Title;
                        newSong.ArtistID = Artist;
                        newSong.TrackNumber = TrackNumber.HasValue ? (int)TrackNumber.Value : 0;
                        newSong.Duration = Duration.HasValue ? (long)Duration.Value : 0;
                        newSong.FileExtension = FileExtension;
                        newSong.FileSizeBytes = FileSize.HasValue ?  (long)FileSize.Value : 0;
                        newSong.UserRating =  SimpleRating.HasValue ? (int)SimpleRating.Value : 0;                 
                        //newSong.ID = new Random().Next().ToString();
                        newSong.ID = ID;

                        // Only add if not found
                        if (!AllSongsList.Contains(newSong))
                            AllSongsList.Add(newSong);
                    }
                    break;


                case MusicHelperActions.WriteIDs:
                    AddOrReplaceRPIDInFile(file);

                    musicFileCounter++;
                    if (SetMusicLibraryIDProgressReport != null)
                        SetMusicLibraryIDProgressReport(this, new GenericEventArgs<int>(musicFileCounter));
                    break;
            }
        }
        

        bool TryGetRPIDForFile(ShellFile file, ref string ID)
        {
            string wholeID = (file.Properties.System.Title.ValueType == typeof(string)) ? file.Properties.System.Media.UniqueFileIdentifier.Value : "";

            // No UID string
            if (string.IsNullOrWhiteSpace(wholeID)) return false;

            // RPID not present
            if (!wholeID.Contains("RPID=")) return false;

            string[] allIDpairs = wholeID.Split(new char[] { ';' });
            foreach (string IDpair in allIDpairs)
            {
                if (IDpair.StartsWith("RPID="))
                {
                    try
                    {
                        ID = IDpair.Substring(5);
                        return true;
                    }
                    catch { }
                }
            }

            // RPID was blank
            return false;
        }
        void AddOrReplaceRPIDInFile(ShellFile file)
        {
            string wholeID = (file.Properties.System.Title.ValueType == typeof(string)) ? file.Properties.System.Media.UniqueFileIdentifier.Value : "";
            if (string.IsNullOrWhiteSpace(wholeID)) wholeID = "";

            // Remove any existing RPID
            if (wholeID.Contains("RPID="))
                wholeID = stripRPIDFromUID(wholeID);

            // Remove any trailing semicolon
            wholeID = removeTrailingSemicolon(wholeID);

            // Generate new ID
            string strRPID = System.Guid.NewGuid().ToString();

            if (wholeID.Length > 0)
                wholeID += ";";
            wholeID += "RPID=" + strRPID;

            Debug.Print("Setting whole ID to " + wholeID);
            file.Properties.System.Media.UniqueFileIdentifier.Value = wholeID;
        }
        string stripRPIDFromUID(string wholeID)
        {
            if (string.IsNullOrWhiteSpace(wholeID)) return "";

            if (!wholeID.Contains("RPID=")) return wholeID;

            string[] allIDpairs = wholeID.Split(new char[] { ';' });
            StringBuilder sbOutput = new StringBuilder();

            foreach (string IDpair in allIDpairs)
            {
                if (!IDpair.StartsWith("RPID="))
                    sbOutput.Append(IDpair + ";");
            }

            string newUIDString = (sbOutput.Length > 0) ? sbOutput.ToString() : "";

            newUIDString = removeTrailingSemicolon(newUIDString);

            return newUIDString;
        }
        string removeTrailingSemicolon(string inputString)
        {
            if (string.IsNullOrWhiteSpace(inputString)) return "";
            if (!inputString.EndsWith(";")) return inputString;
            
            return inputString.Substring(0, inputString.Length - 1);
        }
        void writeUIDStringToFile(ShellFile file, string newValue)
        {
            file.Properties.System.Media.UniqueFileIdentifier.Value = newValue;
        }
        #endregion

        bool isExtensionValid(string strPath)
        {
            try
            {
                return String.Equals(strPath.Substring(strPath.Length - 3), "mp3") ;
            }
            catch 
            {
                return false;
            }
        }


        */

    }
}
