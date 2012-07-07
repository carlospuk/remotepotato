using System;
using System.Collections.Generic;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.ComponentModel;
using System.Threading;
using System.IO;
using CommonEPG;

namespace SilverPotato
{
    public partial class ClickItem : UserControl
    {
        public ClickItemLayouts Layout;
        // Default thumbnail dimensions
        public double ThumbnailWidth = 150;
        public double ThumbnailHeight = 120;
        public double TextOnlyFontSize = 16;
        public double TextRightColumnWidth = 40;
        public bool DisabledRecordDot ;
        bool HasLoadedThumbnail;
        Uri ThumbnailUri;
        public int Index;

        public ClickItem()
        {
            InitializeComponent();
            

            DisabledRecordDot = false;
            ThumbnailUri = null;
            Layout = ClickItemLayouts.ThumbnailWithOverlay;
            brdMain.MouseEnter +=new MouseEventHandler(brdMain_MouseEnter);
            brdMain.MouseLeave  +=new MouseEventHandler(brdMain_MouseLeave);
            brdMain.MouseLeftButtonUp  +=new MouseButtonEventHandler(brdMain_MouseLeftButtonUp);
            imgThumbnail.ImageFailed += new EventHandler<ExceptionRoutedEventArgs>(imgThumbnail_ImageFailed);
            imgThumbnail.ImageOpened += new EventHandler<RoutedEventArgs>(imgThumbnail_ImageOpened);
        }


        public void InitializeWithFormat(ClickItemLayouts layout)
        {
            Layout = layout;
            LayoutFromLayout();
        }
        public void Refresh()
        {
            LayoutFromLayout();
        }
        public void SetTextOnlyFontSize(double newSize)
        {
            TextOnlyFontSize = newSize;
        }
        public void SetTextRightColumnWidth(double newWidth)
        {
            TextRightColumnWidth = newWidth;
        }
        public void SetLayout(ClickItemLayouts layout)
        {
            Layout = layout;
            LayoutFromLayout();  // if changing from thumbnail to text, the thumbnail dimensions will remain stored in case we change back
        }
        private void LayoutFromLayout()
        {
            switch (Layout)
            {
                case ClickItemLayouts.TextOnly:
                    brdThumbnail.Visibility = Visibility.Collapsed;
                    brdMain.HorizontalAlignment = HorizontalAlignment.Left;
                    brdMain.Height = double.NaN;
               //     brdMain.Width = 400;
                    brdMain.Width = double.NaN;
                    
                    gdIconsAndText.Width = double.NaN;  // expand stretch
                    
                    gdIconsAndText.HorizontalAlignment = HorizontalAlignment.Stretch;
                    gdIconsAndText.Background = null;
                    lblText.TextAlignment = TextAlignment.Left;
                    lblText.FontSize = TextOnlyFontSize;
                    lblText.TextWrapping = TextWrapping.NoWrap;

                    // No right hand text column
                    cdTextRight.Width = new GridLength(0);
                    break;


                case ClickItemLayouts.TextWithRightColumn:
                    brdThumbnail.Visibility = Visibility.Collapsed;
                    brdMain.HorizontalAlignment = HorizontalAlignment.Stretch;
                    brdMain.Height = double.NaN;
                    brdMain.Width = double.NaN;

                    gdIconsAndText.Width = double.NaN;  // expand stretch
                    gdIconsAndText.HorizontalAlignment = HorizontalAlignment.Stretch;
                    gdIconsAndText.Background = null;

                    lblText.TextAlignment = TextAlignment.Left;
                    lblText.FontSize = TextOnlyFontSize;
                    lblText.TextWrapping = TextWrapping.NoWrap;

                    // Right hand text column
                    cdTextRight.Width = new GridLength(TextRightColumnWidth);
                    lblTextRight.FontSize = TextOnlyFontSize;
                    break;


                case ClickItemLayouts.ThumbnailWithOverlay:
                    brdMain.HorizontalAlignment = HorizontalAlignment.Center;
                    imgThumbnail.Width = ThumbnailWidth;
                    imgThumbnail.Height = ThumbnailHeight;
                    brdMain.Height = ThumbnailHeight;
                    //brdMain.MaxHeight = ThumbnailHeight;
                    brdMain.Width = ThumbnailWidth;
                  //  brdMain.MaxWidth = ThumbnailWidth;
                    brdThumbnail.Visibility = Visibility.Visible;

                    gdIconsAndText.Background = new SolidColorBrush(Colors.Black);
                    gdIconsAndText.Background.Opacity = 0.2;

                    lblText.TextAlignment = TextAlignment.Center;
                    lblText.FontSize = 11;
                    lblText.TextWrapping = TextWrapping.Wrap;

                    // No right hand text column
                    cdTextRight.Width = new GridLength(0);

                    // Default thumbnail if none already
                    if (imgThumbnail.Source == null)
                        imgThumbnail.Source = ImageManager.bmpThumbnailDefault;

                    // Grow anim
                    Animations.DoAnimation(0.3, stMain, "(ScaleX)", "(ScaleY)", 0.95, 1.05, null, 0.95, 1.05, null, false, GrowAnim_Phase1_Completed);
                    break;

                default:
                    break;
            }

            

            
        }
        private void GrowAnim_Phase1_Completed(object sender, EventArgs e)
        {
            Animations.DoAnimation(0.5, stMain, "(ScaleX)", "(ScaleY)", null, 1.0, null, null , 1.0, null, false, null);
        }



