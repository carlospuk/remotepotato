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

namespace SilverPotato
{
    public partial class ChooserStrip : UserControl
    {
        protected ClickItemsPane contentPane;
        protected MusicActionButtonsPane ActionButtonsPane;
        protected Dictionary<string, List<ClickItem>> CurrentGroupedItems;
        const int CONTRACTED_STRIP_WIDTH = 48;

        public ChooserStrip()
        {
            InitializeComponent();


            // MUST define events in code (as usercontrol is inherited)
            gdExpandStrip.MouseLeftButtonUp +=new MouseButtonEventHandler(gdExpandStrip_MouseLeftButtonUp);
            gdExpandStrip.MouseEnter += new MouseEventHandler(gdExpandStrip_MouseEnter);
            gdExpandStrip.MouseLeave += new MouseEventHandler(gdExpandStrip_MouseLeave);
//            LayoutRoot.MouseLeftButtonUp +=new MouseButtonEventHandler(gdExpandStrip_MouseLeftButtonUp); INTERFERES
            imgHeaderImage.ImageFailed += new EventHandler<ExceptionRoutedEventArgs>(imgHeaderImage_ImageFailed);
            imgHeaderImage.ImageOpened += new EventHandler<RoutedEventArgs>(imgHeaderImage_ImageOpened);
            StripState = StripStates.Expanded;
            CurrentGroupedItems = new Dictionary<string, List<ClickItem>>();
        }


        protected void SetWidthTo(double NewWidth)
        {
            LayoutRoot.Width = NewWidth;
        }

        #region Action Buttons
        protected void ShowHideActionButtons(bool show)
        {
            if (show)
            {
                ActionButtonsPane = new MusicActionButtonsPane();
                rdActionButtons.Height = new GridLength(90);
                gdActionButtonsParent.Children.Add(ActionButtonsPane);
            }
            else
            {
                rdActionButtons.Height = new GridLength(0);
            }
        }

        #endregion

        #region Header Image
        protected void ShowHideHeaderImage(bool showHeaderImage)
        {
            rdHeaderImage.Height = (showHeaderImage) ? new GridLength(82): new GridLength(0);
        }
        protected void SetHeaderImageTo(Uri newHeaderImageUri)
        {
            imgHeaderImage.Source = new BitmapImage(newHeaderImageUri);
        }
        protected void SetHeaderImageToBitmap(BitmapImage bmp)
        {
            imgHeaderImage.Source = bmp;
        }
        void imgHeaderImage_ImageOpened(object sender, RoutedEventArgs e)
        {
            // nothing to do
        }
        void imgHeaderImage_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            imgHeaderImage.Source = ImageManager.bmpThumbnailDefault;
        }
        #endregion

        protected void InitialiseContentPane(ClickItemsPane.ClickItemsPaneItemLayouts PaneItemLayout)
        {
            if (contentPane != null)
                contentPane = null;

            contentPane = new ClickItemsPane(null, ClickItemsPane.ClickItemsPaneLayouts.PaneAndToolbar, PaneItemLayout);
            contentPane.HideRefreshControl();  // up to inherited classes to decide whether to hide more
            gdContentPaneParent.Children.Add(contentPane);
        }

        
        protected void RefreshContentPane()
        {
            contentPane.ReplaceItemsWithNewItems(CurrentGroupedItems);
        }

        #region Expand / Contract
        public event EventHandler StripBeginExpanding;
        enum StripStates
        { Expanded, Contracted }
        StripStates StripState;
        double ExpandedWidth;

        private void gdExpandStrip_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ExpandStrip();
        }

        public void ContractStrip()
        {
            if (StripState == StripStates.Contracted) return;

            StripState = StripStates.Contracted;
            ExpandedWidth = LayoutRoot.ActualWidth;
            gdExpandStrip.Visibility = Visibility.Visible;
            Animations.DoFadeIn(0.8, gdExpandStrip); // Fade in 'Expand' button
            Animations.DoFadeOut(0.15, gdContentPaneParent); // Fade out main content (quickly!)
            Animations.DoChangeWidthTo(0.25, LayoutRoot, CONTRACTED_STRIP_WIDTH, ContractStrip_Completed);
        }
        void ContractStrip_Completed(object sender, EventArgs e)
        {
            gsToggleFadeOut.Color = Functions.HexColor("#00000000");

            // Disable content
            gdContentPaneParent.Visibility = Visibility.Collapsed;
            gdActionButtonsParent.Visibility = Visibility.Collapsed;

            // entire area can be used to expand
            brdMain.Cursor = Cursors.Hand;
            brdMain.MouseLeftButtonUp += new MouseButtonEventHandler(brdMain_MouseLeftButtonUp);
        }

        void brdMain_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ExpandStrip();
        }
        public void ExpandStrip()
        {
            if (StripState == StripStates.Expanded) return;

            StripState = StripStates.Expanded;

            // Re-enable content
            gdContentPaneParent.Visibility = Visibility.Visible;
            gdActionButtonsParent.Visibility = Visibility.Visible;

            if (StripBeginExpanding != null) StripBeginExpanding(this, new EventArgs());
            Animations.DoFadeOut(0.25, gdExpandStrip); // Fade out 'Expand' button
            Animations.DoFadeIn(0.15, gdContentPaneParent); // Fade in main content - quickly!
            Animations.DoChangeWidthTo(0.25, LayoutRoot, ExpandedWidth, ExpandStrip_Completed);
        }
        void ExpandStrip_Completed(object sender, EventArgs e)
        {
            gsToggleFadeOut.Color = Functions.HexColor("#FF000000");
            gdExpandStrip.Visibility = Visibility.Collapsed;

            // Disable using entire area to expand
            brdMain.Cursor = Cursors.Arrow;
            brdMain.MouseLeftButtonUp -= new MouseButtonEventHandler(brdMain_MouseLeftButtonUp);
        }


        void gdExpandStrip_MouseLeave(object sender, MouseEventArgs e)
        {
            imgExpandStrip.Source = ImageManager.LoadImageFromContentPath("/Images/btnExpandCircle.png");
        }
        void gdExpandStrip_MouseEnter(object sender, MouseEventArgs e)
        {
            imgExpandStrip.Source = ImageManager.LoadImageFromContentPath("/Images/btnExpandCircle_MouseOver.png");
        }
        #endregion





    }
}
