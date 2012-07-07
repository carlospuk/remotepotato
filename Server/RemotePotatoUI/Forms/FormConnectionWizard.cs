using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Threading;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using RemotePotatoServer.Network;
using RemotePotatoServer.Properties;

namespace RemotePotatoServer
{
    public partial class FormConnectionWizard : Form
    {
        string LocalIP = null;
        public FormConnectionWizard()
        {
            InitializeComponent();

            Shown += new EventHandler(FormConnectionWizard_Shown);
            DNSHelper dh = new DNSHelper();
            dh.Start();
        }

        void FormConnectionWizard_Shown(object sender, EventArgs e)
        {
            ShowHideFromSettings();
            BindControls();
            SelectPages();

            // Password box
            SetPasswordBoxFromSettings();
            
            // Get local IP and external IP in background
            Network.IPHelper ipHelper = new Network.IPHelper();  // EVENT NOT REQUIRED ipHelper.QueryExternalIPAsync_Completed += new EventHandler<Network.IPHelper.GetExternalIPEventArgs>(ipHelper_QueryExternalIPAsync_Completed);
            LocalIP = ipHelper.GetLocalIP();
            ipHelper.QueryExternalIPAsync();
        }

        void SelectPages()
        {
            // Pages can be added to the skip list here depending upon server settings, context etc.
             
        }

        #region Automated Expert Actions - Firewall etc
        void AutomateExpertActions()
        {
            AddFirewallRules(); // Always add them, even if it means doubling up            
            ReserveURLForPort(); // Reserve URL
        }
        #endregion

        #region Tab 0 - Welcome/Security
        private void cbEnableSecurity_MouseClick(object sender, MouseEventArgs e)
        {
            ShowHideFromSettings();
        }
        void TryMoveFromStep0(ref bool cancel, ref string CancelReason)
        {
            bool success = true;
            if (cbEnableSecurity.Checked)
            {
                success &= (txtUsername.Text.Trim().Length > 0);
                success &= (txtPassword.Text.Trim().Length > 0);

                if (!success)
                    CancelReason = "The username and/or password cannot be blank.";
            }

            // EXPERT?  DO FIREWALL RULES ETC
            if (!cbShowExpertOptions.Checked)
                AutomateExpertActions();

            cancel = !success;
        }
        #endregion

        #region Tab 1 - Ports
        private void nudSilverlightStreamingPort_ValueChanged(object sender, EventArgs e)
        {
            int highestPort = Convert.ToInt32(nudSilverlightStreamingPort.Value) + Settings.Default.SilverlightStreamingNumberOfPorts - 1;
            lblSilverlightStreamingHighestPort.Text = "- " + highestPort.ToString();
        }




        void TryMoveFromStep1(ref bool cancel, ref string CancelReason)
        {

            int httpport = Convert.ToInt32(nudPort.Value );
            int lowestStreamPort = Convert.ToInt32(nudSilverlightStreamingPort.Value);
            int highestStreamPort = lowestStreamPort + Settings.Default.SilverlightStreamingNumberOfPorts;
            if (
                (httpport >= lowestStreamPort) &&
                (httpport <= highestStreamPort)
                )
            {
                cancel = true;
                CancelReason = "The main port number must not be the same as one of the streaming port numbers; i.e. it must be lower than " + lowestStreamPort.ToString() + " and higher than " + highestStreamPort.ToString() + ".";
            }

     
        }

        #endregion

        #region Tab 2 - Firewall/URLReserver
        void TryMoveFromStep2(ref bool cancel, ref string CancelReason)
        {

            if (cbAddWindowsFirewallRule.Checked)
            {
                AddFirewallRules();
            }

            if ((Settings.Default.LastSetSecurityForPort != Settings.Default.Port))
            {
                ReserveURLForPort();
                return;
            }
            

        }
        void AddFirewallRules()
        {
            // Add firewall rules
            if (!FirewallHelper.AddFirewallRules())
                RPMessageBox.ShowAlert("The firewall rules could not be added to Windows Firewall.\r\n\r\nYou may need to manually add incoming and outgoing rules for your ports.");

            Settings.Default.HaveOfferedWindowsFirewallForPort = Convert.ToInt32( Settings.Default.Port );
            
        }
        void ReserveURLForPort()
        {
            URLReserver reserver = new URLReserver();
            int ResultCode = reserver.ReserveUrl(Convert.ToInt32(Settings.Default.Port), "/", true);
            if (ResultCode == 0)
            {
                Functions.WriteLineToLogFile("URLReserver: 0 OK");
                Settings.Default.LastSetSecurityForPort = Convert.ToInt32(Settings.Default.Port);
            }
            else
            {
                Functions.WriteLineToLogFile("URLReserver: NOT OK");
                RPMessageBox.ShowAlert("Could not reserve a Url for Remote Potato server - error code " + ResultCode.ToString());
            }

        }
        #endregion

