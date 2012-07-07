using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections;
using Microsoft.MediaCenter.Guide;
using Microsoft.MediaCenter.Store;
using Microsoft.MediaCenter.Pvr;
using CommonEPG;
using System.Globalization;

using System.Diagnostics;

/*
 *  The lowest-level methods for interfacing with native WMC .NET classes
 *  Be very careful here, it's all undocumented.
 *  
 *  Also, use the data retrieved sensibly and respect any issues of Copyright.
 */

namespace CommonEPG
{
    public class ObjectStoreManager : IDisposable
    {
        ObjectStore os;
        Scheduler recScheduler;

        #region Constructor / Dispose
        public ObjectStoreManager()
        {

        }
        public void Dispose()
        {
            if (recScheduler != null)
                recScheduler.Dispose();

            if (os != null)
            {
                os.Dispose();
            }

            
        }
        public bool Init()
        {
            try
            {
                // This is the magic way to get R/W access to the database.  Cannot be guaranteed to work in future versions of windows
                string providerName = Encoding.Unicode.GetString(Convert.FromBase64String("QQBuAG8AbgB5AG0AbwB1AHMAIQBVAHMAZQByAA=="));

                string epgClientID = ObjectStore.GetClientId(true);
                byte[] epgBytes = Encoding.Unicode.GetBytes(epgClientID);
                SHA256 sha = SHA256Managed.Create();
                byte[] epgHashed = sha.ComputeHash(epgBytes);
                string providerPassword = Convert.ToBase64String(epgHashed);
                DebugNormal("Opening store.");
                os = ObjectStore.Open(providerName, providerPassword);
                DebugNormal("Store opened.");               

                DebugNormal("Creating scheduler.");
                recScheduler = new Scheduler(os, ScheduleConflictSource.AutomaticUpdate);
                DebugNormal("Scheduler created.");

                return true;
            }
            catch (Exception ex)
            {
                DebugError(ex);
                return false;
            }
        }
        #endregion

        // Events
        public event EventHandler<DebugReportEventArgs> DebugReport;

        #region Schedule Recordings

        // Class members
        EventWaitHandle ewhScheduleRecording = null; // WAIT HANDLE to convert async to sync
        public RecordingResult ScheduleInitialFailureResult;
        public bool ScheduleInitialSucceeded;
        public Request requestInProgress; 

