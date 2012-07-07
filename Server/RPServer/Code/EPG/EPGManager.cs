using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Text;
using System.IO;
using System.Web;
using RemotePotatoServer.Properties;
using CommonEPG;
using CommonEPG.Comparers;

namespace RemotePotatoServer
{
    public static class EPGManager
    {
        // Private Members.
        public static bool isWMCOnline = false;
        internal static MCData mcData = null;
        public static Dictionary<string, TVService> AllTVChannels;
        public static int TimespanMinutes = 180;
        public static double EPGScaleFactor = 4;
        private const int DAYS_BACK_TO_GET_RECORDINGS = 3;  // was 60; not sure why

        // Constructor
        static EPGManager()
        {
            AllTVChannels = new Dictionary<string, CommonEPG.TVService>();
            CachedMovies = new SortedDictionary<string, TVMovie>();
            MovieCacheLastUpdated = DateTime.Now.AddDays(-7);

            AllRequests = new Dictionary<long, RPRequest>();
            AllRecordings = new Dictionary<long, RPRecording>();
        }

        // EVENTS I CAN RAISE
        public static event EventHandler EPGChannelsRepopulated;
        // These tasks are raised by the provider and passed up to the parent
        public static event EventHandler<CommonEPG.DebugReportEventArgs> EPGDebugReport;

        public static bool Initialise()
        {           
            mcData = new MCData();

            // Hook up debugging info
            mcData.DebugReport += new EventHandler<DebugReportEventArgs>(epg_DebugReport);
            mcData.RecordingsDataNeedsRefresh += new EventHandler(mcData_RecordingsDataNeedsRefresh);

            // Initialize MCData
            string key = "Wilkinson";
            isWMCOnline = mcData.Initialize(ref key);
            return isWMCOnline;       
        }
        

        #region Channels / Services
        // Wrapper around mcdata
        public static bool ExternalPopulateTVChannels(bool skipCache)
        {
            if (AllTVChannels == null) AllTVChannels = new Dictionary<string, TVService>();
            if (! Settings.Default.EnableMediaCenterSupport) return true;
            bool result;

            MCData tempMCData = null;
            try
            {
                tempMCData = new MCData();

                // Hook up debugging info
                tempMCData.DebugReport += new EventHandler<DebugReportEventArgs>(tempMCData_DebugReport);

                // Initialize MCData
                string key = "Wilkinson";
                isWMCOnline = tempMCData.Initialize(ref key);
                if (!isWMCOnline)
                    return false;

                result = PopulateTVChannels(skipCache, tempMCData);
            }
            finally
            {
                if (tempMCData != null)
                {
                    tempMCData.DebugReport -= new EventHandler<DebugReportEventArgs>(tempMCData_DebugReport);

                    tempMCData.FreeResources();
                    tempMCData.Dispose();
                }
            }

            return result;
        }

        internal static bool PopulateTVChannels(bool skipCache)
        {
            return PopulateTVChannels(skipCache, mcData);
        }
        private static bool PopulateTVChannels(bool skipCache, MCData useMCData)
        {
            if (!isWMCOnline) return false;
            if (useMCData == null) return false;

            AllTVChannels = null;

            // Get from stored local channels if they exist
            bool getChannelsFromMediaCenter = true;
            if ((SavedChannelsExist) && (!skipCache))
            {
                AllTVChannels = ChannelsFromLocalStore();  // Try to get channels from local storage

                // If no channels were loaded, then get them from Media Center
                getChannelsFromMediaCenter = (AllTVChannels.Count < 1);
            }

            if (getChannelsFromMediaCenter)
                GetChannelsFromMediaCenter(useMCData, true);

            // Fire event
            if (EPGChannelsRepopulated != null) EPGChannelsRepopulated(new object(), new EventArgs());
            
            return (AllTVChannels != null);
        }
        /// <summary>
        /// Get channels from Media Center and set first five as favorites
        /// </summary>
        /// <param name="useMCData"></param>
        private static void GetChannelsFromMediaCenter(MCData useMCData, bool autoSetFaves)
        {
            AllTVChannels = useMCData.GetChannels(Settings.Default.MergeLineUps,
                    Settings.Default.ImportInternetTVChannels,
                    Settings.Default.ImportHiddenTVChannels,
                    Settings.Default.BlockChannelsUserHidden,
                    Settings.Default.BlockChannelsUserAdded,
                    Settings.Default.BlockChannelsUserMapped,
                    Settings.Default.BlockChannelsUnknown,
                    Settings.Default.DebugChannels);

            // By default, set the first five channels as favourites
            if (autoSetFaves)
            {
                int fCounter = 5;
                foreach (TVService tvs in AllTVChannels.Values)
                {
                    tvs.IsFavorite = true;
                    if (--fCounter < 0) break;
                }
            }

            SaveChannelsToLocal();
        }

