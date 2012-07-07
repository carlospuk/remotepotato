using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using CommonEPG;
using FatAttitude.WTVTranscoder;
using FatAttitude.MediaStreamer;
using FatAttitude.SilverLive;

namespace SilverPotato
{
    public partial class StreamingVideoPage : UserControl, IDisposable, IPage
    {
        TVProgramme StreamTVProgramme;
        TimeSpan StartPosition;
        DispatcherTimer positionTimer;
        bool mediaIsFaded = true;
        int SECONDS_BUFFER = 6;
        double SECONDS_WAIT_TIL_ACCESS_STREAM = 0.2;  // TESTING: new faster streaming
        int TIMEOUT = 15;  // secs
        DateTime LastPositionChange = DateTime.Now;
        TimeSpan LastPosition = new TimeSpan();
        

        enum VideoZoomLevels
        {
            Small,
            Med,
            Large,
            XLarge,
            FullWindow,
            FullScreen
        }

        public StreamingVideoPage()
        {
            InitializeComponent();

            positionTimer = new DispatcherTimer();
            positionTimer.Interval = TimeSpan.FromMilliseconds(250);
            positionTimer.Tick += new EventHandler(positionTimer_Tick);
            positionTimer.Start();


#if DEBUG
            brdMediaElement.Background = new SolidColorBrush( Functions.HexColor("#000099") );
#endif
            Application.Current.Host.Content.FullScreenChanged += new EventHandler(Content_FullScreenChanged);
            volPopup.ValueChanged += new EventHandler<RoutedPropertyChangedEventArgs<double>>(volPopup_ValueChanged);

        }

        
        public StreamingVideoPage(TVProgramme _TVProg, TimeSpan startFrom) : this()
        {
            StreamTVProgramme = _TVProg;
            StartPosition = startFrom;

            // show options dialog
            lblShowTitle.Text = StreamTVProgramme.Title;


            this.Loaded += new RoutedEventHandler(StreamingVideoPage_Loaded);
        }
        bool PageIsLoaded = false;
        void StreamingVideoPage_Loaded(object sender, RoutedEventArgs e)
        {
            WireStreamingManagerEvents();
            PopulateHLSEncodingComboBox();
            SetHLSAdvancedOptionsView();
            progressSlider.PositionUserChanged += new EventHandler<FatAttitude.MediaControls.ProgressSlider.PositionUserChangedEventArgs>(progressSlider_PositionUserChanged);


            if (Settings.AskForStreamingTypeEachTime)
                ShowStreamingChoice();
            else
            {
                DoStreamingTypeChosen();
            }


            PageIsLoaded = true;
        }

        public void Dispose()
        {
            if ((meStreamingVideo.CurrentState == MediaElementState.Playing) || (meStreamingVideo.CurrentState == MediaElementState.Paused))
                meStreamingVideo.Stop();

            if (positionTimer.IsEnabled)
                positionTimer.Stop();
            positionTimer = null;
        }


        // Video Methods
        double DefaultZoomSize;

        bool wiredStreamingManagerEvents;
        void WireStreamingManagerEvents()
        {
            if (wiredStreamingManagerEvents) return;

            StreamingManager.StartStreamingFile_Completed += new EventHandler<GenericEventArgs<WTVStreamingVideoResult>>(StreamingManager_StartStreamingFile_Completed);
            StreamingManager.StartStreamingFileByHLS_Completed += new EventHandler<GenericEventArgs<MediaStreamingResult>>(StreamingManager_StartStreamingFileByHLS_Completed);
            StreamingManager.ProbeFile_Completed += new EventHandler<ProbeFileResultEventArgs>(StreamingManager_ProbeFile_Completed);

            wiredStreamingManagerEvents = true;
        }
        void UnWireStreamingManagerEvents()
        {
            if (! wiredStreamingManagerEvents) return;

            StreamingManager.StartStreamingFile_Completed -= new EventHandler<GenericEventArgs<WTVStreamingVideoResult>>(StreamingManager_StartStreamingFile_Completed);
            StreamingManager.StartStreamingFileByHLS_Completed -= new EventHandler<GenericEventArgs<MediaStreamingResult>>(StreamingManager_StartStreamingFileByHLS_Completed);
            StreamingManager.ProbeFile_Completed -= new EventHandler<ProbeFileResultEventArgs>(StreamingManager_ProbeFile_Completed);

            wiredStreamingManagerEvents = false;
        }
        void BeginStreaming()
        {
            // Visuals
            VisualManager.ShowProgressWaiterWithinGrid(LayoutRoot, 1.0, "Connecting", "");

            // Stop any playing music
            if (VisualManager.MusicPlayer != null)
                VisualManager.MusicPlayer.Stop();
            
            lblStatus.Text = "Fetching Stream URL...";
            

            // Prepare vid box zoom and GUI elements
            WTVProfileQuality targetQuality = (WTVProfileQuality)cmbStreamQuality.SelectedIndex;
            DefaultZoomSize = FrameZoomForQuality(targetQuality);
            Functions.WriteLineToLogFile("Beginning streaming: quality requested is: " + targetQuality.ToString());

            if (Settings.EnableHTTPLiveStreaming)
            {
                BeginStreaming_HLS();
            }
            else
                BeginStreaming_WMSP();
        }
        void StopStreaming()
        {
            if (meStreamingVideo == null) return;

            if (
                (meStreamingVideo.CurrentState == MediaElementState.Playing) ||
                (meStreamingVideo.CurrentState == MediaElementState.Paused)
                )
            {
                meStreamingVideo.Stop();
            }

            meStreamingVideo.Source = null;

            if (LatestHLSMediaStreamingResult != null) // i.e. HLS streaming
                StreamingManager.StopStreamingFromHLSID(LatestHLSMediaStreamingResult.StreamerID);
        }       

        #region HLS Streaming
        SiverLiveStreamSource silverSource;
        MediaStreamingResult LatestHLSMediaStreamingResult;

