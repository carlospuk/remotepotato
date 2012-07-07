using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.IO.IsolatedStorage;

namespace SilverPotato
{
    public static class Settings
    {

        // Local Settings
        #region Local Settings
        public static bool ZipDataStreams
        {
            get
            {
                return true;  // to enhance security
                //return GetBool("ZipDataStreams",true);
            }
            set
            {
                SetKey("ZipDataStreams", value);
            }
        }
        public static bool EnableAlphaFeatures
        {
            get
            {
                return GetBool("EnableAlphaFeatures", true);  // Yes, allow alpha features by default
            }
            set
            {
                SetKey("EnableAlphaFeatures", value);
            }
        }
        public static bool EnableHTTPLiveStreaming
        {
            get
            {
                return GetBool("EnableHTTPLiveStreaming", true); 
            }
            set
            {
                SetKey("EnableHTTPLiveStreaming", value);
            }
        }
        public static bool AskForStreamingTypeEachTime
        {
            get
            {
                return GetBool("AskForStreamingTypeEachTime", true);
            }
            set
            {
                SetKey("AskForStreamingTypeEachTime", value);
            }
        }
        public static bool DebugLogos
        {
            get
            {
                return GetBool("DebugLogos");
            }
            set
            {
                SetKey("DebugLogos", value);
            }
        }
        public static bool DebugCache
        {
            get
            {
                return GetBool("DebugCache", false);
            }
            set
            {
                SetKey("DebugCache", value);
            }
        }
        public static bool DebugAdvanced
        {
            get
            {
                return GetBool("DebugAdvanced", false);
            }
            set
            {
                SetKey("DebugAdvanced", value);
            }
        }
        public static bool DebugHTTP
        {
            get
            {
                return GetBool("DebugHTTP");
            }
            set
            {
                SetKey("DebugHTTP", value);
            }
        }
        public static bool EPGSmartScrolling
        {
            get
            {
                return GetBool("EPGSmartScrolling", true);
            }
            set
            {
                SetKey("EPGSmartScrolling", value);
            }
        }
        public static bool EPGDontLoadLazily
        {
            get
            {
                return GetBool("EPGDontLoadLazily", false);
            }
            set
            {
                SetKey("EPGDontLoadLazily", value);
            }
        }
        public static bool WarnOnMusicImportDelay
        {
            get
            {
                return GetBool("WarnOnMusicImportDelay", true);
            }
            set
            {
                SetKey("WarnOnMusicImportDelay", value);
            }
        }
        public static bool DisableMovingBackground
        {
            get
            {
                return GetBool("DisableMovingBackground", false);
            }
            set
            {
                SetKey("DisableMovingBackground", value);
            }
        }
        public static bool RememberUserCredentials
        {
            get
            {
                return GetBool("RememberUserCredentials", false);
            }
            set
            {
                SetKey("RememberUserCredentials", value);
            }
        }
        public static bool ShowTimesInEPG
        {
            get
            {
                return GetBool("ShowTimesInEPG", true);
            }
            set
            {
                SetKey("ShowTimesInEPG", value);
            }
        }
        public static bool EPGGetShowDescriptions
        {
            get
            {
                return GetBool("EPGGetShowDescriptions", false);
            }
            set
            {
                SetKey("EPGGetShowDescriptions", value);
            }
        }
        public static bool ShowEpisodeTitlesInEPG
        {
            get
            {
                return GetBool("ShowEpisodeTitlesInEPG", false);
            }
            set
            {
                SetKey("ShowEpisodeTitlesInEPG", value);
            }
        }
        public static bool SilverlightCacheLogosAndThumbs
        {
            get
            {
                // Default to the setting specified within the remote potato server if no local overriden value is found
                return GetBool("SilverlightCacheLogosAndThumbs", true);
            }
            set
            {
                SetKey("SilverlightCacheLogosAndThumbs", value);
            }
        }
        public static bool SilverlightCacheEPGDays
        {
            get
            {
                // Default to the setting specified within the remote potato server if no local overriden value is found
                return GetBool("SilverlightCacheEPGDays", SettingsImporter.SettingIsTrue("SilverlightCacheEPGDays"));
            }
            set
            {
                SetKey("SilverlightCacheEPGDays", value);
            }
        }
        public static bool SilverlightStoreCacheLocally
        {
            get
            {
                // Default to the setting specified within the remote potato server if no local overriden value is found
                return GetBool("SilverlightStoreCacheLocally", SettingsImporter.SettingIsTrue("SilverlightStoreCacheLocally"));
            }
            set
            {
                SetKey("SilverlightStoreCacheLocally", value);
            }
        }
        public static double LastUsedVolumeLevel
        {
            get
            {
                return (double)GetKeyOrDefault("LastUsedVolumeLevel", 0.8);
            }
            set
            {
                SetKey("LastUsedVolumeLevel", value);
            }
        }
        public static bool PlayStartupSound
        {
            get
            {
                return GetBool("PlayStartupSound", true);
            }
            set
            {
                SetKey("PlayStartupSound", value);
            }
        }
        public static ChannelFilterTypes ChannelFilter
        {
            get
            {
                return (ChannelFilterTypes)GetKeyOrDefault("ChannelFilter", ChannelFilterTypes.Favourites);
            }
            set
            {
                SetKey("ChannelFilter", value);
            }
        }
        public static void ToggleChannelFilter()
        {
            ChannelFilterTypes cfT = Settings.ChannelFilter;
            ChannelFilterTypes newFilter = ChannelFilterTypes.AllChannels;
            switch (cfT)
            {
                case ChannelFilterTypes.AllChannels:
                    newFilter = ChannelFilterTypes.Favourites;
                    break;

                case ChannelFilterTypes.Favourites:
                    newFilter = ChannelFilterTypes.AllChannels;
                    break;
            }
            Settings.ChannelFilter = newFilter;
        }
        public static long AppStorageStepAmount
        {
            get
            {
                object size =GetKeyOrDefault("AppStorageStepAmount", 20000000); // Increase by 20 Mb each time
                Int64 i = Convert.ToInt64(size);
                long l = (long)i;
                return l;
            }
            set
            {
                SetKey("AppStorageSize", value);
            }
        }
        public static long AppStorageTriggerLowThreshold
        {
            get
            {
                object size = GetKeyOrDefault("AppStorageTriggerLowThreshold", 10000000); // Below 10Mb free will increase the amount
                Int64 i = Convert.ToInt64(size);
                long l = (long)i;
                return l;
            }
            set
            {
                SetKey("AppStorageSize", value);
            }
        }
        public static string StoredCredentialUsername
        {
            get
            {
                return GetString("StoredCredentialUsername", "");
            }
            set
            {
                SetKey("StoredCredentialUsername", value);
            }
        }
        public static string StoredCredentialPassword
        {
            get
            {
                return GetString("StoredCredentialPassword", "");
            }
            set
            {
                SetKey("StoredCredentialPassword", value);
            }
        }
        public static int SlideShowInterval
        {
            get
            {
                return GetInt("SlideShowInterval", 6);
            }
            set
            {
                SetKey("SlideShowInterval", value);
            }
        }

