using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using HundredMilesSoftware.UltraID3Lib;

namespace FatAttitude
{
    public class LegacyThumbnailExtractor2
    {

        // Objects
        public Size DesiredSize  {get; set;}

        #region Implementation

        public LegacyThumbnailExtractor2(ThumbnailSizes thumbSize)
        {
            // Default Size

            // LEGACY **
            switch (thumbSize)
            {
                case ThumbnailSizes.Small:
                    this.DesiredSize = new Size(50, 50);
                    break;

                case ThumbnailSizes.Medium:
                    this.DesiredSize = new Size(100, 100);
                    break;

                case ThumbnailSizes.Large:
                    this.DesiredSize = new Size(200, 200);
                    break;

                case ThumbnailSizes.ExtraLarge:
                    this.DesiredSize = new Size(300, 300);
                    break;

                default:
                    this.DesiredSize = new Size(100, 100);
                    break;
            }

            
        }
        public Bitmap GetThumbnail(string fileName)
        {
            // Get MP3 artwork if embedded
            if (Path.GetExtension(fileName).ToUpperInvariant() == ".MP3")
            {
                Bitmap tryGetBMP = GetThumbnailFromID3Tag(fileName);

                if (tryGetBMP != null)
                {
                    // Resize?
                    if ((tryGetBMP.Width > DesiredSize.Width) | (tryGetBMP.Height > DesiredSize.Height))
                    {
                        Image resizedBmp = resizeImage((Image)tryGetBMP, DesiredSize, true);
                        return new Bitmap(resizedBmp);
                    }

                    return tryGetBMP;

                }
            }
            
            // Otherwise, get thumbnail
            return GetThumbnailFromIExtractImage(fileName, 32);
        }

        #region ID3
        Bitmap GetThumbnailFromID3Tag(string fileName)
        {
            UltraID3 myMp3;
            try
            {
                myMp3 = new UltraID3();
                myMp3.Read(fileName);
                ID3FrameCollection myArtworkCollection = myMp3.ID3v2Tag.Frames.GetFrames(MultipleInstanceID3v2FrameTypes.ID3v22Picture);
                Bitmap tryGetBMP = getThumbnailFromFrameCollection(myArtworkCollection);

                if (tryGetBMP != null) return tryGetBMP;

                myArtworkCollection = myMp3.ID3v2Tag.Frames.GetFrames(MultipleInstanceID3v2FrameTypes.ID3v23Picture);
                tryGetBMP = getThumbnailFromFrameCollection(myArtworkCollection);

                return tryGetBMP;
            }
            catch
            {
                myMp3 = null;
            }

            return null;
        }
        Bitmap getThumbnailFromFrameCollection(ID3FrameCollection fc)
        {
            Bitmap retrievedPic = null;

            foreach (ID3v2Frame fra in fc)
            {
                if (fra is ID3v2PictureFrame)
                {
                    ID3v2PictureFrame pfra = (ID3v2PictureFrame)fra;

                    // If we don't have a picture grab the first one
                    if (retrievedPic == null)
                        retrievedPic = pfra.Picture;
                    else
                    {
                        // We have a picture, only overwrite if this is cover art
                        if (pfra.PictureType == PictureTypes.CoverFront)
                            retrievedPic = pfra.Picture;
                    }
                }
            }


            return retrievedPic;
        }
        #endregion

