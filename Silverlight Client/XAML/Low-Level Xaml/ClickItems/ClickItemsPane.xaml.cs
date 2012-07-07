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
using System.Threading;


namespace SilverPotato
{
    public partial class ClickItemsPane : UserControl
    {
        Dictionary<string, List<ClickItem>> DisplayedItems;
        ClickItemsPaneLayouts PaneLayout;
        public ClickItemsPaneItemLayouts DisplayedItemsLayout;
        public bool ShowHeaders = true;

        // Constructors
        public ClickItemsPane()
        {
            InitializeComponent();


            
            Loaded += new RoutedEventHandler(ClickItemsPane_Loaded);
        }
        
        void ClickItemsPane_Loaded(object sender, RoutedEventArgs e)
        {
            // Mouse wheel
            Functions.RegisterForNotification("VerticalOffset", svContent, OnVerticalOffsetChanged);


            // Scale widths etc
            // Scrollviewer width equals width of container minus scrollbar
            //svContent.Width = spContentAndScrollBar.ActualWidth - sbContent.ActualWidth;
        }


        
        public ClickItemsPane(Dictionary<string, List<ClickItem>> items) 
            : this(items, ClickItemsPaneLayouts.PaneAndToolbar, ClickItemsPaneItemLayouts.List) { }
        public ClickItemsPane(Dictionary<string, List<ClickItem>> items, ClickItemsPaneLayouts layout, ClickItemsPaneItemLayouts format) : this()
        {
            DisplayedItems = items;

            WireUpEventsFromDisplayedItems();
            PaneLayout = layout;
            DisplayedItemsLayout = format;
            LayoutFromItems(false);
        }


        // Public Events
        public event EventHandler ItemClicked;
        public event EventHandler AwaitingRefreshedContent;

        #region Public Styling Methods
        // Show/Hide Pane
        public void ShowPaneControls()
        {
            rdPaneViewControls.Height = new GridLength(30);
        }
        public void HidePaneControls()
        {
            rdPaneViewControls.Height = new GridLength(0);
        }
        public void HideRefreshControl()
        {
            btnRefresh.Visibility = Visibility.Collapsed;
        }
        public void SetContentMargin(Thickness newMargin)
        {
            svContent.Margin = newMargin;
        }
        #endregion

