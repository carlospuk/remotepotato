using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using CommonEPG;

namespace SilverPotato
{

    public partial class EPGGrid : UserControl
    {
        public List<EPGStrip> displayedEPGStrips;
        public List<EPGStrip> stripsInLastViewport;
        const int MARGIN_BEFORE_SELECTED_TIME = 50;
        bool IsInitializing;
        System.Threading.Timer ScrollTimeoutTimer;

        const int EPG_CELL_HEIGHT = 62;

        public EPGGrid()
        {
            IsInitializing = true;
            InitializeComponent();
           

            Loaded += new RoutedEventHandler(EPGGrid_Loaded);
        }

        void EPGGrid_Loaded(object sender, RoutedEventArgs e)
        {
            RegisterForScrollViewDependencyPropertyEvents();

            displayedEPGStrips = new List<EPGStrip>();
            PopulateTimeHeaders();

            int EPGhoursLong = 24 + SettingsImporter.SettingAsIntOrZero("SilverlightEPGOverspillHours");
            cvChannelStrips.Width = (Functions.EPGZoomFactor * (60 * EPGhoursLong));
            stripsInLastViewport = new List<EPGStrip>();

            TimerCallback tCallBack = new TimerCallback(ScrollTimeoutTimer_fire);
            ScrollTimeoutTimer = new Timer(tCallBack, null, 200, 200);

            IsInitializing = false;
        }

        // General
        public void JumpToNow()
        {
            TimeSpan nowTimeSpan = DateTime.Now.TimeOfDay;
            JumpToTime(nowTimeSpan);
        }
        public void JumpToTime(TimeSpan time)
        {
            double pixelHour = (time.TotalHours * 60 * Functions.EPGZoomFactor);
            pixelHour = pixelHour - MARGIN_BEFORE_SELECTED_TIME;
            if (pixelHour < 0) pixelHour = 0;


            svProgrammes.ScrollToHorizontalOffset(pixelHour);
            AdjustLeftmostCellsTextInAllStrips();
        }


        #region Current Time Line
        void DrawCurrentTimeLine()
        {
            TimeSpan nowTimeSpan = DateTime.Now.TimeOfDay;
            double pixelHour = (nowTimeSpan.TotalHours * 60 * Functions.EPGZoomFactor);
            //pixelHour = (pixelHour - 8);  // Shift to the left out of sight of the default frame
            //if (pixelHour < 0) pixelHour = 0;
            cvCurrentTime.Height = cvChannelStrips.ActualHeight;
            cvCurrentTime.Margin = new Thickness(pixelHour, 0, 0, 0);
            cvCurrentTime.SetValue(Canvas.ZIndexProperty, Functions.ZOrderHighest);
            cvCurrentTime.Visibility = Visibility.Visible;
        }
        void HideCurrentTimeLine()
        {
            cvCurrentTime.Visibility = Visibility.Collapsed;
        }
        #endregion

        #region Channels
        // After a strip is filled, adjust the leftmost cell so label is visible
        public void FillChannelsFromScheduleManager()
        {
            double baseWidth = (SettingsImporter.SettingIsTrue("EPGShowChannelNumbers")) ?
                128 :
                88;

            // Approx column width - depending on channel numbers
            cdChannels.Width = new GridLength(baseWidth + 6);
            spChannels.Width = baseWidth;
            svChannels.Width = baseWidth + 6;

            // Clear existing channels
            spChannels.Children.Clear();
            // Put up channels
            foreach (TVService tvc in ScheduleManager.EPGDisplayedChannels)
            {
                spChannels.Children.Add(new EPGChannelCell(tvc));
            }

            double newChannelCanvasHeight = (double)(EPG_CELL_HEIGHT * ScheduleManager.EPGDisplayedChannels.Count);
            cvChannelStrips.SetValue(Canvas.HeightProperty, (double)newChannelCanvasHeight);
            AdjustScrollbarsForContent();
        }
        #endregion

        #region Time Markers
        void PopulateTimeHeaders()
        {
            // Do time markers
            spTimeHeaders.Children.Clear();
            int markerWidthMins = 30;
            long timeHeaderCounter = DateTime.Now.Date.ToUniversalTime().Ticks;
            int EPGhoursLong = 24 + SettingsImporter.SettingAsIntOrZero("SilverlightEPGOverspillHours");
            long endWindowTime = timeHeaderCounter + (TimeSpan.FromHours(EPGhoursLong).Ticks);
            do
            {
                EPGTimeMarker newMarker = new EPGTimeMarker(new DateTime(timeHeaderCounter).ToLocalTime().ToShortTimeString(), markerWidthMins);
                spTimeHeaders.Children.Add(newMarker);

                timeHeaderCounter += (TimeSpan.FromMinutes(markerWidthMins).Ticks);

            } while (timeHeaderCounter < endWindowTime);


        }
        #endregion

