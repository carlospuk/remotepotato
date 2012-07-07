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
using System.Collections.Generic;
using CommonEPG;

namespace SilverPotato
{
    public class RPRequestClickItem : ClickItem
    {
        // Members
        public SeriesRequestClickItemTextFormat TextFormat { set; get; }
        public RPRequest LinkedRequest;

        // Constructor
        public RPRequestClickItem()
        {
            LinkedRequest = null;
        }
        public RPRequestClickItem(RPRequest rq, SeriesRequestClickItemTextFormat format, ClickItemLayouts layout)
            : this()
        {
            base.InitializeWithFormat(layout);
            LinkedRequest = rq;
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

            TVService tvs;
            switch (TextFormat)
            {
                case SeriesRequestClickItemTextFormat.TitleAndChannel:
                    sb.Append(LinkedRequest.Title);
                    tvs = LinkedRequest.TVService();
                    if (tvs != null)
                        sb.Append(" (" + tvs.Callsign + ")");
                    break;

                case SeriesRequestClickItemTextFormat.TitleAndChannelAndType:
                    sb.Append(LinkedRequest.Title);
                    tvs = LinkedRequest.TVService();
                    if (tvs != null)
                        sb.Append(" (" + tvs.Callsign + ")");
                    sb.Append(" (" + LinkedRequest.RequestType.ToString() + ")");
                    break;
                    
                default:
                    sb.Append(LinkedRequest.Title);
                    break;
            }

            return sb.ToString();
            
        }
        private void HandleRecordingDot()
        {
            HandleRecordDotFor(FirstRecording);  // can pass NULL to this function
        }
        private void HandleThumbnail()
        {
            if (FirstRecording == null) return;

            SetThumbnailTo(FirstRecording.ThumbnailUriOrNull());
        }
        private RPRecording FirstRecording
        {
            get
            {
                List<RPRecording> recordings = LinkedRequest.Recordings();
                if (recordings == null) return null;
                if (recordings.Count < 1) return null;

                return recordings[0];
            }
        }

        public enum SeriesRequestClickItemTextFormat
        {
            TitleOnly,
            TitleAndChannel,
            TitleAndChannelAndType
        }

    }
}
