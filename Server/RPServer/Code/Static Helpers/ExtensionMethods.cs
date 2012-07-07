using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using RemotePotatoServer.Properties;

namespace RemotePotatoServer
{
    public static class ExtensionMethods
    {
        // Extension methods
        // TV Programme
        public static DateTime StartTimeDT(this CommonEPG.TVProgramme tvp)
        {
            return new DateTime(tvp.StartTime, DateTimeKind.Utc);
        }
        public static DateTime StopTimeDT(this CommonEPG.TVProgramme tvp)
        {
            return new DateTime(tvp.StopTime, DateTimeKind.Utc);
        }
        public static DateTime OriginalAirDateDT(this CommonEPG.TVProgramme tvp)
        {
            return new DateTime(tvp.OriginalAirDate, DateTimeKind.Utc);
        }
        public static string ToPrettyStartStopLocalTimes(this CommonEPG.TVProgramme tvp)
        {
            return tvp.StartTimeDT().ToLocalTime().ToShortTimeString() + " - " + tvp.StopTimeDT().ToLocalTime().ToShortTimeString();
        }
        public static string ToPrettyDayNameAndDate(this CommonEPG.TVProgramme tvp)
        {
            return tvp.StartTimeDT().ToPrettyDayNameAndDate();
        }
        public static string ToPrettyDate(this CommonEPG.TVProgramme tvp)
        {
            return tvp.StartTimeDT().ToPrettyDate();
        }
        public static string MatchedChannelCallsign(this CommonEPG.TVProgramme tvp)
        {
            CommonEPG.TVService tvc = EPGManager.TVServiceWithIDOrNull(tvp.ServiceID);
            if (tvc == null) return "Unknown";
            return tvc.Callsign;
        }
        public static bool HasEndedYet(this CommonEPG.TVProgramme tvp)
        {
            return (tvp.StopTimeDT().ToLocalTime() <= DateTime.Now);
        }
        public static int DurationMinutes(this CommonEPG.TVProgramme tvp)
        {
            try
            {
                long lDuration = tvp.StopTime - tvp.StartTime;
                TimeSpan tDuration = new TimeSpan(lDuration);
                return Convert.ToInt32(tDuration.TotalMinutes);
            }
            catch
            {

                if (Settings.Default.DebugAdvanced)
                {
                    Functions.WriteLineToLogFile("Error [extension method error DurationMinutes] - could not get duration for show " + tvp.Title + " starts:" + tvp.StartTime.ToString() + " stops:" + tvp.StopTime.ToString());
                }
            }
            return 0;
        }
        public static bool IsRecommended(this CommonEPG.TVProgramme tvp)
        {
            return (tvp.StarRating > 6);
        }
        public static bool IsTopRated(this CommonEPG.TVProgramme tvp)
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
        // Date/Time
        public static string ToPrettyDayNameAndDate(this DateTime dt)
        {
            return CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedDayName(dt.ToLocalTime().DayOfWeek) + " " + dt.ToPrettyDate();
        }
        public static string ToPrettyDate(this DateTime dt)
        {
            return dt.ToLocalTime().Day.ToString() + " " + CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(dt.ToLocalTime().Month);
        }

        // TimeSpan
        public static string ToPrettyString(this TimeSpan span)
        {
            StringBuilder prettyOutput = new StringBuilder(15);

            if (span.Days > 0)
                prettyOutput.Append(span.Days.ToString() + " day" + span.Days.Plural() + ", ");

            if (span.Hours > 0)
                prettyOutput.Append(span.Hours.ToString() + " hour" + span.Hours.Plural() + " and ");

            if (span.Minutes > 0)
                prettyOutput.Append(span.Minutes.ToString() + " minute" + span.Minutes.Plural() + ".");

            if (prettyOutput.Length == 0)
                prettyOutput.Append("right about now");
            else  // ANY timespan below or equal to zero  (includes negative timespans)
                prettyOutput.Insert(0, "in ");

            return prettyOutput.ToString();
        }

        // Double
        public static string Plural(this int i)
        {
            return (i != 1) ? "s" : "";
        }

        // NameValueCollection
        public static bool HasParameter(this System.Collections.Specialized.NameValueCollection qs, string name)
        {
            return (qs.Get(name) != null);
        }
    }
}
