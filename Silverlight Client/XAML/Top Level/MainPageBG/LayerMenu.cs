using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace SilverPotato
{
    public class LayerMenu : LayerBase
    {
        public LayerMenu(double movementAmount)
        {
            InitializeComponent();

            MovementAmount = movementAmount;

            CreateMenu();
        }

        public void SetMenuTitle(string newTitle)
        {
            if (lblMainMenuTitle == null) return;

            if (lblMainMenuTitle.Text != newTitle)
            {
                lblMainMenuTitle.Opacity = 0;
                lblMainMenuTitle.Text = newTitle;
                Animations.DoFadeIn(1.6, lblMainMenuTitle);
            }
        }

        TextBlock lblMainMenuTitle;
        bool shouldShowEPGMenuItem = true;
        void CreateMenu()
        {

            StackPanel spMenu = new StackPanel();
            spMenu.Orientation = Orientation.Vertical;
            spMenu.HorizontalAlignment = HorizontalAlignment.Center;
            spMenu.VerticalAlignment = VerticalAlignment.Center;

            lblMainMenuTitle = menuTextblock("Remote Potato", "TITLE", false, Colors.White);
            lblMainMenuTitle.Opacity = 0.0;
            lblMainMenuTitle.FontSize = 36;
            lblMainMenuTitle.Margin = new Thickness(0, 0, 0, 20);
            spMenu.Children.Add(lblMainMenuTitle);

            if (NetworkManager.ServerCapability.HasMediaCenterSupport)
                AddNewMenuItem(ref spMenu, "TV Guide", "EPG");
            if (NetworkManager.ServerCapability.HasMediaCenterSupport)
                AddNewMenuItem(ref spMenu, "Search Guide", "SEARCH");
            
            if (NetworkManager.ServerCapability.HasRecordedTVLibrary)
            AddNewMenuItem(ref spMenu, "Recorded TV", "RECTV");
            if (NetworkManager.ServerCapability.HasMediaCenterSupport)
                AddNewMenuItem(ref spMenu, "Scheduled Recordings", "SCHEDULED");
            if (NetworkManager.ServerCapability.HasMediaCenterSupport)
                AddNewMenuItem(ref spMenu, "Manage Series", "MANAGESERIES");
            if (NetworkManager.ServerCapability.HasMediaCenterSupport)
                AddNewMenuItem(ref spMenu, "Movie Guide", "MOVIEGUIDE");
            if (NetworkManager.ServerCapability.HasMusicLibrary)
                AddNewMenuItem(ref spMenu, "Music", "MUSIC");
            if (NetworkManager.ServerCapability.HasPictureLibrary)
                AddNewMenuItem(ref spMenu, "Pictures", "PICTURES");
            if (Settings.EnableAlphaFeatures)
            {
                if (NetworkManager.ServerCapability.HasMediaCenterSupport)
                    AddNewMenuItem(ref spMenu, "Movies", "MOVIES");
                if (NetworkManager.ServerCapability.HasVideoLibrary)
                    AddNewMenuItem(ref spMenu, "Videos", "VIDEOS");
                if (NetworkManager.ServerCapability.HasMediaCenterSupport)
                    AddNewMenuItem(ref spMenu, "Remote Control", "REMOTECONTROL");
            }
            AddNewMenuItem(ref spMenu, "Settings", "SETTINGS");
            AddNewMenuItem(ref spMenu, "Exit", "EXIT");
            
            gdMain.Children.Add(spMenu);
        }
        Border brdTVGuideMenuItem;
        Border brdSearchMenuItem;
        void AddNewMenuItem(ref StackPanel sp, string txtMenuTitle, string tag)
        {
            Border b = menuItem(txtMenuTitle, tag);
            sp.Children.Add(b);

            if (tag == "EPG")
            {
                brdTVGuideMenuItem = b;  // store
                brdTVGuideMenuItem.Visibility = shouldShowEPGMenuItem ? Visibility.Visible : Visibility.Collapsed;
            }

            if (tag == "SEARCH")
            {
                brdSearchMenuItem = b;
                brdSearchMenuItem.Visibility = shouldShowEPGMenuItem ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public void ShowHideTVGuideMenuItem(bool showEPGItem)
        {
            // set flag
            shouldShowEPGMenuItem = showEPGItem;

            // if it's already created, hide/show it)
            if (brdTVGuideMenuItem != null)
            {
                brdTVGuideMenuItem.Visibility = showEPGItem ? Visibility.Visible : Visibility.Collapsed;
            }
            if (brdSearchMenuItem!= null)
            {
                brdSearchMenuItem.Visibility = showEPGItem ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        Border menuItem(string txtName, string tag)
        {
            Border b = new Border();

            TextBlock tb = menuTextblock(txtName, tag, true, Colors.White);
            b.Cursor = Cursors.Hand;
            b.Tag = tag;
            b.Child = tb;

            b.MouseEnter += new MouseEventHandler(menuItem_MouseEnter);
            b.MouseLeave += new MouseEventHandler(menuItem_MouseLeave);
            b.MouseLeftButtonUp += new MouseButtonEventHandler(menuItem_MouseLeftButtonUp);

            return b;
        }
        private TextBlock menuTextblock(string txtTitle, string txtTag, bool useGradient, Color textColor)
        {
            TextBlock tb = new TextBlock();
            tb.FontSize = 28;
            tb.Text = txtTitle;
            tb.TextAlignment = TextAlignment.Center;


            if (useGradient)
            {
                LinearGradientBrush lgb = new LinearGradientBrush();
                GradientStop gs = new GradientStop();
                gs.Offset = 0;
                gs.Color = Functions.HexColor("#8888AA");
                lgb.GradientStops.Add(gs);
                GradientStop gs2 = new GradientStop();
                gs2.Offset = 1;
                gs2.Color = Functions.HexColor("#DDDDDD");
                lgb.GradientStops.Add(gs2);
                tb.Foreground = lgb;
            }
            else
                tb.Foreground = new SolidColorBrush(textColor);


            tb.Tag = txtTag;

            return tb;
        }


        Brush savedBrush = null;
        void menuItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Check quote
            FileManager.IncreaseStorageAsNecessary();  // Must be done on a button click!

            Border b = (Border)sender;

            string txtTag = (string)b.Tag;
            switch (txtTag)
            {
                case "EPG":
                    VisualManager.ShowEPG();
                    break;

                case "SETTINGS":
                    VisualManager.ShowSettingsPage();
                    break;

                case "SCHEDULED":
                    VisualManager.ShowScheduledRecordings();
                    break;

                case "RECTV":
                    VisualManager.ShowRecordedTV();
                    break;

                case "MANAGESERIES":
                    VisualManager.ShowManageSeries();
                    break;

                case "PICTURES":
                    VisualManager.ShowPictures();
                    break;

                case "VIDEOS":
                    VisualManager.ShowVideos();
                    break;

                case "MOVIES":
                    VisualManager.ShowMovies();
                    break;

                case "MUSIC":
                    VisualManager.ShowMusic();
                    break;

                case "MOVIEGUIDE":
                    VisualManager.ShowMovieGuide();
                    break;

                case "SEARCH":
                    VisualManager.ShowSearchPane();
                    break;

                case "REMOTECONTROL":
                    VisualManager.ShowRemoteControl();
                    break;


                case "EXIT":
                    System.Windows.Browser.HtmlPage.Window.Navigate(new Uri("/mainmenu", UriKind.Relative));
                    break;

                default:
                    break;
            }
        }
        void menuItem_MouseLeave(object sender, MouseEventArgs e)
        {
            Border b = (Border)sender;
            TextBlock tb = (TextBlock)b.Child;

            if (savedBrush != null)
                tb.Foreground = savedBrush;
        }
        void menuItem_MouseEnter(object sender, MouseEventArgs e)
        {
            Border b = (Border)sender;
            TextBlock tb = (TextBlock)b.Child;

            savedBrush = tb.Foreground;
            tb.Foreground = new SolidColorBrush(Colors.White);
        }

    }
}
