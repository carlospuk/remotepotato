using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;

namespace RemotePotatoServer
{
    public class MCLibraryFolderHelper
    {

        public MCLibraryFolderHelper()
        {

        }

        public enum Libraries { Image, Movie, Video }

        public List<string> MediaCenterLibraryFolders(Libraries library)
        {
            List<string> output = new List<string>();

            RegistryKey rkRoot = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Media Center\\MediaFolders\\" + library.ToString() + "\\", false);
            if (rkRoot == null)
                return output; // NO folders

            object oFolderCount = rkRoot.GetValue("FolderCount");
            if (oFolderCount == null) return output; // No folders

            int folderCount = (int)oFolderCount;
            if (folderCount > 0)
            {
                for (int i = 0; i < folderCount; i++)
                {
                    object oFolderName = rkRoot.GetValue("Folder" + i.ToString());
                    if (oFolderName != null)
                    {
                        string sFolderName = (string)oFolderName;
                        output.Add(sFolderName);
                    }
                }
            }

            return output;
        }

    }
}
