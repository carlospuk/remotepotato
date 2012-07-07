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
    public partial class StatusBar : UserControl
    {
        public StatusBar()
        {
            InitializeComponent();

            RenderVersionString();
            VisualManager.InitialiseTaskBar(gdTaskBarContainer);
        }

        public void RenderVersionString()
        {
            // Show version
            string strServerVersion = ((NetworkManager.ServerVersion.Major == 0) && (NetworkManager.ServerVersion.Minor == 0)) ?
                "" : "Server v" + NetworkManager.ServerVersion.ToString(2);

            lblFooterText.Text = "Remote Potato " + strServerVersion + " ©2010 FatAttitude By C Partridge";
        }


        private void btnViewLog_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            VisualManager.ShowDebugLog();
        }


    }
}