        // Refresh
        public static bool UpdateTVChannels()
        {
            bool debug = ((Settings.Default.DebugChannels) & (Settings.Default.DebugAdvanced));
            Functions.WriteLineToLogFileIfSetting(debug, "EPG: UpdateTVChannels() Running:");

            // If we don't have any existing channels at all then get some
            if ((AllTVChannels == null) || (AllTVChannels.Count < 1))
            {
                Functions.WriteLineToLogFileIfSetting(debug, "EPG: UpdateTVChannels(): No existing TV Channels, populating from cache first...");
                if (!PopulateTVChannels(false)) return false;

                Functions.WriteLineToLogFileIfSetting(debug, "EPG: UpdateTVChannels(): Populated from cache: there are " + AllTVChannels.Count.ToString() + " channels.");
                if (AllTVChannels.Count < 1) return false;
            }

            Functions.WriteLineToLogFileIfSetting(debug, "EPG: UpdateTVChannels(): Backing up " + AllTVChannels.Count.ToString() + " channels.");
            Dictionary<string, TVService> OldTVChannelsKeyedByChannelNumberString = new Dictionary<string, TVService>();
            foreach (TVService tvs in AllTVChannels.Values)
            {
                TVService copiedOldService = tvs.DeepCopy();

                // Skip duplicate keys otherwise you spend 4 hours on a Sunday trying to remotely debug a native code crash
                if (OldTVChannelsKeyedByChannelNumberString.ContainsKey(copiedOldService.ChannelNumberString()))
                    Functions.WriteLineToLogFileIfSetting(debug, "EPG: UpdateTVChannels() Skipping service with duplicate channel num string (" + copiedOldService.ChannelNumberString() + ") Callsign is " + copiedOldService.Callsign );
                else
                {
                    Functions.WriteLineToLogFileIfSetting(debug, "EPG: UpdateTVChannels() Storing " + copiedOldService.ChannelNumberString() +  " : " + copiedOldService.Callsign );
                    OldTVChannelsKeyedByChannelNumberString.Add(copiedOldService.ChannelNumberString() , copiedOldService);
                }
            }

            // Re-populate AllTVChannels from MediaCenter
            Functions.WriteLineToLogFileIfSetting(debug, "EPG: UpdateTVChannels(): Getting newest channels from 7MC...");
            PopulateTVChannels(true);
            Functions.WriteLineToLogFileIfSetting(debug, "EPG: UpdateTVChannels(): Populated from 7MC: there are " + AllTVChannels.Count.ToString() + " channels.");

            // Go through new channels - retrieve isFavourite information and user sort order from old channels
            Functions.WriteLineToLogFileIfSetting(debug, "EPG: UpdateTVChannels(): Merging info from old channels into newer list.");
            List<string> MatchedChannelNumberStrings = new List<string>();
            foreach (TVService newService in AllTVChannels.Values)
            {
                if (OldTVChannelsKeyedByChannelNumberString.ContainsKey(newService.ChannelNumberString()))
                {
                    if (!MatchedChannelNumberStrings.Contains(newService.ChannelNumberString()))
                    {
                        TVService oldService = OldTVChannelsKeyedByChannelNumberString[newService.ChannelNumberString()];

                        // Merge in old user sort and isfavorite information
                        newService.IsFavorite = oldService.IsFavorite;
                        newService.UserSortOrder = oldService.UserSortOrder;

                        if (newService.MCChannelID != oldService.MCChannelID) Functions.WriteLineToLogFile("EPG: UpdateTVChannels(): INFORMATION: Service " + newService.Callsign + " changed MC-ID from " + oldService.MCChannelID.ToString() + " to " + newService.MCChannelID.ToString() + " - merging in change.");
                        if (newService.UniqueId != oldService.UniqueId) Functions.WriteLineToLogFile("EPG: UpdateTVChannels(): INFORMATION: Service " + newService.Callsign + " changed Unique Service ID from " + oldService.UniqueId + " to " + newService.UniqueId + " - merging in change.");

                        MatchedChannelNumberStrings.Add(newService.ChannelNumberString());
                    }
                }
                else
                {
                    Functions.WriteLineToLogFileIfSetting(debug, "EPG: UpdateTVChannels(): NEW channel " + newService.Callsign + " was found.  (Chan number " + newService.ChannelNumberString() + ")");
                }
            }

            Functions.WriteLineToLogFileIfSetting(debug, "EPG: UpdateTVChannels(): Merge complete.");

            // Save it
            Functions.WriteLineToLogFileIfSetting(debug, "EPG: UpdateTVChannels(): Saving.");
            SaveChannelsToLocal();

            Functions.WriteLineToLogFileIfSetting(debug, "EPG: UpdateTVChannels(): DONE.");
            return true;
        }