        #region Tab 3 - Router UPnP
        NATHelper nathelper;

        #region Activity Pane / Status
        void ShowActivityMask()
        {
            gpPortCheck.Visible = true;
            gpRouterReport.Visible = false;
            
        }
        void HideActivityMask()
        {
            gpPortCheck.Visible = false;
            gpRouterReport.Visible = true;
        }
        delegate void ChangeActivityCallback(string txtActivity);
        private void ChangeActivityStatus(string txtActivity)
        {
            ChangeActivityCallback d = new ChangeActivityCallback(unsafeChangeActivityStatus);
            this.Invoke(d, new object[] { txtActivity });
        }
        void unsafeChangeActivityStatus(string txtActivity)
        {
            lblRouterActivityStatus.Text = txtActivity;
        }

        #endregion

        void ShownTab3()
        {
            TemporaryCludge();
            return;
            /*
            if (nathelper == null)
                nathelper = new NATHelper();

            // Show port check mask and marquee
            ShowActivityMask();
            ChangeActivityStatus( cbShowExpertOptions.Checked ? "Checking if ports are forwarded through router" : "Checking if your router is set up correctly..." );

            Thread t = new Thread(new ThreadStart(CheckPortForwarding));
            t.Start(); */
        }

        void TemporaryCludge()
        {
            HideActivityMask();
            btnAddPortForwardingRules.Visible = false;
            int lowestStreamPort = Convert.ToInt32(Settings.Default.SilverlightStreamingPort);
            int highestStreamPort = lowestStreamPort + Settings.Default.SilverlightStreamingNumberOfPorts - 1;
            string txtManualRules = String.Format("You need to forward port {0} and ports {1}-{2} to IP address {3} (this computer)\r\n\r\nNeed help? Visit www.portforward.com", Settings.Default.Port.ToString(), lowestStreamPort.ToString(), highestStreamPort.ToString(), LocalIP);
            ChangeBlurbText("If you have not already done so, you will need to set up your home router (if you have one) to allow Internet connections through to this PC.\r\n\r\nFuture editions of Remote Potato will aim to achieve this automatically - for now, please consult your manual or the website www.portforward.com to learn how to add the following rules to your home router:\r\n\r\n" + txtManualRules);
        }

        private void CheckPortForwarding()
        {

            /*  THIS IS A PROPER CHECK FOR INTERNET ACCESS FROM OUTSIDE, UNFORTUNATELY MY ROUTER FAILS BECAUSE THE TEST SERVER TCPLISTENER HANGS AROUND IN ITS ROUTING TABLE AND CAUSES IT TO FAIL WHEN ADDING THE FORWARDING RULE
            Network.PortChecker pChecker = new Network.PortChecker();
            pChecker.CheckPortOpenAsync_Completed += new EventHandler<Network.PortChecker.CheckPortCompletedEventArgs>(pChecker_CheckPortOpenAsync_Completed);
            pChecker.CheckPortOpenAsync(Convert.ToInt32(Settings.Default.Port), true);
            
             SO, INSTEAD, JUST CHECK VIA UPNP MAPPINGS:
             
             */
            if (nathelper == null) return;
            PortChecker.CheckPortCompletedEventArgs args = null;
            string txtError = "";
            
            if (!nathelper.GatewayFound)
            {
                if (!nathelper.Discover(ref txtError))
                {
                    HideActivityMask();
                    ChangeBlurbText("Remote Potato could not find a UPnp router on your local network.\r\n\r\nDo you have a router?  If so, click the button below to attempt to set up your router anyway; if this fails, you will be given instructions on manual setup.");
                    return;
                }
            }

            if (nathelper.RPMappingsExist(LocalIP))
            {
                args = new PortChecker.CheckPortCompletedEventArgs(true, true, "");
            }
            else
            {
                args = new PortChecker.CheckPortCompletedEventArgs(true, false, "");
            }

            changePortCheckStatus(args);
            
        }
        void pChecker_CheckPortOpenAsync_Completed(object sender, Network.PortChecker.CheckPortCompletedEventArgs e)
        {
            changePortCheckStatus(e);  
        }

