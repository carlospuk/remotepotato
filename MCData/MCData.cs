using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using CommonEPG;
using Microsoft.MediaCenter.Guide;
using Microsoft.MediaCenter.Store;
using Microsoft.MediaCenter.Pvr;

/*
 * This is the Main class and calls down to lower-level methods within the ObjectStoreManager
 * that retrieve EPG data, schedules, etc and convert them to their Remote Potato equivalents
 */

namespace CommonEPG
{
    public class MCData : IDisposable
    {
        // Class Members
        ObjectStoreManager storeManager;
        
        // Events
        public event EventHandler<DebugReportEventArgs> DebugReport;
        public event EventHandler RecordingsDataNeedsRefresh;

        #region Init / Construct / Dispose
        public bool Initialize(ref string key)
        {
            if (key != "Wilkinson") return false;

            DebugNormal("Initialising Store.");
            storeManager = new ObjectStoreManager();
            storeManager.DebugReport += new EventHandler<DebugReportEventArgs>(storeManager_DebugReport);
            // Initialise Store Manager
            return storeManager.Init();
        }
        public void Dispose()
        {
            storeManager.Dispose();
            storeManager = null;
        }
        public void FreeResources()
        {
            // nothing to do
        }
        #endregion

        #region Channels 
        public Dictionary<string, TVService> GetChannels(bool mergeLineups, bool includeInternetTV, bool includeBlockedChannels,  bool blockUserHidden, bool blockUserAdded, bool blockUserMapped, bool blockUnknown, bool DebugChannelList)
        {
            DebugNormal("Getting list of channels");

            return storeManager.GetAllServices(mergeLineups, includeInternetTV, includeBlockedChannels,  blockUserHidden, blockUserAdded, blockUserMapped, blockUnknown, DebugChannelList);
        }
        public List<string> GetFavoriteLineUpNames()
        {
            return storeManager.GetFavoriteLineUpNames();
        }
        // Faves
        public void RemoveAllServicesFromRemotePotatoLineUp()
        {
            storeManager.RemoveAllServicesFromRemotePotatoLineUp();
        }
        public void AddAndRemoveServicesInRemotePotatoLineUp(List<TVService> servicesToAdd, List<TVService> servicesToRemove)
        {
            storeManager.AddOrRemoveServicesToRemotePotatoLineUp(servicesToAdd, servicesToRemove);
        }
        public void AddServicesToRemotePotatoLineUp(List<TVService> services)
        {
            storeManager.AddOrRemoveServicesToRemotePotatoLineUp(services, null);
        }
        public void RemoveServicesFromRemotePotatoLineUp(List<TVService> services)
        {
            storeManager.AddOrRemoveServicesToRemotePotatoLineUp(null, services);
        }
        #endregion

        #region Programmes
        public TVProgramme GetTVProgramme(string programmeId)
        {
            return storeManager.GetTVProgrammeWithUID(programmeId);
        }
        
        public List<TVProgramme> GetTVProgrammes(string channelId, bool omitDescriptions)
        {
            return GetTVProgrammes(null, channelId, omitDescriptions);
        }
        public List<TVProgramme> GetTVProgrammes(DateRange dateRange, string channelId, bool omitDescriptions)
        {
            return GetTVProgrammes(dateRange, new string[] { channelId }, omitDescriptions, TVProgrammeType.All);
        }
        public List<TVProgramme> GetTVProgrammes(DateRange dateRange, string[] channelIds, bool omitDescriptions, CommonEPG.TVProgrammeType matchType)
        {
            List<EPGRequest> requests = new List<EPGRequest>();
            foreach (string channelID in channelIds)
            {
                EPGRequest rq = new EPGRequest(channelID, dateRange);
                requests.Add(rq);
            }

            return GetTVProgrammes(requests, omitDescriptions, matchType);
        }
        public List<TVProgramme> GetTVProgrammes(List<EPGRequest> EPGrequests, bool omitDescriptions, CommonEPG.TVProgrammeType matchType)
        {
            return storeManager.GetTVProgrammesUsingEPGRequests(EPGrequests, omitDescriptions, matchType);
        }
        public TVProgrammeCrew GetTVProgrammeCrewFromTVProgrammeUID(string UID)
        {
            return storeManager.GetTVProgrammeCrewFromTVProgrammeUID(UID);
        }
        public TVProgrammeInfoBlob GetInfoBlobForTVProgrammeUID(string progUID, List<string>considerServiceIDs)
        {
            return storeManager.GetInfoBlobForTVProgrammeUID(progUID, considerServiceIDs );
        }
        #endregion

