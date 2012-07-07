using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Resources;

namespace SilverPotato
{
    public partial class MainPage : UserControl
    {

        public MainPage()
        {
            InitializeComponent();

            Loaded += new RoutedEventHandler(MainPage_Loaded);   
        }

        void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            Functions.WriteLineToLogFile("Remote Potato - Silverlight - Starting Up...");

            // Play Startup Sound
            PlayStartupSound();

            // Potato Anim
            DoLogoAnim();
            
            // Hook up root element for display
            VisualManager.Initialise(cvBackground, LayoutRoot, gdMainContent, gdNetActivity);

            Initialize();
        }

        MediaElement meStartSound;
        void PlayStartupSound()
        {
            if (! Settings.PlayStartupSound) return;

            meStartSound = new MediaElement();
            meStartSound.MediaOpened += new RoutedEventHandler(meStartSound_MediaOpened);
            LayoutRoot.Children.Add(meStartSound);

            string fullPath = "Sounds/StartSound.mp3";
            StreamResourceInfo sr = Application.GetResourceStream(new Uri("SilverPotato;component/" + fullPath, UriKind.RelativeOrAbsolute));
            
            meStartSound.AutoPlay = false;
            meStartSound.SetSource(sr.Stream);
            meStartSound.Volume = 0.5;
            meStartSound.Position = TimeSpan.FromSeconds(0);
        }

        void meStartSound_MediaOpened(object sender, RoutedEventArgs e)
        {
            meStartSound.Stop();

            meStartSound.Play();
        }

        #region Logo Animation
        private void DoLogoAnim()
        {
            rpLogo.Opacity = 0.0;
            stMainLogo.ScaleX = 3.5;
            stMainLogo.ScaleY = 3.5;
            Animations.DoAnimation(1.5, stMainLogo, "(ScaleX)", "(ScaleY)", null, 2.5, null, null, 2.5, null, false, DoLogoAnim_2);
            Animations.DoFadeIn(1.3, rpLogo);
        }
        private void DoLogoAnim_2(object o, EventArgs e)
        {
            Animations.DoAnimation(2.5, stMainLogo, "(ScaleX)", "(ScaleY)", null, 1.0, null, null, 1.0, null, false, null);
            Animations.DoFadeOut(2.2, rpLogo, DoLogoAnim_3);
        }
        private void DoLogoAnim_3(object o, EventArgs e)
        {
            gdMainLogo.Visibility = Visibility.Collapsed;
        } 
        #endregion


        #region Initialization
        public void Initialize()
        {

            Functions.WriteLineToLogFile("Initialising image manager.");
            ImageManager.Initialize();

            NetworkManager.ServerAvailabilityCheck_Completed += new EventHandler<ServerReadyEventArgs>(NetworkManager_ServerAvailabilityCheck_Completed);
            NetworkManager.ServerLoginComplete += new EventHandler<GenericEventArgs<bool>>(NetworkManager_ServerLoginComplete);

            VisualManager.LoginPageLoginDone += new EventHandler<LoginPageCompleteEventArgs>(VisualManager_LoginPageLoginDone);
            NetworkManager.CheckServerIsRunning();

            // Prune Cache
            EPGCache.pruneOldEPGCacheFiles();
        }
        void NetworkManager_ServerAvailabilityCheck_Completed(object sender, ServerReadyEventArgs e)
        {
            if (!e.ServerIsFound)
            {
                ErrorManager.DisplayAndLogError("The Remote Potato Server at " + NetworkManager.hostURL + " could not be found; please check that it is running and refresh this web page.");
                return;
            }

            if (e.ServerVersion < Functions.MinimumServerVersionRequired)
            {
                ErrorManager.DisplayAndLogError("The Remote Potato Server at " + NetworkManager.hostURL + " needs upgrading in order to support this skin - it is currently version " + e.ServerVersion.ToString(2) + " and needs to be at least version " + Functions.MinimumServerVersionRequired.ToString() + ".");
                return;
            }
            
            // Update footer server version
            gdFooter.RenderVersionString();

            // Server is found - login?
            if (!e.ServerRequiresPassword)
                ServerBeginInitialDataRetrieval();
            else
                VisualManager.ShowLoginPage();
        }
        void VisualManager_LoginPageLoginDone(object sender, LoginPageCompleteEventArgs e)
        {
            LoginToServer(e.UN, e.PW);
        }
        void LoginToServer(string un, string pw)
        {
            NetworkManager.LoginToServer(un, pw);
        }
        void NetworkManager_ServerLoginComplete(object sender, GenericEventArgs<bool> e)
        {
            if (!e.Value)
            {
                Functions.WriteLineToLogFile("Server login failed.");
                VisualManager.ShowLoginPage();
                return;
            }

            // Success
            ServerBeginInitialDataRetrieval();
        }