        #region Layout 
        public void ReplaceItemsWithNewItems(Dictionary<string, List<ClickItem>> items)
        {
            UnwireEventsFromDisplayedItems(); // Clear the events
            DisplayedItems = items; // Update the internal array
            ClearContentPaneCompleted += new EventHandler(ReplaceItemsWithNewItems_2);
            ClearContentPaneAsync(); // Clear the GUI stuff
        }
        void ReplaceItemsWithNewItems_2(object sender, EventArgs e)
        {
            ClearContentPaneCompleted -= new EventHandler(ReplaceItemsWithNewItems_2);
            WireUpEventsFromDisplayedItems(); // Reset the events
            
            LayoutFromItems(false); // Insert new GUI stuff
        }
        bool ShouldReFormatClickItems = false;
        public void LayoutFromItems(bool reformatClickItems)
        {
            if (DisplayedItems == null) return;  // unpopulated yet
            ShouldReFormatClickItems = reformatClickItems; // temp store

            ClearContentPaneCompleted += new EventHandler(LayoutFromItems_2);
            ClearContentPaneAsync();
        }
        void LayoutFromItems_2(object sender, EventArgs e)
        {
            ClearContentPaneCompleted -= new EventHandler(LayoutFromItems_2);
            // Set each clickItem to the correct display format, e.g. list or thumbnail
            if (ShouldReFormatClickItems)
                SetAllItemsToCurrentDisplayedItemsLayout();

            // Place click items into an appropriate container (stackpanel or wrappanel)
            foreach (KeyValuePair<string, List<ClickItem>> kvp in DisplayedItems)
            {
                string txtHeader = RemoveSquareBrackets(kvp.Key);
                //if (! string.IsNullOrEmpty(txtHeader )) // Don't add blank headers?
                if (ShowHeaders)
                    spContent.Children.Add(groupHeader(txtHeader));

                if (DisplayedItemsLayout == ClickItemsPaneItemLayouts.Thumbnails)
                {
                    WrapPanel wp = new WrapPanel();
                    wp.HorizontalAlignment = HorizontalAlignment.Stretch;
                    wp.Clip = null;
                    foreach (ClickItem ci in kvp.Value)
                    {
                        ci.Margin = new Thickness(1);
                        try
                        {
                            wp.Children.Add(ci);
                        }
                        catch (Exception ex)
                        {
                            Functions.WriteLineToLogFile("LayoutFromItems_2: Cannot add wrapPanel to content pane.");
                            Functions.WriteExceptionToLogFile(ex);
                        }  
                    }
                    spContent.Children.Add(wp);
                }
                else // LIST OR 2-COLUMN LIST
                {
                    StackPanel sp = new StackPanel();
                    sp.HorizontalAlignment = HorizontalAlignment.Stretch;
                    foreach (ClickItem ci in kvp.Value)
                    {
                        ci.Margin = new Thickness(1);

                        try
                        {
                            sp.Children.Add(ci);  // error: element is already the child of another element
                        }
                        catch
                        {}
                        
                    }
                    spContent.Children.Add(sp);
                }
            }

            spContent.UpdateLayout();
            ProcessEndScroll();

            // Faded?
            spContent.Opacity = 1.0;

            // Load thumbnails for items in view
            LoadThumbnailsForClickItemsInView();
        }
        private void SetAllItemsToCurrentDisplayedItemsLayout()
        {
            switch (DisplayedItemsLayout)
            {
                case ClickItemsPaneItemLayouts.List:
                    SetAllItemsToClickItemLayout(ClickItem.ClickItemLayouts.TextOnly);
                    break;

                case ClickItemsPaneItemLayouts.ListTwoColumns:
                    SetAllItemsToClickItemLayout(ClickItem.ClickItemLayouts.TextWithRightColumn);
                    break;

                case ClickItemsPaneItemLayouts.Thumbnails:
                    SetAllItemsToClickItemLayout(ClickItem.ClickItemLayouts.ThumbnailWithOverlay);
                    break;
            }
        }
        private void SetAllItemsToClickItemLayout(ClickItem.ClickItemLayouts newLayout)
        {
            foreach (List<ClickItem> lstItems in DisplayedItems.Values)
            {
                foreach (ClickItem ci in lstItems)
                {
                    ci.SetLayout(newLayout);
                }
            }

            // Load thumbnails
            LoadThumbnailsForClickItemsInView();
        }
        private void WireUpEventsFromDisplayedItems()
        {
            if (DisplayedItems == null) return;

            foreach (List<ClickItem> lstItems in DisplayedItems.Values)
            {
                foreach (ClickItem ci in lstItems)
                {
                    ci.Clicked += new EventHandler(clickItem_Clicked);
                }
            }
        }
        private void UnwireEventsFromDisplayedItems()
        {
            if (DisplayedItems == null) return;

            foreach (List<ClickItem> lstItems in DisplayedItems.Values)
            {
                foreach (ClickItem ci in lstItems)
                {
                    ci.Clicked -= new EventHandler(clickItem_Clicked);
                }
            }
        }
        private string RemoveSquareBrackets(string inputString)
        {
            int startSquares = inputString.IndexOf("[");
            int endSquares = inputString.LastIndexOf("]");
            if ((startSquares < 0) || (endSquares < 0)) return inputString;

            string replaceString = inputString.Substring(startSquares, endSquares - startSquares + 1);
            return inputString.Replace(replaceString, "");
        }
        event EventHandler ClearContentPaneCompleted;
        public void ClearContentPaneAsync()
        {
            // Fade the panel before wiping it
            Animations.DoFadeOut(0.15, spContent, ClearContentPaneAsync_2);
        }
        void ClearContentPaneAsync_2(object sender, EventArgs e)
        {
            // Clear any child WrapPanels
            foreach (FrameworkElement child in spContent.Children)
            {
                if (child is WrapPanel)
                {
                    WrapPanel wp = (WrapPanel)child;
                    wp.Children.Clear();
                }
                else if (child is StackPanel)
                {
                    StackPanel sp = (StackPanel)child;
                    sp.Children.Clear();
                }
            }

            // Clear all the parent chidlren (textboxes and wrapPanels)
            spContent.Children.Clear();

            // re-show the empty panel
            spContent.Opacity = 1.0;

            // Allow the (syncronous) method ClearContentPanel to return
            if (ClearContentPaneCompleted != null) ClearContentPaneCompleted(new object(), new EventArgs());
        }
        private TextBlock groupHeader(string txtTitle)
        {
            TextBlock tb = new TextBlock();
            tb.Foreground = new SolidColorBrush(Colors.White);
            tb.FontSize = 18;
            tb.FontFamily = new FontFamily("Arial");
            tb.FontWeight = FontWeights.Bold;
            tb.Text = txtTitle;

            tb.Margin = string.IsNullOrEmpty(txtTitle.Trim()) ?
                new Thickness(0.0, 10, 0, 0) :
                new Thickness(0.0, 15, 0, 0);
            
            
            return tb;
        }
        #endregion