        delegate void ChangeStatusCallBack(PortChecker.CheckPortCompletedEventArgs args);
        private void changePortCheckStatus(PortChecker.CheckPortCompletedEventArgs args)
        {
            ChangeStatusCallBack d = new ChangeStatusCallBack(unsafeChangePortCheckStatus);
            this.Invoke(d, new object[] { args });
        }
        void unsafeChangePortCheckStatus(Network.PortChecker.CheckPortCompletedEventArgs e)
        {
            HideActivityMask();
            btnAddPortForwardingRules.Enabled = true;  // unless we change it to false

            if (!e.DidComplete)
            {
                ChangeBlurbText("Remote Potato was unable to test whether Internet connections are getting through to this computer from outside your home network.\r\n\r\n" +
                                        "Your router may need to be set up to allow this.  ('port forwarding')  Remote Potato can try to automatically set up your router for you using UPnP technology.\r\n\r\n" +
                                        "You may skip this step if you prefer to set up your router manually later, if your router doesn't support UPnP or if you won’t be using Remote Potato over the Internet.\r\n\r\n" +
                                        "If unsure, answer 'Yes'.");
            }
            else
            {
                if (e.PortOpen)
                {
                    ChangeBlurbText("Remote Potato determined that your router is correctly set up to allow access from outside your home network.");
                    btnAddPortForwardingRules.Enabled = false;
                }
                else
                {
                    ChangeBlurbText("Remote Potato has checked and Internet connections are not getting through to this computer from outside your home network.\r\n\r\n" + 
                                            "The most likely reason is that your router has to be set up to allow this. ('port forwarding')  Remote Potato can try to automatically set up your router for you using UPnP technology.\r\n\r\n" +
                                            "You may skip this step if you prefer to set up your router manually later, if your router doesn't support UPnP or if you won’t be using Remote Potato over the Internet.\r\n\r\n" +
                                            "If unsure, answer 'Yes'.");
                    
                }
            }

            
        }
        delegate void ChangeBlurbTextCallback(string txt);
        private void ChangeBlurbText(string txt)
        {
            ChangeBlurbTextCallback d = new ChangeBlurbTextCallback(unsafeChangeBlurbText);
            this.Invoke(d, new object[] { txt });
        }
        void unsafeChangeBlurbText(string txt)
        {
            lblRouterBlurb.Text = txt;
        }

        // ADD RULES
        private void btnAddPortForwardingRules_Click(object sender, EventArgs e)
        {
            ShowActivityMask();

            Thread t = new Thread(new ThreadStart(AddPortForwardingRules));
            t.Start();
        }

        // SHOW ROUTER SETTINGS
        private void btnShowRouterSettings_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if ( (nathelper == null) || (! nathelper.GatewayFound) )
            {
                RPMessageBox.ShowAlert("A compatible router (UPnP) could not be found on your network.");
                return;
            }

            string strMappings = nathelper.ListMappings();
            RPMessageBox.Show(strMappings, "Router Port Mappings");
        }

        void AddPortForwardingRules()
        {
            if (nathelper == null) return;
            if (! nathelper.GatewayFound) return;

            ChangeActivityStatus("Checking Local IP Address...");

            if (string.IsNullOrWhiteSpace(LocalIP))
            {
                ChangeBlurbText("Remote Potato could not get the local IP address, so could not set up your router.");
            }
            else
            {
                ChangeActivityStatus("Checking for router...");
                string txtError = "";

                // Manual rules
                int lowestStreamPort = Convert.ToInt32(Settings.Default.SilverlightStreamingPort);
                int highestStreamPort = lowestStreamPort + Settings.Default.SilverlightStreamingNumberOfPorts - 1;
                string txtManualRules = String.Format("You need to forward port {0} and ports {1}-{2} to IP address {3} (this computer)\r\n\r\nNeed help? Visit www.portforward.com", Settings.Default.Port.ToString(), lowestStreamPort.ToString(), highestStreamPort.ToString(), LocalIP);

                if (!nathelper.Discover(ref txtError))
                {
                    ChangeBlurbText("Remote Potato could not find a router on your local network - have you disabled UPnP on your router?\r\n\r\n" + txtManualRules);
                }
                else
                {
                    ChangeActivityStatus("Setting up router...");
                    if (nathelper.ForwardRPPorts(LocalIP) == NATHelper.NatHelperReponseCodes.OK)
                    {
                        RPMessageBox.Show("Your router has been automatically set up for Remote Potato.\r\nClick OK to verify the changes.");
                        // TODO: 
                        CheckPortForwarding();
                    }
                    else
                    {
                        ChangeBlurbText("Your router could not be automatically set up (see the debug log for more information)\r\n\r\n." + txtManualRules);
                    }

                }

                nathelper = null;
            }

            // Finished - hide mask
            this.Invoke(new Action( HideActivityMask));
        }
        void TryMoveFromStep3(ref bool cancel, ref string CancelReason)
        { }
        #endregion

