using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using RemotePotatoServer.Properties;

namespace RemotePotatoServer
{
    public partial class FormThemesChooser : Form
    {
        bool initialising;

        public FormThemesChooser()
        {
            initialising = true;

            InitializeComponent();

            PopulateThemesListboxesFromSettings();

            initialising = false;

            SetupMainTheme();
            SetupMobileTheme();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            CloseMe();
        }
        void CloseMe()
        {
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
        }


        #region Themes
        // select correct values in listboxes from settings (initially)
        private void PopulateThemesListboxesFromSettings()
        {
            Themes.GetThemeNamesFromFolderStructure();
            lstCurrentMainTheme.DataSource = Themes.ThemeNames;
            lstCurrentMobileTheme.DataSource = Themes.MobileThemeNames;
            lstCurrentMainTheme.Text = Settings.Default.CurrentMainThemeName;
            lstCurrentMobileTheme.Text = Settings.Default.CurrentMobileThemeName;
        }
        // Bind listboxes to settings
        private void lstCurrentMainTheme_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (initialising) return;
            Settings.Default.CurrentMainThemeName = (string)lstCurrentMainTheme.SelectedItem;

            SetupMainTheme();
        }

        private void lstCurrentMobileTheme_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (initialising) return;

            Settings.Default.CurrentMobileThemeName = (string)lstCurrentMobileTheme.SelectedItem;

            SetupMobileTheme();
        }
        private void SetupMainTheme()
        {
            string filePath = "static/skins/" + Settings.Default.CurrentMainThemeName + "/about.txt";
            txtAboutTheme.Text = FileCache.ReadTextFile(filePath);
            Themes.LoadActiveThemeSettings();
        }
        private void SetupMobileTheme()
        {
            string filePath = "static/skins/" + Settings.Default.CurrentMobileThemeName + "/about.txt";
            txtAboutMobileTheme.Text = FileCache.ReadTextFile(filePath);
        }

        #endregion



    }
}
