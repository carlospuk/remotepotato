using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Shapes;
using System.Collections.Generic;
using RemotePotatoServer;
using CommonEPG;

namespace SilverPotato
{
    public class EPGCacheRetriever
    {
        // Some static variables to limit web retrievals
     //   static int currentWebCalls;
     //   static const int MaxSimultaneousWebCalls = 10;

        public event EventHandler<GenericEventArgs<EPGCacheRetrieverResult>> GetProgrammesForEPGRequestCompleted;
        public event EventHandler<GenericEventArgs<EPGCacheRetrieverResult>> GetProgrammesForEPGRequest_WillGetFromServer;
        private EPGRequest gettingEPGRequest;
        
        // Cache
        public EPGCacheRetriever()
        {
            // Events
        }
        public void GetProgrammesForEPGRequest(EPGRequest rq)
        {
            gettingEPGRequest = rq;
            if (Settings.SilverlightCacheEPGDays)
            {
                if (Settings.DebugCache) Functions.WriteLineToLogFile("Checking in cache for epg request: " + rq.CacheUniqueFilename);
                if (EPGCache.isCached(rq))
                {
                    if (Settings.DebugCache) Functions.WriteLineToLogFile("EPGREquest " + rq.CacheUniqueFilename + "is cached - fetching from cache.");
                    // It's cached, return the service slices
                    EPGCacheRetrieverResult result = new EPGCacheRetrieverResult();
                    result.Source = EPGCacheRetrieverSourceTypes.FromCache;
                    result.Output = EPGCache.getFromCache(rq);
                    if (GetProgrammesForEPGRequestCompleted != null)
                        GetProgrammesForEPGRequestCompleted(this, new GenericEventArgs<EPGCacheRetrieverResult>(result));
                    return;
                }
            }            

            // It's not cached, so fetch the data from the server
            if (Settings.DebugCache) Functions.WriteLineToLogFile("EPGRequest " + rq.CacheUniqueFilename + "not found in cache - fetching from server.");
            EPGImporter importer = new EPGImporter();
            importer.GetProgrammesForEPGRequestsAsZipStringCompleted += new EventHandler<GenericEventArgs<string>>(importer_GetProgrammesForEPGRequestsAsZipStringCompleted);
            importer.GetProgrammesForEPGRequestsAsZipString(new List<EPGRequest>() { rq });
            

            // Notify that there will be a delay, e.g. so the strip can draw a 'loading' text box.
            if (GetProgrammesForEPGRequest_WillGetFromServer != null)
            {
                EPGCacheRetrieverResult wResult = new EPGCacheRetrieverResult();
                wResult.Source = EPGCacheRetrieverSourceTypes.FromServer;
                GetProgrammesForEPGRequest_WillGetFromServer(this, new GenericEventArgs<EPGCacheRetrieverResult>(wResult));
            }
        }

        void importer_GetProgrammesForEPGRequestsAsZipStringCompleted(object sender, GenericEventArgs<string> e)
        {
       
            // Empty string if it didn't work
            if (String.IsNullOrEmpty(e.Value))
            {
                Functions.WriteLineToLogFile("EPG CacheRetriever: Received null string from EPG importer.");
                EPGCacheRetrieverResult result = new EPGCacheRetrieverResult();
                result.Success = false;
                GetProgrammesForEPGRequestCompleted(null, new GenericEventArgs<EPGCacheRetrieverResult>(result));
                return;
            }

            // Cache the zipped string direct to disk
            if (Settings.SilverlightCacheEPGDays)
            {
                if (Settings.DebugCache) Functions.WriteLineToLogFile("Storing slice " + gettingEPGRequest.CacheUniqueFilename + "in cache.");
                EPGCache.storeInCache(gettingEPGRequest, e.Value);
            }

            // Convert
            List<TVProgramme> output = EPGCache.ZipStringToTVProgrammesList(e.Value);

            // Complete
            EPGCacheRetrieverResult cResult = new EPGCacheRetrieverResult();
            cResult.Output = output;
            cResult.Source = EPGCacheRetrieverSourceTypes.FromServer;
            GetProgrammesForEPGRequestCompleted(this, new GenericEventArgs<EPGCacheRetrieverResult>(cResult));
        }
        

    }

    public class EPGCacheRetrieverResult
    {
        public EPGCacheRetrieverResult() {
            Success = true;
        }

        public bool Success { get; set; }
        public List<TVProgramme> Output { get; set; }
        public EPGCacheRetrieverSourceTypes Source { get; set; }
        
    }

    public enum EPGCacheRetrieverSourceTypes
    {
        FromCache,
        FromServer,
        Unknown
    }
}