        #region IExtract
        Bitmap GetThumbnailFromIExtractImage(string fileName, int colorDepth)
        {
        IShellFolder desktopFolder;
        IShellFolder someFolder = null;
        IExtractImage extract = null;
        IntPtr pidl;
        IntPtr filePidl;

        // Manually define the IIDs for IShellFolder and IExtractImage
        Guid IID_IShellFolder = new Guid("000214E6-0000-0000-C000-000000000046");
        Guid IID_IExtractImage = new Guid("BB2E617C-0920-11d1-9A0B-00C04FC2D6C1");

        //Divide the file name into a path and file name
        string folderName = Path.GetDirectoryName(fileName);
        string shortFileName = Path.GetFileName(fileName);

        //Get the desktop IShellFolder
        desktopFolder = getDesktopFolder;

        //Get the parent folder IShellFolder
        int cParsed = 0;
        int pdwAttrib = 0;

        desktopFolder.ParseDisplayName(IntPtr.Zero, IntPtr.Zero, folderName, out cParsed, out pidl, out pdwAttrib);
        desktopFolder.BindToObject(pidl, IntPtr.Zero, ref IID_IShellFolder, ref someFolder);

        //Get the file//s IExtractImage
        someFolder.ParseDisplayName(IntPtr.Zero, IntPtr.Zero, shortFileName, out cParsed, out filePidl, out pdwAttrib);
        object iunk = null;
        IntPtr[] pidl_array = new IntPtr[] { filePidl };
        someFolder.GetUIObjectOf(IntPtr.Zero, 1, pidl_array, ref IID_IExtractImage, IntPtr.Zero, out iunk);
        extract = (IExtractImage)iunk;

        //Set the size
        SIZE size;
        size.cx = DesiredSize.Width;
        size.cy = DesiredSize.Height;
        
        StringBuilder location = new StringBuilder(260, 260);
        int priority = 0;
        int requestedColourDepth = 32;
        // The IEIFLAG_ORIGSIZE flag tells it to use the original aspect
        // ratio for the image size. The IEIFLAG_QUALITY flag tells the 
        // interface we want the image to be the best possible quality.
        EIEIFLAG flags = EIEIFLAG.IEIFLAG_ORIGSIZE | EIEIFLAG.IEIFLAG_QUALITY; // EIEIFLAG.IEIFLAG_ASPECT;// EIEIFLAG.IEIFLAG_SCREEN; //EIEIFLAG flags = EIEIFLAG.IEIFLAG_ORIGSIZE | EIEIFLAG.IEIFLAG_QUALITY;
        int uFlags = (int)flags;

        IntPtr bmp;
        //Interop will throw an exception if one of these calls fail.
        try
        {
            extract.GetLocation(location, location.Capacity, ref priority, ref size, requestedColourDepth, ref uFlags);
            extract.Extract(out bmp);

            if (!bmp.Equals(IntPtr.Zero))
            {
                return Image.FromHbitmap(bmp);
            }
            else
            {
                return null;
            }
        }
        catch 
        {
            return null;
        }
        finally
        {
            //Free the pidls. The Runtime Callable Wrappers should automatically release the COM objects
            Marshal.FreeCoTaskMem(pidl);
            Marshal.FreeCoTaskMem(filePidl);
        }
    }
        #endregion

        #region Image Resizing Help
        public static Image resizeImage(Image imgToResize, Size size, bool allowGrowth)
        {
            int sourceWidth = imgToResize.Width;
            int sourceHeight = imgToResize.Height;

            float nPercent = 0;
            float nPercentW = 0;
            float nPercentH = 0;

            nPercentW = ((float)size.Width / (float)sourceWidth);
            nPercentH = ((float)size.Height / (float)sourceHeight);

            if (nPercentH < nPercentW)
                nPercent = nPercentH;
            else
                nPercent = nPercentW;


            if (!allowGrowth)
            {
                if (nPercent > 100) nPercent = 100;
            }


            int destWidth = (int)(sourceWidth * nPercent);
            int destHeight = (int)(sourceHeight * nPercent);

            Bitmap b = new Bitmap(destWidth, destHeight);
            Graphics g = Graphics.FromImage((Image)b);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            g.DrawImage(imgToResize, 0, 0, destWidth, destHeight);
            g.Dispose();

            return (Image)b;
        }
        #endregion
        private IShellFolder getDesktopFolder
        {
            get
            {
                IShellFolder ppshf;
                int r = UnManagedMethods.SHGetDesktopFolder(out ppshf);
                return ppshf;
            }
        }

        #endregion



