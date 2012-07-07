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
    public partial class RPRequestInfoPane : UserControl
    {
        RPRequest LinkedSeriesRequest;
        List<TVProgramme> AllShowings;

        // Constructors
        public RPRequestInfoPane()
        {
            InitializeComponent();

            ScheduleManager.Recordings_Changed += new EventHandler(ScheduleManager_Recordings_Changed);
            AllShowings = new List<TVProgramme>();
        }
        public RPRequestInfoPane(RPRequest rq) : this()
        {
            PopulatePaneFromRPRequest(rq);
        }


        // Populate Pane with Data
        public void PopulatePaneFromRPRequest(RPRequest rq)
        {
            LinkedSeriesRequest = rq;
            lblTitle.Text = rq.Title + " - Series Information";

            ShowCancelSeriesButton();  // always, why not
            populateAllShowingsList();
        }
   

        // Other showings Callback
        void ScheduleManager_Recordings_Changed(object sender, EventArgs e)
        {
            populateAllShowingsList();
        }
        void populateAllShowingsList()
        {
            spPastShowings.Children.Clear();
            spFutureShowings.Children.Clear();

            List<TVProgramme> allProgs = LinkedSeriesRequest.TVProgrammes();
            if ((allProgs == null) || (allProgs.Count < 1))
            {
                Functions.ShowHideElement(txtNoShowings, true);
                return;
            }

            Functions.ShowHideElement(txtNoShowings, false);

            // Sort by start date
            allProgs.Sort(new CommonEPG.Comparers.TVProgrammeStartTimeComparer());

            foreach (TVProgramme tvp in allProgs)
            {
                TVProgClickItem ci = new TVProgClickItem(tvp, TVProgClickItem.TVProgClickItemTextFormat.DateAndTimeAndChannel, ClickItem.ClickItemLayouts.TextOnly);
                ci.Clicked += new EventHandler(Showing_Clicked);

                // TODO
                //if (re.IsInPast())
                //    spPastShowings.Children.Add(ci);
                //else
                    spFutureShowings.Children.Add(ci);
            }

            Functions.ShowHideElement(spPastShowingsContainer, (spPastShowings.Children.Count > 0));
            Functions.ShowHideElement(spFutureShowingsContainer, (spFutureShowings.Children.Count > 0));
        }
        void Showing_Clicked(object sender, EventArgs e)
        {
            TVProgClickItem ce = (TVProgClickItem)sender;

            if (ce.LinkedTVProgramme != null)
            {
                ShowInfoPane sip = new ShowInfoPane(ce.LinkedTVProgramme);
                VisualManager.PushOntoScreenStack(sip);
            }
        }

        // Add components / wire up events
        private void ShowCancelSeriesButton()
        {
            Functions.ShowHideElement(lbCancelSeries, true);
        }


        // BUTTONS pushed
        void lbCancelSeries_Click(object sender, EventArgs e)
        {
            VisualManager.questionBox.Closed += new EventHandler(lbCancelSeries_Click_2);
            VisualManager.ShowQuestionBox("Cancel Series", "This will cancel the whole series.  Are you sure you wish to continue?");
        }
        void lbCancelSeries_Click_2(object sender, EventArgs e)
        {
            VisualManager.questionBox.Closed -= new EventHandler(lbCancelSeries_Click_2);
            if (VisualManager.QuestionBoxDialogResult)
                DoCancelSeries();
        }

        void lbGoBack_Click(object sender, EventArgs e)
        {
            CloseMe();
        }
        void CloseMe()
        {
            VisualManager.PopOffScreenStackCurrentWindow();
        }


        // Recordings
        void DoCancelSeries()
        {
            if (LinkedSeriesRequest == null) return;

            VisualManager.ShowActivityModal();
            lbCancelSeries.IsEnabled = false;
            RecordingManager.CancelRequest_Completed += new EventHandler<GenericEventArgs<string>>(RecordingManager_CancelSeries_Completed);
            RecordingManager.CancelRequest(LinkedSeriesRequest.ID.ToString());
        }
        void RecordingManager_CancelSeries_Completed(object sender, GenericEventArgs<string> e)
        {
            VisualManager.HideActivityModal();
            lbCancelSeries.IsEnabled = true;
            RecordingManager.CancelRequest_Completed -= RecordingManager_CancelSeries_Completed;

            if (e.Value != "OK")
            {
                ErrorManager.DisplayAndLogError("Could not cancel recording: " + e.Value);
                return;
            }

            ScheduleManager.RemoveRequestFromListById(LinkedSeriesRequest.ID);
            CloseMe();
        }




    }

}
