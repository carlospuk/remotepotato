using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Threading;
using System.Timers;
using System.Text;
using System.Runtime.InteropServices;
using System.ComponentModel;
using CommonEPG;
using RemotePotatoServer.Properties;
using Toub.MediaCenter.Dvrms.Metadata;

namespace RemotePotatoServer
{
    public sealed class RecTV
    {
        // Members
        const int MINUTES_BEFORE_ADDING_NEW_RECORDING = 1;  // wait 1 minute after file creation date before adding file
        const int IGNORE_FILES_IN_USE_MORE_THAN_THIS_MINUTES_OLD = 300;  // 5 hours

        public Dictionary<string, TVProgramme> RecordedTVProgrammes;
        List<FileSystemWatcher> FileWatchers;
        List<string> PostponedFiles;
        System.Timers.Timer tmPostponedFilesTimer;
        public event EventHandler<DebugReportEventArgs> DebugReport;
        bool HasInitialised;

        // Constructor
        private RecTV()
        {
            RecordedTVProgrammes = new Dictionary<string, TVProgramme>();
            FileWatchers = new List<FileSystemWatcher>();
            PostponedFiles = new List<string>();
        }
        ~RecTV()
        {
            tmPostponedFilesTimer.Stop();
        }
        object initLock = new object();
        public void Initialize()
        {
            Monitor.Enter(initLock);
            
            if (HasInitialised)
            {
                DebugNormal("Bailing out of Init - already initialised.");
                return;
            }

            DebugNormal("Initializing...");
            RefreshCache();  // fill cache with current programmes

            // Start up the file system watchers
            InitFileWatchers();

            // Start up the files checker
            InitPostponedFilesChecker();

            HasInitialised = true;
            DebugNormal("Initialized...");

            Monitor.Exit(initLock);
        }

        #region Postponed Files
        object initPostponedFiles = new object();
        object checkPostponedFiles = new object();
        void InitPostponedFilesChecker()
        {
            Monitor.Enter(initPostponedFiles);

            StopPostponedFilesChecker();
            //TimerCallback tcb = new TimerCallback(tmPostponedFilesTimer_Tick);
            //tmPostponedFilesTimer = new System.Threading.Timer(tcb, null, TimeSpan.FromMinutes(Settings.Default.RecTVRecheckPostponedFilesEvery), TimeSpan.FromMinutes(Settings.Default.RecTVRecheckPostponedFilesEvery));
            tmPostponedFilesTimer = new System.Timers.Timer(Settings.Default.RecTVRecheckPostponedFilesEvery * 60 * 1000);
            tmPostponedFilesTimer.AutoReset = true;
            tmPostponedFilesTimer.Elapsed += new ElapsedEventHandler(tmPostponedFilesTimer_Elapsed);
            tmPostponedFilesTimer.Start();

            Monitor.Exit(initPostponedFiles);
        }

        
        private void StopPostponedFilesChecker()
        {
            if (tmPostponedFilesTimer != null)
            {
                //tmPostponedFilesTimer.Stop();
                tmPostponedFilesTimer.Dispose();
                tmPostponedFilesTimer = null;
            }
        }
        void tmPostponedFilesTimer_Tick(Object stateObject)
        {
            CheckPostponedFiles();
        }
        void tmPostponedFilesTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            CheckPostponedFiles();
        }
        private void CheckPostponedFiles()
        {
            Monitor.Enter(checkPostponedFiles);
        
            DebugNormal("Checking if we postponed any files in use...");
            if (PostponedFiles.Count < 1)
            {
                Monitor.Exit(checkPostponedFiles);
                return; // no files
            }

            DebugNormal("Found " + PostponedFiles.Count.ToString() + " postponed files to re-check...");
            List<string> FilesToDelete = new List<string>();
            // Enumerate in reverse order in case of removals
            foreach (string f in PostponedFiles)
            {
                FileInfo fi = new FileInfo(f);

                DebugNormal("Checking " + fi.Name);
                AddTVProgrammeResults ATVresult = AddTVProgrammeIfPossible(fi);
                if (ATVresult == AddTVProgrammeResults.Success)
                {
                    // It wasn't in use, it's been added - so let's remove it
                    FilesToDelete.Add(f);
                    DebugNormal("Added file " + fi.Name + " and removed from postpone list.");
                }
                else
                {
                    DebugNormal("File " + fi.Name + " could not be added yet: " + ATVresult.ToString() );

                    // Is it waaaay old?  Remove if so
                    if (MinutesSinceFileWasCreated(fi) > IGNORE_FILES_IN_USE_MORE_THAN_THIS_MINUTES_OLD)
                    {
                        FilesToDelete.Add(f);
                        DebugNormal("File " + fi.Name + " is older than " + IGNORE_FILES_IN_USE_MORE_THAN_THIS_MINUTES_OLD.ToString() +  " minutes and still in use, abandoning attempts to add it to recorded TV list.");
                    }
                }
            }

            // Delete the files we've added
            foreach (string f in FilesToDelete)
            {
                PostponedFiles.Remove(f);
            }

            Monitor.Exit(checkPostponedFiles);
        }
        #endregion


