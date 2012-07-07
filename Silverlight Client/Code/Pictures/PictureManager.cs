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
    public static class PictureManager
    {
        // Members
        public static List<RPPictureItem> AllPictures;
        public static event EventHandler PictureLibraryLoaded;
        public static event EventHandler<GenericEventArgs<double>> PictureLibraryLoadingProgress;
        public static bool PicturesUpdating;
        public const int BATCH_SIZE = 500;

        // Constructor
        static PictureManager()
        {
            AllPictures = new List<RPPictureItem>();
            PicturesUpdating = false;

            picFoldersSource =
              new ObservableCollection<PictureFolder>() {
                  new PictureFolder("Picture Library", "", new PictureFolder[] { }) 
                                                                                };
            PictureImporter.ImportPictureBatchCompleted += new EventHandler<GenericEventArgs<RPPictureBatch>>(PictureImporter_ImportPictureBatchCompleted);
        }


        #region Get Pictures
        static int batchCursor = 0;
        public static void GetAllPictures()
        {
            if (PicturesUpdating) return;

            PicturesUpdating = true;

            if (AllPictures != null)
                AllPictures.Clear();

            GetNextPictureBatch();
            
        }
        static void GetNextPictureBatch()
        {
            PictureImporter.ImportPictureBatch(batchCursor);
        }
        static void PictureImporter_ImportPictureBatchCompleted(object sender, GenericEventArgs<RPPictureBatch> e)
        {
            // Store
            AllPictures.AddRange(e.Value.Pictures);
            ProcessAlbumTree(e.Value.Pictures);

            // Update GUI so far...
            if (PictureLibraryLoadingProgress != null)
            {
                double progress = (Convert.ToDouble(AllPictures.Count) / Convert.ToDouble(e.Value.TotalPicturesInLibrary));
                PictureLibraryLoadingProgress(new object(), new GenericEventArgs<double>(progress));
            }

            // More batches?
            if (e.Value.Pictures.Count >= BATCH_SIZE)
            {
                batchCursor = batchCursor + BATCH_SIZE;
                GetNextPictureBatch();
                return;
            }

            // It's over
            PicturesUpdating = false;
            if (PictureLibraryLoaded != null) PictureLibraryLoaded(new object(), new EventArgs());            
            //FlattenFolders();  // not used at moment
        }
        static void ProcessAlbumTree(List<RPPictureItem> pics)
        {
            // PROCESS ALBUMS HERE...
            GetFolders(pics);
        }
        #endregion

        #region Folders
        public static ObservableCollection<PictureFolder> picFoldersSource;
        static void GetFolders(List<RPPictureItem> pics)
        {
            List<string> picFolders = new List<string>();
            foreach (RPPictureItem pi in pics)
            {
                string folderPath = PathOnly(pi.FileName);
                if (!picFolders.Contains(folderPath))
                    picFolders.Add(folderPath);
            }

            picFolders.Sort();

            // Now we have a sorted list of all the folders, sorted by hierarchy
            Stack<PictureFolder> theFolders = new Stack<PictureFolder>();
            foreach (string folderPath in picFolders)
            {
                string[] parts = FileParts(folderPath);
                string currentPath = "";
                int levelDeep = 1;
                PictureFolder nextFolder = picFoldersSource[0];
                foreach (string filePart in parts)
                {
                    if (filePart.Length < 1) continue;

                    currentPath += filePart + "\\";

                    

                    // Traverse deeper
                    PictureFolder pfSubFolder;
                    if (! nextFolder.TryGetItemWithKey(filePart, out pfSubFolder))
                    {
                        pfSubFolder = new PictureFolder(filePart, currentPath, new PictureFolder[] { });
                        nextFolder.Items.Add(pfSubFolder); // create the level   
                    }
                    
                    nextFolder = pfSubFolder;  // continue traversing down

                    levelDeep++;
                }
            }

            
        }
        static void FlattenFolders()
        {
            // Remove any levels with only one child
            MergeFolder(picFoldersSource[0]);
        }
        static void MergeFolder(PictureFolder folder)
        {
            // if only one child, bring it up
            if (folder.Items.Count == 1)
            {
                ObservableCollection<PictureFolder> childItems = folder.Items[0].Items;
                folder.Items = childItems;
            }

            foreach (PictureFolder pf in folder.Items)
            {
                MergeFolder(pf);
            }
        }

        static string PathOnly(string FN)
        {
            int lastSlash = FN.LastIndexOf("\\");
            if (lastSlash < 0) return FN;
            return FN.Substring(0, lastSlash);
        }
        static string[] FileParts(string FN)
        {
            return FN.Split(new char[] {'\\'});
        }

        #endregion

        #region Filters / Lists
        public static Dictionary<string, List<RPPictureItem>> AllPicturesGroupedByNothing(PictureFolder folder)
        {
            Dictionary<string, List<RPPictureItem>> output = new Dictionary<string, List<RPPictureItem>>();

            output.Add(folder.Key, PicturesAtPath(folder.Path));

            return output;
        }
        public static List<RPPictureItem> PicturesAtPath(string path)
        {
            int limiter = 500; // LIMIT TO 500 PICS
            List<RPPictureItem> output = new List<RPPictureItem>();
            foreach (RPPictureItem pi in AllPictures)
            {
                if (pathIsWithinFolder(pi.FileName, path))
                {
                    output.Add(pi);
                    if (limiter-- < 0) break;
                }
                
            }
            return output;
        }
        static bool pathIsWithinFolder(string filePath, string folderPath)
        {
            if (folderPath.EndsWith("\\"))
                folderPath = folderPath.Substring(0, folderPath.Length - 1);

            if (filePath.EndsWith("\\"))
                filePath = filePath.Substring(0, folderPath.Length - 1);

            return (PathOnly(filePath).Equals(folderPath));
        }
        #endregion

        #region Extension Methods
        public static Uri ThumbnailUriOrNull(this RPPictureItem pi, string size)
        {
            if (string.IsNullOrEmpty(size)) size = "medium";

            if (String.IsNullOrEmpty(pi.FileName))
                return null;
            else
                return new Uri(NetworkManager.hostURL + "getfilethumbnail64?filename=" + Uri.EscapeUriString( Functions.EncodeToBase64(pi.FileName) ) +
                                                        "&size=" + size, UriKind.Absolute);
        }
        public static Uri SourceUriOrNull(this RPPictureItem pi, Size frameSize)
        {
            if (String.IsNullOrEmpty(pi.FileName))
                return null;
            else
            {
                string strAdjustedFN = pi.FileName;
                /*
                strAdjustedFN = strAdjustedFN.Replace("\\\\", "#UNC#");  // preserve UNC server prefix
                strAdjustedFN = strAdjustedFN.Replace("\\", "/");
                strAdjustedFN = strAdjustedFN.Replace("#UNC#", "\\\\");
                */
                
                // Base64 encode so it works with non-ASCII characters
                strAdjustedFN = Functions.EncodeToBase64(strAdjustedFN);
                return new Uri(NetworkManager.hostURL + "xml/picture/getwithfilename64/"  + Convert.ToInt32(frameSize.Width).ToString() + "/" + Convert.ToInt32( frameSize.Height).ToString()
                    + "/" +   strAdjustedFN + "?token=" + NetworkManager.serverToken , UriKind.Absolute);
            }
        }
        public static Uri DeepZoomSourceUriOrNull(this RPPictureItem pi)
        {
            if (String.IsNullOrEmpty(pi.FileName))
                return null;
            else
                return new Uri(NetworkManager.hostURL + "rppictureitemdeepzoom?picid=" + Uri.EscapeUriString(pi.ID),
                                                UriKind.Absolute);
        }
        #endregion
    }
}
