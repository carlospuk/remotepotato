using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace SilverPotato
{
    public partial class EPGContainer : UserControl
    {
        private static DateTime CurrentDisplayDate;
        private static bool shouldJumpToNowHourWhenEPGLoaded;

        public EPGContainer()
        {
            InitializeComponent();

            // Events
            ScheduleManager.ChannelsUpdated += new EventHandler(ScheduleManager_ChannelsUpdated);
        }

        public void Initialize()
        {           
            // Render the correct filter button
            renderChannelFilterButton();

            // Load the actual EPG
            CurrentDisplayDate = DateTime.Now.Date;

            lblCurrentDate.Text = CurrentDisplayDate.ToPrettyLongDayNameAndDate();
            GenerateDayJumpLinks();
            DisplayDefaultEPG();

            InitScrolling();
            InitRefreshTimer();
            EnableAutoRefresh();  // starts timer.  Timer is suspended by VisualManager when removing container
        }

        private void DisplayDefaultEPG()
        {
            shouldJumpToNowHourWhenEPGLoaded = true;

            if (! ScheduleManager.GotChannelsFromServer)
            {
                Functions.WriteLineToLogFile("EPG Container: Waiting for channels to be updated.");
                VisualManager.ShowActivityModal();
            }
            else
                DisplayDefaultEPG_2();
        }
        void ScheduleManager_ChannelsUpdated(object sender, EventArgs e)
        {
            VisualManager.HideActivityModal();
            DisplayDefaultEPG_2();
        }
        private void DisplayDefaultEPG_2()
        {
            TheEPG.FillChannelsFromScheduleManager();
            FillWithCurrentDisplayDate();
        }
        private void FillWithCurrentDisplayDate()
        {
            HighlightCurrentDayJumpButton();

            Functions.WriteLineToLogFile("Refresh EPG: Full date requested is: " + CurrentDisplayDate.ToShortDateString() + " " + CurrentDisplayDate.ToShortTimeString() + " " + CurrentDisplayDate.Kind.ToString());
            lblCurrentDate.Text = CurrentDisplayDate.ToPrettyLongDayNameAndDate();
            
            // Respect global channel filter setting
            TheEPG.DisplayEPGForDate(CurrentDisplayDate, shouldJumpToNowHourWhenEPGLoaded);

            // Reset 'jump' flag
            if (shouldJumpToNowHourWhenEPGLoaded) shouldJumpToNowHourWhenEPGLoaded = false;
        }

        

        #region Auto Refresh / Auto Refresh Timer
        DispatcherTimer refreshTimer;
        void InitRefreshTimer()
        {
            refreshTimer = new DispatcherTimer();
            refreshTimer.Interval = TimeSpan.FromSeconds(20);  // CHANGE THIS
            refreshTimer.Tick += new EventHandler(refreshTimer_Tick);
        }
        public void EnableAutoRefresh()
        {
            if (! refreshTimer.IsEnabled )
                refreshTimer.Start();
        }
        public void DisableAutoRefresh()
        {
            if (refreshTimer.IsEnabled)
                refreshTimer.Stop();
        }
        void refreshTimer_Tick(object sender, EventArgs e)
        {
            RefreshDisplayedEPG();   // EPG takes care of whether this is sane, e.g. actually populated, any cells, nulls, etc.
        }
        public void RefreshDisplayedEPG()
        {
            TheEPG.RefreshEPG();
        }
        #endregion


        #region Day Jump Links
        private void GenerateDayJumpLinks()
        {
            spDayJumpLinks.Children.Clear();

            DateTime dateCounter = DateTime.Now.Date;
            for (int i = 0; i < 15; i++)
            {
                EPGDayJumpButton jb = new EPGDayJumpButton(dateCounter);
                jb.Click += new EventHandler<EPGDayJumpButtonEventArgs>(jb_Click);
                jb.Margin = new Thickness(2, 0, 0, 0);
                spDayJumpLinks.Children.Add(jb);

                if (i == 0) currentlyHighlightedDayJumpButton = jb;
                dateCounter = dateCounter.AddDays(1);
            }


        }
        EPGDayJumpButton currentlyHighlightedDayJumpButton;
        void jb_Click(object sender, EPGDayJumpButtonEventArgs e)
        {
            if (TheEPG.IsPopulating) return;

            DateTime destDate = e.SelectedDate;
            destDate = DateTime.SpecifyKind(destDate, DateTimeKind.Local);  // important as otherwise .ToLocal() calls (e.g. in pretty display methods) will pull it back into a different time zone
            CurrentDisplayDate = destDate;
            FillWithCurrentDisplayDate();
        }
        void HighlightCurrentDayJumpButton()
        {
            EPGDayJumpButton jb = JumpButtonForDate(CurrentDisplayDate);
            if (jb == null) return;

            currentlyHighlightedDayJumpButton.ClearHighlighted();
            jb.SetHighlighted();
            currentlyHighlightedDayJumpButton = jb;
        }
        EPGDayJumpButton JumpButtonForDate(DateTime localdate)
        {
            foreach (FrameworkElement fe in spDayJumpLinks.Children)
            {
                if (!(fe is EPGDayJumpButton)) continue;

                EPGDayJumpButton jl = (EPGDayJumpButton)fe;
                if (jl.StoredDate.Equals(localdate)) return jl;
            }
            return null;
        }
        #endregion
        #region DayJump Links Scroll Left/Right
        DispatcherTimer scrollTimer;
        enum ScrollStatus { Forward, Backward, Stopping, Stopped };
        ScrollStatus currentScrollStatus;
        void InitScrolling()
        {
            scrollTimer = new DispatcherTimer();
            scrollTimer.Interval = TimeSpan.FromMilliseconds(25);
            scrollTimer.Tick += new EventHandler(scrollTimer_Tick);
        }
        // Scroll Days left and right
        void StartScrolling()
        {
            scrollTimer.Start();
        }
        void StopScrolling()
        {
            scrollTimer.Stop();
        }
        void scrollTimer_Tick(object sender, EventArgs e)
        {
            double currentOffset = svDayJumpLinksContainer.HorizontalOffset;
            double newOffset;

            switch (currentScrollStatus)
            {
                case ScrollStatus.Stopping:
                    scrollTimer.Stop();
                    currentScrollStatus = ScrollStatus.Stopped;
                    break;

                case ScrollStatus.Forward:
                    newOffset = currentOffset + 10;
                    svDayJumpLinksContainer.ScrollToHorizontalOffset(newOffset);

                    break;

                case ScrollStatus.Backward:
                    newOffset = currentOffset - 10;
                    svDayJumpLinksContainer.ScrollToHorizontalOffset(newOffset);
                    break;

                default:
                    break;
            }

        }
        private void brdScrollDaysLeft_MouseEnter(object sender, MouseEventArgs e)
        {
            brdScrollDaysLeft.Opacity = 1.0;
            currentScrollStatus = ScrollStatus.Backward;
            if (!scrollTimer.IsEnabled)
                scrollTimer.Start();
        }
        private void brdScrollDaysLeft_MouseLeave(object sender, MouseEventArgs e)
        {
            brdScrollDaysLeft.Opacity = 0.6;
            currentScrollStatus = ScrollStatus.Stopping;

        }
        private void brdScrollDaysRight_MouseEnter(object sender, MouseEventArgs e)
        {
            brdScrollDaysRight.Opacity = 1.0;
            currentScrollStatus = ScrollStatus.Forward;
            if (!scrollTimer.IsEnabled)
                scrollTimer.Start();
        }
        private void brdScrollDaysRight_MouseLeave(object sender, MouseEventArgs e)
        {
            brdScrollDaysRight.Opacity = 0.6;
            currentScrollStatus = ScrollStatus.Stopping;
        }
        #endregion

        #region Fwd / Back Buttons
        private void btnGoBackOneDay_Click(object sender, RoutedEventArgs e)
        {
            CurrentDisplayDate = CurrentDisplayDate.AddDays(-1);
            FillWithCurrentDisplayDate();
        }
        private void btnGoFwdOneDay_Click(object sender, RoutedEventArgs e)
        {
            CurrentDisplayDate = CurrentDisplayDate.AddDays(1);
            FillWithCurrentDisplayDate();
        }
        private void btnGoBackOneDay_MouseLeave(object sender, MouseEventArgs e)
        {
            imgEPGGridLeftArrow.Width = 18;
        }
        private void btnGoBackOneDay_MouseEnter(object sender, MouseEventArgs e)
        {
            imgEPGGridLeftArrow.Width = 20;
        }
        private void btnGoFwdOneDay_MouseEnter(object sender, MouseEventArgs e)
        {
            imgEPGGridRightArrow.Width = 20;
        }
        private void btnGoFwdOneDay_MouseLeave(object sender, MouseEventArgs e)
        {
            imgEPGGridRightArrow.Width = 18;
        }
        
        #endregion

        #region Time Jump Buttons
        private void lblJump7am_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            TheEPG.JumpToTime(TimeSpan.FromHours(7));
        }
        private void lblJump12pm_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            TheEPG.JumpToTime(TimeSpan.FromHours(12));
        }
        private void lblJump7pm_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            TheEPG.JumpToTime(TimeSpan.FromHours(19));
        }
        private void lblJump10pm_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            TheEPG.JumpToTime(TimeSpan.FromHours(22));
        }
        private void lblJumpNow_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (CurrentDisplayDate == DateTime.Now.Date)
            {
                TheEPG.JumpToNow();
            }
            else
            {
                CurrentDisplayDate = DateTime.Now.Date;
                shouldJumpToNowHourWhenEPGLoaded = true;
                FillWithCurrentDisplayDate();
            }
        }

        private void brdAnyJumpLink_MouseEnter(object sender, MouseEventArgs e)
        {
            Border b = (Border)sender;
            b.Background = new SolidColorBrush(Colors.Yellow);
        }
        private void brdAnyJumpLink_MouseLeave(object sender, MouseEventArgs e)
        {
            Border b = (Border)sender;
            b.Background = new SolidColorBrush(Functions.HexColor("#DDDDDD"));
        }
        #endregion



        #region Channel Filter

        private void btnChannelFilter_MouseEnter(object sender, MouseEventArgs e)
        {
            gsChannelFilterBase.Color = Functions.HexColor("#CCCCCC");
        }

        private void btnChannelFilter_MouseLeave(object sender, MouseEventArgs e)
        {
            gsChannelFilterBase.Color = Functions.HexColor("#777777");
        }
        private void btnChannelFilter_Click(object sender, RoutedEventArgs e)
        {
            Settings.ToggleChannelFilter();
            renderChannelFilterButton();
            ScheduleManager.CalculateEPGDisplayedChannels();
            
            // Refresh channel list
            TheEPG.PopulateAllChannelStrips();
            TheEPG.FillChannelsFromScheduleManager();

            // Display correct date - this will create a ScheduleDay that includes the new filter etc.
            FillWithCurrentDisplayDate();
            TheEPG.AdjustScrollbarsForContent();  // safety
        }
        void renderChannelFilterButton()
        {
            imgChannelFilterButton.Source = Settings.ChannelFilter == ChannelFilterTypes.AllChannels ?
                ImageManager.LoadImageFromContentPath("/Images/txtFilterAllChannels.png") :
                ImageManager.LoadImageFromContentPath("/Images/txtFilterFavouriteChannels.png");
        }
        #endregion


    }
}
