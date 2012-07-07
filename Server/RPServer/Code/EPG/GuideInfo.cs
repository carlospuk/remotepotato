using System;
using System.Web;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Serialization;
using System.Xml;
using RemotePotatoServer.Properties;
using System.Globalization;
using Microsoft.MediaCenter.TV.Scheduling;
using CommonEPG;

namespace RemotePotatoServer
{
    public static class GuideInfo
    {
        private static ICollection<ScheduleEvent> _cachedScheduleEvents;
        public static Dictionary<string, string> _cachedScheduledEventNames;
        public static List<ScheduleRequest> CachedScheduleRequests;
        public static EventSchedule es;

        // Property for event schedule
        public static EventSchedule TheEventSchedule
        {
            get { return GuideInfo.es; }
            set { GuideInfo.es = value; }
        }
        public static bool EventScheduleExists;

        // Events I can raise
        //public static event EventHandler<CommonEPG.DebugReportEventArgs> DebugReport;

        // Constructor / Init
        static GuideInfo()
        {
            // Init variables
            CachedScheduleRequests = new List<ScheduleRequest>();
            _cachedScheduledEventNames = new Dictionary<string, string>();
            
            try
            {
                es = new EventSchedule();
                EventScheduleExists = true;
            }
            catch (Exception e)
            {
                EventScheduleExists = false;
                Functions.WriteExceptionToLogFile(e);
            }
        }

