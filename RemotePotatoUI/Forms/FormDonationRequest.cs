using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using RemotePotatoServer.Properties;

namespace RemotePotatoServer
{
    public partial class FormDonationRequest : Form
    {
        public FormDonationRequest()
        {
            InitializeComponent();
        }

        private void btnNotNow_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.Ignore;
        }

        private void btnAlreadyDonated_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.Yes;
        }

        private void btnWantToDonate_Click(object sender, EventArgs e)
        {
            GoToDonateWebPage();
            this.DialogResult = System.Windows.Forms.DialogResult.Yes;
        }
        void GoToDonateWebPage()
        {
            string target = "http://www.remotepotato.com/donate.aspx";
            System.Diagnostics.Process.Start(target);
        }
    }
}
