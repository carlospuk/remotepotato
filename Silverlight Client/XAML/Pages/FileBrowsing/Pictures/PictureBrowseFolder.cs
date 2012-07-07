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
    public class PictureBrowseFolder : FileBrowseFolder
    {

        public PictureBrowseFolder(FileBrowseRoot _rootController, FileBrowseRequest fbRequest, ClickItemsPane.ClickItemsPaneItemLayouts paneLayout)
            : base(_rootController, fbRequest, paneLayout)
        {

        }

        // Setup
        public override void SetTitle()
        {
            if (LinkedBrowseRequest.FullPath == "PICTURES_LIBRARY")
            {
                lblPageTitle.Text = @"Picture Library";
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
            filters.Add("*.bmp");
            filters.Add("*.gif");
            filters.Add("*.jpg");
            filters.Add("*.jpeg");
            filters.Add("*.tiff");
            filters.Add("*.tif");
            filters.Add("*.png");

            fbRequest.Filters = filters;
        }

        // Click Items
        public override List<ClickItem> CreateClickItemsFromBrowseResultFiles(ClickItem.ClickItemLayouts itemLayout)
        {
            List<ClickItem> fileItems = new List<ClickItem>();
            int iCounter = 0;
            foreach (BrowseItem fileItem in LinkedBrowseResult.Files)
            {
                RPPictureItem picItem = pictureItemFromBrowseItem(fileItem);

                // Important line - use the PARENT BASE CLASS VARIABLE to store the derived class, so we can pass it to the ClickItemsPane
                ClickItem ci = new RPPictureItemClickItem(iCounter++, picItem, RPPictureItemClickItem.RPPictureClickItemTextFormat.TitleOnly, itemLayout);
                fileItems.Add(ci);
            }
            return fileItems;
        }
        public override void ItemClicked(ClickItem ci)
        {
            if (ci is RPPictureItemClickItem)
            {
                RPPictureItemClickItem pici = (RPPictureItemClickItem)ci;

                // Convert values to list
                List<RPPictureItem> lstTemp = new List<RPPictureItem>();

                foreach (BrowseItem fileItem in LinkedBrowseResult.Files)
                {
                    // Important line - use the PARENT BASE CLASS VARIABLE to store the derived class, so we can pass it to the ClickItemsPane
                    RPPictureItem picItem = pictureItemFromBrowseItem(fileItem);

                    lstTemp.Add(picItem);
                }


                VisualManager.ShowPictureViewer(lstTemp, pici.Index, pici.imgThumbnail.Source);


                // Load pics view
                return;
            }
        }
        public override void FolderClicked(ClickItem ci)
        {
            if (ci is FolderClickItem)
            {
                // Create a new pictures folder 
                FolderClickItem fci = (FolderClickItem)ci;

                BrowseItem folderItem = fci.LinkedBrowseItem;
                if (folderItem == null) return;

                string folderPath = folderItem.Name;
                if (string.IsNullOrEmpty(folderPath)) return;

                FileBrowseRequest newRequest = new FileBrowseRequest();
                newRequest.FullPath = folderPath;

                // Push the child folder onto the parent stack
                PictureBrowseFolder childFolder = new PictureBrowseFolder(rootController, newRequest, contentPane.DisplayedItemsLayout); // child matches parent layout, e.g. just text/thumbnails/etc
                rootController.PushFolder(childFolder);
                return;
            }
        }



        RPPictureItem pictureItemFromBrowseItem(BrowseItem item)
        {
            RPPictureItem pi = new RPPictureItem();
            pi.FileName = item.Name;

            pi.Title = Functions.finalPathComponentOfString(item.Name);
            pi.ID = pi.Title;
            return pi;
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
