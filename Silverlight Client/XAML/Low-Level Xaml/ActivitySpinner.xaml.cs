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
    public partial class ActivitySpinner : UserControl
    {
        public ActivitySpinner()
        {
            InitializeComponent();

            Loaded += new RoutedEventHandler(ActivitySpinner_Loaded);
        }
        public ActivitySpinner(double TheScale) : this()
        {
            setScale(TheScale);
        }

        void ActivitySpinner_Loaded(object sender, RoutedEventArgs e)
        {
            Duration duration = new Duration(TimeSpan.FromSeconds(2.4));
            Storyboard sb = new Storyboard();
            sb.Duration = duration;

            DoubleAnimation dX = new DoubleAnimation();

            dX.Duration = duration;

            //dX.RepeatBehavior = RepeatBehavior.Forever;
            sb.Children.Add(dX);
            Storyboard.SetTarget(dX, rotateTransform);
            Storyboard.SetTargetProperty(dX, new PropertyPath("(Angle)"));

            dX.From = 0;
            dX.To = 360;
            sb.RepeatBehavior = RepeatBehavior.Forever;
            sb.Begin();
        }


        // External calls, methods, etc...
        // SCALE // COLOUR // ETC.
        public void setOutlineColour(Color newColour)
        {
            SolidColorBrush scb = new SolidColorBrush(newColour);
            rctMainRectangle.Stroke = scb;
            elpMainCircle.Stroke = scb;
        }
        public void setSize(double newSize)
        {
            double multiplier = (newSize / Width);
            setScale(multiplier);
        }
        public void setScale(double scaleAmount)
        {
            stScaleTransform.ScaleX = scaleAmount;
            stScaleTransform.ScaleY = scaleAmount;
        }
        public void setTintColour(Color newColour)
        {
            scbTintColour.Color = newColour;
        }

        public double newSize
        {
            get { return (Width * stScaleTransform.ScaleX); }
        }
    }
}
