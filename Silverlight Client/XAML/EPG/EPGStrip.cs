using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Threading;
using System.ComponentModel;
using System.Collections.Generic;
using CommonEPG;

namespace SilverPotato
{
    public class EPGStrip : Canvas
    {
        public DateTime TheLocalDate { get; set; }
        public int Index { get; set; }
        public string TVServiceID { get; set; }
        public bool HasFilled { get; set; }   // has been filled, all 26 hours of it
        public bool IsFilling { get; set; }
        public bool ShouldCancelFill {get; set;}
        public bool ContainsMessageCells { get; set; }

        // Private objects
        Dictionary<string, EPGCell> displayedEPGCells;
        EPGRequestManager requestManager;
        Color _defaultBackgroundColour;

        public EPGStrip()
            : base()
        {
            // Defaults
            displayedEPGCells = new Dictionary<string, EPGCell>();
            IsFilling = false;
            HasFilled = false;
            ShouldCancelFill = false;
            ContainsMessageCells = false;

            // Default setup
            this.Height = 62.0; // Same height as an EPG cell (allowing for the 1px margin on top and bottom)
            int EPGhoursLong = 24 + SettingsImporter.SettingAsIntOrZero("SilverlightEPGOverspillHours");
            this.Width = (double)(Functions.EPGZoomFactor * (60 * (24 + EPGhoursLong)));
        }

        
        public void Dispose()
        {
            ShouldCancelFill = true;

            // Dispose everything!
            foreach (FrameworkElement fe in Children)
            {
                if (fe is EPGCell)
                {
                    EPGCell cell = (EPGCell)fe;
                    cell.Dispose();
                    cell = null;
                }

                if (fe is TextBlock)
                {
                    TextBlock tb = (TextBlock)fe;
                    tb = null;
                }
            }

            foreach (EPGCell epgc in displayedEPGCells.Values)
            {
                epgc.Dispose();
            }
            displayedEPGCells.Clear();

            // Remove all children
            Children.Clear();
        }

        public void FadeCellsWithProgrammeEnded()
        {
            if (displayedEPGCells == null) return;

            foreach (EPGCell cell in displayedEPGCells.Values)
            {
                cell.FadeCellIfProgrammeEnded();
            }
        }


        #region Get Programmes
        public event EventHandler FillStrip_Complete;
        public void FillStrip(DateTime theLocalDate, EPGRequestManager _requestManager)
        {
            if (IsFilling) return; // already filling

            IsFilling = true;
            ShouldCancelFill = false;
            HasFilled = false;

            // Store
            TheLocalDate = theLocalDate;

            requestManager = _requestManager;
            requestManager.EPGRequest_Available -= new EventHandler<GenericEventArgs<EPGStrip>>(requestManager_EPGRequest_Available);
            requestManager.EPGRequest_Available += new EventHandler<GenericEventArgs<EPGStrip>>(requestManager_EPGRequest_Available);
            requestManager.EPGRequest_WillGetFromServer -= new EventHandler<GenericEventArgs<EPGStrip>>(requestManager_EPGRequest_WillGetFromServer);
            requestManager.EPGRequest_WillGetFromServer += new EventHandler<GenericEventArgs<EPGStrip>>(requestManager_EPGRequest_WillGetFromServer);
            
            /* THIS WAS CAUSING SOME REAL THREADING ISSUES WITH FLAGS ETC, REALLY BEST AVOID - PROMISE!  
             * BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(DoFillStrip);
            worker.RunWorkerAsync();*/

            requestManager.ReportEPGRequest(this, TheEPGRequest);
        }

        void requestManager_EPGRequest_WillGetFromServer(object sender, GenericEventArgs<EPGStrip> e)
        {
            if (!e.Value.Equals(this)) return;  // is it for us?

            // There will be a delay, put up a text box(es)
            Dispatcher.BeginInvoke(FillWithPleaseWaitCells);
        }
        void FillWithPleaseWaitCells()
        {
            FillWithMessageCells("Fetching Shows from Server");
        }