        #region Programmes
        public bool IsPopulating;
        DateTime currentDisplayDate;


        /// <summary>
        /// Fade programmes that have ended and move the current time line
        /// </summary>
        public void RefreshEPG()
        {
            // All these methods must be safe when there are no cells / EPG / channels etc.

            if (cvCurrentTime.Visibility == Visibility.Visible)
                DrawCurrentTimeLine();

            FadeCellsWithProgrammeEnded();
        }
        bool shouldJumpToNowAfterPopulatingStrips;
        public void DisplayEPGForDate(DateTime theDate, bool jumpToNowWhenLoaded)
        {
            if (IsPopulating) return;

            IsPopulating = true;

            shouldJumpToNowAfterPopulatingStrips = jumpToNowWhenLoaded;

            currentDisplayDate = theDate;
            Animations.DoFadeOut(0.3, cvChannelStrips, DisplayEPGForDate_2);
        }
        void DisplayEPGForDate_2(object sender, EventArgs e)
        {
            RemoveAllChannelStripsSafely();
            PopulateAllChannelStrips();

            // Fade back up
            Animations.DoFadeIn(0.2, cvChannelStrips);

            FillViewport();

            // Today?
            if (currentDisplayDate.Equals(DateTime.Now.Date))
                DrawCurrentTimeLine();
            else
                HideCurrentTimeLine();

            // Jump to now?
            if (shouldJumpToNowAfterPopulatingStrips) JumpToNow();
        }

        // Fill viewport only  (and clear strips outside viewport 'window')
        private void FillViewport()
        {
            List<EPGStrip> stripsInViewPort = StripsInViewport();
            // Clear/delete strips not in view
            // For maximum efficiency, only iterate through the strips in the last viewport when clearing, checking they're not also in the new viewport
            foreach (EPGStrip strip in stripsInLastViewport)
            {
                if (!(stripsInViewPort.Contains(strip)))
                {
                    // Strips no longer in viewport
                    strip.ClearStrip(); // this checks if it's currently filling and flags to cancel too
                }
            }

            // Compile a list of strips to fill:
            //  1. Not already full
            //  2. Not already filling
            List<EPGStrip> stripsToFill = new List<EPGStrip>();
            foreach (EPGStrip strip in stripsInViewPort)
            {
                if (strip.HasFilled) continue;  // for day-changes to work, this test relies on strips being destroyed and re-constructed between EPG viewing different DAYS
                if (strip.IsFilling) continue;

                stripsToFill.Add(strip);
            }

            // Now we know how many strips need filling, we can initialise a new request manager
            EPGRequestManager requestManager = new EPGRequestManager();
            requestManager.Initialize(stripsToFill.Count);

            // Now activate each strip in the list of strips to fill
            foreach (EPGStrip strip in stripsToFill)
            {
                // Fill the strip  
                strip.FillStrip(currentDisplayDate, requestManager);
            }

            // Track changes
            stripsInLastViewport = stripsInViewPort;
            IsPopulating = false;
        }
        List<EPGStrip> StripsInViewport()
        {
            // If we're not loading the EPG lazily, then return all the strips there are.. ..ouch.
            if (Settings.EPGDontLoadLazily)
                return displayedEPGStrips;

            int extraStripsAboveAndBelow = 2;

            double ScrollOffset = svProgrammes.VerticalOffset;
            int topIndex = Convert.ToInt32(Math.Floor(ScrollOffset / EPG_CELL_HEIGHT));
            double VerticalStripsInWindow = Math.Ceiling(svProgrammes.ActualHeight / EPG_CELL_HEIGHT) + 1;

            topIndex = topIndex - extraStripsAboveAndBelow;
            if (topIndex < 0) topIndex = 0;
            int bottomIndex = topIndex + (int)VerticalStripsInWindow + extraStripsAboveAndBelow;

            List<EPGStrip> output = new List<EPGStrip>();
            for (int index = topIndex; index < bottomIndex; index++)
            {
                if (!(displayedEPGStrips.Count > index)) break;
                EPGStrip nextStrip = displayedEPGStrips[index];

                output.Add(nextStrip);
            }

            return output;
        }

        // Create the ChannelStrips
        public void PopulateAllChannelStrips()
        {
            RemoveAllChannelStripsSafely();

            int index = 0;
            bool bgAlternateColour = false;
            foreach (TVService tvs in ScheduleManager.EPGDisplayedChannels)
            {
                // New stackpanel
                EPGStrip strip = new EPGStrip();
                strip.Index = index;
                strip.TVServiceID = tvs.UniqueId;
                strip.DefaultBackgroundColour = (bgAlternateColour) ? Color.FromArgb(255, 30, 30, 40) : Color.FromArgb(255, 10, 10, 10);

                // Events
                strip.CellClicked += new EventHandler<GenericEventArgs<TVProgramme>>(channelStrip_CellClicked);
                strip.FillStrip_Complete += new EventHandler(strip_FillStrip_Complete);

                double top = (EPG_CELL_HEIGHT * index);
                strip.SetValue(Canvas.LeftProperty, (double)0);
                strip.SetValue(Canvas.TopProperty, top);
                cvChannelStrips.Children.Add(strip);
                displayedEPGStrips.Add(strip);


                index++;
                bgAlternateColour = !bgAlternateColour;
            }

            IsPopulating = false;
        }

