using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using CommonEPG;
using System.Xml;

namespace SilverPotato
{
    public sealed class ServerLibraryManager
    {
        // Members
        string serverRootPath;
        List<string> filters;
        public List<BrowseItem> AllFiles;

        // Constructor
        private ServerLibraryManager(string friendlyName, string _serverRootPath, List<string> _filters)
        {
            serverRootPath = _serverRootPath;
            filters = _filters;

            AllFiles = new List<BrowseItem>();

            FoldersSource =
              new ObservableCollection<ServerFolder>() {
                  new ServerFolder(friendlyName, "", new ServerFolder[] { }) 
                                                                                };
            ServerFileImporter.BrowseToBrowseRequest_Completed += new EventHandler<GenericEventArgs<FileBrowseResult>>(ServerFileImporter_BrowseToBrowseRequest_Completed);
        }


        public event EventHandler<GenericEventArgs<string>> GetFolderContents_Completed;
        public void GetRoot()
        {
            GetFolderContents(serverRootPath);
        }
        public void GetFolderContents(string serverPath)
        {
            FileBrowseRequest request = new FileBrowseRequest();
            request.Filters = filters;
            request.FullPath = serverPath;

            ServerFileImporter.BrowseToBrowseRequest(request);
        }
        void ServerFileImporter_BrowseToBrowseRequest_Completed(object sender, GenericEventArgs<FileBrowseResult> e)
        {
            FileBrowseResult result = e.Value;

            if (! result.Success) return;

            GetFiles(result);
            GetFolders(result);

            if (GetFolderContents_Completed != null) GetFolderContents_Completed(this, new GenericEventArgs<string>(result.BaseDirectory));
        }


        #region Files
        void GetFiles(FileBrowseResult fbr)
        {
            if (fbr.Files == null) return;
            if (fbr.Files.Count < 1) return;

            // Store full path in all files
            foreach (BrowseItem item in fbr.Files)
            {
                item.Name = Path.Combine(fbr.BaseDirectory, item.Name);
            }
            AllFiles.AddRange(fbr.Files);
        }
        // Filters
        #endregion

        #region Filters / Lists
        public bool StoredFilesExistInFolder(ServerFolder folder)
        {
            return (FilesAtPath(folder.Path).Count > 0);
            
        }
        public Dictionary<string, List<BrowseItem>> AllFilesGroupedByNothing(ServerFolder folder)
        {
            Dictionary<string, List<BrowseItem>> output = new Dictionary<string, List<BrowseItem>>();

            output.Add(folder.Key, FilesAtPath(folder.Path));

            return output;
        }
        public List<BrowseItem> FilesAtPath(string path)
        {
            int limiter = 500; // LIMIT TO 500 PICS
            List<BrowseItem> output = new List<BrowseItem>();
            foreach (BrowseItem item in AllFiles)
            {
                if (pathIsWithinFolder(item.Name, path))
                {
                    output.Add(item);
                    if (limiter-- < 0) break;
                }

            }
            return output;
        }
        bool pathIsWithinFolder(string filePath, string folderPath)
        {
            return (filePath.StartsWith(folderPath));

        }
        #endregion

        #region Folders
        public ObservableCollection<ServerFolder> FoldersSource;
        void GetFolders(FileBrowseResult fbr)
        {
            // We have a sorted list of the folders, sorted by hierarchy
            Stack<PictureFolder> theFolders = new Stack<PictureFolder>();
            foreach (BrowseItem item in fbr.Directories)
            {
                string folderPath = Path.Combine(fbr.BaseDirectory, item.Name);

                string[] parts = FileParts(folderPath);
                string currentPath = "";
                int levelDeep = 1;
                ServerFolder nextFolder = FoldersSource[0];
                foreach (string filePart in parts)
                {
                    if (filePart.Length < 1) continue;

                    currentPath += filePart + "\\";

                    

                    // Traverse deeper
                    ServerFolder pfSubFolder;
                    if (! nextFolder.TryGetItemWithKey(filePart, out pfSubFolder))
                    {
                        pfSubFolder = new ServerFolder(filePart, currentPath, new ServerFolder[] { });
                        nextFolder.Items.Add(pfSubFolder); // create the level   
                    }
                    
                    nextFolder = pfSubFolder;  // continue traversing down

                    levelDeep++;
                }
            }

            
        }
        void FlattenFolders()
        {
            // Remove any levels with only one child
            MergeFolder(FoldersSource[0]);
        }
        void MergeFolder(ServerFolder folder)
        {
            // if only one child, bring it up
            if (folder.Items.Count == 1)
            {
                ObservableCollection<ServerFolder> childItems = folder.Items[0].Items;
                folder.Items = childItems;
            }

            foreach (ServerFolder pf in folder.Items)
            {
                MergeFolder(pf);
            }
        }

        string PathOnly(string FN)
        {
            int lastSlash = FN.LastIndexOf("\\");
            if (lastSlash < 0) return FN;
            return FN.Substring(0, lastSlash);
        }
        string[] FileParts(string FN)
        {
            return FN.Split(new char[] {'\\'});
        }

        #endregion



        #region Singleton Methods
        static ServerLibraryManager instance = null;
        static readonly object padlock = new object();
        public static ServerLibraryManager PictureLibrary
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        List<string> filters = new List<string>();
                        filters.Add("*.bmp");
                        filters.Add("*.gif");
                        filters.Add("*.jpg");
                        filters.Add("*.tif");
                        filters.Add("*.png");
                        filters.Add("*.iff");

                        instance = new ServerLibraryManager("Picture Library","PICTURES_LIBRARY", filters);

                    }
                    return instance;
                }
            }
        }
        #endregion

    }
}
