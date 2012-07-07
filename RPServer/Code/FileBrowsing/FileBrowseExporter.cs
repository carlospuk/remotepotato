using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CommonEPG;
using FatAttitude.HTML;
using RemotePotatoServer.Properties;


namespace RemotePotatoServer
{
    static class FileBrowseExporter
    {
        public static string FileBrowseUsingRequestAsXML(FileBrowseRequest request)
        {
            return XMLHelper.Serialize<FileBrowseResult>( FileBrowseUsingRequest(request) );
        }
        private static FileBrowseResult FileBrowseUsingRequest(FileBrowseRequest request)
        {
            return BrowsePath(request.FullPath, request.Filters, request.GetDurationOfMediaFiles);
        }

        public enum MediaFileTypes
        {
            Music,
            Image,
            Video
        }

        // MOVE ***************************

        static FatAttitude.ShellHelper sHelper;
        /// <summary>
        /// Browse to a library path; use filters for specified media type and get duration for any non-image media type
        /// </summary>
        /// <param name="FullPath"></param>
        /// <param name="fileType"></param>
        /// <returns></returns>
        public static FileBrowseResult BrowsePath(string FullPath, MediaFileTypes fileType)
        {
            List<string> Filters = filtersForMediaType(fileType);
            return BrowsePath(FullPath, Filters, (fileType != MediaFileTypes.Image));
        }
        /// <summary>
        /// Browse to a library path; use filters for specified media type and optionally attempt to get the duration of the files
        /// </summary>
        /// <param name="FullPath"></param>
        /// <param name="fileType"></param>
        /// <param name="getMediaDuration"></param>
        /// <returns></returns>
        public static FileBrowseResult BrowsePath(string FullPath, MediaFileTypes fileType, bool getMediaDuration)
        {
            List<string> Filters = filtersForMediaType(fileType);
            return BrowsePath(FullPath, Filters, getMediaDuration);
        }
        static List<string> filtersForMediaType(MediaFileTypes fileType)
        {
            System.Collections.Specialized.StringCollection sc = new System.Collections.Specialized.StringCollection();
            switch (fileType)
            {
                case MediaFileTypes.Image:
                    sc = Settings.Default.ImageFileExtensions;
                    break;

                case MediaFileTypes.Music:
                    sc = Settings.Default.MusicFileExtensions;
                    break;

                case MediaFileTypes.Video:
                    sc = Settings.Default.VideoFileExtensions;
                    break;

                default:
                    break;
            }

            List<string> Filters = new List<string>();

            foreach (string strExt in sc)
            {
                Filters.Add("*." + strExt);
            }

            return Filters;
        }
        public static FileBrowseResult BrowsePath(string FullPath, List<string> Filters)
        {
            return BrowsePath(FullPath, Filters, false);
        }
        public static FileBrowseResult BrowsePath(string FullPath, List<string> Filters, bool getMediaDuration)
        {
            // Libraries
            if (FullPath.Equals("VIDEO_LIBRARY")) return GetFoldersForLibrary("videos", Filters);
            if (FullPath.Equals("DOCUMENTS_LIBRARY")) return GetFoldersForLibrary("documents", Filters);
            if (FullPath.Equals("MUSIC_LIBRARY")) return GetFoldersForLibrary("music", Filters);
            if (FullPath.Equals("PICTURES_LIBRARY")) return GetFoldersForLibrary("pictures", Filters);

            // 'Faux' library (collection)
            if (FullPath.Equals("MOVIE_LIBRARY")) return GetMoviesInMovieLibrary();

            FileBrowseResult output = new FileBrowseResult();

            output.BaseDirectory = FullPath;

            // Exists
            if (! Directory.Exists(FullPath)) 
            {
                output.ErrorText = "Directory not found.";
                output.Success = false;
                return output;
            }

            if (Filters.Count < 1) Filters = new List<string>() {"*.*"};

            DirectoryInfo masterDi = new DirectoryInfo(FullPath);

            // 1. Files
            List<FileInfo> tempOutputFiles = new List<FileInfo>();
            foreach (string filter in Filters)
                tempOutputFiles.AddRange(masterDi.GetFiles(filter));
            
            // Sort by name
            var orderedFiles = tempOutputFiles.OrderBy(f => f.Name);
            if (getMediaDuration) CreateShellHelperIfNull();  // use a shell helper to get duration of media files
            foreach (FileInfo fi in orderedFiles)
                output.Files.Add(fileInfoToBrowseItem(fi, getMediaDuration, false));
            sHelper = null;

            // 2. Directories
            DirectoryInfo[] directoryInfos = masterDi.GetDirectories();
            var orderedDirectories = directoryInfos.OrderBy(d => d.Name);
            foreach (DirectoryInfo di in orderedDirectories)
                output.Directories.Add( directoryInfoToBrowseItem(di, Filters));
            
            // Success
            output.Success = true;
            output.ErrorText = "OK";
            return output;
        }
        static void CreateShellHelperIfNull()
        {
            if (sHelper == null)
                sHelper = new FatAttitude.ShellHelper();
        }
        static FileBrowseResult GetFoldersForLibrary(string libraryName, List<string> Filters) // music, videos, pictures, documents
        {
            FileBrowseResult output = new FileBrowseResult();

            CreateShellHelperIfNull();

            try
            {

                List<String> folders  = new List<string>();
                folders = FoldersForLibrary(libraryName);

                foreach (string folder in folders)
                {
                    BrowseItem folderItem = stringToBrowseItem(folder, true, Filters);
                    if (folderItem != null) output.Directories.Add(folderItem);
                }

                output.ErrorText = "OK";
                output.Success = true;
            }
            catch (ArgumentException )
            {
                output.Success = false;
                output.ErrorText = "NO LIBRARY";
            }
            catch (Exception ex)
            {
                output.Success = false;
                output.ErrorText = ex.Message;
            }

            sHelper = null;
            return output;
        }
        static List<string> FoldersForLibrary(string libName)
        {
            // Check for permission first
            if ((libName.ToUpperInvariant().Equals("PICTURES")) && (!Settings.Default.EnablePictureLibrary))
                return new List<string>();
            if ((libName.ToUpperInvariant().Equals("VIDEOS")) && (!Settings.Default.EnableVideoLibrary))
                return new List<string>();
            if ((libName.ToUpperInvariant().Equals("MUSIC")) && (!Settings.Default.EnableMusicLibrary)) // not used
                return new List<string>();


            switch (libName.ToUpperInvariant())
            {
                // These libraries have a remote potato local list, ALWAYS used if OS doesn't support media libraries and optional otherwise
                case "PICTURES":
                    if ((Functions.OSSupportsExplorerLibraries) && (Settings.Default.UseExplorerLibraryForPictureFolders))
                        return sHelper.FoldersInLibrary(libName);
                    else
                        return FoldersInRemotePotatoUserSettings(libName);

                case "VIDEOS":
                    if ((Functions.OSSupportsExplorerLibraries) && (Settings.Default.UseExplorerLibraryForVideoFolders))
                        return sHelper.FoldersInLibrary(libName);
                    else
                        return FoldersInRemotePotatoUserSettings(libName);

                // These libraries have no remote potato local folder list
                case "MUSIC": // not used
                case "DOCUMENTS":
                    if (Functions.OSSupportsExplorerLibraries) 
                        return sHelper.FoldersInLibrary(libName);
                    else
                        return FoldersInWindowsCommonFoldersForLibrary(libName);  // best guess
                   
                default:
                    return new List<string>();
            }

        }
        static List<string> FoldersInRemotePotatoUserSettings(string libName)
        {
            List<string> prototypeList = new List<string>();

            System.Collections.Specialized.StringCollection useCollection = new System.Collections.Specialized.StringCollection();
            switch (libName.ToUpperInvariant() )
            {
                case "PICTURES":
                    useCollection = Settings.Default.PictureLibraryFolders;
                    break;

                case "VIDEOS":
                    useCollection = Settings.Default.VideoLibraryFolders;
                    break;

                default:
                    return new List<string>();
            }

            // Remove blanks
            List<string> outputList = new List<string>();
            foreach (string str in useCollection)
            {
                if (!(string.IsNullOrWhiteSpace(str))) outputList.Add(str);
            }

            return outputList;
        }
        static List<string> FoldersInWindowsCommonFoldersForLibrary(string libName)
        {
            List<string> prototypeList = new List<string>();

            switch (libName.ToUpperInvariant())
            {
                case "PICTURES":
                    prototypeList.Add(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures));
                    prototypeList.Add(Environment.GetFolderPath(Environment.SpecialFolder.CommonPictures));
                    break;

                case "VIDEOS":
                    prototypeList.Add(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos));
                    prototypeList.Add(Environment.GetFolderPath(Environment.SpecialFolder.CommonVideos));
                    break;

