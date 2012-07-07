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

namespace SilverPotato
{
    public partial class SettingsPage : UserControl
    {
        bool populatingGUI;

        public SettingsPage()
        {
            populatingGUI = true;
            InitializeComponent();
            Loaded += new RoutedEventHandler(SettingsPage_Loaded);
            SettingsImporter.GetSettingsCompleted += new EventHandler<GenericEventArgs<bool>>(SettingsImporter_GetSettingsCompleted);

            ScheduleManager.ChannelsUpdated += new EventHandler(ScheduleManager_ChannelsUpdated);
        }

        bool IsPopulated;
        void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            

            PopulateControls();

            IsPopulated = true;
        }

        #region Data to GUI
        private void PopulateControls()
        {
            populatingGUI = true;

            // Only show EPG_related boxes if there's a media center
            Functions.ShowHideElement(spEPGCheckBoxes, NetworkManager.ServerCapability.HasMediaCenterSupport);

            lblSettingsLastImportedDate.Text = SettingsImporter.LastImportedSettingsDate();
            //cbZipDataDuringTransfers.IsChecked = Settings.ZipDataStreams;
            cbShowTimesInEPG.IsChecked = Settings.ShowTimesInEPG;
            cbShowEpisodeTitlesInEPG.IsChecked = Settings.ShowEpisodeTitlesInEPG;
            cbDebugLogos.IsChecked = Settings.DebugLogos;
            cbEPGSmartScrolling.IsChecked = Settings.EPGSmartScrolling;
            cbEPGDontLoadLazily.IsChecked = Settings.EPGDontLoadLazily;
            cbDebugHTTP.IsChecked = Settings.DebugHTTP;
            cbDebugCache.IsChecked = Settings.DebugCache;
            cbCacheEPGDays.IsChecked = Settings.SilverlightCacheEPGDays;
            cbEPGGetShowDescriptions.IsChecked = Settings.EPGGetShowDescriptions;
            cbDebugAdvanced.IsChecked = Settings.DebugAdvanced;
            cbWarnOnMusicImportDelay.IsChecked = Settings.WarnOnMusicImportDelay;
            cbPlayStartupSound.IsChecked = Settings.PlayStartupSound;
            cbDisableMovingBackground.IsChecked = Settings.DisableMovingBackground;
            cbEnableAlphaFeatures.IsChecked = Settings.EnableAlphaFeatures;
            cmbStreamingType.SelectedIndex = (Settings.EnableHTTPLiveStreaming ? 0 : 1);
            cbAskStreamingTypeEachTime.IsChecked = Settings.AskForStreamingTypeEachTime;
            PopulateSlideshowIntervalSlider();

            lblVersion.Text = "Server version v" + NetworkManager.ServerVersion.ToString(2) + ", Silverlight Skin version " + Functions.VersionString();
            lblServerOSString.Text = "Server is running " + NetworkManager.ServerOSString;

            // Channels
            ShowLastRefreshedChannels();

            populatingGUI = false;
        }
        #endregion

        #region GUI To Data
        private void cbZipDataDuringTransfers_Click(object sender, RoutedEventArgs e)
        {
         //   Settings.ZipDataStreams = cbZipDataDuringTransfers.IsChecked.Value;
        }

        private void cbShowTimesInEPG_Click(object sender, RoutedEventArgs e)
        {
            Settings.ShowTimesInEPG = cbShowTimesInEPG.IsChecked.Value;
        }
        private void cbShowEpisodeTitlesInEPG_Click(object sender, RoutedEventArgs e)
        {
            Settings.ShowEpisodeTitlesInEPG = cbShowEpisodeTitlesInEPG.IsChecked.Value;
        }

        private void cbDebugLogos_Click(object sender, RoutedEventArgs e)
        {
            Settings.DebugLogos = cbDebugLogos.IsChecked.Value;
        }

        private void cbDebugCache_Click(object sender, RoutedEventArgs e)
        {
            Settings.DebugCache = cbDebugCache.IsChecked.Value;
        }

        private void cbEPGSmartScrolling_Click(object sender, RoutedEventArgs e)
        {
            Settings.EPGSmartScrolling = cbEPGSmartScrolling.IsChecked.Value;
        }
        private void cbDebugHTTP_Click(object sender, RoutedEventArgs e)
        {
            Settings.DebugHTTP = cbDebugHTTP.IsChecked.Value;
        }
        private void cbDebugAdvanced_Click(object sender, RoutedEventArgs e)
        {
            Settings.DebugAdvanced = cbDebugAdvanced.IsChecked.Value;
        }

