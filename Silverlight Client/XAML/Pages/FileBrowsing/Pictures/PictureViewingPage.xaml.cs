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
using System.Windows.Threading;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using CommonEPG;
using FatAttitude.WTVTranscoder;

namespace SilverPotato
{
    public partial class PictureViewingPage : UserControl, IDisposable
    {
        int CurrentPictureIndex;
        List<RPPictureItem> Pictures;
        DispatcherTimer generalTimer;
        double DefaultPictureSize = 600;
        bool SlideShowPlaying = false;

        enum VideoZoomLevels
        {
            Small,
            Med,
            Large,
            XLarge,
            FullWindow,
            FullScreen
        }

        public PictureViewingPage()
        {
            InitializeComponent();

            generalTimer = new DispatcherTimer();
            generalTimer.Interval = TimeSpan.FromSeconds(0.2);
            generalTimer.Tick += new EventHandler(generalTimer_Tick);
            generalTimer.Start();

            imgCurrentPicture.ImageOpened += new EventHandler<RoutedEventArgs>(imgCurrentPicture_ImageOpened);
            imgCurrentPicture.ImageFailed += new EventHandler<ExceptionRoutedEventArgs>(imgCurrentPicture_ImageFailed);
            Application.Current.Host.Content.FullScreenChanged += new EventHandler(Content_FullScreenChanged);
        }
        public PictureViewingPage(List<RPPictureItem> pics, int Index, ImageSource previewSource)
            : this()
        {
            Pictures = pics;
            CurrentPictureIndex = Index;
            bool foo = ValidateCurrentIndex();

            imgOverlayPicture.Source = previewSource;
            imgOverlayPicture.Dispatcher.BeginInvoke(ShowOverlayPicFillingWindow); // give it a chance to update first
            //ResetPicPositionAndSize();

            LoadCurrentPicture();
        }
        public void Dispose()
        {
            if (generalTimer.IsEnabled)
                generalTimer.Stop();
            generalTimer = null;
        }
        RPPictureItem CurrentPicture
        {
            get
            {
                if (Pictures == null) return null;

                if (! (Pictures.Count > CurrentPictureIndex)) return null;
                return Pictures[CurrentPictureIndex];
            }
        }


        void CloseMe()
        {
            VisualManager.HidePictureViewer();
        }


        #region Show / Load Pics
        void LoadCurrentPicture()
        {
            if (CurrentPicture == null) return;

            VisualManager.ShowActivityWithinGrid(LayoutRoot, 3.0);

            
            double multiplier = 0.0;

            // Load either thumbnail (low quality) or entire image
            switch (cmbPictureQuality.SelectedIndex )
            {
                case 0:
                    multiplier = 0.25;
                    break;

                    case 1:
                    multiplier = 0.5;
                    break;

                    case 2:
                    multiplier = 1.2;
                    break;

                    case 3:
                    multiplier = 2.2;
                    break;

                    case 4:
                    multiplier = 4;
                    break;
            }

            double browserWidth = App.Current.Host.Content.ActualWidth;
            double browserHeight = App.Current.Host.Content.ActualHeight;
            Size frameSize = new Size((int)browserWidth * multiplier, (int)browserHeight * multiplier);
            Uri newUri = CurrentPicture.SourceUriOrNull(frameSize);
            imgCurrentPicture.Source = new BitmapImage(newUri);
        }


