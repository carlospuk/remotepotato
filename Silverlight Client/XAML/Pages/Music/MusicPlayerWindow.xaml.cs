using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using CommonEPG;

namespace SilverPotato
{
    public partial class MusicPlayerWindow : UserControl
    {
        // Members
        const double EXPANDED_HEIGHT = 420;
        const double CONTRACTED_HEIGHT = 200; // change in XAML too
        bool IgnoreNextMediaEndedEvent;

        // Events
        public event EventHandler MinimiseClicked;

        public MusicPlayerWindow()
        {
            InitializeComponent();

            InitialisePlayer();
            InitialisePlaylist();
        }

        void InitialisePlayer()
        {
            // TODO EVENTS HERE !  MediaPlayer.
            MainPlayer.MediaEnded += new EventHandler(MainPlayer_MediaEnded);
            MainPlayer.SkipNext += new EventHandler(MainPlayer_SkipNext);
            MainPlayer.SkipPrev += new EventHandler(MainPlayer_SkipPrev);
        }
        void InitialisePlaylist()
        {
            MainPlaylist.UserMovedToNewSong += new EventHandler<GenericEventArgs<RPMusicSong>>(MainPlaylist_UserMovedToNewSong);
        }

      
        // Incoming Player Events
        void MainPlayer_MediaStopped(object sender, EventArgs e)
        {

        }
        void MainPlayer_MediaEnded(object sender, EventArgs e)
        {
            if (IgnoreNextMediaEndedEvent)
            {
                IgnoreNextMediaEndedEvent = false;
                return;
            }

            PushNextSongToPlayerIfRequired();
        }
        void MainPlayer_SkipPrev(object sender, EventArgs e)
        {
            PlayPrevSong();
        }
        void MainPlayer_SkipNext(object sender, EventArgs e)
        {
            PlayNextSong();
        }


        // PLAYLIST
        public void AddSong(RPMusicSong song)
        {
            MainPlaylist.AddSong(song);

            // And check list in case this is the first
            if (MainPlaylist.ItemCount == 1)
                PushNextSongToPlayerIfRequired();
        }
        public void AddSongs(List<RPMusicSong> songs)
        {
            MainPlaylist.AddSongs(songs);
            PushNextSongToPlayerIfRequired();
        }
        public void StopAndWipePlaylist()
        {
            MainPlaylist.WipeSongs();
            Stop();
        }
        public void Stop()
        {
            if (MainPlayer.IsActive)
                IgnoreNextMediaEndedEvent = true;  // don't fire a 'push next song' when the media stops on the next line
            MainPlayer.Stop();
        }
        void PushNextSongToPlayerIfRequired()
        {
            if (! MainPlayer.IsActive) // ready for a new song
                PlayNextSong();
        }
        void PlayNextSong()
        {
            RPMusicSong nextSong = MainPlaylist.MoveToNextSongOrNull();
            if (nextSong != null)
            {
                MainPlayer.Play(nextSong);
            }
        }
        void PlayPrevSong()
        {
            RPMusicSong prevSong = MainPlaylist.MoveToPrevSongOrNull();
            if (prevSong != null)
            {
                MainPlayer.Play(prevSong);
            }
        }

        // Incoming - user can also click items in playlist to move
        void MainPlaylist_UserMovedToNewSong(object sender, GenericEventArgs<RPMusicSong> e)
        {
            RPMusicSong nextSong = e.Value;
            
            if (nextSong != null)
                MainPlayer.Play(nextSong);
        }


        #region Minimise
        private void btnMinimise_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (MinimiseClicked != null) MinimiseClicked(this, new EventArgs());
        }
        private void btnMinimise_MouseEnter(object sender, MouseEventArgs e)
        {
            btnMinimise.Source = ImageManager.LoadImageFromContentPath("/Images/btnMinimiseWhite2_MouseOver.png");
        }
        private void btnMinimise_MouseLeave(object sender, MouseEventArgs e)
        {
            btnMinimise.Source = ImageManager.LoadImageFromContentPath("/Images/btnMinimiseWhite2.png");
        }
        #endregion

        private void btnClearPlaylist_Click(object sender, RoutedEventArgs e)
        {
            MainPlaylist.WipeSongs();
        }

        #region Expand / Contract Playlist
        private void imgTogglePlaylistHeight_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ChangeWindowHeight(!IsWindowExpanded());
        }
        private void imgTogglePlaylistHeight_MouseEnter(object sender, MouseEventArgs e)
        {
            ShowCorrectExpandPlaylistImage(true);
        }
        private void imgTogglePlaylistHeight_MouseLeave(object sender, MouseEventArgs e)
        {
            ShowCorrectExpandPlaylistImage(false);
        }
        void ShowCorrectExpandPlaylistImage(bool isMouseOver)
        {
            if (isMouseOver)
            {
                imgTogglePlaylistHeight.Source = IsWindowExpanded() ?
                    ImageManager.LoadImageFromContentPath("/Images/btnExpandCircleUp_MouseOver.png") :
                    ImageManager.LoadImageFromContentPath("/Images/btnExpandCircleDown_MouseOver.png");
            }
            else
            {
                imgTogglePlaylistHeight.Source = IsWindowExpanded() ?
                    ImageManager.LoadImageFromContentPath("/Images/btnExpandCircleUp.png") :
                    ImageManager.LoadImageFromContentPath("/Images/btnExpandCircleDown.png");
            }
        }
        void ChangeWindowHeight(bool shouldExpand)
        {
            double newHeight = shouldExpand ? EXPANDED_HEIGHT : CONTRACTED_HEIGHT;
            Animations.DoChangeHeightTo(0.15, brdMain, newHeight, ChangeWindowHeight_Completed);
        }
        void ChangeWindowHeight_Completed(object sender, EventArgs e)
        {
            ShowCorrectExpandPlaylistImage(false);  // assume not mouse over
        }
        bool IsWindowExpanded()
        {
            return (brdMain.ActualHeight > 340);
        }
        #endregion


    }
}