        #region File Watchers

        void InitFileWatchers()
        {
            ClearAllWatchers();
            
            foreach (string recTVFolder in Settings.Default.RecordedTVFolders)
            {
                if (! Directory.Exists(recTVFolder)) continue;

                InitFileWatcher(recTVFolder);
            }
        }
        void InitFileWatcher(string strPath)
        {
            FileSystemWatcher fw = new FileSystemWatcher(strPath);
            fw.Filter = @"";
            //fw.NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size;
            // try without fw.Changed += new FileSystemEventHandler(fw_Changed);
            fw.Created += new FileSystemEventHandler(fw_Created);
            fw.Deleted += new FileSystemEventHandler(fw_Deleted);
            fw.Renamed += new RenamedEventHandler(fw_Renamed);
            fw.EnableRaisingEvents = true;

            // Recurse subdirectories ?
            if (Settings.Default.RecurseRecTVSubfolders)
                fw.IncludeSubdirectories = true;

            FileWatchers.Add(fw);
        }
        void ClearAllWatchers()
        {
            foreach (FileSystemWatcher fw in FileWatchers)
            {
                fw.EnableRaisingEvents = false;
            }

            FileWatchers.Clear();
        }

        // EVENTS *********
        /*void fw_Renamed(object sender, RenamedEventArgs e)
        {
            DebugNormal("Watcher: File renamed - " + e.FullPath);

            string ID = "";
            if (!TryGetProgrammeIdFromFilePath(e.OldFullPath, ref ID)) return;

            if (RecordedTVProgrammes.ContainsKey(ID))
            {
                RecordedTVProgrammes.Remove(ID);
                TVProgramme tvp = MakeTVProgrammeFromWtvFile(e.FullPath);
                RecordedTVProgrammes.Add(tvp.Id, tvp);
            }
                    
        }*/

        bool fileIsRecordedTVFile(string fileName)
        {
            if (fileName.EndsWith("wtv")) return true;
            if (fileName.EndsWith("dvr-ms")) return true;
            return false;
        }

