using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Text;
using System.Xml;
using RemotePotatoServer.Properties;

namespace RemotePotatoServer
{
    public static class Themes
    {

        public static List<string> ThemeNames;
        public static List<string> MobileThemeNames;
        private static bool _usingMobileTheme = false;

        private static event EventHandler UsingMobileThemeChanged;

        public static bool UsingMobileTheme
        {
            get { return Themes._usingMobileTheme; }
            set {
                bool clientSwitched = (value != _usingMobileTheme);
                Themes._usingMobileTheme = value;

                if (clientSwitched)
                    UsingMobileThemeChanged(new object(), new EventArgs());
            }
        }


        

        static Themes()
        {
            ThemeNames = new List<string>();
            MobileThemeNames = new List<string>();

            UsingMobileThemeChanged += new EventHandler(Themes_UsingMobileThemeChanged);
        }

        // the type of theme changed - init settings
        static void Themes_UsingMobileThemeChanged(object sender, EventArgs e)
        {
            LoadActiveThemeSettings();   
        }

        public static void LoadActiveThemeSettings()
        {
            string xmlsettings = FileCache.ReadSkinTextFile("settings.xml");
            StringReader sr = new StringReader(xmlsettings);
            XmlReader xr = XmlReader.Create(sr);

            try
            {
                xr.MoveToContent();
                while (xr.Read())
                {
                    if (xr.Name == "epgtimespan")
                        EPGManager.TimespanMinutes = xr.ReadElementContentAsInt();
                    if (xr.Name == "epgzoomfactor")
                        EPGManager.EPGScaleFactor = xr.ReadElementContentAsDouble();
                }
            }
            catch (Exception e)
            {
                Functions.WriteLineToLogFile("Error loading active theme settings:");
                Functions.WriteExceptionToLogFile(e);
            }

            xr.Close();
            sr.Close();
            xmlsettings = "";
        }

        public static void GetThemeNamesFromFolderStructure()
        {
            ThemeNames.Clear();
            MobileThemeNames.Clear();

            DirectoryInfo themeFolder = new DirectoryInfo(Functions.SkinFolder);
            if (!themeFolder.Exists)
            {
                Functions.WriteLineToLogFile("Themes Folder does not exist: " + Functions.SkinFolder);
                return;
            }

            foreach (DirectoryInfo di in themeFolder.GetDirectories())
            {
                ThemeNames.Add(CultureInfo.CurrentCulture.TextInfo.ToTitleCase(di.Name));
                MobileThemeNames.Add(CultureInfo.CurrentCulture.TextInfo.ToTitleCase(di.Name));                
            }
        }

        public static string ActiveThemeName
        {
            get
            {
                return _usingMobileTheme ? Settings.Default.CurrentMobileThemeName : Settings.Default.CurrentMainThemeName;
            }
        }
        public static string ActiveThemeFolder
        {
            get
            {
                return Path.Combine(Functions.SkinFolder, ActiveThemeName);
            }
        }

    }
}