        // GUI Helpers
        public void HandleRecordDotFor(RPRecording rec)
        {
            if (rec == null)
            {
                BlankRecordDot();
                return;
            }

            if (DisabledRecordDot) return;

            if (
                (rec.State == RPRecordingStates.Initializing) ||
                (rec.State == RPRecordingStates.Recorded) ||
                (rec.State == RPRecordingStates.Recording) ||
                (rec.State == RPRecordingStates.Scheduled) 
                )
            {
                ShowRecordDot(rec.IsRecurring());
            }
            else
            {
                BlankRecordDot();
            }
        }
        private void ShowRecordDot(bool isSeries)
        {
            imgRecordDot.Source = isSeries ? ImageManager.bmpRecordDotSeries : ImageManager.bmpRecordDotOneTime;
        }
        public void BlankRecordDot()
        {
            imgRecordDot.Source = null;
        }
        public void DisableRecordDots()
        {
            DisabledRecordDot = true;
            cdIcons.Width = new GridLength(0);
        }
        public void EnableRecordDots()
        {
            DisabledRecordDot = false;
            cdIcons.Width = new GridLength(20);
        }
        public void SetThumbnailTo(BitmapImage bmp)
        {
            SetImageBitmap(bmp);
        }
        public void SetThumbnailTo(Uri thumbUri)
        {
            ThumbnailUri = thumbUri;
            // But don't load it yet.
        }
        public void LoadThumbnail()
        {
            if (ThumbnailUri == null) return;
            if (HasLoadedThumbnail) return;


            // Don't load thumbnails for text only clickitems
            if (Layout != ClickItemLayouts.TextOnly)
            {
                LogoCacheRetriever retriever = new LogoCacheRetriever();
                retriever.GetBitmap_Completed += new EventHandler<GenericEventArgs<MemoryStream>>(GetBitmap_Completed);
                retriever.GetBitmapFromSomewhere(ThumbnailUri);
            }

        }

        
        void GetBitmap_Completed(object sender, GenericEventArgs<MemoryStream> e)
        {
            // Set thumbnail to bitmapimage that was passed back (may be from cache, may be from disk)
            if (e.Value == null) return;

            dDoSomethingWithMemoryStream d = new dDoSomethingWithMemoryStream(SetImageBitmapFromReturnedStream);
            Dispatcher.BeginInvoke(d, e.Value);
        }
        delegate void dDoSomethingWithMemoryStream(MemoryStream ms);
        void SetImageBitmapFromReturnedStream(MemoryStream ms)
        {
            try
            {
                if (ms == null) return;
                if (ms.Length < 1) return;
                if (! ms.CanRead) return;

                BitmapImage bmp = new BitmapImage();
                bmp.SetSource(ms);

                SetImageBitmap(bmp);

                ms.Close();
                ms.Dispose();

            }
            catch (Exception ex)
            {
                Functions.WriteLineToLogFile("Error getting bitmap from memory stream: ");
                Functions.WriteExceptionToLogFile(ex);
            }
        }
        void SetImageBitmap(BitmapImage bmp)
        {
            try
            {
                imgThumbnail.Source = bmp;

                HasLoadedThumbnail = true;
            }
            catch (Exception ex)
            {
                Functions.WriteLineToLogFile("Error setting bitmap: ");
                Functions.WriteExceptionToLogFile(ex);
            }
        }
        


        // Thumbnails
        void imgThumbnail_ImageOpened(object sender, RoutedEventArgs e)
        {
            imgThumbnail.Opacity = 0.0;
            Animations.DoFadeIn(0.5, imgThumbnail);
        }
        bool fellBackToDefaultThumbnail = false;
        void imgThumbnail_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            if (!fellBackToDefaultThumbnail)
            {
                fellBackToDefaultThumbnail = true;
                imgThumbnail.Source = ImageManager.bmpThumbnailDefault;

                imgThumbnail.Opacity = 0.0;
                Animations.DoFadeIn(0.5, imgThumbnail);

            }
        }


        // Event Handlers
        public event EventHandler Clicked;
        Brush replacedBrush;
        private void brdMain_MouseEnter(object sender, MouseEventArgs e)
        {
            replacedBrush = lblText.Foreground;
            lblText.Foreground = new SolidColorBrush(Colors.White);


            if (Layout == ClickItemLayouts.ThumbnailWithOverlay)
            {
                this.SetValue(Canvas.ZIndexProperty, Functions.ZOrderHighest);
                Animations.DoAnimation(0.07, stMain, "(ScaleX)", "(ScaleY)", 1.0, 1.15, null, 1.0, 1.15, null, false, null);
            }
        }
        private void brdMain_MouseLeave(object sender, MouseEventArgs e)
        {
            lblText.Foreground = replacedBrush;

            if (Layout == ClickItemLayouts.ThumbnailWithOverlay)
            {
                Animations.DoAnimation(0.15, stMain, "(ScaleX)", "(ScaleY)", 1.15, 1.0, null, 1.15, 1.0, null, false, null);
            }
        }
        private void brdMain_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (Clicked != null)
                Clicked(this, new EventArgs());
        }


        public enum ClickItemLayouts
        {
            ThumbnailWithOverlay,
            TextOnly,
            TextWithRightColumn
        }


    }
}
