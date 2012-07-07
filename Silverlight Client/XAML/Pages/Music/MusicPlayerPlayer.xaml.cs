using System;
using System.Collections.Generic;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using CommonEPG;

namespace SilverPotato
{
    public partial class MusicPlayerPlayer : UserControl
    {
        RPMusicSong StreamingSong;
        DispatcherTimer positionTimer;
        bool showingBufferCircle = true;
        int TIMEOUT = 15;  // secs
        DateTime LastPositionChange = DateTime.Now;
        TimeSpan LastPosition = new TimeSpan();
        public bool IsActive = false;
        

        // Constructors
        public MusicPlayerPlayer()
        {
            InitializeComponent();

            positionTimer = new DispatcherTimer();
            positionTimer.Interval = TimeSpan.FromMilliseconds(250);
            positionTimer.Tick += new EventHandler(positionTimer_Tick);
            positionTimer.Start();

            InitialiseMediaPlayer();

            // Events
            volPopup.ValueChanged += new EventHandler<RoutedPropertyChangedEventArgs<double>>(volPopup_ValueChanged);

            imgCurrentSongThumb.ImageFailed +=new EventHandler<ExceptionRoutedEventArgs>(imgCurrentSongThumb_ImageFailed);
            imgCurrentSongThumb.ImageOpened +=new EventHandler<RoutedEventArgs>(imgCurrentSongThumb_ImageOpened);

            MusicImporter.CheckSongCanStreamCompleted += new EventHandler<GenericEventArgs<bool>>(MusicImporter_CheckSongCanStreamCompleted);
        }

        public void Dispose()
        {
            if ((mePlayer.CurrentState == MediaElementState.Playing) || (mePlayer.CurrentState == MediaElementState.Paused))
                mePlayer.Stop();

            if (positionTimer.IsEnabled)
                positionTimer.Stop();
            positionTimer = null;
        }


        // Events
        public event EventHandler MediaEnded;
        public event EventHandler<GenericEventArgs<Exception>> MediaFailed;
        public event EventHandler SkipNext;
        public event EventHandler SkipPrev;

        // Audio Methods
        void InitialiseMediaPlayer()
        {
            mePlayer.BufferingTime = TimeSpan.FromSeconds(10);
            mePlayer.CurrentStateChanged +=new RoutedEventHandler(mePlayer_CurrentStateChanged);
            mePlayer.MediaEnded +=new RoutedEventHandler(mePlayer_MediaEnded);
            mePlayer.MediaFailed +=new EventHandler<ExceptionRoutedEventArgs>(mePlayer_MediaFailed);
            mePlayer.MediaOpened +=new RoutedEventHandler(mePlayer_MediaOpened);
            mePlayer.DownloadProgressChanged +=new RoutedEventHandler(mePlayer_DownloadProgressChanged);
            mePlayer.BufferingProgressChanged +=new RoutedEventHandler(mePlayer_BufferingProgressChanged);

            volPopup.SetSliderValueTo(Settings.LastUsedVolumeLevel);
        }
        public void Play(RPMusicSong _song)
        {
            if (IsActive)
            {
                Stop();
                mePlayer.Source = null;
            }

            IsActive = true; // immediately active, so that playlists build up without overwriting
            StreamingSong = _song;

            // Check if file exists on server
            MusicImporter.CheckIfSongCanStream(_song);
        }

        void MusicImporter_CheckSongCanStreamCompleted(object sender, GenericEventArgs<bool> e)
        {
            if (!e.Value)
            {
                MessageBox.Show("The song was not found on the server - it may have been deleted.");
                SkipNext(this, new EventArgs());
                return;
            }
            // Song exists on server

            // Get thumbnail first
            ShowThumbnailForCurrentSong();

            // Then song
            Uri streamUri = StreamingSong.StreamSourceUri();
            mePlayer.AutoPlay = true;
            mePlayer.Source = streamUri;
        }
        
        public void Stop()
        {
            mePlayer.Stop();
            IsActive = false;
        }

