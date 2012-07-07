using System;
using System.Threading;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.IO.IsolatedStorage;
using CommonEPG;
using CommonEPG.Comparers;
using System.Text;

namespace SilverPotato
{


    public static class ScheduleManager
    {
        public static Object scheduleLock = new object();
        public static DateTime lastUpdatedChannelsFromServer;

        static ScheduleManager()
        {
            
            AllTVChannels = new Dictionary<string, TVService>();
            EPGDisplayedChannels = new List<TVService>();

            AllRecordings = new Dictionary<long, RPRecording>();
            AllRequests = new Dictionary<long, RPRequest>();
            TVProgrammeStore = new Dictionary<string, TVProgramme>();
            AllRecordedTVProgrammes = new Dictionary<string, TVProgramme>();

            lastUpdatedChannelsFromServer = new DateTime();
        }

        public static void Initialize()
        {
            RecordingManager.GetRecordedTVCompleted += new EventHandler<GenericEventArgs<List<TVProgramme>>>(RecordingManager_GetRecordedTVCompleted);

            GetAllChannels();
            GetRecordingObjectsFromServer_Completed += new EventHandler(ScheduleManager_GetRecordingObjectsFromServer_Completed);
            GetRecordingObjectsFromServer(); // will happen at same time as channels request
        }
        static void ScheduleManager_GetRecordingObjectsFromServer_Completed(object sender, EventArgs e)
        {
            GetRecordingObjectsFromServer_Completed -= new EventHandler(ScheduleManager_GetRecordingObjectsFromServer_Completed);
        }
   

        #region Channels / Services
        // Members
        public static Dictionary<string, TVService> AllTVChannels;
        public static List<TVService> EPGDisplayedChannels;

        // Events
        public static event EventHandler ChannelsUpdated;
        
        // Retrieval
        public static bool GotChannelsFromServer = false;
        public static bool ChannelsUpdating = false;
        public static void GetAllChannels()
        {
            if (ChannelsUpdating) return; // already doing it

            ChannelsUpdating = true;
            EPGImporter importer = new EPGImporter();
            importer.GetChannelsCompleted += new EventHandler<GenericEventArgs<List<TVService>>>(importer_GetChannelsCompleted);
            importer.GetAllChannels(); 
        }

        static void importer_GetChannelsCompleted(object sender, GenericEventArgs<List<TVService>> e)
        {
            ChannelsUpdating = false;
            if (e.Value.Count < 1) return;

            AllTVChannels.Clear();
            foreach (TVService tvc in e.Value)
            {
                AllTVChannels.Add(tvc.UniqueId, tvc);
            }
            // Calculate helper lists
            CalculateEPGDisplayedChannels();

            // Flags and events
            GotChannelsFromServer = true;
            lastUpdatedChannelsFromServer = DateTime.Now;
            if (ChannelsUpdated != null)
                ChannelsUpdated(new object(), new EventArgs());
        }

