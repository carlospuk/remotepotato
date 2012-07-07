using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Text;
using CommonEPG;

namespace SilverPotato
{
    public class EPGImporter
    {
        public event EventHandler<GenericEventArgs<List<TVService>>> GetChannelsCompleted;
        
        public event EventHandler<GenericEventArgs<RPRecordingsBlob>> GetRecordingsCompleted;
        public event EventHandler<GenericEventArgs<List<TVProgramme>>> SubmitSearchCompleted;
        public event EventHandler<GenericEventArgs<TVProgrammeInfoBlob>> GetProgrammeInfoBlobCompleted;


        #region Dynamic Methods
        // Constructor
        public EPGImporter()
        {

        }

        // Events
        public event EventHandler<GenericEventArgs<string>> GetProgrammesForDateAsZipStringCompleted;
        public event EventHandler<GenericEventArgs<string>> GetProgrammesForEPGRequestsAsZipStringCompleted;
        // Methods

        /// <summary>
        /// Make an HTTP request for the programmes on the specified channels.
        /// Although these channels will already be limited to just the favourite channels if already defined, an additional
        /// flag can make a special, shorter POST request to avoid enumerating all these channel IDs.
        /// </summary>
        /// <param name="localDate"></param>
        /// <param name="limitToFavoriteChannels">Make a shorter POST request to get just favourite channels</param>
        public void GetProgrammesForScheduleSliceAsZipString(ScheduleSliceInfo ssi)
        {
            DateTime localDate = ssi.TheLocalDate;
            string japDate = localDate.Year.ToString() + "-" + localDate.Month.ToString("D2") + "-" + localDate.Day.ToString("D2");
            // Convert list of channels to XML string
            string ChansAsXML = "";
            try
            {
                System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(ssi.TVServiceIDs.GetType());
                StringWriter sw = new StringWriter();
                serializer.Serialize(sw, ssi.TVServiceIDs);
                ChansAsXML = sw.ToString();
            }
            catch 
            {        }
            
            if (String.IsNullOrEmpty(ChansAsXML)) 
            {
                Functions.WriteLineToLogFile("Error - Cannot get programmes; list of channel service IDs is null.");
                GetProgrammesForDateAsZipStringCompleted(new object(), new GenericEventArgs<string>(null));
                return;
            }

            RPWebClient client = new RPWebClient();
            client.GetStringByPostingCompleted += new EventHandler<UploadStringCompletedEventArgs>(GetProgrammesForScheduleSliceAsZipString_DownloadRPStringCompleted);
            string strOmitDescription = Settings.EPGGetShowDescriptions ? "" : "nodescription/";
            client.GetStringByPostingString("xml/programmes/" + strOmitDescription + "limitchannels/date/" + japDate + Settings.ZipDataStreamsAddendum, ChansAsXML);
            
        }
        void GetProgrammesForScheduleSliceAsZipString_DownloadRPStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                ErrorManager.DisplayAndLogError("Sorry, we could not download the TV Programmes from server.\r\nPlease try refreshing or re-loading the server's programme database, especially if you have recently upgraded to a newer version of Remote Potato.  Also try clearing your browser's cache and re-starting or re-loading the Remote Potato server.");
                Functions.WriteExceptionToLogFile(e.Error);
                GetProgrammesForDateAsZipStringCompleted(this, new GenericEventArgs<string>(null));
                return;
            }

