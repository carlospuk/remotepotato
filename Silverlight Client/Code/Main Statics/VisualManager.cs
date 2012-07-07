using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections;
using System.Collections.Generic;
using CommonEPG;

namespace SilverPotato
{
    public static class VisualManager
    {
        static bool CurrentWindowIsModal = false;

        static Canvas CanvasBackground;
        static Grid UberRootParent;
        static Grid RootParent;
        static Grid NetworkActivityContainerGrid;
        static Stack<FrameworkElement> OpenWindows;
        static Stack<TweenPage> OpenTweens;
        static Queue<FrameworkElement> scalingObjects;

        // Windows
        public static TaskBar TheTaskBar;
        public static EPGContainer TheEPGContainer;
        public static LoginPage TheLoginPage;
        public static SettingsPage TheSettingsPage;
        public static MovieGuidePage TheMovieGuidePage;
        public static SearchGuidePane TheSearchPane;
        public static RecordedTVPane TheRecordedTVPage;
        public static ScheduledRecordingsPage TheScheduledRecordingsPage;
        public static ManageSeriesPage TheManageSeriesPage;
        private static ActivitySpinner TheActivitySpinner;
        private static ActivitySpinner TheNetworkActivitySpinner;
        private static StreamingVideoPage streamingVideoPage;
        private static PictureViewingPage picViewingPage;
        private static MusicBrowser musicBrowser;
        public static MusicPlayerWindow MusicPlayer;
        static RemoteControlPane TheRemoteControlPane;
        static PictureBrowseRoot ThePicturesRoot;
        static VideosBrowseRoot TheVideosRoot;
        static MoviesBrowseRoot TheMovieRoot; // there are no children though
        static TextViewer TheDebugLog;
        public static QuestionBox questionBox;
        

        public static void Initialise(Canvas cvBackground, Grid uberRootParent, Grid rootParent, Grid gdNetworkActivityContainerGrid)
        {
            CanvasBackground = cvBackground;
            UberRootParent = uberRootParent;
            RootParent = rootParent;
            NetworkActivityContainerGrid = gdNetworkActivityContainerGrid;

            // Event for background move
            InitBackgroundLayers();
            UberRootParent.MouseMove += new MouseEventHandler(LayoutRoot_MouseMove);

            // Screen Space
            InitScreenSpace();

            OpenWindows = new Stack<FrameworkElement>();
            OpenTweens = new Stack<TweenPage>();
            scalingObjects = new Queue<FrameworkElement>();
            questionBox = new QuestionBox();
            
        }

        public static void InitialiseTaskBar(Grid taskBarParent)
        {
            TheTaskBar = new TaskBar();
            taskBarParent.Children.Add(TheTaskBar);
        }

        static bool displayingStreamingVideo = false;
        public static void ShowStreamingVideo(CommonEPG.TVProgramme tvp, TimeSpan startFrom)
        {
            if (streamingVideoPage != null)
            {
                UberRootParent.Children.Remove(streamingVideoPage);
                streamingVideoPage = null;
            }

            streamingVideoPage = new StreamingVideoPage(tvp, startFrom);
            UberRootParent.Children.Add(streamingVideoPage);
            displayingStreamingVideo = true;
        }
        public static void HideStreamingVideo()
        {
            if (streamingVideoPage == null) return;
            if (!displayingStreamingVideo) return;

            streamingVideoPage.NotifyWillBeHidden();

            UberRootParent.Children.Remove(streamingVideoPage);
            streamingVideoPage.Dispose();
            streamingVideoPage = null;
        } 

        public static bool displayingPictureViewer = false;
        public static void ShowPictureViewer(List<RPPictureItem> pics, int Index, ImageSource firstPreview)
        {
            if (picViewingPage != null)
            {
                UberRootParent.Children.Remove(picViewingPage);
                picViewingPage = null;
            }

            picViewingPage = new PictureViewingPage(pics, Index, firstPreview);
            picViewingPage.Opacity = 0.0;
            UberRootParent.Children.Add(picViewingPage);
            displayingPictureViewer = true;
            Animations.DoFadeIn(0.4, picViewingPage);
        }
        public static void HidePictureViewer()
        {
            if (picViewingPage == null) return;
            if (!displayingPictureViewer) return;

            Animations.DoFadeOut(0.4, picViewingPage, HidePictureViewer_Complete);
        }
        static void HidePictureViewer_Complete(object sender, EventArgs e)
        {
            UberRootParent.Children.Remove(picViewingPage);
            picViewingPage = null;
        }

