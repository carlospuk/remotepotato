using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using CommonEPG;

namespace SilverPotato
{
    public class ArtistChooserStrip : ChooserStrip
    {
        ArtistChooserStripFilterTypes FilterType;

        public ArtistChooserStrip(ArtistChooserStripFilterTypes filterType) : base()
        {
            FilterType = filterType;
            lblStripTitle.Text = FilterType == ArtistChooserStripFilterTypes.AllArtists ? "Artists" : "Album Artists";

            // Initial appearance is text-only
            InitialiseContentPane(ClickItemsPane.ClickItemsPaneItemLayouts.List);
            contentPane.HidePaneControls(); // Artists is list-only
            contentPane.ItemClicked +=new EventHandler(contentPane_ItemClicked);
            SetWidthTo(250);

            PopulateStrip();
        }

        

        void PopulateStrip()
        {
            
            int counter = 0;
            Dictionary<string, List<RPMusicArtist>> artists = MusicManager.AllArtistsGroupedByAlpha(FilterType == ArtistChooserStripFilterTypes.AlbumArtists);
            foreach (KeyValuePair<string, List<RPMusicArtist>> kvp in artists)
            {
                List<ClickItem> clickItems = new List<ClickItem>();
                foreach (RPMusicArtist ar in kvp.Value)
                {
                    // Use the base class
                    ClickItem ci = new MusicArtistClickItem(counter++, ar, MusicArtistClickItem.RPMusicArtistClickItemTextFormat.TitleOnly, ClickItem.ClickItemLayouts.TextOnly, 100, 20);
                    clickItems.Add(ci);
                }

                // Add into current grouped items
                CurrentGroupedItems.Add(kvp.Key, clickItems);
            }

            RefreshContentPane();
        }
  


        // ITEMS CLICKED
        public event EventHandler<GenericEventArgs<object>> ItemClicked;
        void contentPane_ItemClicked(object sender, EventArgs e)
        {
            if (!(sender is MusicArtistClickItem)) return;
            MusicArtistClickItem maci = (MusicArtistClickItem)sender;
            ItemClicked(new object(), new GenericEventArgs<object>(maci.LinkedDataItem));
        }

    }


    public enum ArtistChooserStripFilterTypes
    {
        AllArtists,
        AlbumArtists
    }


}
