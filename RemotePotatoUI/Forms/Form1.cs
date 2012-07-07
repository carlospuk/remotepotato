using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.IO;
using System.Net;
using System.Windows.Forms;
using System.Security.Cryptography;
using RemotePotatoServer.Properties;
using Microsoft.Win32;
using System.Deployment.Application;
using CommonEPG;
//using FatAttitude;  // music helper
// temp
using System.Xml.Serialization;
using System.Xml;
using System.Data;
using System.Linq;

namespace RemotePotatoServer
{
    public partial class Form1 : Form
    {
        // Private Members
        ThreadController webserverTC = null;
        private bool initialising = false;
        private frmPleaseWait fPleaseWait = null;

        #region Form Constructor / Startup
        public Form1()
        {
            initialising = true;

            InitializeComponent();

        }

        
        
        private void Form1_Load(object sender, EventArgs e)
        {           
            // Info
            Functions.WriteLineToLogFile("Remote Potato Settings UI v" + UIFunctions.VersionText + " - starting up.");

            // CREATE ANY OBJECTS
            // WIRE UP EVENTS
            Form1InitEvents(); // All incoming events

            // PREPARE SETTINGS VALUES
            Form1InitSettings(); // Set any generated Settings values

            // Bind controls
            BindControls();  // Bind Controls (was in form constructor)

            // GET FORM HEIGHT CORRECT
            showOptionsIfAppropriate();  // Set form height (to help with design)

            // Show correct interface
            ShowInterfaceForEnabledModules();

#if DEBUG
            ToggleServer();
#endif
        }
        void ShowInterfaceForEnabledModules()
        {
            ShowHideTabPage(tpChannels, Settings.Default.EnableMediaCenterSupport);
            ShowHideTabPage(tpEPG, Settings.Default.EnableMediaCenterSupport);

            // Expert Tab Display
            AddRemoveExpertTabsAsNecessary();
        }
        private void Form1InitSettings()
        {
            // Upgrade?
            if (Settings.Default.SettingsUpgradeRequired)
            {
                Settings.Default.Upgrade();
                Settings.Default.SettingsUpgradeRequired = false;
            }

            // Always save settings immediately
            Settings.Default.PropertyChanged += new PropertyChangedEventHandler(Default_PropertyChanged);

            // SETTINGS - DEFAULTS
            if (!Settings.Default.HaveDoneVanillaSetup)
                VanillaSetup();

            // User Agents
            if (Settings.Default.RecentUserAgents == null) Settings.Default.RecentUserAgents = new System.Collections.Specialized.StringCollection();
            if (Settings.Default.MobileUserAgents == null) Settings.Default.MobileUserAgents = new System.Collections.Specialized.StringCollection();

            // Get external IP in background
            Network.IPHelper ipHelper = new Network.IPHelper();  // EVENT NOT REQUIRED ipHelper.QueryExternalIPAsync_Completed += new EventHandler<Network.IPHelper.GetExternalIPEventArgs>(ipHelper_QueryExternalIPAsync_Completed);
            ipHelper.QueryExternalIPAsync();
        }
        void VanillaSetup()
        {
            // Win7 onwards allows media center
            Settings.Default.EnableMediaCenterSupport = (UIFunctions.OSSupportsMediaCenterFunctionality);

            SetDefaultRecordingPath();

            SetDefaultLibraryFoldersPaths();

            // Menu title
            if ((Environment.UserName != "") && (!(Environment.UserName.ToUpperInvariant().Equals("SYSTEM"))))
            {
                string strSystemType = (Settings.Default.EnableMediaCenterSupport ? "Media Center" : "PC");

                string proposedTitle = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Environment.UserName) + "'s " + strSystemType;
                if (proposedTitle.Length > 30) proposedTitle = proposedTitle.Substring(0, 30);
                Settings.Default.MainMenuTitle = proposedTitle;
            }

            // Never do again
            Settings.Default.HaveDoneVanillaSetup = true;
        }
        private static void SetDefaultRecordingPath()
        {
            string recPath = Functions.GetRecordPath();
            if (!String.IsNullOrEmpty(recPath))
            {
                System.Collections.Specialized.StringCollection sc = new System.Collections.Specialized.StringCollection();
                sc.Add(recPath);
                Settings.Default.RecordedTVFolders = sc;
            }
            else
            {
                System.Collections.Specialized.StringCollection sc = new System.Collections.Specialized.StringCollection();
                sc.Add(@"C:\");
                Settings.Default.RecordedTVFolders = sc;
            }
        }
        private static void SetDefaultLibraryFoldersPaths()
        {
            List<string> prototypePictureList = new List<string>();
            prototypePictureList.Add(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures));
            prototypePictureList.Add(Environment.GetFolderPath(Environment.SpecialFolder.CommonPictures));
            Settings.Default.PictureLibraryFolders = clearNullEntriesFromList(prototypePictureList);
            if (Settings.Default.PictureLibraryFolders.Count < 1) Settings.Default.PictureLibraryFolders.Add(@"C:\");

            List<string> prototypeVideoList = new List<string>();
            prototypeVideoList.Add(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos));
            prototypeVideoList.Add(Environment.GetFolderPath(Environment.SpecialFolder.CommonVideos));
            Settings.Default.VideoLibraryFolders = clearNullEntriesFromList(prototypeVideoList);
            if (Settings.Default.VideoLibraryFolders.Count < 1) Settings.Default.VideoLibraryFolders.Add(@"C:\");
        }
        private static System.Collections.Specialized.StringCollection clearNullEntriesFromList(List<String> strList)
        {
            System.Collections.Specialized.StringCollection outputList = new System.Collections.Specialized.StringCollection();
            foreach (string str in strList)
            {
                if (!string.IsNullOrWhiteSpace(str))
                    outputList.Add(str);
            }

            return outputList;
        }
        private void Form1InitEvents()
        {
            EPGManager.EPGChannelsRepopulated += new EventHandler(EPGManager_EPGChannelsRepopulated);

            ServiceManager.ServiceStatusChanged += new EventHandler(ServiceManager_ServiceStatusChanged);
        }
        private void Form1_Shown_1(object sender, EventArgs e)
        {
            // Version text and tech preview caption?
            string versionString = "v" + Functions.VersionText;

            this.Text += " " + versionString;
            // And the about box
            lblAboutVersion.Text = versionString;

            // Let's get the screen up quick
            Application.DoEvents();
            ShowPWForm("Please Wait - Initialising", 30);
            Application.DoEvents();

            // POPULATE FORM CONTROLS (BEFORE EVENTS ENABLED) ---------
            PopulateFormControls();

            // First-time startup for IR Manager
            if (Settings.Default.EnableMediaCenterSupport)
            {
                if (!Settings.Default.HaveEverSetRPKeySenderToStartWithWindows)
                {
                    cbStartRPKeySenderWithWindows.Checked = true;  // do before setting 'initialising' to false;
                    if (!RegRunHelper.SetRPKeySenderStartup(true))
                        RPMessageBox.ShowAlert("Could not set Remote Potato IR Helper to load on startup - file was not found.\r\n\r\nCheck that you have correctly installed Remote Potato.");

                    // ...and run the EXE
                    string appPath = System.IO.Path.Combine(UIFunctions.AppInstallFolder, "RPKeySender.exe");
                    if (File.Exists(appPath))
                        System.Diagnostics.Process.Start(appPath);

                    Settings.Default.HaveEverSetRPKeySenderToStartWithWindows = true;  // Never do this again.
                }
                else
                {
                    // Not the first run... check IR Helper is running (if it should be)
                    WarnIfIRHelperSettoRunButNotRunning();
                }
            }

            // ENABLE CONTROL EVENTS **************************************************
            initialising = false; // We're done
            // ************************************************************************

            // No longer initialize server due to shared assembly issues (ie who 'owns' the singleton - end up with 2 instances of things like EPGManager, so a double init of MCData)
            UpdatePWFormProgress(80);

            // Shown 'about' box yet?
            ShowAboutBoxOnFirstTime();

            // Streaming Pack check
            WarnIfStreamingPackNotInstalled();  // just updates GUI, doesn't display a message box

            // No account - warning prompt (but not the VERY first time)
            if (Settings.Default.EverShownSetupWizard)
                ShowPromptIfNoMediaLibraryAccount();

            // Server Status
            DisplayServerStatusInGUI();

            // Close the 'Please Wait' dialog
            ClosePWForm();

            // Nag?
            NagIfTimeToNag();

            // Shows the updated HTML page if the major/minor version numbers have updated
            ShowUpdateInfoIfAppropriate();

            // Check for updates
            CheckForAppUpdatesIfTimeElapsed();

            

            // Setup wizard?
            ShowSetupWizardIfNeverShown();
        }

        private void PopulateFormControls()
        {
            // Advanced options
            cmbRecommendedMovieMinimumRating.SelectedIndex = Settings.Default.RecommendedMovieMinimumRating;

            //Streaming options
            cmbSilverlightStreamingQuality.SelectedIndex = Settings.Default.SilverlightStreamingQuality;

            // Default options
            PopulateRecordingOptionsFromSettings();

            // Channels - populate; use cache if available
            EPGManager.ExternalPopulateTVChannels(false);

            // Tech Preview only
            // Button to send favorites to Media Center
           // btnSendFavoritesToMediaCenter.Visible = Settings.Default.IsTechPreview;
            btnSendFavoritesToMediaCenter.Visible = false;

            // Recent Browsers
            bindRecentUserAgentListbox();

            // Mobile User Agents
            populateMobileUserAgents();
            UpdatePWFormProgress(40);

            // Start with windows?
            UpdateStartWithWindowsCheckbox();

        }

        private void ShowAboutBoxOnFirstTime()
        {
            if (!Settings.Default.ForcedAboutTab)
            {
                tabControl1.SelectedTab = tpAbout;
                Settings.Default.ForcedAboutTab = true;
            }
        }
        private void ShowUpdateInfoIfAppropriate()
        {

            Version lastVersionShown = new Version(Settings.Default.LastUpdateInfoShownForMajVersion, Settings.Default.LastUpdateInfoShownForMinVersion);
            Version thisVersionMajMinOnly = new Version(Functions.ServerVersion.Major, Functions.ServerVersion.Minor);

            if (thisVersionMajMinOnly > lastVersionShown)
            {
                string target;
                if (Settings.Default.IsTechPreview )
                    target = "http://www.remotepotato.com/changelog.aspx?techpreview=yes#v" + thisVersionMajMinOnly.Major.ToString() + "." + thisVersionMajMinOnly.Minor.ToString();
                else
                    target = "http://www.remotepotato.com/changelog.aspx?techpreview=no#v" + thisVersionMajMinOnly.Major.ToString() + "." + thisVersionMajMinOnly.Minor.ToString();
                System.Diagnostics.Process.Start(target);

                Settings.Default.LastUpdateInfoShownForMajVersion = thisVersionMajMinOnly.Major;
                Settings.Default.LastUpdateInfoShownForMinVersion = thisVersionMajMinOnly.Minor;
            }
       
        }
        private void WarnIfStreamingPackNotInstalled()
        {
            if (!Functions.IsStreamingPackInstalled)
                lblWarnStreamPack.Visible = true;
            else
            {
                if (Functions.StreamingPackBuild < Settings.Default.StreamPackMinimumBuildRequired)
                {
                    lblWarnStreamPack.Visible = true;
                    lblWarnStreamPack.Text = "The version of Remote Potato streaming pack currently installed needs upgrading to v" + Settings.Default.StreamPackMinimumBuildRequired.ToString() + " to support the latest features.  Please visit www.remotepotato.com to install a newer version of the streaming pack.";
                }
                else
                    lblWarnStreamPack.Visible = false;
            }


            
        }
        bool WarnIfLegacyAppRunning()
        {
                if (Functions.isProcessRunning("RemotePotato"))
                {
                    RPMessageBox.ShowAlert("An older version of Remote Potato is still installed and currently running.\r\nPlease stop and/or uninstall the older version to continue.");
                    return true;
                }
            return false;
        }
        #endregion
        
        // Settings: Property changed (call methods)
        void Default_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Settings.Default.Save();  // Always save settings immediately, in case App is force quit on shutdown

            /*
            // Settings have changed; can call appropriate methods here if required
            switch (e.PropertyName)
            {

                default:
                    break;
            }
            */
            
        }