        #region ShellFolder Enumerations
        [Flags]
        enum ESTRRET : int
        {
            STRRET_WSTR = 0x0000, // Use STRRET.pOleStr
            STRRET_OFFSET = 0x0001, // Use STRRET.uOffset to Ansi
            STRRET_CSTR = 0x0002 // Use STRRET.cStr
        }
        [Flags]
        enum ESHCONTF : int
        {
            SHCONTF_FOLDERS = 32,
            SHCONTF_NONFOLDERS = 64,
            SHCONTF_INCLUDEHIDDEN = 128
        }

        [Flags]
        enum ESHGDN : int
        {
            SHGDN_NORMAL = 0,
            SHGDN_INFOLDER = 1,
            SHGDN_FORADDRESSBAR = 16384,
            SHGDN_FORPARSING = 32768
        }
        [Flags]
        enum ESFGAO : int
        {
            SFGAO_CANCOPY = 1,
            SFGAO_CANMOVE = 2,
            SFGAO_CANLINK = 4,
            SFGAO_CANRENAME = 16,
            SFGAO_CANDELETE = 32,
            SFGAO_HASPROPSHEET = 64,
            SFGAO_DROPTARGET = 256,
            SFGAO_CAPABILITYMASK = 375,
            SFGAO_LINK = 65536,
            SFGAO_SHARE = 131072,
            SFGAO_READONLY = 262144,
            SFGAO_GHOSTED = 524288,
            SFGAO_DISPLAYATTRMASK = 983040,
            SFGAO_FILESYSANCESTOR = 268435456,
            SFGAO_FOLDER = 536870912,
            SFGAO_FILESYSTEM = 1073741824,
            SFGAO_HASSUBFOLDER = -2147483648,
            SFGAO_CONTENTSMASK = -2147483648,
            SFGAO_VALIDATE = 16777216,
            SFGAO_REMOVABLE = 33554432,
            SFGAO_COMPRESSED = 67108864
        }
        #endregion

        #region IExtractImage Enumerations
        enum EIEIFLAG : int
        {
            IEIFLAG_ASYNC = 0x0001, // ask the extractor if it supports ASYNC extract (free threaded)
            IEIFLAG_CACHE = 0x0002, // returned from the extractor if it does NOT cache the thumbnail
            IEIFLAG_ASPECT = 0x0004, // passed to the extractor to beg it to render to the aspect ratio of the supplied rect
            IEIFLAG_OFFLINE = 0x0008, // if the extractor shouldn't hit the net to get any content neede for the rendering
            IEIFLAG_GLEAM = 0x0010, // does the image have a gleam ? this will be returned if it does
            IEIFLAG_SCREEN = 0x0020, // render as if for the screen (this is exlusive with IEIFLAG_ASPECT )
            IEIFLAG_ORIGSIZE = 0x0040, // render to the approx size passed, but crop if neccessary
            IEIFLAG_NOSTAMP = 0x0080, // returned from the extractor if it does NOT want an icon stamp on the thumbnail
            IEIFLAG_NOBORDER = 0x0100, // returned from the extractor if it does NOT want an a border around the thumbnail
            IEIFLAG_QUALITY = 0x0200 // passed to the Extract method to indicate that a slower, higher quality image is desired, re-compute the thumbnail
        }
        #endregion

        #region ShellFolder Structures
        [StructLayoutAttribute(LayoutKind.Sequential, Pack = 4, Size = 0, CharSet = CharSet.Auto)]
        private struct STRRET_CSTR
        {
            public ESTRRET uType;
            [MarshalAs(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst = 520)]
            public byte[] cStr;
        }

        [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Auto)]
        private struct STRRET_ANY
        {
            [FieldOffset(0)]
            public ESTRRET uType;
            [FieldOffset(4)]
            public IntPtr pOLEString;
        }

        [StructLayoutAttribute(LayoutKind.Sequential)]
        private struct SIZE
        {
            public int cx;
            public int cy;
        }
        #endregion