        public static void ShowMusicPlayer()
        {
            if (MusicPlayer == null)
            {
                MusicPlayer = new MusicPlayerWindow();
                MusicPlayer.HorizontalAlignment = HorizontalAlignment.Right;
                MusicPlayer.VerticalAlignment = VerticalAlignment.Top;
                MusicPlayer.Margin = new Thickness(0, 40, 20, 0);
                MusicPlayer.MinimiseClicked += new EventHandler(MusicPlayer_MinimiseClicked);
            }

            ShowScreenSpaceWindow(MusicPlayer);
        }
        static void MusicPlayer_MinimiseClicked(object sender, EventArgs e)
        {
            MinimiseScreenSpaceWindow(MusicPlayer);
        }


        // Login page - special case visually, must cover the whole screen ie be modal
        public static event EventHandler<LoginPageCompleteEventArgs> LoginPageLoginDone;
        static bool fadingInLoginPage;
        public static void ShowLoginPage()
        {
            if (TheLoginPage == null)
            {
                TheLoginPage = new LoginPage();
                TheLoginPage.LoginPageDone += new EventHandler<LoginPageCompleteEventArgs>(TheLoginPage_LoginPageDone);
            }

            TheLoginPage.Opacity = 0.0;
            if (! UberRootParent.Children.Contains(TheLoginPage))
                UberRootParent.Children.Add(TheLoginPage);
            ScaleTo(0.3, TheLoginPage.brdLogin, 0.7, 1.0);
            Animations.DoFadeIn(0.4, TheLoginPage, TheLoginPage_FadedIn);
            fadingInLoginPage = true;
        }
        static void TheLoginPage_FadedIn(object sender, EventArgs e)
        {
            fadingInLoginPage = false;
        }
        static void TheLoginPage_LoginPageDone(object sender, LoginPageCompleteEventArgs e)
        {
            Animations.DoFadeOut(0.4, TheLoginPage, TheLoginPage_FadedOut);
            ScaleTo(0.3, TheLoginPage.brdLogin, 1.0, 2.5);
            //
            //PopOffScreenCurrentWindow();
            if (LoginPageLoginDone != null) LoginPageLoginDone(new object(), new LoginPageCompleteEventArgs(e.UN, e.PW));
        }
        static void TheLoginPage_FadedOut(object sender, EventArgs e)
        {
            if (fadingInLoginPage) return;  // don't remove it, as it's recently been re-activated and is re-displaying

            UberRootParent.Children.Remove(TheLoginPage);
            TheLoginPage = null;
        }


        #region Screen Space
        static List<FrameworkElement> ScreenSpaceWindows;
        //static Canvas cvScreenSpace;
        static Grid cvScreenSpace;
        public static void InitScreenSpace()
        {
            ScreenSpaceWindows = new List<FrameworkElement>();
            ClosingScreenSpaceWindows = new Queue<FrameworkElement>();
            MinimisingScreenSpaceWindows = new Queue<FrameworkElement>();
            FadingScreenSpaceWindows = new Queue<FrameworkElement>();

            cvScreenSpace = new Grid();
            UberRootParent.Children.Add(cvScreenSpace);
        }        
        public static void ShowScreenSpaceWindow(FrameworkElement window)
        {
            if (! ScreenSpaceWindows.Contains(window))
            {
                // Not yet in the screen space - add to space 
                ScreenSpaceWindows.Add(window);

                // Add to gui
                cvScreenSpace.Children.Add(window);

                // Add to taskbar
                TheTaskBar.AddRunningTask(window);
            }

            // Already faded up?
            if (window.Visibility == Visibility.Visible)
                return;

            window.Opacity = 0.0;
            window.Visibility = Visibility.Visible;
            cvScreenSpace.SetValue(Canvas.ZIndexProperty, Functions.ZOrderHighest);
            
            Animations.DoFadeIn(0.3, window);
            return;     
        }
        static void HideScreenSpaceWindows()
        {
            foreach (FrameworkElement window in ScreenSpaceWindows)
            {
                FadeScreenSpaceWindow(window);
            }
        }

        // Close windows
        static Queue<FrameworkElement> ClosingScreenSpaceWindows;
        public static void CloseScreenSpaceWindow(FrameworkElement window)
        {
            if (!ScreenSpaceWindows.Contains(window)) return;

            ClosingScreenSpaceWindows.Enqueue(window);
            Animations.DoFadeOut(0.3, window, CloseScreenSpaceWindow_Faded);
        }
        static void CloseScreenSpaceWindow_Faded(object sender, EventArgs e)
        {
            if (ClosingScreenSpaceWindows.Count < 1) return;

            FrameworkElement window = ClosingScreenSpaceWindows.Dequeue();
            window.Visibility = Visibility.Collapsed;

            // Remove from local store
            ScreenSpaceWindows.Remove(window);

            // Remove from canvas
            cvScreenSpace.Children.Remove(window);

            // Notify taskbar to remove

        }