        void wClient_GetStringByPostingCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            RPWebClient wClient = (RPWebClient)sender;
            wClient.GetStringByPostingCompleted -= new EventHandler<UploadStringCompletedEventArgs>(wClient_GetStringByPostingCompleted);

            
        }
        void CopyCurrentPictureToOverlay()
        {
            imgOverlayPicture.Source = null;
            imgOverlayPicture.Source = imgCurrentPicture.Source;  // put first so it adjusts its width / height etc

            imgOverlayPicture.Width = imgCurrentPicture.ActualWidth;
            imgOverlayPicture.Height = imgCurrentPicture.ActualHeight;
            scaleTransformOverlay.ScaleX = scaleTransform.ScaleX;
            scaleTransformOverlay.ScaleY = scaleTransform.ScaleY;

            gdOverlayPicture.Margin = gdCurrentPicture.Margin;
            
            imgOverlayPicture.Visibility = Visibility.Visible;
            imgOverlayPicture.Opacity = 1.0;
        }
        void imgCurrentPicture_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            VisualManager.HideActivityWithinGrid(LayoutRoot);
            MessageBox.Show("Cannot load picture - see log for details.");
            Functions.WriteExceptionToLogFile(e.ErrorException);
        }
        void imgCurrentPicture_ImageOpened(object sender, RoutedEventArgs e)
        {
            lblPicTitle.Text = CurrentPicture.Title;
            ResetPicPositionAndSize();

            VisualManager.HideActivityWithinGrid(LayoutRoot);
            FadeOutOverlayPicture();
        }
        void FadeOutOverlayPicture()
        {
            Animations.DoFadeOut(0.2, imgOverlayPicture, FadeOutOverlayPicture_Complete);
        }
        void FadeOutOverlayPicture_Complete(object sender, EventArgs e)
        {
            imgOverlayPicture.Visibility = Visibility.Collapsed;
        }
        #endregion

        #region Timer
        void generalTimer_Tick(object sender, EventArgs e)
        {
            CheckForOSDFades();

            if (SlideShowPlaying)
                CheckForSlideShowFlip();
        }
        #endregion


        #region Slideshow
        
        DateTime dtNextSlideShowFlip;
        private void btnStartStopSlideshow_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (SlideShowPlaying)
                StopSlideShow();
            else
                StartSlideShow();
        }
        private void StartSlideShow()
        {
            Functions.ChangeImageTo(imgStartStopSlideshow, "/Images/btnStop.png");
            SetNextSlideShowFlipTime();
            FadeOutOSD();
            rctSlideShowIntervalTime.Height = 0;
            rctSlideShowIntervalTime.Opacity = 0.7;
            SlideShowPlaying = true;
        }
        void StopSlideShow()
        {
            Functions.ChangeImageTo(imgStartStopSlideshow, "/Images/btnPlay.png");
            Animations.DoFadeOut(1, rctSlideShowIntervalTime);
            SlideShowPlaying = false;
        }
        void CheckForSlideShowFlip()
        {
            double secsRemaining = dtNextSlideShowFlip.Subtract(DateTime.Now).TotalSeconds;

            DrawSSRectangle(secsRemaining);

            if (secsRemaining <= 0)
                SlideShowFlipNext();
        }
        void SlideShowFlipNext()
        {
            // Any more?
            if (TryShowNextPic())
            {
                SetNextSlideShowFlipTime();
            }
            else
                StopSlideShow();
        }
        void SetNextSlideShowFlipTime()
        {
            dtNextSlideShowFlip = DateTime.Now.AddSeconds(Settings.SlideShowInterval);             // Don't do again for a while
        }
        void DrawSSRectangle(double SecsRemaining)
        {
            // Height proportional to time elapsed
            double dMaxInterval = Settings.SlideShowInterval;
            if (dMaxInterval < 1) dMaxInterval = 1; // avoid div by zero
            double dSecsElapsed = dMaxInterval - SecsRemaining;
            double perc = (dSecsElapsed / dMaxInterval);
            if (perc > 1.0) perc = 1.0;

            // % of 20
            double drawHeight = 20.0 * perc;

            rctSlideShowIntervalTime.Height = drawHeight;
        }
        #endregion

        // Buttons / Bars
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

        #region Move forwards / back
        private void btnMovePrev_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (SlideShowPlaying) SetNextSlideShowFlipTime(); // Reset slideshow counter

            TryShowPrevPic();
        }
        bool TryShowPrevPic()
        {
            CurrentPictureIndex = CurrentPictureIndex - 1;
            if (!ValidateCurrentIndex()) return false;
            CopyCurrentPictureToOverlay();
            LoadCurrentPicture();
            return true;
        }
        private void btnMoveNext_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (SlideShowPlaying) SetNextSlideShowFlipTime(); // Reset slideshow counter

            TryShowNextPic();
        }

        private bool TryShowNextPic()
        {
            CurrentPictureIndex = CurrentPictureIndex + 1;
            if (!ValidateCurrentIndex()) return false;
            CopyCurrentPictureToOverlay();
            LoadCurrentPicture();
            return true;
        }
        private void btnMoveControl_MouseEnter(object sender, MouseEventArgs e)
        {
            Border brd = (Border)sender;
            Image img = (Image)brd.Child;
            img.Opacity = 1.0;
        }
        private void btnMoveControl_MouseLeave(object sender, MouseEventArgs e)
        {
            Border brd = (Border)sender;
            Image img = (Image)brd.Child;
            img.Opacity = 0.75;
        }
        bool ValidateCurrentIndex()
        {
            if (!(Pictures.Count > CurrentPictureIndex))
            {
                CurrentPictureIndex = (Pictures.Count - 1);
                return false;
            }

            if (CurrentPictureIndex < 0)
            {
                CurrentPictureIndex = 0;
                return false;
            }
            return true;
        }
        #endregion

        #region Zoom / Quality
        // Zoom
        double preFullScreenSliderValue = 0.0;
        void Content_FullScreenChanged(object sender, EventArgs e)
        {
            ResetPicMargin();
            SizePicForFullOrNormalScreenSize();
        }
        void SizePicForFullOrNormalScreenSize()
        {
            if (Application.Current.Host.Content.IsFullScreen)
            {
                preFullScreenSliderValue = sldZoomLevel.Value;

                ShowPicFillingWindow();
            }
            else  // show at previous height, if there is one?
            {
                if ((preFullScreenSliderValue > 0) )
                {
                    sldZoomLevel.Value = preFullScreenSliderValue; ;
                }
                else
                {
                    // Default zoom level
                    ShowPicFillingWindow(); 
                }

                preFullScreenSliderValue = 0;
            }
        }
        /// <summary>
        /// Show the picture so its largest edge is at maximum size
        /// </summary>
        void ShowPicFillingWindow()
        {
            if ((imgCurrentPicture.ActualWidth == 0) || (imgCurrentPicture.ActualHeight == 0)) return;

            double widthRatio = Application.Current.Host.Content.ActualWidth / imgCurrentPicture.ActualWidth;
            double heightRatio = Application.Current.Host.Content.ActualHeight / imgCurrentPicture.ActualHeight;
            if (widthRatio <= heightRatio)
                sldZoomLevel.Value = widthRatio;
            else
                sldZoomLevel.Value = heightRatio;

        }

        void ResizePicWithSlider(double newPicSize)
        {
            double targetWidthRatio = newPicSize / DefaultPictureSize;
            sldZoomLevel.Value = targetWidthRatio;  // this calls sldZoomLevel_Value... which calls ZoomPicToZoomMultiplier...
        }
        private void sldZoomLevel_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sldZoomLevel == null) return;
            if (SlideShowPlaying) SetNextSlideShowFlipTime(); // Reset slideshow counter

            ZoomPicToZoomMultiplier(sldZoomLevel.Value);
        }
        private void ZoomPicToZoomMultiplier(double ZoomMultiplier)
        {
            scaleTransform.ScaleX = ZoomMultiplier;
            scaleTransform.ScaleY = ZoomMultiplier;

        }
        void ResetPicPositionAndSize()
        {
            ResetPicMargin();

            // Position
            ShowPicFillingWindow();

            // Safety
            MouseButtonDown = false;
        }
        private void ResetPicMargin()
        {
            // Margin
            gdCurrentPicture.Margin = new Thickness(0);
        }
        void ShowOverlayPicFillingWindow()
        {
            if ((imgOverlayPicture.ActualWidth == 0) || (imgOverlayPicture.ActualHeight == 0)) return;

            bool longestEdgeViewPortIsWidth = Application.Current.Host.Content.ActualWidth >= Application.Current.Host.Content.ActualHeight;
            bool longestEdgePictureIsWidth = (imgOverlayPicture.ActualWidth >= imgOverlayPicture.ActualHeight);
            bool widthExpansion;  // width expansion or height expansion
            if (longestEdgeViewPortIsWidth)
                widthExpansion = (!(longestEdgePictureIsWidth));
            else
                widthExpansion = longestEdgePictureIsWidth;

            double targetRatio = widthExpansion ? Application.Current.Host.Content.ActualWidth / imgOverlayPicture.ActualWidth :
                                                    Application.Current.Host.Content.ActualHeight / imgOverlayPicture.ActualHeight;

            scaleTransformOverlay.ScaleX = targetRatio;
            scaleTransformOverlay.ScaleY = targetRatio;
        }

        // Quality

        private void cmbPictureQuality_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbPictureQuality == null) return;

            // Reload picture
            CopyCurrentPictureToOverlay();
            LoadCurrentPicture();
        }

        

        #region Mouse Pan and Zoom
        private void LayoutRoot_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            int deltaDirection = (e.Delta < 0) ? -1 : 1;  // reversed  // v0.91.3: re-reversed! up is zoom in
            sldZoomLevel.Value += (sldZoomLevel.SmallChange * deltaDirection);
        }
        bool MouseButtonDown = false;
        Point DragStartMousePoint;
        Thickness DragStartMargin;
        private void gdPictures_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            MouseButtonDown = true;
            DragStartMousePoint = e.GetPosition(null);
            DragStartMargin = gdCurrentPicture.Margin;
        }
        private void gdPictures_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            MouseButtonDown = false;
        }
        private void gdPictures_MouseMove(object sender, MouseEventArgs e)
        {
            if (!MouseButtonDown)
                return;

            // DRAGGING / PANNING
            Point MousePoint = e.GetPosition(null);

            double sensitivity = 1.6;
            double moveMarginUpDown = (MousePoint.Y - DragStartMousePoint.Y) * sensitivity;  // moved Y since dragging started
            double moveMarginLeftRight = (MousePoint.X - DragStartMousePoint.X) * sensitivity;  // moved Y since dragging started

            double newLeftMargin = moveMarginLeftRight;
            double newTopMargin = moveMarginUpDown;

            gdCurrentPicture.Margin = new Thickness(DragStartMargin.Left + newLeftMargin, DragStartMargin.Top + newTopMargin, 0, 0);

            if (SlideShowPlaying) SetNextSlideShowFlipTime(); // Reset slideshow counter
        }
        private void LayoutRoot_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Catch-all
            MouseButtonDown = false;
        }
        #endregion
        #endregion

        #region OSD Fading
        DateTime dtOSDMouseLastMoved;
        bool OSDFaded = false;
        private void LayoutRoot_MouseMove_1(object sender, MouseEventArgs e)
        {
            MouseMoveHappened();
        }
        void CheckForOSDFades()
        {
            if (dtOSDMouseLastMoved == null) return;
            if (OSDFaded) return;

            TimeSpan elapsed = (DateTime.Now - dtOSDMouseLastMoved);
            if (elapsed.TotalSeconds > 5)
            {
                FadeOutOSD();
            }
        }
        void MouseMoveHappened()
        {
            dtOSDMouseLastMoved = DateTime.Now;

            if (OSDFaded)
            {
                FadeInOSD();
            }
        }
        private void FadeInOSD()
        {
            Animations.DoFadeIn(0.3, brdOSDTop);
            Animations.DoFadeIn(0.3, brdOSDbottom);
            LayoutRoot.Cursor = null;
            OSDFaded = false;
        }
        private void FadeOutOSD()
        {
            Animations.DoFadeOut(1.0, brdOSDTop);
            Animations.DoFadeOut(1.0, brdOSDbottom);
            LayoutRoot.Cursor = Cursors.None;
            OSDFaded = true;
            
        }

        #endregion





    }
}
