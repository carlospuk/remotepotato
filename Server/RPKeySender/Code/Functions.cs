using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;

namespace RPKeySender
{
    public static class Functions
    {

        // Errors
        static Functions()
        {
            StoredLogEntries = new List<string>();
        }
        public static string AppDataFolder
        {
            get
            {
                string dirPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "\\" + "RemotePotato";
                if (!Directory.Exists(dirPath))
                {
                    try
                    {
                        Directory.CreateDirectory(dirPath);
                    }
                    catch (Exception e)
                    {
                        Functions.WriteLineToLogFile("Could not create App Data directory");
                        Functions.WriteExceptionToLogFile(e);
                        return "";
                    }
                }
                return dirPath;
            }
        }
        static List<string> StoredLogEntries;
        public static string DebugLogFileFN
        {
            get
            {
                string strPath = AppDataFolder;
                return Path.Combine(strPath, "RPKeySender.log");
            }
        }
        static object writeLogLock = new object();
        public static void WriteLineToLogFileIfLoggingKeys(string txtLine)
        {
            WriteLineToLogFileIfSetting(txtLine, RPKeySender.Properties.Settings.Default.LogKeys);
        }
        public static void WriteLineToLogFileIfSetting(string txtLine, bool Setting)
        {
            if (Setting)
                WriteLineToLogFile(txtLine);
        }
        public static void WriteLineToLogFile(string txtLine)
        {
            Monitor.Enter(writeLogLock);
            string logLine = System.String.Format("{0:G}: {1}.", System.DateTime.Now, txtLine);

            System.IO.StreamWriter sw;
            try
            {
                sw = System.IO.File.AppendText(DebugLogFileFN);
            }
            catch
            {
                // Store the log entry for later
                if (StoredLogEntries.Count < 150)  // limit
                    StoredLogEntries.Add(logLine);

                Monitor.Exit(writeLogLock);
                return;
            }

            try
            {
                // Write any pending log entries
                if (StoredLogEntries.Count > 0)
                {
                    foreach (string s in StoredLogEntries)
                    {
                        sw.WriteLine(s);
                    }
                    StoredLogEntries.Clear();
                }

                sw.WriteLine(logLine);
            }
            finally
            {
                sw.Close();
            }

            Monitor.Exit(writeLogLock);
        }
        public static void WriteExceptionToLogFile(Exception e)
        {
            string txtException = "EXCEPTION DETAILS: " + e.Message + Environment.NewLine + e.Source + Environment.NewLine + e.StackTrace + Environment.NewLine;

            WriteLineToLogFile(txtException);

            if (e.InnerException != null)
            {
                WriteLineToLogFile(Environment.NewLine + "INNER:");
                WriteExceptionToLogFile(e.InnerException);
            }
        }

    }
}
