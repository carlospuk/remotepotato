using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
    public partial class RecordedTVPane : UserControl
    {
        ClickItemsPane contentPane;
        TVProgClickItem.TVProgClickItemTextFormat currentLabelFormat = TVProgClickItem.TVProgClickItemTextFormat.TitleAndEpisodeTitle;
        string currentGroupBy = "date";

        public RecordedTVPane()
        {
            InitializeComponent();

            InitialiseContentPane();

            Loaded += new RoutedEventHandler(ViewRecordedTVPage_Loaded);
            ScheduleManager.RecordedTVUpdated += new EventHandler(ScheduleManager_RecordedTVUpdated);
        }



        void ViewRecordedTVPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (
                (ScheduleManager.AllRecordedTVProgrammes == null) ||
                (ScheduleManager.AllRecordedTVProgrammes.Count < 1)
                )
            {
                VisualManager.ShowActivityWithinGrid(LayoutRoot, 3.0);
                ScheduleManager.GetRecordedTV(false);
                return;  // we'll pick up when the schedule manager fires the event to say it's populated its array 
            }

            // else.....
            Fill();
        }
        void ScheduleManager_RecordedTVUpdated(object sender, EventArgs e)
        {
            Fill();
        }


        #region Fill / Refresh Content
        void InitialiseContentPane()
        {
            contentPane = new ClickItemsPane(null, ClickItemsPane.ClickItemsPaneLayouts.PaneAndToolbar,
                ClickItemsPane.ClickItemsPaneItemLayouts.Thumbnails);

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
            Dictionary<string, List<TVProgramme>> GroupedEvents = ScheduleManager.RecordedTVGroupedBy(currentGroupBy);

            Dictionary<string, List<ClickItem>> GroupedItems = new Dictionary<string, List<ClickItem>>();
            foreach (KeyValuePair<string, List<TVProgramme>> kvp in GroupedEvents)
            {
                List<ClickItem> clickItems = new List<ClickItem>();
                foreach (TVProgramme tvp in kvp.Value)
                {
                    // Important line - use the PARENT BASE CLASS VARIABLE to store the derived class, so we can pass it to the ClickItemsPane
                    ClickItem ci = new TVProgClickItem(tvp, currentLabelFormat, ClickItem.ClickItemLayouts.ThumbnailWithOverlay);
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
            RefreshRecordedTVNow();
        }
        void RefreshRecordedTVNow()
        {
            ScheduleManager.GetRecordedTV(true);
            VisualManager.ShowActivityWithinGrid(LayoutRoot);
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
        private void cmbGroupBy_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbGroupBy == null) return;

            if (cmbGroupBy.SelectedIndex == 0)
            {
                currentGroupBy = "date";
                currentLabelFormat = TVProgClickItem.TVProgClickItemTextFormat.TitleAndEpisodeTitle;
            }
            else if (cmbGroupBy.SelectedIndex == 1)
            {
                currentGroupBy = "title";
                currentLabelFormat = TVProgClickItem.TVProgClickItemTextFormat.TitleThenNewlineThenDate;
            }
            else
            {
                currentGroupBy = "series";
                currentLabelFormat = TVProgClickItem.TVProgClickItemTextFormat.DateAndTime;
            }

            ReGroupContent();
        }
        void ReGroupContent()
        {
            VisualManager.ShowActivityWithinGrid(LayoutRoot);
            Fill();
        }


    }
}