        void strip_FillStrip_Complete(object sender, EventArgs e)
        {
            TimerCallback cb = new TimerCallback(DoAdjustLeftmostCells);
            System.Threading.Timer adjustTimer = new Timer(cb, sender, 200, System.Threading.Timeout.Infinite);
        }
        delegate void AdjustCellsDelegate(object state);
        void DoAdjustLeftmostCells(object sender)
        {
            AdjustCellsDelegate d = new AdjustCellsDelegate(DoDoAdjustLeftmostCells);
            Dispatcher.BeginInvoke(d, sender);
        }
        void DoDoAdjustLeftmostCells(object sender)
        {
            AdjustLeftmostCellsText((EPGStrip)sender);
        }
        #endregion

        private void RemoveAllChannelStripsSafely()
        {
            foreach (FrameworkElement fe in cvChannelStrips.Children)
            {
                if (fe is TextBlock)
                {
                    TextBlock tb = (TextBlock)fe;
                    tb = null;
                }

            }

            foreach (EPGStrip strip in displayedEPGStrips)
            {
                // Unwire events
                strip.CellClicked -= new EventHandler<GenericEventArgs<TVProgramme>>(channelStrip_CellClicked);
                strip.FillStrip_Complete -= new EventHandler(strip_FillStrip_Complete);

                strip.Dispose();  // also cancels any fill operations
            }

            // Clear visual children
            cvChannelStrips.Children.Clear();

            // Clear strips array
            displayedEPGStrips.Clear();
        }
        public void AdjustScrollbarsForContent()
        {
            /*
            svProgrammes.UpdateLayout();
            //sbVertical.Maximum = svProgrammes.ExtentHeight - svProgrammes.ViewportHeight;
            sbVertical.Maximum = cvChannelStrips.ActualHeight - svProgrammes.ViewportHeight;
            sbHorizontal.Maximum = svProgrammes.ExtentWidth - svProgrammes.ViewportWidth;
            sbVertical.ViewportSize = svProgrammes.ViewportHeight;
            sbHorizontal.ViewportSize = svProgrammes.ViewportWidth;

            sbHorizontal.SmallChange = Convert.ToDouble((60 * Functions.EPGZoomFactor));
            sbHorizontal.LargeChange = Convert.ToDouble((180 * Functions.EPGZoomFactor));
            */
        }
        void FadeCellsWithProgrammeEnded()
        {
            foreach (EPGStrip strip in displayedEPGStrips)
            {
                strip.FadeCellsWithProgrammeEnded();
            }
        }



        #region Cell Clicks
        // EPG Cell is clicked
        void channelStrip_CellClicked(object sender, GenericEventArgs<TVProgramme> e)
        {
            // Make info pane from schedule request if there is one.
            ShowInfoPane infoPane = new ShowInfoPane(e.Value);
            VisualManager.PushOntoScreenStack(infoPane);
        }
        #endregion

        #region Cell Scrolling / Mouse Wheel