        // Methods
        public static TVService TVServiceWithCallsignOrNull(string callsign)
        {
            foreach (CommonEPG.TVService tvc in AllTVChannels.Values)
            {
                if (tvc.Callsign.ToLowerInvariant() == callsign.ToLowerInvariant())
                    return tvc;
            }

            return null;
        }
        public static TVService TVServiceWithIDOrNull(string serviceId)
        {
            if (AllTVChannels.ContainsKey(serviceId))
                return AllTVChannels[serviceId];
            
            return null;
        }
        public static string CallsignFromServiceID(string svcID)
        {
            TVService tvs = TVServiceWithIDOrNull(svcID);
            if (tvs == null)
                return "Unknown";

            if (!string.IsNullOrEmpty(tvs.Callsign))
                return tvs.Callsign;
            else
                return "None";
        }
        public static string ServiceIDFromCallsign(string callsign)
        {
            TVService tvs = TVServiceWithCallsignOrNull(callsign);
            if (tvs == null)
                return "";

            if (!string.IsNullOrEmpty(tvs.UniqueId))
                return tvs.UniqueId;
            else
                return "";
        }

        // Property Methods
        public static List<TVService> FavoriteTVChannels
        {
            get
            {
                List<CommonEPG.TVService> output = new List<CommonEPG.TVService>();
                if (AllTVChannels == null) return output;
                foreach (TVService tvc in AllTVChannels.Values)
                {
                    if (tvc.IsFavorite)
                        output.Add(tvc);
                }

                return output;
            }
        }
        public static List<TVService> EPGDisplayedTVChannels  // depends on settings - favorites or all.  If no faves selected, returns all.
        {
            get
            {
                if (Settings.Default.ShowFavouriteChannelsInEPG)
                {
                    if (FavoriteTVChannels.Count > 0)
                        return FavoriteTVChannels;
                }

                return AllTVChannels.Values.ToList();
            }
        }
        public static List<string> EPGDisplayedTVChannelsServiceIDs
        {
            get
            {
                List<string> ServiceIDs = new List<string>();
                foreach (TVService tvs in EPGDisplayedTVChannels)
                {
                    ServiceIDs.Add(tvs.UniqueId);
                }

                return ServiceIDs;
            }
        }
       
        #endregion

        #region Favourite Channels
        public static bool MakeChannelFavorite(string serviceID)
        {
            TVService tvs = TVServiceWithIDOrNull(serviceID);
            if (tvs == null) return false;

            tvs.IsFavorite = true;
            SaveChannelsToLocal();
            return true;
        }
        public static bool MakeChannelNotFavorite(string serviceID)
        {
            TVService tvs = TVServiceWithIDOrNull(serviceID);
            if (tvs == null) return false;

            tvs.IsFavorite = false;
            SaveChannelsToLocal();
            return true;
        }
        public static void SendFavoriteChannelsToMediaCenter()
        {
            List<TVService> faves = FavoriteTVChannels;
            if (faves.Count < 1) return;

            mcData.RemoveAllServicesFromRemotePotatoLineUp();

            System.Threading.Thread.Sleep(500);

            mcData.AddServicesToRemotePotatoLineUp(faves);
        }
        #endregion

        #region Search

