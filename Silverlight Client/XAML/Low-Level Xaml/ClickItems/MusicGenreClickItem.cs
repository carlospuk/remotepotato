using System;
using System.Net;
using System.Windows;
using CommonEPG;

namespace SilverPotato
{
    public class MusicGenreClickItem : ClickItem
    {
        // Members
        public RPMusicGenre LinkedDataItem;

        // Constructor
        public MusicGenreClickItem()
        {
            LinkedDataItem = null;
            DisableRecordDots();  // no record dots on piccies
        }
        public MusicGenreClickItem(int index, RPMusicGenre dataItem)
            : this()
        {
            ThumbnailWidth = 100;
            ThumbnailHeight = 20; // dummy values
            base.InitializeWithFormat(ClickItemLayouts.TextOnly);
            base.Index = index;

            // Store in local members
            LinkedDataItem = dataItem;
            
            LayoutFromLinkedDataItem();
        }

        public void LayoutFromLinkedDataItem()
        {
            lblText.Text = BuildLabelText();
        }

        private string BuildLabelText()
        {
            string txtLabelText;

            txtLabelText = LinkedDataItem.Name;      

            return txtLabelText;

        }




    }
}
