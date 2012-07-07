using System;
using System.Net;
using System.Windows;
using CommonEPG;

namespace SilverPotato
{
    public class MusicAlbumClickItem : ClickItem
    {
        // Members
        public RPMusicAlbumClickItemTextFormat TextFormat { set; get; }
        public RPMusicAlbum LinkedDataItem;

        public enum RPMusicAlbumClickItemTextFormat
        {
            TitleOnly,
            ArtistAndTitle,
            TitleAndArtist
        }

        // Constructor
        public MusicAlbumClickItem()
        {
            LinkedDataItem = null;
            DisableRecordDots();  // no record dots on piccies
        }
        public MusicAlbumClickItem(int index, RPMusicAlbum dataItem, RPMusicAlbumClickItemTextFormat format, ClickItemLayouts layout, double _Size)
            : this()
        {
            ThumbnailWidth = _Size;
            ThumbnailHeight = _Size;
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
                case RPMusicAlbumClickItemTextFormat.TitleOnly:
                    txtLabelText = LinkedDataItem.Title;
                    break;

                case RPMusicAlbumClickItemTextFormat.ArtistAndTitle:
                    if (LinkedDataItem.IsPseudoAlbum() ) // special case - psuedo-album
                        txtLabelText = LinkedDataItem.Title;
                    else
                        txtLabelText = LinkedDataItem.ArtistName() + " - " + LinkedDataItem.Title;
                    break;

                case RPMusicAlbumClickItemTextFormat.TitleAndArtist:
                    if (LinkedDataItem.IsPseudoAlbum()) // special case - psuedo-album
                        txtLabelText = LinkedDataItem.Title;
                    else   
                        txtLabelText = LinkedDataItem.Title + " - " + LinkedDataItem.ArtistName();
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
            SetThumbnailTo(LinkedDataItem.ThumbnailUriOrNull("medium"));
        }


    }
}