        void RegisterForScrollViewDependencyPropertyEvents()
        {
            Functions.RegisterForNotification("HorizontalOffset", svProgrammes, OnHorizontalOffsetChanged);
            Functions.RegisterForNotification("VerticalOffset", svProgrammes, OnVerticalOffsetChanged);
            Functions.RegisterForNotification("VerticalOffset", svChannels, OnVerticalOffsetChanged);
        }
        public void OnHorizontalOffsetChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            SynchroniseHorizontalScrolling((double)e.NewValue);   
        }
        public void OnVerticalOffsetChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            SynchroniseverticalScrolling((double)e.NewValue);
        }


        DateTime timeOfLastVerticalScroll = new DateTime();
        bool awaitingScrollTimeout = false;

        bool isSynchronisingVerticalScrolling;
        void SynchroniseverticalScrolling(double verticalOffset)
        {
            if (IsInitializing) return;

            if (isSynchronisingVerticalScrolling) return;  // prevent recurrence
            isSynchronisingVerticalScrolling = true;

            // Adjust cells that might not have been in view before

            Dispatcher.BeginInvoke(AdjustLeftmostCellsTextInAllStrips);
            svProgrammes.ScrollToVerticalOffset(verticalOffset);
            svChannels.ScrollToVerticalOffset(verticalOffset);

            timeOfLastVerticalScroll = DateTime.Now; // timer (method below) calls FillViewPort 1.5s after last scroll
            awaitingScrollTimeout = true;
            isSynchronisingVerticalScrolling = false;
        }
        // Fires every second - draw new grid strips that have come into view
        void ScrollTimeoutTimer_fire(object State)
        {
            if (Settings.EPGDontLoadLazily) return;
            if (!awaitingScrollTimeout) return;

            TimeSpan timeSinceLastVerticalScroll = DateTime.Now.Subtract(timeOfLastVerticalScroll);
            if (timeSinceLastVerticalScroll.TotalMilliseconds > 250)
            {
                awaitingScrollTimeout = false;
                Dispatcher.BeginInvoke(FillViewport);
            }
        }


        // HORIZONTAL 
        bool isSynchronisingHorizontalScrolling;
        void SynchroniseHorizontalScrolling(double horizontalOffset)
        {
            if (IsInitializing) return;
            if (isSynchronisingHorizontalScrolling) return;  // prevent recurrence
            isSynchronisingHorizontalScrolling = true;

            svTimeHeaders.ScrollToHorizontalOffset(horizontalOffset);
            svProgrammes.ScrollToHorizontalOffset(horizontalOffset);
            Dispatcher.BeginInvoke(AdjustLeftmostCellsTextInAllStrips);

            isSynchronisingHorizontalScrolling = false;
        }


        #endregion



        #region Clever Label Scrolling
        List<EPGCell> currentlyShiftedCells = new List<EPGCell>();
        IEnumerable<UIElement> LeftmostEPGCells()
        {
            Rect svRectInHost = svProgrammes.GetBoundsRelativeTo(Application.Current.RootVisual).Value;
            Rect svThinRectInHost = new Rect(svRectInHost.X, svRectInHost.Y, 1, svRectInHost.Height);


            var elementsOnLeft = VisualTreeHelper.FindElementsInHostCoordinates(svThinRectInHost, this.LayoutRoot);
            var cellItems = from item in elementsOnLeft
                            where item is EPGCell
                            select item;
            return cellItems;
        }
        IEnumerable<UIElement> LeftmostEPGCellInStrip(EPGStrip strip)
        {
            double stripPositionWithinSV = strip.Index * EPG_CELL_HEIGHT;
            double stripPositionOnScreen = (stripPositionWithinSV - svProgrammes.VerticalOffset);

            Rect svRectInHost = svProgrammes.GetBoundsRelativeTo(Application.Current.RootVisual).Value;

            Rect svStripRectInHost = new Rect(svRectInHost.X, (svRectInHost.Y + stripPositionOnScreen), 1, strip.ActualHeight);

            var elementsOnLeft = VisualTreeHelper.FindElementsInHostCoordinates(svStripRectInHost, this.LayoutRoot);
            var cellItems = from item in elementsOnLeft
                            where item is EPGCell
                            select item;
            return cellItems;
        }
        void AdjustLeftmostCellsTextInAllStrips()
        {
            AdjustLeftmostCellsText(null);
        }
        private void AdjustLeftmostCellsText(EPGStrip limitToThisStrip)  // pass NULL for all strips
        {
            if (!Settings.EPGSmartScrolling) return;
            bool AdjustAllStrips = (limitToThisStrip == null);


            // Which cells are to be shifted
            List<EPGCell> cellsToBeShifted = new List<EPGCell>();
            if (AdjustAllStrips)
            {
                foreach (UIElement ui in LeftmostEPGCells())
                {
                    cellsToBeShifted.Add((EPGCell)ui);
                }
            }
            else // Limit to one strip
            {
                foreach (UIElement ui in LeftmostEPGCellInStrip(limitToThisStrip))
                {
                    cellsToBeShifted.Add((EPGCell)ui);
                }
            }


            // Which cells are no longer to be shifted
            if (AdjustAllStrips)
            {
                List<EPGCell> cellsToBeUnshifted = currentlyShiftedCells.Except(cellsToBeShifted).ToList();

                // Unshift old cells
                foreach (EPGCell cell in cellsToBeUnshifted)
                {
                    cell.resetLabels();
                }

                // Now reset the master list of currently shifted cells for re-population
                currentlyShiftedCells.Clear();
            }


            foreach (UIElement ui in cellsToBeShifted)
            {
                EPGCell epgc = (EPGCell)ui; // guaranteed

                // work out amount overlapping to the left
                double cellXPos = (double)epgc.GetValue(Canvas.LeftProperty);
                double overlapAmount = (svProgrammes.HorizontalOffset - cellXPos);

                epgc.shiftLabelsToX(overlapAmount);

                if (!currentlyShiftedCells.Contains(epgc))
                    currentlyShiftedCells.Add(epgc);
            }


        }


        #endregion





    }


    
}
