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
    public partial class MusicBrowser : UserControl
    {
        RootButtonStrip RootStrip;
        Chooser MusicChooser;

        public MusicBrowser()
        {
            InitializeComponent();


            MusicManager.WebGetMusicFramework_Completed += new EventHandler(MusicManager_WebGetMusicFramework_Completed);
            if ((!MusicManager.GettingMusicFramework) && (!MusicManager.GotMusicFramework))
            {
                VisualManager.ShowActivityWithinGrid(gdSpinner, 3.0);
                MusicManager.WebGetMusicFramework();
            }
            else
            {
                EnableInterface();
            }
        }

        void MusicManager_WebGetMusicFramework_Completed(object sender, EventArgs e)
        {
            VisualManager.HideActivityWithinGrid(gdSpinner);
            EnableInterface();

        }

        void EnableInterface()
        {
            gdMessage.Visibility = Visibility.Collapsed;

            if ((RootStrip != null) || (MusicChooser != null))
            {
                gdRootStrip.Children.Clear();
                gdChooser.Children.Clear();
                RootStrip = null;
                MusicChooser = null;
            }

            RootStrip = new RootButtonStrip();
            RootStrip.RootButtonClicked += new EventHandler<GenericEventArgs<RootMusicButtonTypes>>(RootStrip_RootButtonClicked);
            gdRootStrip.Children.Add(RootStrip);
            
            // enable buttons if desired to implement
            MusicChooser = new Chooser();
            gdChooser.Children.Add(MusicChooser);
        }

        



      

        // Root Buttons
        void RootStrip_RootButtonClicked(object sender, GenericEventArgs<RootMusicButtonTypes> e)
        {
            switch (e.Value)
            {
                case RootMusicButtonTypes.AllAlbums:
                    AlbumChooserStrip csAlbums = new AlbumChooserStrip(AlbumChooserStrip.AlbumChooserSourceTypes.None, null);
                    csAlbums.ItemClicked += new EventHandler<GenericEventArgs<object>>(csAlbums_ItemClicked);
                    MusicChooser.ClearAllStrips();
                    MusicChooser.AddStrip(csAlbums);
                    break;

                case RootMusicButtonTypes.AllArtists:
                    ArtistChooserStrip csArtists = new ArtistChooserStrip(ArtistChooserStripFilterTypes.AllArtists);
                    csArtists.ItemClicked += new EventHandler<GenericEventArgs<object>>(csArtists_ItemClicked);
                    MusicChooser.ClearAllStrips();
                    MusicChooser.AddStrip(csArtists);
                    break;

                case RootMusicButtonTypes.AllAlbumArtists:
                    ArtistChooserStrip csAlbumArtists = new ArtistChooserStrip(ArtistChooserStripFilterTypes.AlbumArtists);
                    csAlbumArtists.ItemClicked += new EventHandler<GenericEventArgs<object>>(csArtists_ItemClicked);
                    MusicChooser.ClearAllStrips();
                    MusicChooser.AddStrip(csAlbumArtists);
                    break;

                case RootMusicButtonTypes.AllGenres:
                    GenreChooserStrip csGenres = new GenreChooserStrip();
                    csGenres.ItemClicked += new EventHandler<GenericEventArgs<object>>(csGenres_ItemClicked);
                    MusicChooser.ClearAllStrips();
                    MusicChooser.AddStrip(csGenres);
                    break;

                case RootMusicButtonTypes.AllSongs:

                    if ( (Settings.WarnOnMusicImportDelay)  && (! Settings.WarnedOnMusicImportDelay))
                    {
                        VisualManager.questionBox.Closed += new EventHandler(AskWhetherToImportAllSongs_QuestionBoxClosed);
                        VisualManager.ShowQuestionBox("Load All Songs", "This will retrieve all songs from your music library, which could take some time.\r\n(~10 seconds per 4000 songs)\r\n\r\nAre you sure you wish to continue?");

                        Settings.WarnedOnMusicImportDelay = true;
                        
                    }
                    else
                        ImportAllSongs();
                    break;
            }
        }
         void AskWhetherToImportAllSongs_QuestionBoxClosed(object sender, EventArgs e)
        {
            VisualManager.questionBox.Closed -= new EventHandler(AskWhetherToImportAllSongs_QuestionBoxClosed);
            if (VisualManager.QuestionBoxDialogResult)
            {
                ImportAllSongs();
            }
        }
         void ImportAllSongs()
         {
             SongsChooserStrip csAllSongs = new SongsChooserStrip(SongsChooserStrip.SongChooserSourceTypes.AllSongs, null);
             csAllSongs.ItemClicked += new EventHandler<GenericEventArgs<object>>(csSongs_ItemClicked);
             MusicChooser.ClearAllStrips();
             MusicChooser.AddStrip(csAllSongs);
         }




        // Data drill-down
        void csArtists_ItemClicked(object sender, GenericEventArgs<object> e)
        {
            RPMusicArtist artist = (RPMusicArtist)e.Value;

            // Any albums?
            if (artist.Albums().Count > 0)
            {
                AlbumChooserStrip csAlbumsForArtist = new AlbumChooserStrip(AlbumChooserStrip.AlbumChooserSourceTypes.Artist, artist);
                csAlbumsForArtist.ItemClicked += new EventHandler<GenericEventArgs<object>>(csAlbums_ItemClicked);
                MusicChooser.AddStrip(csAlbumsForArtist);
            }
            else // No, skip to songs
            {
                SongsChooserStrip csSongsForArtist = new SongsChooserStrip(SongsChooserStrip.SongChooserSourceTypes.Artist, artist);
                csSongsForArtist.ItemClicked += new EventHandler<GenericEventArgs<object>>(csSongs_ItemClicked);
                MusicChooser.AddStrip(csSongsForArtist);
            }
        }
        void csAlbums_ItemClicked(object sender, GenericEventArgs<object> e)
        {
            // An album was clicked
            RPMusicAlbum album = (RPMusicAlbum)e.Value;
            SongsChooserStrip csSongsForAlbum;

            // Was it a special 'album' for an artist?
            if (album.ID == "[ALL_SONGS_BY_ARTIST]")
                csSongsForAlbum = new SongsChooserStrip(SongsChooserStrip.SongChooserSourceTypes.Artist, album.Artist() );
            else if (album.ID == "[ALL_SONGS_BY_GENRE]")
                csSongsForAlbum = new SongsChooserStrip(SongsChooserStrip.SongChooserSourceTypes.Genre, album.Genre());
            else
                csSongsForAlbum = new SongsChooserStrip(SongsChooserStrip.SongChooserSourceTypes.Album, album);

            csSongsForAlbum.ItemClicked += new EventHandler<GenericEventArgs<object>>(csSongs_ItemClicked);
            MusicChooser.AddStrip(csSongsForAlbum);
        }
        void csGenres_ItemClicked(object sender, GenericEventArgs<object> e)
        {
            RPMusicGenre genre = (RPMusicGenre)e.Value;
            AlbumChooserStrip csAlbumsForGenre = new AlbumChooserStrip(AlbumChooserStrip.AlbumChooserSourceTypes.Genre, genre);
            csAlbumsForGenre.ItemClicked += new EventHandler<GenericEventArgs<object>>(csAlbums_ItemClicked);
            MusicChooser.AddStrip(csAlbumsForGenre);
        }
        void csSongs_ItemClicked(object sender, GenericEventArgs<object> e)
        {
            RPMusicSong song = (RPMusicSong)e.Value;
            SongInfoStrip sInfoStrip = new SongInfoStrip(song);

            MusicChooser.AddStrip(sInfoStrip);
        }




    }
}
