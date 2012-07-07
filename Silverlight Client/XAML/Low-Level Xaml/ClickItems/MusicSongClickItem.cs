using System;
using System.Net;
using System.Windows;
using CommonEPG;

namespace SilverPotato
{
    public class MusicSongClickItem : ClickItem
    {
        // Members
        public RPMusicSongClickItemTextFormat TextFormat { set; get; }
        public RPMusicSong LinkedDataItem;
        public bool ShowTrackNumber;

        public enum RPMusicSongClickItemTextFormat
        {
            TitleOnly,
            ArtistAndTitle,
            TitleAndDuration,
            ArtistAndTitleAndDuration
        }

        // Constructor
        public MusicSongClickItem()
        {
            LinkedDataItem = null;
            DisableRecordDots();  // no record dots on piccies
        }
        public MusicSongClickItem(int index, RPMusicSong dataItem, RPMusicSongClickItemTextFormat format) : this(index, dataItem, format, false) {}
        public MusicSongClickItem(int index, RPMusicSong dataItem, RPMusicSongClickItemTextFormat format, bool _showTrackNumber)
            : this()
        {
            ThumbnailWidth = 200;
            ThumbnailHeight = 20; // dummies
            // Text Size
            SetTextOnlyFontSize(14);
            SetTextRightColumnWidth(38);
            // Store members
            ShowTrackNumber = _showTrackNumber;

            base.InitializeWithFormat(ClickItemLayouts.TextWithRightColumn);
            base.Index = index;

            // Store in local members
            TextFormat = format;
            LinkedDataItem = dataItem;
            
            LayoutFromLinkedDataItem();
        }

        public void LayoutFromLinkedDataItem()
        {
            BuildLabelText();
            HandleThumbnail();
        }

        private void BuildLabelText()
        {
            string txtLabelTextL = string.Empty;
            string txtLabelTextR = string.Empty;

            switch (TextFormat)
            {
                case RPMusicSongClickItemTextFormat.TitleOnly:
                    txtLabelTextL = LinkedDataItem.Title;
                    break;

                case RPMusicSongClickItemTextFormat.ArtistAndTitle:
                    txtLabelTextL = LinkedDataItem.ArtistName() + " - " + LinkedDataItem.Title;
                    break;

                case RPMusicSongClickItemTextFormat.ArtistAndTitleAndDuration:
                    txtLabelTextL = LinkedDataItem.ArtistName() + " - " + LinkedDataItem.Title;
                    txtLabelTextR = LinkedDataItem.ToPrettyDuration();
                    break;

                case RPMusicSongClickItemTextFormat.TitleAndDuration:
                    txtLabelTextL = LinkedDataItem.Title;
                    txtLabelTextR = LinkedDataItem.ToPrettyDuration();
                    break;

                default:
                    txtLabelTextL = "Unknown Title";
                    break;
            }

            // Add track number?
            if (ShowTrackNumber)
            {
                if (LinkedDataItem.TrackNumber > 0)
                    txtLabelTextL = LinkedDataItem.TrackNumber.ToString() + ". " + txtLabelTextL;
            }

            lblText.Text = txtLabelTextL;
            lblText.TextTrimming = TextTrimming.WordEllipsis;
            lblTextRight.Text = txtLabelTextR;
        }

        private void HandleThumbnail()
        {
            if (LinkedDataItem == null) return;
            SetThumbnailTo(LinkedDataItem.ThumbnailUriOrNull("small"));
        }


    }
}
