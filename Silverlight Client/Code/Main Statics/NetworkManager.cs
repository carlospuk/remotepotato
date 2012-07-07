using System;
using System.Collections.Generic;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Linq;

namespace SilverPotato
{
    public static class NetworkManager
    {

        public static string serverToken = "";
        public static Version ServerVersion = new Version("0.0");
        public static string _hostURL = "";
        public static ServerCapabilities ServerCapability;
        public static string ServerOSString;

        public static void Initialise()
        {
            // Store host URL - this MUST be done here as can only be done on the UI thread
            Functions.WriteLineToLogFile("Initialising network manager; host URL is " + NetworkManager.hostURL);
        }

        /// <summary>
        /// URL of the hosting page, with trailing / slash 
        /// </summary>
        public static string hostURL
        {
            get
            {
                if (! string.IsNullOrEmpty(_hostURL)) return _hostURL;

#if SILVERPOTATO
                
                Uri docUri = System.Windows.Browser.HtmlPage.Document.DocumentUri;

                string docPort = (docUri.Port == 80) ? "" : (":" + docUri.Port.ToString() );
                _hostURL = "http://" + docUri.Host + docPort + "/";
                
#endif

#if WINDOWS_PHONE
                _hostURL = "http://bigbitemedia.dyndns.org:9090/";
                
#endif

#if DEBUG
                                //   _hostURL = "http://lisselan.webhop.net:9080/";
                                //  _hostURL = "http://tv.kairubyte.com/";
                            //   _hostURL = "http://bigbitemedia.dyndns.org:9090/";
                             //      _hostURL = "http://192.168.0.32:9080/"; // fakeXP
                                   // _hostURL = "http://192.168.0.33:9080/"; // fakeVista
                                    _hostURL = "http://127.0.0.1:9080/";

#endif

                return _hostURL; // stored in _hostURL for next time
            }
        }
        /// <summary>
        /// URL of the hosting page, with trailing / slash 
        /// </summary>
        static string hostStreamingURLTemplate
        {
            get
            {
                string hURL = hostURL;
                string streamURL = hostURL.Replace("http://", "");
                
                // Remove any port
                int colonLoc = streamURL.LastIndexOf(":");
                if ( colonLoc > 0)
                {
                    streamURL = streamURL.Substring(0, colonLoc);
                }

                // Remove any trailing slash 
                if (streamURL.EndsWith("/"))
                    streamURL = streamURL.Substring(0, streamURL.Length - 1);

                // Append streaming port and protocol at beginning
                /*
                 int sPort = (SettingsImporter.SettingAsIntOrZero("SilverlightStreamingPort"));
                if (sPort > 0)
                    streamURL += ":" + sPort.ToString();
                else
                    Functions.WriteLineToLogFile("Error - cannot get streaming port value; please refresh settings!");
                 */
                streamURL += ":**PORT**";

                streamURL = "mms://" + streamURL + "/fakefile.wmv";
                //streamURL = "http://" + streamURL;

                return streamURL;
            }
        }
        public static string hostStreamingURLForPort(string Port)
        {
            string streamURL = hostStreamingURLTemplate;

            streamURL = streamURL.Replace("**PORT**", Port);

            return streamURL;
        }
        public static string hostHLS_URL_WithRelativePath(string strRelativePath)
        {
            string hURL = hostURL;

            Uri baseUri = new Uri(hURL);
            Uri relUri = new Uri(baseUri, strRelativePath);
            return relUri.ToString();
        }

        public static event EventHandler<ServerReadyEventArgs> ServerAvailabilityCheck_Completed;
        public static void CheckServerIsRunning()
        {
            Functions.WriteLineToLogFile("Polling RP server...");
            RPWebClient client = new RPWebClient();
            client.GetStringByGettingCompleted += new EventHandler<UploadStringCompletedEventArgs>(ping_DownloadRPStringCompleted);
            client.GetStringByGetting("ping",false);
        }
        static void ping_DownloadRPStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                Functions.WriteLineToLogFile("Server not found.");
                Functions.WriteExceptionToLogFile(e.Error);

                if (ServerAvailabilityCheck_Completed != null) ServerAvailabilityCheck_Completed(new object(), new ServerReadyEventArgs(false,false,new Version(0,0)));
                return;
            }

            Functions.WriteLineToLogFile("Server responded: " + e.Result);

            string lcResult = e.Result.ToUpper();

            // GET SERVER VERSION
            string strServerVersion = tagValueWithinString(lcResult, "SERVERVERSION");
            try
            {
                ServerVersion = new Version(strServerVersion);
            }
            catch (Exception ex)
            {
                Functions.WriteLineToLogFile("Error parsing server version string.");
                Functions.WriteExceptionToLogFile(ex);
                ServerVersion = new Version(0, 0);
            }

            // CAPABILITIES
            string strServerCapabilityString = tagValueWithinString(lcResult, "SERVERCAPABILITIES");
            ServerCapability = new ServerCapabilities(strServerCapabilityString);

            ServerOSString = tagValueWithinString(lcResult, "SERVEROSVERSIONSTRING");