        // Minimise windows
        static Queue<FrameworkElement> MinimisingScreenSpaceWindows;
        public static void MinimiseScreenSpaceWindow(FrameworkElement window)
        {
            if (!ScreenSpaceWindows.Contains(window)) return;

            MinimisingScreenSpaceWindows.Enqueue(window);
            MoveTo(0.3, window, -300, 300); // fly away towards bottom left
            Animations.DoFadeOut(0.25, window, MinimiseScreenSpaceWindow_Faded);
        }
        static void MinimiseScreenSpaceWindow_Faded(object sender, EventArgs e)
        {
            if (MinimisingScreenSpaceWindows.Count < 1) return;

            FrameworkElement window = MinimisingScreenSpaceWindows.Dequeue();
            window.Visibility = Visibility.Collapsed;
        }

        // Minimise windows
        static Queue<FrameworkElement> FadingScreenSpaceWindows;
        public static void FadeScreenSpaceWindow(FrameworkElement window)
        {
            if (!ScreenSpaceWindows.Contains(window)) return;
            if (window.Opacity < 1.0) return;  // already faded / fading


            MinimisingScreenSpaceWindows.Enqueue(window);
            Animations.DoFadeOut(0.2, window, MinimiseScreenSpaceWindow_Faded);
        }
        static void FadeScreenSpaceWindow_Faded(object sender, EventArgs e)
        {
            if (FadingScreenSpaceWindows.Count < 1) return;

            FrameworkElement window = MinimisingScreenSpaceWindows.Dequeue();
            window.Visibility = Visibility.Collapsed;
        }


        // Maximise
        public static void MaximiseScreenSpaceWindow(FrameworkElement window)
        {
            if (!ScreenSpaceWindows.Contains(window)) return;
            if (window.Opacity > 0.0) return;  // already maximised

            window.Opacity = 0.0; // fade in
            window.Visibility = Visibility.Visible;
            cvScreenSpace.SetValue(Canvas.ZIndexProperty, Functions.ZOrderHighest);
            MoveTo(0.25, window, 0, 0); // fly up to normal position
            Animations.DoFadeIn(0.25, window);
        }

        #endregion

