using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace RemotePotatoServer
{
    public partial class frmPleaseWait : Form
    {
        public frmPleaseWait(string txtActivity)
        {
            InitializeComponent();

            setActivity(txtActivity);
        }


        public void setActivity(string txtLabel)
        {
            lblActivity.Text = txtLabel;
        }
        public void setProgress(int progress)
        {
            pbProgress.Value = progress;
            pbProgress.Refresh();
            Application.DoEvents();
        }
        public void CloseMe()
        {
            this.Close();
        }

        private void frmPleaseWait_Load(object sender, EventArgs e)
        {
            this.CenterToParent();
        }

    }
}