            // DETERMINE LOGIN REQUIREMENTS
            if (lcResult.Contains("NEED_PASSWORD"))
            {
                Functions.WriteLineToLogFile("Server found: Password required.");
                if (ServerAvailabilityCheck_Completed != null) ServerAvailabilityCheck_Completed(new object(), new ServerReadyEventArgs(true, true, ServerVersion));
            }
            else
            {
                Functions.WriteLineToLogFile("Server found: No password required.");
                if (ServerAvailabilityCheck_Completed != null) ServerAvailabilityCheck_Completed(new object(), new ServerReadyEventArgs(true, false, ServerVersion));
                return;
            }
            
        }
        static string tagValueWithinString(string inString, string tagName)
        {
            int tagNameStart = inString.IndexOf(tagName);
            if (tagNameStart < 1) return "";
            int valueSpeechMarkStart = inString.IndexOf("\"", tagNameStart);
            if (valueSpeechMarkStart < 1) return "";
            int valueSpeechMarkEnd = inString.IndexOf("\"", valueSpeechMarkStart+1);
            if (valueSpeechMarkEnd < 1) return "";
            try
            {
                return inString.Substring(valueSpeechMarkStart + 1, valueSpeechMarkEnd - valueSpeechMarkStart - 1);
            }
            catch
            {
                return "";
            }
        }


        public static event EventHandler<GenericEventArgs<bool>> ServerLoginComplete;
        public static void LoginToServer(string un, string pw)
        {
            Functions.WriteLineToLogFile("Logging into RP server...");
            RPWebClient client = new RPWebClient();
            client.GetStringByGettingCompleted += new EventHandler<UploadStringCompletedEventArgs>(login_DownloadRPStringCompleted);

            // Encode
            un = Uri.EscapeUriString( Functions.EncodeToBase64(un));
            pw = Uri.EscapeUriString( Functions.EncodeToBase64(pw));

            client.GetStringByGetting("xml/login64?un=" + un + "&pw=" + pw, false);  // the only time we don't use an Auth Token is when logging in
        }
        static void login_DownloadRPStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                Functions.WriteLineToLogFile("(Login) Server not found.");
                Functions.WriteExceptionToLogFile(e.Error);
                if (ServerLoginComplete != null) ServerLoginComplete(new object(), new GenericEventArgs<bool>(false));
                return;
            }

            string ucResult = e.Result;

            if (! ucResult.Contains("OK"))
            {
                Functions.WriteLineToLogFile("(Login) Login to server failed.  (" + ucResult + ")");
                if (ServerLoginComplete != null) ServerLoginComplete(new object(), new GenericEventArgs<bool>(false));
                return;
            }

            // Success
            Functions.WriteLineToLogFile("(Login) Login to server OK.");
            // Get token
            int tagLoc = ucResult.IndexOf("token=");
            if (tagLoc > 0)
            {
                int tokLoc = tagLoc + 7;
                int tokEndLoc = ucResult.IndexOf("\"", tokLoc +1 );
                NetworkManager.serverToken = ucResult.Substring(tokLoc, (tokEndLoc - tokLoc));
                if (Settings.DebugHTTP) Functions.WriteLineToLogFile("Got token from server: " + NetworkManager.serverToken);
            }

            if (ServerLoginComplete != null) ServerLoginComplete(new object(), new GenericEventArgs<bool>(true));
                

        }

    }

    public class ServerCapabilities
    {
        private List<string> capFlags;

        public ServerCapabilities(string capString)
        {
            if (string.IsNullOrEmpty(capString))
                capFlags = new List<string>();
            else
                ParseCapString(capString);
        }
        void ParseCapString(string cs)
        {
            capFlags = cs.ToUpper().Split(',').ToList<string>();
        }

        public bool HasMusicLibrary
        {
            get
            {
                return (capFlags.Contains("MUSIC")) || (capFlags.Count == 0);
            }
        }
        public bool HasPictureLibrary
        {
            get
            {
                return (capFlags.Contains("PICTURES")) || (capFlags.Count == 0);
            }
        }
        public bool HasVideoLibrary
        {
            get
            {
                return (capFlags.Contains("VIDEOS")) || (capFlags.Count == 0);
            }
        }
                
        public bool HasRecordedTVLibrary
        {
            get
            {
                return (capFlags.Contains("RECORDEDTV")) || (capFlags.Count == 0);
            }
        }
        public bool HasMediaCenterSupport
        {
            get
            {
                return (capFlags.Contains("MCE")) || (capFlags.Count == 0);
            }
        }
        public bool HasWMStreaming
        {
            get
            {
                return (capFlags.Contains("STREAM-MSWMSP")) || (capFlags.Count == 0);
            }
        }
    }

    public class ServerReadyEventArgs : EventArgs
    {
        public readonly bool ServerIsFound;
        public readonly bool ServerRequiresPassword;
        public readonly Version ServerVersion;

        public ServerReadyEventArgs(bool found, bool needsPW, Version serverVersion)
        {
            ServerIsFound = found;
            ServerRequiresPassword = needsPW;
            ServerVersion = serverVersion;
        }

    }
}

