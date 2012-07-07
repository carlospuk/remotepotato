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
    public partial class FormConnectionInformation : Form
    {
        bool shouldRunTestServer = false;
        public FormConnectionInformation(bool _shouldRunTestServer)
        {
            InitializeComponent();

            shouldRunTestServer = _shouldRunTestServer;

            Shown += new EventHandler(FormConnectionInformation_Shown);
        }

        void FormConnectionInformation_Shown(object sender, EventArgs e)
        {
            ucConnectionSummary1.Init(shouldRunTestServer);
        }
    }
}
