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
    public partial class TweenPage : UserControl
    {
        public TweenPage()
        {
            InitializeComponent();
        }

        public event EventHandler Clicked;
        private void LayoutRoot_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Clicked(this, new EventArgs());
        }
    }
}