        #region Show Stack Pages
        public static void ShowEPG()
        {
            if (TheEPGContainer == null)
            {
                TheEPGContainer = new EPGContainer();
                PushOntoScreenStack(TheEPGContainer, 0.0, 0.25, false);
                TheEPGContainer.Initialize();
            }
            else
            {
                PushOntoScreenStack(TheEPGContainer, 0.0, 0.25, false);
                TheEPGContainer.EnableAutoRefresh();  // disabled when removing (PopOffCurrentWindow)
            }
        }
        public static void ShowScheduledRecordings()
        {
            if (TheScheduledRecordingsPage == null)
            {
                TheScheduledRecordingsPage = new ScheduledRecordingsPage();
                PushOntoScreenStack(TheScheduledRecordingsPage);
            }
            else
            {
                PushOntoScreenStack(TheScheduledRecordingsPage);
                TheScheduledRecordingsPage.Fill();
            }
        }
        public static void ShowRecordedTV()
        {
            if (TheRecordedTVPage == null)
            {
                TheRecordedTVPage = new RecordedTVPane();
                PushOntoScreenStack(TheRecordedTVPage);
            }
            else
            {
                // TheRecordedTVPage.RefreshRecordings(); NO NEED TO REFRESH AGAIN
                PushOntoScreenStack(TheRecordedTVPage);
            }
        }
        public static void ShowMovieGuide()
        {
            if (TheMovieGuidePage == null)
            {
                TheMovieGuidePage = new MovieGuidePage();
                PushOntoScreenStack(TheMovieGuidePage);
            }
            else
            {
                PushOntoScreenStack(TheMovieGuidePage);
            }
        }
        public static void ShowPictures()
        {
            if (ThePicturesRoot == null)
            {
                ThePicturesRoot = new PictureBrowseRoot();
                PushOntoScreenStack(ThePicturesRoot);
            }
            else
            {
                PushOntoScreenStack(ThePicturesRoot);
            }
        }
        public static void ShowVideos()
        {
            if (TheVideosRoot == null)
            {
                TheVideosRoot = new VideosBrowseRoot();
                PushOntoScreenStack(TheVideosRoot);
            }
            else
            {
                PushOntoScreenStack(TheVideosRoot);
            }
        }
        public static void ShowMovies()
        {
            if (TheMovieRoot == null)
            {
                TheMovieRoot = new MoviesBrowseRoot();
                PushOntoScreenStack(TheMovieRoot);
            }
            else
            {
                PushOntoScreenStack(TheMovieRoot);
            }
        }
        public static void ShowMusic()
        {
            if (musicBrowser == null)
            {
                musicBrowser = new MusicBrowser();
                PushOntoScreenStack(musicBrowser);
            }
            else
            {
                PushOntoScreenStack(musicBrowser);
            }
        }
        public static void ShowRemoteControl()
        {
            if (TheRemoteControlPane == null)
            {
                TheRemoteControlPane = new RemoteControlPane();
                PushOntoScreenStack(TheRemoteControlPane);
            }
            else
            {
                PushOntoScreenStack(TheRemoteControlPane);
            }
        }
        public static void ShowManageSeries()
        {
            if (TheManageSeriesPage == null)
            {
                TheManageSeriesPage = new ManageSeriesPage();
                PushOntoScreenStack(TheManageSeriesPage);
            }
            else
            {
                PushOntoScreenStack(TheManageSeriesPage);
                TheManageSeriesPage.Fill();
            }
        }
        public static void ShowSearchPane()
        {
            if (TheSearchPane == null)
            {
                TheSearchPane = new SearchGuidePane();
                PushOntoScreenStack(TheSearchPane);
            }
            else
            {
                PushOntoScreenStack(TheSearchPane);
                TheSearchPane.ResetSearchForm();
            }
        }
        public static void ShowSettingsPage()
        {
            if (TheSettingsPage == null)
            {
                TheSettingsPage = new SettingsPage();
                PushOntoScreenStack(TheSettingsPage);
            }
            else
            {
                PushOntoScreenStack(TheSettingsPage);
            }
        }
        public static void ShowDebugLog()
        {
            if (TheDebugLog == null)
            {
                TheDebugLog = new TextViewer(Functions.CurrentLog.ToString());
                PushOntoScreenStack(TheDebugLog);
            }
            else
            {
                TheDebugLog.RefreshText(Functions.CurrentLog.ToString());
                PushOntoScreenStack(TheDebugLog);
            }
        }
        #endregion

        #region Show Question Box
        public static void ShowQuestionBox(string title, string body)
        {
            questionBox.SetText(title, body);
            questionBox.Show();
        }
        public static bool QuestionBoxDialogResult
        {
            get
            {
                return (questionBox.DialogResult.HasValue) ? questionBox.DialogResult.Value : false;
            }
        }
        #endregion