        void ProbeHLSStreams()
        {
            VisualManager.ShowProgressWaiterWithinGrid(LayoutRoot, 1, "Getting Streams", "Please Wait");

            // Probe the file first
            StreamingManager.ProbeFile(StreamTVProgramme.Filename);
        }
        void StreamingManager_ProbeFile_Completed(object sender, ProbeFileResultEventArgs e)
        {
            // hide 'connecting' visual
            VisualManager.HideProgressWaiterWithinGrid(LayoutRoot);

            if (! e.Success)
            {
                MessageBox.Show("Cannot examine the file to be streamed : " + e.ErrorText);

                VisualManager.HideStreamingVideo();
                return;
            }

            // Get aspect ratio from probed files...
            storeTargetHLSVideoAspectRatio(e.Streams);

            // Populate audio stream choice 
            PopulateHLSAudioStreamCombo(e.Streams);

            // Show options
            ShowHLSStreamingVideoOptionsWindow();

        }
        string targetHLSVideoAspectRatio = "16:9";
        void storeTargetHLSVideoAspectRatio(List<AVStream> streams)
        {
            foreach (AVStream str in streams)
            {
                if (str.CodecType != AVCodecType.Video) continue;

                // Video stream
                targetHLSVideoAspectRatio = str.DisplayAspectRatio;
                break;
            }
        }
        void PopulateHLSAudioStreamCombo(List<AVStream> streams)
        {
            ComboBoxItem cbiDefault = comboItemForStreamTemplate();
            cbiDefault.Content = "Default";
            cmbHLSAudioStream.Items.Add(cbiDefault);

            // Add all audio streams to combo
            foreach (AVStream str in streams)
            {
                if (str.CodecType != AVCodecType.Audio) continue;
                // Add item
                cmbHLSAudioStream.Items.Add(comboItemForStream(str));
            }

            // Intelligently pick a stream
            int autoChooseIndex = 0;
            int indexCounter = 1 ;
            foreach (AVStream str in streams)
            {
                if (str.CodecType != AVCodecType.Audio) continue;

                autoChooseIndex = indexCounter;
                if (str.AudioCodecSubType != AudioStreamTypes.Commentary) break;  // Choose the commentary if there really is nothing else    

                indexCounter++;
            }

            // Set default: first normal stream, if there is one            
            cmbHLSAudioStream.SelectedIndex = autoChooseIndex;
        }
        ComboBoxItem comboItemForStream(AVStream str)
        {
            ComboBoxItem cbi = comboItemForStreamTemplate();

            cbi.Content = str.ToString();
            cbi.Tag = str.StreamIndex;
            return cbi;
        }
        ComboBoxItem comboItemForStreamTemplate()
        {
            ComboBoxItem cbi = new ComboBoxItem();
            cbi.FontFamily = new FontFamily("Arial");
            cbi.FontSize = 12.0;
            cbi.Cursor = Cursors.Hand;

            return cbi;
        }
        void ShowHLSStreamingVideoOptionsWindow()
        {
            ShowHideHLSStreamingVideoOptionsWindow(true);
            ShowHideWMSPStreamingVideoOptionsWindow(false);
        }
        void BeginStreaming_HLS()
        {
            int targetAudioStreamIndex = -1;
            if (cmbHLSAudioStream.SelectedIndex > 0)
            {
                ComboBoxItem cbi = (ComboBoxItem)cmbHLSAudioStream.SelectedItem;
                targetAudioStreamIndex = (int)cbi.Tag;
            }

            // Let's stream
            RequestStream_HLS(targetAudioStreamIndex);
        }
        void RequestStream_HLS(int AudioStreamIndex)
        {
            MediaStreamingRequest msRq = null;
            int desktopQualityLevel = (int)cmbHLSProfile.SelectedIndex;
            msRq = MediaStreamingRequest.RequestWithDesktopProfileLevel(desktopQualityLevel);

            msRq.ClientID = "silverpotato";
            msRq.ClientDevice = "browser";
            msRq.InputFile = StreamTVProgramme.Filename;
            msRq.UseAudioStreamIndex = AudioStreamIndex;
            msRq.StartAt = Convert.ToInt32(StartPosition.TotalSeconds);
            msRq.StreamingType = MediaStreamingRequest.StreamingTypes.HttpLiveStreaming;
            // Set aspect ratio from what was passed into this method
            msRq.CustomParameters.AspectRatio = targetHLSVideoAspectRatio;
            
            

            // Override bitrate?
            string strBitrate = VideoBitrateForComboBoxSelection();
            if (strBitrate.Length > 0)
                msRq.CustomParameters.VideoBitRate = strBitrate;

            string strAudioBitrate = AudioBitrateForComboBoxSelection();
            if (strAudioBitrate.Length > 0)
                msRq.CustomParameters.AudioBitRate = strAudioBitrate;

            string strAudioSampleRate = AudioSampleRateForComboBoxSelection();
            if (strAudioSampleRate.Length > 0)
                msRq.CustomParameters.AudioSampleRate = strAudioSampleRate;


            StreamingManager.StartStreamingFromHLSStreamingRequest(msRq);
        }

        void StreamingManager_StartStreamingFileByHLS_Completed(object sender, GenericEventArgs<MediaStreamingResult> e)
        {

            if (e.Value.Success)
            {
                WaitThenStreamWithHLS(e.Value);
            }
            else // FAILED:  
            {
                // hide 'connecting' visual
                VisualManager.HideProgressWaiterWithinGrid(LayoutRoot);

                MessageBox.Show("Cannot stream this content: " + e.Value.ErrorText);

                VisualManager.HideStreamingVideo();
            }
        }
        void WaitThenStreamWithHLS(MediaStreamingResult streamresult)
        {
            // Store the stream result to use when we stream, e.g to get the URL for streaming
            LatestHLSMediaStreamingResult = streamresult;

            DispatcherTimer dt = new DispatcherTimer();
            dt.Interval = TimeSpan.FromSeconds(SECONDS_WAIT_TIL_ACCESS_STREAM);
            dt.Tick += new EventHandler(StreamingManager_HLSStreamIsReady_2);
            dt.Start();

            // Hide visuals  (will probably get a respite in a minute when it's called for buffering)
            VisualManager.UpdateProgressWaiter(LayoutRoot, "Waiting for stream", "");
        }
        void StreamingManager_HLSStreamIsReady_2(object sender, EventArgs e)
        {
            // Remove timer
            DispatcherTimer dt = (DispatcherTimer)sender;
            dt.Stop();
            dt = null;

            VisualManager.UpdateProgressWaiter(LayoutRoot, "Opening LiveStream", "");

            string LatestStreamingURL = NetworkManager.hostHLS_URL_WithRelativePath(LatestHLSMediaStreamingResult.LiveStreamingIndexPath);
            LastPositionChange = DateTime.Now;
            Functions.WriteLineToLogFile("Attempting to stream video via HTTP Live Streaming from " + LatestStreamingURL);
            meStreamingVideo.BufferingTime = TimeSpan.FromSeconds(SECONDS_BUFFER);

            Guid myGuid;
            Guid.TryParse("ea655078-c9af-4765-8883-848d58c6b2ee", out myGuid);
            silverSource = new SiverLiveStreamSource(LatestStreamingURL, myGuid);
            silverSource.InitAsyncCompleted += new EventHandler<InitAsyncCompletedEventArgs>(slSource_InitAsyncCompleted);

            silverSource.InitAsync();
        }