        // Methods
        public static TVService TVServiceWithCallsignOrNull(string callsign)
        {
            foreach (CommonEPG.TVService tvc in AllTVChannels.Values)
            {
                if (tvc.Callsign.ToLower() == callsign.ToLower())
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
        public static List<TVService> AllTVChannelsToList()
        {
            return AllTVChannelsToList(false);            
        }
        public static List<TVService> AllTVChannelsToList(bool favoritesOnly)
        {
            List<TVService> output = new List<TVService>();
            foreach (TVService tvs in AllTVChannels.Values)
            {
                if (! favoritesOnly)
                    output.Add(tvs);
                else
                    if (tvs.IsFavorite) output.Add(tvs);
            }
            return output;
            
        }
        public static List<string> AllTVChannelCallsigns(bool alphaSorted)
        {
            List<TVService> Services = AllTVChannelsToList(false);

            if (alphaSorted)
            {
                TVCServiceCallsignComparer comparer = new TVCServiceCallsignComparer();
                Services.Sort(comparer);
            }

            List<string> Callsigns = new List<string>();
            foreach (TVService tvs in Services)
            {
                Callsigns.Add(tvs.Callsign);
            }

            return Callsigns;
        }
        public static string EPGDisplayedChannels_IDsOnlyAsXML
        {
            get
            {
                List<string> output = new List<string>();
                foreach (TVService tvs in EPGDisplayedChannels)
                {
                    output.Add(tvs.UniqueId);
                }

                try
                {
                    System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(output.GetType());
                    StringWriter sw = new StringWriter();
                    serializer.Serialize(sw, output);
                    return sw.ToString();
                }
                catch (Exception ex)
                {
                    Functions.WriteLineToLogFile("Couldn't serialise list of EPG Displayed channels");
                    Functions.WriteExceptionToLogFile(ex);
                }

                return "";
            }
        }
        

        // Helper Methods
        public static void CalculateEPGDisplayedChannels()
        {
            EPGDisplayedChannels.Clear();

            foreach (TVService tvc in AllTVChannels.Values)
            {
                if (Settings.ChannelFilter == ChannelFilterTypes.Favourites)
                {
                    // Just favourites
                    if (tvc.IsFavorite)
                    {
                        EPGDisplayedChannels.Add(tvc);
                    }
                }
                else
                {
                    // All channels
                    EPGDisplayedChannels.Add(tvc);
                }
            }

            // If there were no favourites, display all channels
            if (EPGDisplayedChannels.Count < 1)
            {
                foreach (TVService tvc in AllTVChannels.Values) { EPGDisplayedChannels.Add(tvc); }
            }

        }
        
        #endregion

        #region TV Programme Store
        public static Dictionary<string, TVProgramme> TVProgrammeStore;
        public static void MergeIntoTVProgrammeStore(List<TVServiceSlice> slices, bool setAsLongTermTenants)
        {
            Monitor.Enter(TVProgrammeStore);
            
            foreach (TVServiceSlice slice in slices)
            {
                MergeIntoTVProgrammeStore(slice.TVProgrammes, setAsLongTermTenants);
            }

            Monitor.Exit(TVProgrammeStore);
        }
        public static void MergeIntoTVProgrammeStore(List<TVProgramme> progs, bool setAsLongTermTenants)
        {
            if (progs == null) return;

            foreach (TVProgramme prog in progs)
            {
                TVProgramme existingProg = null;
                // Is item already in the store?
                if (TVProgrammeStore.TryGetValue(prog.Id, out existingProg))
                {
                    // Do NOT replace, but update long term flag if necessary
                    if (setAsLongTermTenants)
                        if (!existingProg.IsLongTermTenant)
                            existingProg.IsLongTermTenant = true;

                    // IF item is a long term tenant, ALWAYS respect that

                    // No need to replace/update item fields, programmes don't change and aren't linked to anything.

                    // Updated
                    existingProg.FireUpdated();
                }
                else  // Item not in there
                {
                    if (setAsLongTermTenants)
                        prog.IsLongTermTenant = true;
                    TVProgrammeStore.Add(prog.Id, prog);
                }
            }

            // Sort the store


            SortTVProgrammeStore();
            
        
        }
        public static void SortTVProgrammeStore()
        {
            List<TVProgramme> sortedStoreProgs = new List<TVProgramme>();  // Silverlight doesnt allow dictionary.values => List
            
            foreach (TVProgramme tvp in TVProgrammeStore.Values)
            {
                sortedStoreProgs.Add(tvp);
            }

            // Sort the store first by start time, then by service
//            sortedStoreProgs.Sort(new TVProgrammeServiceComparer());
            sortedStoreProgs.Sort(new TVProgrammeStartTimeComparer());

            // Put it all back together again
            TVProgrammeStore.Clear();
            foreach (TVProgramme tvp in sortedStoreProgs)
            {
                TVProgrammeStore.Add(tvp.Id, tvp);
            }
        }
        public static void RemoveTVProgrammeStoreTemporaryItems()
        {
            List<string> ProgsToBeRemoved = new List<string>();
            foreach (KeyValuePair<string, TVProgramme> kvp in TVProgrammeStore)
            {
                if (! kvp.Value.IsLongTermTenant)
                    ProgsToBeRemoved.Add(kvp.Key);
            }

            foreach (string strID in ProgsToBeRemoved)
            {
                TVProgrammeStore.Remove(strID);
            }
        }
        public static void RemoveTVProgrammeStoreLongTermTenants()
        {
            List<string> ProgsToBeRemoved = new List<string>();
            foreach (KeyValuePair<string, TVProgramme> kvp in TVProgrammeStore)
            {
                if (kvp.Value.IsLongTermTenant)
                    ProgsToBeRemoved.Add(kvp.Key);
            }

            foreach (string strID in ProgsToBeRemoved)
            {
                TVProgrammeStore.Remove(strID);
            }
        }

        // Retrieve Lists / Filters
        public static Dictionary<string, List<TVProgramme>> SeriesShowingsOfProgrammeGroupedByEpisodeTitle(TVProgramme matchProg)
        {
            Dictionary<string, List<TVProgramme>> output = new Dictionary<string, List<TVProgramme>>();

            
            List<string> usedProgramIDs = new List<string>();

            List<TVProgramme> seriesShowings = matchProg.OtherShowingsWithinSeries();
            foreach (TVProgramme tvp in seriesShowings)
            {    
                // Do we have this episode (program) already?
                if (! usedProgramIDs.Contains(tvp.MCProgramID))
                {
                    // Add key (so it's not used again)
                    usedProgramIDs.Add(tvp.MCProgramID);

                    // No, add all showings
                    List<TVProgramme> episodeShowings = tvp.OtherShowingsOfThisProgramme();
                    if (episodeShowings.Count < 1) continue;  // Must be the current ep with no other showings

                    // Key by episode title if there is one
                    string headerKey = (string.IsNullOrEmpty( tvp.EpisodeTitle )) ?
                        "[BLANK]" :
                        "\"" + tvp.EpisodeTitle + "\"";
                    
                
                    // Exists already?  Remove and merge to current list (NB: Sometimes program IDs within 7MC can be different even for identical programs - unsure why, presumably epg source)
                    if (output.ContainsKey(headerKey))
                    {
                        // Merge into our current list
                        List<TVProgramme> removeList = output[headerKey];
                        output.Remove(headerKey); // remove it
                        episodeShowings.AddRange(removeList);
                        removeList.Clear();
                        removeList = null;
                        // Now we can proceed
                    }

                    output.Add(headerKey, episodeShowings);
                }                
            }

            return output;
        }
        public static Dictionary<string, List<TVProgramme>> OtherShowingsOfProgrammeGroupedBy(TVProgramme matchProg, string groupBy) // group by channel
        {
            Dictionary<string, List<TVProgramme>> output = new Dictionary<string, List<TVProgramme>>();

            List<TVProgramme> episodeShowings = matchProg.OtherShowingsOfThisProgramme();
            if (episodeShowings.Count < 1) return output; // no showings
             
            foreach (TVService tvs in EPGDisplayedChannels)
            {
                List<TVProgramme> progsOnChannel = new List<TVProgramme>();
                foreach (TVProgramme tvp in episodeShowings)
                {
                    if (tvp.ServiceID == tvs.UniqueId)
                        progsOnChannel.Add(tvp);
                }

                // Add by callsign ID - if there are any
                if (progsOnChannel.Count > 0)
                {
                    output.Add(tvs.Callsign, progsOnChannel);
                }
            }

            return output;
        }
        public static Dictionary<string, List<TVProgramme>> ProgrammesOfTypeGroupedByDate(TVProgrammeType pType, bool topRatedOnly)
        {
            Dictionary<string, List<TVProgramme>> output = new Dictionary<string, List<TVProgramme>>();

            List<string> usedProgramIDs = new List<string>();

            for (int weekCounter = 0; weekCounter < 3; weekCounter++)
            {
                string groupTitle = "";
                DateRange weekRange;
                                
                if (weekCounter == 0)
                {
                    groupTitle = "Today";
                    weekRange = new DateRange(DateTime.Now.Date.ToUniversalTime(), DateTime.Now.Date.AddDays(1).ToUniversalTime());
                }
                else if (weekCounter == 1)
                {
                    groupTitle = "This week";
                    weekRange = new DateRange(DateTime.Now.Date.AddDays(1).ToUniversalTime(), DateTime.Now.Date.AddDays(7).ToUniversalTime());
                }
                else 
                {
                    groupTitle = "Next week";
                    weekRange = new DateRange(DateTime.Now.Date.AddDays(7).ToUniversalTime(), DateTime.Now.Date.AddDays(14).ToUniversalTime());
                }
                
                
                List<TVProgramme> weekShowings = new List<TVProgramme>();

                foreach (TVProgramme tvp in TVProgrammeStore.Values)
                {
                    if (usedProgramIDs.Contains(tvp.Id)) continue; // already listed

                    if (tvp.ProgramType != pType) continue;
                    if (topRatedOnly)
                        if (tvp.StarRating < Settings.Default.RecommendedMovieMinimumRating) continue;

                    DateRange tvpRange = new DateRange(tvp.StartTimeDT(), tvp.StopTimeDT());
                    bool withinDateRange = weekRange.ContainsPartOfDateRange(tvpRange);

                    if (withinDateRange)
                    {
                        weekShowings.Add(tvp);

                        usedProgramIDs.Add(tvp.Id);
                    }

                    
                }


                // End of the week
                output.Add(groupTitle, weekShowings);

            }

            return output;
        }
        public static List<TVProgramme> ProgrammesOfType(TVProgrammeType pType)
        {
            List<TVProgramme> output = new List<TVProgramme>();
            foreach (TVProgramme tvp in TVProgrammeStore.Values)
            {
                if (tvp.ProgramType == pType)
                    output.Add(tvp);
            }

            return output;
        }
        #endregion

        #region Recordings
        public static Dictionary<long, RPRequest> AllRequests;
        public static Dictionary<long, RPRecording> AllRecordings;

        // Get / Update Recording Events
        public static event EventHandler Recordings_Changed;
        static event EventHandler GetRecordingObjectsFromServer_Completed;
        public static bool RecordingsUpdating = false;
        public static void GetRecordingObjectsFromServer()
        {
            RecordingsUpdating = true;
            EPGImporter importer = new EPGImporter();
            importer.GetRecordingsCompleted += new EventHandler<GenericEventArgs<RPRecordingsBlob>>(importer_GetRecordingsCompleted);
            importer.GetAllTVRecordingEvents();  
        }

        static void importer_GetRecordingsCompleted(object sender, GenericEventArgs<RPRecordingsBlob> e)
        {
            RecordingsUpdating = false;
            if (e.Value == null) return;

            RPRecordingsBlob recBlob = e.Value;

            // Populate local Dictionaries
            // 1. Requests
            AllRequests.Clear();
            foreach (RPRequest req in recBlob.RPRequests)
            {
                AllRequests.Add(req.ID, req);
            }
            recBlob.RPRequests.Clear();

            // 2. Recordings
            AllRecordings.Clear();
            foreach (RPRecording rec in recBlob.RPRecordings)
            {
                AllRecordings.Add(rec.Id, rec);
            }
            recBlob.RPRecordings.Clear();

            // 3. Additional Programmes (for use when viewing series request upcoming shows)
            RemoveTVProgrammeStoreLongTermTenants();
            MergeIntoTVProgrammeStore(recBlob.TVProgrammes, true);
            recBlob.TVProgrammes.Clear();
            recBlob = null;

            // Sync'd old schedule with these recordings
            if (Recordings_Changed != null) Recordings_Changed(new object(), new EventArgs());
            if (GetRecordingObjectsFromServer_Completed != null) GetRecordingObjectsFromServer_Completed(new object(), new EventArgs());
        }

        // Remove Recordings from lists
        public static void RemoveRequestFromListById(long ID) // Remove request and all associated recordings
        {
            if (!AllRequests.ContainsKey(ID)) return;

            // Remove corresponding recordings first
            RPRequest req = AllRequests[ID];
            List<RPRecording> recordings = req.Recordings();
            List<TVProgramme> programmesToUpdate = new List<TVProgramme>();
            foreach (RPRecording rec in recordings)
            {
                // Get linked TV programme
                TVProgramme tvp = rec.TVProgramme();
                if (tvp != null)
                    programmesToUpdate.Add(tvp);
                
                AllRecordings.Remove(rec.Id);
            }

            // Remove the request
            AllRequests.Remove(ID);

            // Update Linked TV Programmes  (AFTER removing the recordings and request!)
            foreach (TVProgramme tvp in programmesToUpdate)
            {
                tvp.FireUpdated();
            }

            if (Recordings_Changed != null) Recordings_Changed(new object(), new EventArgs());
        }
        public static void RemoveRecordingFromListById(long ID) // Remove recording
        {
            if (!AllRecordings.ContainsKey(ID)) return;

            RPRecording rec = AllRecordings[ID];

            // Get linked tv programme
            TVProgramme tvp = rec.TVProgramme();

            // Remove the recording
            AllRecordings.Remove(ID);

            // Update Linked TV Programme  (AFTER removing the recording!)
            if (tvp != null)
                tvp.FireUpdated();

            if (Recordings_Changed != null) Recordings_Changed(new object(), new EventArgs());
        }

        // Merge in lists
        public static void MergeInRecordingsFromList(List<RPRecording> recordings)
        {
            if (recordings == null) return;

            foreach (RPRecording recording in recordings)
            {
                if (!AllRecordings.ContainsKey(recording.Id))
                    AllRecordings.Add(recording.Id, recording);
            }
        }
        public static void MergeInRequestsFromList(List<RPRequest> requests)
        {
            if (requests == null) return;

            foreach (RPRequest request in requests)
            {
                if (!AllRequests.ContainsKey(request.ID))
                    AllRequests.Add(request.ID, request);
            }
        }

        // Retrieve Lists/Filters
        public static Dictionary<string, List<RPRecording>> UpcomingRecordingsGroupedBy(string groupBy)
        {
            Dictionary<string, List<RPRecording>> output = new Dictionary<string, List<RPRecording>>();


            DateTime dateCounter = DateTime.Now.Date;
            DateTime universalDate = dateCounter.ToUniversalTime();
            for (int i = 0; i < 31; i++)
            {
                List<RPRecording> dayList = new List<RPRecording>();
                foreach (RPRecording rec in AllRecordings.Values)
                {
                    TVProgramme tvp = rec.TVProgramme();
                    if (tvp == null) continue;

                    if (
                        ((rec.State == RPRecordingStates.Scheduled) || (rec.State == RPRecordingStates.Recording)) &&
                        ((tvp.StartTime >= universalDate.Ticks) && (tvp.StartTime < universalDate.AddDays(1).Ticks))
                        )
                    {
                        dayList.Add(rec);
                    }
                }

                // Sort the list by date
                dayList.Sort(new RPRecordingStartTimeComparer());

                if (dayList.Count > 0)
                    output.Add(dateCounter.ToPrettyDayNameAndDate(), dayList);

                dateCounter = dateCounter.AddDays(1);
                universalDate = dateCounter.ToUniversalTime();
            }

            return output;
        }
        public static Dictionary<string, List<RPRequest>> SeriesRequestsGroupedBy(string groupBy)
        {
            Dictionary<string, List<RPRequest>> output = new Dictionary<string, List<RPRequest>>();

            if (AllRequests == null) return output;

            List<RPRequest> allRequests = new List<RPRequest>();
            foreach (RPRequest req in AllRequests.Values)
            {
                if ((req.RequestType == RPRequestTypes.Series) || (req.RequestType == RPRequestTypes.Keyword) )
                    allRequests.Add(req);
            }

            // Sort list by name for now!
            allRequests.Sort(new CommonEPG.Comparers.RPRequestTitleComparer());

            output.Add("All Series Recordings", allRequests);

            return output;
        }
        public static List<RPRecording> AllRecordingsOnDate(DateTime localDate)
        {
            List<RPRecording> output = new List<RPRecording>();
            foreach (RPRecording rec in AllRecordings.Values)
            {
                if (rec.TVProgramme().StartTimeDT().ToLocalTime().Date == localDate.Date)
                    output.Add(rec);
            }

            return output;
        }  // TODO REMOVE?
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

            if (string.IsNullOrEmpty(svcID)) return TVServiceTemplate;
            if (string.IsNullOrEmpty(tvp.ServiceID)) return TVServiceTemplate;

            try
            {
                TVService tvs = null;
                if (AllTVChannels.TryGetValue(tvp.ServiceID, out tvs))
                    return tvs;
                else
                    return TVServiceTemplate;
                
            }
            catch { }
            return TVServiceTemplate;
        }
        public static TVService TVServiceTemplate
        {
            get
            {
                TVService tvs = new TVService();
                tvs.Callsign = "Unknown.";
                tvs.UniqueId = "0";
                return tvs;
            }
        }
        public static RPRecording Recording(this TVProgramme tvp)  // TV Programme => any request (if known) 
        {
            if (tvp.isGeneratedFromFile) return null;  // No recording linked, it's from a file

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
        public static TVProgramme TVProgramme(this RPRecording rpr) // Recording => TV Programme
        {
            foreach (TVProgramme tvp in TVProgrammeStore.Values)
            {
                if (tvp.Id == rpr.TVProgrammeID.ToString())
                    return tvp;
            }
            return null;
        }
        public static TVService TVService(this RPRequest rq) // Request => TV Service
        {
            foreach (TVService tvs in AllTVChannels.Values)
            {
                if (tvs.UniqueId.Equals(rq.ServiceID.ToString())) return tvs;
            }
            return null;
        }
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
        public static List<TVProgramme> OtherShowingsWithinSeries(this TVProgramme matchProg)
        {
            List<TVProgramme> output = new List<TVProgramme>();

            if (matchProg.SeriesID < 1) return output;  // Not in a series, no programs

            foreach (TVProgramme tvp in TVProgrammeStore.Values)
            {
                if (!tvp.Equals(matchProg))
                    if (tvp.SeriesID == matchProg.SeriesID)
                        output.Add(tvp);
            }
            return output;
        }
        public static List<TVProgramme> OtherShowingsOfThisProgramme(this TVProgramme matchProg)
        {
            List<TVProgramme> output = new List<TVProgramme>();

            foreach (TVProgramme tvp in TVProgrammeStore.Values)
            {
                if (! tvp.Equals(matchProg))
                    if (tvp.MCProgramID == matchProg.MCProgramID)
                        output.Add(tvp);
            }
            return output;
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
            foreach (RPRequest req in AllRequests.Values)
            {
                if (req.ID == rpr.RPRequestID) return req;
            }
            return null;
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
        public static RPRequest RecordingRequest(this TVProgramme tvp) // TVProgramme => Recording => Request
        {
            RPRecording rec = tvp.Recording();
            if (rec == null) return null;

            return rec.Request();
        }
        public static TimeSpan Duration(this TVProgramme tvp)
        {
            return (tvp.StopTimeDT() - tvp.StartTimeDT());
        }
       

        // updated from V1
        public static bool HasEndedYet(this TVProgramme tvp)
        {
            return (tvp.StopTimeDT().ToLocalTime() <= DateTime.Now);
        }
        public static bool IsCurrentlyShowing(this TVProgramme tvp)
        {
            return (tvp.StartTimeDT().ToLocalTime() <= DateTime.Now && tvp.StopTimeDT().ToLocalTime() > DateTime.Now);
        }
        public static int DurationMinutes(this TVProgramme tvp)
        {
            try
            {
                long lDuration = tvp.StopTime - tvp.StartTime;
                TimeSpan tDuration = new TimeSpan(lDuration);
                return Convert.ToInt32(tDuration.TotalMinutes);
            }
            catch
            {


                Functions.WriteLineToLogFile("Error [extension method error DurationMinutes] - could not get duration for show " + tvp.Title + " starts:" + tvp.StartTime.ToString() + " stops:" + tvp.StopTime.ToString());

            }
            return 0;
        }
        public static bool IsRecommended(this TVProgramme tvp)
        {
            return (tvp.StarRating > 6);
        }
        public static bool IsTopRated(this TVProgramme tvp)
        {
            return (tvp.StarRating > 7);
        }
        public static string ChannelNumberString(this CommonEPG.TVService tvc)
        {
            if (tvc.MCChannelNumber < 1) return "";

            string txtOutput = tvc.MCChannelNumber.ToString();
            if (tvc.MCSubChannelNumber > 0)
                txtOutput += "." + tvc.MCSubChannelNumber.ToString();

            return txtOutput;
        }
        public static string ToTooltipString(this TVProgramme tvp)
        {
            //return tvp.Title + Environment.NewLine + tvp.ToPrettyStartStopLocalTimes() + Environment.NewLine + WordWrap(tvp.Description,50);
            return tvp.Title + Environment.NewLine + tvp.ToPrettyStartStopLocalTimes();
        }
        public static bool HasActiveRecording(this TVProgramme tvp)
        {
            RPRecording rec = tvp.Recording();
            if (rec == null) return false;

            return ((rec.State == RPRecordingStates.Recorded) ||
                (rec.State == RPRecordingStates.Recording) ||
                (rec.State == RPRecordingStates.Scheduled) ||
                (rec.State == RPRecordingStates.Initializing)
                );
        }
        public static bool isSeriesRecording(this TVProgramme tvp)
        {
            RPRecording rec = tvp.Recording();
            if (rec == null) return false;

            return (
                (rec.RequestType == RPRequestTypes.Series) ||
                (rec.RequestType == RPRequestTypes.Keyword)
                );
        }
        public static string RecordingStateString(this TVProgramme tvp)
        {
            RPRecording rec = tvp.Recording();
            if (rec == null) 
                return "This program is not set to record.";

            return rec.State.ToPrettyString();
        }
        public static Uri ThumbnailUriOrNull(this RPRecording rec)
        {
            TVProgramme tvp = rec.TVProgramme();
            if (tvp == null) return null;

            return tvp.ThumbnailUriOrNull();
        }
        public static Uri ThumbnailUriOrNull(this TVProgramme tvp)
        {
            if (String.IsNullOrEmpty(tvp.Filename))
                return null;
            else
                return new Uri(NetworkManager.hostURL + "rectvthumbnail64?filename=" + Uri.EscapeUriString( Functions.EncodeToBase64( tvp.Filename) ), UriKind.Absolute);
        }
        public static Uri LogoUriRemote(this TVService tvc)
        {
            Uri theUri = new Uri(NetworkManager.hostURL + "logo/" + Uri.EscapeUriString(tvc.Callsign), UriKind.Absolute);
            if (Settings.DebugLogos) Functions.WriteLineToLogFile("Logo URL for channel is: " + theUri.AbsoluteUri);
            return theUri;
        }
        public static bool CanChooseStartPositionWhenStreaming(this TVProgramme tvp)
        {
            if (string.IsNullOrEmpty( tvp.Filename) ) return false;

            string ext = Path.GetExtension(tvp.Filename).ToLower();
            return ((ext == ".wtv") || (ext == ".dvr-ms")) ;
        }

        // Comparers that require extension methods
        public class RPRecordingStartTimeComparer : IComparer<RPRecording>  // must be in here to use the extension methods
        {
            public int Compare(RPRecording rec1, RPRecording rec2)
            {
                try
                {
                    if (rec1.TVProgramme().StartTime > rec2.TVProgramme().StartTime) return 1;
                    if (rec1.TVProgramme().StartTime < rec2.TVProgramme().StartTime) return -1;
                }
                catch { }
                return 0;
            }
        }
        
        #endregion

        #region RecordedTV
        public static event EventHandler RecordedTVUpdated;
        public static Dictionary<string, TVProgramme> AllRecordedTVProgrammes;
        public static void GetRecordedTV(bool shouldRefresh)
        {
            RecordingManager.GetRecordedTV(shouldRefresh);
        }
        static void RecordingManager_GetRecordedTVCompleted(object sender, GenericEventArgs<List<TVProgramme>> e)
        {
            AllRecordedTVProgrammes.Clear();
            // Populate local array
            foreach (TVProgramme tvp in e.Value)
            {
                if (AllRecordedTVProgrammes.ContainsKey(tvp.Id))
                    AllRecordedTVProgrammes.Remove(tvp.Id);
                
                AllRecordedTVProgrammes.Add(tvp.Id, tvp);
            }

            e.Value.Clear(); // save memory

            // Fire an event
            if (RecordedTVUpdated != null) RecordedTVUpdated(new object(), new EventArgs());
        }
        // Filter
        public static Dictionary<string, List<TVProgramme>> RecordedTVGroupedBy(string groupBy)
        {
            if (groupBy == "series")
                return RecordedTVGroupedByTitle(true);
            else if (groupBy == "title")
                return RecordedTVGroupedByTitle(false);
            else
                return RecordedTVGroupedByDate();
        }
        public static Dictionary<string, List<TVProgramme>> RecordedTVGroupedByTitle(bool groupSeries)
        {
            Dictionary<string, List<TVProgramme>> dctOutput = new Dictionary<string, List<TVProgramme>>();
            
            foreach (TVProgramme tvp in AllRecordedTVProgrammes.Values)
            {

                // Group by series, or just first letter
                string Alpha = 
                    groupSeries ?
                    tvp.Title.ToUpper() :
                    tvp.Title.Substring(0, 1).ToUpper();

                if (dctOutput.ContainsKey(Alpha))
                    dctOutput[Alpha].Add(tvp);
                else
                {
                    List<TVProgramme> newAlphaList = new List<TVProgramme>();
                    newAlphaList.Add(tvp);
                    dctOutput.Add(Alpha, newAlphaList);
                }
            }

            // Sort each letter by alpha
            foreach (List<TVProgramme> alphaList in dctOutput.Values)
            {
                alphaList.Sort(new TVProgrammeTitleComparer());
            }

            

            List<KeyValuePair<string, List<TVProgramme>>> lstOutput = new List<KeyValuePair<string,List<TVProgramme>>>();
            foreach (KeyValuePair<string, List<TVProgramme>> kvp in dctOutput)
            {
                lstOutput.Add(kvp);
            }
            lstOutput.Sort(new KVPTitleComparer());

            Dictionary<string, List<TVProgramme>> dctSortedOutput = new Dictionary<string, List<TVProgramme>>();
            foreach (KeyValuePair<string, List<TVProgramme>> kvp in lstOutput)
            {
                dctSortedOutput.Add(kvp.Key, kvp.Value);
            }

            // Clear old objects
            dctOutput.Clear();
            lstOutput.Clear();

            return dctSortedOutput;
        }

        public class KVPTitleComparer : IComparer<KeyValuePair<string, List<TVProgramme>>>
        {
            public int Compare(KeyValuePair<string, List<TVProgramme>> kvp1, KeyValuePair<string, List<TVProgramme>> kvp2)
            {
                return string.Compare(kvp1.Key, kvp2.Key);
            }
        }
        public static Dictionary<string, List<TVProgramme>> RecordedTVGroupedByDate()
        {
            Dictionary<string, List<TVProgramme>> output = new Dictionary<string, List<TVProgramme>>();

            DateTime endWindowCounter = DateTime.Now.Date.AddDays(1).ToUniversalTime();
            TimeSpan skipbackTime = TimeSpan.FromDays(-7);
            DateTime startWindowCounter = endWindowCounter;
            for (int i = 0; i < 120; i++)
            {
                if (i == 4)
                    skipbackTime = TimeSpan.FromDays(-30);

                // Skip back
                endWindowCounter = startWindowCounter;
                startWindowCounter = endWindowCounter + skipbackTime;

                List<TVProgramme> dayList = new List<TVProgramme>();
                foreach (TVProgramme tvp in AllRecordedTVProgrammes.Values)
                {
                    if (
                        ((tvp.StartTime >= startWindowCounter.Ticks) && (tvp.StartTime < endWindowCounter.Ticks))
                        )
                    {
                        dayList.Add(tvp);
                    }
                }

                if (dayList.Count > 0)
                {
                    // Sort each group DESCENDING (newest first)
                    dayList.Sort(new TVProgrammeStartTimeComparerDescending());

                    string gpHeader;
                    if (i == 0)
                        gpHeader = "This week:";
                    else if (i == 1)
                        gpHeader = "One week ago:";
                    else if (i == 2)
                        gpHeader = "Two weeks ago:";
                    else if (i == 3)
                        gpHeader = "Three weeks ago:";
                    else if (i == 4)
                        gpHeader = "Last month:";
                    else
                        gpHeader = (i-3).ToString() + " months ago:";
                    output.Add(gpHeader, dayList);
                }
            }

            return output;
        }

        #endregion

    }


    




}