        public static List<TVProgramme> SearchTVProgrammes(string searchText, EPGSearchTextType searchTextType, EPGSearchMatchType searchMatchType, out bool resultsWereTruncated)
        {
            return mcData.SearchTVProgrammes(searchText, searchTextType, searchMatchType, out resultsWereTruncated, AllTVChannels.Keys.ToArray());
        }
        #endregion

        #region Logos
        public static string HTMLForLogoImageOrCallsign(this TVService tvs, string cssClass, bool showTextIfNoLogo)
        {
            if (tvs == null) return "";
            if (String.IsNullOrEmpty(tvs.Callsign)) return "";
            

            string altText = showTextIfNoLogo ? tvs.Callsign : "";

            if (!Settings.Default.ShowChannelLogos)
                return tvs.Callsign;


            if (!String.IsNullOrEmpty(tvs.LogoUri))
            {
                // There's a logo, return it
                string strHTML = "<img alt=\"" + altText + "\" title=\"" + altText + "\" src=\"/logo/" + HttpUtility.UrlEncode(tvs.UniqueId) + "\" ";
                if (!(string.IsNullOrEmpty(cssClass)))
                    strHTML += "class = \"" + cssClass + "\"";
                strHTML += "/>";
                return strHTML;
            }
            else
            {
                // No logo - just text if allowed
                return altText; ;
            }

            
        }
        public static bool LogoForServiceExists(string svcID, out string logoFN)
        {
            logoFN = "";

            TVService tvs = TVServiceWithIDOrNull(svcID);
            if (tvs == null)
                return false;

            if (string.IsNullOrEmpty(tvs.LogoUri))
                return false;

            logoFN = FileFNFromUri(tvs.LogoUri);
            return true;
        }
        public static bool GetLogoDataForCallsign(string svcID, int width, int height, out byte[] logoData)
        {
            logoData = new byte[] { };
            TVService tvs = TVServiceWithIDOrNull(svcID);
            if (tvs == null)
                return false;

            if (tvs.LogoUri == null)
                return false;

            string fileFN = FileFNFromUri(tvs.LogoUri);

            byte[] inLogoData = FileCache.ReadBinaryFile(fileFN);

            System.Drawing.Size sz = new System.Drawing.Size(width, height);
            return (ImageResizer.ResizePicture(inLogoData, sz, out logoData, true));
        }
        public static string FileFNFromUri(string fileUri)
        {
            return fileUri.Replace("file://", "");
        }
        #endregion

        #region Local Channel Store
        public static Dictionary<string, TVService> ChannelsFromLocalStore()
        {
            if (!SavedChannelsExist) return new Dictionary<string, TVService>();

            string xmlChannels = FileCache.ReadTextFileFromDisk(SavedChannelsFNFullPath);
            List<TVService> channels = EPGImporter.ChannelListFromString(xmlChannels);

            Dictionary<string, TVService> dChannels = new Dictionary<string, TVService>();
            foreach (TVService tvc in channels)
            {
                dChannels.Add(tvc.UniqueId, tvc);
            }
            return dChannels;
        }
        public static void OverwriteAllChannelsWithNewList(List<TVService> newChannelList)
        {
            AllTVChannels.Clear();

            foreach (TVService tvc in newChannelList)
            {
                AllTVChannels.Add(tvc.UniqueId, tvc);
            }
        }
        public static bool SaveChannelsToLocal()
        {
            DeleteSavedChannels();
            string xmlChannels = EPGExporter.AllChannelsAsXML();
            return FileCache.WriteTextFileToDisk(SavedChannelsFNFullPath, xmlChannels);
        }
        public static bool SavedChannelsExist
        {
            get
            {
                return File.Exists(SavedChannelsFNFullPath);
            }
        }
        public static void DeleteSavedChannels()
        {
            if (SavedChannelsExist)
            {
                FileInfo f = new FileInfo(SavedChannelsFNFullPath);
                f.Delete();
            }
        }
        public static string SavedChannelsFNFullPath
        {
            get
            {
                return Functions.AppDataFolder + "\\" + "channelsV2.xml";
            }
        }
        #endregion

        #region Movies
        public static SortedDictionary<string, TVMovie> CachedMovies;

        public static List<TVProgramme> GetTVMovies(DateRange dateRange)  // actually all progs
        {
            return mcData.GetTVProgrammes(dateRange, AllTVChannels.Keys.ToArray(), false, TVProgrammeType.Movie);
        }
        
