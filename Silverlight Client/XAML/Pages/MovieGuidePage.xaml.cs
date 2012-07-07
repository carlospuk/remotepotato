using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Xml;
using System.IO;
using System.Xml.Serialization;
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
    public partial class MovieGuidePage : UserControl
    {
        ClickItemsPane contentPane;
        TVProgClickItem.TVProgClickItemTextFormat currentLabelFormat = TVProgClickItem.TVProgClickItemTextFormat.TitleThenDateAndChannel;
        string currentGroupBy = "date";
        bool FaveChannelsOnly = false;
        bool topRatedOnly = true;

        public MovieGuidePage()
        {
            InitializeComponent();

            InitialiseContentPane();

            Loaded += new RoutedEventHandler(MoviesPage_Loaded);
        }

        void MoviesPage_Loaded(object sender, RoutedEventArgs e)
        {
            List<TVProgramme> testMovies = ScheduleManager.ProgrammesOfType(TVProgrammeType.Movie);
            

            if (
                (testMovies == null) ||
                (testMovies.Count < 1)
                )
            {
                GetMoviesFromWeb();                 // we'll pick up when the schedule manager fires the event to say it's populated its array 
            }
            else
                Fill();
        }

        void GetMoviesFromWeb()
        {
            VisualManager.ShowActivityWithinGrid(LayoutRoot, 3.0);

            List<TVService> channelsToUse = ScheduleManager.AllTVChannelsToList(FaveChannelsOnly);
            
            // Over the next 2 weeks
            DateRange range = new DateRange(DateTime.Now.ToUniversalTime(), DateTime.Now.AddDays(14).ToUniversalTime() );

            EPGImporter importer = new EPGImporter();
            importer.GetProgrammesForEPGRequestsAsZipStringCompleted += new EventHandler<GenericEventArgs<string>>(importer_GetProgrammesForEPGRequestsAsZipStringCompleted);
            importer.GetMoviesAsZipStringOnServices(channelsToUse, range);
        }

        void importer_GetProgrammesForEPGRequestsAsZipStringCompleted(object sender, GenericEventArgs<string> e)
        {
            VisualManager.HideActivityWithinGrid(LayoutRoot);

            // To be honest, it's not great doing this here - feels like there should be a function in schedulemanager
            if (!string.IsNullOrEmpty(e.Value))
            {
                List<TVProgramme> programmes = EPGCache.ZipStringToTVProgrammesList(e.Value);

                ScheduleManager.MergeIntoTVProgrammeStore(programmes, true);

                Fill();
            }
            else
            {
                ErrorManager.DisplayAndLogError("Sorry we couldn't get the list of movies from the server right now.");
            }
            

            
        }



        #region Fill / Refresh Content
        void InitialiseContentPane()
        {
            contentPane = new ClickItemsPane(null, ClickItemsPane.ClickItemsPaneLayouts.PaneAndToolbar,
                ClickItemsPane.ClickItemsPaneItemLayouts.List);

            contentPane.ItemClicked += new EventHandler(contentPane_ItemClicked);
            contentPane.AwaitingRefreshedContent += new EventHandler(contentPane_RefreshClicked);
            gdContent.Children.Add(contentPane);
        }
        public void RefreshRecordings()
        {
            Fill();
        }
        public void Fill()
        {
            Dictionary<string, List<TVProgramme>> GroupedEvents = ScheduleManager.ProgrammesOfTypeGroupedByDate(TVProgrammeType.Movie, topRatedOnly);

            Dictionary<string, List<ClickItem>> GroupedItems = new Dictionary<string, List<ClickItem>>();
            foreach (KeyValuePair<string, List<TVProgramme>> kvp in GroupedEvents)
            {
                List<ClickItem> clickItems = new List<ClickItem>();
                foreach (TVProgramme tvp in kvp.Value)
                {
                    // Important line - use the PARENT BASE CLASS VARIABLE to store the derived class, so we can pass it to the ClickItemsPane
                    ClickItem ci = new TVProgClickItem(tvp, currentLabelFormat, ClickItem.ClickItemLayouts.TextOnly);
                    clickItems.Add(ci);
                }
                GroupedItems.Add(kvp.Key, clickItems);
            }

            // Populate content pane
            contentPane.ReplaceItemsWithNewItems(GroupedItems);

            VisualManager.HideActivityWithinGrid(LayoutRoot);
        }
        void contentPane_RefreshClicked(object sender, EventArgs e)
        {
            GetMoviesFromWeb();
        }
        
  
        #endregion


        // Event raised by the content pane - one of its items has been clicked
        void contentPane_ItemClicked(object sender, EventArgs e)
        {
            ClickItem ci = (ClickItem)sender; // Base class
            if (!(ci is TVProgClickItem)) return;
            TVProgClickItem tvpci = (TVProgClickItem)ci;  // Cast into derived class, we know it's a reci as it came from here
            ShowInfoPane sip;
            sip = new ShowInfoPane(tvpci.LinkedTVProgramme);
            VisualManager.PushOntoScreenStack(sip);
        }

        // GROUPING

        private void cmbFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbFilter == null) return;

            topRatedOnly = (cmbFilter.SelectedIndex == 0);

            Fill();
        }



    }
}
