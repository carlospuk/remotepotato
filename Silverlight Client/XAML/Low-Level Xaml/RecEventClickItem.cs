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
using System.Text;
using CommonEPG;

namespace SilverPotato
{
    public class RecEventClickItem : ClickItem
    {
        // Members
        public RecEventClickItemTextFormat TextFormat { set; get; }
        public TVRecordingEvent LinkedTVRecordingEvent;

        // Constructor
        public RecEventClickItem()
        {
            LinkedTVRecordingEvent = null;
        }
        public RecEventClickItem(TVRecordingEvent re, RecEventClickItemTextFormat format, ClickItemLayouts layout)
            : this(re, format, layout, false) { }
        public RecEventClickItem(TVRecordingEvent re, RecEventClickItemTextFormat format, ClickItemLayouts layout, bool disableRecordDot): this()
        {
            base.InitializeWithFormat(layout);
            if (disableRecordDot) base.DisableRecordDots();
            LinkedTVRecordingEvent = re;
            TextFormat = format;
            LayoutItem();
        }

        public void LayoutItem()
        {
            lblText.Text = BuildLabelText();
            HandleRecordingDot();
            HandleThumbnail();
        }

        private string BuildLabelText()
        {
            StringBuilder sb = new StringBuilder();

            DateTime dtStart = new DateTime(LinkedTVRecordingEvent.StartTime).ToLocalTime();
            switch (TextFormat)
            {
                case RecEventClickItemTextFormat.TimeAndTitleAndChannel:
                    sb.Append(dtStart.ToShortTimeString());
                    sb.Append(": " + LinkedTVRecordingEvent.Title);
                    sb.Append(" (" + LinkedTVRecordingEvent.ChannelCallsign + ")");
                    break;

                case RecEventClickItemTextFormat.DateAndTimeAndChannel:
                    sb.Append(dtStart.ToPrettyDayNameAndDate() + ", " + dtStart.ToShortTimeString());
                    sb.Append(" (" + LinkedTVRecordingEvent.ChannelCallsign + ")");
                    break;

                case RecEventClickItemTextFormat.DateAndTimeAndTitle:
                    sb.Append(dtStart.ToPrettyDayNameAndDate() + ", " + dtStart.ToShortTimeString());
                    sb.Append(": " + LinkedTVRecordingEvent.Title);
                    break;

                default:
                    sb.Append(LinkedTVRecordingEvent.Title);
                    break;
            }

            return sb.ToString();
            
        }
        private void HandleRecordingDot()
        {
            HandleRecordDotFor(LinkedTVRecordingEvent);
        }
        private void HandleThumbnail()
        {
             if (! String.IsNullOrEmpty(LinkedTVRecordingEvent.FileName))
                 SetThumbnailTo(LinkedTVRecordingEvent.ThumbnailUriOrNull());
        }

        public enum RecEventClickItemTextFormat
        {
            DateAndTimeAndTitle,
            TimeAndTitleAndChannel,
            DateAndTimeAndChannel,
            TitleOnly
        }

    }
}
