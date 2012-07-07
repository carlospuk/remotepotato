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
    public partial class LabelButton : UserControl
    {
        public event EventHandler Click;
        public string DisplayText
        {
            get
            {
                return (string)GetValue(DisplayTextProperty);
            }
            set
            {
                SetValue(DisplayTextProperty, value);
            }
        }
        public string IconSource
        {
            get
            {
                return (string)GetValue(IconSourceProperty);
            }
            set
            {
                SetValue(IconSourceProperty, value);
            }
        }

        public static readonly DependencyProperty DisplayTextProperty = 
            DependencyProperty.Register("DisplayText", typeof(string), typeof(LabelButton), new PropertyMetadata("", onDisplayTextChanged));
        public static readonly DependencyProperty IconSourceProperty =
            DependencyProperty.Register("IconSource", typeof(string), typeof(LabelButton), new PropertyMetadata("", onIconSourceChanged));


        private static void onDisplayTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            LabelButton lb = d as LabelButton;
            lb.lblText.Text = (string)e.NewValue;
        }
        private static void onIconSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            LabelButton lb = d as LabelButton;
            string newSource = (string)e.NewValue;
            if (string.IsNullOrEmpty(newSource))
                lb.imgButton.Source = ImageManager.LoadImageFromContentPath("/Images/btnSideDefault.png");
            else
                lb.imgButton.Source = ImageManager.LoadImageFromContentPath(newSource);
        }

        public LabelButton()
        {
            InitializeComponent();
        }
        public LabelButton(string txtContents) : this()
        {
            lblText.Text = txtContents;
        }

        private void Label_MouseEnter(object sender, MouseEventArgs e)
        {
            lblText.Foreground = new SolidColorBrush(Colors.White);
            imgButton.Opacity = 1.0;
        }

        private void Label_MouseLeave(object sender, MouseEventArgs e)
        {
            lblText.Foreground = new SolidColorBrush(Functions.HexColor("#FFCCCCCC"));
            imgButton.Opacity = 0.8;
        }

        private void Label_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (Click != null)
                Click(this, new EventArgs());
        }
    }
}
