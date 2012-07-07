using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Xml.Serialization;
using RemotePotatoServer.Properties;
using Microsoft.Win32;
using System.Runtime.Serialization.Formatters.Binary;


namespace RemotePotatoServer
{
    public static class Functions
    {

        static Functions()
        {
            StoredLogEntries = new List<string>();
        }

        // Errors
        static List<string> StoredLogEntries;
        public static string DebugLogFileFN
        {
            get
            {
                string strPath = AppDataFolder;
                return Path.Combine(strPath, "RPServer.log");
            }
        }
        static object writeLogLock = new object();
        public static void WriteLineToLogFileIfAdvanced(string txtLine)
        {
            WriteLineToLogFileIfSetting(Settings.Default.DebugAdvanced, txtLine);
        }
        public static void WriteLineToLogFileIfSetting(bool setting, string txtLine)
        {
            if (setting)
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
        public static void WriteExceptionToLogFileIfAdvanced(Exception e)
        {
            WriteExceptionToLogFileIfSetting(Settings.Default.DebugAdvanced, e);
        }
        public static void WriteExceptionToLogFileIfSetting(bool setting, Exception e)
        {
            if (setting)
                WriteExceptionToLogFile(e);
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
        public static void logAPIoutputString(string XMLresponse)
        {
            if (Settings.Default.DebugFullAPI)
            {
                WriteLineToLogFile("\r\n\r\nAPI: Output XML (" + XMLresponse.Length.ToString() + "chars):\r\n" + "[" + XMLresponse + "]");
            }
        }

        // Flag for recordings
        public static bool isVideoStreamingObjectActive = false;

        // Dates
        public static bool partOfEventOccursInsideTimeWindow(DateTime eventStart, DateTime eventStop, DateTime windowStart, DateTime windowStop)
        {
            return (
                (eventStop > windowStart) & (eventStart < windowStop)
                );
        }
        public static string japDateFormat(DateTime theDate)
        {
            return theDate.Year.ToString() + "-" + theDate.Month.ToString("D2") + "-" + theDate.Day.ToString("D2");
        }

        // Encryption
        public static string EncodePassword(string originalPassword)
        {
            //Declarations
            Byte[] originalBytes;
            Byte[] encodedBytes;
            MD5 md5;

            //Instantiate MD5CryptoServiceProvider, get bytes for original password and compute hash (encoded password)
            md5 = new MD5CryptoServiceProvider();
            originalBytes = Encoding.Unicode.GetBytes(originalPassword);
            encodedBytes = md5.ComputeHash(originalBytes);

            //Convert encoded bytes back to a 'readable' string
            return BitConverter.ToString(encodedBytes);
        }
        public static bool StringHashesToPasswordHash(string inputString, bool stringIsAlreadyHashed)
        {
            string hashedString = (stringIsAlreadyHashed) ? inputString : EncodePassword(inputString);
            return (Settings.Default.UserPassword.Equals(hashedString));
        }
        public static string ConvertUTF16ToUTF8(string strUtf16)
        {
            return Encoding.UTF8.GetString(Encoding.Unicode.GetBytes(strUtf16));
        }
        /**
        * This method ensures that the output String has only
        * valid XML unicode characters as specified by the
        * XML 1.0 standard. For reference, please see
        * <a href="http://www.w3.org/TR/2000/REC-xml-20001006#NT-Char">the
        * standard</a>. This method will return an empty
        * String if the input is null or empty.
        *
        * @param in The String whose non-valid characters we want to remove.
        * @return The in String, stripped of non-valid characters.
        */
      
        public static string StripIllegalXmlCharacters(string text)
        {
            const string illegalXmlChars = @"[\u0000-\u0008]|[\u000B-\u000C]|[\u000E-\u0019]|[\u007F-\u009F]";

            var regex = new Regex(illegalXmlChars, RegexOptions.IgnoreCase);

            if (regex.IsMatch(text))
            {
                text = regex.Replace(text, " ");
            }

            return text;
        }

        // Capabilties
        public static string ServerCapabilities
        {
            get
            {
                List<string> capFlags = new List<string>();
                if (Settings.Default.EnableMediaCenterSupport) capFlags.Add("MCE");
                if (Settings.Default.EnableMusicLibrary) capFlags.Add("MUSIC");
                if (Settings.Default.EnablePictureLibrary) capFlags.Add("PICTURES");
                if (Settings.Default.EnableVideoLibrary) capFlags.Add("VIDEOS");
                capFlags.Add("RECORDEDTV");
                capFlags.Add("STREAM-HTTPLIVE");
                capFlags.Add("STREAM-MSWMSP");

                StringBuilder sbFlags = new StringBuilder(40);
                foreach (string flag in capFlags)
                {
                    sbFlags.Append(flag + ",");
                }

                sbFlags.Remove(sbFlags.Length - 1, 1);

                return sbFlags.ToString();
            }
        }

        // System / IO
        public static bool OSSupportsExplorerLibraries
        {
            get
            {
                return (Environment.OSVersion.Version >= new Version(6, 1));
            }
        }
        public static bool OSSupportsAdvancedFirewallInNetSH
        {
            get
            {
                return (Environment.OSVersion.Version >= new Version(6, 0)); // At least VISTA
            }
        }
        public static bool OSSupportsMediaDurationInShell
        {
            get
            {
                return (Environment.OSVersion.Version >= new Version(6, 1));
            }
        }
        public static bool isXP
        {
            get
            {
                return (OSName.Equals("XP"));
            }
        }
        static string OSName
        {
            get
            {
                //Get Operating system information.
                OperatingSystem os = Environment.OSVersion;
                //Get version information about the os.
                Version vs = os.Version;

                //Variable to hold our return value
                string operatingSystem = "?";

                if (os.Platform == PlatformID.Win32Windows)
                {
                    //This is a pre-NT version of Windows
                    switch (vs.Minor)
                    {
                        case 0:
                            operatingSystem = "95";
                            break;
                        case 10:
                            if (vs.Revision.ToString() == "2222A")
                                operatingSystem = "98SE";
                            else
                                operatingSystem = "98";
                            break;
                        case 90:
                            operatingSystem = "Me";
                            break;
                        default:
                            break;
                    }
                }
                else if (os.Platform == PlatformID.Win32NT)
                {
                    switch (vs.Major)
                    {
                        case 3:
                            operatingSystem = "NT 3.51";
                            break;
                        case 4:
                            operatingSystem = "NT 4.0";
                            break;
                        case 5:
                            if (vs.Minor == 0)
                                operatingSystem = "2000";
                            else
                                operatingSystem = "XP";
                            break;
                        case 6:
                            if (vs.Minor == 0)
                                operatingSystem = "Vista";
                            else
                                operatingSystem = "7";
                            break;
                        default:
                            break;
                    }
                }

                return operatingSystem;
            }
        }
        public static string GetRecordPath()
        {                   
           // Start with windows?
            try
            {
                RegistryKey rkRecPath = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Media Center\\Service\\Recording", false);
                return (string)rkRecPath.GetValue("RecordPath", "C:\\");
            }
            catch { }
            return "";
        }
        public static bool isRunningIn32BitMode
        {
            get
            {
                return (IntPtr.Size == 4);
            }
        }
        public static bool isLegacyAppRunning()
        {
            return isProcessRunning("RemotePotato.exe");
        }
        public static bool isProcessRunning(string name)
        {
            return (findProcessOrNull(name) != null);
        }
        private static Process findProcessOrNull(string name)
        {
            foreach (Process clsProcess in Process.GetProcesses())
            {
                string strProcessName = clsProcess.ProcessName;
                //WriteLineToLogFile(strProcessName);
                if (clsProcess.ProcessName.ToUpperInvariant().Equals(name.ToUpperInvariant()))
                {
                    return clsProcess;
                }
            }
            return null;
        }
        public static string BitnessString
        {
            get
            {
                return (isRunningIn32BitMode) ? "X86" : "X64";
            }
        }

        // Cloning
        public static object DeepClone(object obj)
        {
            object objResult = null;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(ms, obj);

                ms.Position = 0;
                objResult = bf.Deserialize(ms);
            }
            return objResult;
        }

        public static string VersionText
        {
            get
            {
                string strDev = (Settings.Default.IsTechPreview ? "TP" : "");
                return ServerVersion.ToString() + strDev;
            }
        }
        public static Version ServerVersion
        {
            get
            {
                Version v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                return v;
            }
        }



        #region Folder Names
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
                    catch 
                    {
                        // Can't log anywhere!  Eek.
                        File.Create(@"C:\RemotePotatoPanicCouldNotCreateDataDirectory.txt");
                    }
                }
                return dirPath;
            }            
        }
        public static string SkinFolder
        {
            get
            {
                string ADF = AppDataFolder;
                return Path.Combine(ADF, "static\\skins");
            }
        }
        public static string StreamBaseFolder
        {
            get
            {
                string ADF = AppDataFolder;
                return Path.Combine(ADF, "static\\mediastreams\\");
            }
        }
        public static string AppInstallFolder
        {
            get
            {
                string loc = System.Reflection.Assembly.GetExecutingAssembly().Location;
                return System.IO.Path.GetDirectoryName(loc);
//                    System.Reflection.Assembly.GetExecutingAssembly.GetModules()[0].FullyQualifiedName    
            }
        }
        public static string ToolkitFolder
        {
            get
            {
                return Path.Combine(AppInstallFolder, "toolkit");
            }
        }
        public static string ZipTempFolder
        {
            get
            {
                string ADF = AppDataFolder;
                return Path.Combine(ADF, "ziptemp");
            }
        }
        #endregion


        // Mime Type
        public static string MimeTypeForFileName(string localFilePath)
        {
            if (localFilePath.ToLower().EndsWith("jpg"))
                return "image/jpeg";
            else if (localFilePath.ToLower().EndsWith("png"))
                return "image/png";
            else if (localFilePath.ToLower().EndsWith("gif"))
                return "image/gif";
            else if (localFilePath.ToLower().EndsWith("bmp"))
                return "image/bmp";
            else if (localFilePath.ToLower().EndsWith("css"))
                return "text/css";
            else if (localFilePath.ToLower().EndsWith("htm"))
                return "text/html";
            else if (localFilePath.ToLower().EndsWith("html"))
                return "text/html";
            else if (localFilePath.ToLower().EndsWith("mp3"))
                return "audio/mpeg";
            else if (localFilePath.ToLower().EndsWith("wma"))
                return "audio/x-ms-wma";
            else if (localFilePath.ToLower().EndsWith("xap"))
                return "application/x-silverlight-2";
            else if (localFilePath.ToLower().EndsWith("zip"))
                return "application/zip";
            else if (localFilePath.ToLower().EndsWith("xml"))
                return "text/xml";
            else
                return "image/unknown";
        }

        // IsBlankPasswordRegistryEnabled
        public static bool IsBlankPasswordLimitedOnMachine
        {
            get
            {

                try
                {
                    RegistryKey rkLimitBlankPassUse = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\Lsa\\", false);
                        
                    int sValue = (int)rkLimitBlankPassUse.GetValue("LimitBlankPasswordUse", 0);
                    return (sValue == 1);
                }
                catch (Exception e){
                    Functions.WriteLineToLogFile("Error Getting LimitBlankPasswordUse registry value");
                    Functions.WriteExceptionToLogFile(e);
                }
                return false;
            }
        }
        public static void SetBlankPasswordLimitOnMachine(bool limitBlankPasswords)
        {
            try
            {
                RegistryKey rkLimitBlankPassUse = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\Lsa\\", true);

                int setValue = limitBlankPasswords ? 1 : 0;
                rkLimitBlankPassUse.SetValue("LimitBlankPasswordUse", setValue);                
            }
            catch (Exception e)
            {
                Functions.WriteLineToLogFile("Error setting LimitBlankPasswordUse registry value");
                Functions.WriteExceptionToLogFile(e);
            }
        }

        // Streaming pack
        public static bool IsStreamingPackInstalled
        {
            get
            {
       
            try
            {
                RegistryKey rkRecPath = Registry.CurrentUser.OpenSubKey("SOFTWARE\\FatAttitude\\RemotePotatoStreamingPack\\", false);
                string sValue = (string)rkRecPath.GetValue("Installed", "False");
                return (sValue.ToLowerInvariant().Equals("true"));
            }
            catch { }
            return false;
            }
        }
        public static int StreamingPackBuild
        {
            get
            {

                try
                {
                    RegistryKey rkRecPath = Registry.CurrentUser.OpenSubKey("SOFTWARE\\FatAttitude\\RemotePotatoStreamingPack\\", false);
                    return (int)rkRecPath.GetValue("Build", false);
                }
                catch { }
                return 0;
            }
        }

        // Object Copy
        public static void Copy<T, U>(ref T source, ref U target)
        {
            Type sourceType = source.GetType();
            Type targetType = target.GetType();
            PropertyInfo[] sourceProperties = sourceType.GetProperties();
            PropertyInfo[] targetProperties = targetType.GetProperties();

            foreach (PropertyInfo tp in targetProperties)
            {
                foreach (PropertyInfo sp in sourceProperties)
                {
                    if (tp.Name == sp.Name)
                        tp.SetValue(target, sp.GetValue(source, null), null);
                }
            }
        }

        // Strings
        #region Base64
        public static string DecodeFromBase64(string encodedData)
       {
           return DecodeFromBase64(encodedData, Encoding.UTF8);
       }
       public static string DecodeFromBase64(string encodedData, Encoding _encoding)
        {
            string returnValue = "";
            try
            {
                byte[] encodedDataAsBytes  = Convert.FromBase64String(encodedData);

                returnValue = _encoding.GetString(encodedDataAsBytes);
            }
           catch (Exception ex)
           {
               Functions.WriteLineToLogFile("Couldn't decode base64 string " + encodedData);
               Functions.WriteExceptionToLogFile(ex);
           }

           return returnValue;
       }
       public static string EncodeToBase64(string strToEncode)
       {
           return EncodeToBase64(strToEncode, Encoding.UTF8);
       }
       public static string EncodeToBase64(string strToEncode, Encoding _encoding)
       {
           string returnValue = "";
           try
           {
               byte[] encodedData = _encoding.GetBytes(strToEncode);
               returnValue = Convert.ToBase64String(encodedData);
           }
           catch (Exception ex)
           {
               Functions.WriteLineToLogFile("Couldn't encode base64 string " + strToEncode);
               Functions.WriteExceptionToLogFile(ex);
           }

           return returnValue;


       }

        #endregion

       public static List<string> StringListFromXML(string theXML)
       {
           List<string> output = new List<string>();
           XmlSerializer serializer = new XmlSerializer(output.GetType());
           StringReader sr = new StringReader(theXML);
           try
           {
               return (List<string>)serializer.Deserialize(sr);
           }
           catch (Exception ex)
           {
               Functions.WriteLineToLogFile("Could not deserialise list of strings.");
               Functions.WriteExceptionToLogFile(ex);
           }

           return output;
       }
  
        // HTML
       public static string DivTag(string ofClass)
       {
           return "<div class=\"" + ofClass + "\">";
       }
       public static string DivTag(string ofClass, double withWidth, string txtTooltip)
       {
           try
           {
               Int32 iWidth = Convert.ToInt32(withWidth);
               return "<div title=\"" + txtTooltip + "\" class=\"" + ofClass + "\" style=\"width: " + iWidth.ToString() + "px;\">";
           }
           catch { }
           {
               Functions.WriteLineToLogFile("Error: invalid width supplied to DivTag : " + withWidth.ToString());
               return "<div title=\"" + txtTooltip + "\" class=\"" + ofClass + "\" style=\"width: 150px;\">";
           }
       }
       public static string LinkTagOpen(string href)
       {
            return LinkTagOpen(href, null);
       }
       public static string LinkTagOpen(string href, string target)
       {
           string targetString = (target != null) ? " target=\"" + target + "\"" : "";
           return "<a href=\"" + href + "\"" + targetString + ">";
       }
       public static string LinkConfirmClick(string txtMessage)
        {
            return " onclick=\"javascript:return confirm('" + txtMessage + "')\" ";
        }
       public static string imgTag(string src, string ofClass)
        {
            return "<img src=\"" + src + "\" class=\"" + ofClass + "\" />";
        }
        // IO
       public static void ShowExplorerFolder(string folder)
        {
            return;
        }

       /// <summary>
        /// Decrypt a crypted string.
        /// </summary>
        /// <param name="cryptedString">The crypted string.</param>
        /// <returns>The decrypted string.</returns>
        /// <exception cref="ArgumentNullException">This exception will be thrown 
        /// when the crypted string is null or empty.</exception>
       public static string DecryptString(string cryptedString)
        {
            if (String.IsNullOrEmpty(cryptedString))
            {
                throw new ArgumentNullException
                   ("The string which needs to be decrypted can not be null.");
            }

            byte[] theKey = Convert.FromBase64String("4yELBlvHTII=;");

            DESCryptoServiceProvider cryptoProvider = new DESCryptoServiceProvider();
            MemoryStream memoryStream = new MemoryStream
                    (Convert.FromBase64String(cryptedString));
            CryptoStream cryptoStream = new CryptoStream(memoryStream,
                cryptoProvider.CreateDecryptor(theKey, theKey), CryptoStreamMode.Read);
            StreamReader reader = new StreamReader(cryptoStream);
            return reader.ReadToEnd();
        }
       public static string DecryptBinaryFile(string fileName)
        {
            if (!File.Exists(fileName))
            {
                throw new ArgumentNullException
                   ("The string which needs to be decrypted can not be null.");
            }
            
            FileStream fs = File.OpenRead(fileName);
            BinaryReader br = new BinaryReader(fs); 

            byte[] theKey = Convert.FromBase64String("4yELBlvHTII=;"); // was a MS copyright str

            DESCryptoServiceProvider cryptoProvider = new DESCryptoServiceProvider();
           // MemoryStream memoryStream = new MemoryStream(Convert.FromBase64String(cryptedString));
            CryptoStream cryptoStream = new CryptoStream(fs, cryptoProvider.CreateDecryptor(theKey, theKey), CryptoStreamMode.Read);
            StreamReader reader = new StreamReader(cryptoStream);
            return reader.ReadToEnd();
        }


    }
}

