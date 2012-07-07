using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using CommonEPG;

namespace SilverPotato
{
    public class AlbumChooserStrip : ChooserStrip
    {
        public enum AlbumChooserSourceTypes
        {
            None,
            Artist,
            Genre
        }
        AlbumChooserSourceTypes SourceType;
        RPMusicCollection Source;

        public AlbumChooserStrip(AlbumChooserSourceTypes sourceType, RPMusicCollection source)
        {
            SourceType = sourceType;
            Source = source;

            // Initial appearance is thumbnails
            InitialiseContentPane(ClickItemsPane.ClickItemsPaneItemLayouts.Thumbnails);
            contentPane.ItemClicked += new EventHandler(contentPane_ItemClicked);
            SetWidthTo(530);

            PopulateStripWithAlbums();
        }



        public void PopulateStripWithAlbums()
        {
            Dictionary<string, List<RPMusicAlbum>> albums;
            switch (SourceType)
            {
                case AlbumChooserSourceTypes.Artist:
                    RPMusicArtist artist = (RPMusicArtist)Source;
                    lblStripTitle.Text = "Albums by " + artist.Name;
                    albums = MusicManager.AlbumsForArtist(artist.ID, true);
                    break;

                case AlbumChooserSourceTypes.Genre:
                    RPMusicGenre genre = (RPMusicGenre)Source;
                    lblStripTitle.Text = genre.Name + " Albums";
                    albums = MusicManager.AlbumsForGenre(genre, true);
                    break;

                default:
                    lblStripTitle.Text = "All Albums";
                    albums = MusicManager.AllAlbumsGroupedByAlpha();
                    break;
            }
            
            int counter = 0;
            foreach (KeyValuePair<string, List<RPMusicAlbum>> kvp in albums)
            {
                List<ClickItem> clickItems = new List<ClickItem>();
                foreach (RPMusicAlbum ar in kvp.Value)
                {
                    MusicAlbumClickItem.RPMusicAlbumClickItemTextFormat TextFormat;

                    // Album title text format
                    if (SourceType == AlbumChooserSourceTypes.None) // All albums
                        TextFormat = MusicAlbumClickItem.RPMusicAlbumClickItemTextFormat.TitleAndArtist;
                    else if (SourceType == AlbumChooserSourceTypes.Artist)
                        TextFormat = MusicAlbumClickItem.RPMusicAlbumClickItemTextFormat.TitleOnly;
                    else
                        TextFormat = MusicAlbumClickItem.RPMusicAlbumClickItemTextFormat.TitleAndArtist;

                    // Use the base class
                    ClickItem ci = new MusicAlbumClickItem(counter++, ar, TextFormat, ClickItem.ClickItemLayouts.ThumbnailWithOverlay, 90);
                    clickItems.Add(ci);
                }

                // Add into current grouped items
                CurrentGroupedItems.Add(kvp.Key, clickItems);
            }

            RefreshContentPane();
        }



        // ITEMS CLICKED
        public event EventHandler<GenericEventArgs<object>> ItemClicked;
        void contentPane_ItemClicked(object sender, EventArgs e)
        {
            if (!(sender is MusicAlbumClickItem)) return;
            MusicAlbumClickItem maci = (MusicAlbumClickItem)sender;
            ItemClicked(new object(), new GenericEventArgs<object>(maci.LinkedDataItem));
        }

    }



}
