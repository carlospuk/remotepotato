using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using CommonEPG;

namespace SilverPotato
{
    public partial class ShowInfoPane : UserControl, IDisposable
    {
        TVProgramme LinkedTVProgramme;
        ClickItemsPane contentPane;
        int currentPage = 0;

        // Constructors
        public ShowInfoPane()
        {
            InitializeComponent();

            imgThumbnail.ImageFailed += new EventHandler<ExceptionRoutedEventArgs>(imgThumbnail_ImageFailed);
            imgChannelLogo.ImageFailed += new EventHandler<ExceptionRoutedEventArgs>(imgChannelLogo_ImageFailed);

            LinkedTVProgramme = null;
            txtRecordingResult.Text = "";
            // Thumbnail
            AddDefaultThumbnail();
        }
     
        public ShowInfoPane(TVProgramme tvp) : this()
        {
            LinkedTVProgramme = tvp;
            LinkedTVProgramme.Updated += new EventHandler(LinkedTVProgramme_Updated); 

            PopulatePane();
            GetExtraInfo();  // Contact server to get additional info  (could do this when clicking labels)
        }
        public void Dispose()
        {
            if (LinkedTVProgramme != null)
            {
                LinkedTVProgramme.Updated -= LinkedTVProgramme_Updated;
            }

            // Remove reference
            LinkedTVProgramme = null;
        }



        // Populate Pane with Data