        #region Status display / spool
        delegate void ChangeStatusCallBack(string newStatus);
        private void changeStatus(string newStatus)
        {
            ChangeStatusCallBack d = new ChangeStatusCallBack(unsafeChangeStatus);
            this.Invoke(d, new object[] { newStatus });
        }
        void unsafeChangeStatus(string newStatus)
        {
            lblStatus.Text = newStatus;
        }
        private void changeStatusBottom(string newStatus)
        {
            lblStatusBottom.Text = newStatus;
        }
        private void btnWipeLogSpool_Click(object sender, EventArgs e)
        {
            ShowDebugLog();
        }
        // These delegates enable asynchronous calls for background tasks
        delegate void changeStatusBottomCallBack(string text);
        delegate void setBottomProgressBarValueCallBack(int value);
        #endregion

        #region Minimise to System Tray
        private void notifyIcon1_MouseDoubleClick_1(object sender, MouseEventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
        }

        // Menus 
        private void mnuRestore_Click(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
        }
        private void mnuExit_Click(object sender, EventArgs e)
        {
            ExitProgram();
        }
        private void ExitProgram()
        {
            // Transfer fave channels into settings string
            ChanMgrPromptIfDirty();

            // Save settings
            Settings.Default.Save();

            System.Environment.Exit(0);
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;  // Handle App exit manually

            if (ServerIsRunning)
            if (ServerRunningType == ServerRunningTypes.ServiceRunning)
            {

                if (!Settings.Default.ShownServiceRunsInBackgroundWarning)
                {
                    Settings.Default.ShownServiceRunsInBackgroundWarning = true;
                    RPMessageBox.Show("Remote Potato Server will continue to run in the background, regardless of whether you are logged into this computer.\r\n\r\nYou can stop the service using this settings application or the Windows Services dialog.\r\n\r\nIf you wish to prevent the Remote Potato Server from starting automatically with Windows, you should un-check the box marked 'Start Remote Potato server with Windows'.");
                }

            
            }
            else if (ServerRunningType == ServerRunningTypes.ApplicationRunning)
            {
                // Stop server
                webserverTC.Stop();
            }

            ExitProgram();

        }
        #endregion

        #region General Settings / Controls / Debug Tab
        // Tab Control tab changing, e.g. to save channels
        TabPage currentTabPage = null;
        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
       {
           if (currentTabPage == tpChannels)
           {
               ChanMgrPromptIfDirty();
           }

           currentTabPage = tabControl1.SelectedTab;
       }
        // Form Control events
        private void ShowDebugLog()
        {
           System.Diagnostics.Process.Start("explorer.exe", "/select, " + Functions.DebugLogFileFN);
        }
        // Expert Display
        private void showOptionsIfAppropriate()
        {
            bool showingExpert = Settings.Default.ShowExpert; // get once

            btnShowHideOptions.Image = (showingExpert) ?
                 (Image)RemotePotatoServer.Properties.Resources.imgGearGreen :
                 (Image)RemotePotatoServer.Properties.Resources.imgGearBlack;

           if (showingExpert)
           {
               pnlOptions.Visible = true;
               this.Height = 455;
           }
           else
           {
               pnlOptions.Visible = false;
               this.Height = 76;
           }
        }
        private void btnShowHideOptions_Click(object sender, EventArgs e)
        {
            bool sShowExpert = Settings.Default.ShowExpert;
            sShowExpert = !sShowExpert;
            Settings.Default.ShowExpert = sShowExpert;
            
            showOptionsIfAppropriate();
        }

        DialogResult ShowChangeMediaLibraryAccountDialog()
        {
            FormMediaLibraryAccountSetter fSetAccount = new FormMediaLibraryAccountSetter();
            DialogResult rst = fSetAccount.ShowDialog();
            return rst;
        }
        void ShowPromptIfNoMediaLibraryAccount()
        {
            if (!MediaLibraryAccountHasBeenSetUp)
            {
                if (RPMessageBox.ShowQuestionWithTimeout("You must enter details of a local user whose music and picture library Remote Potato will access.\r\n\r\nDo you wish to do this now?", "Music and Picture Library access", 30000) == DialogResult.Yes)
                {
                    DialogResult foo = ShowChangeMediaLibraryAccountDialog();
                }
            }
        }
        bool MediaLibraryAccountHasBeenSetUp
        {
            get
            {
                return (! (ServiceManager.RPServiceAccountName == "LocalSystem"));
            }
        }
        // Start with Windows
        void UpdateStartWithWindowsCheckbox()
        {
            cbStartWithWindows.Checked = ServiceManager.RPServiceStartsAutomatically();
            cbStartRPKeySenderWithWindows.Checked = RegRunHelper.IsRPKeySenderSetToRunOnStartup;
        }
        private void cbStartWithWindows_CheckedChanged_1(object sender, EventArgs e)
        {
            if (initialising) return;

            string ErrorText = string.Empty;
            if (! ServiceManager.SetRPServiceStartupType((cbStartWithWindows.Checked), ref ErrorText) )
            {
                RPMessageBox.ShowAlert("Could not change service startup type: " + ErrorText);
                UpdateStartWithWindowsCheckbox();  // (un)tick checkbox
            }
            // Success - do nothing
        }
        private void cbStartRPKeySenderWithWindows_CheckedChanged(object sender, EventArgs e)
        {
            if (initialising) return;

            bool shouldInstall = cbStartRPKeySenderWithWindows.Checked;
            bool result = RegRunHelper.SetRPKeySenderStartup(shouldInstall);

            if (!result)
            {
                RPMessageBox.ShowAlert("Could not set Remote Potato IR Helper to load on startup - file was not found.\r\n\r\nCheck that you have correctly installed Remote Potato.");
                initialising = true;
                cbStartRPKeySenderWithWindows.Checked = false;
                initialising = false;
            }

            WarnIfIRHelperSettoRunButNotRunning();
        }
        void WarnIfIRHelperSettoRunButNotRunning()
        {
            if (RegRunHelper.IsRPKeySenderSetToRunOnStartup) 
            {
                bool isIRRunning = false;
                try
                {
                    System.Threading.Mutex.OpenExisting("Global\\RPKeySender");
                    isIRRunning = true;
                }
                catch (System.Threading.WaitHandleCannotBeOpenedException)
                {
                    isIRRunning = false;   
                }
                catch (Exception ex)
                {
                    Functions.WriteLineToLogFile("Error checking if RP IR Helper app is running:");
                    Functions.WriteExceptionToLogFile(ex);
                    
                    isIRRunning = false;
                }

                if (!isIRRunning)
                {
                    if (RPMessageBox.ShowQuestion("The Remote Potato IR Helper has been set to run when you start Windows, but is not currently running.\r\n\r\nTo use your Remote Potato client as an 'infra red' remote control, the helper needs to be running.\r\n\r\nWould you like to run the helper in the background now?", "Run IR Helper Now") == System.Windows.Forms.DialogResult.Yes)
                    {
                        TryRunIRHelperAndReportResult();
                    }
                }

            }
        }
        private void btnHelpIRHelper_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string txtHelper = "Remote Potato can allow you to use your laptop, mobile phone or other client as a second 'infra red' remote control to control Windows Media Center over the network.\r\n\r\nIn order to use this feature, the Remote Potato 'IR Helper' app must be running on this machine.";