        #region Push onto main screen stack
        public static void PopOffBackToPrimary()
        {
            PopOffBackToPrimary(false);
        }
        public static void PopOffBackToPrimary(bool forceCloseModal)
        {
            if (
                (!forceCloseModal) &&
                (CurrentWindowIsModal)
                ) return;

            // Hide any screen space windows
            HideScreenSpaceWindows();

            //try
            {
                while (OpenWindows.Count > 0)
                {
                    FrameworkElement winToRemove = OpenWindows.Pop();
                    NotifyWindowOfClosing(winToRemove);
                    RootParent.Children.Remove(winToRemove);
                    // TODO: dispose ?
                    winToRemove = null;

                    TweenPage tp = OpenTweens.Pop();
                    RootParent.Children.Remove(tp);
                    tp = null;
                }

                ShowMainMenu();
     
            }
           // catch
            {
            }
        }
        public static void PushOntoScreenStack(FrameworkElement newWindow)
        {
            PushOntoScreenStack(newWindow, false);
        }
        public static void PushOntoScreenStack(FrameworkElement newWindow, bool isModal)
        {
            PushOntoScreenStack(newWindow, 0.0, 0.34, isModal);
        }
        public static void PushOntoScreenStack(FrameworkElement newWindow, double OpacityForOldWindow, double FadeInDurationSecs, bool isModal)
        {
            if (newWindow == null) return;
            if (newWindow.Parent != null)
            {
                Functions.WriteLineToLogFile("Not adding new window - is already child of another window.");
                return;
            }
            if (CurrentWindowIsModal)
            {
                // will not push anything on top of a modal window - SHOULD BE ALLOWED TO OVERRIDE THIS
                MessageBox.Show("Cannot display new window: the current window must be closed.", "Cannot show new window", MessageBoxButton.OK);
                return;
            }

            CurrentWindowIsModal = isModal;

            // Hide any screen space windows
            HideScreenSpaceWindows();

            // Fade out and scale back old window
            if (OpenWindows.Count == 0)
            {
                // fade main menu
                HideMainMenu();
            }
            else
            {
                // fade current window
                FrameworkElement currentWindow = OpenWindows.Peek();
                Animations.DoFadeTo(FadeInDurationSecs, currentWindow, OpacityForOldWindow);
                ScaleTo(FadeInDurationSecs, currentWindow, 2.5);
            }

            // Insert a tween panel
            InsertTween();

            // Display new window and scale up
            OpenWindows.Push(newWindow);
            RootParent.Children.Add(newWindow);  // can cause error if already child window
            newWindow.SetValue(FrameworkElement.OpacityProperty, 0.0);
            //newWindow.SetValue(Canvas.ZIndexProperty, Functions.ZOrderHighest);  // MUST be for debug, as will raise it above all tweens!
            Animations.DoFadeIn(FadeInDurationSecs, newWindow);
            ScaleTo(FadeInDurationSecs, newWindow, 0.3, 1.0);
            
        }
        private static void InsertTween()
        {
            TweenPage tp = new TweenPage();
            tp.Clicked += new EventHandler(TweenPage_Clicked);
            OpenTweens.Push(tp);
            RootParent.Children.Add(tp);
        }
        static void TweenPage_Clicked(object sender, EventArgs e)
        {
            PopOffScreenStackCurrentWindow();
        }
        private static FrameworkElement fadingOutElement;
        public static void PopOffScreenStackCurrentWindow()
        {
            PopOffScreenStackCurrentWindow(false);
        }
        public static void PopOffScreenStackCurrentWindow(bool forceCloseModal)
        {
            // Cannot pop off when a modal window is displayed
            if (
                (!forceCloseModal) && 
                (CurrentWindowIsModal)
                )
                return;

            // No longer modal
            CurrentWindowIsModal = false;

            // Hide any screen space windows
            HideScreenSpaceWindows();

            if (OpenWindows.Count > 0)  
            {
                fadingOutElement = OpenWindows.Pop();
                NotifyWindowOfClosing(fadingOutElement);
                ScaleTo(0.25, fadingOutElement, 0.3);
                Animations.DoFadeOut(0.25, fadingOutElement, PopOffScreen_Faded);
            }

            if (OpenTweens.Count > 0)
            {
                TweenPage tp = OpenTweens.Pop();
                RootParent.Children.Remove(tp);
                // Close ?
                tp = null;
            }

            // Fade back up old window
            if (OpenWindows.Count > 0)
            {
                FrameworkElement currentWindow = OpenWindows.Peek();
                Animations.DoFadeTo(0.25, currentWindow, 1.0);
                ScaleTo(0.25, currentWindow, 1.0);
            }
            else
            {
               // fade back up main menu
                ShowMainMenu();
            }
        }
        public static void PopOffScreen_Faded(object sender, EventArgs e)
        {
            RootParent.Children.Remove(fadingOutElement);

            // Remove local (static) references to object to avoid null pointers
            if (fadingOutElement.Equals(TheManageSeriesPage))
                TheManageSeriesPage = null;
            else if (fadingOutElement.Equals(TheScheduledRecordingsPage))
                TheScheduledRecordingsPage = null;
            else if (fadingOutElement.Equals(TheSettingsPage))
                TheSettingsPage = null;
            else if (fadingOutElement.Equals(TheSearchPane))
                TheSearchPane = null;
            else if (fadingOutElement.Equals(TheLoginPage))
                TheLoginPage = null;
            else if (fadingOutElement.Equals(ThePicturesRoot))
            {
                // Keep it!
            }
            else if (fadingOutElement is EPGContainer)
            {
                EPGContainer epgc = (EPGContainer)fadingOutElement;
                epgc.DisableAutoRefresh();  // stop it refreshing while it's hidden
            }
            else
            {
                // Dispose all unknown elements
                fadingOutElement = null;
            }

            GC.Collect();
        }
        static void NotifyWindowOfClosing(FrameworkElement window)
        {
            IPage currentPage = window as IPage;
            if (currentPage != null)  // i.e. it implements IPage
            {
                currentPage.NotifyWillBeHidden();
            }


        }
        #endregion

