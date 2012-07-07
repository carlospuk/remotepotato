using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using CommonEPG;

namespace SilverPotato
{
    public class TVProgClickItem : ClickItem
    {
        // Members
        public TVProgClickItemTextFormat TextFormat { set; get; }
        public TVProgramme LinkedTVProgramme;

        // Constructor
        public TVProgClickItem()
        {
            LinkedTVProgramme = null;
        }
        public TVProgClickItem(TVProgramme tvp, TVProgClickItemTextFormat format, ClickItemLayouts layout)
            : this()
        {
            base.InitializeWithFormat(layout);
            LinkedTVProgramme = tvp;
            LinkedTVProgramme.Updated += new EventHandler(LinkedTVProgramme_Updated);
            TextFormat = format;

            LayoutFromLinkedTVProgramme();
        }

        void LinkedTVProgramme_Updated(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(LayoutFromLinkedTVProgramme );  //avoid cross-thread.  (ScheduleManager Merge into TV Prog store -> TV Prog update -> here)
        }
        public void LayoutFromLinkedTVProgramme()
        {
            lblText.Text = BuildLabelText();
            HandleRecordingDot();
            HandleThumbnail();
        }

        private string BuildLabelText()
        {
            string txtLabelText;
            TVService tvs = LinkedTVProgramme.TVService();
            if (tvs == null )
                return "Unknown Callsign."; 


            switch (TextFormat)
            {
                case TVProgClickItemTextFormat.TimeTitleThenChannel:
                    txtLabelText = LinkedTVProgramme.StartTimeDT().ToLocalTime().ToShortTimeString() + ": " +
                        LinkedTVProgramme.Title + " (" + tvs.Callsign + ")";
                    break;

                case TVProgClickItemTextFormat.TitleThenDateAndChannel:
                    txtLabelText = LinkedTVProgramme.Title + " (" + LinkedTVProgramme.ToPrettyDate() + ", " + tvs.Callsign + ")";                    
                    break;

                case TVProgClickItemTextFormat.TitleThenDayOfWeekThenDateAndChannel:
                    txtLabelText = LinkedTVProgramme.Title + " (" + LinkedTVProgramme.ToPrettyDayNameAndDate() + ", " + tvs.Callsign + ")";
                    break;

                case TVProgClickItemTextFormat.TitleThenNewlineThenDate:
                    txtLabelText = LinkedTVProgramme.Title + Environment.NewLine + LinkedTVProgramme.ToPrettyDate();                    
                    break;

                case TVProgClickItemTextFormat.DateAndTimeAndChannel:
                    txtLabelText = LinkedTVProgramme.ToPrettyDate() + ", " + LinkedTVProgramme.StartTimeDT().ToLocalTime().ToShortTimeString() + " (" + tvs.Callsign + ")";
                    break;

                case TVProgClickItemTextFormat.DayDateAndTimeAndChannel:
                    txtLabelText = LinkedTVProgramme.ToPrettyDayNameAndDate() + ", " + LinkedTVProgramme.StartTimeDT().ToLocalTime().ToShortTimeString() + " (" + tvs.Callsign + ")";
                    break;

                case TVProgClickItemTextFormat.DateTimeEpisodeTitleAndChannel:

                    txtLabelText = LinkedTVProgramme.ToPrettyDate() + ", " + LinkedTVProgramme.StartTimeDT().ToLocalTime().ToShortTimeString();
                    
                    // Episode title?
                    if (!(string.IsNullOrEmpty(LinkedTVProgramme.EpisodeTitle)))
                        txtLabelText += ": \"" + LinkedTVProgramme.EpisodeTitle + "\"";
                    
                    // Channel
                    txtLabelText += " (" + tvs.Callsign + ")";
                    break;

                case TVProgClickItemTextFormat.DateAndTime:
                    txtLabelText = LinkedTVProgramme.ToPrettyDate() + ", " + LinkedTVProgramme.StartTimeDT().ToLocalTime().ToShortTimeString();
                    break;

                case TVProgClickItemTextFormat.TitleOnly:
                    txtLabelText = LinkedTVProgramme.Title;
                    break;

                case TVProgClickItemTextFormat.TitleAndEpisodeTitle:
                    txtLabelText = LinkedTVProgramme.Title;
                    if (!string.IsNullOrEmpty(LinkedTVProgramme.EpisodeTitle))
                        txtLabelText += Environment.NewLine + "\"" + LinkedTVProgramme.EpisodeTitle + "\"";
                    break;

                default:
                    txtLabelText = LinkedTVProgramme.Title;
                    break;

            }

           return txtLabelText;
            
        }
        private void HandleRecordingDot()
        {
            RPRecording rec = LinkedTVProgramme.Recording();
            if (rec == null)
                BlankRecordDot();
            else
            {
                HandleRecordDotFor(rec);
            }
        }
        private void HandleThumbnail()
        {
            if (LinkedTVProgramme == null) return;
            if (LinkedTVProgramme.Filename == null) return;

            SetThumbnailTo(LinkedTVProgramme.ThumbnailUriOrNull());
        }

        public enum TVProgClickItemTextFormat
        {
            TimeTitleThenChannel,
            TitleOnly,
            TitleAndEpisodeTitle,
            TitleThenDateAndChannel,
            TitleThenDayOfWeekThenDateAndChannel,
            TitleThenNewlineThenDate,
            DateAndTimeAndChannel,
            DayDateAndTimeAndChannel,
            DateAndTime,
            DateTimeEpisodeTitleAndChannel
        }

    }
}
