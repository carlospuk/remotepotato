using System;
using System.Net;
using System.Windows;
using System.Collections.Generic;
using System.Collections;
using System.Xml;
using System.Xml.Serialization;
using System.Windows.Media.Imaging;
using System.Text;
using System.IO;
using System.Threading;
using CommonEPG;
using RemotePotatoServer; // for XML serialization
using System.ComponentModel;

namespace SilverPotato
{
    public static class LogoCache
    {

        
        static LogoCache()
        {

        }

        #region Cache Retrieve / Store
        public static bool isCached(Uri theUri)
        {
            // Local images are ignored
            if (! theUri.IsAbsoluteUri) return false;

            return (isCached(UriToCacheFilename(theUri)));
        }
        static bool isCached(string filename)
        {
            return (FileManager.IsolatedStorageFileExists(filename));
        }

        public static MemoryStream getFromCache(Uri theUri)
        {
            return getFromCache(UriToCacheFilename(theUri));
        }
        static MemoryStream getFromCache(string fileName)
        {
            if (!isCached(fileName)) return null;
            return FileManager.ReadStreamFromIsolatedStorage(fileName);
        }




        public static void storeInCache(Uri theUri, MemoryStream ms)
        {
            FileManager.WriteStreamToIsolatedStorage(UriToCacheFilename(theUri), ms);
        }
        public static void clearCachedLogos()
        {
            string[] files = FileManager.GetAllFilesMatchingPattern("*.logo");

            int pruneCounter = 0;
            foreach (string fn in files)
            {
                FileManager.DeleteFileFromIsolatedStorage(fn);
                pruneCounter++;
            }

            Functions.WriteLineToLogFile("Removed " + pruneCounter.ToString() + " logos from cache.");
        }

        #endregion

        // Uris
        static string UriToCacheFilename(Uri u)
        {
            if (!u.IsAbsoluteUri) return "";

            string strFN = u.LocalPath + "$" + u.Query;
            

            strFN = strFN.Replace(@"http://", "");

            strFN = strFN.Replace(@"/", "#");
            strFN = strFN.Replace(@"\", "#");
            strFN = strFN.Replace(@":", "#");
            strFN = strFN.Replace(@"?", "$");
            strFN = strFN.Replace(@"*", "%");

            if (strFN.Length > 100)
                strFN = strFN.Substring(strFN.Length - 100, 100);  // use last portion of the path

            strFN = strFN + ".uricached";


            return strFN;
        }
    }



}
