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
    public partial class ScheduledRecordingsPage : UserControl
    {
        ClickItemsPane contentPane;

        public ScheduledRecordingsPage()
        {
            InitializeComponent();

            // Events (in constructor, not _Loaded method)
            ScheduleManager.Recordings_Changed += new EventHandler(ScheduleManager_Recordings_Changed);

            Loaded += new RoutedEventHandler(ScheduledRecordingsPage_Loaded);
        }
        void ScheduledRecordingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (ScheduleManager.RecordingsUpdating)
            {
                VisualManager.ShowActivityWithinGrid(LayoutRoot, 3.0);
            }
            else
                Fill();
        }


        void ScheduleManager_Recordings_Changed(object sender, EventArgs e)
        {
            VisualManager.HideActivityWithinGrid(LayoutRoot);
            Fill();
        }

        public void Fill()
        {
            bool foundAtLeastOneItem = false;
            gdContent.Children.Clear();
            Dictionary<string, List<RPRecording>> GroupedEvents = ScheduleManager.UpcomingRecordingsGroupedBy("date");

            Dictionary<string, List<ClickItem>> GroupedItems = new Dictionary<string, List<ClickItem>>();
            foreach (KeyValuePair<string, List<RPRecording>> kvp in GroupedEvents)
            {
                List<ClickItem> clickItems = new List<ClickItem>();
                foreach (RPRecording rec in kvp.Value)
                {
                    if (!foundAtLeastOneItem) foundAtLeastOneItem = true;

                    TVProgramme tvp = rec.TVProgramme();
                    if (tvp == null) continue;

                    // Important line - use the PARENT BASE CLASS VARIABLE to store the derived class, so we can pass it to the ClickItemsPane
                    ClickItem ci = new TVProgClickItem(tvp, TVProgClickItem.TVProgClickItemTextFormat.TimeTitleThenChannel, ClickItem.ClickItemLayouts.TextOnly);
                    clickItems.Add(ci);
                }
                GroupedItems.Add(kvp.Key, clickItems);
            }

            if (contentPane != null)
            {
                contentPane.ItemClicked -= new EventHandler(contentPane_ItemClicked);
                contentPane = null;
            }
            contentPane = new ClickItemsPane(GroupedItems, ClickItemsPane.ClickItemsPaneLayouts.PaneAndToolbar, ClickItemsPane.ClickItemsPaneItemLayouts.List);
            contentPane.ItemClicked += new EventHandler(contentPane_ItemClicked);
            contentPane.AwaitingRefreshedContent += new EventHandler(contentPane_RefreshClicked);
            gdContent.Children.Add(contentPane);

            // Label if empty
            lblNoContent.Visibility = foundAtLeastOneItem ? Visibility.Collapsed : Visibility.Visible;
        }



        void contentPane_RefreshClicked(object sender, EventArgs e)
        {
            RefreshContent();
        }
        void RefreshContent()
        {
            VisualManager.ShowActivityWithinGrid(LayoutRoot);
            Animations.DoFadeOut(0.3, gdContent, RefreshContent_2);
        }
        void RefreshContent_2(object sender, EventArgs e)
        {
            gdContent.Children.Clear();
            gdContent.Opacity = 1.0;
            
            ScheduleManager.GetRecordingObjectsFromServer();
        }
        // Event raised by the content pane - one of its items has been clicked
        void contentPane_ItemClicked(object sender, EventArgs e)
        {
            ClickItem ci = (ClickItem)sender; // Base class
            if (!(ci is TVProgClickItem)) return;
            TVProgClickItem tvpci = (TVProgClickItem)ci;  // Cast into derived class, we know it's a reci as it came from here
            TVProgramme tvp = tvpci.LinkedTVProgramme;
            if (tvp == null) return;
            ShowInfoPane sip;
            sip = new ShowInfoPane(tvp);
            VisualManager.PushOntoScreenStack(sip);
        }

    }
}