        void slSource_InitAsyncCompleted(object sender, InitAsyncCompletedEventArgs e)
        {
            SiverLiveStreamSource slSource = (SiverLiveStreamSource)sender;
            slSource.InitAsyncCompleted -= new EventHandler<InitAsyncCompletedEventArgs>(slSource_InitAsyncCompleted);

            meStreamingVideo.SetSource(slSource);
        }

        // Helpers
        void PopulateHLSEncodingComboBox()
        {
            VideoEncodingParameters[] profiles = MediaStreamingRequest.desktopVideoEncodingProfiles;
            for (int i = 0; i < profiles.Count(); i++)
            {
                ComboBoxItem cbi = new ComboBoxItem();
                cbi.Cursor = Cursors.Hand;
                cbi.Content = profiles[i].ToString();

                cmbHLSProfile.Items.Add(cbi);
            }

            // Default profile
            if (profiles.Count() > 1)
                cmbHLSProfile.SelectedIndex = 1;
            else
                if (profiles.Count() > 0)
                    cmbHLSProfile.SelectedIndex = 0;
        }
        string VideoBitrateForComboBoxSelection()
        {

            /*
             * <ComboBoxItem Content="320k"  />
                                <ComboBoxItem Content="400k"  />
                                <ComboBoxItem Content="600k"  />
                                <ComboBoxItem Content="800k"  />
                                <ComboBoxItem Content="960k"  />
                                <ComboBoxItem Content="1200k"  />
                                <ComboBoxItem Content="1400k"  />
                                <ComboBoxItem Content="1600k"  />
                                <ComboBoxItem Content="1800k"  />
                                <ComboBoxItem Content="2000k"  />
                                <ComboBoxItem Content="2500k"  />
                                <ComboBoxItem Content="3000k"  />*/
            switch (cmbHLSVideoBitrate.SelectedIndex)
            {
                case 0:
                    return "";
                case 1:
                    return "400k";
                case 2:
                    return "600k";
                case 3:
                    return "800k";
                case 4:
                    return "960k";
                case 5:
                    return "1200k";
                case 6:
                    return "1400k";
                case 7:
                    return "1600k";
                case 8:
                    return "1800k";
                case 9:
                    return "2000k";
                case 10:
                    return "2500k";
                case 11:
                    return "3000k";

                default:
                    return "";
            }
        }
        string AudioBitrateForComboBoxSelection()
        {

            /*
             * <                         <ComboBoxItem Content="32k"  />
                                <ComboBoxItem Content="48k"  />
                                <ComboBoxItem Content="64k"  />
                                <ComboBoxItem Content="92k"  />
                                <ComboBoxItem Content="128k"  />>*/
            switch (cmbHLSAudioBitrate.SelectedIndex)
            {
                case 0:
                    return "";
                case 1:
                    return "32k";
                case 2:
                    return "48k";
                case 3:
                    return "64k";
                case 4:
                    return "92k";
                case 5:
                    return "128k";

                default:
                    return "";
            }
        }
        string AudioSampleRateForComboBoxSelection()
        {

            /*                                <ComboBoxItem Content="Default for this quality"  />
                                <ComboBoxItem Content="24 khz"  />
                                <ComboBoxItem Content="32 khz"  />
                                <ComboBoxItem Content="44.1 khz (recommended)"  />
                                <ComboBoxItem Content="48 khz (unsupported)"  />*/
            switch (cmbHLSAudioSampleRate.SelectedIndex)
            {
                case 0:
                    return "";
                case 1:
                    return "24000";
                case 2:
                    return "32000";
                case 3:
                    return "44100";
                case 4:
                    return "48000";

                default:
                    return "";
            }
        }

        bool ShowingHLSAdvancedOptions;
        private void btnShowHideHLSAdvanced_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ShowingHLSAdvancedOptions = !ShowingHLSAdvancedOptions;
            SetHLSAdvancedOptionsView();
        }
        void SetHLSAdvancedOptionsView()
        {
            if (ShowingHLSAdvancedOptions)
            {
                rdHLSAdvancedOptions.Height = new GridLength(100);
                brdHLSStreamOptions.Height = 230;
                btnShowHideAdvanced.Text = "Hide Advanced";
            }
            else
            {
                rdHLSAdvancedOptions.Height = new GridLength(0);
                brdHLSStreamOptions.Height = 150;
                btnShowHideAdvanced.Text = "Show Advanced";
            }
        }

        #endregion

        #region MS-WMSP Streaming
        WTVStreamingVideoResult LastStreamingResult;
        string LatestStreamingPort;
        void BeginStreaming_WMSP()
        {
            WTVProfileQuality targetQuality = (WTVProfileQuality)cmbStreamQuality.SelectedIndex;
            int DeInterlaceMode = (cmbInterlaceVideo.SelectedIndex >= 0) ? cmbInterlaceVideo.SelectedIndex : 0;

            WTVStreamingVideoRequest strmRq = null;
            strmRq = new WTVStreamingVideoRequest(StreamTVProgramme.Filename, targetQuality, DeInterlaceMode, StartPosition);

            StreamingManager.StartStreamingFromWMSPStreamingRequest(strmRq);
        }
        void StreamingManager_StartStreamingFile_Completed(object sender, GenericEventArgs<WTVStreamingVideoResult> e)
        {
            LastStreamingResult = e.Value;

            if (e.Value.ResultCode == FatAttitude.WTVTranscoder.DSStreamResultCodes.OK)
            {
                WaitThenStreamWithWMSP(e.Value);
            }
            else // FAILED:  
            {
                // hide 'connecting' visual
                VisualManager.HideProgressWaiterWithinGrid(LayoutRoot);

                if (e.Value.ResultCode == FatAttitude.WTVTranscoder.DSStreamResultCodes.ErrorAC3CodecNotFound)
                    MessageBox.Show("Cannot stream this content as it requires AC3 audio and the AC3 audio codec is not available.\r\n\r\nPlease install the Remote Potato streaming pack from www.fatattitude.com onto the SERVER (i.e. not onto this computer).");
                else if (e.Value.ResultCode == FatAttitude.WTVTranscoder.DSStreamResultCodes.ErrorAlreadyStreaming)
                    MessageBox.Show("Cannot stream this content right now - streamer is still closing, please try again in a moment.");
                else if (e.Value.ResultCode == FatAttitude.WTVTranscoder.DSStreamResultCodes.ErrorExceptionOccurred)
                {
                    if (string.IsNullOrEmpty(e.Value.ResultString))
                        MessageBox.Show("Cannot stream this content - there was an error on the server.  The file may be in use, or an incompatible type." + e.Value.ResultString);
                    else
                        MessageBox.Show("Cannot stream this content - there was an error on the server:\r\n\r\n" + e.Value.ResultString);
                }
                else if (e.Value.ResultCode == FatAttitude.WTVTranscoder.DSStreamResultCodes.Error)
                    MessageBox.Show("Cannot stream this content - there was an error on the client:\r\n\r\n" + e.Value.ResultString);
                else
                    MessageBox.Show("Cannot stream this content.  (Result Code: " + e.Value.ToString() + ")");


                VisualManager.HideStreamingVideo();
            }
        }
        void WaitThenStreamWithWMSP(WTVStreamingVideoResult streamresult)
        {
            // Store the port for when we stream
            LatestStreamingPort = streamresult.Port;

            DispatcherTimer dt = new DispatcherTimer();
            dt.Interval = TimeSpan.FromSeconds(SECONDS_WAIT_TIL_ACCESS_STREAM);
            dt.Tick += new EventHandler(StreamingManager_WMSPStreamIsReady_2);
            dt.Start();

            // Hide visuals  (will probably get a respite in a minute when it's called for buffering)
            VisualManager.UpdateProgressWaiter(LayoutRoot, "Waiting for stream", "");
        }
        void StreamingManager_WMSPStreamIsReady_2(object sender, EventArgs e)
        {
            // Remove timer
            DispatcherTimer dt = (DispatcherTimer)sender;
            dt.Stop();
            dt = null;

            // Prepare streaming Uri
            string strUri = NetworkManager.hostStreamingURLForPort(LatestStreamingPort);
            Uri StreamUri = new Uri(strUri);

            VisualManager.UpdateProgressWaiter(LayoutRoot, "Opening stream", "");

            Functions.WriteLineToLogFile("Attempting to stream video from " + StreamUri);
            meStreamingVideo.BufferingTime = TimeSpan.FromSeconds(SECONDS_BUFFER);
            meStreamingVideo.Source = StreamUri;
            LastPositionChange = DateTime.Now;
        }
        #endregion
    

