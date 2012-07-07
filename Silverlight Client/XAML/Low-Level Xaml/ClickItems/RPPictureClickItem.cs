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
    public class RPPictureItemClickItem : ClickItem
    {
        // Members
        public RPPictureClickItemTextFormat TextFormat { set; get; }
        public RPPictureItem LinkedPictureItem;

        // Constructor
        public RPPictureItemClickItem()
        {
            LinkedPictureItem = null;

            brdThumbnail.BorderBrush = null;
            imgThumbnail.Source = ImageManager.bmpThumbnailDefaultPictures;

            DisableRecordDots();  // no record dots on piccies
        }
        public RPPictureItemClickItem(int index, RPPictureItem picItem, RPPictureClickItemTextFormat format, ClickItemLayouts layout)
            : this(index, picItem, format, layout, 120, 90)
        { }
        public RPPictureItemClickItem(int index, RPPictureItem picItem, RPPictureClickItemTextFormat format, ClickItemLayouts layout, double _Width, double _Height)
            : this()
        {
            ThumbnailWidth = _Width;
            ThumbnailHeight = _Height;
            base.InitializeWithFormat(layout);
            base.Index = index;
            LinkedPictureItem = picItem;
            TextFormat = format;

            LayoutFromLinkedPictureItem();
        }

        public void LayoutFromLinkedPictureItem()
        {
            lblText.Text = BuildLabelText();
            HandleThumbnail();
        }

        private string BuildLabelText()
        {
            string txtLabelText ;


            switch (TextFormat)
            {
                case RPPictureClickItemTextFormat.TitleOnly:
                    txtLabelText = LinkedPictureItem.Title;
                    break;

                default:
                    txtLabelText = "Unknown Title";
                    break;
                

            }

           return txtLabelText;
            
        }

        private void HandleThumbnail()
        {
            if (LinkedPictureItem == null) return;
            if (LinkedPictureItem.FileName == null) return;

            SetThumbnailTo(LinkedPictureItem.ThumbnailUriOrNull("medium"));
        }

        public enum RPPictureClickItemTextFormat
        {
            TitleOnly,
        }

    }
}
