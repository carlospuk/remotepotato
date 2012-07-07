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
using CommonEPG;

namespace SilverPotato
{
    public partial class Playlist : UserControl
    {
        // Members
        List<PlaylistItem> CurrentPlaylist;

        // Constructor
        public Playlist()
        {
            InitializeComponent();

            CurrentPlaylist = new List<PlaylistItem>();
        }

        // External Add / Retrieve
        public void AddSong(RPMusicSong song)
        {
            PlaylistItem pli = new PlaylistItem(song);
            CurrentPlaylist.Add(pli);

            PlaylistItemLBI lbi = new PlaylistItemLBI(pli);
            lbi.Padding = new Thickness(0);
            lbi.ItemClicked += new EventHandler(lbi_ItemSelected);
            lbi.ItemBecameCurrent += new EventHandler(lbi_ItemBecameCurrent);
            lstPlaylist.Items.Add(lbi);
        }
        public void AddSongs(List<RPMusicSong> songs)
        {
            foreach (RPMusicSong song in songs)
            {
                AddSong(song);
            }
        }
        public RPMusicSong MoveToNextSongOrNull()
        {
            if (!IsMoreItems) return null;
            SelectNextPlaylistItem();  // does its own bounds checking

            if (CurrentPlaylistItem != null)
                return CurrentPlaylistItem.Song;
            else
                return null;
        }
        public RPMusicSong MoveToPrevSongOrNull()
        {
            if (CurrentPosition < 1) return null; // No previous song, or no songs
            
            SelectPrevPlaylistItem();   // does its own bounds checking          

            if (CurrentPlaylistItem != null)
                return CurrentPlaylistItem.Song;
            else
                return null;
        }
        void SetCurrentItemSelectedStatus(bool isSelected)
        {
            if (CurrentPlaylistItem != null)
            {
                CurrentPlaylistItem.IsCurrent = isSelected;
            }
        }
        public void WipeSongs()
        {
            lstPlaylist.Items.Clear();
            CurrentPlaylist.Clear();
        }

        // User-driven move to item, with callback
        public event EventHandler<GenericEventArgs<RPMusicSong>> UserMovedToNewSong;
        // PlaylistItem was clicked
        void lbi_ItemSelected(object sender, EventArgs e)
        {
            PlaylistItemLBI lbi = (PlaylistItemLBI)sender;
            PlaylistItem pli = lbi.LinkedItem;
            
            // Already selected? Do nothing.
            if (pli.IsCurrent) return;

            SetCurrentItemSelectedStatus(false);
            pli.IsCurrent = true;

            if (UserMovedToNewSong != null)
                UserMovedToNewSong(this, new GenericEventArgs<RPMusicSong>(pli.Song));
        }
        // PlaylistItem changed status, so possibly became the current one 
        void lbi_ItemBecameCurrent(object sender, EventArgs e)
        {
            PlaylistItemLBI lbi = (PlaylistItemLBI)sender;

            // Current item?
            if (lbi.LinkedItem.IsCurrent)
                lstPlaylist.ScrollIntoView(lbi);
        }


        // Local Properties & Helpers
        PlaylistItem CurrentPlaylistItem
        {
            get
            {
                foreach (PlaylistItem pli in CurrentPlaylist)
                {
                    if (pli.IsCurrent) return pli;
                }
                return null;
            }
        }
        int CurrentPosition
        {
            get
            {
                PlaylistItem pli = CurrentPlaylistItem;
                if (pli == null) return -1;

                return CurrentPlaylist.IndexOf(pli);
            }
        }
        void SelectNextPlaylistItem()
        {
            PlaylistItem nextPli = NextPlaylistItemOrNull;  // must get this before setting the current one to false (next line)
            if (nextPli == null) return;

            // Unselect current item and select new item
            SetCurrentItemSelectedStatus(false);
            nextPli.IsCurrent = true;
        }
        void SelectPrevPlaylistItem()
        {
            PlaylistItem prevPli = PrevPlaylistItemOrNull;  // must get this before setting the current one to false (next line)
            if (prevPli == null) return;
            
            // Unselect current item and select new item
            SetCurrentItemSelectedStatus(false);
            prevPli.IsCurrent = true;  
        }
        
        PlaylistItem NextPlaylistItemOrNull
        {
            get
            {
                if (!IsMoreItems) return null;
                
                int currentItemPos = CurrentPosition;
                currentItemPos += 1;
                return CurrentPlaylist[currentItemPos];
            }
        }
        PlaylistItem PrevPlaylistItemOrNull
        {
            get
            {
                int currentItemPos = CurrentPosition;
                if (currentItemPos < 1) return null;

                currentItemPos -= 1;
                return CurrentPlaylist[currentItemPos];
            }
        }
        bool IsMoreItems
        {
            get
            {
                return (NumberofItemsAfterPosition > 0);
            }
        }
        int NumberofItemsAfterPosition
        {
            get
            {
                return (CurrentPlaylist.Count) - CurrentPosition - 1;
            }
        }
        public int ItemCount
        {
            get
            {
                return CurrentPlaylist.Count;
            }
        }

    }
}
