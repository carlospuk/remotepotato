using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.IO;
using RemotePotatoServer.Properties;

namespace RemotePotatoServer
{
    public static class UIFunctions
    {
        // Product Code
        public static string ProductCode
        {
            get
            {
                if (Settings.Default.IsTechPreview)
                    return "RP1";
                else
                    return "RP0";
            }
        }
        public static string ReadLogFileFromDisk()
        {
            try
            {
                return ReadTextFileFromDisk(Functions.DebugLogFileFN);
            }
            catch
            {
                return "Cannot read log file.";
            }

        }
        public static string VersionText
        {
            get
            {
                string strDev = (Settings.Default.IsTechPreview ? "dev" : "");
                return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() + strDev;
            }
        }

        // IO
        public static string ReadTextFile(string filePath)
        {
            filePath = ConvertRelativePathToAbsolute(filePath);

            // Exception?
            FileInfo f = new FileInfo(filePath);

            if (!File.Exists(filePath))
            {
                if (Settings.Default.DebugAdvanced)
                    Functions.WriteLineToLogFile("FileCache: Text File Not Found: " + filePath);
                return "";
            }

            string input = ReadTextFileFromDisk(filePath);

            return input;
        }
        static string ConvertRelativePathToAbsolute(string filePath)
        {
            if (!filePath.Contains(":"))
            {
                return Path.Combine(Functions.AppDataFolder, filePath);
            }
            else
                return filePath;
        }
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

        public static string ConnectionInfoString
        {
            get
            {
                string strOutput = "";

                string ip = Settings.Default.LastPublicIP;
                if (string.IsNullOrWhiteSpace(ip))
                {
                    strOutput = "(no IP information available)";
                    return strOutput;
                }
                else
                    strOutput += "Server Address: " + ip;

                if (Settings.Default.Port != 80)
                    strOutput += "   Port:" + Settings.Default.Port.ToString();

                return strOutput;
            }
        }
        public static string AppInstallFolder
        {
            get
            {
                string loc = System.Reflection.Assembly.GetExecutingAssembly().Location;
                return System.IO.Path.GetDirectoryName(loc);
            }
        }

        public static bool OSSupportsMediaCenterFunctionality
        {
            get
            {
                return (Environment.OSVersion.Version >= new Version(6,1));
            }
        }

    }
}