        // Lookup Information
        public static bool CacheUpcomingScheduleRequests()
        {
            if (!EventScheduleExists)
                return false;

            CachedScheduleRequests  = ((ICollection<ScheduleRequest>)es.GetScheduleRequests()).ToList();
            return true;
        }
        public static List<ScheduleRequest> GetScheduleRequestsOfType(string[] _type)
        {
            List<ScheduleRequest> output = new List<ScheduleRequest>();

            foreach (string type in _type)
            {
                foreach (ScheduleRequest sr in CachedScheduleRequests)
                {
                    if ((string)sr.GetExtendedProperty("RequestType") == type)
                        output.Add(sr);
                }
            }

            return output;
        }
        public static bool CacheUpcomingScheduleEvents(int maxDaysInAdvance)
        {
            if (!EventScheduleExists)
                return false;

            _cachedScheduleEvents = es.GetScheduleEvents(DateTime.Now, DateTime.Now.AddDays(maxDaysInAdvance), Microsoft.MediaCenter.TV.Scheduling.ScheduleEventStates.All);
            return true;
        }
        public static List<ScheduleEvent> GetAllScheduleEventsDirect()
        {
            if (!EventScheduleExists)
                return new List<ScheduleEvent>();

            DateTime startWindow = DateTime.Now.ToUniversalTime().AddDays(-7);
            DateTime endWindow = DateTime.Now.ToUniversalTime().AddDays(31);
            return ((ICollection<ScheduleEvent>)es.GetScheduleEvents(startWindow, endWindow, ScheduleEventStates.All)).ToList();
        }
        public static List<ScheduleRequest> GetAllScheduleRequestsDirect()
        {
            if (!EventScheduleExists)
                return new List<ScheduleRequest>();

            DateTime startWindow = DateTime.Now.ToUniversalTime().AddDays(-7);
            DateTime endWindow = DateTime.Now.ToUniversalTime().AddDays(31);
            return ((ICollection<ScheduleRequest>)es.GetScheduleRequests()).ToList();
        }
        public static bool CachePastScheduleEvents(int maxDaysBack)
        {
            if (!EventScheduleExists)
                return false;

            _cachedScheduleEvents = es.GetScheduleEvents(DateTime.Now.AddDays(0 - maxDaysBack), DateTime.Now, Microsoft.MediaCenter.TV.Scheduling.ScheduleEventStates.All);
            return true;
        }
        public static bool CacheScheduleEventsForDateRange(DateTime _startWindow, DateTime _endWindow, ScheduleEventStates _state)
        {
            if (!EventScheduleExists)
                return false;

            _cachedScheduleEvents = es.GetScheduleEvents(_startWindow, _endWindow, _state);
            return true;
        }
        /// <summary>
        /// Store a keyed dictionary of scheduled events, keyed by name and start time
        /// </summary>
        /// <param name="_startWindow"></param>
        /// <param name="_endWindow"></param>
        /// <param name="_state"></param>
        /// <returns></returns>
        public static bool CacheRedDotRecordingsForDateRange(DateTime _startWindow, DateTime _endWindow, ScheduleEventStates _state)
        {
            if (!EventScheduleExists)
                return false;

            _cachedScheduledEventNames.Clear();

            ICollection<ScheduleEvent> events = es.GetScheduleEvents(_startWindow, _endWindow, _state);
            foreach (ScheduleEvent se in events)
            {
                try
                {
                    _cachedScheduledEventNames.Add(se.ToKey(), se.Id);
                }
                catch (Exception ex) {

                    Functions.WriteLineToLogFile("Error adding scheduled event to cached list:");
                    Functions.WriteExceptionToLogFile(ex);
                }  // in case of duplicates etc
            }
            return true;
        }
        public static bool CachedRedDotRecordingMatchesAndIsRecording(TVMovie tvm, ref bool isSeriesRecording)
        {
            foreach (CommonEPG.TVProgramme tvp in tvm.Showings)
            {
                if (CachedRedDotRecordingMatchesAndIsRecording(tvp, ref isSeriesRecording))
                    return true;
            }
            return false;
        }
        public static bool CachedRedDotRecordingMatchesAndIsRecording(CommonEPG.TVProgramme tvp, ref bool isSeriesRecording)
        {
            return CachedRedDotRecordingMatchesAndIsRecording(tvp.StartTime, tvp.Title, ref isSeriesRecording);
        }
        public static bool CachedRedDotRecordingMatchesAndIsRecording(long startTimeTicks, string title, ref bool isSeriesRecording)
        {
            string theKey = startTimeTicks.ToString();
            if (title.Length > 15)
                title = title.Substring(0, 15);
            theKey += title;

            return CachedRedDotRecordingMatchesAndIsRecording(theKey, ref isSeriesRecording);
        }
        public static bool CachedRedDotRecordingMatchesAndIsRecording(string eventKey, ref bool isSeriesRecording)
        {
            isSeriesRecording = false;

            if (!EventScheduleExists)
                return false;



            if (!_cachedScheduledEventNames.ContainsKey(eventKey))
                return false;


            string seID = _cachedScheduledEventNames[eventKey];

            
            // is this event recording?
            ScheduleEvent se = es.GetScheduleEventWithId(seID);
            if (se != null)
            {
                //eventID = se.Id;
                if ((se.State == ScheduleEventStates.WillOccur) | (se.State == ScheduleEventStates.IsOccurring) | (se.State == ScheduleEventStates.HasOccurred))
                {
                    ScheduleRequest sr = null;
                    try
                    {  sr = se.GetScheduleRequest(); // Get property from the schedule request if possible, to get series info and also avoid strain on maxRequests errors!
                       isSeriesRecording = ((string)sr.GetExtendedProperty("RequestType") == "Series");
                    }
                    catch { }  // no schedule request attached, so no series recording: dont flag this
                    return true;
                }
            }
            return false;
        }
        public static bool EventExistsInCachedEventsAtThisTime(DateTime exactTime, string withTitle, ref bool isSeriesRecording)
        {
            string strIgnore = "";
            return EventExistsInCachedEventsAroundThisTime(exactTime, 0, withTitle, ref isSeriesRecording, ref strIgnore);
        }
        public static bool EventExistsInCachedEventsAtThisTime(DateTime exactTime, string withTitle, ref bool isSeriesRecording, ref string eventID)
        {
            return EventExistsInCachedEventsAroundThisTime(exactTime, 0, withTitle, ref isSeriesRecording, ref eventID);
        }
        public static bool EventExistsInCachedEventsAroundThisTime(DateTime exactTime, int searchWindow, string withTitle, ref bool isSeriesRecording, ref string eventID)
        {
            if (_cachedScheduleEvents == null) return false;

            // Default
            isSeriesRecording = false;

            foreach (ScheduleEvent se in _cachedScheduleEvents)
            {
                // within search window
                TimeSpan timeDifference = (se.StartTime - exactTime.ToUniversalTime());
                if (Math.Abs(timeDifference.TotalMinutes) <= searchWindow)
                {
                    ScheduleRequest sr = null;
                    try
                    {
                        sr = se.GetScheduleRequest(); // Get property from the schedule request if possible, to get series info and also avoid strain on maxRequests errors!
                    }
                    catch
                    {
                    }

                    if (sr != null)
                    {
                        if ((string)sr.GetExtendedProperty("Title") == withTitle)
                        {
                            isSeriesRecording = ((string)sr.GetExtendedProperty("RequestType") == "Series");
                            eventID = se.Id;  // Store ID
                            return true;
                        }
                    }
                    else
                    {
                        try  // in case of exceeding allowed number of property queries
                        {
                            // No sched request, so use event
                            if ((string)se.GetExtendedProperty("Title") == withTitle)
                            {
                                eventID = se.Id;  // Store ID
                                return true;
                            }
                        }
                        catch { }  // poss Max requests error
                    }
                }
            }

            return false;
        }
        public static bool IsScheduleEventPartOfSeries(ScheduleEvent se, out string _requestid)
        {
            ScheduleRequest sr = null;
            _requestid = "";
            try
            {
                sr = se.GetScheduleRequest();
            }
            catch { return false; }

            if (sr == null)
                return false;

            if ((string)sr.GetExtendedProperty("RequestType")== "Series")
            {
                _requestid = sr.Id;
                return true;
            }

            return false;
        }
        public static List<ScheduleEvent> GetRecordedEventFromExactTime(DateTime exactTime)
        {
            List<ScheduleEvent> myEvents = new List<ScheduleEvent>();

            if (EventScheduleExists)
            {

                ICollection<ScheduleEvent> events = es.GetScheduleEvents(exactTime.ToUniversalTime() , exactTime.ToUniversalTime().AddMinutes(1), ScheduleEventStates.HasOccurred);

                foreach (ScheduleEvent se in events)
                {
                    myEvents.Add(se);
                }
            }

            return myEvents;
        }
        public static bool GetScheduleEventForTVProgramme(CommonEPG.TVProgramme tvp, ref ScheduleEvent matchingEvent, ref ScheduleRequest matchingRequest)
        {
            CommonEPG.TVService tvc = EPGManager.TVServiceWithIDOrNull(tvp.ServiceID);
            if (tvc == null) return false;

            return GetScheduleEventAtExactTimeOnChannel(tvp.StartTimeDT(), tvc.Callsign, false, "", ref matchingEvent, ref matchingRequest);
        }
        public static bool GetScheduleEventAtExactTimeOnChannel(DateTime exactTime, string callsign, bool matchTitleStart, string txtTitleStartsWith, ref ScheduleEvent matchingEvent, ref ScheduleRequest matchingRequest)
        {
            // We have a channel to search for, what's the service ID
            string svcID = EPGManager.ServiceIDFromCallsign(callsign);

            // So now bother to get the events
            List<ScheduleEvent> theList = GetScheduleEventsAroundExactTime(exactTime, 3);

            foreach (ScheduleEvent se in theList)
            {
                if (matchTitleStart)
                {
                    string seTitle = (string)se.GetExtendedProperty("Title");
                    if (!seTitle.ToLowerInvariant().StartsWith(txtTitleStartsWith.ToLowerInvariant()))
                        continue;  // ie Do not match
                }

                if ((string)se.GetExtendedProperty("ServiceID") == svcID)
                {
                    matchingEvent = se;

                    // Match schedule request, if there is one
                    try
                    {
                        matchingRequest = se.GetScheduleRequest();
                    }
                    catch
                    {
                        matchingRequest = null;
                    }

                    return true;
                }
            }

            return false; // no match
        }
        public static List<ScheduleEvent> GetScheduleEventsAtExactTime(DateTime exactTime)
        {
            return GetScheduleEventsAroundExactTime(exactTime, 0);
        }
        public static List<ScheduleEvent> GetScheduleEventsAroundExactTime(DateTime exactTime, int leaway)
        {
            List<ScheduleEvent> myEvents = new List<ScheduleEvent>();

            if (EventScheduleExists)
            {
                ICollection<ScheduleEvent> events = es.GetScheduleEvents(exactTime.ToUniversalTime().AddMinutes(0 - leaway), exactTime.ToUniversalTime().AddMinutes(leaway + 1), ScheduleEventStates.All); // At least one minute must be given

                foreach (ScheduleEvent se in events)
                {
                    myEvents.Add(se);
                }
            }

            return myEvents;
        }
        public static List<ScheduleEvent> GetPastScheduleEvents(int daysBehind)
        {
            List<ScheduleEvent> output = new List<ScheduleEvent>();

            if (!EventScheduleExists)
                return output;

            ICollection<ScheduleEvent> _pastScheduleEvents = es.GetScheduleEvents(DateTime.Now.ToUniversalTime().AddDays(0 - daysBehind), DateTime.Now, Microsoft.MediaCenter.TV.Scheduling.ScheduleEventStates.HasOccurred);

            foreach (ScheduleEvent se in _pastScheduleEvents)
            {
            
                output.Add(se);
            }
            
            return output;
        }
        /// <summary>
        /// Searches the pre-populated array of upcoming events to match ones on a certain date
        /// </summary>
        /// <param name="onThisDay">The date, in local time</param>
        /// <param name="MatchesState"></param>
        /// <returns></returns>
        /// 
        public static List<ScheduleEvent> GetScheduleEventsRecordingOrWillRecordOn(DateTime onThisDay)
        {
            List<ScheduleEvent> output = new List<ScheduleEvent>();
            foreach (ScheduleEvent se in GuideInfo._cachedScheduleEvents)
            {
                if ((se.StartTime.ToLocalTime().Date.Equals(onThisDay.Date))
                    & ((se.State == ScheduleEventStates.WillOccur) | (se.State == ScheduleEventStates.IsOccurring))
                    )
                    output.Add(se);
            }
            
            IComparer<ScheduleEvent> theComparer = new SEStartDateComparer();
            output.Sort(theComparer);

            return output;
        }
        public static List<ScheduleEvent> GetScheduleEventsOn(DateTime onThisDay)
        {
            List<ScheduleEvent> output = new List<ScheduleEvent>();
            foreach (ScheduleEvent se in GuideInfo._cachedScheduleEvents)
            {
                if ((se.StartTime.ToLocalTime().Date.Equals(onThisDay.Date))
                    )
                    output.Add(se);
            }

            IComparer<ScheduleEvent> theComparer = new SEStartDateComparer();
            output.Sort(theComparer);

            return output;
        }
        class SEStartDateComparer : IComparer<ScheduleEvent>
        {
            public int Compare(ScheduleEvent info1, ScheduleEvent info2)
            {                
                if (info1.StartTime > info2.StartTime) return 1;
                if (info1.StartTime < info2.StartTime) return -1;
                return 0;
            }
        }

