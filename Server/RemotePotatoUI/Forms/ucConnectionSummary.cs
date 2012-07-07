using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using RemotePotatoServer.Properties;

namespace RemotePotatoServer
{
    public partial class ucConnectionSummary : UserControl
    {
        bool isInitialised = false;
        string demo_address = "";
        string demo_port = "";
        string demo_type = "";
        string LAN_address = "";
        string LAN_port = "";
        string WAN_address = "";
        string WAN_port = "";

        public ucConnectionSummary()
        {
            InitializeComponent();

        }

        bool runTestPortServer = false;
        public void Init(bool _runTestPortServer)
        {
            runTestPortServer = _runTestPortServer;

            // LAN
            Network.IPHelper helper = new Network.IPHelper();
            LAN_address = helper.GetLocalIP();
            LAN_port = Settings.Default.Port.ToString();
            WAN_port = LAN_port;

            
            if (! Settings.Default.DynamicDNSServiceUsed)
            {
                //  We need the external IP
                helper.QueryExternalIPAsync_Completed += new EventHandler<Network.IPHelper.GetExternalIPEventArgs>(helper_QueryExternalIPAsync_Completed);
                helper.QueryExternalIPAsync();
            }
            else
            {
                // External address is Dynamic hostname
                WAN_address = Settings.Default.DynamicDNSHostname;
                WAN_port = Settings.Default.Port.ToString();
                Complete_Init();   
            }
        }
        void selectDemo(bool useWAN)
        {
            if (useWAN)
            {
                demo_address = WAN_address;
                demo_port = WAN_port;
                demo_type = "WAN";
            }
            else
            {
                demo_address = LAN_address;
                demo_port = LAN_port;
                demo_type = "LAN";
            }
        }
        
        void helper_QueryExternalIPAsync_Completed(object sender, Network.IPHelper.GetExternalIPEventArgs e)
        {
            WAN_address = Settings.Default.LastPublicIP;
            WAN_port = Settings.Default.Port.ToString();

            Complete_Init();
        }
        void Complete_Init()
        {
            // Default WAN settings
            selectDemo(true);

            populateConnectionInfo();
            populateDemoBoxes();

            // THESE 3 LINES ARE PART OF THE TEMPORARY DISABLE OF PORT CHECKING
            pbCheckNet.Visible = false;
            lblCheckNet.Text = "";
            isInitialised = true;
            /* TEMPORARY REMOVED DUE TO ERRORS 
            Network.PortChecker checker = new Network.PortChecker();
            
            pbCheckNet.Visible = true;
            pbCheckNet.Value = 100;
            lblCheckNet.Text = "Checking ports...";
            lblCheckNet.ForeColor = Color.DarkBlue;

            checker.CheckPortOpenAsync_Completed += new EventHandler<Network.PortChecker.CheckPortCompletedEventArgs>(checker_CheckPortOpenAsync_Completed);
            checker.CheckPortOpenAsync((Convert.ToInt32(Settings.Default.Port)), runTestPortServer);
             */
        }

        delegate void CheckPortOpenCompleted(Network.PortChecker.CheckPortCompletedEventArgs args);
        void checker_CheckPortOpenAsync_Completed(object sender, Network.PortChecker.CheckPortCompletedEventArgs e)
        {
            CheckPortOpenCompleted d = new CheckPortOpenCompleted(UnsafeCheckPortOpenCompleted);
            this.Invoke(d, e);
        }
        void UnsafeCheckPortOpenCompleted(Network.PortChecker.CheckPortCompletedEventArgs e)
        {
            pbCheckNet.Visible = false;

            if (!e.DidComplete)
            {
                lblCheckNet.Text = "Could not check whether Remote Potato can be accessed over the Internet";
                lblCheckNet.ForeColor = Color.Maroon;
            }
            else if (e.PortOpen)
            {
                lblCheckNet.Text = "Port is open - Remote Potato can be accessed over the Internet";
                lblCheckNet.ForeColor = Color.DarkGreen;
            }
            else
            {
                lblCheckNet.Text = "Port is not open - Remote Potato cannot be accessed over the Internet";
                lblCheckNet.ForeColor = Color.Maroon;
            }

            isInitialised = true;
        }

        void populateConnectionInfo()
        {
            lblLANSettings.Text = lblLANSettings.Text.Replace("**LAN-ADDRESS**", LAN_address);
            lblLANSettings.Text = lblLANSettings.Text.Replace("**LAN-PORT**", LAN_port);
            lblWANSettings.Text = lblWANSettings.Text.Replace("**WAN-ADDRESS**", WAN_address);
            lblWANSettings.Text = lblWANSettings.Text.Replace("**WAN-PORT**", WAN_port);
        }

        void populateDemoBoxes()
        {
            if (demo_type == "LAN")
            {
                lblDemoType1.Text = "(local network)";
                lblDemoType2.Text = "(local network)";
            }
            else if (demo_type == "WAN")
            {
                lblDemoType1.Text = "(over the Internet)";
                lblDemoType2.Text = "(over the Internet)";
            }

            lblBrowserURLandPort.Text = "http://" + demo_address + ":" + demo_port;
                lblAppURL.Text = demo_address;
                lblAppPort.Text = demo_port;
        }

        private void btnShowLANSettings_Click(object sender, EventArgs e)
        {
            if (!isInitialised) return;
            selectDemo(false);
            populateDemoBoxes();
        }

        private void btnShowWANSettings_Click(object sender, EventArgs e)
        {
            if (!isInitialised) return;
            selectDemo(true);
            populateDemoBoxes();
        }

        private void lblConnectWANDetails_Click(object sender, EventArgs e)
        {

        }

        private void llAddFirewall_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (RPMessageBox.ShowQuestion("Most connection problems can be solved by either changing the settings on either your router or Windows Firewall.\r\n\r\nWould you like to read more about this online now?", "Solve Connection Problems") == DialogResult.No)
                return;

            string target = "http://forums.fatattitude.com/viewforum.php?f=12";
            System.Diagnostics.Process.Start(target);
        }
    }
}
