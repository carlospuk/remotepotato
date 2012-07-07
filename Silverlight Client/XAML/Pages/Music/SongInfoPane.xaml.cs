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
using CommonEPG;

namespace SilverPotato
{
    public partial class SongInfoPane : UserControl
    {
        RPMusicSong LinkedSong;

        public SongInfoPane(RPMusicSong _song)
        {
            InitializeComponent();

            LinkedSong = _song;
            PopulateInfo();
        }

        void PopulateInfo()
        {
            lblMediaFullTitle.Text = "Full Title:" + LinkedSong.Title;
            lblDuration.Text = "Duration: " + string.Format("{0:00}:{1:00}", LinkedSong.DurationTS().Minutes, LinkedSong.DurationTS().Seconds);
            lblUserRating.Text = "Rating: " + LinkedSong.UserRating + "/100";
        }
    }
}
