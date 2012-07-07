using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace FatAttitude
{
public class LegacyThumbnailExtractor : IDisposable
{

    public LegacyThumbnailExtractor(ThumbnailSizes thumbSize)
    {
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
    ~LegacyThumbnailExtractor()
    {
        Dispose();
    }

    [Flags]
    private enum ESTRRET
    {
        STRRET_WSTR = 0,
        STRRET_OFFSET = 1,
        STRRET_CSTR = 2
    }

    [Flags]
    private enum ESHCONTF
    {
        SHCONTF_FOLDERS = 32,
        SHCONTF_NONFOLDERS = 64,
        SHCONTF_INCLUDEHIDDEN = 128,
    }

    [Flags]
    private enum ESHGDN
    {
        SHGDN_NORMAL = 0,
        SHGDN_INFOLDER = 1,
        SHGDN_FORADDRESSBAR = 16384,
        SHGDN_FORPARSING = 32768
    }

    [Flags]
    private enum ESFGAO
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
        SFGAO_COMPRESSED = 67108864,
    }

    private enum EIEIFLAG
    {
        IEIFLAG_ASYNC = 1,
        IEIFLAG_CACHE = 2,
        IEIFLAG_ASPECT = 4,
        IEIFLAG_OFFLINE = 8,
        IEIFLAG_GLEAM = 16,
        IEIFLAG_SCREEN = 32,
        IEIFLAG_ORIGSIZE = 64,
        IEIFLAG_NOSTAMP = 128,
        IEIFLAG_NOBORDER = 256,
        IEIFLAG_QUALITY = 512
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 0, CharSet = CharSet.Auto)]
    private struct STRRET_CSTR
    {
        public ESTRRET uType;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 520)]
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

    [ComImport(), Guid("00000000-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IUnknown
    {

        [PreserveSig()]
        IntPtr QueryInterface(ref Guid riid, ref IntPtr pVoid);

        [PreserveSig()]
        IntPtr AddRef();

        [PreserveSig()]
        IntPtr Release();
    }

    [ComImportAttribute()]
    [GuidAttribute("00000002-0000-0000-C000-000000000046")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMalloc
    {

        [PreserveSig()]
        IntPtr Alloc(int cb);

        [PreserveSig()]
        IntPtr Realloc(IntPtr pv, int cb);

        [PreserveSig()]
        void Free(IntPtr pv);

        [PreserveSig()]
        int GetSize(IntPtr pv);

        [PreserveSig()]
        int DidAlloc(IntPtr pv);

        [PreserveSig()]
        void HeapMinimize();
    }

    [ComImportAttribute()]
    [GuidAttribute("000214F2-0000-0000-C000-000000000046")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IEnumIDList
    {

        [PreserveSig()]
        int Next(int celt, ref IntPtr rgelt, ref int pceltFetched);

        void Skip(int celt);

        void Reset();

        void Clone(ref IEnumIDList ppenum);
    }

    [ComImportAttribute()]
    [GuidAttribute("000214E6-0000-0000-C000-000000000046")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellFolder
    {

        void ParseDisplayName(IntPtr hwndOwner, IntPtr pbcReserved,
          [MarshalAs(UnmanagedType.LPWStr)]string lpszDisplayName,
          ref int pchEaten, ref IntPtr ppidl, ref int pdwAttributes);

        void EnumObjects(IntPtr hwndOwner,
          [MarshalAs(UnmanagedType.U4)]ESHCONTF grfFlags,
          ref IEnumIDList ppenumIDList);

        void BindToObject(IntPtr pidl, IntPtr pbcReserved, ref Guid riid,
          ref IShellFolder ppvOut);

        void BindToStorage(IntPtr pidl, IntPtr pbcReserved, ref Guid riid, IntPtr ppvObj);

        [PreserveSig()]
        int CompareIDs(IntPtr lParam, IntPtr pidl1, IntPtr pidl2);

        void CreateViewObject(IntPtr hwndOwner, ref Guid riid,
          IntPtr ppvOut);

        void GetAttributesOf(int cidl, IntPtr apidl,
          [MarshalAs(UnmanagedType.U4)]ref ESFGAO rgfInOut);

        //void GetUIObjectOf(IntPtr hwndOwner, int cidl, IntPtr apidl, Guid riid, ref int prgfInOut, out IUnknown ppvOut);

        // Retrieves an OLE interface that can be used to carry out actions on the 
        // specified file objects or folders. Return value: error code, if any
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

        /* APPARENTLY OK IF cidl=1
         * void GetUIObjectOf(
          IntPtr hwndOwner,
          UInt32 cidl,
          ref    IntPtr apidl,
          [In] ref Guid riid,
          UInt32 rgfReserved,
          out IntPtr ppv);
        */


        void GetDisplayNameOf(IntPtr pidl,
          [MarshalAs(UnmanagedType.U4)]ESHGDN uFlags,
          ref STRRET_CSTR lpName);

        void SetNameOf(IntPtr hwndOwner, IntPtr pidl,
          [MarshalAs(UnmanagedType.LPWStr)]string lpszName,
          [MarshalAs(UnmanagedType.U4)] ESHCONTF uFlags,
          ref IntPtr ppidlOut);
    }
    [ComImportAttribute(), GuidAttribute("BB2E617C-0920-11d1-9A0B-00C04FC2D6C1"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IExtractImage
    {
        void GetLocation([Out(), MarshalAs(UnmanagedType.LPWStr)] 
      StringBuilder pszPathBuffer, int cch, ref int pdwPriority, ref SIZE prgSize, int dwRecClrDepth, ref int pdwFlags);

        void Extract(ref IntPtr phBmpThumbnail);
    }

    private class UnmanagedMethods
    {

        [DllImport("shell32", CharSet = CharSet.Auto)]
        internal extern static int SHGetMalloc(ref IMalloc ppMalloc);

        [DllImport("shell32", CharSet = CharSet.Auto)]
        internal extern static int SHGetDesktopFolder(ref IShellFolder ppshf);

        [DllImport("shell32", CharSet = CharSet.Auto)]
        internal extern static int SHGetPathFromIDList(IntPtr pidl, StringBuilder pszPath);

        [DllImport("gdi32", CharSet = CharSet.Auto)]
        internal extern static int DeleteObject(IntPtr hObject);

    }


    private IMalloc alloc = null;
    private bool disposed = false;
    private Size _desiredSize = new Size(100, 100);
    private Bitmap _thumbNail;

    public Bitmap ThumbNail
    {
        get
        {
            return _thumbNail;
        }
    }

    public Size DesiredSize
    {
        get { return _desiredSize; }
        set { _desiredSize = value; }
    }

    private IMalloc Allocator
    {
        get
        {
            if (!disposed)
            {
                if (alloc == null)
                {
                    UnmanagedMethods.SHGetMalloc(ref alloc);
                }
            }
            else
            {
                Debug.Assert(false, "Object has been disposed.");
            }
            return alloc;
        }
    }

    public Bitmap GetThumbnail(string fileName)
    {
        if (!File.Exists(fileName) && !Directory.Exists(fileName))
        {
            throw new FileNotFoundException(string.Format("The file '{0}' does not exist", fileName), fileName);
        }
        if (_thumbNail != null)
        {
            _thumbNail.Dispose();
            _thumbNail = null;
        }
        IShellFolder folder = null;
        try
        {
            folder = getDesktopFolder;
        }
        catch (Exception ex)
        {
            throw ex;
        }
        if (folder != null)
        {
            IntPtr pidlMain = IntPtr.Zero;
            try
            {
                int cParsed = 0;
                int pdwAttrib = 0;
                string filePath = Path.GetDirectoryName(fileName);
                folder.ParseDisplayName(IntPtr.Zero, IntPtr.Zero, filePath, ref cParsed, ref pidlMain, ref pdwAttrib);
            }
            catch (Exception ex)
            {
                Marshal.ReleaseComObject(folder);
                throw ex;
            }
            if (pidlMain != IntPtr.Zero)
            {
                Guid iidShellFolder = new Guid("000214E6-0000-0000-C000-000000000046");
                IShellFolder item = null;
                try
                {
                    folder.BindToObject(pidlMain, IntPtr.Zero, ref iidShellFolder, ref item);
                }
                catch (Exception ex)
                {
                    Marshal.ReleaseComObject(folder);
                    Allocator.Free(pidlMain);
                    throw ex;
                }
                if (item != null)
                {
                    IEnumIDList idEnum = null;
                    try
                    {
                        item.EnumObjects(IntPtr.Zero, (ESHCONTF.SHCONTF_FOLDERS | ESHCONTF.SHCONTF_NONFOLDERS | ESHCONTF.SHCONTF_INCLUDEHIDDEN), ref idEnum);
                    }
                    catch (Exception ex)
                    {
                        Marshal.ReleaseComObject(folder);
                        Allocator.Free(pidlMain);
                        throw ex;
                    }
                    if (idEnum != null)
                    {
                        int hRes = 0;
                        IntPtr pidl = IntPtr.Zero;
                        int fetched = 0;
                        bool complete = false;
                        while (!complete)
                        {
                            hRes = idEnum.Next(1, ref pidl, ref fetched);
                            if (hRes != 0)
                            {
                                pidl = IntPtr.Zero;
                                complete = true;
                            }
                            else
                            {
                                if (_getThumbNail(fileName, pidl, item))
                                {
                                    complete = true;
                                }
                            }
                            if (pidl != IntPtr.Zero)
                            {
                                Allocator.Free(pidl);
                            }
                        }
                        Marshal.ReleaseComObject(idEnum);
                    }
                    Marshal.ReleaseComObject(item);
                }
                Allocator.Free(pidlMain);
            }
            Marshal.ReleaseComObject(folder);
        }
        return ThumbNail;
    }

    private bool _getThumbNail(string file, IntPtr pidl, IShellFolder item)
    {
        IntPtr hBmp = IntPtr.Zero;
        IExtractImage extractImage = null;
        IntPtr[] pidl_array = new IntPtr[] { pidl };
        try
        {
            string pidlPath = PathFromPidl(pidl);
            if (Path.GetFileName(pidlPath).ToUpper().Equals(Path.GetFileName(file).ToUpper()))
            {
                object iunk = null;
                Guid iidExtractImage = new Guid("BB2E617C-0920-11d1-9A0B-00C04FC2D6C1");
                item.GetUIObjectOf(IntPtr.Zero, 1, pidl_array, ref iidExtractImage, IntPtr.Zero, out iunk);
                extractImage = (IExtractImage)iunk;
                if (extractImage != null)
                {
                    Console.WriteLine("Got an IExtractImage object!");
                    SIZE sz = new SIZE();
                    sz.cx = DesiredSize.Width;
                    sz.cy = DesiredSize.Height;
                    StringBuilder location = new StringBuilder(260, 260);
                    int priority = 0;
                    int requestedColourDepth = 32;
                    EIEIFLAG flags = EIEIFLAG.IEIFLAG_ASPECT | EIEIFLAG.IEIFLAG_SCREEN;
                    int uFlags = (int)flags;
                    extractImage.GetLocation(location, location.Capacity, ref priority, ref sz, requestedColourDepth, ref uFlags);
                    extractImage.Extract(ref hBmp);
                    if (hBmp != IntPtr.Zero)
                    {
                        _thumbNail = Bitmap.FromHbitmap(hBmp);
                    }
                    Marshal.ReleaseComObject(extractImage);
                    extractImage = null;
                }
                return true;
            }
            else
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            if (hBmp != IntPtr.Zero)
            {
                UnmanagedMethods.DeleteObject(hBmp);
            }
            if (extractImage != null)
            {
                Marshal.ReleaseComObject(extractImage);
            }

           

            throw ex;
        }
    }

    private string PathFromPidl(IntPtr pidl)
    {
        StringBuilder path = new StringBuilder(260, 260);
        int result = UnmanagedMethods.SHGetPathFromIDList(pidl, path);
        if (result == 0)
        {
            return string.Empty;
        }
        else
        {
            return path.ToString();
        }
    }

    private IShellFolder getDesktopFolder
    {
        get
        {
            IShellFolder ppshf = null;
            int r = UnmanagedMethods.SHGetDesktopFolder(ref ppshf);
            return ppshf;
        }
    }

    public void Dispose()
    {
        if (!disposed)
        {
            if (alloc != null)
            {
                Marshal.ReleaseComObject(alloc);
            }
            alloc = null;
            if (_thumbNail != null)
            {
                _thumbNail.Dispose();
            }
            disposed = true;
        }
    }

}
}