        #region Show Position Timer / Video Incoming Events
        // Streaming Video Events
        void positionTimer_Tick(object sender, EventArgs e)
        {
            if (
                (meStreamingVideo.CurrentState != MediaElementState.Playing) &&
                (meStreamingVideo.CurrentState != MediaElementState.Paused) &&
                (meStreamingVideo.CurrentState != MediaElementState.Stopped) &&
                (meStreamingVideo.CurrentState != MediaElementState.Buffering)
                )
                return;

            ShowCurrentPosition();
            CheckForTimeOut();
            CheckForOSDFades();
        }
        private void ShowCurrentPosition()
        {
            
                progressSlider.SetPosition(AbsoluteCurrentPosition);
            

            return;
            /*
            if (StreamDuration.TotalSeconds > 0)
            {
                rctPlayed.Visibility = Visibility.Visible;
                double percPlayed = (AbsoluteCurrentPosition.TotalSeconds / StreamDuration.TotalSeconds);
                rctPlayed.Width = (rctSeekBar.ActualWidth * percPlayed);

                // Textually
                lblTimeDisplay.Text = String.Format("{0:00}:{1:00}:{2:00} / {3:00}:{4:00}:{5:00}",
                    AbsoluteCurrentPosition.Hours, AbsoluteCurrentPosition.Minutes, AbsoluteCurrentPosition.Seconds,
                    (StreamTVProgramme.Duration()).Hours, (StreamTVProgramme.Duration()).Minutes, (StreamTVProgramme.Duration()).Seconds);

                StopVideoIfWithinTwoSecondsOfEnd();
            }
            else // No duration available
            {
                rctPlayed.Visibility = Visibility.Collapsed;

                // Textually
                lblTimeDisplay.Text = String.Format("{0:00}:{1:00}:{2:00}",
                    meStreamingVideo.Position.Hours, meStreamingVideo.Position.Minutes, meStreamingVideo.Position.Seconds);
            }

            */

        }
        void StopVideoIfWithinTwoSecondsOfEnd()
        {
            if (StreamTimeRemaining.TotalSeconds < 2)
                meStreamingVideo.Stop();
        }
        private void meStreamingVideo_DownloadProgressChanged(object sender, RoutedEventArgs e)
        {
            // Do nothing
        }
        private void meStreamingVideo_CurrentStateChanged(object sender, RoutedEventArgs e)
        {
            ProcessCurrentVideoState();
        }
        private void ProcessCurrentVideoState()
        {
            lblStatus.Text = meStreamingVideo.CurrentState.ToString();

            if (meStreamingVideo.CurrentState == MediaElementState.Buffering)
            {
                if (!mediaIsFaded)
                {
                    FadeMediaToBuffering();
                }

            }
            else  // Not buffering
            {
                if (mediaIsFaded)
                {
                    UnfadeMediaFromBuffering();
                }
            }


            switch (meStreamingVideo.CurrentState)
            {
                case MediaElementState.Buffering:
                    imgPlayPause.Source = ImageManager.LoadImageFromContentPath("/Images/btnStop.png");
                    btnPlayTriangle.Visibility = System.Windows.Visibility.Collapsed;
                    break;

                case MediaElementState.Playing:
                    imgPlayPause.Source = meStreamingVideo.CanPause ? ImageManager.LoadImageFromContentPath("/Images/btnPause.png") : ImageManager.LoadImageFromContentPath("/Images/btnStop.png");
                    btnPlayTriangle.Visibility = System.Windows.Visibility.Collapsed;
                    break;

                case MediaElementState.Paused:
                    imgPlayPause.Source = ImageManager.LoadImageFromContentPath("/Images/btnPlay.png");
                    btnPlayTriangle.Visibility = System.Windows.Visibility.Visible;
                    break;

                case MediaElementState.Stopped:
                    imgPlayPause.Source = ImageManager.LoadImageFromContentPath("/Images/btnPlay.png");
                    btnPlayTriangle.Visibility = System.Windows.Visibility.Collapsed;
                    DoMediaEnded();
                    break;

                default:
                    break;
            }

        }

        private void UnfadeMediaFromBuffering()
        {
            VisualManager.HideProgressWaiterWithinGrid(LayoutRoot);

            // Fade up media
            Animations.DoFadeTo(0.1, meStreamingVideo, 1.0);
            mediaIsFaded = false;
        }

        private void FadeMediaToBuffering()
        {
            // Initiate faded buffering mode
            Animations.DoFadeTo(0.3, meStreamingVideo, 0.6);
            VisualManager.ShowProgressWaiterWithinGrid(LayoutRoot, 1.0, "Buffering", (meStreamingVideo.BufferingProgress * 100).ToString() + "%");
            mediaIsFaded = true;
        }
 