        private static DateTime MovieCacheLastUpdated;
        public static TVMovie GetCachedMovieByID(int Id)
        {
            foreach (TVMovie tvm in CachedMovies.Values)
            {
                if (tvm.Id == Id)
                    return tvm;
            }
            return null;
        }
        public static void CacheMoviesIfExpired(List<TVProgramme>progList, bool restrictToFavouriteChannels)
        {
            TimeSpan elapsed = (DateTime.Now - MovieCacheLastUpdated);
            if (elapsed.TotalMinutes > 15)
                CacheMoviesNow(progList, restrictToFavouriteChannels);
        }
        public static void CacheMoviesNow(List<TVProgramme> progList, bool restrictToFavouriteChannels)
        {
            CachedMovies.Clear();

            foreach (TVProgramme tvp in progList)
            {
                if (
                    (tvp.ProgramType == TVProgrammeType.Movie) 
                    )
                {
                    if (restrictToFavouriteChannels)
                    {
                        TVService tvs = tvp.TVService();
                        if (tvs != null)
                            if (!FavoriteTVChannels.Contains(tvs))
                                continue;
                    }

                    AddOrMergeToMovieCache(tvp);
                }
            }

            if (CachedMovies.Count > 0)  // Don't refresh cache time if there's nothing in it
                MovieCacheLastUpdated = DateTime.Now;
        }
        private static void AddOrMergeToMovieCache(TVProgramme tvp)
        {
            if (CachedMovies.ContainsKey(tvp.Title))
            {
                MergeToMovieCache(tvp);
            }
            else
            {
                AddToMovieCache(tvp);
            }
        }
        private static void AddToMovieCache(TVProgramme tvp)
        {
            int newInt = CachedMovies.Count + 1;
            TVMovie tvm = new TVMovie(newInt, tvp);
            CachedMovies.Add(tvp.Title, tvm);
        }
        private static void MergeToMovieCache(TVProgramme tvp)
        {
            TVMovie existingMovie = CachedMovies[tvp.Title];
            existingMovie.Showings.Add(tvp);
        }
        #endregion

        #region Recordings
        public static Dictionary<long, RPRequest> AllRequests;
        public static Dictionary<long, RPRecording> AllRecordings;
        public static bool ReloadingRecordings = false;

        // Filters
        public static List<RPRecording> AllRecordingsOnDate(DateTime localDate)
        {
            List<RPRecording> output = new List<RPRecording>();
            foreach (RPRecording rec in AllRecordings.Values)
            {
                TVProgramme tvp = rec.TVProgramme();
                if (tvp == null)
                {
                    Functions.WriteLineToLogFile("Could not find a TV Programme for the recording: " + rec.Title);
                    continue;  
                }
                if (rec.TVProgramme().StartTimeDT().ToLocalTime().Date == localDate.Date)
                    output.Add(rec);
            }

            // Sort by start time ascending
            output.Sort(new RPRecordingStartTimeComparer());

            return output;
        }
        public static List<RPRecording> AllRecordingsTodayRemaining()
        {
            List<RPRecording> output = new List<RPRecording>();
            foreach (RPRecording rec in AllRecordings.Values)
            {
                TVProgramme tvp = rec.TVProgramme();
                if (tvp == null)
                {
                    Functions.WriteLineToLogFile("Could not find a TV Programme for the recording: " + rec.Title);
                    continue;  
                }

                if (
                    (tvp.StartTimeDT().ToLocalTime().Date == DateTime.Now.Date) &&
                    (tvp.StartTimeDT().ToLocalTime() > DateTime.Now)
                    )
                    output.Add(rec);
            }

            // Sort by start time ascending
            output.Sort(new RPRecordingStartTimeComparer());

            return output;
        }

