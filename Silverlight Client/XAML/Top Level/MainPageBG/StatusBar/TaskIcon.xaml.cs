using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace SilverPotato
{
    public partial class TaskIcon : UserControl
    {
        public event EventHandler Clicked;
        
        public TaskIcon()
        {
            InitializeComponent();
        }

        public void SetIcon(BitmapImage newIcon)
        {
            imgIcon.Source = newIcon;
        }

        #region MouseOver / Click
        private void Border_MouseEnter(object sender, MouseEventArgs e)
        {
            brdMain.Background = new SolidColorBrush(Functions.HexColor("#FFFFFFFF"));
            
        }
        private void Border_MouseLeave(object sender, MouseEventArgs e)
        {
            brdMain.Background = new SolidColorBrush(Functions.HexColor("#DDFFFFFF"));
        }
        private void Border_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (Clicked != null) Clicked(this, new EventArgs());
        }
        #endregion
    }
}
