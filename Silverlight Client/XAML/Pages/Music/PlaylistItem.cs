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
using CommonEPG;

namespace SilverPotato
{
    public class PlaylistItem
    {
        public RPMusicSong Song;
        bool isCurrent;
        public event EventHandler IsCurrentChanged;


        public PlaylistItem(RPMusicSong _song)
        {
            Song = _song;
        }




        // Properties
        public bool IsCurrent
        {
            get
            {
                return isCurrent;
            }
            set
            {
                isCurrent = value;
                if (IsCurrentChanged != null)
                    IsCurrentChanged(this, new EventArgs());
            }
        }
    }
}