        // Init
        public static bool ReloadAllRecordings()
        {
            return GetAllRecordingSchedule(DAYS_BACK_TO_GET_RECORDINGS, false);
        }
        private static bool GetAllRecordingSchedule(double daysBack, bool merge)
        {
            if (ReloadingRecordings) 
                return true; // Don't bother, already doing it

            // Set flag
            ReloadingRecordings = true;

            if (!isWMCOnline) return false;
            //if (!merge)
            {
                AllRecordings.Clear();
                AllRequests.Clear();
            }

            DateRange theRange = new DateRange(DateTime.Now.ToUniversalTime().AddDays(- daysBack), DateTime.Now.ToUniversalTime().AddDays(100));

            MergeInRequests(mcData.GetAllRequests(theRange));
            MergeInRecordings(mcData.GetAllRecordingsForRequests(AllRequests.Values.ToList(), theRange));
            

           // SortAllRecordingsByDate();
          //  SortAllRequestsByTitle();

            // Unset flag
            ReloadingRecordings = false;

            return true;
        }
        private static void MergeInRecordings(List<RPRecording> recordings)
        {
            if (recordings == null) return;

            foreach (RPRecording recording in recordings)
            {
                if (AllRecordings.ContainsKey(recording.Id))
                    AllRecordings.Remove(recording.Id);
                
                AllRecordings.Add(recording.Id, recording);   
            }
        }
        static void SortAllRecordingsByDate()
        {
            List<RPRecording> recs = AllRecordings.Values.ToList();
            recs.Sort(new RPRecordingStartTimeComparer());

            AllRecordings.Clear();
            foreach (RPRecording rec in recs)
            {
                AllRecordings.Add(rec.Id, rec);
            }
        }
        private static void MergeInRequests(List<RPRequest> requests)
        {
            if (requests == null) return;

            foreach (RPRequest request in requests)
            {
                if (AllRequests.ContainsKey(request.ID))
                    AllRequests.Remove(request.ID);

                AllRequests.Add(request.ID, request);
            }
        }
        static void SortAllRequestsByTitle()
        {
            List<RPRequest> reqs = AllRequests.Values.ToList();
            reqs.Sort(new RPRequestTitleComparer());
            AllRequests.Clear();
            foreach (RPRequest req in reqs)
            {
                AllRequests.Add(req.ID, req);
            }
        }

        // Refresh when out-of-date
        static void mcData_RecordingsDataNeedsRefresh(object sender, EventArgs e)
        {
            ReloadAllRecordings();
        }

        static void PopulateDefaultsIfUnset(ref RecordingRequest rr)
        {
            

            if (rr.Quality == -1)
                rr.Quality = Settings.Default.DefaultQuality;

            if (rr.KeepUntil == KeepUntilTypes.NotSet)
                rr.KeepUntil = (KeepUntilTypes)Settings.Default.DefaultKeepUntil;

            if (rr.KeepUntil == KeepUntilTypes.LatestEpisodes)
            {
                if (rr.KeepNumberOfEpisodes < 1)
                    rr.KeepNumberOfEpisodes = Settings.Default.DefaultKeepNumberOfEpisodes;
            }

            if (rr.Postpadding == 0)
                rr.Postpadding = Convert.ToInt32( Settings.Default.DefaultPostPadding );

            if (rr.Prepadding == 0)
                rr.Prepadding = Convert.ToInt32(Settings.Default.DefaultPrePadding);

        }

