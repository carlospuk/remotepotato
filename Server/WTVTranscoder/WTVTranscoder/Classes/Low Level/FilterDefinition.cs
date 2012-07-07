using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DirectShowLib;
using DirectShowLib.SBE;
using WindowsMediaLib;

namespace FatAttitude.WTVTranscoder
{
    /// <summary>
    /// Represents an installed DirectShow filter or codec
    /// </summary>
    public class FilterDefinition
    {
        public string DevicePath { get; set; }
        public Guid CLSID {get; set;}
        public string DisplayString { get; set; }

        // Constructors
        public FilterDefinition() { 
            CLSID = Guid.Empty;
            DevicePath = string.Empty;
            DisplayString = string.Empty;
        }
        public FilterDefinition(Guid _CLSID, string _DisplayString)
            : this()
        {
            CLSID = _CLSID;
            DisplayString = _DisplayString;
        }
        public FilterDefinition(string _DevicePath, string _DisplayString)
            : this()
        {
            DevicePath = _DevicePath;
            DisplayString = _DisplayString;
        }

        // Helper Property
        bool HasDevicePath
        {
            get
            {
                return (!string.IsNullOrEmpty(DevicePath));
            }
        }


        #region Static Methods
        // Add to filter graph
        public static IBaseFilter AddToFilterGraph(FilterDefinition fd, ref IGraphBuilder graph)
        {
            return AddToFilterGraph(fd, ref graph, fd.DisplayString);
        }
        public static IBaseFilter AddToFilterGraph(FilterDefinition fd, ref IGraphBuilder graph, string _graphDisplayName)
        {
            if (fd.HasDevicePath)
                return FilterGraphTools.AddFilterByDevicePath(graph, fd.DevicePath, _graphDisplayName);
            else
                if (fd.CLSID != Guid.Empty)
                    return FilterGraphTools.AddFilterFromClsid(graph, fd.CLSID, _graphDisplayName);
                else
                    return null;
        }

        // Helper property - is the system in 32 bit mode
        private static bool isRunningIn32BitMode
        {
            get
            {
                return (IntPtr.Size == 4);
            }
        }
        #endregion
    }
}
   