        void ServerBeginInitialDataRetrieval()
        {
            Functions.WriteLineToLogFile("Server found.  Getting settings.");
            SettingsImporter.Initialize();
            SettingsImporter.GetSettingsCompleted += new EventHandler<GenericEventArgs<bool>>(SettingsImporter_GetSettingsCompleted);
            SettingsImporter.GetSettings();  // could wire 

        }
        void SettingsImporter_GetSettingsCompleted(object sender, GenericEventArgs<bool> e)
        {
            // The settings will probably be re-imported and we don't want this method to be fired again
            SettingsImporter.GetSettingsCompleted -= new EventHandler<GenericEventArgs<bool>>(SettingsImporter_GetSettingsCompleted);

            if (!e.Value)
            {
                Functions.WriteLineToLogFile("Could not get settings:");
                ErrorManager.DisplayAndLogError("The Remote Potato password may be incorrect; cannot get settings.\r\nTry clearing your browser's cache completely and also re-starting the main server app to update any changes to your stored password.");
                return;
            }

            // Got settings
            Functions.WriteLineToLogFile("Got settings OK.");

            // Show main menu
            VisualManager.ShowMainMenu();

            // Show title
            VisualManager.SetMenuTitle(SettingsImporter.SettingOrEmptyString("MainMenuTitle"));
            // Show EPG item?
            //VisualManager.SetMenuIsShowingEPG(SettingsImporter.SettingIsTrue("EnableEPG"));
            VisualManager.SetMenuIsShowingEPG(true);

            // Carry on
            ServerContinueDataRetrieval();
        }
        void ServerContinueDataRetrieval()
        {
            Functions.WriteLineToLogFile("Initialising schedule manager. ");
            ScheduleManager.Initialize();
        }


        #endregion


        #region Buttons (Nav, Home, View Log)
        private void btnTopNavBack_Click(object sender, RoutedEventArgs e)
        {
            VisualManager.PopOffScreenStackCurrentWindow();
        }
        private void btnTopNavHome_Click(object sender, RoutedEventArgs e)
        {
            // MessageBox.Show(NetworkManager.hostStreamingURL);

            VisualManager.PopOffBackToPrimary();
        }
        private void btnTopNavBack_MouseEnter(object sender, MouseEventArgs e)
        {
            Image img = (Image)btnTopNavBack.Child;
            if (ImageManager.bmpBtnBackOn != null)
            img.Source = ImageManager.bmpBtnBackOn;
        }

        private void btnTopNavHome_MouseLeave(object sender, MouseEventArgs e)
        {
            imgTopNavHome.Opacity = 0.7;
        }

        private void btnTopNavHome_MouseEnter(object sender, MouseEventArgs e)
        {
            imgTopNavHome.Opacity = 1.0;
        }

        private void btnTopNavBack_MouseLeave(object sender, MouseEventArgs e)
        {
            Image img = (Image)btnTopNavBack.Child;

            if (ImageManager.bmpBtnBack != null)
            img.Source = ImageManager.bmpBtnBack;
        }


        private void btnToggleFullScreen_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Functions.ToggleFullScreen();
        }

        private void btnToggleFullScreen_MouseEnter(object sender, MouseEventArgs e)
        {
            imgToggleFullScreen.Opacity = 1.0;
        }

        private void btnToggleFullScreen_MouseLeave(object sender, MouseEventArgs e)
        {
            imgToggleFullScreen.Opacity = 0.7;
        } 
        #endregion




    }
}