        void fw_Deleted(object sender, FileSystemEventArgs e)
        {
            if (!fileIsRecordedTVFile(e.FullPath)) return;

            DebugNormal("Watcher: File deleted - " + e.FullPath);

            string ID = "";
            if (!TryGetProgrammeIdFromFilePath(e.FullPath, ref ID)) return;

            if (RecordedTVProgrammes.ContainsKey(ID))
                RecordedTVProgrammes.Remove(ID);
        }
        void fw_Created(object sender, FileSystemEventArgs e)
        {
            if (!fileIsRecordedTVFile(e.FullPath)) return;

            DebugNormal("Watcher: File created - " + e.FullPath);

            // If file is not in use, add it, otherwise postpone it
            FileInfo fi = new FileInfo(e.FullPath);
            AddTVProgrammeResults ATVresult = AddTVProgrammeIfPossible(fi);
            if (ATVresult != AddTVProgrammeResults.Success)
            {
                DebugNormal("Cannot add " + fi.Name + " yet:" + ATVresult.ToString() + " - postponing.");
                if (! PostponedFiles.Contains(fi.FullName))
                    PostponedFiles.Add(fi.FullName);
            }

        }
        void fw_Renamed(object sender, RenamedEventArgs e)
        {
            if (!fileIsRecordedTVFile(e.OldFullPath)) return;

            // Is this file in the DB (under its old filename)?  (if not, might be in 'filesinuse' awaiting release)
            string ID = "";
            if (!TryGetProgrammeIdFromFilePath(e.OldFullPath, ref ID)) return;

            // Yes, replace with newer name
            TVProgramme tvp = RecordedTVProgrammes[ID];
            RecordedTVProgrammes.Remove(ID);
            tvp.Filename = e.FullPath;
            RecordedTVProgrammes.Add(tvp.Id, tvp);
        }

        #endregion


        // Refresh Cache / Get Shows
        public bool RefreshCache()
        {
            DebugNormal("Refreshing Cache...");

            RecordedTVProgrammes.Clear();
            PostponedFiles.Clear();
            
            try
            {
                DebugNormal("Looking in " + Settings.Default.RecordedTVFolders.Count.ToString() + " recorded TV folders.");
                foreach (string recTVFolder in Settings.Default.RecordedTVFolders)
                {
                    DebugNormal("Looking in " + recTVFolder);
                    addFolder(recTVFolder);
                }
            }
            catch (Exception ex)
            {
                DebugError(ex);
                return false;
            }

            return true;
        }
        void addFolder(string recTVFolder)
        {
            DirectoryInfo di = new DirectoryInfo(recTVFolder);
            ArrayList files = new ArrayList();
            files.AddRange(di.GetFiles("*.wtv"));
            files.AddRange(di.GetFiles("*.dvr-ms"));
            IComparer dateComparer = new DateComparer();
            files.Sort(dateComparer);

            foreach (FileInfo fi in files)
            {

                try
                {
                    // If file is not in use, add it, otherwise postpone it
                    AddTVProgrammeResults ATVresult = AddTVProgrammeIfPossible(fi);
                    if (ATVresult != AddTVProgrammeResults.Success)
                    {
                        DebugNormal("Cannot add " + fi.Name + " yet, file in use - postponing.");

                        if (!PostponedFiles.Contains(fi.FullName))
                            PostponedFiles.Add(fi.FullName);
                    }
                    else
                        DebugNormal("Added " + fi.Name);
                }
                catch (Exception ex)
                {
                    DebugError(ex);
                }

            }

            if (Settings.Default.RecurseRecTVSubfolders)
            {
                foreach (DirectoryInfo diDir in di.GetDirectories())
                {
                    FileAttributes att = diDir.Attributes;

                    if ((att & FileAttributes.Hidden) == FileAttributes.Hidden) continue;
                    if ((att & FileAttributes.System) == FileAttributes.System) continue;

                    addFolder(diDir.FullName);
                }
            }
        }