        //Request rqInProgress;
        public void ScheduleRecording(RecordingRequest rr, EventWaitHandle _ewh)
        {
            // Store pointer to handle locally
            ewhScheduleRecording = _ewh;
            ScheduleInitialFailureResult = new RecordingResult();  // For now, set shared result to assume failure  (in case of time outs etc)
            ScheduleInitialFailureResult.ErrorMessage = "An error message was never generated.";
            ScheduleInitialSucceeded = false;

            // Channel check
            if (rr.MCChannelID < 1 )
            {
                ScheduleInitialFailureResult.ErrorMessage = "No MC Channel ID was specified.";
                ScheduleInitialFailureResult.RequestResult = RecordingResult.RequestResults.FailedWithError;
                ScheduleRecordingCompleted();
                return;
            }

            // Too much padding
            if ((rr.Postpadding > 1800) && (rr.Prepadding > 1800) )
            {
                ScheduleInitialFailureResult.ErrorMessage = "Pre or Post padding must be less than 30 minutes.";
                ScheduleInitialFailureResult.RequestResult = RecordingResult.RequestResults.FailedWithError;
                ScheduleRecordingCompleted();
                return;
            }

            // Get service
            StoredObject so = os.Fetch(rr.MCChannelID);
            if (!(so is Channel))
            {
                ScheduleInitialFailureResult.ErrorMessage = "The retrieved TV channel was not valid.";
                DebugError("OSM: Retrieved TV channel not valid: ID is [" + rr.MCChannelID.ToString() + "] and so is [" + so.ToString() + "]");
                ScheduleInitialFailureResult.RequestResult = RecordingResult.RequestResults.FailedWithError;
                ScheduleRecordingCompleted();
                return;
            }

            Channel channel = (Channel)so;


            // Get TV programme (ie schedule entry)
            ScheduleEntry schedEntry = null;
            if (rr.RequestType != RecordingRequestType.Manual)
            {
                StoredObject sto = os.Fetch(rr.TVProgrammeID);
                if (!(sto is ScheduleEntry))
                {
                    ScheduleRecordingCompleted();
                    return;
                }

                schedEntry = (ScheduleEntry)sto;
            }
            
            // Store request
            requestInProgress = null;
            switch (rr.RequestType)
            {
                case RecordingRequestType.Series:
                    // Check schedentry has series info
                    if (schedEntry.Program != null)
                        if (schedEntry.Program.Series == null)
                        {
                            ScheduleInitialFailureResult.ErrorMessage = "This show is not part of a recognised series within Media Center.";
                            ScheduleInitialFailureResult.RequestResult = RecordingResult.RequestResults.FailedWithError;
                            ScheduleRecordingCompleted();
                            return;
                        }
                    SeriesRequest sreq = recScheduler.CreateSeriesRequest(schedEntry, channel);
                    sreq.RunType = (rr.FirstRunOnly) ? RunType.FirstRunOnly : RunType.Any;

                    // sreq.IsRecurring = true  ??
                    sreq.AnyChannel = (rr.SeriesRequestSubType == SeriesRequestSubTypes.AnyChannelAnyTime);
                    // Series request HAS to have IsRecurring set to TRUE.  Series requests CANNOT be non recurring!
                    sreq.IsRecurring = true; //= ((rr.SeriesRequestSubType == SeriesRequestSubTypes.AnyChannelAnyTime) || (rr.SeriesRequestSubType == SeriesRequestSubTypes.ThisChannelAnyTime));

                    /*
                    // Experimental - AIR TIME
                    string strAirtimecheck = "";
                        if (!sreq.Airtime.HasValue)
                            strAirtimecheck += "null";
                        else
                            strAirtimecheck += sreq.Airtime.Value.ToString();

                        DebugNormal("Airtime (before alteration) is:" + strAirtimecheck);

                    // 12 hour window
                    if (rr.SeriesRequestSubType == SeriesRequestSubTypes.ThisChannelAnyTime)
                    {
                        sreq.Airtime = TimeSpan.FromHours(12);
                    }
                    else if (rr.SeriesRequestSubType == SeriesRequestSubTypes.ThisChannelThisTime)
                    {
                        sreq.Airtime = TimeSpan.FromHours(3);
                    }
                    */
                    // Look also at...
                    //sreq.Airtime
                    //sreq.DaysOfWeek
                    // sreq.ScheduleLimit - maximum number to schedule
                    

                    // If keep until is 'latest episodes', set the number here...
                    if (rr.KeepUntil == KeepUntilTypes.LatestEpisodes)
                    {
                        if (rr.KeepNumberOfEpisodes > 0)
                            sreq.RecordingLimit = rr.KeepNumberOfEpisodes;
                    }
                    

                    
                    requestInProgress = (Request)sreq;
                    break;

                case RecordingRequestType.OneTime:
                    OneTimeRequest oreq = recScheduler.CreateOneTimeRequest(schedEntry, channel);
                    requestInProgress = (Request)oreq;
                    requestInProgress.AnyChannel = false;
                    //requestInProgress.ScheduleLimit = 5; // This didn't do anything!
                    break;

                case RecordingRequestType.Manual:
                    
                    ManualRequest mreq = recScheduler.CreateManualRequest(rr.StartTime, TimeSpan.FromMinutes(rr.Duration), channel, rr.ManualRecordingName, "", "", "", (System.Globalization.CultureInfo.CurrentCulture.LCID), false);
                    requestInProgress = (Request)mreq;
                    break;
            }

            // Universal Request Settings - all types of recording
            requestInProgress.PostPaddingRequested = TimeSpan.FromSeconds(rr.Postpadding);
            requestInProgress.PrePaddingRequested = TimeSpan.FromSeconds(rr.Prepadding);
            // Keep Until
            if (rr.KeepUntil != KeepUntilTypes.NotSet)
            {
                try {
                    DebugNormal("OSM: Setting 7MC request keeplength using string " + rr.KeepUntil.ToString());
                    requestInProgress.KeepLength = (KeepLength)Enum.Parse(typeof(KeepLength), rr.KeepUntil.ToString(), true);
                    DebugNormal("OSM: 7MC request keepLength is now " + requestInProgress.KeepLength.ToString());
                }
                catch { DebugNormal("ERROR: Couldn't parse KeepUntil value."); }
            }
            // ELSE ...  ASSUME media center uses KeepUntil default when creating recording?  If not, SET HERE ===>


            requestInProgress.Quality = rr.Quality;

            // Update request... (TODO is this step strictly necessary?)
            //rqInProgress.Updated += new StoredObjectEventHandler(ScheduleRecording_2);
            //rqInProgress.UpdateRequest();
            requestInProgress.UpdateRequestedPrograms();
            UpdateDelegate upd = new UpdateDelegate(ScheduleRecording_Done);
            requestInProgress.UpdateAndSchedule(upd, recScheduler);
        }
        void ScheduleRecording_Done()
        {
            // It has been scheduled.
            ScheduleInitialSucceeded = true;
            ScheduleRecordingCompleted();
        }
        void ScheduleRecordingCompleted()
        {
            if (recScheduler != null)
                recScheduler.Schedule();  // just in case this helps commit data

            if (ewhScheduleRecording != null)
                ewhScheduleRecording.Set();
        }

