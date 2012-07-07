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
    public partial class ProgressWaiter : UserControl
    {
        public ProgressWaiter()
        {
            InitializeComponent();

            StartAnimation();
        }
        public ProgressWaiter(string lblMessage) : this()
        {
            
            lblLine1.Text = lblMessage;
            lblLine2.Text = "";
        }


        public void SetText(string txt1, string txt2)
        {
            if (!String.IsNullOrEmpty(txt1))
                lblLine1.Text = txt1; 

            if (!String.IsNullOrEmpty(txt2))
                lblLine2.Text = txt2;
        }


        void StartAnimation()
        {

            Duration duration = new Duration(TimeSpan.FromSeconds(2.4));
            Storyboard sb = new Storyboard();
            sb.Duration = duration;

            DoubleAnimation dX = new DoubleAnimation();

            dX.Duration = duration;

            //dX.RepeatBehavior = RepeatBehavior.Forever;
            sb.Children.Add(dX);
            Storyboard.SetTarget(dX, rtSpinEllipse);
            Storyboard.SetTargetProperty(dX, new PropertyPath("(Angle)"));

            dX.From = 0;
            dX.To = 360;
            sb.RepeatBehavior = RepeatBehavior.Forever;
            sb.Begin();
        }

    }
}
