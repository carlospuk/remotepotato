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
    public partial class FileBrowseFolder : UserControl
    {
        // These are protected so derived classes can access them
        protected ClickItemsPane contentPane;
        protected FileBrowseRequest LinkedBrowseRequest;
        protected FileBrowseResult LinkedBrowseResult;
        protected FileBrowseRoot rootController;

        public FileBrowseFolder()
        {
            InitializeComponent();
        }
        public FileBrowseFolder(FileBrowseRoot _rootController, FileBrowseRequest fbRequest, ClickItemsPane.ClickItemsPaneItemLayouts paneLayout) : this()
        {
            LinkedBrowseRequest = fbRequest;

            SetTitle();

            rootController = _rootController;
            InitialiseContentPane(paneLayout);
            
            AddFilters(LinkedBrowseRequest);

            DoBrowseRequestOnServer();
        }
        public virtual void SetTitle()
        {
            // overridden   
        }
        public virtual void AddFilters(FileBrowseRequest fbRequest)
        {
            // overridden
        }

        void DoBrowseRequestOnServer()
        {
            ServerFileImporter.BrowseToBrowseRequest_Completed += new EventHandler<GenericEventArgs<FileBrowseResult>>(ServerFileImporter_BrowseToBrowseRequest_Completed);
            VisualManager.ShowActivityWithinGrid(gdContent);
            ServerFileImporter.BrowseToBrowseRequest(LinkedBrowseRequest);
        }
        void ServerFileImporter_BrowseToBrowseRequest_Completed(object sender, GenericEventArgs<FileBrowseResult> e)
        {
            ServerFileImporter.BrowseToBrowseRequest_Completed -= new EventHandler<GenericEventArgs<FileBrowseResult>>(ServerFileImporter_BrowseToBrowseRequest_Completed);

            VisualManager.HideActivityWithinGrid(gdContent);

            LinkedBrowseResult = e.Value;
            MakeBrowseResultItemsFullPath(LinkedBrowseResult);

            Fill();
        }

        #region Fill / Refresh Content
        void InitialiseContentPane(ClickItemsPane.ClickItemsPaneItemLayouts paneLayout)
        {
            contentPane = new ClickItemsPane(null, ClickItemsPane.ClickItemsPaneLayouts.PaneAndToolbar, paneLayout);

            contentPane.ShowHeaders = false;

            contentPane.ItemClicked += new EventHandler(contentPane_ItemClicked);
            contentPane.AwaitingRefreshedContent += new EventHandler(contentPane_RefreshClicked);
            
            gdContent.Children.Add(contentPane);
        }
        public void RefreshRecordings()
        {
            Fill();
        }
        bool hideEmptyFolders = true;
        public void Fill()
        {
            Dictionary<string, List<ClickItem>> GroupedItems = new Dictionary<string, List<ClickItem>>();

            // Set Item Layout
            ClickItem.ClickItemLayouts itemLayout;
            if (contentPane.DisplayedItemsLayout == ClickItemsPane.ClickItemsPaneItemLayouts.Thumbnails)
                itemLayout = ClickItem.ClickItemLayouts.ThumbnailWithOverlay;
            else
                itemLayout = ClickItem.ClickItemLayouts.TextOnly;

            int iCounter = 0;
            bool emptyDirectoriesFound = false;
            bool foundAnyValidDirectories = false;
            if (LinkedBrowseResult.Directories.Count > 0)
            {
                List<ClickItem> folderItems = new List<ClickItem>();
                
                foreach (BrowseItem folderItem in LinkedBrowseResult.Directories)
                {
                    // Skip empty directories
                    if ((folderItem.Items < 1) && (hideEmptyFolders))
                    {
                        emptyDirectoriesFound = true;
                        continue;
                    }

                    // Important line - use the PARENT BASE CLASS VARIABLE to store the derived class, so we can pass it to the ClickItemsPane
                    ClickItem ci = new FolderClickItem(iCounter++, folderItem, itemLayout);
                    folderItems.Add(ci);
                    foundAnyValidDirectories = true;
                }

                if (folderItems.Count > 0)
                    GroupedItems.Add("Folders", folderItems);
            }

            if (LinkedBrowseResult.Files.Count > 0)
            {
                List<ClickItem> fileItems = CreateClickItemsFromBrowseResultFiles(itemLayout);
                
                GroupedItems.Add("Files", fileItems);
            }

            if ((!foundAnyValidDirectories) && (LinkedBrowseResult.Files.Count < 1))
            {
                string strEmptyNotification = "No files or folders found.";
                if (emptyDirectoriesFound && hideEmptyFolders)
                {
                    strEmptyNotification += " (click below to view empty folders)";
                    btnShowEmptyFolders.Visibility = System.Windows.Visibility.Visible;
                }
                else
                    btnShowEmptyFolders.Visibility = System.Windows.Visibility.Collapsed;

                lblNotification.Text = strEmptyNotification;
                lblNotification.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                lblNotification.Visibility = System.Windows.Visibility.Collapsed;
            }

            // Populate content pane
            contentPane.ReplaceItemsWithNewItems(GroupedItems);

            VisualManager.HideActivityWithinGrid(LayoutRoot);
        }
        public virtual List<ClickItem> CreateClickItemsFromBrowseResultFiles(ClickItem.ClickItemLayouts itemLayout)
        {
            List<ClickItem> fileItems = new List<ClickItem>();
            return fileItems;
        }
        void MakeBrowseResultItemsFullPath(FileBrowseResult fbResult)
        {
            MakeBrowseItemsFullPath(fbResult.Directories, fbResult.BaseDirectory);
            MakeBrowseItemsFullPath(fbResult.Files, fbResult.BaseDirectory);
        }
        void MakeBrowseItemsFullPath(List<BrowseItem> items, string BaseDirectory)
        {
            foreach (BrowseItem item in items)
            {
                if (! item.Name.Contains("\\"))
                    item.Name = System.IO.Path.Combine(BaseDirectory, item.Name);
            }
        }

        void contentPane_RefreshClicked(object sender, EventArgs e)
        {
            // DO NOTHING
        }
  
        #endregion


        // Event raised by the content pane - one of its items has been clicked
        void contentPane_ItemClicked(object sender, EventArgs e)
        {
            ClickItem ci = (ClickItem)sender; // Base class
           
            FolderClicked(ci);

            // Overriden by inherited classes when items are clicked
            ItemClicked(ci);

        }
        public virtual void ItemClicked(ClickItem ci)
        {

        }
        public virtual void FolderClicked(ClickItem ci)
        {

        }
        

        #region Buttons - Folder Up
        private void btnFolderUp_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // overridden automatically by XAML inheritance: derived classes call back to the methods below
        }
        private void ControlButton_MouseEnter(object sender, MouseEventArgs e)
        {
            // overridden automatically by XAML inheritance: derived classes call back to the methods below
        }
        private void ControlButton_MouseLeave(object sender, MouseEventArgs e)
        {
            // overridden automatically by XAML inheritance: derived classes call back to the methods below
        }
        private void btnShowEmptyFolders_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // overridden automatically by XAML inheritance: derived classes call back to the methods below
        }
        protected void EventMouseLeftButtonUp()
        {
            // Go up a directory
            rootController.PopFolder();
        }
        protected void EventControlButtonMouseEnter(object sender, MouseEventArgs e)
        {
            if (!(sender is Border)) return;
            imgNavBack.Source = ImageManager.LoadImageFromContentPath("/Images/btnFolderUp_MouseOver.png");
        }
        protected void EventControlButtonMouseLeave(object sender, MouseEventArgs e)
        {
            if (!(sender is Border)) return;

            imgNavBack.Source = ImageManager.LoadImageFromContentPath("/Images/btnFolderUp.png");

        }
        protected void EventButtonShowEmptyFilesMouseLeftButtonUp(object sender, MouseEventArgs e)
        {
            hideEmptyFolders = false;
            Fill();
        }
        #endregion



  

    }
}