        // DETERMINE SUCCESS
        public RecordingResult DetermineIfRequestSucceeded(RPRequest rpRequest)
        {
            // Asume failure
            RecordingResult recResult = new RecordingResult();
            recResult.Completed = true;
            recResult.ErrorMessage = "An unknown error occurred.";

            Request recRequest = GetRequestWithID(rpRequest.ID);
            if (recRequest == null)
            {
                recResult.RequestResult = RecordingResult.RequestResults.FailedWithError;
                recResult.ErrorMessage = "Could not re-located the recording request to determine success or failure.  It may have succeeded.";
                return recResult;
            }


            // No requested programmes were generated - so report failure and cancel; it's a useless request.
            if ((recRequest.RequestedPrograms == null) && (!(recRequest is WishListRequest))) // allow a keyword to continue 
            {
                recResult.RequestResult = RecordingResult.RequestResults.NoProgrammesFound;
                return recResult;
            }

            // Let's compile some stats...
            // 

            if (recRequest.HasConflictedRequestedPrograms)
            {
                DebugNormal("Request has conflicted requested programmes.  (only in EPG, there may yet be enough tuners.):");


                // We know that only one programme is requested.  Are there any recordings at all?
                bool FoundAnyRequestedProgrammesThatWillRecord = false;
                int numberOfRequestedProgrammes = 0;
                int numberOfRequestedProgrammesThatWillRecord = 0;
                DebugNormal("Enumerating all requested programmes to see which will record:");
                foreach (RequestedProgram rprog in recRequest.RequestedPrograms)
                {
                    numberOfRequestedProgrammes++;

                    DebugNormal("Program:  " + rprog.ToString());  // should work
                    DebugNormal("WillRecord: " + rprog.WillRecord);
                    //DebugNormal("IsAssigned: " + rprog.IsAssigned);
                    //DebugNormal("IsRequestFilled: " + rprog.IsRequestFilled);
                    //DebugNormal("IsReal: " + rprog.IsReal);
                    if (rprog.WillRecord)
                    {
                        numberOfRequestedProgrammesThatWillRecord++;
                        FoundAnyRequestedProgrammesThatWillRecord = true;  // dont bother checking first to speed up
                    }
                }



                // What kind of request?  
                // ******************************** ONE TIME / MANUAL CONFLICTS ********************************
                if (
                    (recRequest is OneTimeRequest) ||
                    (recRequest is ManualRequest)
                    ) 
                {

                    if (!FoundAnyRequestedProgrammesThatWillRecord)
                    {
                        // No requested programmes will record... ...this is the end of the line
                        recResult.WereConflicts = true;   // not strictly necessary
                        recRequest.Cancel();
                        recResult.Completed = true;
                        recResult.RequestResult = RecordingResult.RequestResults.Conflicts;
                        return recResult;
                    }
                }


                // ******************************** SERIES / WISHLIST CONFLICTS ********************************
                if ((recRequest is SeriesRequest) || (recRequest is WishListRequest))
                {

                    // if !FoundAnyRequested...   Abandon if none will record ?  NOT FOR NOW

                    // Conflicts mean nothing, it comes down to what will actually record, so it's possible
                    // that everything is actually okay. Check now:
                    if (numberOfRequestedProgrammesThatWillRecord < numberOfRequestedProgrammes)  
                    {
                        // Calculate how many are in conflict and warn user
                        recResult.ConflictInfo = "Out of " + numberOfRequestedProgrammes.ToString() + " shows found, " +
                            (numberOfRequestedProgrammes - numberOfRequestedProgrammesThatWillRecord).ToString() +
                            " will not record as they conflict with existing recordings.";

                        recResult.WereConflicts = true;
                    }
                    // else everything is Okay
                }
            }


            // *********************** SUCCESS *********************************

            // RECORDINGS / TV PROGRAMMES
            List<RPRecording> rprecordings = new List<RPRecording>();
            List<TVProgramme> tvprogrammes = new List<TVProgramme>();

            // Make a blob
            RPRecordingsBlob blob = new RPRecordingsBlob();

            // Store NEW request in blob.  (might have changed since the one that was passed to this method)
            RPRequest newRequest = Conversion.RPRequestFromRequest(recRequest);
            blob.RPRequests = new List<RPRequest>() { newRequest };

            // Don't store anything else inside the blob here - it's done in the main callback in EPGManager.cs when all recordings are refreshed.

            // Store the blob inside the record result
            recResult.GeneratedRecordingsBlob = blob;

            // Success!  (?)
            recResult.Success = true;
            recResult.Completed = true;
            recResult.RequestResult = RecordingResult.RequestResults.OK;

            // Return the result (to waitHandle etc)
            return recResult;
        }

        #endregion

        #region Snippets
        /*
         * GET RECORDINGS FROM LIBRARY
         * 
         *   using (Library lib = new Library(os, false, false))
            {
                try
                {
                    foreach (Recording rec in lib.ScheduledRecordings)
                    {
                        if (rec.ShouldRecord)
                        {
                            if (!((rec.StartTime >= dateRange.StartTime) && (rec.StartTime < dateRange.StopTime))) continue;

                            RPRecording mcr = Conversion.RPRecordingFromRecording(rec);
                            output.Add(mcr);
                        }
                    }
                }

                catch (Exception ex)
                {
                    DebugNormal("Couldn't get recordings: " + ex.Message);
                }
            }

            return output;
         */

        /*  
         * 
         *   GET ALL RECORDINGS FROM SCHEDULER.REQUESTEDPROGRAMS
         * 
    public List<RPRecording> GetAllRecordings(DateRange dateRange) // better (doesnt need devices / content recorders)
     {
         List<RPRecording> output = new List<RPRecording>();

         // using (Scheduler sch = new Scheduler(os, ScheduleConflictSource.AutomaticUpdate))
         {
             try
             {
                 RequestedPrograms rqPrograms = recScheduler.RequestedPrograms;
                 if (rqPrograms == null) return output;

                 StoredObjectsEnumerator<RequestedProgram> enmRqProg = (StoredObjectsEnumerator<RequestedProgram>)rqPrograms.GetStoredObjectsEnumerator();
                 while (enmRqProg.MoveNext())
                 {
                     RequestedProgram rqProg = enmRqProg.Current;
                     if (!rqProg.IsActive) continue;
                     if (!rqProg.HasRecordings) continue;

                     IEnumerable<Recording> recordings = rqProg.Recordings;
                     foreach (Recording rec in recordings)
                     {
                         RPRecording mcr = Conversion.RPRecordingFromRecording(rec);
                         output.Add(mcr);
                     }
                 }

                 return output;
             }
             catch (Exception ex)
             {
                 DebugNormal("Couldn't get recordings: " + ex.Message);
                 return output;
             }
         }
     } 
      */

        #endregion

