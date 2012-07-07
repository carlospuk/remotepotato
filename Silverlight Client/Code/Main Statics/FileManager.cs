using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using System.IO.IsolatedStorage;
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace SilverPotato
{
    public static class FileManager
    {
        static object AccessIsoStoreLock = new object();

        /// <summary>
        /// Write a file to isolated storage - overwrites file if it exists
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="fileContents"></param>
        public static void WriteFileToIsolatedStorage(string fileName, string fileContents)
        {
            try
            {
                lock (AccessIsoStoreLock)
                {

                    using (IsolatedStorageFile isoStore = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        if (isoStore.FileExists(fileName))
                            isoStore.DeleteFile(fileName);

                        using (IsolatedStorageFileStream isoStream = new IsolatedStorageFileStream(fileName, FileMode.Create, isoStore))
                        using (var writer = new StreamWriter(isoStream))
                        {
                            writer.Write(fileContents);
                        }
                    }
                }
            }
            catch (Exception ex) {
                Functions.WriteLineToLogFile("Error writing file to storage:");
                Functions.WriteExceptionToLogFile(ex);
            }
        }
        public static string ReadTextFileFromIsolatedStorage(string fileName)
        {
            try
            {
                lock (AccessIsoStoreLock)
                {

                    using (IsolatedStorageFile isoStore = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        using (IsolatedStorageFileStream isoStream = new IsolatedStorageFileStream(fileName, FileMode.Open, isoStore))

                        using (var reader = new StreamReader(isoStream))
                        {
                            string contents = reader.ReadToEnd();
                            reader.Close();
                            isoStream.Close();
                            return contents;
                        }
                    }
                }
            }
            catch { }
            return "";
        }
        public static void DeleteFileFromIsolatedStorage(string fileName)
        {
            try
            {
                lock (AccessIsoStoreLock)
                {

                    using (IsolatedStorageFile isoStore = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        if (isoStore.FileExists(fileName))
                            isoStore.DeleteFile(fileName);
                    }
                }
            }
            catch (Exception ex)
            {
                Functions.WriteLineToLogFile("Error deleting file from storage:");
                Functions.WriteExceptionToLogFile(ex);
            }
        }
        public static bool IsolatedStorageFileExists(string fileName)
        {
            try
            {
                lock (AccessIsoStoreLock)
                {
                    using (IsolatedStorageFile isoStore = IsolatedStorageFile.GetUserStoreForApplication())
                        return isoStore.FileExists(fileName);

                }
            }
            catch { }
            return false;
        }
        
        public static void WriteStreamToIsolatedStorage(string fileName, MemoryStream ms)
        {
            if (ms == null) return;
            if (ms.Length < 1) return; // No stream

 
                try
                {

                    lock (AccessIsoStoreLock)
                    {

                        IsolatedStorageFile isoStore = IsolatedStorageFile.GetUserStoreForApplication();
                        if (isoStore.FileExists(fileName))
                            isoStore.DeleteFile(fileName);

                        using (IsolatedStorageFileStream isoStream = new IsolatedStorageFileStream(fileName, FileMode.Create, isoStore))
                        {
                            using (var writer = new BinaryWriter(isoStream))
                            {
                                ms.Seek(0, SeekOrigin.Begin);
                                byte[] contents = new byte[ms.Length];
                                // Populate buffer
                                ms.Read(contents, 0, (int)ms.Length);
                                writer.Write(contents, 0, (int)ms.Length);
                                writer.Close();
                            }

                            isoStream.Close();
                        }

                    }
                }
                catch (Exception ex)
                {
                    Functions.WriteLineToLogFile("Error writing binary stream to storage:");
                    Functions.WriteExceptionToLogFile(ex);
                }
            
        }
        public static MemoryStream ReadStreamFromIsolatedStorage(string fileName)
        {
            
            try
            {
                lock (AccessIsoStoreLock)
                {
                    IsolatedStorageFile isoStore = IsolatedStorageFile.GetUserStoreForApplication();

                    using (IsolatedStorageFileStream isoStream = new IsolatedStorageFileStream(fileName, FileMode.Open, isoStore))
                    using (var reader = new BinaryReader(isoStream))
                    {
                        // Get bytes from the stream
                        MemoryStream ms = new MemoryStream();
                        ms.SetLength(isoStream.Length);
                        reader.Read(ms.GetBuffer(), 0, (int)isoStream.Length);
                        ms.Flush();
                        reader.Close();

                        return ms;
                    }

                }
            }
            catch (Exception ex)
            {
                Functions.WriteLineToLogFile("Error reading binary stream from storage:");
                Functions.WriteExceptionToLogFile(ex);
            }

            return null;
        }
     
        public static string[] GetAllFilesMatchingPattern(string pattern)
        {
            try
            {
                lock (AccessIsoStoreLock)
                {
                    using (IsolatedStorageFile isoStore = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        return isoStore.GetFileNames(pattern);
                    }
                }
            }
            catch { }
            return new string[] {};
        }

        

        #region Quotas
        public static void IncreaseStorageAsNecessary()
        {
            

            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (isf.AvailableFreeSpace < Settings.AppStorageTriggerLowThreshold)
                {
                    bool foo = IncreaseStorageBy(Settings.AppStorageStepAmount);
                }
            }
        }
        private static bool IncreaseStorageBy(long spaceRequest)
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
            {
                try
                {
                    long newQuota = isf.Quota + spaceRequest;

                    if (true == isf.IncreaseQuotaTo(newQuota))
                    {

                        return true;

                    }

                    else
                    {

                        return false;

                    }

                }

                catch (Exception e)
                {
                    Functions.WriteExceptionToLogFile(e);
                    return false;
                }

            }

        }
        #endregion
    }
}