        // Helper - scale
        private static void ScaleTo(double durationSecs, FrameworkElement fe, double ScaleFrom, double ScaleTo)
        {
            ScaleTransform st = AddScaleTransformIfNeeded(fe);
            scalingObjects.Enqueue(fe);
            Animations.DoAnimation(durationSecs, st, "(ScaleX)", "(ScaleY)", ScaleFrom, ScaleTo, null, ScaleFrom, ScaleTo, null, false, ScaleTo_Completed);
        }
        private static void ScaleTo(double durationSecs, FrameworkElement fe, double ScaleTo)
        {
            ScaleTransform st = AddScaleTransformIfNeeded(fe);
            scalingObjects.Enqueue(fe);
            Animations.DoAnimation(durationSecs, st, "(ScaleX)", "(ScaleY)", null, ScaleTo, null, null, ScaleTo, null, false, ScaleTo_Completed);
        }
        private static ScaleTransform AddScaleTransformIfNeeded(FrameworkElement fe)
        {
            ScaleTransform st;
            if (!(fe.RenderTransform is ScaleTransform))
            {
                st = new ScaleTransform();
                st.ScaleX = 1.0;
                st.ScaleY = 1.0;
                fe.RenderTransformOrigin = new Point(0.5, 0.5);
                fe.RenderTransform = st;
            }
            else
                st = (ScaleTransform)fe.RenderTransform;

            return st;

        }
        private static void ScaleTo_Completed(object sender, EventArgs e)
        {
            FrameworkElement fe = scalingObjects.Dequeue();
            if (fe.RenderTransform is ScaleTransform)
            {
                ScaleTransform st = (ScaleTransform)fe.RenderTransform;
                if ((st.ScaleX == 1.0) && (st.ScaleY == 1.0))
                    fe.RenderTransform = null;
            }
        }

        // Helper move
       // static Queue<FrameworkElement> MovingObjects;
        static void MoveTo(double durationSecs, FrameworkElement fe, double Xoffset, double Yoffset)
        {
            TranslateTransform tt = AddTranslateTransformIfNeeded(fe);
         //   MovingObjects.Enqueue(fe);
            Animations.DoAnimation(durationSecs, tt, "(X)", "(Y)", null, Xoffset, null, null, Yoffset, null, false, MoveTo_Completed);
        }
        private static void MoveTo_Completed(object sender, EventArgs e)
        {
   
        }

        static TranslateTransform AddTranslateTransformIfNeeded(FrameworkElement fe)
        {
            TranslateTransform tt;
            if (!(fe.RenderTransform is TranslateTransform))
            {
                tt = new TranslateTransform();
                tt.X = 0;
                tt.Y = 1;
                fe.RenderTransformOrigin = new Point(0, 0);
                fe.RenderTransform = tt;
            }
            else
                tt = (TranslateTransform)fe.RenderTransform;

            return tt;
        }


        #region Show Activity Spinners
        private static bool ShowingActivityModal = false;
        public static void ShowActivityModal()
        {
            if (ShowingActivityModal) return;

            if (TheActivitySpinner == null)
                TheActivitySpinner = new ActivitySpinner(3.0);
            TheActivitySpinner.Opacity = 0.0;

            RootParent.Children.Add(TheActivitySpinner);
            Animations.DoFadeIn(0.4, TheActivitySpinner);
            ShowingActivityModal = true;
        }
        public static void HideActivityModal()
        {
            if (!ShowingActivityModal) return;

            Animations.DoFadeOut(0.2, TheActivitySpinner, HideActivityModal_2);
        }
        static void HideActivityModal_2(object sender, EventArgs e)
        {
            try
            {
                RootParent.Children.Remove(TheActivitySpinner);
            }
            catch { }

            TheActivitySpinner = null;

            ShowingActivityModal = false;
        }

        private static bool ShowingNetworkActivity = false;
        public static void ShowNetworkActivity()
        {
            NetworkActivityContainerGrid.Dispatcher.BeginInvoke(ShowNetworkActivity2);
        }
        public static void ShowNetworkActivity2()
        {
            if (ShowingNetworkActivity) return;
            if (NetworkActivityContainerGrid.Children.Count > 0) return;

            if (TheNetworkActivitySpinner == null)
                TheNetworkActivitySpinner = new ActivitySpinner(1.0);
            
            NetworkActivityContainerGrid.Children.Add(TheNetworkActivitySpinner);
            ShowingNetworkActivity = true;
        }
        public static void HideNetworkActivity()
        {
            if (!ShowingNetworkActivity) return;

            try  // can raise a strange 'operation is not valid due to the current state of the object' error
            {
                NetworkActivityContainerGrid.Children.Remove(TheNetworkActivitySpinner);
            }
            catch { }
            TheNetworkActivitySpinner = null;

            ShowingNetworkActivity = false;
        }
        