        #region Tab 4 - Dynamic DNS Choice
        void TryMoveFromStep4(ref bool cancel, ref string CancelReason)
        {

        }

        private void cmbDynDNSOption_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Bind to settings
            switch (cmbDynDNSOption.SelectedIndex)
            {
                case 0: // use IP only
                    Settings.Default.DynamicDNSServiceUsed = false;
                    Settings.Default.DynDNSClientEnabled = false;
                    break;

                case 1: // use DynDNS
                    Settings.Default.DynamicDNSServiceUsed = true;
                    break;

                case 2: // Use 3rd party Dynamic DNS service
                    Settings.Default.DynamicDNSServiceUsed = true;
                    break;
            }


            gp3rdPartyDynamicDNS.Visible = false; // unless we say otherwise below

            if (cmbDynDNSOption.SelectedIndex == 1) // DYNDNS 
            {
                // Ensure we visit page 5, the DynDNS Page
                RemovePageFromSkip(5);
            }
            else
            {
                // Ensure we skip page 5, the DynDNS Page
                AddPageToSkip(5);

                if (cmbDynDNSOption.SelectedIndex == 2)  // 3rd party Dynamic DNS service
                {
                    // show option for name
                    gp3rdPartyDynamicDNS.Visible = true;
                }
            }



        }

        #endregion

        #region Tab 5 - DynDNS
        void TryMoveFromStep5(ref bool cancel, ref string CancelReason)
        {
            // Test DNS credentials

            // Use Remote Potato Dyn DNS client?
            if (Settings.Default.DynDNSClientEnabled)
            {
                // Blank creds?
                if ((string.IsNullOrWhiteSpace(Settings.Default.DynDNSUsername)) ||
                    (string.IsNullOrWhiteSpace(Settings.Default.DynDNSPassword)) ||
                    (string.IsNullOrWhiteSpace(Settings.Default.DynamicDNSHostname))
                    )
                {
                    cancel = true;
                    CancelReason = "The DynDNS Hostname, username and password must all be completed to use the in-build DynDNS client.";
                }
                else
                {

                    TestDynDNS(ref cancel, ref CancelReason);
                }
            }
        }

        private static void TestDynDNS(ref bool cancel, ref string CancelReason)
        {
            // Test client
            DNSHelper dHelper = new DNSHelper();
            DNSHelper.DynDnsUpdateResult result = dHelper.NotifyDynDNS(Settings.Default.LastPublicIP);
            if (! (
                (result == DNSHelper.DynDnsUpdateResult.UpdatedIp) ||
                (result == DNSHelper.DynDnsUpdateResult.NoUpdateSameIp)
                ))
            {
                CancelReason = dHelper.ErrorMessageForResult(result);
                CancelReason = "DynDNS could not update your IP address: " + CancelReason;
                cancel = true;
            }
        }
        private void txtDynDNSHostname_Click(object sender, EventArgs e)
        {
            
        }
        private void txtDynDNSHostname_Enter(object sender, EventArgs e)
        {
            txtDynDNSHostname.SelectAll();
        }

        private void cmbDynDNSUseRPClient_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Bind to settings
            Settings.Default.DynDNSClientEnabled = (cmbDynDNSUseRPClient.SelectedIndex == 0);

            gpRemotePotatoDynDNSClientSettings.Enabled = (cmbDynDNSUseRPClient.SelectedIndex == 0);
        }
        private void btnDynDnsSignUp_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string target = "https://www.dyndns.com/services/dns/dyndns/";
            System.Diagnostics.Process.Start(target);
        }

        private void txtDynDNSHostname_TextChanged(object sender, EventArgs e)
        {
            ShowDynDnsOptionsIfHostnameIsPopulated();
        }
        void ShowDynDnsOptionsIfHostnameIsPopulated()
        {
            bool showOptions = (txtDynDNSHostname.Text.Length > 0);
            gpDynDnsClientChoicePanel.Visible = showOptions;
        }

        #endregion

        #region Tab 6 - Done
        void ShownTab6()
        {
            // Show port check mask and marquee
            ucConnectionSummary1.Init(true);  // need to run a test server, remote potato server is not running
        }
        #endregion

        #region Bind Controls / Populate Controls
        void ShowHideFromSettings()
        {
            // STEP 0 - SECURITY
            txtUsername.Visible = (Settings.Default.RequirePassword);
            txtPassword.Visible = (Settings.Default.RequirePassword);
            lblUsername.Visible = (Settings.Default.RequirePassword);
            lblPassword.Visible = (Settings.Default.RequirePassword);
            lblHeaderUsernamePassword.Visible = (Settings.Default.RequirePassword);

            // STEP 2 - PORT FORWARDING
            lblDNSblurb.Text = lblDNSblurb.Text.Replace("**EXTERNAL-IP**", Settings.Default.LastPublicIP);

            // STEP 3 - DYN DNS
            if (Settings.Default.DynDNSClientEnabled)
            {
                cmbDynDNSOption.SelectedIndex = 1; // definitely dyn dns
            }
            else
            {
                if (!Settings.Default.DynamicDNSServiceUsed)
                    cmbDynDNSOption.SelectedIndex = 0;  // ext IP
                else
                {
                    if ((!string.IsNullOrWhiteSpace(Settings.Default.DynamicDNSHostname)) &&
                            (Settings.Default.DynamicDNSHostname.Contains("dyndns"))
                        )
                        cmbDynDNSOption.SelectedIndex = 1; // probably dyn dns  (not using our client)
                    else
                        cmbDynDNSOption.SelectedIndex = 2; // guess at 3rd party
                }
                    
            }
            

            // STEP 4 
            if (Settings.Default.DynDNSClientEnabled)
                cmbDynDNSUseRPClient.SelectedIndex = 0; // use the RP client
            else
                cmbDynDNSUseRPClient.SelectedIndex = 1; // dont use the RP client

            ShowDynDnsOptionsIfHostnameIsPopulated();
                
        }
        void BindControls()
        {
            this.cbEnableSecurity.DataBindings.Add(new System.Windows.Forms.Binding("Checked", Settings.Default, "RequirePassword", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.txtUsername.DataBindings.Add(new System.Windows.Forms.Binding("Text", Settings.Default, "UserName", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.nudPort.DataBindings.Add(new System.Windows.Forms.Binding("Value", Settings.Default, "Port", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.nudSilverlightStreamingPort.DataBindings.Add(new System.Windows.Forms.Binding("Value", Settings.Default, "SilverlightStreamingPort", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.txtDynDNSUsername.DataBindings.Add(new System.Windows.Forms.Binding("Text", Settings.Default, "DynDNSUsername", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.txtDynDNSPassword.DataBindings.Add(new System.Windows.Forms.Binding("Text", Settings.Default, "DynDNSPassword", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.txtDynDNSHostname.DataBindings.Add(new System.Windows.Forms.Binding("Text", Settings.Default, "DynamicDNSHostname", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.txtDynamicDNS3rdPartyExternalURL.DataBindings.Add(new System.Windows.Forms.Binding("Text", Settings.Default, "DynamicDNSHostname", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
        }

        void SetPasswordBoxFromSettings()
        {
            txtPassword.Text = (Settings.Default.RequirePassword) ?
                "xxxxxx" : "";
        }
        private void txtPassword_MouseClick_1(object sender, MouseEventArgs e)
        {
            txtPassword.Text = "";
        }

        private void txtPassword_TextChanged_1(object sender, EventArgs e)
        {
            // Don't hash the default string
            if (txtPassword.Text.Equals("xxxxxx")) return;
            if (string.IsNullOrEmpty(txtPassword.Text.Trim())) return;

            StorePasswordFromPasswordBox();
        }
        void StorePasswordFromPasswordBox()
        {
            string rawPassword = txtPassword.Text.Trim();
            if (string.IsNullOrEmpty(rawPassword)) return;

            string encodedPassword = Functions.EncodePassword(rawPassword);
            Settings.Default.UserPassword = encodedPassword;
        }
        #endregion


        #region Navigation
        int PageIndex = 0;
        List<int> ExpertPages = new List<int>() { 1,2 };  // Zero indexed
        List<int> PagesToSkip = new List<int>();
        bool movingForward = true;
        bool PageIsExpert(int pIndex)
        {
            return ExpertPages.Contains(pIndex);
        }
        bool PageIsOnSkipList(int pIndex)
        {
            return PagesToSkip.Contains(pIndex);
        }
        bool PageShouldBeSkipped(int pIndex)
        {
            bool skipExpertPages = (!cbShowExpertOptions.Checked);
            if (skipExpertPages)
                if (PageIsExpert(pIndex))
                    return true;

            return PageIsOnSkipList(pIndex);
        }
        void AddPageToSkip(int page)
        {
            if (!PagesToSkip.Contains(page))
                PagesToSkip.Add(page);
        }
        void RemovePageFromSkip(int page)
        {
            if (PagesToSkip.Contains(page))
                PagesToSkip.Remove(page);
        }
        private void btnPreviousPage_Click(object sender, EventArgs e)
        {
            NavPrevious();
        }

        private void NavPrevious()
        {
            if (--PageIndex >= (wizardPages1.TabCount - 1))
                PageIndex = 0;

            movingForward = false;
            NavToNewPage();
        }
        private void btnNextPage_Click(object sender, EventArgs e)
        {
            NavNext();
        }

        private void NavNext()
        {
            if (++PageIndex >= (wizardPages1.TabCount - 1))
                PageIndex = wizardPages1.TabCount - 1;

            movingForward = true;
            NavToNewPage();
        }
        void NavToNewPage()
        {
            // Skip any pages (either expert, or on skip list)
            while (PageShouldBeSkipped(PageIndex))
            {
                // Skip Page  - THIS FAILS IF YOU HAVE AN EXPERT PAGE AS FIRST OR LAST IN THE COLLECTION
                if (movingForward)
                    PageIndex++;
                else
                    PageIndex--;
            }
            

            wizardPages1.SelectedIndex = PageIndex;

            // This can be cancelled as and when data is validated, so don't show nav buttons here, do it in selected event
        }
        private void wizardPages1_Selected(object sender, TabControlEventArgs e)
        {
            ShowNavButtons();

            if (e.TabPageIndex == 3)
            {
                ShownTab3();
            }
            else if (e.TabPageIndex == 6)
            {
                ShownTab6();
            }
        }
        void ShowNavButtons()
        {
            btnPreviousPage.Visible = (PageIndex > 0);
            btnNextPage.Visible = (PageIndex < (wizardPages1.TabCount - 1));
            btnClose.Visible = (!btnNextPage.Visible);
        }
        private void wizardPages1_Deselecting(object sender, TabControlCancelEventArgs e)
        {
            bool ShouldCancel = false;
            string CancelReason = "";
            switch (e.TabPageIndex)
            {
                case 0:
                    TryMoveFromStep0(ref ShouldCancel, ref CancelReason);
                    break;

                case 1:
                    TryMoveFromStep1(ref ShouldCancel, ref CancelReason);
                    break;

                case 2:
                    TryMoveFromStep2(ref ShouldCancel, ref CancelReason);
                    break;
                case 3:
                    TryMoveFromStep3(ref ShouldCancel, ref CancelReason);
                    break;
                case 4:
                    TryMoveFromStep4(ref ShouldCancel, ref CancelReason);
                    break;
                case 5:
                    TryMoveFromStep5(ref ShouldCancel, ref CancelReason);
                    break;

            }

            e.Cancel = ShouldCancel;

            if (e.Cancel)
            {
                string strDisplayReason = (string.IsNullOrWhiteSpace(CancelReason)) ? "A value was incorrect, or left blank.\r\n\r\nPlease check all the values you have entered on this page and try again." : CancelReason;
                RPMessageBox.ShowAlert(strDisplayReason);

                // Re-adjust page index
                if (movingForward)
                    PageIndex--;
                else
                    PageIndex++;


            }
        }
        #endregion

        private void btnClose_Click(object sender, EventArgs e)
        {

            if (RPMessageBox.ShowQuestion("Remote Potato for iOS allows you to stream all your music, pictures and video directly to your iPhone or iPad.\r\n\r\nWould you like to read more about the app now?","Remote Potato for iPhone/iPad") == System.Windows.Forms.DialogResult.Yes)
                ShowIOSAppWebPage();

            this.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.Close();

            

        }
        void ShowIOSAppWebPage()
        {
            string target = "http://www.remotepotato.com/ios.aspx";
            System.Diagnostics.Process.Start(target);
        }


       






    }
}