        // ...more settings here


        // Settings Helper(s)
        public static string ZipDataStreamsAddendum
        {
            get
            {
                return "/zip";
                //return (ZipDataStreams) ? "/zip" : "";
            }
        }

        // Low-level settings get/set
        private static void SetKey(string theKey, object theValue)
        {
            IsolatedStorageSettings userSettings = IsolatedStorageSettings.ApplicationSettings; 
            
            if (userSettings.Contains(theKey))
                userSettings.Remove(theKey);

            userSettings.Add(theKey, theValue);
            userSettings.Save();
        }
        private static bool GetBool(string theKey)
        {
            return GetBool(theKey, false);
        }
        private static bool GetBool(string theKey, bool theDefault)
        {
            return (bool)GetKeyOrDefault(theKey, theDefault);
        }
        private static int GetInt(string theKey, int theDefault)
        {
            return (int)GetKeyOrDefault(theKey, theDefault);
        }
        private static string GetString(string theKey, string theDefault)
        {
            return (string)GetKeyOrDefault(theKey, theDefault);
        }
        private static object GetKeyOrDefault(string theKey, object theDefault)
        {
            IsolatedStorageSettings userSettings = IsolatedStorageSettings.ApplicationSettings; 

            if (userSettings.Contains(theKey))
                return userSettings[theKey];

            return theDefault;
        }
        #endregion

        #region Transient Settings
        public static bool WarnedOnMusicImportDelay;
        #endregion

        #region Server Settings
        // Wrapper for SettingsImporter : some cross-app code that expects these to be here
        public static class Default
        {
            public static string DefaultPrePadding
            {
                get
                {
                    string pp = SettingsImporter.SettingOrNull("DefaultPrePadding");
                    if (pp != null)
                        return pp;
                    else
                        return "";
                }
            }
            public static string DefaultPostPadding
            {
                get
                {
                    string pp = SettingsImporter.SettingOrNull("DefaultPostPadding");
                    if (pp != null)
                        return pp;
                    else
                        return "";
                }
            }
            public static bool DebugXML
            {
                get
                {
                    return SettingsImporter.SettingIsTrue("DebugXML");
                }
            }
            public static int RecommendedMovieMinimumRating
            {
                get
                {
                    return SettingsImporter.SettingAsIntOrZero("RecommendedMovieMinimumRating");
                }
            }
        }

        #endregion

    }
}
