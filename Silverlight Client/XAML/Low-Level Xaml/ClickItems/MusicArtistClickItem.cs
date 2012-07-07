using System;
using System.Net;
using System.Windows;
using CommonEPG;

namespace SilverPotato
{
    public class MusicArtistClickItem : ClickItem
    {
        // Members
        public RPMusicArtistClickItemTextFormat TextFormat { set; get; }
        public RPMusicArtist LinkedDataItem;

        public enum RPMusicArtistClickItemTextFormat
        {
            TitleOnly
        }

        // Constructor
        public MusicArtistClickItem()
        {
            LinkedDataItem = null;
            DisableRecordDots();  // no record dots on piccies
        }
        public MusicArtistClickItem(int index, RPMusicArtist dataItem, RPMusicArtistClickItemTextFormat format, ClickItemLayouts layout, double _Width, double _Height)
            : this()
        {
            ThumbnailWidth = _Width;
            ThumbnailHeight = _Height;
            base.InitializeWithFormat(layout);
            base.Index = index;

            // Store in local members
            TextFormat = format;
            LinkedDataItem = dataItem;
            
            LayoutFromLinkedDataItem();
        }

        public void LayoutFromLinkedDataItem()
        {
            lblText.Text = BuildLabelText();
            HandleThumbnail();
        }

        private string BuildLabelText()
        {
            string txtLabelText;


            switch (TextFormat)
            {
                case RPMusicArtistClickItemTextFormat.TitleOnly:
                    txtLabelText = LinkedDataItem.Name;
                    break;

                default:
                    txtLabelText = "Unknown Title";
                    break;


            }

            return txtLabelText;

        }

        private void HandleThumbnail()
        {
            if (LinkedDataItem == null) return;
            SetThumbnailTo(LinkedDataItem.ThumbnailUriOrNull());
        }


    }
}