        // Conversion / Lookup
        public static string ScheduleEventSafeTitle(ScheduleEvent se)
        {
            try
            {
                ScheduleRequest sr = se.GetScheduleRequest();
                if (sr != null) return (string)sr.GetExtendedProperty("Title");
            }
            catch
            {
            }

            return (string)se.GetExtendedProperty("Title");
        }


        // Helpers
        public static bool isRequestTypeSeries(ScheduleRequest sr)
        {
            if (sr == null) return false;

            return (((string)(sr.GetExtendedProperty("RequestType")) == "Series"));
        }
        public static string stringForScheduleEventState(ref ScheduleEvent se)
        {
            if (se.State == ScheduleEventStates.Canceled)
                return "Recording has been cancelled.";
            else if (se.State == ScheduleEventStates.Deleted)
                return "This program was recorded but has now been deleted.";
            else if (se.State == ScheduleEventStates.Conflict)
                return "Recording conflicts with another scheduled item.";
            else if (se.State == ScheduleEventStates.HasOccurred)
                return "This program has been recorded.";
            else if (se.State == ScheduleEventStates.Alternate)
                return "An alternative showing of this program has, or will been recorded.";
            else if (se.State == ScheduleEventStates.IsOccurring)
                return "This program is currently being recorded.";
            else if (se.State == ScheduleEventStates.Error)
                return "This program could not be recorded due to an error.";
            else if (se.State == ScheduleEventStates.WillOccur)
                return "This program will be recorded.";
            else
                return "No new recording will be made.";
        }
        public static string prettyTime(DateTime dt, DateTime enddt, bool includeTime)
        {
            string txtPT = prettyTime(dt, true);
            txtPT += " - " + enddt.ToShortTimeString();
            return txtPT;
        }
        public static string prettyTime(DateTime dt, bool includeTime)
        {
            string txtPT = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedDayName(dt.DayOfWeek) + " " + dt.Day.ToString() + " " + CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(dt.Month);
            if (includeTime) txtPT += ", " + dt.ToShortTimeString();
            return txtPT;
        }
        public static string imgTagForScheduleEvent(ScheduleEvent se)
        {
            // prepare source
            string src;

            // filename exists?
            string fN = (string)se.GetExtendedProperty("FileName");

            if (String.IsNullOrEmpty(fN))
            {
                // TODO: depend upon upcoming type...
                src = "/skin/thumbnail_default.png";
            }
            else
            {            
                src = "/rectvthumbnail?filename=" + HttpUtility.UrlEncode(fN);
            }


            // IMG Tag
            return "<img src=\"" + src + "\" class=\"showthumbnail\"/>";
        }
        public static string ToKey(this ScheduleEvent se)
        {
            string theKey = se.StartTime.Ticks.ToString();
            string title = (string)se.GetExtendedProperty("Title");
            if (title.Length > 15)
                title = title.Substring(0, 15);
            theKey += title;
            return theKey;
        }



    }
}