        void LinkedTVProgramme_Updated(object sender, EventArgs e)
        {
            // Update method can be called by other threads
            this.Dispatcher.BeginInvoke(PopulatePane);
        }
        private void PopulatePane()
        {
            PopulateMainShowDetails();


            // TYPE OF SHOW
            if (LinkedTVProgramme.isGeneratedFromFile)
            {
                PopulateFieldsForProgrammeFromFile();
            }
            else
            {
                RPRecording rec = LinkedTVProgramme.Recording();
                if (
                    (rec != null) &&
                        ((rec.State == RPRecordingStates.Recording) || (rec.State == RPRecordingStates.Scheduled) || (rec.State == RPRecordingStates.Recorded))
                    )
                    PopulateFieldsForProgrammeWithRecording(rec);
                else
                    PopulateFieldsForProgrammeWithoutRecording();

                ShowSeriesInfoOrRecordButton();
            }
        }
        public void PopulateMainShowDetails()
        {
            TVService tvs = LinkedTVProgramme.TVService();
            if (tvs == null)  // Channel not found - use a dummy to avoid extensive null testing below
            {
                tvs = new TVService();
                tvs.Callsign = String.IsNullOrEmpty(LinkedTVProgramme.WTVCallsign) ? "Unknown" : LinkedTVProgramme.WTVCallsign;
                tvs.UniqueId = "0";
            }

            // Override callsign if generated from file
            if (LinkedTVProgramme.isGeneratedFromFile) tvs.Callsign = LinkedTVProgramme.WTVCallsign;

            lblTitle.Text = LinkedTVProgramme.Title;
            if (string.IsNullOrEmpty(LinkedTVProgramme.EpisodeTitle))
                lblEpisodeTitle.Visibility = Visibility.Collapsed;
            else
            {
                lblEpisodeTitle.Visibility = Visibility.Visible;
                lblEpisodeTitle.Text = "\"" + LinkedTVProgramme.EpisodeTitle + "\"";
            }
            lblAiring.Text = LinkedTVProgramme.ToPrettyDayNameAndDate() + " " + LinkedTVProgramme.ToPrettyStartStopLocalTimes() + ", " + tvs.Callsign;
            lblDescription.Text = LinkedTVProgramme.Description;

            DisplayStarRatingOrNothing();

            // Additional Info
            StringBuilder sbAdditionalInfo = new StringBuilder();
            if (LinkedTVProgramme.IsHD) sbAdditionalInfo.Append("HD | ");
            if (LinkedTVProgramme.IsSeries) sbAdditionalInfo.Append("Series | ");
            if (! LinkedTVProgramme.IsFirstShowing) sbAdditionalInfo.Append("Repeat | ");
            if (LinkedTVProgramme.HasSubtitles) sbAdditionalInfo.Append("Subtitles | ");
            if (LinkedTVProgramme.ProgramType != TVProgrammeType.None) sbAdditionalInfo.Append(LinkedTVProgramme.ProgramType.ToString() + " | ");
            if (sbAdditionalInfo.Length > 0) if (sbAdditionalInfo.ToString().EndsWith("| ")) sbAdditionalInfo.Remove(sbAdditionalInfo.Length - 2, 2);
            lblAdditionalInfo.Text = sbAdditionalInfo.ToString();

            // Airdate
            if (LinkedTVProgramme.OriginalAirDate > 0)
            {
                DateTime OriginalDate = new DateTime(LinkedTVProgramme.OriginalAirDate, DateTimeKind.Utc);
                string origText = "Original Air Date: " + OriginalDate.ToPrettyDate();
                if (OriginalDate.Year != DateTime.Now.Year)
                    origText += " " + OriginalDate.Year.ToString();

                lblOriginalAirDate.Text = origText;
                lblOriginalAirDate.Visibility = Visibility.Visible;
            }
            else
                lblOriginalAirDate.Visibility = Visibility.Collapsed;

            // Logo 
            if (tvs.UniqueId != "0")
                imgChannelLogo.Source = new BitmapImage(tvs.LogoUriRemote());
        }
        void DisplayStarRatingOrNothing()
        {
            spStarRating.Children.Clear();
            int starCounter = LinkedTVProgramme.StarRating;
            if (LinkedTVProgramme.StarRating > 0)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (starCounter > 1) // full star
                        spStarRating.Children.Add(starImage("/Images/starOn.png"));
                    else if (starCounter == 1)  // half star
                        spStarRating.Children.Add(starImage("/Images/starHalf.png"));
                    else
                        spStarRating.Children.Add(starImage("/Images/starOff.png"));

                    starCounter = starCounter - 2;
                }
            }
        }
        Image starImage(string imagePath)
        {
            Image img = new Image();
            img.Source = ImageManager.LoadImageFromContentPath(imagePath);
            return img;
        }
        void PopulateFieldsForProgrammeWithoutRecording()
        {
            lblRecordingState.Text = "This show " + 
                ((LinkedTVProgramme.HasEndedYet()) ? "did not" : "is not set to") +
                    " record.";

            // No record dot 
            imgRecordDot.Source = null;

            // Can't do various things when there isn't a recording
            Functions.ShowHideElement(lbCancelShow, false);
            Functions.ShowHideElement(lbDeleteShow, false);
            Functions.ShowHideElement(lbPlayShow, false);  
        }
        void PopulateFieldsForProgrammeFromFile()
        {
            lblRecordingState.Text = "This show " +
                ((LinkedTVProgramme.HasEndedYet()) ? "has been recorded." : "is recording.");

            // No record dot 
            imgRecordDot.Source = ImageManager.bmpRecordDotOneTime;

            SetThumbnailTo(LinkedTVProgramme.ThumbnailUriOrNull());

            // Can't do various things when there isn't a recording
            Functions.ShowHideElement(lbCancelShow, false);
            Functions.ShowHideElement(lbDeleteShow, (!String.IsNullOrEmpty(LinkedTVProgramme.Filename)));  
            Functions.ShowHideElement(lbPlayShow, (! String.IsNullOrEmpty(LinkedTVProgramme.Filename)));  
        }
        void SetThumbnailTo(Uri thumbUri)
        {
            if (thumbUri == null) return;

            imgThumbnail.Source = new BitmapImage(thumbUri);
        }
        void PopulateFieldsForProgrammeWithRecording(RPRecording rec)
        {
            lblRecordingInfo.Text = "";
            imgRecordDot.Source = null;

            // Recording info labels
            lblRecordingState.Text = rec.State.ToPrettyString();
            if (rec.Partial) lblRecordingState.Text = lblRecordingState.Text + " (partial)";

            // Try to load thumbnail
            if (rec.ThumbnailUriOrNull() != null)
                imgThumbnail.Source = new BitmapImage(rec.ThumbnailUriOrNull());

            // It's recording or did record
            Functions.ShowHideElement(lbCancelShow, !LinkedTVProgramme.HasEndedYet());  // Allow to cancel unless it's finished
            Functions.ShowHideElement(lbDeleteShow, ( LinkedTVProgramme.HasEndedYet() && (LinkedTVProgramme.Filename != null)  ));  // Allow to delete if it's finished
            Functions.ShowHideElement(lbPlayShow, 
                ((LinkedTVProgramme.Filename != null) &&
                (! LinkedTVProgramme.IsDRMProtected) )
                );  // Allow to play if it's finished AND it's not DRM protected

            lblRecordingInfo.Text = "This is a " + rec.RequestType.ToString() + " recording.";
            imgRecordDot.Source = rec.IsRecurring() ? ImageManager.bmpRecordDotSeries : ImageManager.bmpRecordDotOneTime;            
        }
        private void ShowSeriesInfoOrRecordButton()
        {
            bool ShowIsRecording = LinkedTVProgramme.HasActiveRecording();

            // 1.  ONE TIME (SHOW) RECORDING
            // Are we recording this showing?
            if (ShowIsRecording)
                Functions.ShowHideElement(lbRecordShow, false);
            else
                // Allow us to record this showing (if it hasn't finished yet)
                Functions.ShowHideElement(lbRecordShow, !LinkedTVProgramme.HasEndedYet());


            // 2.  SERIES RECORDING
            // Don't show series record if it's already being recorded but it just happens this showing isn't
            if (LinkedTVProgramme.HasSeriesRequest())  // Is there already a series request for this show
            {
                Functions.ShowHideElement(lbRecordSeries, false);
                Functions.ShowHideElement(lbSeriesInfo, true);
            }
            else  // Show is not part of a series request
            {
                Functions.ShowHideElement(lbSeriesInfo, false);

                // Is the show an actual series, i.e. can we record series
                Functions.ShowHideElement(lbRecordSeries, LinkedTVProgramme.IsSeries);
            }            
        }


        // Images
        void imgThumbnail_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            imgThumbnail.Source = ImageManager.bmpThumbnailDefault;
        }
        void imgChannelLogo_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            imgChannelLogo.Source = null;
        }

        // Add components / wire up events
        private void AddDefaultThumbnail()
        {
            imgThumbnail.Source = ImageManager.bmpThumbnailDefault;
        }

        // BUTTONS pushed
        void lbRecordShow_Click(object sender, EventArgs e)
        {
            OpenModalRecordOptionsWindow(RecordingRequestType.OneTime);
        }
        void lbRecordSeries_Click(object sender, EventArgs e)
        {
            OpenModalRecordOptionsWindow(RecordingRequestType.Series);
        }
        void lbCancelShow_Click(object sender, EventArgs e)
        {
            VisualManager.questionBox.Closed += new EventHandler(lbCancelShow_Click_2);
            VisualManager.ShowQuestionBox("Cancel Show", "This will stop this showing of " + LinkedTVProgramme.Title + " from being recorded.  Are you sure you wish to continue?");
        }
        void lbCancelShow_Click_2(object sender, EventArgs e)
        {
            VisualManager.questionBox.Closed -= new EventHandler(lbCancelShow_Click_2);
            if (VisualManager.QuestionBoxDialogResult)
                DoCancelShow();
        }

        
        void lbGoBack_Click(object sender, EventArgs e)
        {
            CloseMe();
        }
        private void lbSeriesInfo_Click(object sender, EventArgs e)
        {
            ShowSeriesInfo();
        }
        private void lbDeleteShow_Click(object sender, EventArgs e)
        {
            VisualManager.questionBox.Closed += new EventHandler(lbDeleteShow_Click_2);
            VisualManager.ShowQuestionBox("Delete Show", "This will permanently delete this showing from disk.  Are you sure you wish to continue?");
        }
        void lbDeleteShow_Click_2(object sender, EventArgs e)
        {
            VisualManager.questionBox.Closed -= new EventHandler(lbDeleteShow_Click_2);
            if (VisualManager.QuestionBoxDialogResult)
                DoDeleteRecording();
        }

        void CloseMe()
        {
            VisualManager.PopOffScreenStackCurrentWindow();
        }
        private void lbPlayShow_Click(object sender, EventArgs e)
        {
            DoStreamShow();
        }


        // Streaming
        private void DoStreamShow()
        {
            if (String.IsNullOrEmpty(LinkedTVProgramme.Filename)) return;
            VisualManager.ShowStreamingVideo(LinkedTVProgramme, TimeSpan.FromSeconds(0));  // To do: resume button to start from....
        }

        void DoDeleteRecording()
        {
            if (String.IsNullOrEmpty(LinkedTVProgramme.Filename))
            {
                ErrorManager.DisplayError("There is no filename associated with this recording, so it cannot be deleted.");
                return;
            }

            VisualManager.ShowActivityModal();
            lbDeleteShow.IsEnabled = false;
            RecordingManager.DeleteFile_Completed += new EventHandler<GenericEventArgs<string>>(RecordingManager_DeleteFile_Completed);
            RecordingManager.DeleteFileByFilePath(LinkedTVProgramme.Filename);                
        }
        void RecordingManager_DeleteFile_Completed(object sender, GenericEventArgs<string> e)
        {
            VisualManager.HideActivityModal();
            lbDeleteShow.IsEnabled = true;
            RecordingManager.DeleteFile_Completed -= RecordingManager_DeleteFile_Completed;

            if (e.Value != "OK")
            {
                ErrorManager.DisplayAndLogError("Could not delete this show: " + e.Value);
                return;
            }

            // Refresh recorded TV
            ScheduleManager.GetRecordedTV(true);

            // ME CLOSE
            CloseMe();
        }
        void OpenModalRecordOptionsWindow(RecordingRequestType requestType)
        {
            RecordingRequest rr = RecordingManager.RecordingRequestFromTVProgramme(LinkedTVProgramme, requestType);

            RecordRequestSettingsPane rrSettingsPane = new RecordRequestSettingsPane(rr, LinkedTVProgramme.Title);
            rrSettingsPane.Dismissed += new EventHandler<GenericEventArgs<bool>>(rrSettingsPane_Dismissed);
            VisualManager.PushOntoScreenStack((FrameworkElement)rrSettingsPane, true);  // Show MODAL
        }
        // Modal record settings window closed
        void rrSettingsPane_Dismissed(object sender, GenericEventArgs<bool> e)
        {
            VisualManager.PopOffScreenStackCurrentWindow(true);

            bool DidClickOK = (e.Value);
            if (! DidClickOK) return;

            // ACCEPT was clicked - so make the recording!
            RecordRequestSettingsPane rrSettingsPane = (RecordRequestSettingsPane)sender;
            RecordingRequest rr = rrSettingsPane.LinkedRecordingRequest;
            DoRecordShowWithRequest(rr);
        }
        void DoRecordShowWithRequest(RecordingRequest newRequest)
        {
            RecordingManager.CreateRecording_Completed +=new EventHandler<GenericEventArgs<RecordingResult>>(RecordingManager_CreateRecording_Completed);
            VisualManager.ShowActivityModal();

            RecordingManager.SubmitRecordingRequestToServer(newRequest);
        }
        void RecordingManager_CreateRecording_Completed(object sender, GenericEventArgs<RecordingResult> e)
        {
            VisualManager.HideActivityModal();
            RecordingManager.CreateRecording_Completed -= RecordingManager_CreateRecording_Completed;
            RecordingResult recResult = e.Value;


            if (!recResult.Completed)
            {
                imgRecordDot.Source = ImageManager.LoadImageFromContentPath("/Images/warningtriangle.png");
                lblRecordingInfo.Text = "Recording Request Failed.";
                txtRecordingResult.Text = "Could not complete recording request : " + RecordingResult.FriendlyFailureReason(recResult);
                return;
            }
            else
            {
                // COMPLETED
                if (recResult.Success)
                {
                    lblRecordingInfo.Foreground = new SolidColorBrush(Functions.HexColor("#22AA22"));
                    lblRecordingInfo.Text = "Recording Request Succeeded.";

                    if (recResult.WereConflicts) 
                        imgRecordDot.Source = ImageManager.LoadImageFromContentPath("/Images/infocircle.png");

                    // Store result  (will include any info on conflicts)
                    txtRecordingResult.Text = RecordingResult.FriendlySuccessReport(recResult);

                    // Re-import recordings
                    RPRecordingsBlob blob = e.Value.GeneratedRecordingsBlob;

                    // Was this particular show recorded?
                    bool WasThisShowRecorded = false;
                    foreach (RPRecording rec in blob.RPRecordings)
                    {
                        if (rec.TVProgrammeID == long.Parse(LinkedTVProgramme.Id))
                            WasThisShowRecorded = true;
                    }

                    // Merge all in (this probably clears the blob)
                    ScheduleManager.MergeInRecordingsFromList(blob.RPRecordings);
                    ScheduleManager.MergeInRequestsFromList(blob.RPRequests);

                    // Merge in the new tv programmes to the store
                    ScheduleManager.MergeIntoTVProgrammeStore(blob.TVProgrammes, true);

                    // If this show wasn't recorded, advise
                    if (!WasThisShowRecorded)
                        imgRecordDot.Source = ImageManager.LoadImageFromContentPath("/Images/infocircle.png");
                    // else it's recording, so it'll be given a red dot when the programme update event fires 

                }
                else   // UNSUCCESSFUL
                {
                    imgRecordDot.Source = ImageManager.LoadImageFromContentPath("/Images/warningtriangle.png");
                    lblRecordingInfo.Text = "Recording Request Failed.";
                    txtRecordingResult.Text = "This show could not be recorded: " + RecordingResult.FriendlyFailureReason(recResult);
                    return;
                }
            }

        }
        void DoCancelShow()
        {
            if (!LinkedTVProgramme.HasActiveRecording()) return;

            VisualManager.ShowActivityModal();
            lbCancelShow.IsEnabled = false;
            RecordingManager.CancelRecording_Completed += new EventHandler<GenericEventArgs<string>>(RecordingManager_CancelRecording_Completed);
            RecordingManager.CancelRecording(LinkedTVProgramme.Recording().Id.ToString()); 
        }
        void RecordingManager_CancelRecording_Completed(object sender, GenericEventArgs<string> e)
        {
            VisualManager.HideActivityModal();
            lbCancelShow.IsEnabled = true;
            RecordingManager.CancelRecording_Completed -= new EventHandler<GenericEventArgs<string>>(RecordingManager_CancelRecording_Completed);      
      
            if (e.Value != "OK")
            {
                ErrorManager.DisplayAndLogError("Could not cancel show: " + e.Value);
                return;
            }

            ScheduleManager.RemoveRecordingFromListById(LinkedTVProgramme.Recording().Id);

            // ME CLOSE
            CloseMe();
        }

        // Show series info
        private void ShowSeriesInfo()
        {
            // This show doesn't have to be recording for there to be a series request.
            LinkedTVProgramme.HasSeriesRequest();
            RPRequest req = LinkedTVProgramme.SeriesRequest();
            if (req != null)
            {
                // Check it's a series request
                if ((req.RequestType != RPRequestTypes.Series) && (req.RequestType != RPRequestTypes.Keyword)) return;

                // Show info
                RPRequestInfoPane srip = new RPRequestInfoPane(req);
                VisualManager.PushOntoScreenStack(srip);
            }

            /*  // THE OLD WAY, WHEN IT HAD TO BE A CURRENTLY RECORDING PROGRAMME TO TRACK BACK TO THE REQUEST VIA THE RECORDING
            if (!LinkedTVProgramme.HasActiveRecording()) return;

                RPRequest req = LinkedTVProgramme.RecordingRequest();
                if (req != null)
                {
                    // Check it's a series request
                    if ( (req.RequestType != RPRequestTypes.Series ) && 
                            (req.RequestType != RPRequestTypes.Keyword)) return;

                    // Show info
                    RPRequestInfoPane srip = new RPRequestInfoPane(req);
                    VisualManager.PushOntoScreen(srip);
                }
             */
        }
        private void btnCloseRecordingResultBox_Click(object sender, RoutedEventArgs e)
        {
            ShowHideRecordingInfoPane(false);
        }
        private void imgRecordDot_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (! String.IsNullOrEmpty( txtRecordingResult.Text))
                ShowHideRecordingInfoPane(true);
        }
        void ShowHideRecordingInfoPane(bool show)
        {
            gdRecordingInfo.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }


        #region Get Extra Info
        bool GettingInfoBlob = false;
        void GetExtraInfo()
        {
            if (GettingInfoBlob) return;
            // Not for generated files
            if (LinkedTVProgramme.isGeneratedFromFile) return;
            
            GettingInfoBlob = true;
            VisualManager.ShowActivityWithinGrid(gdContentCrew, 2.0);
            VisualManager.ShowActivityWithinGrid(gdContentShowings, 2.0);
            if (Settings.DebugAdvanced) Functions.WriteLineToLogFile("Fetching InfoBlob for programme with ID " + LinkedTVProgramme.Id);
            EPGImporter importer = new EPGImporter();
            importer.GetProgrammeInfoBlobCompleted += new EventHandler<GenericEventArgs<TVProgrammeInfoBlob>>(importer_GetProgrammeInfoBlobCompleted);
            importer.GetProgrammeInfoBlob(LinkedTVProgramme.Id);
        }

        void importer_GetProgrammeInfoBlobCompleted(object sender, GenericEventArgs<TVProgrammeInfoBlob> e)
        {
            if (e.Value == null)
            {
                lblCrew.Text = "No information is available.";
                lblDescription.Text = "No description is available.";
                // TODO:  no showings either
                return;  // no info
            }

            VisualManager.HideActivityWithinGrid(gdContentCrew);
            VisualManager.HideActivityWithinGrid(gdContentShowings);

            PopulateAdditionalInfo(e.Value);
            
        }
        void PopulateAdditionalInfo(TVProgrammeInfoBlob infoblob)
        {
            // Description
            if (LinkedTVProgramme.Description != infoblob.Description)
            {
                LinkedTVProgramme.Description = infoblob.Description;  // it may re-populate
                lblDescription.Text = infoblob.Description;
            }
            lblCrew.Text = formattedCrewString(infoblob.Crew);

            // Merge in info
            ScheduleManager.MergeIntoTVProgrammeStore(infoblob.OtherShowingsInSeries, true);
            ScheduleManager.MergeIntoTVProgrammeStore(infoblob.OtherShowingsOfThis, true);
            PopulateOtherShowings();
        }
        void PopulateOtherShowings()
        {
            gdContent.Children.Clear();
            // If this is a series, show all showings in series, if not just show the other showings of this (MC)Program
            Dictionary<string, List<TVProgramme>> GroupedProgs =
                (LinkedTVProgramme.IsSeries) ?
                ScheduleManager.SeriesShowingsOfProgrammeGroupedByEpisodeTitle(LinkedTVProgramme) :
                ScheduleManager.OtherShowingsOfProgrammeGroupedBy(LinkedTVProgramme, "channel");

            bool foundAtLeastOneItem = false;
            Dictionary<string, List<ClickItem>> GroupedItems = new Dictionary<string, List<ClickItem>>();
            foreach (KeyValuePair<string, List<TVProgramme>> kvp in GroupedProgs)
            {
                List<ClickItem> clickItems = new List<ClickItem>();
                foreach (TVProgramme tvp in kvp.Value)
                {
                    if (!foundAtLeastOneItem) foundAtLeastOneItem = true;

                    // Important line - use the PARENT BASE CLASS VARIABLE to store the derived class, so we can pass it to the ClickItemsPane
                    ClickItem ci = new TVProgClickItem(tvp, TVProgClickItem.TVProgClickItemTextFormat.DayDateAndTimeAndChannel, ClickItem.ClickItemLayouts.TextOnly);
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
            contentPane.HidePaneControls(); // no sorting etc at top
            contentPane.ItemClicked += new EventHandler(contentPane_ItemClicked);
            contentPane.AwaitingRefreshedContent += new EventHandler(contentPane_RefreshClicked);
            gdContent.Children.Add(contentPane);

          //  lblNoContent.Visibility = foundAtLeastOneItem ? Visibility.Collapsed : Visibility.Visible;
        }

        void contentPane_RefreshClicked(object sender, EventArgs e)
        {
            
        }


        // Event raised by the content pane - one of its items has been clicked
        void contentPane_ItemClicked(object sender, EventArgs e)
        {
            ClickItem ci = (ClickItem)sender; // Base class
            if (!(ci is TVProgClickItem)) return;
            TVProgClickItem tvpci = (TVProgClickItem)ci;  // Cast into derived class, we know it's a tvpci as it came from here
            ShowInfoPane sip = new ShowInfoPane(tvpci.LinkedTVProgramme);
            VisualManager.PushOntoScreenStack(sip);
        }

        
        string formattedCrewString(TVProgrammeCrew crew)
        {
            if (crew == null)
            {
                return "No credits are available for this show.";
            }

            StringBuilder sb = new StringBuilder(200);

            if (crew.Directors != null)
            {
                sb.AppendLine("Directors:");
                sb.AppendLine(formattedPeopleString(crew.Directors));
            }

            if (crew.Actors != null)
            {
                if (sb.Length > 0) sb.AppendLine();
                sb.AppendLine("Actors:");
                sb.AppendLine(formattedPeopleString(crew.Actors));
            }

            if (crew.Writers != null)
            {
                if (sb.Length > 0) sb.AppendLine();
                sb.AppendLine("Writers:");
                sb.AppendLine(formattedPeopleString(crew.Writers));
            }

            if (crew.Producers != null)
            {
                if (sb.Length > 0) sb.AppendLine();
                sb.AppendLine("Producers:");
                sb.AppendLine(formattedPeopleString(crew.Producers));
            }

            return sb.ToString();
        }
        string formattedPeopleString(string peopledelimited)
        {
            List<string> ThePeople = new List<string>();
            string[] people = peopledelimited.Split(new char[] { '/' });
            
            // Make unique list
            foreach (string person in people)
            {
                if (!ThePeople.Contains(person))
                    ThePeople.Add(person);
            }

            StringBuilder sbPeople = new StringBuilder(20);
            foreach (string person in ThePeople)
            {
                sbPeople.AppendLine(person);
            }

            return sbPeople.ToString();
        }
        #endregion


        #region TabPages
        private void pageLink_MouseEnter(object sender, MouseEventArgs e)
        {
            Border brd = (Border)sender;
            int destPage = Convert.ToInt32(brd.Tag);
            if (destPage == currentPage) return;  // Dont change colour, it's this page
            

            TextBlock tb = (TextBlock)brd.Child;
            tb.Foreground = new SolidColorBrush(Colors.White);
        }

        private void pageLink_MouseLeave(object sender, MouseEventArgs e)
        {
            Border brd = (Border)sender;
            int destPage = Convert.ToInt32(brd.Tag);
            if (destPage == currentPage) return;  // Dont change colour, it's this page

            TextBlock tb = (TextBlock)brd.Child;
            tb.Foreground = new SolidColorBrush(Functions.HexColor("#DDDDDD"));
        }
        private void pageLink_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Border brd = (Border)sender;
            int destPage = Convert.ToInt32(brd.Tag);
            if (destPage == currentPage) return;  // Dont change page, it's this page

            
            TextBlock tb = (TextBlock)brd.Child;
            tb.Cursor = Cursors.Arrow; // not clickable any more
            tb.Foreground = new SolidColorBrush(Functions.HexColor("#FFFFAA"));

            SwitchToPage(destPage);
        }
        Queue<int> fadingOutPageNumber = new Queue<int>();
        void SwitchToPage(int page)
        {
            // Fade out current page and de-highlight link
            fadingOutPageNumber.Enqueue(currentPage);
            switch (currentPage)
            {
                case 0:
                    txtGoPageInfo.Foreground = new SolidColorBrush(Functions.HexColor("#DDDDDD"));
                    txtGoPageInfo.Cursor = Cursors.Hand;
                    Animations.DoFadeOut(0.3, gdContentInfo, fadeOutCurrentPage_Completed);
                    break;

                case 1:
                    txtGoPageShowings.Foreground = new SolidColorBrush(Functions.HexColor("#DDDDDD"));
                    txtGoPageShowings.Cursor = Cursors.Hand;
                    Animations.DoFadeOut(0.3, gdContentShowings, fadeOutCurrentPage_Completed);
                    break;

                case 2:
                    txtGoPageCast.Foreground = new SolidColorBrush(Functions.HexColor("#DDDDDD"));
                    txtGoPageCast.Cursor = Cursors.Hand;
                    Animations.DoFadeOut(0.3, gdContentCrew, fadeOutCurrentPage_Completed);
                    break;
            }
           

            // fade in new page 
            currentPage = page;
            switch (page)
            {
                case 0:
                    gdContentInfo.Opacity = 0.0;
                    gdContentInfo.Visibility = Visibility.Visible;
                    Animations.DoFadeIn(0.3, gdContentInfo);
                    break;

                case 1:
                    gdContentShowings.Opacity = 0.0;
                    gdContentShowings.Visibility = Visibility.Visible;
                    Animations.DoFadeIn(0.3, gdContentShowings);
                    break;

                case 2:
                    gdContentCrew.Opacity = 0.0;
                    gdContentCrew.Visibility = Visibility.Visible;
                    Animations.DoFadeIn(0.3, gdContentCrew);
                    break;
            }
            
        }
        void fadeOutCurrentPage_Completed(object sender, EventArgs e)
        {
            int oldPage = fadingOutPageNumber.Dequeue();
            switch (oldPage)
            {
                case 0:
                    gdContentInfo.Visibility = Visibility.Collapsed;
                    break;

                case 1:
                    gdContentShowings.Visibility = Visibility.Collapsed;
                    break;

                case 2:
                    gdContentCrew.Visibility = Visibility.Collapsed;
                    break;
            }
        }
        #endregion
    

    }

    public enum ShowInfoPaneSources
    {
        TVProgramme,
        TVRecordingEvent
    }
}

