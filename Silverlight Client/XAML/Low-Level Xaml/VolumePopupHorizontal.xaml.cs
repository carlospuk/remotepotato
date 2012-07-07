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
    public partial class VolumePopupHorizontal : UserControl
    {
        public event EventHandler<RoutedPropertyChangedEventArgs<double>> ValueChanged;


        public VolumePopupHorizontal()
        {
            InitializeComponent();
        }

        bool settingSlider;
        public void SetSliderValueTo(double newValue)
        {
            settingSlider = true;
            sldVolume.Value = newValue;
            settingSlider = false;
        }

        private void sldVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (settingSlider) return;

            if (e.NewValue == 0.0)
                imgCurrentVolume.Source = ImageManager.LoadImageFromContentPath("/Images/btnMainVolOff.png");
            else if (e.NewValue < 0.3)
                imgCurrentVolume.Source = ImageManager.LoadImageFromContentPath("/Images/btnMainVolLow.png");
            else if (e.NewValue < 0.7)
                imgCurrentVolume.Source = ImageManager.LoadImageFromContentPath("/Images/btnMainVolMed.png");
            else 
                imgCurrentVolume.Source = ImageManager.LoadImageFromContentPath("/Images/btnMainVolFull.png");

            if (ValueChanged != null)
                ValueChanged(this, new RoutedPropertyChangedEventArgs<double>(e.OldValue, e.NewValue));
        }



    }
}
