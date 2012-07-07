using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using CommonEPG;

namespace SilverPotato
{
    public class SongsChooserStrip : ChooserStrip
    {
        public enum SongChooserSourceTypes
        {
            None,
            Artist,
            Album,
            Genre,
            Playlist,
            AllSongs
        }
        SongChooserSourceTypes SourceType;
        RPMusicCollection Source;
        List<RPMusicSong> DisplayedSongs;
        bool PopulatedSongsList = false;
        //string FilterText;

        public SongsChooserStrip(SongChooserSourceTypes sourceType, RPMusicCollection source)
        {
            DisplayedSongs = new List<RPMusicSong>();
            SourceType = sourceType;
            Source = source;

            // Initial appearance is list
            InitialiseContentPane(ClickItemsPane.ClickItemsPaneItemLayouts.ListTwoColumns);
            contentPane.ItemClicked += new EventHandler(contentPane_ItemClicked);
            contentPane.HidePaneControls(); // Songs can only be as a list
            SetWidthTo(260);

            // We have play buttons
            ShowHideActionButtons(true);
            ActionButtonsPane.ButtonClicked += new EventHandler<MusicActionButtonClickedEventArgs>(ActionButtonsPane_ButtonClicked);
            ActionButtonsPane.lbDownload.Visibility = Visibility.Collapsed;
            PopulateStripWithSongs();
        }


        // Refresh list if musicmanager loads while we're active
        public void PopulateStripWithSongs()
        {
            if (SourceType != SongChooserSourceTypes.AllSongs && Source == null)
            {
                MessageBox.Show("No song source found.");
                return;
            }

            switch (SourceType)
            {
                case SongChooserSourceTypes.Album:
                    RPMusicAlbum album = (RPMusicAlbum)Source;
                    lblStripTitle.Text = album.Title;

                    ShowHideHeaderImage(true);
                    
                    SetHeaderImageTo(album.ThumbnailUriOrNull("medium"));

                    MusicImporter.ImportSongsForAlbumCompleted += new EventHandler<GenericEventArgs<List<RPMusicSong>>>(MusicImporter_ImportCompleted);
                    MusicImporter.ImportSongsForAlbum(album.ID);
                    VisualManager.ShowActivityWithinGrid(gdContentPaneParent, 1.6);
                    break;

                case SongChooserSourceTypes.Artist:
                    RPMusicArtist artist = (RPMusicArtist)Source;
                    lblStripTitle.Text = "Songs by " + artist.Name;
                    MusicImporter.ImportSongsForArtistCompleted += new EventHandler<GenericEventArgs<List<RPMusicSong>>>(MusicImporter_ImportCompleted);
                    MusicImporter.ImportSongsForArtist(artist.ID);
                    VisualManager.ShowActivityWithinGrid(gdContentPaneParent, 1.6);
                    break;

                case SongChooserSourceTypes.Genre:
                    RPMusicGenre genre = (RPMusicGenre)Source;
                    lblStripTitle.Text = "All " + genre.Name + " Songs";
                    MusicImporter.ImportSongsForGenreCompleted += new EventHandler<GenericEventArgs<List<RPMusicSong>>>(MusicImporter_ImportCompleted);
                    MusicImporter.ImportSongsForGenre(genre.ID);
                    VisualManager.ShowActivityWithinGrid(gdContentPaneParent, 1.6);
                    break;

                case SongChooserSourceTypes.AllSongs:
                    lblStripTitle.Text = "All Songs";
                    MusicImporter.ImportAllSongsCompleted += new EventHandler<GenericEventArgs<List<RPMusicSong>>>(MusicImporter_ImportCompleted);
                    MusicImporter.ImportAllSongs();
                    VisualManager.ShowActivityWithinGrid(gdContentPaneParent, 1.6);
                    break;

                default:
                    lblStripTitle.Text = "No Songs Chosen";
                    break;
            }
            
          
        }

        void MusicImporter_ImportCompleted(object sender, GenericEventArgs<List<RPMusicSong>> e)
        {
            // Activity
            VisualManager.HideActivityWithinGrid(gdContentPaneParent);

            // Unwire event(s) - shouldn't complain if they're not wired up.
            MusicImporter.ImportSongsForArtistCompleted -= new EventHandler<GenericEventArgs<List<RPMusicSong>>>(MusicImporter_ImportCompleted);
            MusicImporter.ImportSongsForAlbumCompleted -= new EventHandler<GenericEventArgs<List<RPMusicSong>>>(MusicImporter_ImportCompleted);
            MusicImporter.ImportSongsForGenreCompleted -= new EventHandler<GenericEventArgs<List<RPMusicSong>>>(MusicImporter_ImportCompleted);
            MusicImporter.ImportAllSongsCompleted -= new EventHandler<GenericEventArgs<List<RPMusicSong>>>(MusicImporter_ImportCompleted);

            int counter = 0;
            List<ClickItem> clickItems = new List<ClickItem>();
            DisplayedSongs.Clear();
            foreach (RPMusicSong sg in e.Value)
            {
                // Use the base class
                MusicSongClickItem.RPMusicSongClickItemTextFormat TextFormat;
                if (
                    (SourceType == SongChooserSourceTypes.AllSongs) ||
                    (SourceType == SongChooserSourceTypes.Genre)
                    )
                {
                    TextFormat = MusicSongClickItem.RPMusicSongClickItemTextFormat.ArtistAndTitleAndDuration;
                }
                else 
                {
                    TextFormat = MusicSongClickItem.RPMusicSongClickItemTextFormat.TitleAndDuration;
                }

                bool ShowTrackNumbers = (SourceType == SongChooserSourceTypes.Album);

                ClickItem ci = new MusicSongClickItem(counter, sg, TextFormat, ShowTrackNumbers);
                clickItems.Add(ci);

                // Also store in displayed songs array, for use with action buttons
                DisplayedSongs.Add(sg);
            }

            // Add into current grouped items
            CurrentGroupedItems.Add("[SONGS]", clickItems);

            // Got songs
            PopulatedSongsList = true;

            RefreshContentPane();
        }

        // ITEMS CLICKED
        public event EventHandler<GenericEventArgs<object>> ItemClicked;
        void contentPane_ItemClicked(object sender, EventArgs e)
        {
            if (!(sender is MusicSongClickItem)) return;
            MusicSongClickItem msci  = (MusicSongClickItem)sender;
            ItemClicked(new object(), new GenericEventArgs<object>(msci.LinkedDataItem));
        }

        // Action Buttons Clicked
        void ActionButtonsPane_ButtonClicked(object sender, MusicActionButtonClickedEventArgs e)
        {
            if (!PopulatedSongsList) return;

            switch (e.ButtonType)
            {
                case MusicActionButtonTypes.PlayNow:
                    VisualManager.ShowMusicPlayer();
                    VisualManager.MusicPlayer.StopAndWipePlaylist();
                    VisualManager.MusicPlayer.AddSongs(DisplayedSongs);
                    break;

                case MusicActionButtonTypes.AddToNowPlaying:
                    VisualManager.ShowMusicPlayer();
                    foreach (RPMusicSong sg in DisplayedSongs)
                    {
                        VisualManager.MusicPlayer.AddSong(sg);
                    }
                    break;


                default:
                    break;
            }
        }

    }



}
