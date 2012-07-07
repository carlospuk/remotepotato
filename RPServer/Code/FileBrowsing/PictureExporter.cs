using System;
using System.Collections.Generic;
using CommonEPG;
using System.IO;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using FatAttitude;

namespace RemotePotatoServer
{
    public static class PictureExporter
    {

        public static bool SendThumbnailsAsZipFile(FileBrowseRequest request, ThumbnailSizes thumbSize, ref BrowserSender bSender)
        {
            // We'll need a shell helper
            FatAttitude.ShellHelper sh = new FatAttitude.ShellHelper();

            // Set up temp directory
            string tempFolderName = Path.GetRandomFileName();
            string tempPath = Path.Combine (Functions.ZipTempFolder, tempFolderName);
            Directory.CreateDirectory(tempPath);

            // Go through the thumbnails (filter already applied, so these are pic files)
            FileBrowseResult fbResult = FileBrowseExporter.BrowsePath(request.FullPath, request.Filters);
            // Any files?
            if (fbResult.Files.Count < 1) return false;

            int SkipCounter = 0;
            int OutputCounter = 0;
            List<string> outputFiles = new List<string>();
            foreach (BrowseItem bItem in fbResult.Files)
            {
                // Skip items before batch
                if (request.ThumbnailsLimitToBatch)
                    if (request.ThumbnailsBatch > 0)
                        if (SkipCounter++ < (request.ThumbnailsBatchSize * request.ThumbnailsBatch))
                            continue;

                string strFullPath = Path.Combine(fbResult.BaseDirectory, bItem.Name);
                string strLog = ""; // ignore log
                Bitmap bmp = sh.ThumbnailForFile(strFullPath, thumbSize, ref strLog);

                string fnSansExtension = Path.GetFileNameWithoutExtension(bItem.Name);
                string strOutputFileFullPath = Path.Combine(tempPath,  (fnSansExtension + "_thumb.jpg" ) );
                bmp.Save(strOutputFileFullPath, ImageFormat.Jpeg);

                outputFiles.Add(strOutputFileFullPath);

                // End of batch?
                if (request.ThumbnailsLimitToBatch)
                    if (OutputCounter++ >= request.ThumbnailsBatchSize)
                        break;
            }
            
            // Now zip up the files
            string strOutputZipFile = Path.Combine(Functions.ZipTempFolder, (  Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + ".zip") );
            bool result = ZipHelper.CreateZipFileFromFiles(outputFiles, strOutputZipFile);

            // And send the zip file to the browser
            result &= (bSender.SendFileToBrowser(strOutputZipFile));
            File.Delete(strOutputZipFile);
            Directory.Delete(tempPath, true);

            return result;
        }

    }
}