        #region Recordings
        public List<RPRequest> GetAllRequests(DateRange dateRange)
        {
            List<RPRequest> output = new List<RPRequest>();

            using (Requests rqs = new Requests(os))
            {
                try
                {

                    foreach (Request req in rqs.AllActive)
                    {
                        //if (req is OneTimeRequest) continue;
                        //if (req is ManualRequest) continue;
                        //if (req.Complete) continue;  // no longer active
                        if (req.Recordings == null) continue;
                        if (!req.IsLatestVersion) continue;

                        //if (!((req.StartTime > dateRange.StartTime) && (req.StartTime < dateRange.StopTime))) continue;

                        RPRequest newRequest = Conversion.RPRequestFromRequest(req);
                        output.Add(newRequest);
                    }


                    return output;
                }
                catch (Exception ex)
                {
                    DebugNormal("Couldn't get recordings: " + ex.Message);
                    return output;
                }
            }
            /*
            try
            {
                RequestedPrograms rqPrograms = sch.RequestedPrograms;
                if (rqPrograms == null) return output;

                HashSet<Request> uniqueRequests = new HashSet<Request>();
                StoredObjectsEnumerator<RequestedProgram> enmRqProg = (StoredObjectsEnumerator<RequestedProgram>)rqPrograms.GetStoredObjectsEnumerator();
                while (enmRqProg.MoveNext())
                {
                    RequestedProgram rqProg = enmRqProg.Current;

                    if (!rqProg.IsActive) continue;  // Don't proceed for old (deleted) shows

                    Request rq = rqProg.Request;
                    // Don't include old (deleted) requests

                    uniqueRequests.Add(rq);  // only adds if unique
                }
                

                // Go through reqests and convert each one
                foreach (Request r in uniqueRequests)
                {
                    RPRequest newRequest = Conversion.RPRequestFromRequest(r);
                    output.Add(newRequest);
                }

                return output;
            }
            catch (Exception ex)
            {
                DebugNormal("Couldn't get recordings: " + ex.Message);
                return output;
            }
            */

        }
        public List<RPRecording> GetAllRecordingsForRequests(List<RPRequest> ExistingRequests, DateRange dateRange) // better (doesnt need devices / content recorders)
        {
            List<RPRecording> output = new List<RPRecording>();

            try
            {
                foreach (RPRequest RPReq in ExistingRequests)
                {
                    Request req = GetRequestWithID(RPReq.ID);

                    foreach (Recording rec in req.Recordings)
                    {
                        if (rec.Abandoned) continue;  // Not abandoned recordings
                        if (!rec.ShouldRecord)
                        {
                         //   DebugNormal("Found recording that won't record: " + rec.Id.ToString());
                            continue; // Not recordings that aren't going to happen
                        }
                        //if (!((rec.StartTime >= dateRange.StartTime) && (rec.StartTime < dateRange.StopTime))) continue;

                        RPRecording mcr = Conversion.RPRecordingFromRecording(rec);
                        output.Add(mcr);
                        
                    }
                }
            }

            catch (Exception ex)
            {
                DebugNormal("Couldn't get recordings: " + ex.Message);
            }
        

            return output;


            /*
            try
            {
                Requests rqs = new Requests(os);
                foreach (Request req in rqs.AllActive)
                {
                    //if (!((req.StartTime > dateRange.StartTime) && (req.StartTime < dateRange.StopTime))) continue;
                    if (!req.IsLatestVersion) continue;
                   // if (req.Complete) continue;  // no longer active
                    
                        

                        IEnumerable<Recording> recordings = req.Recordings;
                        foreach (Recording rec in recordings)
                        {
                            if (rec.ShouldRecord)
                            {
                                if (!((rec.StartTime >= dateRange.StartTime) && (rec.StartTime < dateRange.StopTime))) continue;

                                RPRecording mcr = Conversion.RPRecordingFromRecording(rec);
                                output.Add(mcr);
                            }
                        }

                }

                return output;
            }
            catch (Exception ex)
            {
                DebugNormal("Couldn't get recordings: " + ex.Message);
                return output;
            }
            */


        }
        // Actions
        public bool CancelRequest(long requestID)
        {
            Request req = GetRequestWithID(requestID);
            if (req == null) return false;

            req.Cancel(true);

            return true;
        }
        public bool CancelRecording(long recordingID)
        {
            Recording rec = GetRecordingWithID(recordingID);
            if (rec == null) return false;

            RecordingState recState = rec.State;
            if (
                (recState == RecordingState.Scheduled) ||
                (recState == RecordingState.Initializing) ||
                (recState == RecordingState.None)
                )
            {
                // Not scheduled - cancel
                if (rec.RequestedProgram != null)
                    rec.RequestedProgram.Cancel();
                else
                    return false;
            }
            else if (recState == RecordingState.Recording)
            {
                /*
                // using ehiproxy
                //rec.ProgramContent
                Assignment ass = rec.Assignment;                                
                ContentRecorder crecorder = ass.ContentRecorder ;

                IRecorder irec = (IRecorder)crecorder;
                irec.Stop();

                Devices devs = crecorder.Devices;
                
                foreach (Device dv in devs)
                {
                    string rID = dv.RecorderId;
                    DebugInfoOnly("recorder ID: " + rID);

                }
                */
                return false;
            }

            return true;
        }
        public RPRequest GetRPRequestWithID(long ID)
        {
            Request obj = GetRequestWithID(ID);
            RPRequest rpr = Conversion.RPRequestFromRequest(obj);

            return rpr;
        }
        private Request GetRequestWithID(long ID)
        {
            StoredObject so = os.Fetch(ID);
            if (!(so is Request)) return null;
            Request obj = (Request)so;
            return obj;
        }
        private Recording GetRecordingWithID(long ID)
        {
            StoredObject so = os.Fetch(ID);
            if (!(so is Recording)) return null;
            Recording obj = (Recording)so;
            return obj;
        }

        #endregion

