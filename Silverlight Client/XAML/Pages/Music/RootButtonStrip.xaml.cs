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


    public partial class RootButtonStrip : UserControl
    {
        public RootButtonStrip()
        {
            InitializeComponent();
        }

        public event EventHandler<GenericEventArgs<RootMusicButtonTypes>> RootButtonClicked;


        private void btnRootDisplayArtists_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (RootButtonClicked != null)
                RootButtonClicked(this, new GenericEventArgs<RootMusicButtonTypes>(RootMusicButtonTypes.AllArtists));
        }

        private void btnRootDisplayAlbumArtists_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (RootButtonClicked != null)
                RootButtonClicked(this, new GenericEventArgs<RootMusicButtonTypes>(RootMusicButtonTypes.AllAlbumArtists));
        }

        private void btnRootDisplayAlbums_MouseLeftButtonUp_1(object sender, MouseButtonEventArgs e)
        {
            if (RootButtonClicked != null)
                RootButtonClicked(this, new GenericEventArgs<RootMusicButtonTypes>(RootMusicButtonTypes.AllAlbums));
        }

        private void btnRootDisplayGenres_MouseLeftButtonUp_1(object sender, MouseButtonEventArgs e)
        {
            if (RootButtonClicked != null)
                RootButtonClicked(this, new GenericEventArgs<RootMusicButtonTypes>(RootMusicButtonTypes.AllGenres));
        }

        private void btnRootDisplaySongs_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (RootButtonClicked != null)
                RootButtonClicked(this, new GenericEventArgs<RootMusicButtonTypes>(RootMusicButtonTypes.AllSongs));
        }



        #region MouseOvers

        private void Border_MouseEnter(object sender, MouseEventArgs e)
        {
            Border brd = (Border)sender;
            brd.BorderBrush = new SolidColorBrush(Colors.Yellow);
        }
        private void Border_MouseLeave(object sender, MouseEventArgs e)
        {
            Border brd = (Border)sender;
            brd.BorderBrush = new SolidColorBrush(Colors.White);
        }

        #endregion

 


    }

    public enum RootMusicButtonTypes
    {
        AllAlbums,
        AllArtists,
        AllAlbumArtists,
        AllGenres,
        AllSongs
    }


}