        public enum AddTVProgrammeResults { FileTooNew, FileInUse, Success, Unknown };
        AddTVProgrammeResults AddTVProgrammeIfPossible(FileInfo fi)
        {
            // Not if it's only a minute since it was made
            if (MinutesSinceFileWasCreated(fi) < MINUTES_BEFORE_ADDING_NEW_RECORDING) return AddTVProgrammeResults.FileTooNew;

            // Is file unavailable for read access
            if (CanFileBeRead(fi))
                return AddTVProgrammeResults.FileInUse;
            else
            {
                AddTVProgrammeUsingFile(fi);
                return AddTVProgrammeResults.Success;
            }

            
        }
        double MinutesSinceFileWasCreated(FileInfo fi)
        {
            TimeSpan timeSinceCreation = (DateTime.Now.Subtract(fi.CreationTime));
            return timeSinceCreation.TotalMinutes;
        }
        void AddTVProgrammeUsingFile(FileInfo fi)
        {
            TVProgramme tvprog;
            tvprog = TVProgrammeFromWtvFile(fi.FullName);
            if (tvprog != null)
            {
                // If it exists, delete it - we'll add the newer version in
                if (RecordedTVProgrammes.ContainsKey(tvprog.Id))
                    RecordedTVProgrammes.Remove(tvprog.Id);

                RecordedTVProgrammes.Add(tvprog.Id, tvprog);
            }
            else
            {
                DebugError("Couldn't add recorded TV show: " + fi.FullName);
            }
        }
        // Helper
        TVProgramme TVProgrammeFromWtvFile(string filename)
        {
            TVProgramme tvp = new TVProgramme();

            tvp.isGeneratedFromFile = true;

            DvrmsMetadataEditor MetaEd = new DvrmsMetadataEditor(filename);
            try
            {
                tvp.Filename = filename;

                Dictionary<string, MetadataItem> attributes = new Dictionary<string, MetadataItem>();
                if (!MetaEd.GetMetaData(ref attributes)) return null;

                // Title
                if (attributes.ContainsKey("Title"))
                {
                    MetadataItem Mtitle = attributes["Title"];
                    tvp.Title = (string)Mtitle.Value;
                }
                else
                    tvp.Title = "Untitled Show";

                /* if (attributes.ContainsKey("WM/WMRVProgramID"))
                {
                    // Use the file ID as it will persist
                    MetadataItem Mid = attributes["WM/WMRVProgramID"];
                    string strID = (string)Mid.Value;
                    // Strip any !! bit
                    int locExcl = strID.LastIndexOf("!");
                    if (locExcl > 0)
                        tvp.Id = strID.Substring(locExcl + 1);
                    else
                        tvp.Id = strID;
                }
                else */ // THIS WAS CAUSING SD AND HD SHOWS TO MERGE
                {
                    // Make up an ID
                    Random r = new Random();
                    int iRan = r.Next(100000000, 900000000);

                    string addendum = "";
                    if ((tvp.Title != null) && (tvp.Title.Length > 4))
                        addendum += tvp.Title.Substring(0, 3);

                    tvp.Id = iRan.ToString() + addendum;
                }

                // Episode Title
                if (attributes.ContainsKey("WM/SubTitle"))
                {
                    MetadataItem Msubtitle = attributes["WM/SubTitle"];
                    if (Msubtitle.Value != null)
                        tvp.EpisodeTitle = (string)Msubtitle.Value;
                }
                else
                    tvp.EpisodeTitle = "";

                // Description
                if (attributes.ContainsKey("WM/SubTitleDescription"))
                {
                    MetadataItem Mdesc = attributes["WM/SubTitleDescription"];
                    if (Mdesc.Value != null)
                        tvp.Description = (string)Mdesc.Value;
                }

                if (attributes.ContainsKey("WM/MediaIsSport"))
                {
                    MetadataItem Msport = attributes["WM/MediaIsSport"];
                    if (Msport.Value != null)
                    {
                        bool isSport = (bool)Msport.Value;
                        if (isSport) tvp.ProgramType = TVProgrammeType.Sport;
                    }
                }

                if (attributes.ContainsKey("WM/MediaIsMovie"))
                {
                    MetadataItem MisMovie = attributes["WM/MediaIsMovie"];
                    if (MisMovie.Value != null)
                    {
                        bool isMovie = (bool)MisMovie.Value;
                        if (isMovie) tvp.ProgramType = TVProgrammeType.Movie;
                    }
                }



                if (attributes.ContainsKey("WM/WMRVEncodeTime"))
                {
                    MetadataItem Mdate = attributes["WM/WMRVEncodeTime"];
                    if (Mdate.Value != null)
                    {
                        try
                        {
                            long tickTime = (long)Mdate.Value;
                            DateTime TheStartTime = new DateTime(tickTime, DateTimeKind.Utc);
                            tvp.StartTime = TheStartTime.Ticks;
                        }
                        catch (Exception ex)
                        {
                            Functions.WriteLineToLogFile("Error setting start time from WTV metadata for file " + filename);
                            Functions.WriteExceptionToLogFile(ex);
                        }
                    }
                }
                else
                {
                    Functions.WriteLineToLogFile("No start time in WTV metadata for file " + filename + " - using current time.");
                    tvp.StartTime = DateTime.Now.ToUniversalTime().Ticks;
                }


                if (attributes.ContainsKey("WM/WMRVEndTime"))
                {
                    MetadataItem Menddate = attributes["WM/WMRVEndTime"];
                    if (Menddate.Value != null)
                    {
                        long tickTime = (long)Menddate.Value;
                        DateTime TheEndTime = new DateTime(tickTime, DateTimeKind.Utc);
                        tvp.StopTime = TheEndTime.Ticks;
                    }
                }
                else
                {
                    Functions.WriteLineToLogFile("No end time in WTV metadata for file " + filename + " - using current time plus 5 seconds.");
                    tvp.StopTime = DateTime.Now.AddMinutes(5).ToUniversalTime().Ticks;
                }

                if (attributes.ContainsKey("WM/WMRVContentProtectedPercent"))
                {
                    MetadataItem Mprotected = attributes["WM/WMRVContentProtectedPercent"];
                    if (Mprotected.Value != null)
                    {
                        int percentProtected = (int)Mprotected.Value;
                        if (percentProtected > 0) tvp.IsDRMProtected = true;
                    }
                }


                if (attributes.ContainsKey("WM/MediaStationCallSign"))
                {
                    MetadataItem Mchannel = attributes["WM/MediaStationCallSign"];
                    if (Mchannel.Value != null)
                    {
                        tvp.WTVCallsign = (string)Mchannel.Value;
                    }
                }

                // Only populate if we don't already have a callsign
                if (string.IsNullOrEmpty(tvp.WTVCallsign))
                {
                    if (attributes.ContainsKey("WM/MediaStationName"))  // Added by request: also use station name
                    {
                        MetadataItem Mchanname = attributes["WM/MediaStationName"];
                        if (Mchanname.Value != null)
                            tvp.WTVCallsign = (string)Mchanname.Value;

                    }
                    else
                    {
                        tvp.WTVCallsign = "Unknown channel.";
                    }
                }

                if (attributes.ContainsKey("WM/WMRVSeriesUID"))
                {
                    MetadataItem MSeriesUID = attributes["WM/WMRVSeriesUID"];
                    if (MSeriesUID.Value != null)
                        tvp.IsSeries = !String.IsNullOrEmpty((string)MSeriesUID.Value);
                }


                if (attributes.ContainsKey("WM/WMRVHDContent"))
                {
                    MetadataItem MisHD = attributes["WM/WMRVHDContent"];
                    if (MisHD.Value != null)
                        tvp.IsHD = (bool)MisHD.Value;
                }

                if (attributes.ContainsKey("WM/MediaIsSubtitled"))
                {
                    MetadataItem MisSubT = attributes["WM/MediaIsSubtitled"];
                    if (MisSubT.Value != null)
                        tvp.HasSubtitles = (bool)MisSubT.Value;
                }

                // Original date
                if (attributes.ContainsKey("WM/MediaOriginalBroadcastDateTime"))
                {
                    MetadataItem Morigdate = attributes["WM/MediaOriginalBroadcastDateTime"];
                    if (Morigdate.Value != null)
                    {
                        string strOrigDate = (string)Morigdate.Value;
                        if (!string.IsNullOrWhiteSpace(strOrigDate))
                        {
                            DateTime dtOrigDate;
                            if (DateTime.TryParse(strOrigDate, out dtOrigDate))
                            {
                                tvp.OriginalAirDate = dtOrigDate.ToUniversalTime().Ticks;
                            }
                            else
                            {
                                if (Settings.Default.DebugAdvanced)
                                    Functions.WriteLineToLogFile("Couldn't parse WTV original broadcast date for " + filename + ": [" + strOrigDate + "]");
                            }
                        }
                    }
                }


                if (attributes.ContainsKey("WM/MediaIsRepeat"))
                {
                    MetadataItem MisRepeat = attributes["WM/MediaIsRepeat"];
                    if (MisRepeat.Value != null)
                    {
                        bool isRepeat = (bool)MisRepeat.Value;
                        tvp.IsFirstShowing = !isRepeat;
                    }
                }


            }
            catch (Exception ex)
            {
                if (Settings.Default.DebugAdvanced)
                {
                    Functions.WriteLineToLogFile("Couldn't get WTV metadata for " + filename + ":");
                    Functions.WriteExceptionToLogFile(ex);
                }

                return null;
            }
            finally
            {
                MetaEd.ReleaseResources();
                MetaEd = null;
            }

            return tvp;
        }
        bool TryGetProgrammeIdFromFilePath(string filePath, ref string ID)
        {
            foreach (TVProgramme tvp in RecordedTVProgrammes.Values)
            {
                if (tvp.Filename.Equals(filePath))
                {
                    ID = tvp.Id;
                    return true;
                }
            }

            return false;
        }