        // Schedule Recordings
        /// <summary>
        /// Schedule a recording - this examines the request and populates Service ID, etc.
        /// The MCDATA.Schedule... method should never be called directly outside this method
        /// </summary>
        public static RecordingResult ScheduleRecording(RecordingRequest rr)
        {
            // Populate defaults, e.g. quality, if not set
            PopulateDefaultsIfUnset(ref rr);

            RecordingResult failedResult = new RecordingResult();
            failedResult.Completed = false;

            if (rr.RequestType != RecordingRequestType.Manual)  // manual recordings already have a service ID specified
            {
                if (rr.TVProgrammeID < 1)
                {
                    failedResult.ErrorMessage = "No TV Programme ID was specified.";
                    return failedResult;
                }

                // Populate the Service ID if not already populated
                TVProgramme tvp = mcData.GetTVProgramme(rr.TVProgrammeID.ToString());
                if (tvp == null)
                {
                    failedResult.ErrorMessage = "No TV Programme with the specified ID could be found.";
                    return failedResult;
                }

                rr.ServiceID = long.Parse(tvp.ServiceID);  // could fail
            }

            // Get the channel ID from the service ID
            TVService tvs = TVServiceWithIDOrNull(rr.ServiceID.ToString());
            if (tvs == null)
            {
                failedResult.ErrorMessage = "No TV Channel with the retrieved ID could be found.";
                return failedResult;
            }
            rr.MCChannelID = tvs.MCChannelID;
            

            // ************** SCHEDULE THE RECORDING ************************
            RPRequest generatedRequest;
            RecordingResult earlyRecResult;
            if (!mcData.ScheduleRecording(rr, out generatedRequest, out earlyRecResult))
            {
                // Failed already - return the early result
                return earlyRecResult;
            }


            RecordingResult recResult = mcData.DetermineRecordingResultForRequest(generatedRequest);

            // Success?
            if (recResult.Success)
            {
                // Wait a moment so Scheduler can catch up and associate our request with our recordings...
                System.Threading.Thread.Sleep(600);

                // Now refresh and get the generated recordings...
                EPGManager.ReloadAllRecordings();

                try
                {
                    RPRequest req = recResult.GeneratedRecordingsBlob.RPRequests[0];

                    // Add recordings
                    recResult.GeneratedRecordingsBlob.RPRecordings = req.Recordings();

                    // Add programs linked to these recordings
                    foreach (RPRecording rec in recResult.GeneratedRecordingsBlob.RPRecordings)
                    {
                        TVProgramme tvp = rec.TVProgramme();
                        if (tvp != null)
                            recResult.GeneratedRecordingsBlob.TVProgrammes.Add(tvp);
                    }
                }
                catch (Exception ex) {
                    Functions.WriteLineToLogFile("ScheduleRecording(): Error retrieving recordings:");
                    Functions.WriteExceptionToLogFile(ex);
                    recResult.Success = false;
                    recResult.ErrorMessage = "Exception occured while retrieving recordings - the recording may have been scheduled.";
                }
            }

            return recResult;


        }
        #endregion

        #region Extension Methods

        // Extension methods as helpers, e.g. bools 
        public static bool IsRecurring(this RPRecording rec)
        {
            return ((rec.RequestType == RPRequestTypes.Series) || (rec.RequestType == RPRequestTypes.Keyword));
        }
        public static bool HasSeriesRequest(this TVProgramme tvp)  // For TVProgs not recording, but possibly part of a series, so we know whether to offer a 'record series' option
        {
            if (tvp.SeriesID < 1) return false;

            foreach (RPRequest rpreq in AllRequests.Values)
            {
                if (rpreq.SeriesID == tvp.SeriesID)
                {
                    return true;
                }
            }
            return false;
        }
        // Extension methods to link records: 
        public static TVService TVService(this TVProgramme tvp) // TVProgramme => TVService
        {
            string svcID = null;

            // If it's a fake, generated TV Programme...
            if (tvp.isGeneratedFromFile)
            {
                // Attempt to match the callsign to one of our channels
                svcID = ServiceIDFromCallsign(tvp.WTVCallsign);

                // If there's no match, generate a dummy TV service with the correct callsign
                if (string.IsNullOrEmpty(svcID))
                {
                    TVService tvs = new TVService();
                    tvs.Callsign = tvp.WTVCallsign;
                    tvs.UniqueId = "0";
                    return tvs;
                }
            }
            else
                svcID = tvp.ServiceID;  // Normal service retrieval

            if (string.IsNullOrEmpty(svcID)) return null;

            try
            {
                return AllTVChannels[tvp.ServiceID];
            }
            catch { }
            return null;
        }
        public static RPRecording Recording(this TVProgramme tvp)  // TV Programme => any recording (if known) 
        {
            // This actually iterates through all the recordings to find matches, there is no stored ID linking them
            foreach (RPRecording rpr in AllRecordings.Values)
            {
                try
                {
                    long tvpID = long.Parse(tvp.Id);

                    if (rpr.TVProgrammeID == tvpID)
                        return rpr;
                }
                catch { }
            }

            return null;
        }
        public static TVProgramme TVProgramme(this RPRecording rpr)
        {
            if (rpr.RequestType != RPRequestTypes.Manual)
                return mcData.GetTVProgramme(rpr.TVProgrammeID.ToString());

            // Manual recording is a special case
            TVProgramme tvp = new TVProgramme();
            tvp.Title = rpr.Title;
            tvp.ServiceID = rpr.ManualRecordingServiceID.ToString();
            tvp.StartTime = rpr.ManualRecordingStartTime.Ticks;
            tvp.StopTime = tvp.StartTime + (TimeSpan.FromSeconds(rpr.ManualRecordingDuration).Ticks);
            tvp.Id = "-10";
            return tvp;
        } // Recording => TV Programme
        public static List<TVProgramme> TVProgrammes(this RPRequest rq)
        {
            List<TVProgramme> output = new List<TVProgramme>();
            foreach (RPRecording rec in AllRecordings.Values)
            {
                if (rec.RPRequestID != rq.ID) continue;

                TVProgramme tvp = rec.TVProgramme();
                if (tvp != null)
                    output.Add(tvp);
            }

            return output;
        }  // Request => TV Programmes  (not really used, better to get recordings as more info and you can then get the tv progs for each recording)
        public static TVService TVService(this RPRequest rq) // Request => TV Service
        {
            foreach (TVService tvs in AllTVChannels.Values)
            {
                if (tvs.UniqueId.Equals(rq.ServiceID.ToString())) return tvs;
            }
            return null;
        }
        public static List<RPRecording> Recordings(this RPRequest rq)
        {
            List<RPRecording> output = new List<RPRecording>();
            foreach (RPRecording rec in AllRecordings.Values)
            {
                if (rec.RPRequestID != rq.ID) continue;
                output.Add(rec);
            }

            return output;
        } // Request => Recordings  (from which you can get the tv progs)
        public static RPRequest Request(this RPRecording rpr) // Recording => Request  (linked by ID)
        {
            return mcData.GetRPRequestWithID(rpr.RPRequestID);
        }
        public static RPRequest SeriesRequest(this TVProgramme tvp)
        {
            if (tvp.SeriesID < 1) return null;

            foreach (RPRequest rpreq in AllRequests.Values)
            {
                if (rpreq.SeriesID == tvp.SeriesID)
                {
                    return rpreq;
                }
            }
            return null;
        }
        public static string ToPrettyString(this RPRecordingStates state)
        {
            if (state == RPRecordingStates.Deleted)
                return "This program was recorded but has now been deleted.";
            else if (state == RPRecordingStates.Initializing)
                return "This program's recording is initializing.";
            else if (state == RPRecordingStates.Recorded)
                return "This program has been recorded.";
            else if (state == RPRecordingStates.Recording)
                return "This program is currently being recorded.";
            else if (state == RPRecordingStates.Scheduled)
                return "This program will be recorded.";
            else
                return "No new recording will be made.";
        }
        