        #region Search
        public List<Service> ListOfServicesFromIDs(string[] serviceIDs)
        {
            List<Service> services = new List<Service>();
            try
            {
                foreach (string serviceUID in serviceIDs)
                {
                    long lID = long.Parse(serviceUID, NumberStyles.Any, CultureInfo.GetCultureInfo("EN-US"));
                    StoredObject so = os.Fetch(lID);
                    if (!(so is Service)) continue;
                    if (so == null) continue;
                    Service service = (Service)so;
                    services.Add(service);
                }
            }
            catch (Exception ex)
            {
                DebugError(ex);
                return services;
            }

            return services;
        }
        public List<TVProgramme> SearchTVProgrammesByDateRange(DateRange dateRange, string searchText, EPGSearchTextType searchTextType, EPGSearchMatchType searchMatchType, out bool resultsWereTruncated, string[] serviceIDs)
        {
            resultsWereTruncated = false;
            Stopwatch timePerformance = new Stopwatch();
            timePerformance.Start();

            List<TVProgramme> mainOutput = new List<TVProgramme>();

            List<Service> services = ListOfServicesFromIDs(serviceIDs);
            int limiter = 50;
            foreach (Service service in services)
            {
                List<TVProgramme> svcOutput = new List<TVProgramme>();

                // Get entries in time range using API function (about 8-95 times faster)
                ScheduleEntry[] schedEntries = service.GetScheduleEntriesBetween(dateRange.StartTime, dateRange.StopTime);

                TVProgramme tvp;
                foreach (ScheduleEntry se in schedEntries)
                {
                    // Some schedule entries can be null
                    if (se == null) continue;

                    if (!se.IsLatestVersion) continue;

                    Program p = se.Program;
                    if (p == null) continue;

                    List<string> lstSearchFields = new List<string>();
                    if ((searchTextType != EPGSearchTextType.Credits))
                        if (!String.IsNullOrEmpty(p.Title))
                            lstSearchFields.Add(p.Title);

                    if ( (searchTextType == EPGSearchTextType.TitleAndEpisodeTitle) || (searchTextType == EPGSearchTextType.TitlesAndDescription) || (searchTextType == EPGSearchTextType.AllTextFields) )
                        if (!String.IsNullOrEmpty(p.EpisodeTitle))
                            lstSearchFields.Add(p.EpisodeTitle);

                    if ((searchTextType == EPGSearchTextType.TitlesAndDescription) || (searchTextType == EPGSearchTextType.AllTextFields) )
                        if (!string.IsNullOrEmpty(p.Description))
                            lstSearchFields.Add(p.Description);

                    // if (searchTextType == EPGSearchTextType.Credits)  TODO
                    bool match = (StringListMatch(lstSearchFields, searchMatchType, searchText));
                    lstSearchFields.Clear();
                    lstSearchFields = null;

                    if (match)
                    {
                        tvp = Conversion.TVProgrammeFromScheduleEntry(se);
                        if (tvp != null)
                            mainOutput.Add(tvp);

                        if (limiter-- < 1)
                        {
                            resultsWereTruncated = true;
                            break;
                        }

                    }
                }

                if (limiter < 1) break;
            }

                // Sort by start time (prob not necessary)
                CommonEPG.Comparers.TVProgrammeStartTimeComparer comparer = new CommonEPG.Comparers.TVProgrammeStartTimeComparer();
                mainOutput.Sort(comparer);
            

            timePerformance.Stop();
            DebugNormal("Time to search EPG programmes : " + timePerformance.Elapsed.TotalMilliseconds.ToString() + "ms");

            return mainOutput;
        }

        // Search helpers
        bool StringListMatch(List<string> stringList, EPGSearchMatchType matchType, string searchText)
        {
            searchText = searchText.ToLowerInvariant();

            foreach (string s in stringList)
            {
                switch (matchType)
                {
                    case EPGSearchMatchType.Contains:
                        if (s.ToLowerInvariant().Contains(searchText)) return true;
                        break;

                    case EPGSearchMatchType.ExactMatch:
                        if (s.ToLowerInvariant().Equals(searchText)) return true;
                        break;

                    case EPGSearchMatchType.StartsWith:
                        if (s.ToLowerInvariant().StartsWith(searchText)) return true;
                        break;

                    default:
                        break;
                }
            }

            return false;
        }
        #endregion

        #region Channels 
        // Top Level - Data Retrieval
        public Dictionary<string, TVService> GetAllServices(bool mergeLineUps, bool includeInternetTV, bool includeHiddenChannels, bool blockUserHidden, bool blockUserAdded, bool blockUserMapped, bool blockUnknown, bool DebugChannelList)
        {
            List<Channel> channels = GetChannelsInFirstLineup(mergeLineUps);
            List<TVService> lstOutput = new List<TVService>();

            foreach (Channel c in channels)
            {
                if (c == null) continue;

                bool allowChannel = true;
                ChannelType ct = c.ChannelType;

                if (DebugChannelList)
                {
                    Service svc = c.Service;
                    string svdID = (svc != null) ? svc.Id.ToString() : "<NULL>";
                    int svcType = (svc != null) ? svc.ServiceType : -1;
                    DebugNormal("Found channel:Callsign:" + c.CallSign + "|ChannelType:" + 
                        ct.ToString() + "|ServiceType:" + svcType.ToString() +  "|UserBlockedState:" + c.UserBlockedState.ToString() + 
                        "|Visibility:" + c.Visibility.ToString() + "|Number:" + c.ChannelNumber.ToString() +
                        "." + c.SubNumber.ToString() + "|ChanNumberPriority:" + c.ChannelNumberPriority.ToString() +
                        "|ServiceID:" + svdID  + "|ServiceString:" + svc.ToString() + ".");
                }


                if (!includeInternetTV)
                {
                    if (ct == ChannelType.WmisBroadband)
                        allowChannel = false;
                    //else if (ct == ChannelType.Wmis)  >> NO these are normal channels!
                    //    allowChannel = false;
                }

                if (!includeHiddenChannels)
                {
                    if ((c.UserBlockedState == UserBlockedState.Blocked) || (c.UserBlockedState == UserBlockedState.Disabled))
                        allowChannel = false;

                    if (c.IsSuggestedBlocked)
                        allowChannel = false;

                    if (c.Visibility == ChannelVisibility.NotTunable)
                        allowChannel = false;

                    if (c.Visibility == ChannelVisibility.Blocked)
                        allowChannel = false;
                }

                if (blockUserHidden)
                {
                    if (c.ChannelType == ChannelType.UserHidden)
                        allowChannel = false;
                }

                if (blockUserAdded)
                {
                    if (c.ChannelType == ChannelType.UserAdded)
                        allowChannel = false;
                }

                if (blockUserMapped)
                {
                    if (c.ChannelType == ChannelType.UserMapped)
                        allowChannel = false;
                }

                if (blockUnknown)
                {
                    if (c.ChannelType == ChannelType.Unknown)
                        allowChannel = false;
                }

                // CONVERT CHANNEL
                if (allowChannel)
                {
                    TVService tvs = Conversion.TVServiceFromChannel(c);
                    lstOutput.Add(tvs);                        
                }
                else
                {
                    if (DebugChannelList)     DebugNormal("(channel " + c.CallSign + " not added due to settings restrictions)");
                }
            }

            // Search the favorite line ups and add these into each TV service object
            AddFavoriteLineupsToTVServicesList(lstOutput);

            // Now sort by channel number
            lstOutput.Sort(new CommonEPG.Comparers.TVCServiceNumComparer());
            
            // Now add into dictionary, ensuring unique
            Dictionary<string, TVService> output = new Dictionary<string, TVService>();
            foreach (TVService tvs in lstOutput)
            {
                if (!output.ContainsKey(tvs.UniqueId))
                    output.Add(tvs.UniqueId, tvs);
                else
                    DebugNormal("Not adding duplicate service.  (Callsign:" + tvs.Callsign + ")");
            }

            return output;
        }
        void AddFavoriteLineupsToTVServicesList(List<TVService> services)
        {
            Lineup lu = Util.GetFirstLineup(os);
            if (lu == null) return;
            FavoriteLineups faves = lu.FavoriteLineups;
            if (faves == null) return;
            foreach (FavoriteLineup lineup in faves)
            {
                if ( (lineup == null) || (string.IsNullOrEmpty(lineup.Name ) ) ) continue;

                List<Channel> chans = lineup.Channels.ToList();
                foreach (Channel chan in chans)
                {
                    if (chan == null) continue;
                    if (! chan.IsLatestVersion) continue;

                    Service svc = chan.Service;
                    if (svc == null) continue;
                    if (!svc.IsLatestVersion) continue;

                    // Find the corresponding service in our list
                    foreach (TVService rpTVS in services)
                    {
                        if (long.Parse(rpTVS.UniqueId, NumberStyles.Any, CultureInfo.GetCultureInfo("EN-US")) == svc.Id)
                        {
                            rpTVS.AddToFavoriteLineUp(lineup.Name);
                            break;
                        }
                    }


                }

            }
        }

