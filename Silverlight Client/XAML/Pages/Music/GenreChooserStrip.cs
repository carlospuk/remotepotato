using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using CommonEPG;

namespace SilverPotato
{
    public class GenreChooserStrip : ChooserStrip
    {

        public GenreChooserStrip()
        {
            lblStripTitle.Text = "Genres";

            // Appearance is text-only
            InitialiseContentPane(ClickItemsPane.ClickItemsPaneItemLayouts.List);
            contentPane.HidePaneControls(); // List-only
            contentPane.ItemClicked +=new EventHandler(contentPane_ItemClicked);
            SetWidthTo(200);

            PopulateStripWithAllGenres();
        }


        public void PopulateStripWithAllGenres()
        {
            int counter = 0;
            
            List<ClickItem> clickItems = new List<ClickItem>();
            foreach (RPMusicGenre gn in MusicManager.AllGenres)
            {
                // Use the base class
                ClickItem ci = new MusicGenreClickItem(counter++, gn);
                clickItems.Add(ci);
            }

            // Add into current grouped items
            CurrentGroupedItems.Add("[ALL GENRES]", clickItems);

            RefreshContentPane();
        }


        // ITEMS CLICKED
        public event EventHandler<GenericEventArgs<object>> ItemClicked;
        void contentPane_ItemClicked(object sender, EventArgs e)
        {
            if (!(sender is MusicGenreClickItem)) return;
            MusicGenreClickItem mgci = (MusicGenreClickItem)sender;
            ItemClicked(new object(), new GenericEventArgs<object>(mgci.LinkedDataItem));
        }

    }
}