        public static void ShowActivityWithinGrid(Grid gdParent)
        {
            ShowActivityWithinGrid(gdParent, 2.0);
        }
        public static void ShowActivityWithinGrid(Grid gdParent, double TheScale)
        {
            ShowActivityWithinGrid(gdParent, TheScale, Colors.White);
        }
        public delegate void dShowActivityWithinGrid(Grid gdParent, double TheScale, Color OutlineColor);
        public static void ShowActivityWithinGrid(Grid gdParent, double TheScale, Color OutlineColor)
        {
            dShowActivityWithinGrid d = new dShowActivityWithinGrid(ShowActivityWithinGrid2);
            gdParent.Dispatcher.BeginInvoke(d, gdParent, TheScale, OutlineColor);
        }
        public static void ShowActivityWithinGrid2(Grid gdParent, double TheScale, Color OutlineColor)
        {
            ActivitySpinner spinner;
            if (isSpinnerWithinGrid(gdParent, out spinner)) return;

            // Spinner not already found - create it
            ActivitySpinner actSpin = new ActivitySpinner(TheScale);
            if (OutlineColor != Colors.White) actSpin.setOutlineColour(OutlineColor);
            actSpin.Tag = "ActivitySpinner";
            actSpin.Opacity = 0.0;
            gdParent.Children.Add(actSpin);
            Animations.DoFadeIn(0.4, actSpin);
        }
        static Stack<Grid> hidingActivityWithinGridGrids = new Stack<Grid>();
        public static void HideActivityWithinGrid(Grid gdParent)
        {
            ActivitySpinner spinner;
            if (! isSpinnerWithinGrid(gdParent, out spinner)) return;
            hidingActivityWithinGridGrids.Push(gdParent);
            Animations.DoFadeOut(0.2, spinner, HideActivityWithinGrid_2);
        }
        static void HideActivityWithinGrid_2(object sender, EventArgs e)
        {
            ActivitySpinner spinner;
            Grid gdParent = hidingActivityWithinGridGrids.Pop();
            if (!isSpinnerWithinGrid(gdParent, out spinner)) return;
            gdParent.Children.Remove(spinner);
            spinner = null;
        }
        private static bool isSpinnerWithinGrid(Grid gdParent, out ActivitySpinner spinner)
        {
            spinner = null;

            foreach (FrameworkElement fe in gdParent.Children)
            {
                if (fe.Tag != null)
                    if (fe.Tag is String)
                    {
                        String strTag = (String)fe.Tag;
                        if (strTag.Equals("ActivitySpinner"))
                        {
                            spinner = (ActivitySpinner)fe;
                            return true;
                        }
                    }
            }

            return false;
        }
        #endregion

        #region Progress Waiter
        public static void ShowProgressWaiterWithinGrid(Grid gdParent, double TheScale, string message1, string message2)
        {
            // Check it's not already in the grid
            ProgressWaiter testPW;
            if (isProgressWaiterWithinGrid(gdParent, out testPW))
            {


                // if it's awaiting removal, give it respite!  cancel the removal.
                if (testPW.Equals(PWawaitingremoval))
                {
                    PWawaitingremoval = null;
                    PWawaitingremovalGrid = null;
                }
                else
                {

                    // bring to front
                    testPW.SetValue(Canvas.ZIndexProperty, Functions.ZOrderHighest);
                }


                return;
            }

            // Add it
            ProgressWaiter pw = new ProgressWaiter(message1);
            pw.Tag = "PROGRESSWAITER";
            pw.Opacity = 0.0;
            gdParent.Children.Add(pw);

            // bring to front
            pw.SetValue(Canvas.ZIndexProperty, Functions.ZOrderHighest);

            Animations.DoFadeIn(0.4, pw);
        }
        static ProgressWaiter PWawaitingremoval;
        static Grid PWawaitingremovalGrid;
        public static void HideProgressWaiterWithinGrid(Grid gdParent)
        {
            ProgressWaiter testPW;
            if (! isProgressWaiterWithinGrid(gdParent, out testPW)) return;

            PWawaitingremoval = testPW;
            PWawaitingremovalGrid = gdParent;
            Animations.DoFadeOut(0.4, testPW, fadedOut_ProgressWaiter);
            gdParent.Children.Remove(testPW);
            testPW = null;
        }
        private static void fadedOut_ProgressWaiter(object sender, EventArgs e)
        {
            if (PWawaitingremoval == null) return;  // it was given respite
            PWawaitingremovalGrid.Children.Remove(PWawaitingremoval);
            PWawaitingremoval = null;
            PWawaitingremovalGrid = null;
        }
        public static void UpdateProgressWaiter(Grid gdParent, string message1, string message2)
        {
            ProgressWaiter pw;
            if (!isProgressWaiterWithinGrid(gdParent, out pw)) return;  // not found

            pw.SetText(message1, message2);
        }
        private static bool isProgressWaiterWithinGrid(Grid gdParent, out ProgressWaiter pw)
        {
            pw = null;

            foreach (FrameworkElement fe in gdParent.Children)
            {
                if (fe.Tag != null)
                    if (fe.Tag is String)
                    {
                        String strTag = (String)fe.Tag;
                        if (strTag == "PROGRESSWAITER")
                        {
                            pw = (ProgressWaiter)fe;
                            return true;
                        }
                    }
            }

            return false;
        }
        #endregion


