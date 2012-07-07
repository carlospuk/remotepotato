using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemotePotatoServer
{
    public static class RegRunHelper
    {
        #region Public 
        public static bool SetRPKeySenderStartup(bool enable)
        {
            return SetStartup("RemotePotatoIRHelper", RPKeySenderAppPath, enable);
        }
        public static string RPKeySenderAppPath
        {
            get
            {
                return System.IO.Path.Combine(UIFunctions.AppInstallFolder, "RPKeySender.exe");
            }
        }

        public static bool IsRPKeySenderSetToRunOnStartup
        {
            get
            {
                return IsSetToRunOnStartup("RemotePotatoIRHelper");
            }
        }
        #endregion

        #region Low-Level Reg Helpers
        static bool IsSetToRunOnStartup(string AppName)
        {
            string runKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";

            Microsoft.Win32.RegistryKey startupKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(runKey);
            if (startupKey == null) return false;

            return (startupKey.GetValue(AppName) != null);            
        }
        /// <summary>
        /// Add/Remove registry entries for windows startup.
        /// </summary>
        /// <param name="AppName">Name of the application.</param>
        /// <param name="enable">if set to <c>true</c> [enable].</param>
        static bool SetStartup(string AppName, string AppPath, bool enable)
        {
            if ((enable) &&
                (!System.IO.File.Exists(AppPath))
                )
                return false;

            string runKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";

            Microsoft.Win32.RegistryKey startupKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(runKey);
            if (startupKey == null) return false;

            if (enable)
            {
                if (startupKey.GetValue(AppName) == null)
                {
                    startupKey.Close();
                    startupKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(runKey, true);
                    // Add startup reg key
                    startupKey.SetValue(AppName, AppPath);
                    startupKey.Close();
                }
            }
            else
            {
                // remove startup
                startupKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(runKey, true);
                startupKey.DeleteValue(AppName, false);
                startupKey.Close();
            }

            return true;
        }
        #endregion
    }
}