        bool ChannelListContainsCallsign(List<TVService> lstServices, string matchCallsign)
        {
            foreach (TVService tvs in lstServices)
            {
                if (tvs.Callsign.ToLowerInvariant().Equals(matchCallsign.ToLowerInvariant()))
                    return true;
            }
            return false;
        }
        /*
        private List<TVProgramme> GetTVProgrammesOnServiceWithID(DateRange dateRange, string serviceID, bool omitDescriptions, TVProgrammeType matchType)
        {
            string[] serviceIDs = new string[] { serviceID };
            return GetTVProgrammesOnServicesWithID(dateRange, serviceIDs, omitDescriptions, matchType);
        }
        private List<TVProgramme> GetTVProgrammesOnServicesWithID(DateRange dateRange, string[] serviceUIDs, bool omitDescriptions, TVProgrammeType matchType)
        {
            List<Service> services = new List<Service>();
            try
            {
                foreach (string serviceUID in serviceUIDs)
                {
                    long lID = long.Parse(serviceUID, NumberStyles.Any, CultureInfo.GetCultureInfo("EN-US"));
                    StoredObject so = os.Fetch(lID);
                    if (!(so is Service)) continue;
                    Service service = (Service)so;
                    services.Add(service);
                }
            }
            catch (Exception ex)
            {
                DebugError(ex);
                return null;
            }

            return GetTVProgrammesOnServices(dateRange, services, omitDescriptions, matchType);            
        }  */

        public void RemoveAllServicesFromRemotePotatoLineUp()
        {
            if (!DoesRemotePotatoLineUpExist())  return;

            FavoriteLineup rpFaveLineUp = RemotePotatoFavoriteLineup;
            if (rpFaveLineUp == null) return;

            try
            {
                rpFaveLineUp.ClearAllChannels();
            }
            catch (Exception ex)
            {
                DebugError("Error clearing all channels from remote potato favorites: ", ex);
            }
        }

