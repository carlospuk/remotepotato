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
using System.IO;
using CommonEPG;

namespace SilverPotato
{
    public class MoviesBrowseFolder : FileBrowseFolder
    {

        public MoviesBrowseFolder(FileBrowseRoot _rootController, FileBrowseRequest fbRequest, ClickItemsPane.ClickItemsPaneItemLayouts paneLayout)
            : base(_rootController, fbRequest, paneLayout)
        {

        }

        // Setup
        public override void SetTitle()
        {
            if (LinkedBrowseRequest.FullPath == "MOVIE_LIBRARY")
            {
                lblPageTitle.Text = @"Movies";
                btnFolderUp.Visibility = System.Windows.Visibility.Collapsed;
            }
            else
            {
                lblPageTitle.Text = Functions.finalPathComponentOfString(LinkedBrowseRequest.FullPath);
            }
        }
        public override void AddFilters(FileBrowseRequest fbRequest)
        {
            List<string> filters = new List<string>();
          /*  filters.Add("*.avi");
            filters.Add("*.mpeg");
            filters.Add("*.flv");
            filters.Add("*.mov");
            filters.Add("*.mp4");
            filters.Add("*.wmv");
            filters.Add("*.m4v");
            filters.Add("*.3gp");
            filters.Add("*.m2ts");
            filters.Add("*.mpg");
            filters.Add("*.wtv");
            filters.Add("*.dvr-ms"); 
           
           AUTOMATICALLY DONE ON SERVER
           
           */

            fbRequest.Filters = filters;
        }




        // Click Items
        public override List<ClickItem> CreateClickItemsFromBrowseResultFiles(ClickItem.ClickItemLayouts itemLayout)
        {
            List<ClickItem> fileItems = new List<ClickItem>();
            foreach (BrowseItem fileItem in LinkedBrowseResult.Files)
            {
                TVProgramme tvp = tvProgFromBrowseItem(fileItem);

                // Use the PARENT BASE CLASS VARIABLE to store the derived class, so we can pass it to the ClickItemsPane
                ClickItem ci = new TVProgClickItem(tvp, TVProgClickItem.TVProgClickItemTextFormat.TitleOnly, ClickItem.ClickItemLayouts.ThumbnailWithOverlay);
                fileItems.Add(ci);
            }
            return fileItems;
        }
        public override void ItemClicked(ClickItem ci)
        {
            if (ci is TVProgClickItem)
            {
                TVProgClickItem tvpci = (TVProgClickItem)ci;

                VisualManager.ShowStreamingVideo(tvpci.LinkedTVProgramme, TimeSpan.FromSeconds(0));
                return;
            }
        }
        public override void FolderClicked(ClickItem ci)
        {
            // NO folders
        }

        // Helper - create a TV programme from a video file browse item
        TVProgramme tvProgFromBrowseItem(BrowseItem item)
        {
            TVProgramme tvp = new TVProgramme();
            tvp.IsNotDTV = true;  // IMPORTANT - flag as not DTV  (streamer won't try to deinterlace etc)
            tvp.Description = "Video file located on server at file path: " + item.Name;  // NB: the phrase 'Video file' is referred to in StreamingVideoPage.xaml.cs, so maintain consistency
            tvp.isGeneratedFromFile = true;
            tvp.Title = System.IO.Path.GetFileNameWithoutExtension( Functions.finalPathComponentOfString(item.Name) );
            tvp.Filename = item.Name;
            tvp.WTVCallsign = "No Channel";
            

            
            tvp.StartTime = DateTime.Now.Ticks;
            if (item.Duration > 0)
                tvp.StopTime = DateTime.Now.AddSeconds(item.Duration).Ticks;
            else // Use 2 hours
                tvp.StopTime = DateTime.Now.AddMinutes(120).Ticks;

            return tvp;
        }



        #region Buttons - Folder Up (Must be here in derived class to avoid XAML parser error)
        private void btnFolderUp_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            base.EventMouseLeftButtonUp();
        }
        private void ControlButton_MouseEnter(object sender, MouseEventArgs e)
        {
            base.EventControlButtonMouseEnter(sender, e);
        }
        private void ControlButton_MouseLeave(object sender, MouseEventArgs e)
        {
            base.EventControlButtonMouseLeave(sender, e);
        }
        private void btnShowEmptyFolders_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            base.EventButtonShowEmptyFilesMouseLeftButtonUp(sender, e);
            
        }
        #endregion

    }

}
