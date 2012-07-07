using System;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using DirectShowLib;

namespace FatAttitude.WTVTranscoder
{
    /// <summary>Publishes filter graphs for live or post debugging.</summary>
    public sealed class GraphPublisher : IDisposable
    {
        /// <summary>Cookie used to identify the graph in the ROT.</summary>
        private RunningObjectTableCookie _cookie;

        /// <summary>Publishes a graph to the running object table for GraphEdit to analyze.</summary>
        /// <param name="graph">The graph to be published.</param>
        public GraphPublisher(IGraphBuilder graph) :
            this(graph, null) { }

        /// <summary>Publishes a graph to the running object table for GraphEdit to analyze and to a GRF file.</summary>
        /// <param name="graph">The graph to be published.</param>
        /// <param name="path">The path to the file to which the GRF file should be written.</param>
        public GraphPublisher(IGraphBuilder graph, string path) :
            this(graph, path, true) { }

        /// <summary>Publishes a graph to the running object table for GraphEdit to analyze and to a GRF file.</summary>
        /// <param name="graph">The graph to be published.</param>
        /// <param name="path">The path to the file to which the GRF file should be written.</param>
        /// <param name="useRot">Whether the graph should be added to the running object table.</param>
        public GraphPublisher(IGraphBuilder graph, string path, bool useRot)
        {
            if (graph == null) throw new ArgumentNullException("graph");
            if (path != null) SaveGraphToFile(graph, path);
            if (useRot) _cookie = AddGraphToRot(graph);
        }

        /// <summary>Disposes of the GraphPublisher, removing the graph from the ROT if it was previously added.</summary>
        void IDisposable.Dispose()
        {
            if (_cookie != null)
            {
                _cookie.Dispose();
                _cookie = null;
            }
        }

        /// <summary>Stores the unmanaged cookie value supplied by the ROT.</summary>
        private class RunningObjectTableCookie : IDisposable
        {
            /// <summary>The cookie value.</summary>
            private int _value;
            /// <summary>Whether this is a valid cookie.</summary>
            private bool _valid;

            /// <summary>Initializes the cookie.</summary>
            /// <param name="value">The value of the cookie.</param>
            internal RunningObjectTableCookie(int value)
            {
                _value = value;
                _valid = true;
            }

            /// <summary>Finalizes the cookie, removing the graph from the ROT.</summary>
            ~RunningObjectTableCookie() { Dispose(false); }

            /// <summary>Removes the graph from the ROT.</summary>
            public void Dispose()
            {
                GC.SuppressFinalize(this);
                Dispose(true);
            }

            /// <summary>Removes the graph from the ROT.</summary>
            /// <param name="disposing">Whether this is be called from the IDisposable.Dispose method.</param>
            private void Dispose(bool disposing)
            {
                if (_valid)
                {
                    RemoveGraphFromRot(this);
                    _valid = false;
                    _value = -1;
                }
            }

            /// <summary>Gets or sets the cookie value.</summary>
            public int Value { get { return _value; } }
            /// <summary>Gets or sets whether this is a valid cookie.</summary>
            internal bool IsValid { get { return _valid; } set { _valid = value; } }
        }

        /// <summary>Adds a graph to the ROT.</summary>
        /// <param name="graph">The graph to be added.</param>
        /// <returns>A cookie that can be used to remove this graph from the ROT.</returns>
        private static RunningObjectTableCookie AddGraphToRot(IGraphBuilder graph)
        {
            if (graph == null) throw new ArgumentNullException("graph");
            IRunningObjectTable rot = null;
            IMoniker moniker = null;
            try
            {
                // Get the ROT.
                rot = GetRunningObjectTable(0);

                // Create a moniker for the grpah
                int pid;
                using (Process p = Process.GetCurrentProcess())
                {
                    pid = p.Id;
                }
                IntPtr unkPtr = Marshal.GetIUnknownForObject(graph);
                string item = string.Format("FilterGraph {0} pid {1}", ((int)unkPtr).ToString("x8"), pid.ToString("x8"));
                Marshal.Release(unkPtr);
                moniker = CreateItemMoniker("!", item);

                // Registers the graph in the running object table
                int cookieValue = rot.Register(ROTFLAGS_REGISTRATIONKEEPSALIVE, graph, moniker);
                return new RunningObjectTableCookie(cookieValue);
            }
            finally
            {
                // Releases the COM objects
                if (moniker != null) while (Marshal.ReleaseComObject(moniker) > 0) ;
                if (rot != null) while (Marshal.ReleaseComObject(rot) > 0) ;
            }
        }

