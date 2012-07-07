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
    public partial class ManageSeriesPage : UserControl
    {
        ClickItemsPane contentPane;

        public ManageSeriesPage()
        {
            InitializeComponent();

            // Events (in constructor to avoid double attaching)
            ScheduleManager.Recordings_Changed += new EventHandler(ScheduleManager_Recordings_Changed);

            Loaded += new RoutedEventHandler(ManageSeriesPage_Loaded);
        }

        void ManageSeriesPage_Loaded(object sender, RoutedEventArgs e)
        {
            
            if (ScheduleManager.RecordingsUpdating)
            {
                VisualManager.ShowActivityWithinGrid(LayoutRoot, 3.0);
            }
            else
            {
                Fill();
            }
        }

        void ScheduleManager_Recordings_Changed(object sender, EventArgs e)
        {
            VisualManager.HideActivityWithinGrid(LayoutRoot);
            Fill();
        }


        public void Fill()
        {
            gdContent.Children.Clear();
            Dictionary<string, List<RPRequest>> GroupedEvents = ScheduleManager.SeriesRequestsGroupedBy("");

            bool foundAtLeastOneItem = false;
            Dictionary<string, List<ClickItem>> GroupedItems = new Dictionary<string, List<ClickItem>>();
            foreach (KeyValuePair<string, List<RPRequest>> kvp in GroupedEvents)
            {
                List<ClickItem> clickItems = new List<ClickItem>();
                foreach (RPRequest req in kvp.Value)
                {
                    if (
                            (req.RequestType != RPRequestTypes.Series) &&
                            (req.RequestType != RPRequestTypes.Keyword)
                            )   continue;

                    if (!foundAtLeastOneItem ) foundAtLeastOneItem = true;

                    // Important line - use the PARENT BASE CLASS VARIABLE to store the derived class, so we can pass it to the ClickItemsPane
                    ClickItem ci = new RPRequestClickItem(req, RPRequestClickItem.SeriesRequestClickItemTextFormat.TitleAndChannelAndType, ClickItem.ClickItemLayouts.TextOnly);
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

            lblNoContent.Visibility = foundAtLeastOneItem ? Visibility.Collapsed : Visibility.Visible;
        }


        #region Refresh
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
        #endregion

        // Event raised by the content pane - one of its items has been clicked
        void contentPane_ItemClicked(object sender, EventArgs e)
        {
            ClickItem ci = (ClickItem)sender; // Base class
            if (! (ci is RPRequestClickItem)) return;
            RPRequestClickItem srci = (RPRequestClickItem)ci;  // Cast into derived class, we know it's a srci as it came from here
            RPRequest sr = srci.LinkedRequest;
            if (sr == null) return;
            RPRequestInfoPane srip;
            srip = new RPRequestInfoPane(sr);
            VisualManager.PushOntoScreenStack(srip);
        }


       

        private TextBlock tbGroupHeader(string groupName)
        {
            TextBlock tb = new TextBlock();
            tb.Margin = new Thickness(0, 25, 0, 0);
            tb.Text = groupName;
            tb.FontSize = 18.0;
            tb.Foreground = new SolidColorBrush(Functions.HexColor("#FFFFFFCC"));

            return tb;
        }

    }
}