        // Comparers that need extension methods
        public class RPRecordingStartTimeComparer : IComparer<RPRecording>  // must be in here to use the extension methods
        {
            public int Compare(RPRecording rec1, RPRecording rec2)
            {
                try
                {
                    TVProgramme tvp1 = rec1.TVProgramme();
                    TVProgramme tvp2 = rec2.TVProgramme();
                    if (
                        (tvp1 == null) || (tvp2 == null)
                        )
                        return 0;


                    if (rec1.TVProgramme().StartTime > rec2.TVProgramme().StartTime) return 1;
                    if (rec1.TVProgramme().StartTime < rec2.TVProgramme().StartTime) return -1;
                }
                catch { }
                return 0;
            }
        }
        #endregion

        // Conversion
        public static TVProgramme VideoFileToTVProgramme(string FN)
        {
            TVProgramme tvp = new TVProgramme();
            tvp.Description = "Video file from " + FN;
            tvp.Title = Path.GetFileNameWithoutExtension(FN);

            tvp.Filename = FN;
            tvp.isGeneratedFromFile = true;

            tvp.WTVCallsign = "Remote Potato";

            return tvp;
        }

        // Debug
        #region Debug
        // Events raised by provider
        static void epg_DebugReport(object sender, DebugReportEventArgs e)
        {
            if (EPGDebugReport != null) EPGDebugReport(new object(), e);
        }
        static void tempMCData_DebugReport(object sender, DebugReportEventArgs e)
        {
            // Copied from errorhandler
            bool logReport = false;

            if (e.Severity < 10)
                if (Settings.Default.DebugAdvanced)
                    logReport = true;

            if (e.Severity >= 10)
                logReport = true;

            if (logReport)
            {
                Functions.WriteLineToLogFile(e.DebugText);
                if (e.ThrownException != null)
                    Functions.WriteExceptionToLogFile(e.ThrownException);
            }
        }
        #endregion
    }


}