        // Events
        void clickItem_Clicked(object sender, EventArgs e)
        {
            if (ItemClicked != null) ItemClicked(sender, e);
        }

        #region Display Buttons
        private void btnViewTextOnly_Click(object sender, RoutedEventArgs e)
        {
            DisplayedItemsLayout = ClickItemsPaneItemLayouts.List;
            LayoutFromItems(true);
        }
        private void btnViewThumbnails_Click(object sender, RoutedEventArgs e)
        {
            DisplayedItemsLayout = ClickItemsPaneItemLayouts.Thumbnails;
            LayoutFromItems(true);
        }

        // Mouse events
        private void btnRefresh_MouseLeftButtonUp_1(object sender, MouseButtonEventArgs e)
        {
            // Clear selection / fade
            ReplaceItemsWithNewItems(null);

            if (AwaitingRefreshedContent != null) AwaitingRefreshedContent(this, new EventArgs());
        }
        private void btnRefresh_MouseEnter(object sender, MouseEventArgs e)
        {
            btnRefresh.Opacity = 1.0;
        }
        private void btnRefresh_MouseLeave(object sender, MouseEventArgs e)
        {
            btnRefresh.Opacity = 0.8;
        }
        #endregion

        public enum ClickItemsPaneLayouts
        {
            PaneAndToolbar,
            PaneOnly
        }
        public enum ClickItemsPaneItemLayouts
        {
            List,
            ListTwoColumns,
            Thumbnails
        }

        // Workaround
        private void svContent_LostFocus(object sender, RoutedEventArgs e)
        {
            svContent.Focus();
        }
        public void OnVerticalOffsetChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(ProcessEndScroll);
        }
        void ProcessEndScroll()
        {
            // Work out which clickItems are in view
            // and ask them to update their thumbnails
            LoadThumbnailsForClickItemsInView();
        }

        void LoadThumbnailsForClickItemsInView()
        {
            foreach (FrameworkElement fe in spContent.Children)
            {
                if (fe is WrapPanel)
                {
                    WrapPanel wp = (WrapPanel)fe;
                    foreach (FrameworkElement fe2 in wp.Children)
                    {
                        if (fe2 is ClickItem)
                        {
                            if (fe2.IsInView(svContent))
                            {
                                ClickItem ci = (ClickItem)fe2;
                                ci.LoadThumbnail();
                            }
                        }
                    }

                }
            }
        }


    }


    // Extension Methods

}
