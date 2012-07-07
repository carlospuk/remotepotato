using System;
using System.Net;
using System.Xml;
using System.Xml.Serialization;
using System.Windows;
using System.Text;
using System.IO;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.Generic;
using CommonEPG;

namespace SilverPotato
{
    public static class RecordingManager
    {

        // Create Recordings
        public static event EventHandler<GenericEventArgs<RecordingResult>> CreateRecording_Completed;
        public static void SubmitRecordingRequestToServer(TVProgramme tvp, RecordingRequestType requestType)
        {
            RecordingRequest newRequest = RecordingRequestFromTVProgramme(tvp, requestType);

            SubmitRecordingRequestToServer(newRequest);
        }
        public static RecordingRequest RecordingRequestFromTVProgramme(TVProgramme tvp, RecordingRequestType requestType)
        {
            RecordingRequest newRequest = null;
            switch (requestType)
            {
                case RecordingRequestType.OneTime:
                    newRequest = new RecordingRequest(long.Parse(tvp.Id));
                    break;

                case RecordingRequestType.Series:
                    newRequest = new RecordingRequest(long.Parse(tvp.Id), SeriesRequestSubTypes.ThisChannelAnyTime);
                    break;

                default:
                    Functions.WriteLineToLogFile("Unknown recording request type - cannot make recording request.");
                    break;
            }

            // Set Default Values
            newRequest.KeepUntil = KeepUntilTypes.NotSet;
            newRequest.KeepNumberOfEpisodes = 0;  // UNSET
            newRequest.FirstRunOnly = false;
            newRequest.Quality = -1; // UNSET

            // Padding
            newRequest.Postpadding = SettingsImporter.SettingAsIntOrZero("DefaultPostPadding");
            newRequest.Prepadding = SettingsImporter.SettingAsIntOrZero("DefaultPrePadding");

            return newRequest;
        }
        public static void SubmitRecordingRequestToServer(RecordingRequest rr)
        {
            RPWebClient client = new RPWebClient();
            client.GetStringByPostingCompleted += new EventHandler<UploadStringCompletedEventArgs>(client_UploadRPStringCompleted);
            client.GetStringByPostingObject("/xml/record/byrecordingrequest/", rr);
        }
        static void client_UploadRPStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                Functions.WriteLineToLogFile("RecordingManager: Failed to upload recording request to server:");
                Functions.WriteExceptionToLogFile(e.Error);

                // Create a dummy object to return in the result, with a FALSE status and the error message.
                RecordingResult newRR = new RecordingResult();
                newRR.Completed = false;
                newRR.ErrorMessage = e.Error.Message;
                if (CreateRecording_Completed != null) CreateRecording_Completed(new object(), new GenericEventArgs<RecordingResult>(newRR));
                return;
            }

            RecordingResult rr = RecordingResult.FromXML(e.Result);
            if (CreateRecording_Completed != null) CreateRecording_Completed(new object(), new GenericEventArgs<RecordingResult>(rr));
        }

        // Cancel Request  (series)
        public static event EventHandler<GenericEventArgs<string>> CancelRequest_Completed;
        public static void CancelRequest(string requestID)
        {
            RPWebClient client = new RPWebClient();
            client.GetStringByGettingCompleted += new EventHandler<UploadStringCompletedEventArgs>(CancelRequest_DownloadRPStringCompleted);
            client.GetStringByGetting("/xml/cancelrequest/" + requestID);
        }
        static void CancelRequest_DownloadRPStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                if (CancelRequest_Completed != null) CancelRequest_Completed(new object(), new GenericEventArgs<string>(e.Error.Message));
                return;
            }

            if (CancelRequest_Completed != null) CancelRequest_Completed(new object(), new GenericEventArgs<string>(e.Result));
        }

        // Cancel Recording  (one show)
        public static event EventHandler<GenericEventArgs<string>> CancelRecording_Completed;
        public static void CancelRecording(string recordingID)
        {
            RPWebClient client = new RPWebClient();
            client.GetStringByGettingCompleted += new EventHandler<UploadStringCompletedEventArgs>(CancelRecording_DownloadRPStringCompleted);
            client.GetStringByGetting("/xml/cancelrecording/" + recordingID);
        }
        static void CancelRecording_DownloadRPStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                if (CancelRecording_Completed != null) CancelRecording_Completed(new object(), new GenericEventArgs<string>(e.Error.Message));
                return;
            }

            if (CancelRecording_Completed != null) CancelRecording_Completed(new object(), new GenericEventArgs<string>(e.Result));
        }

        // Delete Shows
        public static event EventHandler<GenericEventArgs<string>> DeleteFile_Completed;
        public static void DeleteFileByFilePath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                if (DeleteFile_Completed != null) DeleteFile_Completed(new object(), new GenericEventArgs<string>("There is no recording request associated with this object."));
            }

            RPWebClient client = new RPWebClient();
            client.GetStringByPostingCompleted += new EventHandler<UploadStringCompletedEventArgs>(DeleteFileByFilePath_GetStringByPostingCompleted);
            client.GetStringByPostingString("/xml/deletefile64/" , Uri.EscapeUriString( Functions.EncodeToBase64( filePath) ));
        }
        static void DeleteFileByFilePath_GetStringByPostingCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                if (DeleteFile_Completed != null) DeleteFile_Completed(new object(), new GenericEventArgs<string>(e.Error.Message));
                return;
            }

            if (DeleteFile_Completed != null) DeleteFile_Completed(new object(), new GenericEventArgs<string>(e.Result));
        }

        // Recorded TV
        public static event EventHandler<GenericEventArgs<List<TVProgramme>>> GetRecordedTVCompleted;
        public static void GetRecordedTV(bool shouldRefresh)
        {
            RPWebClient client = new RPWebClient();
            client.GetStringByGettingCompleted += new EventHandler<UploadStringCompletedEventArgs>(GetRecordedTV_DownloadRPStringCompleted);

            string refreshString = shouldRefresh ? "/refreshnow" : "";
            client.GetStringByGetting("/xml/recordedTV" + refreshString + Settings.ZipDataStreamsAddendum);
        }
        static void GetRecordedTV_DownloadRPStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                ErrorManager.DisplayAndLogError("Couldn't contact server to get recorded TV list.");
                ErrorManager.DisplayAndLogError(e.Error);
                GetRecordedTVCompleted(new object(), new GenericEventArgs<List<TVProgramme>>(new List<TVProgramme>()));
                return;
            }
            string strOut = e.Result;
            if (Settings.ZipDataStreams)
            {
                if (!ZipManager.UnzipString(ref strOut))
                {
                    ErrorManager.DisplayAndLogError("Could not unzip downloaded TV Programmes from server.");
                    GetRecordedTVCompleted(new object(), new GenericEventArgs<List<TVProgramme>>(new List<TVProgramme>()));
                    return;
                }
            }
            List<TVProgramme> theProgs = new List<TVProgramme>();
            try
            {
                XmlSerializer serializer = new XmlSerializer(theProgs.GetType());
                StringReader sr = new StringReader(strOut);
                theProgs = (List<TVProgramme>)serializer.Deserialize(sr);
            }
            catch { }  // No need to report this, probably just no showings

            GetRecordedTVCompleted(new object(), new GenericEventArgs<List<TVProgramme>>(theProgs));
        }
        
    }
}