        void CheckForTimeOut()
        {
            if (! (meStreamingVideo.CurrentState == MediaElementState.Buffering)) return;

            if (!AbsoluteCurrentPosition.Equals(LastPosition))
            {
                LastPosition = AbsoluteCurrentPosition;
                LastPositionChange = DateTime.Now;
            }
            else
            {
                TimeSpan timeSinceLastChange = DateTime.Now - LastPositionChange;
                if (timeSinceLastChange.TotalSeconds > TIMEOUT)
                {
                    meStreamingVideo.Stop();
                }
            }
        }
        private void meStreamingVideo_BufferingProgressChanged(object sender, RoutedEventArgs e)
        {
            int bufProg = Convert.ToInt32(meStreamingVideo.BufferingProgress * 100);
            VisualManager.UpdateProgressWaiter(LayoutRoot, "", bufProg.ToString("D2") + "%");
        
        }
        private void meStreamingVideo_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            Functions.WriteLineToLogFile("Stream Failed:");
            string txtUserReason = "";

            btnFailCross.Visibility = System.Windows.Visibility.Visible;

            if (e.ErrorException.Message.Equals("AG_E_NETWORK_ERROR"))
            {
                if (MediaEverOpened)
                    txtUserReason = "A network error meant that the show was opened but could not be streamed.";
                else
                {
                    if (Settings.EnableHTTPLiveStreaming)
                        txtUserReason = "A network error meant that the show was opened but could not be streamed.";
                    else
                        txtUserReason = "A network error meant that the show could not be streamed - check that port " + LastStreamingResult.Port + " is open and correctly forwarded on your home router and firewall(s).";
                }
            }
            else
                txtUserReason = "An error meant that the show could not be streamed: " + e.ErrorException.Message;

