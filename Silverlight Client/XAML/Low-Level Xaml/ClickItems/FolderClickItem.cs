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
    public class FolderClickItem : ClickItem
    {
        // Members
        public BrowseItem LinkedBrowseItem;

        // Constructor
        public FolderClickItem()
        {
            LinkedBrowseItem = null;

            
            brdThumbnail.BorderBrush = null;
            brdThumbnail.Background = null;

            DisableRecordDots();  // no record dots on piccies
        }
        public FolderClickItem(int index, BrowseItem browseItem, ClickItemLayouts layout)
            : this(index, browseItem, layout, 100, 75)
        { }
        public FolderClickItem(int index, BrowseItem browseItem, ClickItemLayouts layout, double _Width, double _Height)
            : this()
        {
            ThumbnailWidth = _Width;
            ThumbnailHeight = _Height;
            base.InitializeWithFormat(layout);
            base.Index = index;
            LinkedBrowseItem = browseItem;

            if (
                (layout == ClickItemLayouts.TextOnly) ||
                (layout == ClickItemLayouts.TextWithRightColumn)
                )
                lblText.Foreground = new SolidColorBrush(Colors.White);
            else
                lblText.Foreground = new SolidColorBrush(Colors.Black);

            LayoutFromLinkedBrowseItem();
        }

        public void LayoutFromLinkedBrowseItem()
        {
            lblText.Text = BuildLabelText();
            HandleThumbnail();
        }

        private string BuildLabelText()
        {
            return Functions.finalPathComponentOfString(LinkedBrowseItem.Name);
        }

        private void HandleThumbnail()
        {
            SetThumbnailTo(ImageManager.LoadImageFromContentPath("/Images/imgFolder150x75.png"));            
        }




    }
}
