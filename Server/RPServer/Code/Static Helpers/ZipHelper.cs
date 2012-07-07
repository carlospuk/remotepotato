using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using RemotePotatoServer.Properties;

using Ionic.Zip;

namespace RemotePotatoServer
{
    public static class ZipHelper
    {

        public static bool CreateZipFileFromFiles(List<string> inputFiles, string outputFile)
        {
            try
            {
                using (ZipFile zip = new ZipFile())
                {
                    
                    zip.CompressionLevel = Ionic.Zlib.CompressionLevel.BestCompression;
                    zip.FlattenFoldersOnExtract = true;
                    zip.AddFiles(inputFiles, false, "");
                    zip.Save(outputFile);
                }
            }
            catch (Exception ex)
            {
                Functions.WriteLineToLogFile("Could not create zip file.");
                Functions.WriteExceptionToLogFile(ex);
                return false;
            }

            return true;
        }

        public static string ZipString(string inString)
        {
            if (string.IsNullOrEmpty(inString)) return "";

            try
            {
                if (Settings.Default.DebugServer)
                    Functions.WriteLineToLogFile("Zipping up string of length " + inString.Length + ".  Begins:" + inString.Substring(0, 5) + "|Ends:" + inString.Substring(inString.Length - 5));
                byte[] buffer = Encoding.UTF8.GetBytes(inString);
                MemoryStream ms = new MemoryStream();
                // Write from buffer => memorystream
                using (Ionic.Zlib.ZlibStream Zip = new Ionic.Zlib.ZlibStream(ms, Ionic.Zlib.CompressionMode.Compress, Ionic.Zlib.CompressionLevel.BestCompression, true))
                {
                    Zip.Write(buffer, 0, buffer.Length);
                }

                // Read from memorystream into byte array compressed[]
                ms.Position = 0;
                MemoryStream outStream = new MemoryStream();
                byte[] compressed = new byte[ms.Length - 1];
                ms.Read(compressed, 0, compressed.Length);
                ms.Close();

                return Convert.ToBase64String(compressed);
            }
            catch (Exception ex)
            {
                Functions.WriteLineToLogFile("ZipHelper: Error zipping string:");
                Functions.WriteExceptionToLogFile(ex);
            }

            return "";
        }

        public static byte[] ZipStringToBytes(string inString)
        {
            if (string.IsNullOrEmpty(inString)) return null;

            try
            {
                if (Settings.Default.DebugServer)
                    Functions.WriteLineToLogFile("Zipping up string of length " + inString.Length + ".  Begins:" + inString.Substring(0, 5) + "|Ends:" + inString.Substring(inString.Length - 5));
                
                // Place string contents into byte array buffer[]
                byte[] buffer = Encoding.Unicode.GetBytes(inString);

                // buffer => memorystream
                MemoryStream ms = new MemoryStream();
                using (Ionic.Zlib.ZlibStream Zip = new Ionic.Zlib.ZlibStream(ms, Ionic.Zlib.CompressionMode.Compress, Ionic.Zlib.CompressionLevel.BestCompression, true))
                {
                    Zip.Write(buffer, 0, buffer.Length);
                }

                // Read from memorystream => byte array compressed[]
                byte[] compressed = new byte[ms.Length - 1];
                ms.Position = 0;
                MemoryStream outStream = new MemoryStream();
                ms.Read(compressed, 0, compressed.Length);
                ms.Close();

                return compressed;
            }
            catch (Exception ex)
            {
                Functions.WriteLineToLogFile("ZipHelper: Error zipping string:");
                Functions.WriteExceptionToLogFile(ex);
            }

            return null;
        }

    }
}