        // New background
        static List<LayerBase> bgLayers;
        static void InitBackgroundLayers()
        {
            // Clear lists / dict
            bgLayers = new List<LayerBase>();


            // Add three layers
            LayerBase bgLayerD = new LayerBlobs(0.02, 0.4, 16, 100);
            AddLayer(ref bgLayerD);
            LayerBase bgLayerC = new LayerBlobs(0.03, 0.6, 16, 200);
            AddLayer(ref bgLayerC);
            LayerBase bgLayerB = new LayerBlobs(0.04, 0.8, 16, 300);
            AddLayer(ref bgLayerB);
            LayerBase bgLayerA = new LayerBlobs(0.06, 1.0, 14, 400);
            AddLayer(ref bgLayerA);
          

        }
        static bool MainMenuCreated = false;
        static bool MainMenuHidden = false;
        static LayerBase lay_MenuMenu;
        public static void ShowMainMenu()
        {
            if (!MainMenuCreated)
            {
                lay_MenuMenu = new LayerMenu(80);
                lay_MenuMenu.Opacity = 0.0;
                AddLayer(ref lay_MenuMenu);
                Animations.DoFadeIn(0.25, lay_MenuMenu);
                MainMenuCreated = true;
            }
            else
            {
                ScaleTo(0.25, lay_MenuMenu, 1.0);
                Animations.DoFadeIn(0.4, lay_MenuMenu);
            }

            MainMenuHidden = false;
        }
        public static void HideMainMenu()
        {
            if (lay_MenuMenu == null) return;

            ScaleTo(0.25, lay_MenuMenu, 3);
            Animations.DoFadeOut(0.25, lay_MenuMenu);
            MainMenuHidden = true;
        }
        public static void SetMenuTitle(string txtTitle)
        {
            if (MainMenuCreated)
            {
                LayerMenu mainmenu = (LayerMenu)lay_MenuMenu;
                mainmenu.SetMenuTitle(txtTitle);
            }
        }
        public static void SetMenuIsShowingEPG(bool showEPG)
        {
            if (MainMenuCreated)
            {
                LayerMenu mainmenu = (LayerMenu)lay_MenuMenu;
                mainmenu.ShowHideTVGuideMenuItem(showEPG);
            }
        }
        static void AddLayer(ref LayerBase layer)
        {
            CanvasBackground.Children.Add(layer);
            layer.SetValue(Canvas.ZIndexProperty, Functions.ZOrderHighest);
            bgLayers.Add(layer);

        }
        static void LayoutRoot_MouseMove(object sender, MouseEventArgs e)
        {
            if ((MainMenuHidden) || (Settings.DisableMovingBackground))
                return;
            

            Point pt = e.GetPosition(UberRootParent);

            double xPercent = pt.X / UberRootParent.ActualWidth;
            double yPercent = pt.Y / UberRootParent.ActualHeight;

            if (xPercent > 2) return;
            if (yPercent > 2) return;

            xPercent = xPercent - 0.5;  // offset from middle of screen
            yPercent = yPercent - 0.5;

            // temp change


            foreach (LayerBase layer in bgLayers)
            {
                double setX = layer.MovementAmount * xPercent;
                double setY = layer.MovementAmount * yPercent;

                if (!(layer is LayerMenu))  // menu only goes up and down
                    Animations.DoAnimation(0.8, layer.ttTsltTsfm, "(X)", null, -setX, null, false, new ExponentialEase(), null);

                Animations.DoAnimation(0.8, layer.ttTsltTsfm, "(Y)", null, -setY, null, false, new ExponentialEase(), null);

            }
        }



    }
}
