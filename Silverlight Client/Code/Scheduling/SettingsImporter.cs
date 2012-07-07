using System;
using System.IO;
using System.Text;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Xml;
using System.ComponentModel;
using System.Collections;
using CommonEPG;

namespace SilverPotato
{
    public static class SettingsImporter
    {
        public static bool HasSettings;
        public static event EventHandler<GenericEventArgs<bool>> GetSettingsCompleted;

        public static SerializableDictionary<string, string> RPSettings;

        public static void Initialize()
        {
            HasSettings = false;
            RPSettings = new SerializableDictionary<string, string>();
        }


        public static void GetSettings()
        {
            // Get settings from cache to speed things up
            if (IsSettingsInCache())
            {
                Functions.WriteLineToLogFile("[Getting settings from cache for speed]");
                string xml = GetSettingsFromCache();
                ParseSettingsXML(xml);
                if (GetSettingsCompleted != null) GetSettingsCompleted(new object(), new GenericEventArgs<bool>(HasSettings));
            
                // ...dont return, we should then import the settings anyway again just to be fresh...
            }
            
            ImportAllSettings();
        }
        public static void RefreshSettingsFromServer()
        {
            ImportAllSettings();
        }
        public static string SettingsAsPrettyString
        {
            get
            {
                StringBuilder sbSettings = new StringBuilder();
                foreach (KeyValuePair<string, string> kvp in RPSettings)
                {
                    sbSettings.Append(kvp.Key);
                    sbSettings.Append(": ");
                    if ((kvp.Key == "UserName") || (kvp.Key == "UserPassword"))
                        sbSettings.Append("******");
                    else
                        sbSettings.Append(kvp.Value);
                    sbSettings.Append(Environment.NewLine);
                }

                return sbSettings.ToString();
            }
        }

        public static string SettingOrEmptyString(string settingKey)
        {
            if (RPSettings.ContainsKey(settingKey))
                return RPSettings[settingKey];
            else
                return "";
        }
        public static string SettingOrNull(string settingKey)
        {
            if (RPSettings.ContainsKey(settingKey))
                return RPSettings[settingKey];
            else
                return null;
        }
        public static int SettingAsIntOrZero(string settingKey)
        {
            if (RPSettings.ContainsKey(settingKey))
                return Convert.ToInt32(RPSettings[settingKey]);
            else
                return 0;
        }
        public static bool SettingIsTrue(string settingKey)
        {
            string strSetting = SettingOrNull(settingKey);
            if (strSetting == null) return false;
            return ("true" == strSetting.ToLower());
        }

        private static void ImportAllSettings()
        {
            RPWebClient client = new RPWebClient();
            client.GetStringByGettingCompleted += new EventHandler<UploadStringCompletedEventArgs>(ImportAllSettings_DownloadRPStringCompleted);
            client.GetStringByGetting("xml/settings");
        }
        static void ImportAllSettings_DownloadRPStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                Functions.WriteLineToLogFile("[Couldn't get settings from server.]");
                Functions.WriteExceptionToLogFile(e.Error);
                // ERROR
                if (GetSettingsCompleted != null) GetSettingsCompleted(new object(), new GenericEventArgs<bool>(false));
                return;
            }

            Functions.WriteLineToLogFile("[Got settings from server OK]");

            // Cache file
            SetLastImportedDateToFile();
            StoreSettingsInCache(e.Result);

            // Parse settings
            BackgroundWorker bw_ParseXML = new BackgroundWorker();
            bw_ParseXML.WorkerReportsProgress = false;
            bw_ParseXML.WorkerSupportsCancellation = false;
            bw_ParseXML.DoWork += new DoWorkEventHandler(bw_ParseXML_DoWork);
            bw_ParseXML.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw_ParseXML_RunWorkerCompleted);
            bw_ParseXML.RunWorkerAsync(e.Result);
        }

        static void bw_ParseXML_DoWork(object sender, DoWorkEventArgs e)
        {
            ParseSettingsXML((string)e.Argument);
        }
        static void bw_ParseXML_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (GetSettingsCompleted != null) GetSettingsCompleted(new object(), new GenericEventArgs<bool>(HasSettings));
        }
        private static bool IsSettingsInCache()
        {
            return FileManager.IsolatedStorageFileExists("RPSettingsV2.xml");
        }
        private static void StoreSettingsInCache(string settingsXML)
        {
            FileManager.WriteFileToIsolatedStorage("RPSettingsV2.xml", settingsXML);
        }
        private static string GetSettingsFromCache()
        {
            return FileManager.ReadTextFileFromIsolatedStorage("RPSettingsV2.xml");
        }
        private static void SetLastImportedDateToFile()
        {
            long tickDate = DateTime.Now.ToUniversalTime().Ticks;
            string strDate = tickDate.ToString();
            FileManager.WriteFileToIsolatedStorage("settingslastimported.txt", strDate);
        }
        public static string LastImportedSettingsDate()
        {
            if (FileManager.IsolatedStorageFileExists("settingslastimported.txt"))
            {
                string strDate = FileManager.ReadTextFileFromIsolatedStorage("settingslastimported.txt");

                long tickDate = 0;
                if (long.TryParse(strDate, out tickDate))
                {
                    DateTime theDate = new DateTime(tickDate, DateTimeKind.Utc);
                    return theDate.ToLocalTime().ToLongDateString() + " " + theDate.ToLocalTime().ToLongTimeString();
                }
                else
                    return "Unknown.";
            }
            else
                return "Never.";
        }

        private static void ParseSettingsXML(string settingsXML)
        {
            if (String.IsNullOrEmpty(settingsXML)) return ;

            if (RPSettings != null)
                RPSettings = new SerializableDictionary<string,string>();

            try
            {
                XmlSerializer xmls = new XmlSerializer(RPSettings.GetType());
                StringReader sr = new StringReader(settingsXML);
                RPSettings = (SerializableDictionary<string, string>)xmls.Deserialize(sr);
                HasSettings = true;
            }
            catch (Exception ex)
            {
                Functions.WriteLineToLogFile("Error deserializing settings: ");
                Functions.WriteExceptionToLogFile(ex);
                HasSettings = false;
            }
        }
    }
}