        void requestManager_EPGRequest_Available(object sender, GenericEventArgs<EPGStrip> e)
        {
            if (! e.Value.Equals(this)) return;  // is it for us?

            // Yes, so fill (on UI thread)
            Dispatcher.BeginInvoke(FillFromProgrammeStore);
        }
        void FillFromProgrammeStore()
        {
            // LOCK
            Monitor.Enter(ScheduleManager.TVProgrammeStore);

            if (ShouldCancelFill)
            {
                // Booo!  We've been cancelled like Studio 60
                IsFilling = false;
                ShouldCancelFill = false;
                HasFilled = false;  // safety
                Monitor.Exit(ScheduleManager.TVProgrammeStore);
                return;
            }

            if (ContainsMessageCells)
            {
                this.Children.Clear();
                ContainsMessageCells = false;
            }

            long startTimeWindow = TheLocalDate.ToUniversalTime().Ticks;
            int EPGhoursLong = 24 + SettingsImporter.SettingAsIntOrZero("SilverlightEPGOverspillHours");
            long endTimeWindow = startTimeWindow + (TimeSpan.FromHours(EPGhoursLong).Ticks);

            long timeHeaderCounter = startTimeWindow;

            // Track current time
            bool FoundAnyProgrammesForThisService = false;

            // Clear
            this.Children.Clear();

            foreach (TVProgramme tvp in ScheduleManager.TVProgrammeStore.Values)
            {
                if (tvp.ServiceID == TVServiceID)
                {
                    // Programme is relevant to us, i.e. in our window
                    if ((tvp.StopTime > startTimeWindow) && (tvp.StartTime < endTimeWindow))
                    {
                        // Found a show
                        if (!FoundAnyProgrammesForThisService) FoundAnyProgrammesForThisService = true;

                        // Already showing this cell
                        if (displayedEPGCells.ContainsKey(tvp.Id)) continue;

                        // Crop width?
                        double croppedDurationMins = -1;
                        if (tvp.StartTime < startTimeWindow) // starts beforehand
                        {
                            TimeSpan timeElapsed = TimeSpan.FromTicks(startTimeWindow - tvp.StartTime);
                            croppedDurationMins = (tvp.DurationMinutes() - timeElapsed.TotalMinutes);
                        }

                        if (tvp.StopTime > endTimeWindow) // finishes afterwards
                        {
                            TimeSpan timeToCrop = TimeSpan.FromTicks(tvp.StopTime - endTimeWindow);
                            croppedDurationMins = (tvp.DurationMinutes() - timeToCrop.TotalMinutes);
                        }

                        // X Position of cell

                        TimeSpan timeToday = tvp.StartTimeDT().ToLocalTime().Subtract(TheLocalDate.Date);
                        double cellXPos = timeToday.TotalMinutes * Functions.EPGZoomFactor;
                        //Functions.WriteLineToLogFile("Positioning show at " + tvp.StartTimeDT().ToShortTimeString() + "): timeToday:" + timeToday.ToString() + " at " + cellXPos.ToString());

                        EPGCell newCell = new EPGCell(tvp);
                        newCell.Clicked += new EventHandler<GenericEventArgs<TVProgramme>>(newCell_Clicked);
                        // Adjust duration?
                        if (croppedDurationMins > 0)
                            newCell.SetDurationMinutes(Convert.ToInt32(croppedDurationMins));
                        this.Children.Add(newCell);

                        newCell.SetValue(Canvas.TopProperty, (double)0);
                        newCell.SetValue(Canvas.LeftProperty, cellXPos);

                        displayedEPGCells.Add(tvp.Id, newCell);
                    }

                }
            }

            // Assume we're always filling the entire EPG  (i.e. remove this if ever changing to only a few hours horizontally)
            IsFilling = false;
            HasFilled = true;

            // RELEASE LOCK
            Monitor.Exit(ScheduleManager.TVProgrammeStore);

            // Raise event
            if (FillStrip_Complete != null) FillStrip_Complete(this, new EventArgs());
        }

        object myLock = new object();
        public void ClearStrip()
        {
            Monitor.Enter(myLock);
            if (IsFilling) // Cancel fill
            {
                ShouldCancelFill = true;
                IsFilling = false;
                HasFilled = false;
                return;
            }

            this.Children.Clear();
            displayedEPGCells.Clear();
            HasFilled = false;
            IsFilling = false; // safety
            Monitor.Exit(myLock);
        }

        object fillmsgCellLock = new object();
        public void FillWithMessageCells(string txtMessage)
        {
            FillWithMessageCells(txtMessage, 19);
        }
        void FillWithMessageCells(string txtMessage, int numberOfCells)
        {
            Monitor.Enter(fillmsgCellLock);

            if (ContainsMessageCells)
            {
                Monitor.Exit(fillmsgCellLock);
                return;
            }

            // Clear
            this.Children.Clear();

            for (int i = 0; i < numberOfCells; i++)
            {
                TextBlock tb = newTextBlockMessageCell(txtMessage);
                this.Children.Add(tb);
                try
                {
                    tb.SetValue(Canvas.LeftProperty, (double)(i * tb.Width));
                }
                catch 
                {
                    tb.SetValue(Canvas.LeftProperty, (double)(i * 400));
                    // in case tb doesn't have a width yet
                }
            }

            ContainsMessageCells = true;

            Monitor.Exit(fillmsgCellLock);
        }
        TextBlock newTextBlockMessageCell(string txtMessage)
        {
            TextBlock tb = new TextBlock();
            tb.FontSize = 22.0;
            tb.FontFamily = new FontFamily("Arial");
            tb.FontWeight = FontWeights.Bold;

            // Height needs to be 62  (47 + margin of 15 (
            tb.Height = 47.0;
            tb.Margin = new Thickness(160, 15, 160, 0);
            tb.HorizontalAlignment = HorizontalAlignment.Stretch;
            tb.VerticalAlignment = VerticalAlignment.Center;
            tb.TextAlignment = TextAlignment.Center;
            tb.Foreground = new SolidColorBrush(Colors.Gray);
            tb.Text = txtMessage;
            tb.Width = 400;

            return tb;
        }
        #endregion

        // Helper
        EPGRequest TheEPGRequest
        {
            get
            {
                EPGRequest request = new EPGRequest();

                // Use the midnight component of 'TheLocalDate', and span from there to a day's time
                int EPGhoursLong = 24 + SettingsImporter.SettingAsIntOrZero("SilverlightEPGOverspillHours");

                request.StartTime = TheLocalDate.Date.ToUniversalTime().Ticks;
                request.StopTime = TheLocalDate.Date.AddHours(EPGhoursLong).ToUniversalTime().Ticks;
                request.TVServiceID = TVServiceID;

                return request;
            }
        }

        #region Cell Clicks
        // Cells clicked - pass up to parent...
        public event EventHandler<GenericEventArgs<TVProgramme>> CellClicked;
        void newCell_Clicked(object sender, GenericEventArgs<TVProgramme> e)
        {
            if (CellClicked != null) CellClicked(sender, e);
        }
        #endregion


        #region Background Colour
        public Color DefaultBackgroundColour
        {
            get
            {
                return _defaultBackgroundColour;
            }
            set
            {
                _defaultBackgroundColour = value;

                this.Background = new SolidColorBrush(_defaultBackgroundColour);
            }
        }
        #endregion

    }
}
