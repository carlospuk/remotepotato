using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RemotePotatoServer.Properties;
using System.IO;

namespace RemotePotatoServer
{
    public static class FileCache
    {
        static FileCache()
        {
            CachedBinaryFiles = new Dictionary<string, byte[]>();
            CachedTextFiles = new Dictionary<string, string>();

            CacheExceptedExtensions = new List<string>();

            // CACHE EXCEPTIONS
            CacheExceptedExtensions.Add(".m3u8");
            CacheExceptedExtensions.Add(".ts");
        }

        static List<string> CacheExceptedExtensions;

        private static Dictionary<string, byte[]> CachedBinaryFiles;
        private static Dictionary<string, string> CachedTextFiles;

        public static void FlushCache(bool flushBinaryCache, bool flushTextCache)
        {
            if (flushBinaryCache) CachedBinaryFiles.Clear();
            if (flushTextCache) CachedTextFiles.Clear();
        }
        public static void WriteCacheInfoToLog()
        {
            Functions.WriteLineToLogFile("Displaying Cache....");
            Functions.WriteLineToLogFile("Text Files:");
            foreach (string txtFN in CachedTextFiles.Keys)
            {
                Functions.WriteLineToLogFile(txtFN);
            }

            Functions.WriteLineToLogFile("Binary Files:");
            foreach (string txtFN in CachedBinaryFiles.Keys)
            {
                Functions.WriteLineToLogFile(txtFN);
            }

            Functions.WriteLineToLogFile("Cache display done.");
        }
        public static byte[] ReadBinaryFile(string filePath)
        {
            filePath = ConvertRelativePathToAbsolute(filePath);

            // Exception?
            FileInfo f = new FileInfo(filePath);
            bool IsExcepted = CacheExceptedExtensions.Contains(f.Extension);
            if (! IsExcepted)
            {
                if (CachedBinaryFiles.ContainsKey(filePath))
                {
                    if (Settings.Default.DebugCache)
                        Functions.WriteLineToLogFile("CACHE: Reading cached binary file: " + filePath);

                    return CachedBinaryFiles[filePath];
                }
            }
            else
            {
                if (Settings.Default.DebugCache)
                    Functions.WriteLineToLogFile("CACHE: Bypassing binary cache for excepted file: " + filePath);
            }


            if (!File.Exists(filePath))
            {
                return new byte[0];
            }

            if (Settings.Default.DebugCache)
                Functions.WriteLineToLogFile("CACHE: Reading un-cached binary file: " + filePath);

            FileStream fs = File.OpenRead(filePath);
            BinaryReader reader = new BinaryReader(fs);
            byte[] bytes = new byte[fs.Length];
            int read;
            while ((read = reader.Read(bytes, 0, bytes.Length)) != 0)
            { }
            reader.Close();
            fs.Close();
            
            // Add to cache?
            if (  (!IsExcepted) && (Settings.Default.CacheBinaryFiles) && (bytes.Length < Settings.Default.CacheBinaryFileMaxLengthBytes))
            {
                CachedBinaryFiles.Add(filePath, bytes);

                if (Settings.Default.DebugCache)
                    Functions.WriteLineToLogFile("CACHE: Adding binary file: " + filePath + "  (Count:" + CachedBinaryFiles.Count.ToString() + ")");
            }

            return bytes;
        }
        public static string ReadSkinTextFile(string filename)
        {
            string filePath = "static\\skins\\" + Themes.ActiveThemeName + "\\" + filename;
            return ReadTextFile(filePath);
        }
        public static string ReadTextFile(string filePath)
        {
            filePath = ConvertRelativePathToAbsolute(filePath);

            // Exception?
            FileInfo f = new FileInfo(filePath);
            bool IsExcepted = CacheExceptedExtensions.Contains(f.Extension);
            if (! IsExcepted)
            {
                if (CachedTextFiles.ContainsKey(filePath))
                {
                    if (Settings.Default.DebugCache)
                        Functions.WriteLineToLogFile("CACHE: Reading cached text file: " + filePath);

                    return CachedTextFiles[filePath];
                }
            }
            else
            {
                if (Settings.Default.DebugCache)
                    Functions.WriteLineToLogFile("CACHE: Bypassing text cache for excepted file: " + filePath);
            }

            if (!File.Exists(filePath))
            {
                if (Settings.Default.DebugAdvanced)
                    Functions.WriteLineToLogFile("FileCache: Text File Not Found: " + filePath);
                return "";
            }

            if (Settings.Default.DebugCache)
                Functions.WriteLineToLogFile("CACHE: Reading un-cached text file: " + filePath);

            string input = ReadTextFileFromDisk(filePath);

            // Add to cache
            if ( (! IsExcepted) && (Settings.Default.CacheTextFiles) )
            {
                CachedTextFiles.Add(filePath, input);

                if (Settings.Default.DebugCache)
                    Functions.WriteLineToLogFile("CACHE: Adding text file: " + filePath + "  (Count:" + CachedTextFiles.Count.ToString() + ")");
            }

            return input;
        }

        // Helper
        static string ConvertRelativePathToAbsolute(string filePath)
        {
            if (!filePath.Contains(":"))
            {
                return Path.Combine(Functions.AppDataFolder, filePath);
            }
            else
                return filePath;
        }

        public static bool WriteTextFileToDisk(string filePath, string txtContent)
        {
            filePath = ConvertRelativePathToAbsolute(filePath);

            try
            {
                TextWriter tw = new StreamWriter(filePath);
                tw.Write(txtContent);
                tw.Close();
                return true;
            }
            catch (Exception e)
            {
                Functions.WriteLineToLogFile("Could not write file " + filePath + " to disk:");
                Functions.WriteExceptionToLogFile(e);
                return false;
            }
        }
        /// <summary>
        /// Reads an un-cached file from disk
        /// </summary>
        public static string ReadTextFileFromDisk(string filePath)
        {
            filePath = ConvertRelativePathToAbsolute(filePath);

            string input;
            try
            {
                StreamReader sr;
                sr = File.OpenText(filePath);
                input = sr.ReadToEnd();
                sr.Close();
                return input;
            }
            catch (Exception e)
            {
                Functions.WriteLineToLogFile("Could not read file " + filePath + " from disk:");
                Functions.WriteExceptionToLogFile(e);
            }

            return "";
        }
        /// <summary>
        /// Read a binary file and bypass caching
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static byte[] ReadBinaryFileFromDisk(string filePath)
        {
            filePath = ConvertRelativePathToAbsolute(filePath);

            if (!File.Exists(filePath))
            {
                return new byte[0];
            }

            //FileStream fs = File.OpenRead(filePath);
            FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            BinaryReader reader = new BinaryReader(fs);
            byte[] bytes = new byte[fs.Length];
            int read;
            while ((read = reader.Read(bytes, 0, bytes.Length)) != 0)
            { }
            reader.Close();
            fs.Close();

            return bytes;
        }

    }
}
