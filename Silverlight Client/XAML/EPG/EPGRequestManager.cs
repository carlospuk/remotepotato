using System;
using System.Net;
using System.Threading;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using CommonEPG;

namespace SilverPotato
{
    public class EPGRequestManager
    {
        int TotalNumberOfStripsToReport;
        int AwaitingNumberOfStripsToReport;
        Dictionary<EPGStrip, EPGRequest> EPGRequests;
        Dictionary<EPGCacheRetriever, EPGStrip> cacheRetrievers;

        public EPGRequestManager()
        {
            EPGRequests = new Dictionary<EPGStrip, EPGRequest>();
            
        }
        public void Initialize(int totalStrips)
        {
            TotalNumberOfStripsToReport = totalStrips;
            AwaitingNumberOfStripsToReport = totalStrips;
        }

        public event EventHandler<GenericEventArgs<EPGStrip>> EPGRequest_Available;
        public event EventHandler<GenericEventArgs<EPGStrip>> EPGRequest_WillGetFromServer;

        object DictionaryLock = new object();
        public void ReportEPGRequest(EPGStrip strip, EPGRequest request)
        {
            Monitor.Enter(DictionaryLock);
            EPGRequests.Add(strip, request);
            
            Functions.WriteLineToLogFile("EPG request reported for service " + request.TVServiceID + " - there are " + AwaitingNumberOfStripsToReport.ToString() + " strips left to report.");

            AwaitingNumberOfStripsToReport--;

            if (AwaitingNumberOfStripsToReport < 1)
                HandleEPGRequests();
            Monitor.Exit(DictionaryLock);
        }
        void HandleEPGRequests()
        {
            Functions.WriteLineToLogFile("All strips have reported.");
            cacheRetrievers = new Dictionary<EPGCacheRetriever, EPGStrip>();

            // Bridge (convert) to schedule slice info
            foreach (KeyValuePair<EPGStrip, EPGRequest> kvp in EPGRequests)
            {
                EPGRequest rq  = kvp.Value;

                // Store the cache retriever, paired with the epg strip
                EPGCacheRetriever cacheRetriever = new EPGCacheRetriever();
                cacheRetrievers.Add(cacheRetriever, kvp.Key);

                cacheRetriever.GetProgrammesForEPGRequestCompleted += new EventHandler<GenericEventArgs<EPGCacheRetrieverResult>>(cacheRetriever_GetProgrammesOnChannelsForScheduleSliceCompleted);
                cacheRetriever.GetProgrammesForEPGRequest_WillGetFromServer += new EventHandler<GenericEventArgs<EPGCacheRetrieverResult>>(cacheRetriever_GetProgrammesOnChannelsForScheduleSlice_WillGetFromServer);
                
                // Attempt to get the programmes for the EPG Request
                cacheRetriever.GetProgrammesForEPGRequest(rq);
            }

        }

        void cacheRetriever_GetProgrammesOnChannelsForScheduleSlice_WillGetFromServer(object sender, GenericEventArgs<EPGCacheRetrieverResult> e)
        {
            
             
            // Pair cache retriever to the channel strip
            EPGCacheRetriever retriever = (EPGCacheRetriever)sender;
            EPGStrip sourceStrip;
            if (!cacheRetrievers.TryGetValue(retriever, out sourceStrip)) return;

            if (EPGRequest_WillGetFromServer != null)
                EPGRequest_WillGetFromServer(this, new GenericEventArgs<EPGStrip>(sourceStrip));
            
        }

        void cacheRetriever_GetProgrammesOnChannelsForScheduleSliceCompleted(object sender, GenericEventArgs<EPGCacheRetrieverResult> e)
        {
            if (! e.Value.Success) return;
            if (sender == null) return;

            EPGCacheRetriever retriever = (EPGCacheRetriever)sender;

            // Pair cache retriever to the channel strip
            EPGStrip sourceStrip;
            if (! cacheRetrievers.TryGetValue(retriever, out sourceStrip) ) return;

            ScheduleManager.MergeIntoTVProgrammeStore(e.Value.Output, false); // there are cross-thread locks within this

            // The programmes are now in the store: the receiver must do this on the UI thread
            if (EPGRequest_Available != null)
                EPGRequest_Available(this, new GenericEventArgs<EPGStrip>(sourceStrip));
        }


        /*
         * 
            // Events
            cacheRetriever.GetProgrammesOnChannelsForScheduleSliceCompleted += new EventHandler<GenericEventArgs<List<TVServiceSlice>>>(cacheRetriever_GetProgrammesOnChannelsForDateCompleted);
         *  
         * //cacheRetriever.GetProgrammesOnChannelsForScheduleSlice(stripScheduleSliceInfo);            
        }        
        void cacheRetriever_GetProgrammesOnChannelsForDateCompleted(object sender, GenericEventArgs<List<TVServiceSlice>> e)
        {
            ScheduleManager.MergeIntoTVProgrammeStore(e.Value, false); // there are cross-thread locks within this

            // The programmes are now in the store: this must be done on the UI thread
            Dispatcher.BeginInvoke(FillFromProgrammeStore);
         * 
         * */

    }
}
