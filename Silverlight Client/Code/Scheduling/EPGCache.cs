using System;
using System.Net;
using System.Windows;
using System.Collections.Generic;
using System.Collections;
using System.Xml;
using System.Xml.Serialization;
using System.Text;
using System.IO;
using CommonEPG;
using RemotePotatoServer; // for XML serialization
namespace SilverPotato
{
    public static class EPGCache
    {

        #region Static Methods - Check the cache etc.
        static EPGCache()
        {

        }

        #region Cache Retrieve / Store
        public static bool isCached(EPGRequest rq)
        {
            return (FileManager.IsolatedStorageFileExists(rq.CacheUniqueFilename));
        }
        public static List<TVProgramme> getFromCache(EPGRequest rq)
        {
            if (!isCached(rq)) return new List<TVProgramme>();

            string txtZipString = FileManager.ReadTextFileFromIsolatedStorage(rq.CacheUniqueFilename);
            return ZipStringToTVProgrammesList(txtZipString);
        }
        public static void storeInCache(EPGRequest rq, string zipString)
        {
            if (string.IsNullOrEmpty(zipString)) return;

            FileManager.WriteFileToIsolatedStorage(rq.CacheUniqueFilename, zipString);
        }
        public static void pruneOldEPGCacheFiles()
        {
            string[] files = FileManager.GetAllFilesMatchingPattern("*.slice");

            int pruneCounter = 0;
            foreach (string fn in files)
            {
                if (fn.Length > 9)
                {
                    try
                    {
                        DateTime dt = DateTime.Parse(fn.Substring(0, 4) + "-" + fn.Substring(5, 2) + fn.Substring(8, 2));
                        if (dt.Date < DateTime.Now.Date) // OLD CACHE FILE
                        {
                            FileManager.DeleteFileFromIsolatedStorage(fn);
                            pruneCounter++;
                        }
                    }
                    catch { }
                }

            }

            Functions.WriteLineToLogFile("Pruned " + pruneCounter.ToString() + " old EPG slice(s) from cache.");
        }

        #endregion

        public static List<TVProgramme> ZipStringToTVProgrammesList(string strOut)
        {
            if (string.IsNullOrEmpty(strOut))
            {
                Functions.WriteLineToLogFile("ZipString: Blank string returned from server.");
                return new List<TVProgramme>();
            }

            // strOut is a base64 string encoded string
            // which decodes to a byte[] array 
            // which unzips to a UTF-8 encoded string which deserialises

            if (Settings.ZipDataStreams)
            {
                if (!ZipManager.UnzipString(ref strOut))
                {
                    ErrorManager.DisplayAndLogError("Could not unzip downloaded TV Programmes from server.");
                    return null;
                }
            }

            List<TVProgramme> theProgrammes = new List<TVProgramme>();
            XmlSerializer serializer = new XmlSerializer(theProgrammes.GetType());
            StringReader sr = new StringReader(strOut);
            theProgrammes = (List<TVProgramme>)serializer.Deserialize(sr);

            return theProgrammes;
        }
        static List<TVServiceSlice> ProgrammeListToTVServiceSlices(List<TVProgramme> theProgrammes)
        {
            List<TVServiceSlice> output = new List<TVServiceSlice>();

            // Cut into slices per TV service, assume ordered by channel

            string trackSvcID = "";
            TVServiceSlice currentSlice = null;
            foreach (TVProgramme tvp in theProgrammes)
            {
                if (tvp.ServiceID != trackSvcID)
                {
                    // Store current slice
                    if (currentSlice != null)
                        output.Add(currentSlice);

                    // We're onto a new channel/service, so create a new slice for it
                    currentSlice = new TVServiceSlice();
                    currentSlice.TVServiceID = tvp.ServiceID;
                    currentSlice.LocalDate = tvp.StopTimeDT().Date; // assume first programme stops in the slice date! (little dangerous)

                    trackSvcID = tvp.ServiceID;
                }

                if (currentSlice != null)
                {
                    currentSlice.TVProgrammes.Add(tvp);
                }
            }

            // Add final slice
            if (currentSlice != null)
                output.Add(currentSlice);

            // Return output
            return output;
        }
        #endregion
    }



}