            Functions.WriteExceptionToLogFile(e.ErrorException);
            Animations.DoFadeOut(1.0, meStreamingVideo, ResetVideoObject);
            ShowHidePostStreamWindow(true, txtUserReason);
        }
        private void ResetVideoObject(object sender, EventArgs e)
        {
            meStreamingVideo.Source = null;
        }
        bool MediaEverOpened = false;
        private void meStreamingVideo_MediaOpened(object sender, RoutedEventArgs e)
        {
            MediaEverOpened = true;
            Functions.WriteLineToLogFile("MediaStreaming: Media Opened OK");
            SetVolumeToLevel(Settings.LastUsedVolumeLevel);

            // Capabilities
            // Functions.ShowHideElement(btnPlay, meStreamingVideo.CanPause);
            Functions.ShowHideElement(btnRestart, false);
            progressSlider.SetTotalDuration(StreamDuration);

            SizeVideoForFullOrNormalScreenSize();
            ShowOrHideSliderForFullScreen();

            CurrentAspectRatio = StandardAspectRatioNames.OriginalSize;

            StretchVideoIfRequired();
        }
        private void meStreamingVideo_MarkerReached(object sender, TimelineMarkerRoutedEventArgs e)
        {

        }
        private void meStreamingVideo_MediaEnded(object sender, RoutedEventArgs e)
        {
            try
            {
                meStreamingVideo.Stop();
            }
            catch { }


            DoMediaEnded();
        }
        void DoMediaEnded()
        {
            meStreamingVideo.Source = null;
            ShowHidePostStreamWindow(true, "Streaming of the show ended normally.");
        }
        #endregion


        void CloseMe()
        {
            VisualManager.HideStreamingVideo();
        }

        // Properties
        private TimeSpan AbsoluteCurrentPosition
        {
            get
            {
                return meStreamingVideo.Position + StartPosition;
            }
        }
        private TimeSpan StreamDuration
        {
            get
            {
                return StreamTVProgramme.Duration();
            }
        }
        private TimeSpan StreamTimeRemaining
        {
            get
            {
                return StreamDuration - AbsoluteCurrentPosition;
            }
        }

        // Buttons / Bars
        private void btnPlay_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            DoPlayPauseToggle();
        }

        private void DoPlayPauseToggle()
        {
            switch (meStreamingVideo.CurrentState)
            {
                case MediaElementState.Paused:
                    meStreamingVideo.Play();
                    break;

                case MediaElementState.Stopped:
                    meStreamingVideo.Play();
                    break;

                case MediaElementState.Playing:
                    if (meStreamingVideo.CanPause)
                        meStreamingVideo.Pause();
                    else
                        meStreamingVideo.Stop();
                    break;

                case MediaElementState.Buffering:
                    meStreamingVideo.Stop();
                    DoMediaEnded();
                    break;

                default:
                    break;
            }
        }
        private void btnRestart_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            meStreamingVideo.Pause();
            meStreamingVideo.Position = TimeSpan.FromSeconds(0);
            meStreamingVideo.Play();
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
        private void btnToggleFullScreen_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Functions.ToggleFullScreen();
        }
        private void brdTopNavBack_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            CloseMe();
        }
        void progressSlider_PositionUserChanged(object sender, FatAttitude.MediaControls.ProgressSlider.PositionUserChangedEventArgs e)
        {
            if (!meStreamingVideo.CanSeek) return;

            if (Settings.EnableHTTPLiveStreaming)
            {
                meStreamingVideo.Position = e.NewPosition;
                //FadeMediaToBuffering();
            }
            else
            {
                meStreamingVideo.Pause();
                meStreamingVideo.Position = e.NewPosition;
                meStreamingVideo.Play();
            }
        }
        private void btnStartStreaming_Click(object sender, RoutedEventArgs e)
        {
            ShowHideWMSPStreamingVideoOptionsWindow(false);
            BeginStreaming();
        }
        private void btnStartHLSStreaming_Click(object sender, RoutedEventArgs e)
        {
            ShowHideHLSStreamingVideoOptionsWindow(false);
            BeginStreaming();
        }
        private void btnTopNavBack_MouseEnter(object sender, MouseEventArgs e)
        {
            Image img = (Image)btnTopNavBack.Child;
            if (ImageManager.bmpBtnBackOn != null)
                img.Source = ImageManager.bmpBtnBackOn;
        }
        private void btnTopNavBack_MouseLeave(object sender, MouseEventArgs e)
        {
            Image img = (Image)btnTopNavBack.Child;

            if (ImageManager.bmpBtnBack != null)
                img.Source = ImageManager.bmpBtnBack;
        }

        private void meStreamingVideo_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            DoPlayPauseToggle();
        }

        private void btnPlayTriangle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            DoPlayPauseToggle();
        }
        private void btnFailCross_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Nothing.
        }

        


        #region Stream options window
        bool showAdvancedStreamingOptions = false;
        void ShowHideWMSPStreamingVideoOptionsWindow(bool show)
        {
            bool showStartFrom = StreamTVProgramme.CanChooseStartPositionWhenStreaming();

            if (show)
            {
                Functions.ShowHideElement(lblStartFrom, showStartFrom);
                Functions.ShowHideElement(lblStartFromHeader, showStartFrom);
                Functions.ShowHideElement(sldStartFrom, showStartFrom);

                brdStreamOptions.Height = (showStartFrom) ? 240 : 190;  // 50 less without these
                rdStreamStartAt1.Height = (showStartFrom) ? new GridLength(25.0) : new GridLength(0.0);
                rdStreamStartAt2.Height = (showStartFrom) ? new GridLength(25.0) : new GridLength(0.0);

                brdStreamOptions.Visibility = Visibility.Visible;
                Animations.DoFadeIn(0.5, brdStreamOptions);
                Animations.DoFadeOut(0.5, meStreamingVideo);                
            }
            else
            {
                Animations.DoFadeIn(0.5, meStreamingVideo);
                Animations.DoFadeOut(0.5, brdStreamOptions, HaveHiddenStreamingOptionsWindow);
            }
        }
        void ShowHideHLSStreamingVideoOptionsWindow(bool show)
        {

            if (show)
            {
                brdHLSStreamOptions.Visibility = Visibility.Visible;
                Animations.DoFadeIn(0.5, brdHLSStreamOptions);
                Animations.DoFadeOut(0.5, meStreamingVideo);
            }
            else
            {
                Animations.DoFadeIn(0.5, meStreamingVideo);
                Animations.DoFadeOut(0.5, brdHLSStreamOptions, HaveHiddenHLSStreamingOptionsWindow);
            }
        }
        private void HaveHiddenStreamingOptionsWindow(object sender, EventArgs e)
        {
            brdStreamOptions.Visibility = Visibility.Collapsed;
        }
        private void HaveHiddenHLSStreamingOptionsWindow(object sender, EventArgs e)
        {
            brdHLSStreamOptions.Visibility = Visibility.Collapsed;
        }
        void populateWMSPStreamOptionsWindow()
        {

                // Show advanced options?
                SetDisplayForAdvancedStreamingOptions();

                int defStreamQuality = (int)(StreamingManager.DefaultStreamingQuality);
                if (cmbStreamQuality.Items.Count > defStreamQuality)
                    cmbStreamQuality.SelectedIndex = defStreamQuality;

                double percStartFrom;
                TimeSpan sDuration = StreamTVProgramme.Duration();
                if (sDuration.TotalSeconds > 0)
                    percStartFrom = (StartPosition.TotalSeconds / sDuration.TotalSeconds) * 100;
                else
                    percStartFrom = 0;

                sldStartFrom.Value = percStartFrom;

                SetDeinterlaceComboToDefaultValue();
            
        }
        void ShowWMSPStreamingVideoOptionsWindow()
        {
            ShowHideHLSStreamingVideoOptionsWindow(false);
            ShowHideWMSPStreamingVideoOptionsWindow(true);
        }
        private void sldStartFrom_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (StreamTVProgramme.Duration().TotalSeconds < 1)
            {
                lblStartFrom.Text = "(no duration available)";
            }
            else
            {
                if (sldStartFrom.Value == 0)
                {
                    lblStartFrom.Text = "Start";
                }
                else
                {
                    double percStartFrom = (sldStartFrom.Value / 100);
                    double secondsStartFrom = StreamTVProgramme.Duration().TotalSeconds * percStartFrom;
                    double iSecondsStartFrom = Math.Floor(secondsStartFrom);
                    StartPosition = TimeSpan.FromSeconds(iSecondsStartFrom);
                    lblStartFrom.Text = String.Format("{0:00}:{1:00}:{2:00}",
                        StartPosition.Hours, StartPosition.Minutes, StartPosition.Seconds);
                }
            }
        }
        private void btnShowHideAdvanced_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ToggleShowAdvancedStreamingOptions();
        }
        void ToggleShowAdvancedStreamingOptions()
        {
            showAdvancedStreamingOptions = (!showAdvancedStreamingOptions);

            SetDisplayForAdvancedStreamingOptions();
        }
        void SetDisplayForAdvancedStreamingOptions()
        {
            if (showAdvancedStreamingOptions)
            {
                rdAdvanced.Height = new GridLength(40);
                btnShowHideAdvanced.Text = "Hide advanced.";
            }
            else
            {
                rdAdvanced.Height = new GridLength(0);
                btnShowHideAdvanced.Text = "Show advanced.";
            }
        }
        /// <summary>
        /// If the current TV Programme to be streamed is a recorded TV file (
        /// </summary>
        void SetDeinterlaceComboToDefaultValue()
        {
            if (StreamTVProgramme.IsNotDTV) return;  // e.g. a video file, or movie: leave as DONT deinterlace

            // It's a Recorded TV Programme, should we deinterlace by default?
            bool deinterlaceByDefault = SettingsImporter.SettingIsTrue("DeinterlaceRecTVByDefault");

            if (deinterlaceByDefault)
                cmbInterlaceVideo.SelectedIndex = 1;
            else
                cmbInterlaceVideo.SelectedIndex = 0;  // safety
        }

        #endregion

        #region Post Stream Options Window
        void ShowHidePostStreamWindow(bool show)
        {
            ShowHidePostStreamWindow(show, "");
        }
        void ShowHidePostStreamWindow(bool show, string txtBlurb)
        {
            if (show)
            {
                // Hide progress waiter if it's showing
                VisualManager.HideProgressWaiterWithinGrid(LayoutRoot);

                lblPostPlayOptionsBlurb.Text = txtBlurb;
                brdPostPlayOptions.Visibility = Visibility.Visible;
                Animations.DoFadeIn(0.5, brdPostPlayOptions);
            }
            else
            {
                Animations.DoFadeOut(0.5, brdPostPlayOptions, HaveHiddenPostPlayWindow);
            }
        }
        private void HaveHiddenPostPlayWindow(object sender, EventArgs e)
        {
            brdPostPlayOptions.Visibility = Visibility.Collapsed;
        }
        private void btnPlayAgain_Click(object sender, RoutedEventArgs e)
        {
            ShowHidePostStreamWindow(false);
            ShowHideWMSPStreamingVideoOptionsWindow(true);
        }
        private void btnDismissPostPlayOptions_Click(object sender, RoutedEventArgs e)
        {
            CloseMe();
        }
        #endregion

        #region Zoom

        private void sldZoomLevel_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sldZoomLevel == null) return;

            ZoomVideoToZoomMultiplier(sldZoomLevel.Value);
        }

        double preFullScreenVideoWidth = 0.0;
        double preFullScreenVideoHeight = 0.0;
        void Content_FullScreenChanged(object sender, EventArgs e)
        {
            SizeVideoForFullOrNormalScreenSize();

            ShowOrHideSliderForFullScreen();
        }
        void SizeVideoForFullOrNormalScreenSize()
        {
            if (Application.Current.Host.Content.IsFullScreen)
            {
                preFullScreenVideoWidth = meStreamingVideo.ActualWidth;
                preFullScreenVideoHeight = meStreamingVideo.ActualHeight;

                ShowVideoFillingWindow();
            }
            else  // show at previous height, if there is one?
            {
                if ((preFullScreenVideoWidth > 0) && (preFullScreenVideoHeight > 0))
                {
                    meStreamingVideo.Width = preFullScreenVideoWidth;
                    meStreamingVideo.Height = preFullScreenVideoHeight;
                }
                else
                {
                    // Default zoom level
                    sldZoomLevel.Value = DefaultZoomSize;  // This sorts out the zoom window size...
                }
            }
        }
        void ShowVideoFillingWindow()
        {
            double targetWidth = Application.Current.Host.Content.ActualWidth;
            double targetHeight = Application.Current.Host.Content.ActualHeight;

            meStreamingVideo.Width = targetWidth;
            meStreamingVideo.Height = targetHeight;

//            double targetWidthRatio = targetWidth / meStreamingVideo.ActualWidth;
            //meStreamingVideo.Width = targetWidth;
           // meStreamingVideo.Height = meStreamingVideo.ActualHeight * targetWidthRatio;
        }
        void ShowOrHideSliderForFullScreen()
        {
            spZoomLevel.Visibility = (Application.Current.Host.Content.IsFullScreen) ? Visibility.Collapsed : Visibility.Visible;

        }
        private void ZoomVideoToZoomMultiplier(double ZoomMultiplier)
        {
            double naturalWidth = (meStreamingVideo.NaturalVideoWidth > 0) ? meStreamingVideo.NaturalVideoWidth : 360;
            double naturalHeight = (meStreamingVideo.NaturalVideoHeight > 0) ? meStreamingVideo.NaturalVideoHeight : 240;  // NTSC

            double targetWidth = naturalWidth * ZoomMultiplier;
            double targetHeight = naturalHeight * ZoomMultiplier;

            // Limit to window bounds
            /*
            if (targetWidth > Application.Current.Host.Content.ActualWidth)
                targetWidth = Application.Current.Host.Content.ActualWidth;

            if (targetHeight > Application.Current.Host.Content.ActualHeight)
                targetHeight = Application.Current.Host.Content.ActualHeight;
            */

            // Set the streaming vid bounds size; its stretch property is 'Uniform' so it'll maintain aspect ratio
            meStreamingVideo.Width = targetWidth;
            meStreamingVideo.Height = targetHeight;
        }
        double FrameZoomForQuality(WTVProfileQuality quality)
        {
            switch (quality)
            {
                case WTVProfileQuality.Low:
                    return 2.0;
                case WTVProfileQuality.Normal:
                    return 2.0;
                case WTVProfileQuality.Med:
                    return 1.5;
                case WTVProfileQuality.High:
                    return 1.5;
                case WTVProfileQuality.UltraHigh:
                    return 1.0;
                default:
                    return 1.0;
            }


        }
        #endregion

        #region OSD Fading
        DateTime OSDMouseLastMoved;
        bool OSDFaded = false;
        void CheckForOSDFades()
        {
            if (OSDMouseLastMoved == null) return;
            if (OSDFaded) return;

            TimeSpan elapsed = (DateTime.Now - OSDMouseLastMoved);
            if (elapsed.TotalSeconds > 5)
            {
                FadeOutOSD();
            }
        }

        private void LayoutRoot_MouseMove(object sender, MouseEventArgs e)
        {
            OSDMouseLastMoved = DateTime.Now;

            if (OSDFaded)
            {
                FadeInOSD();
            }
        }
        private void FadeInOSD()
        {
            Animations.DoFadeIn(0.3, brdOSDBottom);
            Animations.DoFadeIn(0.3, brdOSDTop);
            LayoutRoot.Cursor = null;
            OSDFaded = false;
        }
        private void FadeOutOSD()
        {
            Animations.DoFadeOut(1.0, brdOSDBottom);
            Animations.DoFadeOut(1.0, brdOSDTop);
            LayoutRoot.Cursor = Cursors.None;
            OSDFaded = true;
            
        }

        #endregion

        #region Volume Control

        private void btnVolume_MouseEnter(object sender, MouseEventArgs e)
        {
            FadeInVolumePopup();
        }
        void FadeInVolumePopup()
        {
            // Set correct slider value
            if (meStreamingVideo != null)
                volPopup.SetSliderValueTo(meStreamingVideo.Volume);

            volPopup.Opacity = 0.0;
            volPopup.Visibility = Visibility.Visible;
            Animations.DoFadeIn(0.3, volPopup);
        }
        private void volPopup_MouseLeave_1(object sender, MouseEventArgs e)
        {
            FadeOutVolumePopup();
        }
        void FadeOutVolumePopup()
        {
            Animations.DoFadeOut(0.3, volPopup, FadeOutVolumePopup_2);
        }
        void FadeOutVolumePopup_2(object sender, EventArgs e)
        {
            volPopup.Visibility = Visibility.Collapsed;

            if (meStreamingVideo != null)
                Settings.LastUsedVolumeLevel = meStreamingVideo.Volume;  
        }
        void volPopup_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SetVolumeToLevel(e.NewValue);
        }
        void SetVolumeToLevel(double newLevel)
        {
            meStreamingVideo.Volume = newLevel;
                    
            imgVolume.Source = ImageManager.LoadImageFromContentPath( (newLevel== 0.0) ? "/Images/btnVolIsOff.png" : "/Images/btnVolIsOn.png");
        }
        #endregion

        #region Stretch  / Aspect Ratio
        StandardAspectRatioNames CurrentAspectRatio = StandardAspectRatioNames.OriginalSize;
        bool animatingAspectRatioLabel = false;
        bool cancelAspectRatioLabelAnimation = false;
        void StretchVideoIfRequired()
        {
       
            // What is the aspect ratio of the existing video?
            double mediaAspectRatio = (double)meStreamingVideo.NaturalVideoWidth / (double)meStreamingVideo.NaturalVideoHeight;

            // How much must we squash/grow the Y axis to create the request ('CurrentAspectRatio') ratio?
            ScaleTransform newT = new ScaleTransform();
            newT.ScaleX = 1;
            newT.ScaleY = mediaAspectRatio / StandardAspectRatioValue(CurrentAspectRatio); // Scale Y axis to create requested ratio

            // Make the transform
            newT.CenterX = meStreamingVideo.ActualWidth / 2.0;
            newT.CenterY = meStreamingVideo.ActualHeight / 2.0;            
            meStreamingVideo.RenderTransform = newT;

            // Aspect Ratio Button Image
            rctAspectRatio.Height = (14 / StandardAspectRatioValue(  nextAspectRatio() ) );

            // Show label
            lblAspectRatio.Opacity = 0.0;
            lblAspectRatio.Text = strAspectRatio();

            if (!animatingAspectRatioLabel)
                FadeInAspectRatio();  // Begin showing label
            else
                cancelAspectRatioLabelAnimation = true;  // Label already animating, cancel then re-do animation
        }
        void FadeInAspectRatio()
        {
            cancelAspectRatioLabelAnimation = false;
            animatingAspectRatioLabel = true;
            Animations.DoFadeIn(0.2, lblAspectRatio, fadeInAspectRatio_Completed);
        }
        void fadeInAspectRatio_Completed(object sender, EventArgs e)
        {
            if (cancelAspectRatioLabelAnimation)
                FadeInAspectRatio();
            else
                Animations.DoFadeTo(1, lblAspectRatio, 0.95, fadeToAspectRatio_Completed);
        }
        void fadeToAspectRatio_Completed(object sender, EventArgs e)
        {
            if (cancelAspectRatioLabelAnimation)
                FadeInAspectRatio();
            else
                Animations.DoFadeOut(2, lblAspectRatio, fadeOutAspectRatio_Completed);
        }
        void fadeOutAspectRatio_Completed(object sender, EventArgs e)
        {
            if (cancelAspectRatioLabelAnimation)
                FadeInAspectRatio();
            else
                animatingAspectRatioLabel = false;
        }
        private void btnToggleAspectRatio_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ToggleCurrentAspectRatio();
            StretchVideoIfRequired();
        }
        
        void ToggleCurrentAspectRatio()
        {
            CurrentAspectRatio = nextAspectRatio();
        }
        StandardAspectRatioNames nextAspectRatio()
        {
            switch (CurrentAspectRatio)
            {
                case StandardAspectRatioNames.OriginalSize:
                    return StandardAspectRatioNames.SixteenByNine;
                case StandardAspectRatioNames.SixteenByNine:
                    return StandardAspectRatioNames.FourByThree;
                case StandardAspectRatioNames.FourByThree:
                    return StandardAspectRatioNames.WideScreenCinema;
                case StandardAspectRatioNames.WideScreenCinema:
                    return StandardAspectRatioNames.Square;
                default:
                    return StandardAspectRatioNames.OriginalSize;
            }

        }
        string strAspectRatio()
        {
            switch (CurrentAspectRatio)
            {
                case StandardAspectRatioNames.FourByThree:
                    return "4:3";
                case StandardAspectRatioNames.SixteenByNine:
                    return "16:9";
                case StandardAspectRatioNames.WideScreenCinema:
                    return "Widescreen";
                case StandardAspectRatioNames.Square:
                    return "Square";
                case StandardAspectRatioNames.OriginalSize:
                    return "Original";
                default:
                    return "?:?";
            }
        }
        enum StandardAspectRatioNames
        {
            OriginalSize,
            Square,
            SixteenByNine,
            FourByThree,
            WideScreenCinema
        }
        double StandardAspectRatioValue(StandardAspectRatioNames rtName)
        {
            switch (rtName)
            {
                case StandardAspectRatioNames.OriginalSize:
                    return (double)meStreamingVideo.NaturalVideoWidth / (double)meStreamingVideo.NaturalVideoHeight;
            
                case StandardAspectRatioNames.FourByThree:
                    return 4.0 / 3.0;
                case StandardAspectRatioNames.SixteenByNine:
                    return 16.0 / 9.0;
                case StandardAspectRatioNames.WideScreenCinema:
                    return 1.77;
                default:
                    return 1.0;
            }
        }
        #endregion



        #region IPage Members

        public void NotifyWillBeHidden()
        {
            StopStreaming();
            UnWireStreamingManagerEvents();
        }

        #endregion


        #region Choose Streaming Type

        void ShowStreamingChoice()
        {
            populateAskMeSteamingTypeCheckbox();
            Animations.DoFadeIn(0.2, brdChooseStreamType);
        }
        void populateAskMeSteamingTypeCheckbox()
        {
            cbAskStreamingType.IsChecked = Settings.AskForStreamingTypeEachTime;
        }
        private void cbAskStreamingType_Click(object sender, RoutedEventArgs e)
        {
            Settings.AskForStreamingTypeEachTime = cbAskStreamingType.IsChecked.Value;

            if (Settings.AskForStreamingTypeEachTime)
                VisualManager.ShowQuestionBox("Streaming type remembered", "To change the streaming type in future, visit the Settings page from the Main Menu.");
        }
        private void btnChooseHLSStreaming_MouseEnter(object sender, MouseEventArgs e)
        {
            gsChooseHLSStreaming.Color = Colors.White;
        }

        private void btnChooseHLSStreaming_MouseLeave(object sender, MouseEventArgs e)
        {
            gsChooseHLSStreaming.Color = Functions.HexColor("#888888");
        }

        private void btnChooseHLSStreaming_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Settings.EnableHTTPLiveStreaming = true;
            DoStreamingTypeChosen();
        }

        private void btnChooseWMSPStreaming_MouseEnter(object sender, MouseEventArgs e)
        {
            gsChooseHWMSPtreaming.Color = Colors.White;
        }

        private void btnChooseWMSPStreaming_MouseLeave(object sender, MouseEventArgs e)
        {
            gsChooseHWMSPtreaming.Color = Functions.HexColor("#888888");
        }

        private void btnChooseWMSPStreaming_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Settings.EnableHTTPLiveStreaming = false;
            DoStreamingTypeChosen();
        }

        void DoStreamingTypeChosen()
        {
            Animations.DoFadeOut(0.2, brdChooseStreamType, brdChooseStreamType_FadedOut);

            if (Settings.EnableHTTPLiveStreaming)
            {
                // HLS requires the stream info before showing the option window
                ProbeHLSStreams();
            }
            else
            {
                populateWMSPStreamOptionsWindow();
                ShowWMSPStreamingVideoOptionsWindow();
            }
        }

        void brdChooseStreamType_FadedOut(object sender, EventArgs e)
        {
            brdChooseStreamType.Visibility = System.Windows.Visibility.Collapsed;
        }
        #endregion



    }
}