        public void AddOrRemoveServicesToRemotePotatoLineUp(List<TVService> servicesToAdd, List<TVService> servicesToRemove)
        {
            if (!DoesRemotePotatoLineUpExist())
            {
                AddRemotePotatoLineUp();
            }
            
            FavoriteLineup rpFaveLineUp = RemotePotatoFavoriteLineup;
            if (rpFaveLineUp == null) return;

            Lineup defaultLineup = Util.GetFirstLineup(os);
            
            // CHANNELS TO ADD ***************
            List<StoredObject> channelsToAdd = new List<StoredObject>();
            if (servicesToAdd != null)
            {
                DebugError("going through " + servicesToAdd.Count.ToString() + " servicesTOAdd.");
                foreach (TVService rpTVS in servicesToAdd)
                {
                    DebugError("Next rpTVService:" + rpTVS.Callsign);

                    Service svc = GetServiceByID(rpTVS.UniqueId);
                    if (svc == null) continue;

                    DebugError("Found MC service." + svc.CallSign);
                    List<Channel> chansForService = defaultLineup.GetChannelsInLineup(svc);
                    if (chansForService.Count < 1) continue;

                    Channel chan = svc.GetBestChannel(chansForService);// Need this otherwise a 'ghost' channel can be allocated  i.e. not this: //Channel chan = chansForService[0];
                    if (chan == null) continue;
                    DebugError("Found chan." + chan.CallSign);

                    // Already in the list? 
                    if (rpFaveLineUp.Channels.Contains(chan)) continue;
                    DebugError("Chan not in lineup, adding to array.");

                    // Add the channel to the remote potato favorites line up
                    channelsToAdd.Add(chan);
                }
            }

            // CHANNELS TO REMOVE ***************
            List<StoredObject> channelsToRemove = new List<StoredObject>();
            if (servicesToRemove != null)
            {
                foreach (TVService rpTVS in servicesToRemove)
                {
                    Service svc = GetServiceByID(rpTVS.UniqueId);
                    if (svc == null) continue;

                    List<Channel> chansForService = defaultLineup.GetChannelsInLineup(svc);
                    if (chansForService.Count < 1) continue;

                    Channel chan = svc.GetBestChannel(chansForService);// Need this otherwise a 'ghost' channel can be allocated  i.e. not this: //Channel chan = chansForService[0];
                    if (chan == null) continue;

                    // Not in list?
                    if (!rpFaveLineUp.Channels.Contains(chan)) continue;

                    // Add the channel to the remote potato favorites line up
                    channelsToRemove.Add(chan);
                }
            }



            // Try to add them all together, by drilling down (unsure about this)
            try
            {
                rpFaveLineUp.Lock();
                // Perform Deletions
                if (channelsToRemove.Count > 0)
                {
                    foreach (StoredObject so in channelsToRemove)
                    {
                        rpFaveLineUp.Channels.RemoveAllMatching(so);
                    }
                }

                // Perform Additions
                if (channelsToAdd.Count > 0)
                {
                    rpFaveLineUp.Channels.Add(channelsToAdd);
                }

                rpFaveLineUp.Update();
                rpFaveLineUp.Unlock();
            }
            catch (Exception ex)
            {
                DebugError("Error adding or removing channels in remote potato favorites: ", ex);
            }
        }
        void AddRemotePotatoLineUp()
        {
            if (DoesRemotePotatoLineUpExist()) return; // already exists

            Lineup lu = Util.GetFirstLineup(os);
            
            FavoriteLineup newFave = new FavoriteLineup(false);
            newFave.Name = "Remote Potato Favorites";

            lu.AddFavoriteLineup(newFave);
        }
        FavoriteLineup RemotePotatoFavoriteLineup
        {
            get
            {

                Lineup lu = Util.GetFirstLineup(os);
                FavoriteLineups faves = lu.FavoriteLineups;
                foreach (FavoriteLineup lineup in faves)
                {
                    if (
                        (lineup != null) &&
                        (!string.IsNullOrEmpty(lineup.Name)) &&
                        lineup.Name.Equals("Remote Potato Favorites")
                    )
                        return lineup;
                }

                return null;
            }
        }
        public bool DoesRemotePotatoLineUpExist()
        {
            return DoesFavoriteLineUpExist("Remote Potato Favorites");
        }
        bool DoesFavoriteLineUpExist(string lineupName)
        {
            List<String> names = GetFavoriteLineUpNames();
            return (names.Contains(lineupName));
        }
        public List<string> GetFavoriteLineUpNames()
        {
            List<string> FavoriteLineUpNames = new List<string>();

            Lineup lu = Util.GetFirstLineup(os);
            FavoriteLineups faves = lu.FavoriteLineups;
            foreach (FavoriteLineup lineup in faves)
            {
                if (
                    (lineup != null) &&
                    (! string.IsNullOrEmpty(lineup.Name ) )
                    )
                    FavoriteLineUpNames.Add(lineup.Name);
            }

            return FavoriteLineUpNames;
        }
        public void AddRemotePotatoFavoriteLineUp()
        {
            FavoriteLineup flu = new FavoriteLineup(false);
            flu.Name = "Remote Potato Favorites";
            
        }

        Service GetServiceByID(string uid)
        {
            try
            {
                long lID = long.Parse(uid, NumberStyles.Any, CultureInfo.GetCultureInfo("EN-US"));  // allow +e notation
                StoredObject so = os.Fetch(lID);
                if (!(so is Service)) return null;
                return (Service)so;

            }
            catch (Exception ex)
            {
                DebugError(ex);
                return null;
            }
        }
        

        #endregion

        #region Programmes
        public List<TVProgramme> GetTVProgrammesUsingEPGRequests(List<EPGRequest> EPGrequests, bool omitDescriptions, TVProgrammeType matchType)
        {
            Stopwatch timePerformance = new Stopwatch();
            timePerformance.Start();

            List<TVProgramme> mainOutput = new List<TVProgramme>();


            ScheduleEntry[] schedEntries;
            Service service;
            foreach (EPGRequest epgRequest in EPGrequests)
            {
                // Get the service from the object store
                service = GetServiceByID(epgRequest.TVServiceID);
                if (service == null)
                {
                    DebugError("Could not find service with ID " + epgRequest.TVServiceID);
                    continue;  // break if service not found
                }

                List<TVProgramme> svcOutput = new List<TVProgramme>();

                // Get entries in time range using API function (about 8-95 times faster!)
                DateTime startTime = new DateTime(epgRequest.StartTime, DateTimeKind.Utc);
                DateTime stopTime = new DateTime(epgRequest.StopTime, DateTimeKind.Utc);
                schedEntries = service.GetScheduleEntriesBetween(startTime, stopTime);

                TVProgramme tvp;
                foreach (ScheduleEntry se in schedEntries)
                {
                    if (se == null) continue;

                    // Include programme?  (i.e. does it match the requested type)
                    bool includeMe;
                    if (matchType == TVProgrammeType.All)
                        includeMe = true;
                    else
                        includeMe = Conversion.isProgrammeType(se.Program, matchType);

                    if (includeMe)
                    {
                        tvp = Conversion.TVProgrammeFromScheduleEntry(se, omitDescriptions);
                        if (tvp != null)
                            svcOutput.Add(tvp);
                    }
                
                }

                // Sort each service's lineup by start time (prob not necessary)
                CommonEPG.Comparers.TVProgrammeStartTimeComparer comparer = new CommonEPG.Comparers.TVProgrammeStartTimeComparer();
                svcOutput.Sort(comparer);

                mainOutput.AddRange(svcOutput);
                
                svcOutput.Clear();
                svcOutput = null;
            }

            timePerformance.Stop();
            DebugNormal("Time to fetch EPG programmes on " + EPGrequests.Count.ToString() + " services: " + timePerformance.Elapsed.TotalMilliseconds.ToString() + "ms");

            return mainOutput;

        }
        public TVProgramme GetTVProgrammeWithUID(string progUID)
        {
            ScheduleEntry se;
            if (!GetScheduleEntryWithID(progUID, out se)) return null;

            TVProgramme tvp = Conversion.TVProgrammeFromScheduleEntry(se); 

            return tvp;
        }
        // Helper