            GetProgrammesForDateAsZipStringCompleted(this, new GenericEventArgs<string>(e.Result));
        }
        public void GetMoviesAsZipStringOnServices(List<TVService> services, DateRange dateRange)
        {
            List<EPGRequest> requests = new List<EPGRequest>();
            foreach (TVService svc in services)
            {
                EPGRequest request = new EPGRequest(svc.UniqueId, dateRange);
                
                requests.Add(request);
                
            }

            GetProgrammesForEPGRequestsAsZipString(requests, true, TVProgrammeType.Movie);
        }
        public void GetProgrammesForEPGRequestsAsZipString(List<EPGRequest> requests)
        {
            GetProgrammesForEPGRequestsAsZipString(requests, false, TVProgrammeType.All);
        }
        public void GetProgrammesForEPGRequestsAsZipString(List<EPGRequest> requests, bool limitToProgrammeType, TVProgrammeType programmetype)
        {
            // Convert list of channels to XML string
            string RequestsAsXML = "";
            try
            {
                System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(requests.GetType());
                StringWriter sw = new StringWriter();
                serializer.Serialize(sw, requests);
                RequestsAsXML = sw.ToString();
            }
            catch
            { }

            if (String.IsNullOrEmpty(RequestsAsXML))
            {
                Functions.WriteLineToLogFile("Error - Cannot get programmes; serialized list of EPG Requests is null.");
                if (GetProgrammesForDateAsZipStringCompleted != null) GetProgrammesForEPGRequestsAsZipStringCompleted(new object(), new GenericEventArgs<string>(null));
                return;
            }


            string strOmitDescription = Settings.EPGGetShowDescriptions ? "" : "nodescription/";
            string strURL = "xml/programmes/" + strOmitDescription + "byepgrequest" + Settings.ZipDataStreamsAddendum;


            QueryString qsAdditional = null;

            if (limitToProgrammeType)
            {
                qsAdditional = new QueryString();
                qsAdditional.AddKeyValuePair("programmetype", programmetype.ToString() );
            }

            RPWebClient client = new RPWebClient();
            client.GetStringByPostingCompleted += new EventHandler<UploadStringCompletedEventArgs>(GetProgrammesForEPGRequestsAsZipString_GetStringByPostingCompleted);
            client.GetStringByPostingString(strURL, RequestsAsXML, true, qsAdditional);
            
        }

        void GetProgrammesForEPGRequestsAsZipString_GetStringByPostingCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                ErrorManager.DisplayAndLogError("Sorry, we could not download the TV Programmes from server.\r\nPlease try refreshing or re-loading the server's programme database, especially if you have recently upgraded to a newer version of Remote Potato.  Also try clearing your browser's cache and re-starting or re-loading the Remote Potato server.");
                Functions.WriteExceptionToLogFile(e.Error);
                if (GetProgrammesForEPGRequestsAsZipStringCompleted != null) GetProgrammesForEPGRequestsAsZipStringCompleted(new object(), new GenericEventArgs<string>(null));
                return;
            }

            string strOut = e.Result;
            if (string.IsNullOrEmpty(strOut))
            {
                if (GetProgrammesForEPGRequestsAsZipStringCompleted != null) GetProgrammesForEPGRequestsAsZipStringCompleted(new object(), new GenericEventArgs<string>(null));
                return;
            }

            // Return the zipped string
            if (GetProgrammesForEPGRequestsAsZipStringCompleted != null) GetProgrammesForEPGRequestsAsZipStringCompleted(new object(), new GenericEventArgs<string>(strOut));
        }
        

        public void GetProgrammeInfoBlob(string progUID)
        {
            RPWebClient client = new RPWebClient();
            client.GetStringByGettingCompleted += new EventHandler<UploadStringCompletedEventArgs>(GetProgrammeInfoBlob_ClientCompleted);
            string StrUrl = "xml/programme/getinfo/" + progUID + Settings.ZipDataStreamsAddendum;
            client.GetStringByGetting(StrUrl);
        }
        void GetProgrammeInfoBlob_ClientCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            RPWebClient client = (RPWebClient)sender;
            client.GetStringByGettingCompleted -= new EventHandler<UploadStringCompletedEventArgs>(GetProgrammeInfoBlob_ClientCompleted);

            if (e.Error != null)
            {
                Functions.WriteLineToLogFile("EPGImporter: Failed to submit infoblob request to server:");
                Functions.WriteExceptionToLogFile(e.Error);

                // Create a dummy object to return in the result, with a FALSE status and the error message.
                if (GetProgrammeInfoBlobCompleted != null) GetProgrammeInfoBlobCompleted(new object(), new GenericEventArgs<TVProgrammeInfoBlob>(null));
                return;
            }

            string strOut = e.Result;
            if (string.IsNullOrEmpty(strOut))
            {
                // No extra info for this show (or an error)
                // Create a dummy object to return in the result, with a FALSE status and the error message.
                if (GetProgrammeInfoBlobCompleted != null) GetProgrammeInfoBlobCompleted(new object(), new GenericEventArgs<TVProgrammeInfoBlob>(null));
                return;
            }

            // TEMPORARY:  EXTRA ERROR DETECTION FOR REMOTE POTATO SERVER STANDARD HTML ERROR MESSAGE
            if (strOut.Contains("error occurred"))
            {
                // No extra info for this show (or an error)
                // Create a dummy object to return in the result, with a FALSE status and the error message.
                if (GetProgrammeInfoBlobCompleted != null) GetProgrammeInfoBlobCompleted(new object(), new GenericEventArgs<TVProgrammeInfoBlob>(null));
                return;
            }

            if (Settings.ZipDataStreams)
            {
                if (!ZipManager.UnzipString(ref strOut))
                {
                    ErrorManager.DisplayAndLogError("Could not unzip downloaded info blob from server.");
                    // ERROR
                    if (GetProgrammeInfoBlobCompleted != null) GetProgrammeInfoBlobCompleted(new object(), new GenericEventArgs<TVProgrammeInfoBlob>(null));
                    return;
                }
            }

            
            TVProgrammeInfoBlob blob = new TVProgrammeInfoBlob();
            XmlSerializer serializer = new XmlSerializer(blob.GetType());
            StringReader sr = new StringReader(strOut);
            blob = (TVProgrammeInfoBlob)serializer.Deserialize(sr);

            if (GetProgrammeInfoBlobCompleted != null) GetProgrammeInfoBlobCompleted(new object(), new GenericEventArgs<TVProgrammeInfoBlob>(blob));
        }

        public void GetAllChannels()
        {
            RPWebClient client = new RPWebClient();
            client.GetStringByGettingCompleted += new EventHandler<UploadStringCompletedEventArgs>(GetAllChannels_DownloadRPStringCompleted);
            client.GetStringByGetting("xml/channels/all" + Settings.ZipDataStreamsAddendum);
        }
        void GetAllChannels_DownloadRPStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                // ERROR
                ErrorManager.DisplayAndLogError("Could not get list of channels from server.");
                Functions.WriteExceptionToLogFile(e.Error);
                GetChannelsCompleted(new object(), new GenericEventArgs<List<TVService>>(new List<TVService>()));
                return;
            }

            string strOut = e.Result;
            if (Settings.ZipDataStreams)
            {
                if (!ZipManager.UnzipString(ref strOut))
                {
                    ErrorManager.DisplayAndLogError("Could not unzip downloaded TV channels from server.");
                    // ERROR
                    GetChannelsCompleted(new object(), new GenericEventArgs<List<TVService>>(new List<TVService>()));
                    return;
                }
            }

            List<TVService> theChannels = new List<TVService>();
            XmlSerializer serializer = new XmlSerializer(theChannels.GetType());
            StringReader sr = new StringReader(strOut);
            theChannels = (List<TVService>)serializer.Deserialize(sr);

            GetChannelsCompleted(new object(), new GenericEventArgs<List<TVService>>(theChannels));
        }

        public void GetAllTVRecordingEvents()
        {
            RPWebClient client = new RPWebClient();
            client.GetStringByGettingCompleted += new EventHandler<UploadStringCompletedEventArgs>(GetAllTVRecordingEvents_DownloadRPStringCompleted);
            client.GetStringByGetting("xml/recordings" + Settings.ZipDataStreamsAddendum);
        }
        void GetAllTVRecordingEvents_DownloadRPStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                ErrorManager.DisplayAndLogError("Could not get list of recording events from server.");
                Functions.WriteExceptionToLogFile(e.Error);
                GetRecordingsCompleted(new object(), new GenericEventArgs<RPRecordingsBlob>(null));
                return;
            }

            string strOut = e.Result;
            if (Settings.ZipDataStreams)
            {
                if (!ZipManager.UnzipString(ref strOut))
                {
                    ErrorManager.DisplayAndLogError("Could not unzip downloaded recording events from server.");
                    // ERROR
                    GetRecordingsCompleted(new object(), new GenericEventArgs<RPRecordingsBlob>(null));
                    return;
                }
            }
            RPRecordingsBlob recBlob = new RPRecordingsBlob();
            XmlSerializer serializer = new XmlSerializer(recBlob.GetType());
            StringReader sr = new StringReader(strOut);
            recBlob = (RPRecordingsBlob)serializer.Deserialize(sr);

            GetRecordingsCompleted(new object(), new GenericEventArgs<RPRecordingsBlob>(recBlob));
        }

        public void SubmitSearchRequestToServer(EPGSearch theSearch)
        {
            RPWebClient client = new RPWebClient();
            client.GetStringByPostingCompleted += new EventHandler<UploadStringCompletedEventArgs>(SubmitSearch_DownloadRPStringCompleted);
            client.GetStringByPostingObject("xml/programmes/search" + Settings.ZipDataStreamsAddendum, theSearch);
        }
        void SubmitSearch_DownloadRPStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            RPWebClient client = (RPWebClient)sender;
            client.GetStringByPostingCompleted -= new EventHandler<UploadStringCompletedEventArgs>(SubmitSearch_DownloadRPStringCompleted);

            if (e.Error != null)
            {
                Functions.WriteLineToLogFile("EPGImporter: Failed to submit search request to server:");
                Functions.WriteExceptionToLogFile(e.Error);

                // Create a dummy object to return in the result, with a FALSE status and the error message.
                if (SubmitSearchCompleted != null) SubmitSearchCompleted(new object(), new GenericEventArgs<List<TVProgramme>>(new List<TVProgramme>()));
                return;
            }
            
            string strOut = e.Result;
            if (Settings.ZipDataStreams)
            {
                if (!ZipManager.UnzipString(ref strOut))
                {
                    ErrorManager.DisplayAndLogError("Could not unzip downloaded TV Programmes from server.");
                    // ERROR
                    if (SubmitSearchCompleted != null) SubmitSearchCompleted(new object(), new GenericEventArgs<List<TVProgramme>>(new List<TVProgramme>()));
                    return;
                }
            }

            List<TVProgramme> theProgrammes = new List<TVProgramme>();
            XmlSerializer serializer = new XmlSerializer(theProgrammes.GetType());
            StringReader sr = new StringReader(strOut);
            theProgrammes = (List<TVProgramme>)serializer.Deserialize(sr);

            if (SubmitSearchCompleted != null) SubmitSearchCompleted(new object(), new GenericEventArgs<List<TVProgramme>>(theProgrammes));
        }

        #endregion
    }

    // Enums
    public enum ChannelFilterTypes
    {
        AllChannels,
        Favourites,
        Custom
    }


}