        #region Show Position Timer / Current State
        void positionTimer_Tick(object sender, EventArgs e)
        {
            if (
                (mePlayer.CurrentState != MediaElementState.Playing) &&
                (mePlayer.CurrentState != MediaElementState.Paused) &&
                (mePlayer.CurrentState != MediaElementState.Stopped) &&
                (mePlayer.CurrentState != MediaElementState.Buffering)
                )
            {
                ResetPositionDisplay();
                return;
            }

            ShowCurrentPosition();
            CheckForTimeOut();
        }
        private void ResetPositionDisplay()
        {
            rctPlayed.Visibility = Visibility.Collapsed;
            lblTimeDisplay.Text = "";
        }
        private void ShowCurrentPosition()
        {
            if (StreamDuration.TotalSeconds > 0)
            {
                rctPlayed.Visibility = Visibility.Visible;
                double percPlayed = (mePlayer.Position.TotalSeconds / StreamDuration.TotalSeconds);
                rctPlayed.Width = (rctSeekBar.ActualWidth * percPlayed);

                // Textually - Position/Duration
                if (StreamDuration.Hours > 0)
                {
                    lblTimeDisplay.Text = String.Format("{0:00}:{1:00}:{2:00} / {3:00}:{4:00}:{5:00}",
                        mePlayer.Position.Hours, mePlayer.Position.Minutes, mePlayer.Position.Seconds,
                        StreamDuration.Hours, StreamDuration.Minutes, StreamDuration.Seconds);
                }
                else
                {
                    lblTimeDisplay.Text = String.Format("{0:00}:{1:00} / {2:00}:{3:00}",
                        mePlayer.Position.Minutes, mePlayer.Position.Seconds,
                        StreamDuration.Minutes, StreamDuration.Seconds);
                }
            }
            else // No duration available
            {
                rctPlayed.Visibility = Visibility.Collapsed;

                // Textually - Position Only
                lblTimeDisplay.Text = String.Format("{0:00}:{1:00}",
                    mePlayer.Position.Minutes, mePlayer.Position.Seconds);
            }



        }
        // MediaPlayer Events
        private void mePlayer_DownloadProgressChanged(object sender, RoutedEventArgs e)
        {
            double dlBarWidth = (rctSeekBar.ActualWidth * mePlayer.DownloadProgress);
            rctDownloaded.Width = dlBarWidth;
        }
        private void mePlayer_CurrentStateChanged(object sender, RoutedEventArgs e)
        {
            ProcessCurrentPlayerState();

        }
        private void ProcessCurrentPlayerState()
        {
            if (mePlayer.CurrentState == MediaElementState.Buffering)
            {
                if (!showingBufferCircle)
                {
                    // Initiate faded buffering mode
                    VisualManager.ShowProgressWaiterWithinGrid(LayoutRoot, 1.0, "Buffering", (mePlayer.BufferingProgress * 100).ToString() + "%");
                    showingBufferCircle = true;
                }

            }
            else  // Not buffering
            {
                if (showingBufferCircle)
                {
                    VisualManager.HideProgressWaiterWithinGrid(LayoutRoot);

                    // Fade up media
                    showingBufferCircle = false;
                }
            }


            switch (mePlayer.CurrentState)
            {
                case MediaElementState.Buffering:
                    imgPlayPause.Source = ImageManager.LoadImageFromContentPath("/Images/btnStop.png");
                    lblStatus.Text = "Buffering";
                    break;

                case MediaElementState.Playing:
                    // set volume, i.e. if just opened and begun playing
                    mePlayer.Volume = volPopup.sldVolume.Value;

                    imgPlayPause.Source = mePlayer.CanPause ? ImageManager.LoadImageFromContentPath("/Images/btnPause.png") : ImageManager.LoadImageFromContentPath("/Images/btnStop.png");
                    lblStatus.Text = StreamingSong.Title;
                    break;

                case MediaElementState.Paused:
                    imgPlayPause.Source = ImageManager.LoadImageFromContentPath("/Images/btnPlay.png");
                    lblStatus.Text = "Paused";
                    break;

                case MediaElementState.Stopped:
                    imgPlayPause.Source = ImageManager.LoadImageFromContentPath("/Images/btnPlay.png");
                    lblStatus.Text = "";
                    DoMediaEnded();
                    
                    break;

                case MediaElementState.Closed:
                    lblStatus.Text = "";
                    break;

                default:
                    lblStatus.Text = mePlayer.CurrentState.ToString();
                    break;
            }

        }

        TimeSpan StreamDuration
        {
            get
            {
                if (mePlayer.NaturalDuration.HasTimeSpan)
                    return mePlayer.NaturalDuration.TimeSpan;
                else
                    return new TimeSpan(0);
            }
        }


        void CheckForTimeOut()
        {
            if (! (mePlayer.CurrentState == MediaElementState.Buffering)) return;

            if (! mePlayer.Position.Equals(LastPosition))
            {
                LastPosition = mePlayer.Position;
                LastPositionChange = DateTime.Now;
            }
            else
            {
                TimeSpan timeSinceLastChange = DateTime.Now - LastPositionChange;
                if (timeSinceLastChange.TotalSeconds > TIMEOUT)
                {
                    mePlayer.Stop();
                }
            }
        }
        private void mePlayer_BufferingProgressChanged(object sender, RoutedEventArgs e)
        {
            int bufProg = Convert.ToInt32(mePlayer.BufferingProgress * 100);
            VisualManager.UpdateProgressWaiter(LayoutRoot, "", bufProg.ToString("D2") + "%");
        
        }
        private void mePlayer_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            Functions.WriteLineToLogFile("Audio stream Failed:");
            Functions.WriteExceptionToLogFile(e.ErrorException);
            IsActive = false;
            if (MediaFailed != null)
                MediaFailed(this, new GenericEventArgs<Exception>(e.ErrorException));
        }
        private void mePlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            Functions.WriteLineToLogFile("MediaStreaming: Media Opened OK");
            SetVolumeToLevel(Settings.LastUsedVolumeLevel);

