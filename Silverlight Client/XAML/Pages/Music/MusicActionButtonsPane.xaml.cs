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
    public partial class MusicActionButtonsPane : UserControl
    {
        public MusicActionButtonsPane()
        {
            InitializeComponent();
            
            WireUpEvents();
        }

        void WireUpEvents()
        {
            lbAddToNowPlaying.Click += new EventHandler(lbAddToNowPlaying_Click);
            lbDownload.Click += new EventHandler(lbDownload_Click);
            lbPlayNow.Click += new EventHandler(lbPlayNow_Click);
        }


        public event EventHandler<MusicActionButtonClickedEventArgs> ButtonClicked;
        void lbPlayNow_Click(object sender, EventArgs e)
        {
            if (ButtonClicked != null)
                ButtonClicked(this, new MusicActionButtonClickedEventArgs(MusicActionButtonTypes.PlayNow));
        }
        void lbDownload_Click(object sender, EventArgs e)
        {
            if (ButtonClicked != null)
                ButtonClicked(this, new MusicActionButtonClickedEventArgs(MusicActionButtonTypes.Download));
        }
        void lbAddToNowPlaying_Click(object sender, EventArgs e)
        {
            if (ButtonClicked != null)
                ButtonClicked(this, new MusicActionButtonClickedEventArgs(MusicActionButtonTypes.AddToNowPlaying));
        }
    }


    // Event Args
    public enum MusicActionButtonTypes
    {
        PlayNow,
        AddToNowPlaying,
        Download
    }

    public class MusicActionButtonClickedEventArgs : EventArgs
    {
        public MusicActionButtonTypes ButtonType { get; set; }

        public MusicActionButtonClickedEventArgs(MusicActionButtonTypes _buttonType)
        {
            ButtonType = _buttonType;
        }
    }

}
