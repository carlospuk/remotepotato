using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
using System.Diagnostics;

/*
 *  Helper classes to obtain thumbnails for files and duration of media files
 *  using native Windows API calls
 */

namespace FatAttitude
{
    public enum ThumbnailSizes
    {
        Small,
        Medium,
        Large,
        ExtraLarge
    }

    public class ShellHelper
    {

        public Bitmap ThumbnailForFile(string FilePath, ThumbnailSizes thumbSize, ref string txtLog)
        {
            if (
                (!File.Exists(FilePath)) &&
                (!Directory.Exists(FilePath))
                )
                return null;

            if (ShellFile.IsPlatformSupported)
            {
                try
                {
                    using (ShellFile sf = ShellFile.FromFilePath(FilePath))
                    {
                        if (sf == null) return null;

                        ShellThumbnail thumb = sf.Thumbnail;
                        if (thumb == null) return null;

                        switch (thumbSize)
                        {
                            case ThumbnailSizes.Medium:
                                return thumb.MediumBitmap;

                            case ThumbnailSizes.Large:
                                return thumb.LargeBitmap;

                            case ThumbnailSizes.ExtraLarge:
                                return thumb.ExtraLargeBitmap;

                            default: //case ThumbnailSizes.Small:
                                return thumb.SmallBitmap;
                        }

                    }
                }
                catch 
                {
                    // Do not return; try legacy method
                }
            }

            // Use legacy method
            LegacyThumbnailExtractor2 tc = new LegacyThumbnailExtractor2(thumbSize);
                

            Bitmap bThumb = null;
            Bitmap bmpNew = null;
                    
            bThumb = tc.GetThumbnail(FilePath);                    

            // No thumbnail returned - look for folder.jpg (for MP3s)
            if (bThumb != null)
            {
                bmpNew = (Bitmap)bThumb.Clone();
            }
            else
            {
                // NB: use a new thumbnail extractor
                string stub = Path.GetDirectoryName(FilePath);
                string artworkFN = Path.Combine(stub, "folder.jpg");

                txtLog += "Artwork FN: " + artworkFN + Environment.NewLine;
                if (File.Exists(artworkFN))
                {
                    bThumb = tc.GetThumbnail(artworkFN);

                    if (bThumb != null)
                        bmpNew = (Bitmap)bThumb.Clone();
                }
            }
                    
            return bmpNew;
                
        
            
        }

        public List<string> FoldersInLibrary(string shortLibraryName)
        {
            shortLibraryName = shortLibraryName.ToLower();
            IKnownFolder f;
            if (shortLibraryName.Equals("videos"))
                f = KnownFolders.VideosLibrary;
            else if (shortLibraryName.Equals("music"))
                f = KnownFolders.MusicLibrary;
            else if (shortLibraryName.Equals("pictures"))
                f = KnownFolders.PicturesLibrary;
            else if (shortLibraryName.Equals("documents"))
                f = KnownFolders.DocumentsLibrary;
            else
                return new List<string>();

            List<string> output = new List<string>();
            using (ShellLibrary lib = ShellLibrary.Load(f, true))
            {
                foreach (ShellFileSystemFolder folder in lib)
                {
                    output.Add(folder.Path);
                }
            }

            return output;
        }

        public TimeSpan DurationOfMediaFile(string FilePath)
        {
            Version v = Environment.OSVersion.Version;
            // WIN7 APIs
            if (Environment.OSVersion.Version >= new Version(6, 1))
            {
                using (ShellFile sf = ShellFile.FromFilePath(FilePath))
                {
                    ShellProperties props = sf.Properties;
                    ShellProperties.PropertySystem psys = props.System;


                    ulong? duration = psys.Media.Duration.Value;
                    if (duration.HasValue)
                        return TimeSpan.FromTicks((long)duration.Value);
                }
            }
            else
            {
                // LEGACY ***  (XP and Vista)

                LegacyMediaDuration lmd = new LegacyMediaDuration();
                return lmd.GetMediaDuration(FilePath);
            }

            return TimeSpan.FromSeconds(0);
        }


    }

}