        public TVProgrammeCrew GetTVProgrammeCrewFromTVProgrammeUID(string progUID)
        {
            ScheduleEntry se;
            if (!GetScheduleEntryWithID(progUID, out se)) return null;
            Program p = se.Program;
            if (p == null) return new TVProgrammeCrew();

            return Conversion.TVProgrammeCrewFromProgram(p);
        }
        public TVProgrammeInfoBlob GetInfoBlobForTVProgrammeUID(string progUID, List<string> considerServiceIDs)
        {
            ScheduleEntry se;
            if (!GetScheduleEntryWithID(progUID, out se)) return null;
            Program p = se.Program;
            if (p == null) return null;

            TVProgrammeInfoBlob infoblob = new TVProgrammeInfoBlob();
            infoblob.Description = p.Description;  // Also grab programme description - if client is running in turbo mode it will not have this
            infoblob.TVProgrammeId = p.Id.ToString();
            infoblob.Crew = Conversion.TVProgrammeCrewFromProgram(p);
            
            // Other showings in this series
            SeriesInfo series = p.Series;
            if (series != null)
            {
                List<TVProgramme> seriesTVProgrammes = new List<TVProgramme>();
                Programs seriesPrograms = series.Programs;
                if (seriesPrograms != null)
                {
                    TVProgramme seriesProg;
                    foreach (Program seriesProgram in seriesPrograms)
                    {
                        foreach (ScheduleEntry seriesProgramScheduleEntry in seriesProgram.ScheduleEntries)
                        {
                            if (seriesProgramScheduleEntry == null) continue;

                            if (!seriesProgramScheduleEntry.IsLatestVersion) continue;
                            seriesProg = Conversion.TVProgrammeFromScheduleEntry(seriesProgramScheduleEntry);
                            if (seriesProg != null) seriesTVProgrammes.Add(seriesProg);
                        }
                    }
                }

                infoblob.OtherShowingsInSeries = FilterTVProgrammeListByServices(seriesTVProgrammes, considerServiceIDs);
            }

            // Other showings of this program
            if (p.ScheduleEntries != null)
            {
                List<TVProgramme> programShowings = new List<TVProgramme>();
                TVProgramme otherProg;
                foreach (ScheduleEntry programScheduleEntry in p.ScheduleEntries)
                {
                    if (programScheduleEntry == null) continue;

                    if (!programScheduleEntry.IsLatestVersion) continue;
                    otherProg = Conversion.TVProgrammeFromScheduleEntry(programScheduleEntry);
                    if (otherProg != null) programShowings.Add(otherProg);
                }
                infoblob.OtherShowingsOfThis = FilterTVProgrammeListByServices(programShowings, considerServiceIDs);
            }


            return infoblob;
        }
        /// <summary>
        /// Filter a list of TV programmes to return only those on a list of designated services
        /// Also strips out any null items
        /// </summary>
        private List<TVProgramme> FilterTVProgrammeListByServices(List<TVProgramme> inputList, List<string> considerServiceIDs)
        {
            // Validate
            if (inputList == null) return new List<TVProgramme>();
            if (considerServiceIDs == null) return inputList;
            if (considerServiceIDs.Count < 1) return inputList;

            // Filter the list
            List<TVProgramme> outputList = new List<TVProgramme>();

            // Some of these may be null, strip these out
            foreach (TVProgramme tvp in inputList)
            {
                if (tvp == null) continue;  // strip out null programmes too

                if (considerServiceIDs.Contains(tvp.ServiceID))
                    outputList.Add(tvp);
            }

            return outputList;
        }
        // Private - medium level Object Store 
        private bool GetScheduleEntryWithID(string progUID, out ScheduleEntry se)
        {
            se = null;
            long UID;
            if (!long.TryParse(progUID, NumberStyles.Any, CultureInfo.GetCultureInfo("EN-US"), out UID)) return false;

            StoredObject so = os.Fetch(UID);
            if (!(so is ScheduleEntry)) return false;

            se = (ScheduleEntry)so;
            return true;
        }
        private List<Channel> GetChannelsInFirstLineup(bool mergeLineups)
        {
            List<Channel> output = new List<Channel>();

            if (mergeLineups)
            {
                MergedLineups ml = new MergedLineups(os);
                foreach (Lineup lu in ml)
                {
                    if (lu == null)
                    {
                        DebugNormal("Null lineup found within mergedLineups");
                        continue;
                    }

                    Channel[] channels = lu.GetChannels();
                    foreach (Channel c in channels)
                    {
                        if (c != null)
                            output.Add(c);
                    }
                }
            }
            else
            {
                Lineup lu = Util.GetFirstLineup(os);
                if (lu == null)
                {
                    DebugNormal("No lineup was found.");
                    return output;
                }
                else
                {
                    Channel[] channels = lu.GetChannels();
                    foreach (Channel c in channels)
                    {
                        if (c != null)
                            output.Add(c);
                    }
                }
                    
            }

            return output;
        }
        #endregion

        // Debug
        void DebugNormal(string msg)
        {
            if (DebugReport != null) DebugReport(this, new DebugReportEventArgs(msg, 10, null));
        }
        void DebugInfoOnly(string msg)
        {
            if (DebugReport != null) DebugReport(this, new DebugReportEventArgs(msg, 1, null));
        }
        void DebugError(Exception ex)
        {
            DebugError(ex.Message, ex);
        }
        void DebugError(string msg)
        {
            if (DebugReport != null) DebugReport(this, new DebugReportEventArgs(msg, 50, null));
        }
        void DebugError(string msg, Exception ex)
        {
            if (DebugReport != null) DebugReport(this, new DebugReportEventArgs(msg, 50, ex));
        }
    }
}