                case "MUSIC":
                    prototypeList.Add(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic));
                    prototypeList.Add(Environment.GetFolderPath(Environment.SpecialFolder.CommonMusic));
                    break;

                case "DOCUMENTS":
                    prototypeList.Add(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
                    prototypeList.Add(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments));
                    break;

                default:
                    return null;
            }

            // Remove blanks
            List<string> outputList = new List<string>();
            foreach (string str in prototypeList)
            {
                if (!(string.IsNullOrWhiteSpace(str))) outputList.Add(str);
            }

            return outputList;
        }
        /// <summary>
        /// Retrieve the files in all registered 7MC movie library folders, with extensions from Settings.Default.VideoFileExtensions
        /// </summary>
        /// <returns></returns>
        static FileBrowseResult GetMoviesInMovieLibrary()
        {
            FileBrowseResult output = new FileBrowseResult();

            CreateShellHelperIfNull();

            output.Directories = new List<BrowseItem>();

            MCLibraryFolderHelper fHelper = new MCLibraryFolderHelper();
            List<string> movieFolders = fHelper.MediaCenterLibraryFolders(MCLibraryFolderHelper.Libraries.Movie);

            List<string> VideoFilters = filtersForMediaType(MediaFileTypes.Video);
            List<BrowseItem> filesFound = new List<BrowseItem>();
            foreach (string movieFolder in movieFolders)
            {
                browseFolderForFiles(movieFolder, ref filesFound, VideoFilters, true);
            }

            // Now add recorded TV shows that are movies
            foreach (TVProgramme tvp in RecTV.Default.RecordedTVProgrammes.Values)
            {
                if (tvp.ProgramType != TVProgrammeType.Movie) continue;  // movies only

                FileInfo fi = new FileInfo(tvp.Filename);
                BrowseItem newItem = fileInfoToBrowseItem(fi, true, true);
                filesFound.Add(newItem);
            }

            // Sort the list of movies alphabetically by filename (NAME not file path, path is stripped away by the comparer)
            CommonEPG.Comparers.BrowseItemComparer bComparer = new CommonEPG.Comparers.BrowseItemComparer();
            filesFound.Sort(bComparer);
            output.Files = filesFound;

            // Set base directory flag
            output.BaseDirectory = "MOVIE_LIBRARY";
            output.Success = true;

            sHelper = null;

            return output;
        }
        /// <summary>
        /// Recursing function to browse a folder
        /// </summary>
        /// <param name="browseItems"></param>
        /// <param name="Filters"></param>
        static void browseFolderForFiles(string folderPath, ref List<BrowseItem> browseItems, List<string> Filters, bool getMediaDuration)
        {
            DirectoryInfo diThis = new DirectoryInfo(folderPath);
            // RECURSE
            foreach (DirectoryInfo diChild in diThis.GetDirectories() )
            {
                browseFolderForFiles(diChild.FullName, ref browseItems, Filters, getMediaDuration);
            }

            // Now add my files
            foreach (string filter in Filters)
            {
                foreach (FileInfo fiChild in diThis.GetFiles(filter))
                {
                    BrowseItem newItem = fileInfoToBrowseItem(fiChild, getMediaDuration, true);
                    browseItems.Add(newItem);
                }
            }
        }

        static BrowseItem fileInfoToBrowseItem(FileInfo fi, bool getMediaDuration, bool setNameToFullPath)
        {
            BrowseItem output = new BrowseItem();
            
            // Name / path
            if (setNameToFullPath)
                output.Name = fi.FullName;
            else
                output.Name = fi.Name;

            output.Size = fi.Length;
            output.IsDirectory = false;

            if (getMediaDuration)
            {
                try
                {
                    TimeSpan mDuration = DurationOfMediaFile_OSSpecific(fi.FullName);
                    output.Duration = mDuration.TotalSeconds;
                }
                catch  (Exception ex)
                {
                    Functions.WriteLineToLogFileIfAdvanced("Could not get duration of media file: " + fi.FullName + " :");
                    Functions.WriteExceptionToLogFileIfAdvanced(ex);
                }
                
            }
            
            return output;
        }
        public static TimeSpan DurationOfMediaFile_OSSpecific(string FN)
        {
            CreateShellHelperIfNull();

            if (Functions.OSSupportsMediaDurationInShell)
            {
                TimeSpan tryGetTime = sHelper.DurationOfMediaFile(FN); // Use shell
                if (tryGetTime.Ticks > 0)
                    return tryGetTime;
            }
            
            // ELSE use FFMPEG
            return StreamingManager.Default.GetMediaDuration(FN);  // Use FFMPEG
        }
        static BrowseItem directoryInfoToBrowseItem(DirectoryInfo di,  List<string> Filters)
        {
            BrowseItem output = new BrowseItem();
            output.Name = di.Name;
            output.IsDirectory = true;

            // Number of Items
            int numberOfSubdirectories = di.GetDirectories().Count();
            int numberOfFiles = 0;
            foreach (string filter in Filters)
            {
                numberOfFiles += di.GetFiles(filter).Count();
            }
            output.Items = numberOfSubdirectories + numberOfFiles;

            return output;
        }
        static BrowseItem stringToBrowseItem(string strPath, bool isDirectory, List<string> Filters)
        {
            if (isDirectory)
            {
                if (! Directory.Exists(strPath)) return null;
                DirectoryInfo di = new DirectoryInfo(strPath);
                BrowseItem folderItem = directoryInfoToBrowseItem(di, Filters);
                // Override normal behaviour, which is not to include the full path in the name
                // (because this current method is used by the root folder selection, so there is no base folder to set)
                folderItem.Name = strPath;
                return folderItem;
            }
            else
            {
                if (!File.Exists(strPath)) return null;

                BrowseItem output = new BrowseItem();
                output.Name = strPath;
                output.IsDirectory = false;

                return output;
            }
        }

        #region Zip Helper
        public static bool SendFolderFilesAsZipFile(FileBrowseResult fbResult, ref BrowserSender bSender)
        {
            // We'll need a shell helper
            CreateShellHelperIfNull();

            // Set up temp directory
            string tempFolderName = Path.GetRandomFileName();
            string tempPath = Path.Combine(Functions.ZipTempFolder, tempFolderName);
            Directory.CreateDirectory(tempPath);

            // Any files?
            if (fbResult.Files.Count < 1) return false;

            List<string> outputFiles = new List<string>();
            foreach (BrowseItem bItem in fbResult.Files)
            {
                string strFullPath = Path.Combine(fbResult.BaseDirectory, bItem.Name);
                outputFiles.Add(strFullPath);
            }

            // Now zip up the files
            string strOutputZipFile = Path.Combine(Functions.ZipTempFolder, (Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + ".zip"));
            bool result = ZipHelper.CreateZipFileFromFiles(outputFiles, strOutputZipFile);

            // And send the zip file to the browser
            result &= (bSender.SendFileToBrowser(strOutputZipFile, false, false, true));
            File.Delete(strOutputZipFile);
            Directory.Delete(tempPath, true);

            return result;
        }
        #endregion

        #region HTML
        // PICS library
        public static string HTMLTableForPicturesLibrary(string strPath, int numberOfColumns)
        {
            FileBrowseResult fbrs = BrowsePath(strPath, MediaFileTypes.Image);
            return HTMLTableForPicturesLibrary(fbrs, numberOfColumns, 0, 50, true);
        }
        static string HTMLTableForPicturesLibrary(FileBrowseResult fbrs, int numberOfColumns, int pageNumber, int itemsPerPage, bool showThumbnails)
        {
            List<string> content = new List<string>();

            foreach (BrowseItem strFolder in fbrs.Directories)
            {
                string cellContent = "";
             
                // Link
                string folderPath = Path.Combine(fbrs.BaseDirectory, strFolder.Name);
                folderPath = Functions.EncodeToBase64(folderPath);

                string folderImageSource =  "/static/images/imgFolder150x75.png";
                HTMLImage image = new HTMLImage(folderImageSource, "folderpic");
                cellContent += image.ToString();
                cellContent += "<br />";
                cellContent += Path.GetFileName(strFolder.Name);


                HTMLLink lnk = new HTMLLink("browsepics?PATH=" + folderPath, cellContent);

                content.Add(lnk.ToString() );
            }
            foreach (BrowseItem strFile in fbrs.Files)
            {
                // Assemble path to file
                string filePath = Path.Combine(fbrs.BaseDirectory, strFile.Name);
                filePath = Functions.EncodeToBase64(filePath);

                string imgSrc = "getfilethumbnail64?filename=" + filePath + "&size=medium";
                HTMLImage image = new HTMLImage(imgSrc, "thumbnail");

                
                // Link
                HTMLLink lnk = new HTMLLink("viewpic?FN=" + filePath + "&size=extralarge",  image.ToString());

                content.Add(lnk.ToString());
            }

            return HTMLTable.HTMLTableWithCellContents("picturelibrarytable", numberOfColumns, content);

        }

        // VIDS library
        public static string HTMLTableForVideoLibrary(string strPath, int numberOfColumns)
        {
            FileBrowseResult fbrs = BrowsePath(strPath, MediaFileTypes.Video, false); // no need to get duration in HTML
            return HTMLTableForVideoLibrary(fbrs, numberOfColumns, 0, 50, true);
        }
        static string HTMLTableForVideoLibrary(FileBrowseResult fbrs, int numberOfColumns, int pageNumber, int itemsPerPage, bool showThumbnails)
        {
            List<string> content = new List<string>();

            foreach (BrowseItem strFolder in fbrs.Directories)
            {
                string cellContent = "";

                // Link
                string folderPath = Path.Combine(fbrs.BaseDirectory, strFolder.Name);
                folderPath = Functions.EncodeToBase64(folderPath);

                string folderImageSource = "/static/images/imgFolder150x75.png";
                HTMLImage image = new HTMLImage(folderImageSource, "folderpic");
                cellContent += image.ToString();
                cellContent += "<br />";
                cellContent += Path.GetFileName(strFolder.Name);


                HTMLLink lnk = new HTMLLink("browsevideos?PATH=" + folderPath, cellContent);

                content.Add(lnk.ToString());
            }
            foreach (BrowseItem strFile in fbrs.Files)
            {
                // Assemble path to file
                string filePath = Path.Combine(fbrs.BaseDirectory, strFile.Name);
                filePath = Functions.EncodeToBase64(filePath);

                string imgSrc = "getfilethumbnail64?filename=" + filePath + "&size=medium";
                HTMLImage image = new HTMLImage(imgSrc, "thumbnail");


                // Link
                HTMLLink lnk = new HTMLLink("streamvideo?FN=" + filePath, image.ToString());
                content.Add(lnk.ToString());
            }

            return HTMLTable.HTMLTableWithCellContents("videolibrarytable", numberOfColumns, content);

        }
        #endregion

    }
}
