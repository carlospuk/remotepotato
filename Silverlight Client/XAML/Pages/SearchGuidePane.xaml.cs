using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using CommonEPG;

namespace SilverPotato
{
    public partial class SearchGuidePane : UserControl
    {
        List<TVProgramme> matchedProgs;

        // Constructors
        public SearchGuidePane()
        {
            InitializeComponent();

            svResults.SetIsMouseWheelScrollingEnabled(true);
            Application.Current.Host.Content.FullScreenChanged +=new EventHandler(Content_FullScreenChanged);
            Loaded += new RoutedEventHandler(SearchGuidePane_Loaded);
        }

        

        void SearchGuidePane_Loaded(object sender, RoutedEventArgs e)
        {
            matchedProgs = null;
            matchedProgs = new List<TVProgramme>();

            ResetSearchForm();
        }


        public void ResetSearchForm()
        {
            txtSearchText.Text = "";
            cmbSearchMatchType.SelectedIndex = 0;
            cmbSearchTextType.SelectedIndex = 0;

            btnSearchNow.IsEnabled = true;
            spResults.Children.Clear();

            ShowFullScreenWarningIfAppropriate();
        }
        void Content_FullScreenChanged(object sender, EventArgs e)
        {
            ShowFullScreenWarningIfAppropriate();
        }

        private void ShowFullScreenWarningIfAppropriate()
        {
            if (Application.Current.Host.Content.IsFullScreen)
            {
                lblResultsInfo.Text = "Silverlight does not allow keyboard use in full-screen mode, please return to normal screen mode.";
            }
            else
            {
                lblResultsInfo.Text = "Please enter a search term into the box above to begin.";
            }
        }

        // Form events
        private void searchForm_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SearchNow();
            }
        }
        private void btnSearchNow_Click(object sender, RoutedEventArgs e)
        {
            SearchNow();
        }
        void TVProgClickItem_Clicked(object sender, EventArgs e)
        {
            TVProgClickItem ci = (TVProgClickItem)sender;
            ShowInfoPane sip = new ShowInfoPane(ci.LinkedTVProgramme);
            VisualManager.PushOntoScreenStack(sip);
        }
        private void txtSearchText_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Cannot search in full screen, cannot use keyboard
            if (Application.Current.Host.Content.IsFullScreen)
                Functions.ToggleFullScreen();
        }

        #region Search / Fill Data
        private void SearchNow()
        {
            // Validate
            string searchText = txtSearchText.Text;  //.Trim();  // allow spaves
            if ((searchText.Length < 2) && (cmbSearchMatchType.SelectedIndex != 2))
            {
                lblResultsInfo.Text = "Please enter at least two letters, or use 'exact' match.";
                return;
            }


            // Validated
            lblResultsInfo.Text = "Getting results...";

            EPGSearch theSearch = new EPGSearch();
            theSearch.TextToSearch = txtSearchText.Text;

            if (cmbSearchMatchType.SelectedIndex == 0)
                theSearch.MatchType = EPGSearchMatchType.StartsWith;
            else if (cmbSearchMatchType.SelectedIndex == 1)
                theSearch.MatchType = EPGSearchMatchType.Contains;
            else
                theSearch.MatchType = EPGSearchMatchType.ExactMatch;


            if (cmbSearchTextType.SelectedIndex == 0)
                theSearch.TextType = EPGSearchTextType.Title;
            else if (cmbSearchTextType.SelectedIndex == 1)
                theSearch.TextType = EPGSearchTextType.TitleAndEpisodeTitle;
            else if (cmbSearchTextType.SelectedIndex == 2)
                theSearch.TextType = EPGSearchTextType.TitlesAndDescription;
            else
                theSearch.TextType = EPGSearchTextType.AllTextFields;

            // Do search on server
            theSearch.LimitToDateRange = false;

            VisualManager.ShowActivityWithinGrid(LayoutRoot);
            btnSearchNow.IsEnabled = false;

            EPGImporter importer = new EPGImporter();
            importer.SubmitSearchCompleted += new EventHandler<GenericEventArgs<List<TVProgramme>>>(importer_SubmitSearchCompleted);
            importer.SubmitSearchRequestToServer(theSearch);
        }

        void importer_SubmitSearchCompleted(object sender, GenericEventArgs<List<TVProgramme>> e)
        {
            VisualManager.HideActivityWithinGrid(LayoutRoot);
            btnSearchNow.IsEnabled = true;
            if (e.Value != null)
            {
                if (e.Value.Count < 1)
                    lblResultsInfo.Text = "No results were found.";
                else if (e.Value.Count > 48)
                    lblResultsInfo.Text = "Too many results were found, please try a more specific search to narrow down your search.";
                else
                    lblResultsInfo.Text = "Found " + e.Value.Count.ToString() + " programmes.";
            }


            // Add them (temporarily) to the TV programme store, so that if recorded they will get Update event fired
            ScheduleManager.MergeIntoTVProgrammeStore(e.Value, false);
            
            // Tag programmes
            matchedProgs.Clear();
            matchedProgs = e.Value;
            matchedProgs.Sort( new CommonEPG.Comparers.TVProgrammeStartTimeComparer());
            populateProgrammesList();
        }
        #endregion


        // A tv prog was recorded, update the list
        void ScheduleManager_Recordings_Changed(object sender, EventArgs e)
        {
            populateProgrammesList();
        }
        // Other showings Callback
        void populateProgrammesList()
        {
            spResults.Children.Clear();

            foreach (TVProgramme tvp in matchedProgs)
            {
                TVProgClickItem tvpci = new TVProgClickItem(tvp, TVProgClickItem.TVProgClickItemTextFormat.TitleThenDayOfWeekThenDateAndChannel, ClickItem.ClickItemLayouts.TextOnly);
                tvpci.Clicked += new EventHandler(TVProgClickItem_Clicked);
                spResults.Children.Add(tvpci);
            }

            Functions.ShowHideElement(spResultsContainer, (spResults.Children.Count > 0));
        }

        // BUTTONS pushed
        void CloseMe()
        {
            VisualManager.PopOffScreenStackCurrentWindow();
        }

        

    }

}