        bool CanFileBeRead(FileInfo file)
        {
            Stream stream = null;
            try
            {
                using (stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
                { }
                return false;
            }
            catch (IOException e)
            {
                if (!ExceptionForLockedFile(e))
                    return true;
                else
                    return false;
            }
            catch
            {
                // Assume not locked
                return false;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }
            /*http://stackoverflow.com/questions/876473/c-is-there-a-way-to-check-if-a-file-is-in-use

                        FileStream stream = null;

                        try
                        {
                            stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
                        }
                        catch (IOException)
                        {
                            //the file is unavailable because it is:
                            //still being written to
                            //or being processed by another thread
                            //or does not exist (has already been processed)
                            return true;
                        }
                        finally
                        {
                            if (stream != null)
                                stream.Close();
                        }

                        //file is not locked
                        return false;
             */
        }
        private static bool ExceptionForLockedFile(IOException exception)
        {
            int errorCode = Marshal.GetHRForException(exception) & ((1 << 16) - 1);
            return errorCode == 32 || errorCode == 33;
        }


        // Debug
        void DebugNormal(string msg)
        {
            if (DebugReport != null)
                DebugReport(new object(), new DebugReportEventArgs("RecTV: " + msg, 10, null));
        }
        void DebugError(string msg)
        {
            if (DebugReport != null)
                DebugReport(new object(), new DebugReportEventArgs("RecTV: " + msg, 50, null));
        }
        void DebugError(Exception ex)
        {
            if (DebugReport != null)
                DebugReport(new object(), new DebugReportEventArgs("RecTV: Error - " + ex.Message, 50, ex));
        }



        #region Comparers
        class SizeComparer : IComparer
        {
            public int Compare(object info1, object info2)
            {
                FileInfo fileInfo1 = info1 as FileInfo;
                FileInfo fileInfo2 = info2 as FileInfo;
                long fileSize1 = fileInfo1 == null ? -1 : fileInfo1.Length;
                long fileSize2 = fileInfo2 == null ? -1 : fileInfo2.Length;
                if (fileSize1 > fileSize2) return 1;
                if (fileSize1 < fileSize2) return -1;
                return 0;
            }
        }
        class DateComparer : IComparer
        {
            public int Compare(object info1, object info2)
            {
                FileInfo fileInfo1 = info1 as FileInfo;
                FileInfo fileInfo2 = info2 as FileInfo;

                DateTime fileDate1 = (fileInfo1 == null) ? new DateTime(0) : fileInfo1.CreationTimeUtc;
                DateTime fileDate2 = (fileInfo2 == null) ? new DateTime(0) : fileInfo2.CreationTimeUtc;

                if (fileDate1 < fileDate2) return 1;
                if (fileDate1 > fileDate2) return -1;
                return 0;
            }
        }
        #endregion


        #region Singleton Methods
        static RecTV instance = null;
        static readonly object padlock = new object();
        internal static RecTV Default
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new RecTV();
                    }
                    return instance;
                }
            }
        }
        #endregion

       

    }
}