            // Capabilities
            Functions.ShowHideElement(btnPlay, mePlayer.CanPause);
            gdSeekBar.Cursor = (mePlayer.CanSeek) ? Cursors.Hand : Cursors.Arrow;
        }
        private void mePlayer_MarkerReached(object sender, TimelineMarkerRoutedEventArgs e)
        {

        }
        private void mePlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            try
            {
                mePlayer.Stop();
            }
            catch { }


            DoMediaEnded();
        }
        void DoMediaEnded()
        {
            mePlayer.Source = null;
            IsActive = false;
            if (MediaEnded != null)
                MediaEnded(this, new EventArgs());
        }
        #endregion


        #region Thumbnail
        void ShowThumbnailForCurrentSong()
        {
            Uri thumbUri = StreamingSong.ThumbnailUriOrNull("medium");
            imgCurrentSongThumb.Source = new BitmapImage(thumbUri);
        }
        void imgCurrentSongThumb_ImageOpened(object sender, RoutedEventArgs e)
        {

        }
        void imgCurrentSongThumb_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            imgCurrentSongThumb.Source = ImageManager.bmpThumbnailDefault;
        }

        #endregion

        void CloseMe()
        {
            if (mePlayer != null)
            {
                if (mePlayer.CurrentState != MediaElementState.Stopped)
                {
                    try
                    {
                        mePlayer.Stop();
                    }
                    catch { }
                }
            }

            VisualManager.HideStreamingVideo();
        }

        // Buttons / Bars
        private void btnPlay_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            switch (mePlayer.CurrentState)
            {
                case MediaElementState.Paused:
                    mePlayer.Play();
                    break;

                case MediaElementState.Stopped:
                    mePlayer.Play();
                    break;

                case MediaElementState.Playing:
                    if (mePlayer.CanPause)
                        mePlayer.Pause();
                    else
                        mePlayer.Stop();
                    break;

                case MediaElementState.Buffering:
                        mePlayer.Stop();
                        DoMediaEnded();
                    break;

                default:
                    break;
            }
        }
        private void btnRestart_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            mePlayer.Pause();
            mePlayer.Position = TimeSpan.FromSeconds(0);
            mePlayer.Play();
        }
        private void btnSkipPrev_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (SkipPrev != null)
                SkipPrev(this, new EventArgs());
        }
        private void btnSkipNext_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (SkipNext != null)
                SkipNext(this, new EventArgs());
        }
        private void ControlButton_MouseEnter(object sender, MouseEventArgs e)
        {
            if (! (sender is Border)) return;
            Border b = (Border)sender;
            b.BorderBrush = new SolidColorBrush(Colors.White);
        }
        private void ControlButton_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!(sender is Border)) return;
            Border b = (Border)sender;
            b.BorderBrush = new SolidColorBrush(Colors.Black);
        }
        private void brdTopNavBack_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            CloseMe();
        }

        // Seek
        private void gdSeekBar_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!mePlayer.CanSeek) return;

            Point mousePoint = e.GetPosition(rctSeekBar);

            // Calculate point to seek to
            double percOfBar = (mousePoint.X / rctSeekBar.ActualWidth);
            double mediaSeconds = mePlayer.NaturalDuration.HasTimeSpan ? mePlayer.NaturalDuration.TimeSpan.TotalSeconds : -1;
            if (mediaSeconds == -1) return;
            double SeekToSeconds = (mediaSeconds * percOfBar);

            // Cannot seek to a point this isn't yet downloaded
            double dlSeconds = mePlayer.DownloadProgress * mediaSeconds;
            if (SeekToSeconds > dlSeconds) return;

            // End of file?  Go back five seconds.
            if (SeekToSeconds > mediaSeconds) SeekToSeconds = (mediaSeconds - 5);

            bool ShouldPlay = (mePlayer.CurrentState == MediaElementState.Playing);
            
            mePlayer.Pause();
            mePlayer.Position = TimeSpan.FromSeconds(SeekToSeconds);
            
            if (ShouldPlay)
                mePlayer.Play();
        }


        #region Volume Control
        void volPopup_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SetVolumeToLevel(e.NewValue);
        }
        void SetVolumeToLevel(double newLevel)
        {
            mePlayer.Volume = newLevel;
        }
        #endregion





    }
}