        private void cbEPGDontLoadLazily_Click(object sender, RoutedEventArgs e)
        {
            Settings.EPGDontLoadLazily = cbEPGDontLoadLazily.IsChecked.Value;
        }

        private void cbEPGGetShowDescriptions_Click(object sender, RoutedEventArgs e)
        {
            Settings.EPGGetShowDescriptions = cbEPGGetShowDescriptions.IsChecked.Value;
        }

        private void cbCacheEPGDays_Click(object sender, RoutedEventArgs e)
        {
            Settings.SilverlightCacheEPGDays = cbCacheEPGDays.IsChecked.Value;
        }

        private void cbWarnOnMusicImportDelay_Click(object sender, RoutedEventArgs e)
        {
            Settings.WarnOnMusicImportDelay = cbWarnOnMusicImportDelay.IsChecked.Value;
        }
        private void cdDisableMovingBackground_Click(object sender, RoutedEventArgs e)
        {
            Settings.DisableMovingBackground = cbDisableMovingBackground.IsChecked.Value;
        }

        private void cbEnableAlphaFeatures_Click(object sender, RoutedEventArgs e)
        {
            Settings.EnableAlphaFeatures = cbEnableAlphaFeatures.IsChecked.Value;
        }

        private void cbPlayStartupSound_Click(object sender, RoutedEventArgs e)
        {
            Settings.PlayStartupSound = cbPlayStartupSound.IsChecked.Value;
        }
        

        private void cmbStreamingType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsPopulated) return;

            Settings.EnableHTTPLiveStreaming = (cmbStreamingType.SelectedIndex == 0);
        }

        private void cbAskStreamingTypeEachTime_Click(object sender, RoutedEventArgs e)
        {
            Settings.AskForStreamingTypeEachTime = cbAskStreamingTypeEachTime.IsChecked.Value;
        }
        #endregion

        // Refresh Button and callback
        private void btnRefreshSettings_Click(object sender, RoutedEventArgs e)
        {
            VisualManager.ShowActivityModal();
            SettingsImporter.RefreshSettingsFromServer();
        }
        void SettingsImporter_GetSettingsCompleted(object sender, GenericEventArgs<bool> e)
        {
            VisualManager.HideActivityModal();
            PopulateControls();
        }

        // Show server settings
        private void brnShowServerSettings_Click(object sender, RoutedEventArgs e)
        {
            TextViewer tv = new TextViewer(SettingsImporter.SettingsAsPrettyString);
            VisualManager.PushOntoScreenStack(tv);
        }


        #region Slideshow Interval Slider
        void PopulateSlideshowIntervalSlider()
        {
            sldSlideshowInterval.Value = Settings.SlideShowInterval;
            WriteSlideShowValue(Settings.SlideShowInterval);
        }
        private void sldSlideshowInterval_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (populatingGUI) return;

            int i = SlideshowIntervalFromSlider();

            if (i != lastSlideShowIntValue)
            {
                Settings.SlideShowInterval = i;
                lastSlideShowIntValue = i;
                WriteSlideShowValue(i);
            }
        }
        void WriteSlideShowValue(int i)
        {
            lblSlideShowInterval.Text = i.ToString() + " sec(s)";
        }
        int lastSlideShowIntValue = -1;
        int SlideshowIntervalFromSlider()
        {
            return Convert.ToInt32(sldSlideshowInterval.Value);
        }

        #endregion


        #region EPG / Channels
        void ScheduleManager_ChannelsUpdated(object sender, EventArgs e)
        {
            ShowLastRefreshedChannels();
        }
        void ShowLastRefreshedChannels()
        {
            if (ScheduleManager.GotChannelsFromServer)
                lblChannelsLastImportedDate.Text = ScheduleManager.lastUpdatedChannelsFromServer.ToLongDateString() + " " + ScheduleManager.lastUpdatedChannelsFromServer.ToLongTimeString();
            else
                lblChannelsLastImportedDate.Text = "Unknown";
        }
        private void btnRefreshChannels_Click(object sender, RoutedEventArgs e)
        {
            lblChannelsLastImportedDate.Text = "Fetching...";
            ScheduleManager.GetAllChannels();
        }

        #endregion



    



    }
}
