using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace SilverPotato
{
    public class RPWebClient
    {
        private WebClient _webclient;
        
        public RPWebClient() 
        {
            _webclient = new WebClient();
            /*
            if (Functions.RPHostRequiresPassword)
                _webclient.Headers["RPAuth"] = Functions.RPHostPassword; */
        }



        #region GET method
        public event EventHandler<UploadStringCompletedEventArgs> GetStringByGettingCompleted;
        public void GetStringByGetting(string pathName)
        {
            GetStringByGetting(pathName, true);
        }
        public void GetStringByGetting(string pathName, bool useAuthToken)
        {
            string fullURL = NetworkManager.hostURL;

            if (pathName.StartsWith("/"))
                pathName = pathName.Substring(1);

            string txtAppendAuthToken = useAuthToken ? ("?token=" + NetworkManager.serverToken) : "";
            Uri newUri = new Uri(fullURL + pathName + txtAppendAuthToken);
            _webclient.UploadStringCompleted += new UploadStringCompletedEventHandler(_webclient_DownloadStringCompleted);

            if (Settings.DebugHTTP) Functions.WriteLineToLogFile("Getting String from URL : " + newUri.AbsoluteUri);
            VisualManager.ShowNetworkActivity();
            _webclient.UploadStringAsync(newUri,"");
            
        }
        void _webclient_DownloadStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            VisualManager.HideNetworkActivity();
            if (e.Error != null)
            {
                Functions.WriteLineToLogFile("RPWebClient: Error getting string from URL.");

            }

            if (GetStringByGettingCompleted != null)
                GetStringByGettingCompleted(this, e);
        }
        #endregion

        #region POST method
        public event EventHandler<UploadStringCompletedEventArgs> GetStringByPostingCompleted;
        public void GetStringByPostingObject(string pathName, object objectToUpload)
        {
            GetStringByPostingObject(pathName, objectToUpload, true);
        }
        public void GetStringByPostingString(string pathName, string stringToUpload)
        {
            GetStringByPostingString(pathName, stringToUpload, true);
        }
        public void GetStringByPostingObject(string pathName, object objectToUpload, bool useAuthToken)
        {
            System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(objectToUpload.GetType());
            StringWriter sw = new StringWriter();
            serializer.Serialize(sw, objectToUpload);
            GetStringByPostingString(pathName, sw.ToString(), useAuthToken);
        }

        public void GetStringByPostingString(string pathName, string stringToUpload, bool useAuthToken)
        {
            GetStringByPostingString(pathName, stringToUpload, useAuthToken, null);
        }
        public void GetStringByPostingString(string pathName, string stringToUpload, bool useAuthToken, QueryString additionalQS)
        {
            string fullURL = NetworkManager.hostURL;

            if (additionalQS == null)
                additionalQS = new QueryString();

            if (pathName.StartsWith("/"))
                pathName = pathName.Substring(1);

            if (useAuthToken)
                additionalQS.AddKeyValuePair("token", NetworkManager.serverToken);
            
            Uri newUri = new Uri(fullURL + pathName + additionalQS.ToString() );

            _webclient.UploadStringCompleted += new UploadStringCompletedEventHandler(_webclient_UploadRPStringCompleted);
            if (Settings.DebugHTTP) Functions.WriteLineToLogFile("GetStringByPostingString:  Uploading string to URL : " + newUri.AbsoluteUri);
            VisualManager.ShowNetworkActivity();
            _webclient.Encoding = System.Text.Encoding.UTF8;  // NEW!
            _webclient.UploadStringAsync(newUri, stringToUpload);
        }
        void _webclient_UploadRPStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            VisualManager.HideNetworkActivity();
            if (e.Error != null)
            {
                Functions.WriteLineToLogFile("RPWebClient: Error getting string from URL when uploading.");
            }
            GetStringByPostingCompleted(this, e);
            
        }
        #endregion



    }
}
