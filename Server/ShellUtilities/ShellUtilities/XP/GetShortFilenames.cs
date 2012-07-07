using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace FatAttitude
{
    public static class GetShortFilenames
    {

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        static extern int GetShortPathName(
                 [MarshalAs(UnmanagedType.LPTStr)]
                   string path,
                 [MarshalAs(UnmanagedType.LPTStr)]
                   StringBuilder shortPath,
                 int shortPathLength
                 );

        public static string GetShortFileName(string strPath)
        {
            StringBuilder shortPath = new StringBuilder(255);
            GetShortPathName(strPath, shortPath, shortPath.Capacity);
            return shortPath.ToString();
        }


    }
}
