using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace SilverPotato
{
    public partial class PlaylistItemLBI : UserControl
    {
        public PlaylistItem LinkedItem;
        public event EventHandler ItemClicked;
        public event EventHandler ItemBecameCurrent;

        // Constructor
        public PlaylistItemLBI(PlaylistItem pli)
        {
            InitializeComponent();

            LinkedItem = pli;
            LayoutFromLinkedItem();
            WireEventsFromLinkedItem();
        }

        // Layout
        void LayoutFromLinkedItem()
        {
            gdClickItem.Children.Clear();
            MusicSongClickItem ci = new MusicSongClickItem(0, LinkedItem.Song, MusicSongClickItem.RPMusicSongClickItemTextFormat.ArtistAndTitleAndDuration);
            ci.Clicked += new EventHandler(clickItem_Clicked);
            gdClickItem.Children.Add(ci);
        }
        void ShowSelectedIconIfSelected()
        {
            imgIsPlaying.Source = (LinkedItem.IsCurrent) ?
                ImageManager.LoadImageFromContentPath("/Images/imgPlaylistCurrent.png") : null;
        }
        void WireEventsFromLinkedItem()
        {
            LinkedItem.IsCurrentChanged += new EventHandler(LinkedItem_IsSelectedChanged);
        }


        // incoming from Clickitem
        void clickItem_Clicked(object sender, EventArgs e)
        {
            // not required - only one click item in here!
         //   if (!(sender is MusicSongClickItem)) return;
         //   MusicSongClickItem msci = (MusicSongClickItem)sender;

            if (ItemClicked != null) ItemClicked(this, new EventArgs());
        }


        // Update events from Linked Item
        void LinkedItem_IsSelectedChanged(object sender, EventArgs e)
        {
            ShowSelectedIconIfSelected();

            if (ItemBecameCurrent != null) ItemBecameCurrent(this, new EventArgs());
        }



    }
}