            if (IRCommunicator.Default.IsRemoteHelperRunning)
            {
                txtHelper += "\r\n\r\nThe IR Helper app is currently running on this machine - you can double click its icon in the system tray for more details, or to stop the app.";

                RPMessageBox.Show(txtHelper);
            }
            else
            {
                txtHelper += "\r\n\r\nThe IR Helper app is NOT currently running - would you like to start it now?";

                if (RPMessageBox.ShowQuestion(txtHelper, "Run IR Helper App?") == System.Windows.Forms.DialogResult.Yes)
                    TryRunIRHelperAndReportResult();
            }


            //RPMessageBox.Show("
        }
        void TryRunIRHelperAndReportResult()
        {
            try
            {
                System.Diagnostics.Process.Start(RegRunHelper.RPKeySenderAppPath);
                RPMessageBox.Show("The Remote Potato IR helper is now running in the background - you can double-click its icon in the system tray to access its settings window.");
            }
            catch (Exception ex)
            {
                RPMessageBox.ShowAlert("The Remote Potato IR helper could not be run - check that you have fully installed Remote Potato :  " + ex.Message);
                Functions.WriteExceptionToLogFile(ex);
            }
        }

        private void btnHelpOnSilverlight_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string strSilverlightHelp = "When you browse to your Remote Potato server using a web browser, you can choose whether the default home page is a normal (HTML) page or an enhanced (Silverlight) page.\r\n\r\n" +
                "Silverlight is a new technology from Microsoft that allows an enhanced user experience; as long as it's installed on the PC where your web browser is, you'll be able to browse your media more easily and experience a truly integrated UI, even within a web browser.";

            RPMessageBox.Show(strSilverlightHelp, "About Silverlight");
        }
        private void btnShowThemesForm_Click(object sender, EventArgs e)
        {
            using (FormThemesChooser frmThemes = new FormThemesChooser())
            {
                frmThemes.ShowDialog();
            }
        }

        // Default record options
        #region Recording Options
        private void PopulateRecordingOptionsFromSettings()
        {
            cmbDefaultKeepUntil.SelectedIndex = Settings.Default.DefaultKeepUntil;
            EnableDisableKeepuntilBox();
            cmbDefaultQuality.SelectedIndex = Settings.Default.DefaultQuality;
            nudDefaultKeepUntilDays.Value = Convert.ToDecimal(Settings.Default.DefaultKeepNumberOfEpisodes);
        }

        private void cmbDefaultQuality_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (initialising) return;
            if (cmbDefaultQuality == null) return;
            if (cmbDefaultQuality.SelectedIndex < 0) return;
            Settings.Default.DefaultQuality = cmbDefaultQuality.SelectedIndex;
        }
        private void cmbDefaultKeepUntil_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (initialising) return;
            if (cmbDefaultKeepUntil == null) return;
            if (cmbDefaultKeepUntil.SelectedIndex < 0) return;
            Settings.Default.DefaultKeepUntil = cmbDefaultKeepUntil.SelectedIndex;

            EnableDisableKeepuntilBox();
        }
        void EnableDisableKeepuntilBox()
        {
            nudDefaultKeepUntilDays.Enabled = (Settings.Default.DefaultKeepUntil == 5);
        }
        private void nudDefaultKeepUntilDays_ValueChanged(object sender, EventArgs e)
        {
            Settings.Default.DefaultKeepNumberOfEpisodes = Convert.ToInt32(nudDefaultKeepUntilDays.Value);
        }


        //bool repopulatingRecordingOptions = false;
        private void btnResetRecordingOptions_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {

        }
        void PopulateRecordingSettingsFromMediaCenter()
        {
            // This will automatically change the controls



        }
        string RecordSettings_()
        {
            // Start with windows?
            try
            {
                RegistryKey rkRecPath = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Media Center\\Service\\Recording", false);
                return (string)rkRecPath.GetValue("RecordPath", "C:\\");
            }
            catch { }
            return "";
        }
        #endregion
        private void cbShowExpertSettings_MouseClick(object sender, MouseEventArgs e)
        {
            AddRemoveExpertTabsAsNecessary();
        }
        void AddRemoveExpertTabsAsNecessary()
        {
            bool showExpertTabs = Settings.Default.ShowExpertTabsInUI; // get once
            ShowHideTabPage(tpMobile, showExpertTabs);
            ShowHideTabPage(tpAdvanced, showExpertTabs);
            ShowHideTabPage(tpAdvanced2, (showExpertTabs & Settings.Default.EnableMediaCenterSupport));
            ShowHideTabPage(tpDebug, showExpertTabs);

            // Re-Add About page at the end
            ShowHideTabPage(tpAbout, false);
            ShowHideTabPage(tpAbout, true);
        }
        void ShowHideTabPage(TabPage tpPage, bool showPage)
        {
            if (showPage)
            {
                if (!tabControl1.TabPages.Contains(tpPage))
                    tabControl1.TabPages.Add(tpPage);
            }
            else
            {
                if (tabControl1.TabPages.Contains(tpPage))
                    tabControl1.TabPages.Remove(tpPage);
            }
        }
        #endregion

        #region Nags

        int NagEveryDays = 7;
        void NagIfTimeToNag()
        {
            // Don't ever nag if user visited the donate page
            if (Settings.Default.HaveVisitedDonatePage)
                return;

            // If we've never nagged, set the flag and don't nag yet
            if (! Settings.Default.HaveEverNagged)
            {
                Settings.Default.DateLastNagged = DateTime.Now;
                Settings.Default.HaveEverNagged = true;
                return;
            }

            TimeSpan timeSinceNagged = DateTime.Now.Subtract(Settings.Default.DateLastNagged);
            bool shouldNag = (timeSinceNagged.TotalDays > NagEveryDays);

            if (shouldNag)
                ShowNag();
        }
        void ShowNag()
        {
            Settings.Default.DateLastNagged = DateTime.Now;

            FormDonationRequest frmDonate = new FormDonationRequest();
            switch (frmDonate.ShowDialog())
            {
                case System.Windows.Forms.DialogResult.Yes:
                    Settings.Default.HaveVisitedDonatePage = true;
                    break;
            }

        }
        #endregion

        #region Web Server
        // Server on / off
        
        private void btnToggleServer_Click(object sender, EventArgs e)
        {
            btnToggleServer.Enabled = false;
            ToggleServer();
        }
        private void ToggleServer()
        {
            // Initial yellow display of LED - we're trying something
            pbLEDyellow.Visible = true;
            pbLEDred.Visible = false;
            pbLEDgreen.Visible = false;

#if DEBUG
            ToggleServer(false); // try to run as app by default 
#else
            ToggleServer(true); // try to run as service by default 
#endif
        }
        private void ToggleServer(bool runAsService)
        {
            if (ServerIsRunning)
                StopWebServer();
            else
            {
                // Save settings
                Settings.Default.Save();

                StartWebServer(runAsService);
            }
        }
        void StartWebServer(bool startAsService)
        {
            if (ServerIsRunning)
            {
                RPMessageBox.ShowAlert("Cannot start server as it is already running.");
                return; // already running
            }

            // Can we start?
            if (!ValidateSecuritySettings())
            {
                btnToggleServer.Enabled = true;
                return;  // failed validation
            }

            if (WarnIfLegacyAppRunning())
            {
                btnToggleServer.Enabled = true;
                return;  // failed - legacy app is running
            }

            // Offer windows firewall?
            OfferToAddFirewallRulesIfNotAlreadyOffered();

            // No URL reserved - ask first...  (then try to start server again)
            if (
                (Settings.Default.LastSetSecurityForPort != Settings.Default.Port) )
            {
                ReserveURLForPort(true);
                return;
            }

            // Dispose any local process
            if (webserverTC != null)
            {
                webserverTC.IsRunningChanged -= new EventHandler(serverTC_IsRunningChanged);
                webserverTC = null;
            }

            // RUN!
            if ((startAsService) && (ServerRunningType == ServerRunningTypes.ServiceStopped)) // Service must be installed!
            {
                if (!ServiceManager.StartRemotePotatoService()) // sync, returns false if didn't work  (event will fire when status changes to update display etc.)
                {
                    btnToggleServer.Enabled = true;
                    RPMessageBox.ShowAlert("Could not start the Remote Potato service.\r\nCheck that you have entered the correct account name and password in the 'Music Library' tab.\r\n\r\nAlternatively, please try re-installing Remote Potato to rectify this issue.");
                }
            }
            else
            {
                webserverTC = new ThreadController(); // Create controller for webserver
                webserverTC.IsRunningChanged += new EventHandler(serverTC_IsRunningChanged);

                webserverTC.Start();
            }
        }
        void StopWebServer()
        {
            if (!ServerIsRunning) return;

            if (ServerRunningType == ServerRunningTypes.ApplicationRunning)
            {
                webserverTC.Stop();
                return;
            }
                
            if (ServerRunningType == ServerRunningTypes.ServiceRunning)
            {
                if (! ServiceManager.StopRemotePotatoService())
                    RPMessageBox.ShowAlert("Could not stop service.");
            }
        }
        private void btnShowConnectionInfo_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            FormConnectionInformation fConnectInfo = new FormConnectionInformation( (!ServerIsRunning) );
            fConnectInfo.ShowDialog();
        }

        
        
        
        bool ValidateSecuritySettings()
        {
            if (Settings.Default.RequirePassword)
            {
                if (string.IsNullOrEmpty(Settings.Default.UserName) || string.IsNullOrEmpty(Settings.Default.UserPassword))
                {
                    RPMessageBox.ShowAlert("If using security, both a username and password should be specified.");
                    return false;
                }
            }
            return true;
        }
        void ReserveURLForPort(bool startServerAfterSuccessfulReservation)
        {
            URLReserver reserver = new URLReserver();
            int ResultCode = reserver.ReserveUrl(Convert.ToInt32(Settings.Default.Port), "/", true);
            if (ResultCode == 0)
            {
                Functions.WriteLineToLogFile("URLReserver: 0 OK");
                Settings.Default.LastSetSecurityForPort = Convert.ToInt32(Settings.Default.Port);
                if (startServerAfterSuccessfulReservation)
                    ToggleServer();  // try to start webserver again
            }
            else
            {
                Functions.WriteLineToLogFile("URLReserver: NOT OK");
                RPMessageBox.ShowAlert("Could not reserve a Url for Remote Potato server - error code " + ResultCode.ToString());
            }

        }
        private void btnReserveServerURLNow_Click(object sender, EventArgs e)
        {
            ReserveURLForPort(false);
        }
        private void btnStartRPAsApp_Click(object sender, EventArgs e)
        {
            StartWebServer(false);
        }


        // Incoming 
        void serverTC_IsRunningChanged(object sender, EventArgs e)  // App running state (owned by this form) changed
        {
            DisplayServerStatusInGUICallBack d = new DisplayServerStatusInGUICallBack(DisplayServerStatusInGUI);
            this.Invoke(d, new object[] {  });

            if (!webserverTC.IsRunning)
            {
                // why did the server stop running - any reason of note?
                switch (webserverTC.ServerStoppedReason)
                {
                    case ThreadController.ServerStoppedReasons.AccessDenied:
                        ReserveURLForPort(false);
                        break;

                    default:
                        break;
                }
            }

        }   
        void ServiceManager_ServiceStatusChanged(object sender, EventArgs e)   // Service running state changed
        {
            SafeDisplayServerStatusInGUI();
        }
        delegate void DisplayServerStatusInGUICallBack();
        void SafeDisplayServerStatusInGUI()
        {
            DisplayServerStatusInGUICallBack d = new DisplayServerStatusInGUICallBack(DisplayServerStatusInGUI);
            this.Invoke(d);
        }
        void DisplayServerStatusInGUI()
        {
            btnToggleServer.Enabled = true; // allow toggling of server again

            bool serverIsRunning = ServerIsRunning;
            ServerRunningTypes svRunType = ServerRunningType;

            btnShowConnectionInfo.Visible = serverIsRunning;

            if (!serverIsRunning)
            {
                changeStatus("Server is stopped");
            }
            else
            {
                string serverRunText;
                if (svRunType == ServerRunningTypes.ApplicationRunning)
                    serverRunText = "Server is running (as application)";
                else
                    serverRunText = "Server is running";

                //string strConnInfo = UIFunctions.ConnectionInfoString;
                //if (! string.IsNullOrWhiteSpace(strConnInfo))
                //    serverRunText += "\r\n" + UIFunctions.ConnectionInfoString;

                changeStatus(serverRunText);
            }

            if (serverIsRunning)
            {
                tabControl1.Enabled = false;
                lblStatusBottom.Text = "To change the settings, you must first stop the server.";
            }
            else
            {
                tabControl1.Enabled = true;
                lblStatusBottom.Text = string.Empty;
            }

            pbLEDgreen.Visible = serverIsRunning;
            pbLEDred.Visible = !serverIsRunning;
            pbLEDyellow.Visible = false;

            btnToggleServer.Image = serverIsRunning ?
                (Image)RemotePotatoServer.Properties.Resources.btnStopServerSmall :
                (Image)RemotePotatoServer.Properties.Resources.BtnStartServerSmall;
        }
        enum ServerRunningTypes
        {
            ApplicationRunning,
            ApplicationStopped,
            ServiceRunning,
            ServiceStopped,
        }
        bool ServerIsRunning
        {
            get
            {
                return ((ServerRunningType == ServerRunningTypes.ApplicationRunning) || (ServerRunningType == ServerRunningTypes.ServiceRunning));
            }
        }
        /// <summary>
        /// Determine whether the server is running as a service, or an application, and whether it is Started or Stopped
        /// </summary>
        ServerRunningTypes ServerRunningType
        {
            get
            {
                if (webserverTC == null)
                {
                    RPServiceStatusTypes svcStatus = ServiceManager.RemotePotatoServiceStatus;
                    if (svcStatus == RPServiceStatusTypes.Running)
                        return ServerRunningTypes.ServiceRunning;
                    else if (svcStatus == RPServiceStatusTypes.NotInstalled)
                        return ServerRunningTypes.ApplicationStopped;
                    else
                        return ServerRunningTypes.ServiceStopped;
                }

                return webserverTC.IsRunning ?
                    ServerRunningTypes.ApplicationRunning :
                    ServerRunningTypes.ApplicationStopped;
            }
        }
        #endregion

        #region File Cache

        private void btnFlushCache_Click(object sender, EventArgs e)
        {
           FileCache.FlushCache(true, true);
           RPMessageBox.Show("The file cache has been emptied of all files.", "Flush Cache");
        }

       private void cbCacheTextFiles_CheckedChanged(object sender, EventArgs e)
       {
           if (!cbCacheTextFiles.Checked)
           {
               FileCache.FlushCache(false, true);
           }
       }

       private void cbCacheBinaryFiles_CheckedChanged(object sender, EventArgs e)
       {
           if (!cbCacheBinaryFiles.Checked)
           {
               FileCache.FlushCache(true, false);
           }
       }

        
       #endregion


        #region Mobile User Agents
        void ws_UserAgentConnected(object sender, MessageEventArgs e)
        {
            addUserAgentToRecentList(e.Message);
        }
        private void addUserAgentToRecentList(string userAgent)
        {
            if (!Settings.Default.RecentUserAgents.Contains(userAgent))
                Settings.Default.RecentUserAgents.Add(userAgent);
        }
        private void bindRecentUserAgentListbox()
        {
            lbRecentBrowsers.DataSource = Settings.Default.RecentUserAgents;
        }
        private void btnRefreshMobileBrowserList_Click(object sender, EventArgs e)
        {
            lbRecentBrowsers.Refresh();
        }
        private void btnAddRecentBrowserToList_Click(object sender, EventArgs e)
        {
            if (lbRecentBrowsers.SelectedIndex < 0) return;
            if (lbRecentBrowsers.Items.Count < 1) return;

            string recentUA = (string)lbRecentBrowsers.SelectedItem;
            recentUA = recentUA.Trim();
            Settings.Default.MobileUserAgents.Add(recentUA);
            populateMobileUserAgents();
        }
        private void populateMobileUserAgents()
        {
            StringBuilder agents = new StringBuilder();
            foreach (String agent in Settings.Default.MobileUserAgents)
            {
                agents.AppendLine(agent);
            }
            
            txtMobileUserAgents.Text = agents.ToString().Trim();
        }
        private void btnRevertMobileUserAgents_Click_1(object sender, EventArgs e)
        {
            populateMobileUserAgents();
        }
        private void btnSaveMobileUserAgents_Click_1(object sender, EventArgs e)
        {
            List<string> agents = txtMobileUserAgents.Lines.ToList();
            Settings.Default.MobileUserAgents.Clear();
            foreach (String agent in agents)
            {
                if (!string.IsNullOrEmpty(agent.Trim()))
                    Settings.Default.MobileUserAgents.Add(agent);  // no blank lines
            }

            Settings.Default.Save();

            RPMessageBox.Show("Mobile user agents list saved.");
        } 
        #endregion

        #region Navigation Linklabels
        private void pbBuyBeer_Click(object sender, EventArgs e)
        {
            GoToDonateWebPage();
        }
        void GoToDonateWebPage()
        {
            string target = "http://www.remotepotato.com/donate.aspx";
            System.Diagnostics.Process.Start(target);
        }
        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            GoToDonateWebPage();
        }
        
        private void btnAboutSupport_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string target = "http://remotepotatoforums.fatattitude.com";
            System.Diagnostics.Process.Start(target);
        }


        private void btnShowThemes_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                string dir = Functions.AppDataFolder;
                string logPath = Path.Combine(dir, "static/skins/");
                System.Diagnostics.Process.Start(@logPath);
            }
            catch {
                RPMessageBox.ShowAlert("Sorry, but the folder could not be found.");
            }
        }
        private void pbBrowseToMCL_Click(object sender, EventArgs e)
        {
            string target = "http://mychannellogos.com";
            System.Diagnostics.Process.Start(target);
        }
       

        private void btnShowAppFolder_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string dir = Path.GetDirectoryName(Application.ExecutablePath);
            System.Diagnostics.Process.Start(@dir);
        }

        

        #endregion

        #region Please Wait form
        private void ShowPWForm(string txtActivity)
        {
            ShowPWForm(txtActivity, 0);
        }
        private void ShowPWForm(string txtActivity, int initialProgress)
        {
            if (fPleaseWait == null)
            {
                fPleaseWait = new frmPleaseWait(txtActivity);
                fPleaseWait.StartPosition = FormStartPosition.CenterParent;
                fPleaseWait.Show(this);
                fPleaseWait.setProgress(initialProgress);
            }
            else
            {
                UpdatePWForm(txtActivity);
            }
        }
        private void UpdatePWForm(string txtActivity, int newProgress)
        {
            if (fPleaseWait == null) return;

            if (txtActivity != null)
                fPleaseWait.setActivity(txtActivity);

            if (newProgress >= 0)
                fPleaseWait.setProgress(newProgress);
        }
        private void UpdatePWForm(string txtActivity)
        {
            UpdatePWForm(txtActivity, -1);
        }
        private void UpdatePWFormProgress(int newProgress)
        {
            UpdatePWForm(null, newProgress);
        }
        private void ClosePWForm()
        {
            if (fPleaseWait != null)
            {
                fPleaseWait.CloseMe();
                fPleaseWait = null;
            }
        } 
        #endregion
 
        #region Deployment / Updates / Backups
        private void CheckForAppUpdatesIfTimeElapsed()
        {

            if (!Settings.Default.CheckForAppUpdates) return;

            if (Settings.Default.LastCheckedForAppUpdates == null)
            {
                Functions.WriteLineToLogFile("Skipping App update check - never been checked before.");
                Settings.Default.LastCheckedForAppUpdates = DateTime.Now;
                Settings.Default.Save();
            }
            else
            {
                
                TimeSpan elapsed = (DateTime.Now - Settings.Default.LastCheckedForAppUpdates);
                int daysElapsed = Convert.ToInt32(elapsed.TotalDays);
                if (daysElapsed > Convert.ToInt32(Settings.Default.DaysBetweenAppUpdateCheck))
                {
                    Functions.WriteLineToLogFile(daysElapsed.ToString() + " days elapsed since last check - Checking for App Updates Now...");
                    Settings.Default.LastCheckedForAppUpdates = DateTime.Now;
                    Settings.Default.Save();
                    CheckForAppUpdates(true);
                }
                else
                {
                    Functions.WriteLineToLogFile("Skipping App update check - only " + daysElapsed.ToString() + " days since last check.");
                }
            }
            
            
        }
        private void btnCheckVersion_Click(object sender, EventArgs e)
        {
            CheckForAppUpdates(false);
        }
        string GetDeploymentProgressString(DeploymentProgressState state)
        {
            if (state == DeploymentProgressState.DownloadingApplicationFiles)
            {
                return "application files";
            }
            else if (state == DeploymentProgressState.DownloadingApplicationInformation)
            {
                return "application manifest";
            }
            else
            {
                return "deployment manifest";
            }
        }
        // NEW
        private static void CheckForAppUpdates(bool silent)
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                if (!silent)
                    RPMessageBox.Show("Cannot check for updates right now - no Internet connection was found.\r\n\r\nPlease connect to the Internet and try again.");
                return;
            }

            // Check for an update
            RemotePotatoServer.UI.UpdateChecker checker = new RemotePotatoServer.UI.UpdateChecker();
            Version newestVersion = new Version();
            string newestVersionDescription = string.Empty;

            bool isUpdate = checker.IsUpdateAvailable(UIFunctions.ProductCode, Functions.ServerVersion, ref newestVersion, ref newestVersionDescription );
            Functions.WriteLineToLogFile("UpdateCheck: Current version is " + Functions.ServerVersion.ToString() + ", available version is " + newestVersion.ToString());
            if (isUpdate)
            {
                if (RPMessageBox.ShowQuestionWithTimeout("A newer version of Remote Potato is available.\r\n\r\nDetails: \"" + newestVersionDescription + "\"\r\n\r\nDo you want to download to install?\r\n\r\nThis message will disappear after 20 seconds.", "Update available", 20000) == DialogResult.Yes)
                {
                    RPMessageBox.Show("Please follow the links in the web page that is about to open to download the newest version of Remote Potato.\r\n\r\nIMPORTANT: Remember to uninstall this version before installing the newest version!");
                    System.Diagnostics.Process.Start("http://www.remotepotato.com/downloads.aspx");
                    // Update
                    //ApplicationDeployment.CurrentDeployment.UpdateAsync();
                }
            }
            else
            {
                if (!silent)
                    RPMessageBox.Show("This version of Remote Potato is up-to-date.", "Update Check");
            }
        }
        #endregion

        #region Advanced Tab Options
        private void AddRemoveRecTVFolders()
        {
            using (FormFoldersCollection frmFolders = new FormFoldersCollection(Settings.Default.RecordedTVFolders, true, Settings.Default.RecurseRecTVSubfolders, true, false, false, "Add the folders where you keep .wtv or .dvr-ms files:", "Recorded TV Folders", "recorded TV"))
            {

                DialogResult dr = frmFolders.ShowDialog();

                if (dr == System.Windows.Forms.DialogResult.OK)
                {
                    Settings.Default.RecordedTVFolders = frmFolders.Folders;
                    Settings.Default.RecurseRecTVSubfolders = frmFolders.IsRecurseCheckboxChecked;
                }
            }
        }

        // Show cache
        private void btnShowCache_Click(object sender, EventArgs e)
        {
            FileCache.WriteCacheInfoToLog();
            ShowDebugLog();

        }
        private void cmbRecommendedMovieMinimumRating_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (initialising) return;
            Settings.Default.RecommendedMovieMinimumRating = cmbRecommendedMovieMinimumRating.SelectedIndex;
        }
        private void btnDeleteDLLCache_Click(object sender, EventArgs e)
        {
// TODO: delete
        }

        #endregion

        #region Silverlight
        private void cmbSilverlightStreamingQuality_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (initialising) return;
            
            Settings.Default.SilverlightStreamingQuality = cmbSilverlightStreamingQuality.SelectedIndex;
        }

        private void btnHelpForDeinterlace_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string txtHelp = "Most recorded television is 'Interlaced', which means that when played on a computer monitor, it can suffer from ugly-looking horizontal lines, particularly when there is lots of movement.\r\n\r\nChecking this box will attempt to 'deinterlace' the video to remove these lines automatically when you stream recorded TV.\r\n\r\nOn most clients, the setting is merely a default option and can be overriden at the time of streaming.\r\n\r\nTo read more about deinterlacing, visit www.100fps.com";
            RPMessageBox.Show(txtHelp, "Help on Deinterlacing", MessageBoxButtons.OK);
        }

        #endregion

        // Channel Manager
        #region Channel Manager
        List<TVService> LocalChannelCache;
        bool PopulatingChannelManagerListView = false;

        delegate void populateChannelBoxCallBack();
        void EPGManager_EPGChannelsRepopulated(object sender, EventArgs e)
        {
            // Update the Gui
            TryPopulateCheckListBoxWithAllChannels();
        }
        private void TryPopulateCheckListBoxWithAllChannels()
        {
            if (lvChannelManager.InvokeRequired)  // cross thread check
            {
                populateChannelBoxCallBack d = new populateChannelBoxCallBack(PopulateChannelManagerListView);

                this.Invoke(d, new object[] { });
            }
            else
                PopulateChannelManagerListView();
        }
        private void PopulateChannelManagerListView()
        {
            if (EPGManager.AllTVChannels.Count < 1)
                return;

            if (LocalChannelCache != null)
            {
                LocalChannelCache.Clear();
                LocalChannelCache = null;
            }

            // COPY channel (refs) from EPG Manager
            LocalChannelCache = new List<TVService>();
            foreach (TVService tvc in EPGManager.AllTVChannels.Values)
            {
                LocalChannelCache.Add(tvc);
            }

            if (! Settings.Default.DoneVeryFirstChannelListSort)
            {
                SortLocalChannelsByNumber();
                Settings.Default.DoneVeryFirstChannelListSort = true;
            }

            FillListViewFromLocalChannelCache();
        }
        void FillListViewFromLocalChannelCache()
        {
            PopulatingChannelManagerListView = true;

            lvChannelManager.Items.Clear();
            int i = 0;
            ListViewItem lvi;
            foreach (TVService tvc in LocalChannelCache)
            {
                lvi = lviForChannelWithUserIndex(i++);
                if (lvi != null)
                    lvChannelManager.Items.Add(lvi);
            }

            PopulatingChannelManagerListView = false;
            lvChannelManager.Items[0].Selected = true;
            SetLocalChannelCacheDirty(false);
        }
        void RefreshChannelManagerListViewSelectedItem()
        {
            if (lvChannelManager.SelectedIndices.Count < 1) return;

            movingAnItem = true;  // flag to avoid selected item re-populated channel info pane
            int SelectedIndex = lvChannelManager.SelectedIndices[0];
            lvChannelManager.Items.Remove(lvChannelManager.SelectedItems[0]);
            ListViewItem newLvi = lviForChannelWithUserIndex(SelectedIndex);
            lvChannelManager.Items.Insert(SelectedIndex, newLvi);
            newLvi.Selected = true;

            movingAnItem = false;
        }
        private ListViewItem lviForChannelWithUserIndex(int userIndex)
        {
            if (userIndex > (LocalChannelCache.Count - 1))
            {
                return null;
            }

            ListViewItem lviItem= new ListViewItem();

            TVService tvc = LocalChannelCache[userIndex];

            // Favourite?
            lviItem.Checked = (tvc.IsFavorite);

            ListViewItem.ListViewSubItem lvsiChanNum = new ListViewItem.ListViewSubItem();
            lvsiChanNum.Text = tvc.ChannelNumberString();
            
            ListViewItem.ListViewSubItem lvsiCallsign = new ListViewItem.ListViewSubItem();
            lvsiCallsign.Text = tvc.Callsign;

            string foo;
            ListViewItem.ListViewSubItem lvsiHasLogo = new ListViewItem.ListViewSubItem();
            lvsiHasLogo.Text = EPGManager.LogoForServiceExists(tvc.UniqueId, out foo) ? "Yes" : "";

            // Add subitems
            lviItem.SubItems.Add(lvsiChanNum);
            lviItem.SubItems.Add(lvsiCallsign);
            lviItem.SubItems.Add(lvsiHasLogo);

            lviItem.UseItemStyleForSubItems = true;

            // Assign item
           return lviItem;
        }
        private void lvChannelManager_ItemCheck(object sender, ItemCheckEventArgs e)
        {

            if (PopulatingChannelManagerListView == true) return;
            // Toggle the TVChannel object#
            if (! (LocalChannelCache.Count > e.Index)) return;
            TVService editChannel = LocalChannelCache[e.Index];

            if (editChannel.IsFavorite != (e.NewValue == CheckState.Checked))
            {
                editChannel.IsFavorite = (e.NewValue == CheckState.Checked);
                SetLocalChannelCacheDirty(true);
            }
        }

        bool movingAnItem = false;
        private void lvChannelManager_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (movingAnItem) return;

            populateSelectedChannelInfo();
        

        }
        private void lvChannelManager_MouseUp(object sender, MouseEventArgs e)
        {
            btnChanMgrSaveChanges.Focus();
        }


        // Re-order channels
        private void btnChanMgrMoveChannelDown_Click(object sender, EventArgs e)
        {
            TVService tvc = null;
            ListViewItem lvi = null;
            int SelectedIndex = 0;
            if (!GetLVIAndTVChannelForSelectedIndex(ref lvi, ref tvc, ref SelectedIndex))
                return;

            int newIndex = (SelectedIndex + 1);

            attemptMoveLviToNewIndex(lvi, newIndex);
            attemptMoveCachedChannelToNewIndex(tvc, newIndex);
        }
        private void btnChanMgrMoveChannelUp_Click(object sender, EventArgs e)
        {


            TVService tvc = null;
            ListViewItem lvi = null;
            int SelectedIndex = 0;
            if (!GetLVIAndTVChannelForSelectedIndex(ref lvi, ref tvc, ref SelectedIndex))
                return;

            int newIndex = (SelectedIndex - 1);

            attemptMoveLviToNewIndex(lvi, newIndex);
            attemptMoveCachedChannelToNewIndex(tvc, newIndex);
        }
        private void btnChanMgrMoveChannelToTop_Click(object sender, EventArgs e)
        {
            TVService tvc = null;
            ListViewItem lvi = null;
            int SelectedIndex = 0;
            if (!GetLVIAndTVChannelForSelectedIndex(ref lvi, ref tvc, ref SelectedIndex))
                return;

            int newIndex = 0;

            attemptMoveLviToNewIndex(lvi, newIndex);
            attemptMoveCachedChannelToNewIndex(tvc, newIndex);
        }
        private void btnChanMgrMoveChannelToBottom_Click(object sender, EventArgs e)
        {
            TVService tvc = null;
            ListViewItem lvi = null;
            int SelectedIndex = 0;
            if (!GetLVIAndTVChannelForSelectedIndex(ref lvi, ref tvc, ref SelectedIndex))
                return;

            int newIndex = lvChannelManager.Items.Count ;

            attemptMoveLviToNewIndex(lvi, newIndex);
            attemptMoveCachedChannelToNewIndex(tvc, newIndex);
        }

        private bool GetLVIAndTVChannelForSelectedIndex(ref ListViewItem lvi, ref TVService tvc, ref int SelectedIndex)
        {
            if (lvChannelManager.SelectedIndices.Count < 1) return false;
            SelectedIndex = lvChannelManager.SelectedIndices[0];

            try
            {
                lvi = lvChannelManager.Items[SelectedIndex];
                tvc = LocalChannelCache[SelectedIndex];
                return true;
            }
            catch
            {
                return false;
            }
        }
        private void attemptMoveLviToNewIndex(ListViewItem lvi, int newIndex)
        {
            movingAnItem = true;
            lvChannelManager.Items.Remove(lvi);

            if (newIndex < 0) newIndex = 0;
            if (newIndex > lvChannelManager.Items.Count) newIndex = lvChannelManager.Items.Count;

            lvChannelManager.Items.Insert(newIndex, lvi);
            
            
            lvi.Selected = true;
            lvi.Focused = true;
            movingAnItem = false;
            lvChannelManager.EnsureVisible(newIndex);
        }
        private void attemptMoveCachedChannelToNewIndex(TVService tvc, int newIndex)
        {
            LocalChannelCache.Remove(tvc);

            if (newIndex < 0) newIndex = 0;
            if (newIndex > LocalChannelCache.Count) newIndex = LocalChannelCache.Count;

            LocalChannelCache.Insert(newIndex, tvc);
            SetLocalChannelCacheDirty(true);
        }

        // Column re-order
        private void lvChannelManager_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            switch (e.Column)
            {
                case 1: // Number
                    SortLocalChannelsByNumber();
                    FillListViewFromLocalChannelCache();
                    SetLocalChannelCacheDirty(true);
                    break;

                case 2: // callsign
                    SortLocalChannelsByCallsign();
                    FillListViewFromLocalChannelCache();
                    SetLocalChannelCacheDirty(true);
                    break;

                default:
                    break;
            }
        }

        // Sorting
        public void SortLocalChannelsByNumber()
        {
            CommonEPG.Comparers.TVCServiceNumComparer numComparer = new CommonEPG.Comparers.TVCServiceNumComparer();
            LocalChannelCache.Sort(numComparer);    
        }
        public void SortLocalChannelsByCallsign()
        {
            CommonEPG.Comparers.TVCServiceCallsignComparer callsignComparer = new CommonEPG.Comparers.TVCServiceCallsignComparer();
            LocalChannelCache.Sort(callsignComparer);
        }

        // Channel Info Pane
        bool PopulatingSelectedChannelInfo = false;
        private void populateSelectedChannelInfo()
        {
            if (lvChannelManager.SelectedIndices.Count < 1) return;
            int SelectedIndex = lvChannelManager.SelectedIndices[0];
            TVService editChannel = TVChannelForSelectedItem();
            if (editChannel == null) return;

            PopulatingSelectedChannelInfo = true;
            ListViewItem.ListViewSubItem lvsiHasLogo = new ListViewItem.ListViewSubItem();
            // Mapping Info
            currentlySelectedChannelMappingInfo = "Channel " + editChannel.Callsign + " has its logo returned from the internal EPG.";

            // Logo
            string logoFN;
            if (EPGManager.LogoForServiceExists(editChannel.UniqueId, out logoFN))
            {
                currentlySelectedChannelMappingInfo += "\r\nThe logo file returned is " + logoFN;

                if (File.Exists(logoFN))
                    pbChannelLogo.Image = Image.FromFile(logoFN);
                else
                {
                    pbChannelLogo.Image = RemotePotatoServer.Properties.Resources.logo_notfound;
                    currentlySelectedChannelMappingInfo += " (NOT FOUND)";
                }
            }
            else
            {
                pbChannelLogo.Image = RemotePotatoServer.Properties.Resources.logo_none;
                currentlySelectedChannelMappingInfo += "\r\nNo logo file is found.";
            }

            // Channel Name
            lblChannelName.Text = editChannel.Callsign;

            // Channel Number
            nudChannelNumber.Value = Convert.ToDecimal(editChannel.MCChannelNumber);
            nudSubChannelNumber.Value = Convert.ToDecimal(editChannel.MCSubChannelNumber);

            // Fave lineups
            lbChannelFavoriteLineups.DataSource = editChannel.FavoriteLineUpNamesList;

            PopulatingSelectedChannelInfo = false;
        }
        private void nudChannelNumber_ValueChanged(object sender, EventArgs e)
        {
            if (PopulatingChannelManagerListView) return;
            if (PopulatingSelectedChannelInfo) return;

            TVService editChannel = TVChannelForSelectedItem();
            if (editChannel == null) return;

            editChannel.MCChannelNumber = Convert.ToDouble(nudChannelNumber.Value);
            SetLocalChannelCacheDirty(true);

            RefreshChannelManagerListViewSelectedItem();
        }
        private void nudSubChannelNumber_ValueChanged(object sender, EventArgs e)
        {
            if (PopulatingChannelManagerListView) return;
            if (PopulatingSelectedChannelInfo) return;

            TVService editChannel = TVChannelForSelectedItem();
            if (editChannel == null) return;

            editChannel.MCSubChannelNumber = Convert.ToDouble(nudSubChannelNumber.Value);
            SetLocalChannelCacheDirty(true);

            RefreshChannelManagerListViewSelectedItem();
        }
        private string currentlySelectedChannelMappingInfo = "";  
        private void btnShowChannelLogoMappingInfo_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            RPMessageBox.Show(currentlySelectedChannelMappingInfo, "Logo Mapping Information");
        }

        // Send Faves
        private void btnSendFavoritesToMediaCenter_Click(object sender, EventArgs e)
        {
            if (RPMessageBox.ShowQuestion("This will create a 'Favorite LineUp' within Media Center using the channels you have selected as favorites above.  This is an experimental feature and for expert use only.  Do you wish to continue?", "Send Favorite Channels to Media Center") == DialogResult.No)
                return;

            while (Functions.isProcessRunning("ehshell"))
            {
                if (RPMessageBox.ShowQuestion("Windows Media Center is currently running in the background.  Even if you have closed the window, it can sometimes take a few minutes for the program to end.  Please close Windows Media Center, or wait for it to end.\n\nClick Yes to retry, or No to abandon.", "Close Windows Media Center") == DialogResult.No)
                    return;
            }

            SendFavoritesToMediaCenter();

            RPMessageBox.Show("Your favorites are now available within Windows Media Center - scroll to the left of the main TV Guide to see the list of Favorite LineUps.", "Favorites Added to Media Center");
        }
        void SendFavoritesToMediaCenter()
        {
            // Save channels first
            btnChanMgrSaveChanges_Click(new object(), new EventArgs());

            EPGManager.SendFavoriteChannelsToMediaCenter();
        }

        // Save / Load Channels - Set Dirty
        private bool LocalChannelCacheIsDirty;

        private void btnChanMgrSaveChanges_Click(object sender, EventArgs e)
        {
            SaveChannelList();

            if (EPGManager.SaveChannelsToLocal())
            {
                SetLocalChannelCacheDirty(false);
                //RPMessageBox.Show("Your channel list has been saved.");
            }
            else
                RPMessageBox.ShowWarning("Sorry, your channel list could not be saved.");
        }
        private void btnRevertChannelList_Click(object sender, EventArgs e)
        {
            if (RPMessageBox.ShowQuestion("Are you sure?  This will erase any unsaved changes to your favourites or channel order.","Update channels from Media Center") == DialogResult.Yes)
                RevertChannelList();
        }

        private void btnRemoveEmptyChannels_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            RemoveEmptyQAMChannels();
        }
        private void RemoveEmptyQAMChannels()
        {
            bool somethingChanged = false;
            if (LocalChannelCache == null) return; // no channels

            List<TVService> newLocalChannelCache = new List<TVService>();
            foreach (TVService tvs in LocalChannelCache)
            {
                if (tvs.Callsign.ToLowerInvariant().Equals("c" + tvs.ChannelNumberString()))
                {
                    // Don't add, and flag that there's been a change.
                    if (!somethingChanged) somethingChanged = true;
                }
                else
                {
                    newLocalChannelCache.Add(tvs);
                }
            }

            if (somethingChanged)
            {
                LocalChannelCache.Clear();
                LocalChannelCache = null;
                LocalChannelCache = newLocalChannelCache;

                FillListViewFromLocalChannelCache();
                SetLocalChannelCacheDirty(true);
            }
            else
            {
                newLocalChannelCache.Clear();
                newLocalChannelCache = null;
            }
        }
        void SaveChannelList()
        {
            if (LocalChannelCache == null) return; // no channels

            int u = 1;
            foreach (TVService tvc in LocalChannelCache)
            {
                tvc.UserSortOrder = u++;
            }

            EPGManager.OverwriteAllChannelsWithNewList(LocalChannelCache);
        }
        void RevertChannelList()
        {
            EPGManager.ExternalPopulateTVChannels(true);
        }
        void SetLocalChannelCacheDirty(bool set)
        {
            if (LocalChannelCacheIsDirty != set)
            {
                LocalChannelCacheIsDirty = set;
                // Bold up save button if dirty
                btnChanMgrSaveChanges.Font = new Font(btnChanMgrSaveChanges.Font, set ? FontStyle.Bold : FontStyle.Regular);
            }
        }
        void ChanMgrPromptIfDirty()
        {
            if (LocalChannelCacheIsDirty)
            {
                if (RPMessageBox.ShowQuestion("Do you wish to save changes to your channels?", "Save channels") == DialogResult.Yes)
                {
                    SaveChannelList();
                    SetLocalChannelCacheDirty(false);
                }
            }
        }

        // Helper
        private TVService TVChannelForSelectedItem()
        {
            if (lvChannelManager.SelectedIndices.Count < 1) return null;
            int SelectedIndex = lvChannelManager.SelectedIndices[0];

            return LocalChannelCache[SelectedIndex];
        }


        private void btnShowChannelImportingOptions_Click(object sender, EventArgs e)
        {
            ShowChannelImportingOptionsForm();
        }
        void ShowChannelImportingOptionsForm()
        {
            using (FormChannelImportingOptions frmChans = new FormChannelImportingOptions())
            {
                if (frmChans.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    // do nothing
                }
            }
        }
        #endregion

        #region Setup Wizard
        void ShowSetupWizardIfNeverShown()
        {
            if (!(Settings.Default.EverShownSetupWizard))
            {
                Settings.Default.EverShownSetupWizard = true;
                ShowSetupWizard();
            }
        }
        private void ShowSetupWizard()
        {
            FormConnectionWizard fWizard = new FormConnectionWizard();
            if (fWizard.ShowDialog() != System.Windows.Forms.DialogResult.Ignore)
                Functions.WriteLineToLogFile("Ran setup wizard");
            // ignore
            //NetworkHelper helper = new NetworkHelper();
            //helper.Default.AddRPMappings();
            //helper = null;
        }
        #endregion

        // TESTING
        private void btnTestButton_Click(object sender, EventArgs e)
        {
           
        }

        #region Firewall
        private void btnAddWindowsFirewallRules_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            ShowSetupWizard();
        }
        void OfferToAddFirewallRulesIfNotAlreadyOffered()
        {
            if (
                (Settings.Default.HaveOfferedWindowsFirewallForPort != Convert.ToInt32(Settings.Default.Port)) 
               )
            {
                Settings.Default.HaveOfferedWindowsFirewallForPort = Convert.ToInt32(Settings.Default.Port);
                FirewallHelper.AskThenAddFirewallRules();
            }
        }



        #endregion

        #region Pics

        #endregion

        #region Control Data Bindings
        void RemoveControlBindings(Control parentControl) // Not currently using this; it was buggy
        {
            foreach (Control ctrl in parentControl.Controls)
            {
                if (ctrl.Controls.Count > 0)
                    RemoveControlBindings(ctrl);

                ctrl.DataBindings.Clear();
            }
        }
        void BindControls()
        {
            this.cbShowExpertSettings.DataBindings.Add(new System.Windows.Forms.Binding("Checked", Settings.Default, "ShowExpertTabsInUI", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.cbEnableMusicLibrary.DataBindings.Add(new System.Windows.Forms.Binding("Checked", Settings.Default, "EnableMusicLibrary", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.cbEnableMediaCenter.DataBindings.Add(new System.Windows.Forms.Binding("Checked", Settings.Default, "EnableMediaCenterSupport", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.cbEnablePictureLibrary.DataBindings.Add(new System.Windows.Forms.Binding("Checked", Settings.Default, "EnablePictureLibrary", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.cbEnableVideoLibrary.DataBindings.Add(new System.Windows.Forms.Binding("Checked", Settings.Default, "EnableVideoLibrary", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.numericUpDown4.DataBindings.Add(new System.Windows.Forms.Binding("Enabled", Settings.Default, "CheckForAppUpdates", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.numericUpDown4.DataBindings.Add(new System.Windows.Forms.Binding("Value", Settings.Default, "DaysBetweenAppUpdateCheck", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.checkBox8.DataBindings.Add(new System.Windows.Forms.Binding("Checked", Settings.Default, "CheckForAppUpdates", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
           // this.txtPassword.DataBindings.Add(new System.Windows.Forms.Binding("Enabled", Settings.Default, "RequirePassword", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
           // this.txtPassword.DataBindings.Add(new System.Windows.Forms.Binding("Text", Settings.Default, "UserPassword", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.txtManualRecordingName.DataBindings.Add(new System.Windows.Forms.Binding("Text", Settings.Default, "DefaultManualRecordingName", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.nudPrePadding.DataBindings.Add(new System.Windows.Forms.Binding("Value", Settings.Default, "DefaultPrePadding", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.numericUpDown2.DataBindings.Add(new System.Windows.Forms.Binding("Value", Settings.Default, "DefaultPostPadding", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.txtMainMenuTitle.DataBindings.Add(new System.Windows.Forms.Binding("Text", Settings.Default, "MainMenuTitle", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.cbDeinterlaceRecordedTVByDefault.DataBindings.Add(new System.Windows.Forms.Binding("Checked", Settings.Default, "DeinterlaceRecTVByDefault", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.nudStreamTimeToLive.DataBindings.Add(new System.Windows.Forms.Binding("Value", Settings.Default, "MediaStreamerSecondsToKeepAlive", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.nudStreamingVolumePercent.DataBindings.Add(new System.Windows.Forms.Binding("Value", Settings.Default, "StreamingVolumePercent", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.cbRestrictMediaStreamerToLevel30.DataBindings.Add(new System.Windows.Forms.Binding("Checked", Settings.Default, "RestrictMediaStreamerToLevel30", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            // DELETED - NOT USED  this.cbMediaStreamingUseExtraAudioSync.DataBindings.Add(new System.Windows.Forms.Binding("Checked", Settings.Default, "MediaStreamerUseExtraAudioSync", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.cbSilverlightCacheEPGDays.DataBindings.Add(new System.Windows.Forms.Binding("Checked", Settings.Default, "SilverlightCacheEPGDays", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.cbEnforceClientIPSecurity.DataBindings.Add(new System.Windows.Forms.Binding("Checked", Settings.Default, "EnforceClientIPSecurity", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.cbSilverlightStoreCacheLocally.DataBindings.Add(new System.Windows.Forms.Binding("Checked", Settings.Default, "SilverlightStoreCacheLocally", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.numericUpDown5.DataBindings.Add(new System.Windows.Forms.Binding("Value", Settings.Default, "SilverlightEPGOverspillHours", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.cbSilverlightIsDefault.DataBindings.Add(new System.Windows.Forms.Binding("Checked", Settings.Default, "SilverlightIsDefault", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            //  CAUSING ISSUES COS IT'S 2 WAY?  this.cbSilverlightIsDefault.DataBindings.Add(new System.Windows.Forms.Binding("Enabled", Settings.Default, "SilverlightEnabled", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.cbEnableSilverlight.DataBindings.Add(new System.Windows.Forms.Binding("Checked", Settings.Default, "SilverlightEnabled", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.numericUpDown3.DataBindings.Add(new System.Windows.Forms.Binding("Value", Settings.Default, "EPGGridChannelsPerPage", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.nudEPGPreviewWindowSize.DataBindings.Add(new System.Windows.Forms.Binding("Value", Settings.Default, "EPGGridPreviewWindowSize", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.cbEPGShowChannelNumbers.DataBindings.Add(new System.Windows.Forms.Binding("Checked", Settings.Default, "EPGShowChannelNumbers", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.checkBox1.DataBindings.Add(new System.Windows.Forms.Binding("Checked", Settings.Default, "ShowEPGRecommendedMovies", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.cbShowEPGBackgroundColours.DataBindings.Add(new System.Windows.Forms.Binding("Checked", Settings.Default, "ShowBackgroundColoursInEPG", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.cbShowChannelLogos.DataBindings.Add(new System.Windows.Forms.Binding("Checked", Settings.Default, "ShowChannelLogos", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.cbDisplayEPGFavouriteChannelsOnly.DataBindings.Add(new System.Windows.Forms.Binding("Checked", Settings.Default, "ShowFavouriteChannelsInEPG", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.checkBox2.DataBindings.Add(new System.Windows.Forms.Binding("Checked", Settings.Default, "ListMoviesOnFavouriteChannelsOnly", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.checkBox5.DataBindings.Add(new System.Windows.Forms.Binding("Checked", Settings.Default, "RecordingsRetrieveAsParanoid", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.nudViewScheduledRecordingsDaysAhead.DataBindings.Add(new System.Windows.Forms.Binding("Value", Settings.Default, "ViewScheduledRecordingsDaysAhead", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.cbCacheBinaryFiles.DataBindings.Add(new System.Windows.Forms.Binding("Checked", Settings.Default, "CacheBinaryFiles", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.cbCacheTextFiles.DataBindings.Add(new System.Windows.Forms.Binding("Checked", Settings.Default, "CacheTextFiles", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.cbAllowRemoteLog.DataBindings.Add(new System.Windows.Forms.Binding("Checked", Settings.Default, "AllowRemoteLogRetrieval", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.cbDebugChannels.DataBindings.Add(new System.Windows.Forms.Binding("Checked", Settings.Default, "DebugChannels", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.checkBox10.DataBindings.Add(new System.Windows.Forms.Binding("Checked", Settings.Default, "DebugStreaming", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.checkBox6.DataBindings.Add(new System.Windows.Forms.Binding("Checked", Settings.Default, "DebugAdvanced", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.checkBox3.DataBindings.Add(new System.Windows.Forms.Binding("Checked", Settings.Default, "DebugBasic", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.cbDebugCache.DataBindings.Add(new System.Windows.Forms.Binding("Checked", Settings.Default, "DebugCache", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.cbDebugServer.DataBindings.Add(new System.Windows.Forms.Binding("Checked", Settings.Default, "DebugServer", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.cbDebugScheduling.DataBindings.Add(new System.Windows.Forms.Binding("Checked", Settings.Default, "DebugScheduling", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.cbDebugRecordedTV.DataBindings.Add(new System.Windows.Forms.Binding("Checked", Settings.Default, "DebugRecTV", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.cbDebugFullAPI.DataBindings.Add(new System.Windows.Forms.Binding("Checked", Settings.Default, "DebugFullAPI", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.cbDebugStreamingAdvanced.DataBindings.Add(new System.Windows.Forms.Binding("Checked", Settings.Default, "DebugAdvancedStreaming", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.nudRecTVRecheckPostponedFilesEvery.DataBindings.Add(new System.Windows.Forms.Binding("Value", Settings.Default, "RecTVRecheckPostponedFilesEvery", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));

            this.txtCustomFFMpegCLI.DataBindings.Add(new System.Windows.Forms.Binding("Text", Settings.Default, "CustomFFMpegTemplate", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.cbUseCustomFFMpegCLI.DataBindings.Add(new System.Windows.Forms.Binding("Checked", Settings.Default, "UseCustomFFMpegTemplate", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));

            this.cbEnableMediaCenter.Visible = (UIFunctions.OSSupportsMediaCenterFunctionality);
            this.btnDisplayAboutWindowsMediaCenter.Visible = (UIFunctions.OSSupportsMediaCenterFunctionality);

            this.cbDefaultRecordFirstRunOnly.DataBindings.Add(new System.Windows.Forms.Binding("Checked", Settings.Default, "DefaultRecordFirstRunOnly", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            ShowHideMediaCenterRelatedControls();
            
        }
        void ShowHideMediaCenterRelatedControls()
        {
            // Controls visible only with media center support
            this.gpAdvancedEPGMovies.Visible = Settings.Default.EnableMediaCenterSupport;
            this.gpAdvancedScheduledRecordings.Visible = Settings.Default.EnableMediaCenterSupport;
            this.btnHelpIRHelper.Visible = Settings.Default.EnableMediaCenterSupport;
            this.cbStartRPKeySenderWithWindows.Visible = Settings.Default.EnableMediaCenterSupport;
        }

        #endregion

        #region Edit Users
        private void btnEditUsers_Click(object sender, EventArgs e)
        {
            FormUserManagement fUsers = new FormUserManagement();
            fUsers.ShowDialog();
        }
        #endregion

        


        private void cbDebugFullAPI_Click(object sender, EventArgs e)
        {
            if (cbDebugFullAPI.Checked)
                RPMessageBox.ShowAlert("You should check this box only if asked to do so by a support assistant.  Remember to un-check this box later as it will result in very large log files being generated at " + Functions.DebugLogFileFN);
        }


        #region Media Folders

        private void btnEditPicturesLibraryFolders_Click(object sender, EventArgs e)
        {
            using (FormFoldersCollection frmFolders = new FormFoldersCollection(Settings.Default.PictureLibraryFolders, false, false, false, Functions.OSSupportsExplorerLibraries, Settings.Default.UseExplorerLibraryForPictureFolders, "Add the folders where you keep your picture files.  Subfolders are included automatically.", "Picture Library Folders", "pictures"))
            {
                DialogResult dr = frmFolders.ShowDialog();

                // Save settings and folders?
                if (dr == System.Windows.Forms.DialogResult.OK)
                {
                    Settings.Default.PictureLibraryFolders = frmFolders.Folders;
                    if (Functions.OSSupportsExplorerLibraries)
                        Settings.Default.UseExplorerLibraryForPictureFolders = frmFolders.IsUseWin7LibraryCheckboxChecked;
                }
            }
        }

        private void btnEditVideoLibraryFolders_Click(object sender, EventArgs e)
        {
            using (FormFoldersCollection frmFolders = new FormFoldersCollection(Settings.Default.VideoLibraryFolders, false, false, false, Functions.OSSupportsExplorerLibraries, Settings.Default.UseExplorerLibraryForVideoFolders, "Add the folders where you keep your video files.  Subfolders are included automatically.", "Video Library Folders", "videos"))
            {
                DialogResult dr = frmFolders.ShowDialog();

                // Save settings and folders?
                if (dr == System.Windows.Forms.DialogResult.OK)
                {
                    Settings.Default.VideoLibraryFolders = frmFolders.Folders;
                    if (Functions.OSSupportsExplorerLibraries)
                        Settings.Default.UseExplorerLibraryForVideoFolders = frmFolders.IsUseWin7LibraryCheckboxChecked;
                }
            }
        }

        private void btnAddRemoveRecordedTVFolders2_Click(object sender, EventArgs e)
        {
            AddRemoveRecTVFolders();
        }
        private void btnJumpToMusicLibraryTab_Click(object sender, EventArgs e)
        {
            ShowChangeMediaLibraryAccountDialog();
        }
        private void cbEnableMediaCenter_Click(object sender, EventArgs e)
        {
            ShowInterfaceForEnabledModules();

            ShowHideMediaCenterRelatedControls();
        }
        private void btnDisplayAboutWindowsMediaCenter_Click(object sender, EventArgs e)
        {
            string strWMC = "Ticking this box allows you full remote access to Windows 7 Media Center - schedule recordings, browse the EPG, manage series, and more.\r\n\r\nWould you like to read more about this now?";

            if (RPMessageBox.ShowQuestion(strWMC, "Windows Media Center Support") == System.Windows.Forms.DialogResult.Yes)
            {
                string target = "http://www.remotepotato.com/mediacenter.aspx";
                System.Diagnostics.Process.Start(target);

            }
        }

        #endregion


        // Help

   
    }




}