        #region COM Interop for IEnumIDList
        [ComImportAttribute()]
        [GuidAttribute("000214F2-0000-0000-C000-000000000046")]
        [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
        //helpstring("IEnumIDList interface")
        private interface IEnumIDList
        {
            [PreserveSig]
            int Next(
            int celt,
            ref IntPtr rgelt,
            out int pceltFetched);

            void Skip(
            int celt);

            void Reset();

            void Clone(
            ref IEnumIDList ppenum);
        };
        #endregion

        #region COM Interop for IShellFolder
        [ComImportAttribute()]
        [GuidAttribute("000214E6-0000-0000-C000-000000000046")]
        [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
        //helpstring("IShellFolder interface")
        private interface IShellFolder
        {
            void ParseDisplayName(
            IntPtr hwndOwner,
            IntPtr pbcReserved,
            [MarshalAs(UnmanagedType.LPWStr)] string lpszDisplayName,
            out int pchEaten,
            out IntPtr ppidl,
            out int pdwAttributes
            );

            void EnumObjects(
            IntPtr hwndOwner,
            [MarshalAs(UnmanagedType.U4)] ESHCONTF grfFlags,
            ref IEnumIDList ppenumIDList
            );

            void BindToObject(
            IntPtr pidl,
            IntPtr pbcReserved,
            ref Guid riid,
            ref IShellFolder ppvOut
            );

            void BindToStorage(
            IntPtr pidl,
            IntPtr pbcReserved,
            ref Guid riid,
            IntPtr ppvObj
            );

            [PreserveSig]
            int CompareIDs(
            IntPtr lParam,
            IntPtr pidl1,
            IntPtr pidl2
            );

            void CreateViewObject(
            IntPtr hwndOwner,
            ref Guid riid,
            IntPtr ppvOut
            );

            void GetAttributesOf(
            int cidl,
            IntPtr apidl,
            [MarshalAs(UnmanagedType.U4)] ref ESFGAO rgfInOut
            );

            /*void GetUIObjectOf(
            IntPtr hwndOwner,
            int cidl,
            ref IntPtr apidl,
            ref Guid riid,
            out int prgfInOut,
            ref IExtractImage ppvOut
            //ref IUnknown ppvOut
            );*/
            // Retrieves an OLE interface that can be used to carry out actions on the specified file objects or folders. Return value: error code, if any
            [PreserveSig()]
            uint GetUIObjectOf(
                IntPtr hwndOwner,       // Handle to the owner window that the client should specify if it displays a dialog box or message box.
                int cidl,           // Number of file objects or subfolders specified in the apidl parameter. 
                [In(), MarshalAs(UnmanagedType.LPArray)] IntPtr[]
                apidl,          // Address of an array of pointers to ITEMIDLIST structures, each of which uniquely identifies a file object or subfolder relative to the parent folder.
                [In()]
            ref Guid riid,      // Identifier of the COM interface object to return.
                IntPtr rgfReserved,     // Reserved. 
                [MarshalAs(UnmanagedType.Interface)]
            out object ppv);    // Pointer to the requested interface.

            void GetDisplayNameOf(
            IntPtr pidl,
            [MarshalAs(UnmanagedType.U4)] ESHGDN uFlags,
            ref STRRET_CSTR lpName
            );

            void SetNameOf(
            IntPtr hwndOwner,
            IntPtr pidl,
            [MarshalAs(UnmanagedType.LPWStr)] string lpszName,
            [MarshalAs(UnmanagedType.U4)] ESHCONTF uFlags,
            ref IntPtr ppidlOut
            );

        };

        #endregion

        #region COM Interop for IExtractImage
        [ComImportAttribute()]
        [GuidAttribute("BB2E617C-0920-11d1-9A0B-00C04FC2D6C1")]
        [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
        //helpstring("IExtractImage"),
        private interface IExtractImage
        {
            void GetLocation(
            [Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszPathBuffer,
            int cch,
            ref int pdwPriority,
            ref SIZE prgSize,
            int dwRecClrDepth,
            ref int pdwFlags
            );

            void Extract(
            out IntPtr phBmpThumbnail
            );
        }

        #endregion

        #region UnManagedMethods for IShellFolder
        private class UnManagedMethods
        {
            [DllImport("shell32", CharSet = CharSet.Auto)]
            internal extern static int SHGetDesktopFolder(out IShellFolder ppshf);
        }
        #endregion

    }
}
