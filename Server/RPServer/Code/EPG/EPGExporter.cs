using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.IO;
using CommonEPG;
using System.Xml;
using System.Xml.Serialization;
using RemotePotatoServer.Properties;

namespace RemotePotatoServer
{
    static class EPGExporter
    {

        public static string AllChannelsAsXML()
        {
            List<TVService> TheChannels = new List<TVService>();
            foreach (TVService tvc in EPGManager.AllTVChannels.Values)
            {
                TheChannels.Add(tvc);
            }

            return XMLHelper.Serialize<List<TVService>>(TheChannels);
        }

        public static string EPGForLocalDate(DateTime localDate, List<string> TVChannelIDs, bool omitDescriptions, TVProgrammeType matchType)
        {
            DateTime startRange = localDate.Date.ToUniversalTime();
            int extraEPGOverspill = Convert.ToInt32( Settings.Default.SilverlightEPGOverspillHours );
            DateTime endRange = startRange.AddHours(24 + extraEPGOverspill);
            DateRange theRange = new DateRange(startRange, endRange);
            List<TVProgramme> tvProgs = EPGManager.mcData.GetTVProgrammes(theRange, TVChannelIDs.ToArray(), omitDescriptions, matchType);

            return XMLHelper.Serialize<List<TVProgramme>>(tvProgs);
        }

        public static string EPGForDaysRange(int startDaysAhead, int numberOfDays, List<string> TVChannelIDs, bool omitDescriptions, TVProgrammeType matchType)
        {
            DateTime startRange = DateTime.Now.Date.ToUniversalTime().AddDays(startDaysAhead);
            int extraEPGOverspill = Convert.ToInt32(Settings.Default.SilverlightEPGOverspillHours);
            DateTime endRange = startRange.AddDays(numberOfDays);
            endRange = endRange.AddHours(extraEPGOverspill);
            DateRange theRange = new DateRange(startRange, endRange);
            List<TVProgramme> tvProgs = EPGManager.mcData.GetTVProgrammes(theRange, TVChannelIDs.ToArray(), omitDescriptions, matchType);

            return XMLHelper.Serialize<List<TVProgramme>>(tvProgs);
        }

        /// <summary>
        /// Returns an XML'd epg for a date range, or a time range on a day, e.g. 2010-11-05 12:00 to 2010-11-05 15:00
        /// 
        /// </summary>
        /// <param name="startDaysAhead"></param>
        /// <param name="numberOfDays"></param>
        /// <param name="TVChannelIDs"></param>
        /// <param name="omitDescriptions"></param>
        /// <param name="matchType"></param>
        /// <returns></returns>
        public static string EPGForDateRange(DateTime startDateTime, DateTime endDateTime, List<string> TVChannelIDs, bool omitDescriptions, TVProgrammeType matchType)
        {
            DateTime utcStart = startDateTime.ToUniversalTime();
            DateTime utcEnd = endDateTime.ToUniversalTime();
            DateRange theRange = new DateRange(utcStart, utcEnd);
            List<TVProgramme> tvProgs = EPGManager.mcData.GetTVProgrammes(theRange, TVChannelIDs.ToArray(), omitDescriptions, matchType);

            return XMLHelper.Serialize<List<TVProgramme>>(tvProgs);
        }


        public static string EPGwithEPGRequests(List<EPGRequest> requests, bool omitDescriptions, TVProgrammeType matchType)
        {
            List<TVProgramme> tvProgs = EPGManager.mcData.GetTVProgrammes(requests, omitDescriptions, matchType);

            return XMLHelper.Serialize<List<TVProgramme>>(tvProgs);
        }

        /// <summary>
        /// Returns a recordings blob object with all recording requests and recordings inside.
        /// </summary>
        /// <returns></returns>
        public static string RecordingsBlobAsXML()
        {
            List<RPRequest> allRequests = EPGManager.AllRequests.Values.ToList();
            List<RPRecording> allRecordings = EPGManager.AllRecordings.Values.ToList();

            // Pull up all the relevant TV programmes that will record too.  
            List<TVProgramme> progsToRecord = new List<TVProgramme>();
            foreach (RPRecording rec in allRecordings)
            {
                TVProgramme tvp = rec.TVProgramme();
                if (tvp != null)
                    progsToRecord.Add(tvp);
            }

            RPRecordingsBlob newBlob = new RPRecordingsBlob(allRequests, allRecordings, progsToRecord);

            return XMLHelper.Serialize<RPRecordingsBlob>(newBlob);
        }
        public static string AllSettingsAsXML()
        {
            // Defaults would be...
            //foreach (System.Configuration.SettingsProperty prop in Properties.Settings.Default.Properties)

            // Current values
            SerializableDictionary<string, string> serialisedSettings = new SerializableDictionary<string, string>();
            foreach (System.Configuration.SettingsPropertyValue prop in Properties.Settings.Default.PropertyValues)
            {
                serialisedSettings.Add(prop.Name, (string)prop.SerializedValue);
            }

            return XMLHelper.Serialize<SerializableDictionary<string, string>>(serialisedSettings);
        }
        public static string TVProgrammesMatchingSearch(EPGSearch theSearch)
        {
            List<TVProgramme> matchedProgs = null;
            bool wereTruncated = false;
            //if (theSearch.LimitToDateRange)  TODO
            //    matchedProgs = EPGManager.mcData.SearchTVProgrammesByDateRange(theSearch.DateRange, theSearch.TextToSearch, theSearch.TextType, theSearch.MatchType, out wereTruncated);
            //else
                matchedProgs = EPGManager.SearchTVProgrammes(theSearch.TextToSearch, theSearch.TextType, theSearch.MatchType, out wereTruncated);

            return XMLHelper.Serialize<List<TVProgramme>>(matchedProgs);
        }
        public static string AllRecordedTVAsXML()
        {
            List<TVProgramme> recTVprogs = RecTV.Default.RecordedTVProgrammes.Values.ToList();

            return XMLHelper.Serialize<List<TVProgramme>>(recTVprogs);
        }


        public static string TVProgrammeInfoBlobForProgID(string progUID)
        {
            // TODO:  Move this up the chain so it's generated by the client
            List<string> ConsiderIDs = EPGManager.EPGDisplayedTVChannelsServiceIDs;
            if (Settings.Default.DebugAdvanced) Functions.WriteLineToLogFile("Fetching Infoblob and filtering to consider " + ConsiderIDs.Count.ToString() + " channels");

            TVProgrammeInfoBlob blob = EPGManager.mcData.GetInfoBlobForTVProgrammeUID(progUID, ConsiderIDs);
            if (blob == null)
            {
                // Return empty blob
                if (Settings.Default.DebugAdvanced) Functions.WriteLineToLogFile("MCData Infoblob was NULL - returning new blank blob");
                blob = new TVProgrammeInfoBlob();
                blob.Crew = null;  // no crew
                blob.OtherShowingsInSeries = new List<TVProgramme>();
                blob.OtherShowingsOfThis = new List<TVProgramme>();
                blob.TVProgrammeId = progUID;
            }
            else
                if (Settings.Default.DebugAdvanced) Functions.WriteLineToLogFile("MCData Infoblob returned OK.");

            return XMLHelper.Serialize<TVProgrammeInfoBlob>(blob);            
        }
    }
}