        #region Recordings
        public void Test()
        {
            List<RPRequest> requests = GetAllRequests(new DateRange(DateTime.Now.ToUniversalTime(), DateTime.Now.ToUniversalTime().AddDays(5)));
        }
        // Retrieve
        public List<RPRecording> GetAllRecordingsForRequests(List<RPRequest> ExistingRequests, DateRange dateRange)
        {
            return storeManager.GetAllRecordingsForRequests(ExistingRequests, dateRange);
        }
        public List<RPRequest> GetAllRequests(DateRange dateRange)
        {
            return storeManager.GetAllRequests(dateRange);
        }
        public RPRequest GetRPRequestWithID(long ID)
        {
            return storeManager.GetRPRequestWithID(ID);
        }
        // Action - Cancel
        public bool CancelRequest(long requestID)
        {
            bool result = storeManager.CancelRequest(requestID);
            if (result)
                if (RecordingsDataNeedsRefresh != null) RecordingsDataNeedsRefresh(this, new EventArgs());
            return result;
        }
        // Action - Cancel
        public bool CancelRecording(long recordingID)
        {
            bool result = storeManager.CancelRecording(recordingID);
            if (result)
                if (RecordingsDataNeedsRefresh != null) RecordingsDataNeedsRefresh(this, new EventArgs());
            return result;
        }
        #endregion

        #region Schedule Recordings
        
        public bool ScheduleRecording(RecordingRequest rr, out RPRequest rpRequest, out RecordingResult earlyFailureResult)
        {
            EventWaitHandle ewhScheduleRecording = new EventWaitHandle(false, EventResetMode.AutoReset);
            storeManager.ScheduleRecording(rr, ewhScheduleRecording);
            ewhScheduleRecording.WaitOne(TimeSpan.FromSeconds(500));

            // Destroy the stargate
            ewhScheduleRecording = null;

            if (!storeManager.ScheduleInitialSucceeded)
            {
                rpRequest = null;
                earlyFailureResult = storeManager.ScheduleInitialFailureResult;
                return false;
            }
            else
            {
                // Retrieve the shared objects that were generated
                rpRequest = Conversion.RPRequestFromRequest(storeManager.requestInProgress);  // THIS IS ERRORING

                earlyFailureResult = null;
            }

            return true;
        }
        public RecordingResult DetermineRecordingResultForRequest(RPRequest rpreq)
        {
            return storeManager.DetermineIfRequestSucceeded(rpreq);
        }

        #endregion

        #region Search
        public List<TVProgramme> SearchTVProgrammes(string searchText, EPGSearchTextType searchTextType, EPGSearchMatchType searchMatchType, out bool resultsWereTruncated, string[] serviceIDs)
        {
            DateRange relevantTime = new DateRange(DateTime.Now.ToUniversalTime(), DateTime.Now.ToUniversalTime().AddDays(40));
            return SearchTVProgrammesByDateRange(relevantTime, searchText, searchTextType, searchMatchType, out resultsWereTruncated, serviceIDs);
        }
        public List<TVProgramme> SearchTVProgrammesByDateRange(DateRange dateRange, string searchText, EPGSearchTextType searchTextType, EPGSearchMatchType searchMatchType, out bool resultsWereTruncated, string[] serviceIDs)
        {
            return storeManager.SearchTVProgrammesByDateRange(dateRange, searchText, searchTextType, searchMatchType, out resultsWereTruncated, serviceIDs);
        }
        #endregion

        // Debug
        void storeManager_DebugReport(object sender, DebugReportEventArgs e)
        {
            if (DebugReport != null) DebugReport(this, e);
        }
        void DebugNormal(string msg)
        {
            if (DebugReport != null)
                DebugReport(this, new DebugReportEventArgs(msg, 0, null));
        }

    }
}
