using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using CommonEPG;

namespace SilverPotato
{
    public class SongInfoStrip : ChooserStrip
    {
        RPMusicSong Song;

        public SongInfoStrip(RPMusicSong song)
        {
            Song = song;

            // Do we need to initialise it if we're just going to hide it?
            InitialiseContentPane(ClickItemsPane.ClickItemsPaneItemLayouts.List);
            
            contentPane.Visibility = Visibility.Collapsed;
            SetWidthTo(220);

            // Songs have play buttons
            ShowHideActionButtons(true);
            ActionButtonsPane.ButtonClicked += new EventHandler<MusicActionButtonClickedEventArgs>(ActionButtonsPane_ButtonClicked);

            // Song has thumbnail
            ShowHideHeaderImage(true);
            

            PopulateSongInfo();
        }



        void PopulateSongInfo()
        {
            lblStripTitle.Text = Song.Title;
            SetHeaderImageTo(Song.ThumbnailUriOrNull("medium"));

            SongInfoPane sip = new SongInfoPane(Song);
            sip.Margin = new Thickness(0, 30, 0, 0);
            // TODO: song info here

            gdContentPaneParent.Children.Add(sip);
        }



        // Action Buttons Clicked
        void ActionButtonsPane_ButtonClicked(object sender, MusicActionButtonClickedEventArgs e)
        {
            switch (e.ButtonType)
            {
                case MusicActionButtonTypes.PlayNow:
                    VisualManager.ShowMusicPlayer();
                    VisualManager.MusicPlayer.StopAndWipePlaylist();
                    VisualManager.MusicPlayer.AddSong(Song);
                    break;

                case MusicActionButtonTypes.AddToNowPlaying:
                    VisualManager.ShowMusicPlayer();
                    VisualManager.MusicPlayer.AddSong(Song);
                    break;

                case MusicActionButtonTypes.Download:
                    System.Windows.Browser.HtmlPage.Window.Navigate(Song.DownloadSourceUri(), "_new");
                    break;

                default:
                    break;
            }
        }



    }



}
