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
    public partial class RecordRequestSettingsPane : UserControl
    {
        bool IsInitialised = false;
        public RecordingRequest LinkedRecordingRequest;
        string LinkedShowTitle;


        public event EventHandler<GenericEventArgs<bool>> Dismissed;

        public RecordRequestSettingsPane(RecordingRequest _linkedRequest, string _showTitle)
        {
            LinkedRecordingRequest = _linkedRequest;
            LinkedShowTitle = _showTitle;

            InitializeComponent();

            LayoutFromRequest();
            IsInitialised = true;
        }


        #region Data to GUI
        void LayoutFromRequest()
        {
            // Show Series Settings?
            Functions.ShowHideElement(gdSeriesSettings, (LinkedRecordingRequest.RequestType == RecordingRequestType.Series) );

            // Labels
            if (LinkedRecordingRequest.RequestType == RecordingRequestType.Series)
                lblPageTitle.Text = "Series Settings";
            else if (LinkedRecordingRequest.RequestType == RecordingRequestType.OneTime)
                lblPageTitle.Text = "Recording Settings";
            else if (LinkedRecordingRequest.RequestType == RecordingRequestType.Manual)
                lblPageTitle.Text = "Manual Recording";
            else 
                lblPageTitle.Text = "Unknown Recording";

            lblShowTitle.Text = LinkedShowTitle;

            // Series only
            if (LinkedRecordingRequest.RequestType == RecordingRequestType.Series)
            {
                // First Run
                cmbFirstRunOnly.SelectedIndex = LinkedRecordingRequest.FirstRunOnly ? 0 : 1;
                // Sub request
                switch (LinkedRecordingRequest.SeriesRequestSubType)
                {
                    case SeriesRequestSubTypes.ThisChannelThisTime:
                        cmbSeriesRequestSubType.SelectedIndex = 0;
                        break;

                    case SeriesRequestSubTypes.ThisChannelAnyTime:
                        cmbSeriesRequestSubType.SelectedIndex = 1;
                        break;

                    case SeriesRequestSubTypes.AnyChannelAnyTime:
                        cmbSeriesRequestSubType.SelectedIndex = 2;
                        break;
                }

            }

            // Keep Until
            switch (LinkedRecordingRequest.KeepUntil)
            {
                case KeepUntilTypes.NotSet:
                    cmbKeepUntil.SelectedIndex = 0; // "Default"
                    break;

                case KeepUntilTypes.UntilUserWatched:
                    cmbKeepUntil.SelectedIndex = 1;
                    break;

                case KeepUntilTypes.UntilUserDeletes:
                    cmbKeepUntil.SelectedIndex = 2;
                    break;

                case KeepUntilTypes.LatestEpisodes:
                    cmbKeepUntil.SelectedIndex = 3;
                    break;
            }
            ShowKeepEpisodesRowIfAppropriate();
            

            // Padding
            nudPostPadding.Value = LinkedRecordingRequest.Postpadding / 60;
            nudPrePadding.Value = LinkedRecordingRequest.Prepadding / 60;
        }
        void LayoutKeepEpisodes()
        {
            nudKeepNumberOfEpisodes.Value = LinkedRecordingRequest.KeepNumberOfEpisodes;
        }
        #endregion

        #region GUI to Request
        void UpdateRequestFromGUI()
        {
            // First Run
            LinkedRecordingRequest.FirstRunOnly = (cmbFirstRunOnly.SelectedIndex == 0);

            // Keep Until
            switch (cmbKeepUntil.SelectedIndex)
            {
                case 0:
                    LinkedRecordingRequest.KeepUntil = KeepUntilTypes.NotSet;
                    break;

                case 1:
                    LinkedRecordingRequest.KeepUntil = KeepUntilTypes.UntilUserWatched;
                    break;

                case 2:
                    LinkedRecordingRequest.KeepUntil = KeepUntilTypes.UntilUserDeletes;
                    break;

                case 3:
                    LinkedRecordingRequest.KeepUntil = KeepUntilTypes.LatestEpisodes;

                    // Latest eps
                    LinkedRecordingRequest.KeepNumberOfEpisodes = Convert.ToInt32( nudKeepNumberOfEpisodes.Value );
                    break;
            }
            


            // Padding
            LinkedRecordingRequest.Postpadding = Convert.ToInt32(nudPostPadding.Value * 60) ;
            LinkedRecordingRequest.Prepadding = Convert.ToInt32(nudPrePadding.Value * 60);

            // Sub request
            switch (cmbSeriesRequestSubType.SelectedIndex)
            {
                case 0:
                    LinkedRecordingRequest.SeriesRequestSubType = SeriesRequestSubTypes.ThisChannelThisTime;
                    break;

                case 1:
                    LinkedRecordingRequest.SeriesRequestSubType = SeriesRequestSubTypes.ThisChannelAnyTime;
                    break;

                case 2:
                    LinkedRecordingRequest.SeriesRequestSubType = SeriesRequestSubTypes.AnyChannelAnyTime;
                    break;

            }
        }
        #endregion


        #region Dynamic GUI Changes
        // Keep number of episodes row visibility
        private void cmbKeepUntil_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsInitialised) return;

            ShowKeepEpisodesRowIfAppropriate();
        }
        void ShowKeepEpisodesRowIfAppropriate()
        {
            if (cmbKeepUntil.SelectedIndex == 3)
            {
                rdKeepNumberOfEpisodes.Height = new GridLength(25);
            }
            else
                rdKeepNumberOfEpisodes.Height = new GridLength(0);
            
            LayoutKeepEpisodes();
        }
        #endregion

        #region Action Buttons
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (Dismissed != null) Dismissed(this, new GenericEventArgs<bool>(false));
        }
        private void btnAccept_Click(object sender, RoutedEventArgs e)
        {
            UpdateRequestFromGUI();

            if (Dismissed != null) Dismissed(this, new GenericEventArgs<bool>(true));
        }
        #endregion


    }
}