        /// <summary>Removes the graph from the running object table.</summary>
        /// <param name="cookie">The cookie value for the registered graph.</param>
        private static void RemoveGraphFromRot(RunningObjectTableCookie cookie)
        {
            if (!cookie.IsValid) throw new ArgumentException("cookie");
            IRunningObjectTable rot = null;
            try
            {
                // Get the running object table and revoke the cookie
                rot = GetRunningObjectTable(0);
                rot.Revoke(cookie.Value);
                cookie.IsValid = false;
            }
            finally
            {
                // Release the ROT
                if (rot != null) while (Marshal.ReleaseComObject(rot) > 0) ;
            }
        }

        /// <summary>Indicates a strong registration for the object.</summary>
        private const int ROTFLAGS_REGISTRATIONKEEPSALIVE = 1;

        /// <summary>Supplies a pointer to the IRunningObjectTable interface on the local Running Object Table.</summary>
        /// <param name="reserved">Reserved for future use; must be zero.</param>
        /// <returns>Address of IRunningObjectTable* pointer variable that receives the interface pointer to the local ROT.</returns>
        [DllImport("ole32.dll", ExactSpelling = true, PreserveSig = false)]
        private static extern IRunningObjectTable GetRunningObjectTable([In] uint reserved);

        /// <summary>Creates an item moniker that identifies an object within a containing object.</summary>
        /// <param name="lpszDelim">
        /// Pointer to a wide character string (two bytes per character) zero-terminated string containing 
        /// the delimiter (typically "!") used to separate this item's display name from the display name 
        /// of its containing object.
        /// </param>
        /// <param name="lpszItem">
        /// Pointer to a zero-terminated string indicating the containing object's name for the object being 
        /// identified. This name can later be used to retrieve a pointer to the object in a call to IOleItemContainer::GetObject.
        /// </param>
        /// <returns>
        /// Address of IMoniker* pointer variable that receives the interface pointer to the item moniker.
        /// </returns>
        [DllImport("ole32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, PreserveSig = false)]
        private static extern IMoniker CreateItemMoniker([In] string lpszDelim, [In] string lpszItem);

        /// <summary>
        /// This function creates a new compound file storage object using the OLE-provided 
        /// compound file implementation for the IStorage interface. 
        /// </summary>
        /// <param name="pwcsName">
        /// Pointer to the path of the compound file to create. It is passed uninterpreted to the file system. 
        /// This can be a relative name or NULL. If NULL, a temporary compound file is allocated with a unique name. 
        /// </param>
        /// <param name="grfMode">
        /// Specifies the access mode to use when opening the new storage object. For more information, see 
        /// the STGM enumeration. If the caller specifies transacted mode together with STGM_CREATE or 
        /// STGM_CONVERT, the overwrite or conversion takes place at the time the storage object is 
        /// opened and therefore is not revertible.
        /// </param>
        /// <param name="reserved">Reserved for future use; set to zero.</param>
        /// <returns>The opened IStorage for the document file.</returns>
        [DllImport("ole32.dll", PreserveSig = false)]
        private static extern IStorage StgCreateDocfile([MarshalAs(UnmanagedType.LPWStr)]string pwcsName, [In] int grfMode, [In] int reserved);

        /// <summary>Indicates that an existing storage object or stream should be removed before the new one replaces it.</summary>
        private const long STGM_CREATE = 0x00001000L;
        /// <summary>In transacted mode, changes are buffered and written only if an explicit commit operation is called.</summary>
        private const long STGM_TRANSACTED = 0x00010000L;
        /// <summary>Lets you save changes to the object, but does not permit access to its data.</summary>
        private const long STGM_WRITE = 0x00000001L;
        /// <summary>Lets you both access and modify an object's data.</summary>
        private const long STGM_READWRITE = 0x00000002L;
        /// <summary>Prevents others from subsequently opening the object in any mode.</summary>
        private const long STGM_SHARE_EXCLUSIVE = 0x00000010L;

        /// <summary>Saves a graph to a GRF file.</summary>
        /// <param name="graph">The graph to be saved.</param>
        /// <param name="path">Path to the target GRF file.</param>
        private static void SaveGraphToFile(IGraphBuilder graph, string path)
        {
            
            using (DisposalCleanup dc = new DisposalCleanup())
            {
                // Get the graph's persist stream interface
                IPersistStream ps = (IPersistStream)graph;

                // Create the file to which the graph should be stored
                IStorage graphStorage = StgCreateDocfile(path, (int)(STGM_CREATE | STGM_TRANSACTED | STGM_READWRITE | STGM_SHARE_EXCLUSIVE), 0);
                dc.Add(graphStorage);

                // Create the movie graph stream
                IStream stream;
                int hr = graphStorage.CreateStream("ActiveMovieGraph", (STGM)(STGM.Write | STGM.Create | STGM.ShareExclusive), 0, 0, out stream);
                dc.Add(stream);

                // Save out the graph and commit it
                ps.Save(stream, true);
                graphStorage.Commit(0);
            }
        }
    }
}
