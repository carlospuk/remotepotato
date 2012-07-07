using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Drawing;
using System.Drawing.Imaging;
using System.Web;
using System.Xml;
using System.Globalization;
using System.Threading;
using Toub.MediaCenter.Dvrms.Metadata;
using System.Runtime.InteropServices;  // debug DVRMS wm/picture
using System.Collections.Specialized;
using System.Security.Principal;
using RemotePotatoServer.Properties;
using CommonEPG;
using FatAttitude.WTVTranscoder;
using FatAttitude.MediaStreamer;
using FatAttitude;
using FatAttitude.HTML;

namespace RemotePotatoServer
{
    class RequestProcessor : IDisposable
    {

        #region STATIC Methods - tracking Ips, strings, etc
        // Store ip, state data
        static RequestProcessor()
        {
            lastEPGQueryStringForIP = new Dictionary<string, NameValueCollection>();
        }
        private static Dictionary<string, NameValueCollection> lastEPGQueryStringForIP;
        private static string GetClientIPFromSocket(ref Socket ActiveSocket)
        {
            string currentClientEndpoint = ActiveSocket.RemoteEndPoint.ToString();
            if (currentClientEndpoint.Contains(":"))
                currentClientEndpoint = currentClientEndpoint.Substring(0, currentClientEndpoint.IndexOf(":"));
            return currentClientEndpoint;
        }

        #region Schedule Recordings
        private static bool scheduleRecordingForHTML(RecordingRequest rr, out string txtScheduleResult)
        {
            txtScheduleResult = "Unknown result.";
            if (!EPGManager.isWMCOnline) return false;

            RecordingResult recresult = EPGManager.ScheduleRecording(rr);

            if (recresult.Success)
            {
                txtScheduleResult = RecordingResult.FriendlySuccessReport(recresult);

                if (rr.RequestType != RecordingRequestType.Manual)
                    txtScheduleResult += ListOfRecordingsAsHTML(ref recresult.GeneratedRecordingsBlob.RPRecordings, DateRangeType.All, false, true, true, true);
            }
            else
            {
                txtScheduleResult = RecordingResult.FriendlyFailureReason(recresult);
            }

            return true;
            
        }
        #endregion

        #region Debug
        // Event to send back Status Reports
        private static void spoolMessage(string msg)
        {
            Functions.WriteLineToLogFile(msg);
        }
        #endregion

        #endregion

        #region Dynamic Methods - per connection
        // Private Objects (dynamic)
        private string currentClientIP;
        private NameValueCollection qsParams;
        private string txtPageTitle;
        private string txtResponse;
        private string XMLresponse;
        private bool AuthenticatedByToken;

        // New
        HttpListenerContext Context;
        HttpListenerRequest Request;
        HttpListenerResponse Response;
        IPrincipal User;

        // Streaming
        public RequestProcessor()
        {
        }
        public RequestProcessor(HttpListenerContext context)
            : this()
        {
            Request = context.Request;
            Response = context.Response;
            Context = context;
            User = context.User;

        }
        public void Dispose()
        {
            qsParams = null;
            XMLresponse = null;
            txtResponse = null;
        }

        private void ShowMainMenu()
        {
            txtResponse += "<ul id='mainmenu'>";
            if (Settings.Default.EnableMediaCenterSupport)
            {
                if (!string.IsNullOrEmpty(Settings.Default.CurrentEPGType))
                    txtResponse += "<li><a href=\"viewepg" + Settings.Default.CurrentEPGType + "\">Browse Listings</a></li>";
                else
                    txtResponse += "<li><a href=\"viewepggrid\">Browse Listings</a></li>";

                txtResponse += "<li><a href=\"searchbytitle\">Search for a show</a></li>";
                txtResponse += "<li><a href=\"recordmanual\">Manual Recording</a></li>";
            }
            
            //txtResponse += "<li><a href=\"recordshow_onetime\">Record by show name</a></li>";
            txtResponse += "<li><a href=\"recordedtv\">Recorded TV</a></li>";

            if (Settings.Default.EnableMediaCenterSupport)
            {
                txtResponse += "<li><a href=\"scheduledrecordings\">Scheduled Recordings</a></li>";
                txtResponse += "<li><a href=\"viewseriesrequests\">Manage Series</a></li>";
                txtResponse += "<li><a href=\"movies\">Movies</a></li>";
            }

            if (Settings.Default.EnableMusicLibrary)
                txtResponse += "<li><a href=\"musicroot\">Music Library</a></li>";

            if (Settings.Default.EnablePictureLibrary)
                txtResponse += "<li><a href=\"browsepics\">Picture Library</a></li>";

            if (Settings.Default.EnableVideoLibrary)
                txtResponse += "<li><a href=\"browsevideos\">Video Library</a></li>";

            if (Settings.Default.SilverlightEnabled )
            {
                txtResponse += "<li><a href=\"silverlight\">Enhanced View (Silverlight)</a></li>";
            }

            if (Settings.Default.EnableMediaCenterSupport)
                txtResponse += "<li><a href=\"remotecontrol\">Remote Control</a></li>";
            
            txtResponse += "<li><a href='info'>More Information</a></li>";
            txtResponse += "<li><a href='log-out'>Log Out</a></li>";
            txtResponse += "</ul>";

            txtPageTitle = "Main Menu";

        }

        #region Authentication Helpers
        private bool canProceedAuthenticatedByHTTPCookie()
        {
            if (!Settings.Default.RequirePassword)
                return true;

            if (Settings.Default.DebugServer) spoolMessage("Webserver: Checking for authentication cookie.");
            Cookie c = (Cookie)Request.Cookies["RemotePotatoToken"];
            if (c == null) return false;
            string strToken = c.Value;
            if (string.IsNullOrEmpty(strToken)) return false;

            bool result = AuthSessionHelper.Default.AuthenticateToken(strToken, currentClientIP);
            
            if (result)
                if (Settings.Default.DebugServer) spoolMessage("Webserver: Authentication Cookie OK.");
            else
                if (Settings.Default.DebugServer) spoolMessage("Webserver: No authentication cookie or bad authentication cookie.");
            
            return result;
        }
        #endregion

        #region Helpers - Headers, HTTP Request Processing etc.
        public event EventHandler<MessageEventArgs> UserAgentConnected;
        string txtActionOriginalCase;
        public string GetActionFromBrowserRequest()
        {
            string initialUrl = Request.Url.PathAndQuery;
            if (Request.Url.Query != "")
                initialUrl = initialUrl.Replace(Request.Url.Query, "");

            if (initialUrl.StartsWith("/"))
                initialUrl = initialUrl.Substring(1);

            // Make the action LOWER CASE (important)
            txtActionOriginalCase = initialUrl;
            if (txtActionOriginalCase.StartsWith("open/"))
                txtActionOriginalCase = txtActionOriginalCase.Replace("open/","");

            return initialUrl.ToLowerInvariant();
        }
        private void processUserAgentStringFromRequestHeaders()
        {
            string ua = Request.UserAgent;
            if (string.IsNullOrEmpty(ua)) return;  // No mobile theme change will be made to the theme

            // Pass user agent string back to main app
            if (UserAgentConnected != null) UserAgentConnected(this, new MessageEventArgs(ua));

            foreach (string agent in Settings.Default.MobileUserAgents)
            {
                if (ua.Contains(agent))
                {
                    Themes.UsingMobileTheme = true;  // within Themes, this triggers an event to re-load the theme settings if theme type has changed
                    return;
                }
            }

            Themes.UsingMobileTheme = false;  // within Themes, this triggers an event to re-load the theme settings if theme type has changed
        }
        // STORE QUERYSTRINGS
        private void storeEPGQuerystringForCurrentClient()
        {
            if (lastEPGQueryStringForIP.ContainsKey(currentClientIP))
                lastEPGQueryStringForIP.Remove(currentClientIP);

            lastEPGQueryStringForIP.Add(currentClientIP, qsParams);
        }
        #endregion

        #region New Connection Top-Level


        public void Run()
        {
#if !DEBUG
            try
            {
#endif
            // To store headers and styles
            List<string> AdditionalStyles = new List<string>();

            // Set HTTP response version to 1.1 (experimental)
            //  Context.Response.ProtocolVersion = new Version("1.1");

            BrowserSender browserSender = new BrowserSender(Context);
            currentClientIP = Request.RemoteEndPoint.Address.ToString();
            qsParams = Request.QueryString;

            if (Settings.Default.DebugServer) spoolMessage("Client Connected (" + Request.RemoteEndPoint.Address.ToString() + ")");

            if ((Settings.Default.DebugAdvanced) && (Settings.Default.DebugServer))
            {
                spoolMessage("Headers from client:");
                for (int i = 0; i < Request.Headers.Count; ++i)
                    spoolMessage(string.Format("{0}: {1}", Request.Headers.Keys[i], Request.Headers[i]));
            }

            // Split request into lines
            string txtPostObjects = "";
            if (Request.HttpMethod.Equals("POST"))
            {
                StreamReader sr = new StreamReader(Request.InputStream);
                txtPostObjects = sr.ReadToEnd();
            }
            
            // User agent - detect mobile
            processUserAgentStringFromRequestHeaders();

            // Get action string from Url
            string txtAction = GetActionFromBrowserRequest();

            if (Settings.Default.DebugServer)
            {
                Functions.WriteLineToLogFile("From Client: " + txtAction);
            }

            // Build response
            txtResponse = "";
            txtPageTitle = Settings.Default.MainMenuTitle;
            bool foo;

            // R.I.P. Open server  (keep this for legacy compatibility)
            if (txtAction.StartsWith("open"))
                txtAction = txtAction.Substring(5);

            // Special cases / conversions
            if (txtAction.ToLowerInvariant().Equals("apple-touch-icon.png"))
                txtAction = "static/images/apple-touch-icon.png";

            // Querystring authentication is one possible method that overrides all others if true: check for token (and renew)
            if (txtAction.StartsWith("xml/checktoken")) // Special open method - check a token
            {
                bool ignore = CheckForTokenAuthentication();
                string checkForTokenResult = AuthenticatedByToken ? "GOOD" : "BAD";
                string xCheckResponse = "<?xml version=\"1.0\"?><checktokenresponse result=\"" + checkForTokenResult +  "\" />";
                browserSender.SendXMLToBrowser(xCheckResponse);
                return;
            }
            else if (!CheckForTokenAuthentication())
            {
                // invalid token
                browserSender.SendGenericStatusCodePage("403", "Incorrect authentication");
                spoolMessage("API: failed authentication via token.");
                return;
            }               

            // XML METHODS - no HTTP authentication required (uses token-based auth)
            if (txtAction.StartsWith("xml"))
            {
                XMLresponse = "";
                WebServiceProcess(txtAction, ref browserSender, ref txtPostObjects);
                return;
            }

            // Any other non-authenticated methods
            switch (txtAction)
            {
                // SPECIAL FILE NAME SHORTCUTS - NO AUTH REQUIRED **************
                case "robots.txt":
                    if (!browserSender.SendFileToBrowser("static\\robots.txt"))
                        Functions.WriteLineToLogFile("Could not send robots.txt to browser");
                    return;
                case "clientaccesspolicy.xml":
                    if (!browserSender.SendFileToBrowser("static\\clientaccesspolicy.xml"))
                        Functions.WriteLineToLogFile("Could not send clientaccesspolicy.xml to browser (presumably to Silverlight)");
                    return;
                case "silverlightsource":
                    if (!browserSender.SendFileToBrowser("static\\silverlight\\SilverPotato.xap"))
                        Functions.WriteLineToLogFile("Could not send SilverPotato XAP to browser");
                    return;

                //Ping is allowed
                case "ping":
                    Version v = Functions.ServerVersion;
                    string xResponse = "<?xml version=\"1.0\"?><pingresponse result=\"PING_RESULT\" serverversion=\"SERVER_VERSION\" serverrevision=\"SERVER_REVISION\" serverosversionstring=\"SERVER_OS_VERSION_STRING\" serverosversion=\"SERVER_OS_VERSION\" servercapabilities=\"CAP_FLAGS\" />";
                    xResponse = xResponse.Replace("PING_RESULT", Settings.Default.RequirePassword ? "NEED_PASSWORD" : "OK");
                    xResponse = xResponse.Replace("SERVER_VERSION", v.Major.ToString() + "." +
                    v.Minor.ToString() );  // This is culture invariant
                    xResponse = xResponse.Replace("SERVER_OS_VERSION_STRING", Environment.OSVersion.VersionString);
                    xResponse = xResponse.Replace("SERVER_OS_VERSION", Environment.OSVersion.Version.ToString(2) );
                    xResponse = xResponse.Replace("SERVER_REVISION", v.Build.ToString());
                    xResponse = xResponse.Replace("CAP_FLAGS", Functions.ServerCapabilities);
                    browserSender.SendXMLToBrowser(xResponse);
                    return;

                // Fav Icon is allowed
                case "favicon.ico":
                    browserSender.SendFileToBrowser(HttpUtility.UrlDecode("static\\images\\remotepotatoicon.ico"));
                    return;

                default:
                    break;
            }

            // Channel logos are allowed
            if ((txtAction.StartsWith("logo")))
            {
                int hashlocation = txtAction.LastIndexOf("/");
                if (hashlocation < 1)
                {
                    bool fooa = browserSender.Send404Page();
                }
                else
                {
                    txtAction = txtAction.Replace("logo/", "");
                    string logoSvcID = HttpUtility.UrlDecode(txtAction);

                    // Send logo to browser
                    browserSender.SendLogoToBrowser(logoSvcID);
                }
                return;
            }
            
            // Special case 'static' files that aren't => legacy support for streaming
            if (txtAction.StartsWith("httplivestream"))
            {
                ProcessHTTPLSURL(txtAction, ref browserSender);
                return;
            }


            // Static Files 
            if ( (txtAction.StartsWith("static")) )
            {
                int hashlocation = txtAction.LastIndexOf("/");
                if (hashlocation < 1)
                {
                    bool fooa = browserSender.Send404Page();
                }
                else
                {
                    // Send file
                    browserSender.SendFileToBrowser(HttpUtility.UrlDecode(txtAction));
                }
                return;
            }

            // Skin files
            if ( (txtAction.StartsWith("skin")))
            {
                int hashlocation = txtAction.LastIndexOf("/");
                if (hashlocation < 1)
                {
                    bool fooa = browserSender.Send404Page();
                }
                else
                {
                    // Send file
                    browserSender.SendFileToBrowser(HttpUtility.UrlDecode(txtAction), true, false);
                }
                return;
            }

            // Thumbnails are allowed
            if (txtAction == "rectvthumbnail64")
            {
                GetRecTVThumbnail(ref browserSender, true);
                return;
            }
            else if (txtAction == "rectvthumbnail")
            {
                GetRecTVThumbnail(ref browserSender, false);
                return;
            }

            if (txtAction.StartsWith("getfilethumbnail64"))
            {
                GetFileThumbnailUsingQueryString(ref browserSender, true);
                return;
            }
            else if (txtAction.StartsWith("getfilethumbnail"))
            {
                GetFileThumbnailUsingQueryString(ref browserSender, false);
                return;
            }

            if (txtAction.StartsWith("filethumbnail"))
            {
                string txtSize = txtAction.Replace("filethumbnail/","");
                FatAttitude.ThumbnailSizes size = (FatAttitude.ThumbnailSizes) Enum.Parse( (new FatAttitude.ThumbnailSizes().GetType() ), txtSize, true);

                SendFileThumbnail(txtPostObjects, size, ref browserSender); 
                return;
            }
            if (txtAction.StartsWith("musicalbumthumbnail"))
            {
                GetAlbumThumbnail(ref browserSender, txtAction.Contains("musicalbumthumbnail64") );
                return;
            }
            if (txtAction.StartsWith("musicsongthumbnail"))
            {
                GetSongThumbnail(ref browserSender, txtAction.Contains("musicsongthumbnail64"));
                return;
            }
     

            // Silverlight is allowed (no longer contains password info)
            bool showSilverlight = (txtAction.StartsWith("silverlight"));
            if (Settings.Default.SilverlightIsDefault)
                showSilverlight = showSilverlight | (txtAction.Trim().Equals(""));

            if (showSilverlight)
            {
                string silverTemplate = FileCache.ReadTextFile("static\\silverlight\\default_template.htm");
                browserSender.SendNormalHTMLPageToBrowser(silverTemplate);
                return;
            }


            // MORE OPEN METHODS...
            if (txtAction.StartsWith("streamsong"))
            {

                bool isBase64Encoded = (txtAction.StartsWith("streamsong64"));

                if (!SendSongToBrowser(ref browserSender, isBase64Encoded,  true, false))
                    browserSender.Send404Page();
                return;
            }

            // MORE OPEN METHODS...
            if (txtAction.StartsWith("downloadsong"))
            {

                bool isBase64Encoded = (txtAction.StartsWith("downloadsong64"));

                if (!SendSongToBrowser(ref browserSender, isBase64Encoded, true, true))
                    browserSender.Send404Page();
                return;
            }

            // ********************************************************************************************
            // Cookie Authentication Required for all Methods below here **********************************
            // ********************************************************************************************

            bool processMoreActions = false;
            if (canProceedAuthenticatedByHTTPCookie())
            {
                processMoreActions = true;
            }
            else
            {
                spoolMessage("Webserver: requesting login.");

                bool LoginSuccess = false;
                string destURL = "";
                string destQueryString = "";
                ViewLoginPage(txtPostObjects, ref LoginSuccess, ref destURL, ref destQueryString);

                // Successful login
                if (LoginSuccess)
                {
                    processMoreActions = true;
                    txtPageTitle = "";
                    // Assign new (old) action and querystring for further processing
                    txtAction = destURL;
                    qsParams = HttpUtility.ParseQueryString(destQueryString);

                    // We've missed the silverlight check (it's up above), so check again
                    if (Settings.Default.SilverlightIsDefault)
                    {
                        string silverTemplate = FileCache.ReadTextFile("static\\silverlight\\default_template.htm");
                        browserSender.SendNormalHTMLPageToBrowser(silverTemplate);
                        return;
                    }

                }

                
            }

            bool sentWholePage = false;
            if (processMoreActions)
            {
                switch (txtAction)
                {
                    // Legacy Streamsong  (secured)
                    case "streamsong.mp3":
                        if (!SendSongToBrowser(ref browserSender, false, true, false))
                            browserSender.Send404Page();
                        return;
                    case "streamsong":
                        if (!SendSongToBrowser(ref browserSender, false, true, false))
                            browserSender.Send404Page();
                        return;

                    // MANUAL RECORDING ======================================================
                    case "recordmanual":
                        foo = TryManualRecording();
                        break;

                        // Remote Control
                    case "remotecontrol":
                        foo = ViewRemoteControl();
                        break;

                    // Remote Control
                    case "rc":
                        bool haveSentHTMLPage = SendRemoteControlCommand(ref browserSender);
                        if (haveSentHTMLPage) return;  // Don't continue; this method sends a blank page
                        break;

                    // RECORD A SERIES
                    case "recordshow_series":
                        foo = RecordSeries();
                        break;

                    // RECORD (FROM RecordingRequest): MULTIPURPOSE
                    case "recordfromqueue":
                        foo = RecordFromQueue();
                        break;

                        // PICS
                    case "browsepics":
                        ViewPicturesLibrary();
                        break;

                    case "viewpic":
                        foo = ViewPicture(ref browserSender, ref sentWholePage);
                        if (sentWholePage) return; // no more processing required
                        break;

                    case "picfolderaszip":
                        foo = GetPicturesFolderAsZip(ref browserSender);
                        return; // Don't continue, no Reponse left to output

                    // VIDEOS
                    case "browsevideos":
                        ViewVideoLibrary();
                        break;

                    case "streamvideo":
                        foo = StreamVideo();
                        break;

                    // MUSIC
                    case "musicroot":
                        ViewMusic();
                        break;

                    case "musicartists":
                        ViewMusicArtists(false);
                        break;

                    case "musicartist":
                        ViewMusicArtist();
                        break;

                    case "musicalbumartists":
                        ViewMusicArtists(true);
                        break;

                    case "musicalbums":
                        ViewMusicAlbums();
                        break;

                    case "musicalbum":
                        ViewMusicAlbum();
                        break;

                    case "musicgenres":
                        ViewMusicGenres();
                        break;

                    case "musicgenre":
                        ViewMusicGenre();
                        break;

                    case "musicsong":
                        ViewMusicSong();
                        break;

                    // LIST RECORDINGS
                    case "scheduledrecordings":
                        foo = ViewScheduledRecordings();
                        break;

                    case "log-out":
                        DoLogOut();
                        break;

                    // LIST RECORDINGS
                    case "recordedtv":
                        foo = ViewRecordedTVList();
                        AdditionalStyles.Add("rectv");
                        break;

                    // VIEW A SPECIFIC SERIES
                    case "viewseriesrequest":
                        foo = ViewSeriesRequest();
                        AdditionalStyles.Add("showdetails");
                        break;

                    // MANAGE ALL SERIES
                    case "viewseriesrequests":
                        foo = ViewSeriesRequests();
                        break;

                    // VIEW AN EPG PAGE
                    case "viewepglist":
                        foo = ViewEPGList();
                        AdditionalStyles.Add("epg-list");
                        break;

                    // VIEW AN EPG PAGE - GRID
                    case "viewepggrid":
                        Functions.WriteLineToLogFile("RP: (VEPG)");
                        foo = ViewEPGGrid();
                        AdditionalStyles.Add("epg-grid");
                        break;

                    // Shift EPG Grid Up
                    case "epgnavup":
                        foo = EPGGridChannelRetreat();
                        AdditionalStyles.Add("epg-grid");
                        break;

                    // Shift EPG Grid Down
                    case "epgnavdown":
                        foo = EPGGridChannelAdvance();
                        AdditionalStyles.Add("epg-grid");
                        break;

                    // Shift EPG Grid Right
                    case "epgnavright":
                        foo = EPGGridTimeWindowShiftByMinutes(EPGManager.TimespanMinutes);
                        AdditionalStyles.Add("epg-grid");
                        break;

                    // Shift EPG Grid Left
                    case "epgnavleft":
                        foo = EPGGridTimeWindowShiftByMinutes(0 - EPGManager.TimespanMinutes);
                        AdditionalStyles.Add("epg-grid");
                        break;

                    // Shift EPG Grid Left
                    case "epgnavtop":
                        foo = EPGGridChannelSetAbsolute(true, false);
                        AdditionalStyles.Add("epg-grid");
                        break;

                    // Shift EPG Grid Left
                    case "epgnavbottom":
                        foo = EPGGridChannelSetAbsolute(false, true);
                        AdditionalStyles.Add("epg-grid");
                        break;

                    // Shift EPG To Page
                    case "epgjumptopage":
                        foo = EPGGridChannelJump();
                        AdditionalStyles.Add("epg-grid");
                        break;


                    // VIEW AN EPG SHOW
                    case "viewepgprogramme":
                        foo = ViewEPGProgramme();
                        AdditionalStyles.Add("showdetails");
                        break;

                    // STREAM A SHOW
                    case "streamprogramme":
                        foo = StreamRecordedProgramme();
                        AdditionalStyles.Add("showdetails");
                        break;


                    // SEARCH BY TITLE
                    case "searchbytitle":
                        foo = SearchShowsByText();
                        break;

                    // DELETE A RECORDING
                    case "deletefile":
                        foo = DeleteFileFromFilePath(false);
                        break;

                    case "deletefile64":
                        foo = DeleteFileFromFilePath(true);
                        break;

                    // CANCEL A RECORDING
                    case "cancelseriesrequest":
                        foo = CancelRequest();
                        break;

                    // CANCEL A RECORDING
                    case "cancelrecording":
                        foo = CancelRecording();
                        break;


                    // VIEW MOVIES
                    case "movies":
                        foo = ViewMovies();
                        AdditionalStyles.Add("movies");
                        break;

                    // VIEW MOVIES
                    case "viewmovie":
                        foo = ViewMovie();
                        AdditionalStyles.Add("showdetails");
                        AdditionalStyles.Add("movies");
                        break;

                    case "info":
                        txtResponse += "This is the Remote Potato Server v" + Functions.VersionText + " running on " + Environment.OSVersion.VersionString + ".";
                        txtResponse += "<br/><br/>For help and support please visit the <a href='http://forums.fatattitude.com'>FatAttitude Forums</a>.";
                        break;

                    case "mainmenu":
                        ShowMainMenu();
                        break;

                    default:
                        ShowMainMenu();
                        break;

                }
            }

            // Finalise response: convert to master page
            string txtOutputPage = FileCache.ReadSkinTextFile("masterpage.htm");

            // Commit response
            txtOutputPage = txtOutputPage.Replace("**PAGECONTENT**", txtResponse);
            txtResponse = "";

            // Style inclusion?  (this line must be before the Skin section, as the returned string includes **SKINFOLDER** to be replaced
            txtOutputPage = txtOutputPage.Replace("**PAGEADDITIONALSTYLES**", AdditionalStyleLinks(AdditionalStyles));
            // Orientation
            txtOutputPage = txtOutputPage.Replace("**PAGEORIENTATION**", txtOutputPage.Contains("PAGEORIENTATION=LANDSCAPE") ? "landscape" : "portrait");

            // Skin
            txtOutputPage = txtOutputPage.Replace("**SKINFOLDER**", "/static/skins/" + Themes.ActiveThemeName);
            txtOutputPage = txtOutputPage.Replace("**HEADER**", "Remote Potato");
            // Default Page Title
            txtOutputPage = txtOutputPage.Replace("**PAGETITLE**", txtPageTitle);


            // Copyright / Timestamp 
            txtOutputPage = txtOutputPage.Replace("**TIMEANDVERSIONSTRING**", DateTime.Now.ToLongTimeString() + ", v" + Functions.VersionText);

            if (!browserSender.SendNormalHTMLPageToBrowser(txtOutputPage))
            {
                spoolMessage("Webserver failed to send data.");
            }

#if !DEBUG
            }

            catch (Exception e)
            {
                Functions.WriteExceptionToLogFile(e);
                spoolMessage("EXCEPTION OCCURRED: " + e.Message);

                BrowserSender exceptionBrowserSender = new BrowserSender(Context);
                exceptionBrowserSender.SendNormalHTMLPageToBrowser("<h1>Remote Potato Error</h1>An error occurred and remote potato was unable to serve this web page.<br /><br />Check the debug logs for more information.");                
            }
#endif
            
        }

        /// <summary>
        /// Check for a request header token and flag if it has authenticated successfully
        /// </summary>
        /// <param name="RequestHeaders"></param>
        /// <returns>Only returns false if an invalid token is supplied, returns true for authenticated or no token</returns>
        private bool CheckForTokenAuthentication()
        {
            if (!Settings.Default.RequirePassword)
            {
                AuthenticatedByToken = true;  // force authentication as no username/password is required
                return true;
            }

            if (! Request.QueryString.HasParameter("token")) return true;


            if (AuthSessionHelper.Default.AuthenticateToken(Request.QueryString["token"], currentClientIP))
            {
                AuthenticatedByToken = true;
                return true;
            }
            else
                return false;  // Returns false for invalid token

        }
        #endregion

        #region Task Processors - XML / Web Service
        string preProcessActionString(string action)
        {
            // There should be no + signs; if there are, re-URL-encode them
            action = action.Replace("+", "%2B");

            return action;
        }
        private void WebServiceProcess(string action, ref BrowserSender browserSender, ref string PostObjects)
        {
            if (Settings.Default.DebugFullAPI)
            {
                Functions.WriteLineToLogFile("\r\n\r\nAPI: Incoming API Call: " + action);
                if (PostObjects.Length > 0)
                    Functions.WriteLineToLogFile("API: Post Object String: [" + PostObjects + "]");
                else
                    Functions.WriteLineToLogFile("API: Post Object String: Blank.");
            }

            try
            {

                // Pre-process, e.g. to correct '%2B replaced by + sign' bug
                action = preProcessActionString(action);

                // LOGIN - GET TOKEN
                if (action.StartsWith("xml/login"))
                {
                    if (
                        (!qsParams.HasParameter("un")) &&
                        (!qsParams.HasParameter("hashedpw") || (!qsParams.HasParameter("pw")))
                        )
                    {
                        // not enough params
                        XMLresponse = "<?xml version=\"1.0\"?><loginresponse result=\"NOT_ENOUGH_PARAMETERS\" />";
                        browserSender.SendXMLToBrowser(XMLresponse);
                        return;
                    }

                    string UN = "", PW = "", HPW = "", client = "";
                    bool passwordIsHashed = qsParams.HasParameter("hashedpw");

                    // Client (optional)
                    if (qsParams.HasParameter("client"))
                    {
                        client = qsParams["client"].Trim();
                        client = HttpUtility.UrlDecode(client);
                    }

                    UN = qsParams["un"].Trim();
                    UN = HttpUtility.UrlDecode(UN);
                    if (passwordIsHashed)
                    {
                        HPW = qsParams["hashedpw"].Trim();
                        HPW = HttpUtility.UrlDecode(HPW);
                    }
                    else
                    {
                        PW = qsParams["pw"].Trim();
                        PW = HttpUtility.UrlDecode(PW);
                    }

                    if (action.StartsWith("xml/login64"))
                    {
                        UN = Functions.DecodeFromBase64(UN);
                        if (passwordIsHashed)
                            HPW = Functions.DecodeFromBase64(PW);
                        else
                            PW = Functions.DecodeFromBase64(PW);

                        client = Functions.DecodeFromBase64(client);
                    }

                    if ((!UN.Equals(Settings.Default.UserName)) ||
                        (!Functions.StringHashesToPasswordHash(PW, passwordIsHashed))
                        )
                    {
                        // incorrect credentials - always log
                        Functions.WriteLineToLogFile("API: Failed login attempt from client " + client + " with username " + UN);

                        XMLresponse = "<?xml version=\"1.0\"?><loginresponse result=\"INCORRECT_CREDENTIALS\" />";
                        browserSender.SendXMLToBrowser(XMLresponse);
                        return;
                    }

                    // Success!
                    if (Settings.Default.DebugBasic)
                        Functions.WriteLineToLogFile("API: User " + UN + " logged in OK using client " + client);

                    string token = AuthSessionHelper.Default.AddClient(currentClientIP);  // Store session, get token
                    XMLresponse = "<?xml version=\"1.0\"?><loginresponse result=\"OK\" token=\"" + token + "\" />";
                    browserSender.SendXMLToBrowser(XMLresponse);
                    return;
                }


                // ************ REMAINING METHODS MAY REQUIRE AUTHENTICATION
                if (!AuthenticatedByToken)
                {
                    Functions.logAPIoutputString("ASending 403 Incorrect Authentication");
                    browserSender.SendGenericStatusCodePage("403", "Incorrect authentication");
                    spoolMessage("API: Must provide a valid authentication token.  Use /xml/login first.");
                    return;
                }

                // Should we zip up afterwards
                bool zipContent = false;
                if (action.EndsWith("/zip"))
                {
                    zipContent = true;
                    action = action.Replace("/zip", "");
                }
                if (txtActionOriginalCase.EndsWith("/zip"))
                {
                    txtActionOriginalCase = txtActionOriginalCase.Replace("/zip", "");
                }


                if (action.StartsWith("xml/channels/setasfavorite/"))
                {
                    string chanID = action.Replace("xml/channels/setasfavorite/", "");

                    string strResponse = (EPGManager.MakeChannelFavorite(chanID)) ? "OK" : "ERROR";
                    XMLresponse = XMLHelper.XMLReponseWithOutputString(strResponse);
                    if (Settings.Default.DebugChannels) Functions.WriteLineToLogFile("API: Set channel " + chanID + " as favorite OK.");
                }
                else if (action.StartsWith("xml/channels/unsetasfavorite/"))
                {
                    string chanID = action.Replace("xml/channels/unsetasfavorite/", "");

                    string strResponse = (EPGManager.MakeChannelNotFavorite(chanID)) ? "OK" : "ERROR";
                    XMLresponse = XMLHelper.XMLReponseWithOutputString(strResponse);
                    if (Settings.Default.DebugChannels) Functions.WriteLineToLogFile("API: Unset channel " + chanID + " as not favorite OK.");
                }
                else if (action == "xml/channels/all")
                {
                    // New 2011:  Update channels first from media center (MediaCenter can randomly change internal IDs)
                    EPGManager.UpdateTVChannels();

                    XMLresponse = EPGExporter.AllChannelsAsXML();
                    if (Settings.Default.DebugAdvanced) Functions.WriteLineToLogFile("Exporting all channels via XML web request...");
                }
                else if (action.StartsWith("xml/programmes"))
                {
                    string subAction = action.Replace("xml/programmes/", "");

                    // No description?  (turbo mode)
                    bool omitDescriptions = (subAction.StartsWith("nodescription"));
                    if (omitDescriptions)
                    {
                        subAction = subAction.Replace("nodescription/", "");
                    }

                    // If a programme type is specified, restrict to this type, otherwise use all
                    TVProgrammeType restrictProgrammesToType = (qsParams.HasParameter("programmetype")) ?
                        (TVProgrammeType)Enum.Parse(new TVProgrammeType().GetType(), qsParams["programmetype"], true) :
                        TVProgrammeType.All;


                    // 2. CHANNELS
                    List<string> TVServiceIDs = new List<string>();
                    if (subAction.StartsWith("limitchannels/"))
                    {
                        subAction = subAction.Replace("limitchannels/", "");
                        TVServiceIDs = Functions.StringListFromXML(PostObjects);
                    }
                    else if (subAction.StartsWith("favoritechannels/"))
                    {
                        subAction = subAction.Replace("favoritechannels/", "");
                        TVServiceIDs = EPGManager.EPGDisplayedTVChannelsServiceIDs;
                    }
                    else if (subAction.StartsWith("byepgrequest"))
                    {

                        List<EPGRequest> EPGRequests = EPGRequest.ArrayFromXML(PostObjects);

                        try
                        {
                            XMLresponse = EPGExporter.EPGwithEPGRequests(EPGRequests, omitDescriptions, restrictProgrammesToType);
                            if (Settings.Default.DebugAdvanced) Functions.WriteLineToLogFile("Exporting tv programmes (using " + EPGRequests.Count.ToString() + " epg requests) via XML web request...");
                        }
                        catch (Exception ex)
                        {
                            Functions.WriteLineToLogFile("CRITICAL ERROR - could not parse array of EPG requests.  Date passed was " + PostObjects);
                            Functions.WriteExceptionToLogFile(ex);
                        }
                    }

                    // 3. DATE / DAYRANGE
                    if (subAction.StartsWith("date/"))
                    {
                        string strDate = subAction.Replace("date/", "");
                        DateTime localDate = new DateTime();
                        if (DateTime.TryParse(strDate, out localDate))
                        {

                            XMLresponse = EPGExporter.EPGForLocalDate(localDate, TVServiceIDs, omitDescriptions, restrictProgrammesToType);
                            if (Settings.Default.DebugAdvanced) Functions.WriteLineToLogFile("Exporting tv programmes on " + TVServiceIDs.Count.ToString() + " channels for date " + strDate + " via XML web request...");
                        }
                        else
                        {
                            Functions.WriteLineToLogFile("CRITICAL ERROR - could not parse local date to export shows.  Date passed was " + strDate + ".");
                        }
                    }
                    else if (subAction.StartsWith("daterange/"))
                    {
                        string strDate = subAction.Replace("daterange/", "");
                        string[] dateRanges = strDate.Split(new string[] { "/" }, StringSplitOptions.None);
                        if (dateRanges.Count() > 1)
                        {
                            DateTime startDateTime, endDateTime;
                            if (DateTime.TryParse(dateRanges[0], out startDateTime) &&
                                DateTime.TryParse(dateRanges[1], out endDateTime)
                                )
                            {
                                if (Settings.Default.DebugAdvanced) Functions.WriteLineToLogFile("Exporting tv programmes on " + TVServiceIDs.Count.ToString() + " channels for date range " + strDate + " via XML web request...");
                                XMLresponse = EPGExporter.EPGForDateRange(startDateTime, endDateTime, TVServiceIDs, omitDescriptions, restrictProgrammesToType);
                            }
                            else
                            {
                                Functions.WriteLineToLogFile("CRITICAL ERROR - could not parse day ranges.");
                            }
                        }
                    }
                    else if (subAction.StartsWith("dayrange/"))
                    {
                        string strDate = subAction.Replace("dayrange/", "");

                        string[] dayRanges = strDate.Split(new string[] { "/" }, StringSplitOptions.None);
                        if (dayRanges.Count() > 1)
                        {
                            int startDaysAhead, numberOfDays;
                            if (int.TryParse(dayRanges[0], out startDaysAhead) &&
                                int.TryParse(dayRanges[1], out numberOfDays)
                                )
                            {
                                if (Settings.Default.DebugAdvanced) Functions.WriteLineToLogFile("Exporting tv programmes on " + TVServiceIDs.Count.ToString() + " channels for day range " + strDate + " via XML web request...");
                                XMLresponse = EPGExporter.EPGForDaysRange(startDaysAhead, numberOfDays, TVServiceIDs, omitDescriptions, restrictProgrammesToType);
                            }
                            else
                            {
                                Functions.WriteLineToLogFile("CRITICAL ERROR - could not parse day ranges.");
                            }
                        }
                    }
                    else if (subAction.StartsWith("search"))
                    {
                        EPGSearch theSearch = EPGSearch.FromXML(PostObjects);
                        if (theSearch != null)
                        {
                            XMLresponse = EPGExporter.TVProgrammesMatchingSearch(theSearch);

                            if (Settings.Default.DebugAdvanced) Functions.WriteLineToLogFile("Exporting all programmes matching search.");
                        }
                        else
                        {
                            Functions.WriteLineToLogFile("CRITICAL ERROR - could not parse search request XML");
                        }
                    }
                }
                else if (action.StartsWith("xml/programme/getinfo/"))
                {
                    string strUID = action.Replace("xml/programme/getinfo/", "");
                    if (Settings.Default.DebugAdvanced) Functions.WriteLineToLogFile("Exporting programme info blob for prog ID " + strUID + " via XML web request...");
                    XMLresponse = EPGExporter.TVProgrammeInfoBlobForProgID(strUID);
                }
                else if (action.StartsWith("xml/filebrowse/dir"))
                {
                    FileBrowseRequest fbrequest = FileBrowseRequest.FromXML(PostObjects);
                    if (fbrequest != null)
                    {
                        XMLresponse = FileBrowseExporter.FileBrowseUsingRequestAsXML(fbrequest);

                        if (Settings.Default.DebugAdvanced) Functions.WriteLineToLogFile("Exporting list of files matching request (path " + fbrequest.FullPath + ")");
                    }
                    else
                    {
                        Functions.WriteLineToLogFile("CRITICAL ERROR - could not parse file browse request XML");
                    }
                }
                else if (action.StartsWith("xml/recordings"))
                {
                    // FOR NOW.. ..refresh each time, although strictly may not be necessary
                    if (Settings.Default.RecordingsRetrieveAsParanoid) EPGManager.ReloadAllRecordings();

                    XMLresponse = EPGExporter.RecordingsBlobAsXML();
                    if (Settings.Default.DebugAdvanced) Functions.WriteLineToLogFile("Exporting all recordings via XML web request...");
                }
                else if (action.StartsWith("xml/settings"))
                {
                    XMLresponse = EPGExporter.AllSettingsAsXML();
                    if (Settings.Default.DebugAdvanced) Functions.WriteLineToLogFile("Exporting all settings via XML web request...");
                }
                else if (action.StartsWith("xml/record/byrecordingrequest"))
                {
                    RecordingRequest tReq = RecordingRequest.FromXML(PostObjects);

                    XMLresponse = EPGManager.ScheduleRecording(RecordingRequest.FromXML(PostObjects)).ToXML();
                }
                else if (action.StartsWith("xml/recordedtv"))
                {
                    /*if (action.Contains("refreshnow"))
                        RecTV.Default.RefreshCache(); */

                    XMLresponse = EPGExporter.AllRecordedTVAsXML();
                    if (Settings.Default.DebugAdvanced) Functions.WriteLineToLogFile("Exporting all recorded TV via XML web request...");
                }
                else if (action.StartsWith("xml/cancelrequest/"))
                {
                    string txtID = action.Replace("xml/cancelrequest/", "");
                    XMLresponse = WebSvcCancelRequest(txtID);
                }
                else if (action.StartsWith("xml/sendremotekey/"))
                {
                    string txtCmd = action.Replace("xml/sendremotekey/", "");

                    string strResponse = IRCommunicator.Default.SendIRCommand(txtCmd); // Returns OK or HELPER_NOT_RUNNING.  Doesn't return socket errors (it's ASync)
                    XMLresponse = XMLHelper.XMLReponseWithOutputString(strResponse);
                }
                else if (action.StartsWith("xml/showlog"))
                {
                    XMLresponse = "<?xml version=\"1.0\"?><log ";
                    if (Settings.Default.AllowRemoteLogRetrieval)
                    {
                        XMLresponse += "result=\"OK\" contents=\"" + FileCache.ReadTextFileFromDisk(Functions.DebugLogFileFN) + "\">";
                    }
                    else
                    {
                        XMLresponse += "result=\"Error\" contents=\"RemoteLoggingDisabled\">";
                    }

                    XMLresponse += "</log>";
                }
                else if (action.StartsWith("xml/cancelrecording/"))
                {
                    string txtID = action.Replace("xml/cancelrecording/", "");
                    XMLresponse = WebSvcCancelRecording(txtID);
                }
                else if (action.StartsWith("xml/deletefile64"))
                {
                    string filePath = "";
                    if (PostObjects.Trim().Length > 0)
                    {
                        filePath = HttpUtility.UrlDecode(PostObjects);
                        filePath = Functions.DecodeFromBase64(filePath, Encoding.UTF8);
                    }

                    XMLresponse = WebSvcDeleteFileByFilePath(filePath);
                }
                else if (action.StartsWith("xml/deletefile"))
                {
                    string filePath = "";
                    if (PostObjects.Trim().Length > 0)
                        filePath = HttpUtility.UrlDecode(PostObjects);
                    else
                    {
                        // LEGACY - use URL path
                        filePath = action.Replace("xml/deletefile/", "");
                    }

                    XMLresponse = WebSvcDeleteFileByFilePath(filePath);
                }
                else if (action.StartsWith("xml/mediastream/start/bymediastreamingrequest"))
                {
                    MediaStreamingResult streamResult;
                    MediaStreamingRequest streamRq = XMLHelper.Deserialize<MediaStreamingRequest>(PostObjects);

                    if (streamRq != null)
                        streamResult = StreamingManager.Default.StartStreamer(streamRq, Request.UserHostName);
                    else
                        streamResult = new MediaStreamingResult(MediaStreamingResultCodes.NamedError, "Error in streaming request.");

                    XMLresponse = XMLHelper.Serialize<MediaStreamingResult>(streamResult);
                }
                else if (action.StartsWith("xml/mediastream/probe/byfilename"))
                {
                    string strFileName = HttpUtility.UrlDecode(PostObjects);
                    strFileName = strFileName.Trim();

                    if (action.StartsWith("xml/mediastream/probe/byfilename64"))
                        strFileName = Functions.DecodeFromBase64(strFileName);

                    List<AVStream> result;
                    if (strFileName.Length > 0)
                        result = StreamingManager.Default.ProbeFile(strFileName);
                    else
                        result = new List<AVStream>();

                    if (Settings.Default.DebugStreaming)
                        Functions.WriteLineToLogFile("Probed file " + strFileName + ": sending back details of " + result.Count.ToString() + " AV streams.");

                    XMLresponse = XMLHelper.Serialize<List<AVStream>>(result);
                }
                else if (action.StartsWith("xml/mediastream/keepalive/"))
                {
                    string strStreamerID = action.Replace("xml/mediastream/keepalive/", "");
                    int streamerID;
                    string strmStatus;
                    if (int.TryParse(strStreamerID, out streamerID))
                    {
                        strmStatus = StreamingManager.Default.KeepStreamerAliveAndReturnStatus(streamerID);
                    }
                    else
                    {
                        Functions.WriteLineToLogFile("Warning: Could not parse streamer ID " + strStreamerID);
                        strmStatus = "invalid_id";
                    }

                    //XMLresponse = XMLHelper.XMLReponseWithOutputString(strmStatus);
                    XMLresponse = strmStatus;
                    if (Settings.Default.DebugAdvanced)
                        Functions.WriteLineToLogFile("MediaStreaming: GetStatus  (" + strStreamerID + "): " + strmStatus);
                }
                else if (action.StartsWith("xml/mediastream/stop/"))
                {
                    string strStreamerID = action.Replace("xml/mediastream/stop/", "");
                    int streamerID;
                    try
                    {
                        if (int.TryParse(strStreamerID, out streamerID))
                        {
                            if (!StreamingManager.Default.StopStreamer(streamerID))
                                Functions.WriteLineToLogFile("Warning: Could not stop streamer ID " + strStreamerID);
                        }
                        else
                        {
                            Functions.WriteLineToLogFile("Warning: Could not parse streamer ID " + strStreamerID);
                        }
                    }
                    catch (Exception ex)
                    {
                        Functions.WriteExceptionToLogFileIfAdvanced(ex);
                    }

                    XMLresponse = XMLHelper.XMLReponseWithOutputString("No Data");
                }
                else if (action.StartsWith("xml/stream/start"))
                {
                    WTVStreamingVideoResult streamResult;
                    WTVStreamingVideoRequest streamRq = XMLHelper.Deserialize<WTVStreamingVideoRequest>(PostObjects);
                    if (streamRq == null)
                    {
                        streamResult = new WTVStreamingVideoResult(DSStreamResultCodes.ErrorInStreamRequest);
                    }
                    else
                    {
                        try
                        {
                            streamResult = DSStreamingManager.Default.StartStreamer(streamRq);
                        }
                        catch (Exception e)
                        {
                            Functions.WriteLineToLogFile("Exception setting up streaming object:");
                            Functions.WriteExceptionToLogFile(e);
                            streamResult = new WTVStreamingVideoResult(DSStreamResultCodes.ErrorExceptionOccurred, e.Message);
                        }
                    }

                    XMLresponse = XMLHelper.Serialize<WTVStreamingVideoResult>(streamResult);
                }
                else if (action.StartsWith("xml/stream/stop"))
                {
                    XMLresponse = XMLHelper.XMLReponseWithOutputString("ERROR (DEPRECATED)");
                }
                else if (action.StartsWith("xml/picture/thumbnailzip/"))
                {
                    FileBrowseRequest fbrequest = FileBrowseRequest.FromXML(PostObjects);
                    if (fbrequest != null)
                    {
                        if (PictureExporter.SendThumbnailsAsZipFile(fbrequest, FatAttitude.ThumbnailSizes.Small, ref browserSender))
                            return;
                        else
                        {
                            if (Settings.Default.DebugAdvanced) Functions.WriteLineToLogFile("Could not export zip of thumbnails.");
                            browserSender.Send404Page();
                        }
                    }
                    else
                    {
                        Functions.WriteLineToLogFile("ThumbnailZip: Error in requestprocessor- could not parse file browse request XML");
                    }
                }
                else if (action.StartsWith("xml/picture/get"))  // matches /xml/picture/getrequest too
                {
                    string strSubAction = txtActionOriginalCase.Replace("xml/picture/", "");

                    // URL DECODE
                    strSubAction = HttpUtility.UrlDecode(strSubAction);

                    string strParams;
                    string strFileName = "";
                    bool fileNameAtEndOfUri = false;
                    bool fileNameAtEndOfUriIsBase64Encoded = false;
                    if (strSubAction.StartsWith("getwithfilename"))  // GET FILENAME FROM URL
                    {
                        fileNameAtEndOfUri = true;

                        fileNameAtEndOfUriIsBase64Encoded = strSubAction.StartsWith("getwithfilename64");

                        if (fileNameAtEndOfUriIsBase64Encoded)
                            strParams = strSubAction.Replace("getwithfilename64/", "");
                        else
                            strParams = strSubAction.Replace("getwithfilename/", "");
                    }
                    else
                    // GET FILENAME FROM POST STRING
                    {
                        fileNameAtEndOfUri = false;
                        strFileName = PostObjects;
                        strParams = strSubAction.Replace("get/", "");
                    }

                    string[] Params = strParams.Split(new string[] { "/" }, StringSplitOptions.None);

                    bool haveFrameSizes = false;
                    int frameWidth = 0;
                    int frameHeight = 0;
                    if (Params.Count() > 1)
                    {
                        if ((int.TryParse(Params[0], out frameWidth) &&
                                 int.TryParse(Params[1], out frameHeight)
                                ))
                            haveFrameSizes = ((frameWidth > 0) && (frameHeight > 0));
                        else
                            haveFrameSizes = false;
                    }
                    else
                        // Send full picture
                        haveFrameSizes = false;

                    if (!haveFrameSizes) Functions.WriteLineToLogFile("Xml/Picture: invalid frame size (or none supplied): using full picture");

                    if (Settings.Default.DebugAdvanced) Functions.WriteLineToLogFile("Exporting picture " + strFileName + " resized to frame " + frameWidth.ToString() + "x" + frameHeight.ToString());


                    // Get Filename if not got already
                    // File name from Uri ?
                    if (fileNameAtEndOfUri)
                    {
                        if (fileNameAtEndOfUriIsBase64Encoded)
                        {
                            // Take final component and un-encode to produce filename
                            strFileName = Params[Params.Count() - 1];
                            strFileName = HttpUtility.UrlDecode(strFileName);
                            strFileName = Functions.DecodeFromBase64(strFileName);
                        }
                        else
                        {
                            // Reconstruct filename by putting /slashed/ components back together
                            for (int pCount = 2; pCount < Params.Count(); pCount++)
                            {
                                strFileName = strFileName + Params[pCount];
                                if (pCount < (Params.Count() - 1))
                                    strFileName = strFileName + "/";

                                strFileName = HttpUtility.UrlDecode(strFileName);
                            }
                        }


                    }

                    if (string.IsNullOrEmpty(strFileName))
                    {
                        Functions.WriteLineToLogFile("Xml/Picture : No filename specified in POST request, sending 404");
                        browserSender.Send404Page();
                        return;
                    }

                    // Send
                    if (haveFrameSizes)
                    {

                        byte[] resizedPictureData = new byte[] { };
                        if (ImageResizer.ResizePicture(strFileName, new Size(frameWidth, frameHeight), out resizedPictureData, ImageFormat.Jpeg, false))
                        {
                            browserSender.SendDataToBrowser(Functions.MimeTypeForFileName(strFileName), resizedPictureData);
                            return;
                        }
                        else
                        {
                            Functions.WriteLineToLogFile("Xml/Picture: Could not resize picture.");
                            browserSender.Send404Page();
                            return;
                        }
                    }
                    else  // No frame sizes, send full image
                    {
                        browserSender.SendFileToBrowser(strFileName);
                        return;
                    }


                }
                else if (action.StartsWith("xml/music/framework"))
                {
                    using (WMPManager manager = new WMPManager())
                    {
                        XMLresponse = manager.MusicFrameworkAsXML();
                        if (Settings.Default.DebugAdvanced) Functions.WriteLineToLogFile("Exporting music framework blob via XML web request...");
                    }
                }
                else if (action.StartsWith("xml/music/songs/artist"))
                {
                    bool isBase64Encoded = (action.Contains("artist64/"));
                    action = action.Replace("artist64", "artist");
                    txtActionOriginalCase = txtActionOriginalCase.Replace("artist64", "artist");

                    string strArtistID = txtActionOriginalCase.Replace("xml/music/songs/artist/", "");
                    strArtistID = HttpUtility.UrlDecode(strArtistID);

                    if (isBase64Encoded)
                        strArtistID = Functions.DecodeFromBase64(strArtistID);

                    using (WMPManager manager = new WMPManager())
                    {
                        XMLresponse = manager.GetSongsForArtistAsXML(strArtistID);
                        if (Settings.Default.DebugAdvanced) Functions.WriteLineToLogFile("Exporting songs for artist " + strArtistID + " via XML web request...");
                    }
                }
                else if (action.StartsWith("xml/music/songs/album"))
                {
                    bool isBase64Encoded = (action.Contains("album64/"));
                    action = action.Replace("album64", "album");
                    txtActionOriginalCase = txtActionOriginalCase.Replace("album64", "album");

                    // USE case sensitive action string for match
                    string strAlbumID = txtActionOriginalCase.Replace("xml/music/songs/album/", "");
                    strAlbumID = HttpUtility.UrlDecode(strAlbumID);

                    if (isBase64Encoded)
                        strAlbumID = Functions.DecodeFromBase64(strAlbumID);

                    using (WMPManager manager = new WMPManager())
                    {
                        XMLresponse = manager.GetSongsForAlbumAsXML(strAlbumID);
                        if (Settings.Default.DebugAdvanced) Functions.WriteLineToLogFile("Exporting songs for album " + strAlbumID + " via XML web request...");
                    }
                }
                else if (action.StartsWith("xml/music/songs/genre"))
                {
                    bool isBase64Encoded = (action.Contains("genre64/"));
                    action = action.Replace("genre64", "genre");
                    txtActionOriginalCase = txtActionOriginalCase.Replace("genre64", "genre");

                    // USE case sensitive action string for match
                    string strGenreID = txtActionOriginalCase.Replace("xml/music/songs/genre/", "");
                    strGenreID = HttpUtility.UrlDecode(strGenreID);

                    if (isBase64Encoded)
                        strGenreID = Functions.DecodeFromBase64(strGenreID);

                    using (WMPManager manager = new WMPManager())
                    {
                        XMLresponse = manager.GetSongsForGenreAsXML(strGenreID);
                        if (Settings.Default.DebugAdvanced) Functions.WriteLineToLogFile("Exporting songs for genre " + strGenreID + " via XML web request...");
                    }
                }
                else if (action.StartsWith("xml/music/songs/all"))
                {

                    using (WMPManager manager = new WMPManager())
                    {
                        XMLresponse = manager.GetAllSongsAsXML();
                        if (Settings.Default.DebugAdvanced) Functions.WriteLineToLogFile("Exporting all songs in library via XML web request...");
                    }
                }
                else if (action.StartsWith("xml/music/songs/checkexists"))
                {
                    bool isBase64Encoded = (action.Contains("checkexists64/"));
                    action = action.Replace("checkexists64", "checkexists");
                    txtActionOriginalCase = txtActionOriginalCase.Replace("checkexists64", "checkexists");

                    // USE case sensitive action string for match
                    string strSongID = txtActionOriginalCase.Replace("xml/music/songs/checkexists/", "");
                    strSongID = HttpUtility.UrlDecode(strSongID);

                    if (isBase64Encoded)
                        strSongID = Functions.DecodeFromBase64(strSongID);

                    using (WMPManager manager = new WMPManager())
                    {
                        XMLresponse = manager.WMPItemFileExistsAsXML(strSongID);
                        if (Settings.Default.DebugAdvanced) Functions.WriteLineToLogFile("Exporting whether song exists... " + XMLresponse);
                    }
                }
                else
                {


                    Functions.WriteLineToLogFile("XML: Unknown request");
                    XMLresponse = "";
                }


                if (String.IsNullOrEmpty(XMLresponse))
                {
                    Functions.WriteLineToLogFile("NULL xmlresponse - nothing to send out to Silverlight client.  Sending error string.");
                    browserSender.SendStringToBrowser(XMLHelper.XMLReponseWithOutputString("No Data"));
                    return;
                }

                // Strip out any UTF-16 encoding
                XMLresponse = Functions.StripIllegalXmlCharacters(XMLresponse);


                // Zip up ?
                if (zipContent)
                {
                    Functions.logAPIoutputString(XMLresponse);
                    XMLresponse = ZipHelper.ZipString(XMLresponse);

                    if (Settings.Default.DebugServer)
                        Functions.WriteLineToLogFile("Zipped String to base64 string.  Length:" + XMLresponse.Length.ToString() + " characters. ");

                    browserSender.SendZipStringToBrowser(XMLresponse);
                }
                else
                    browserSender.SendXMLToBrowser(XMLresponse);
            }
            catch (Exception e)
            {
                string errorResponse = XMLHelper.XMLReponseWithOutputString("An error occurred - " + e.Message + " - check the debug log on the server.");

                Functions.WriteLineToLogFile("Exception while processing XML request:");
                Functions.WriteExceptionToLogFile(e);

                browserSender.SendXMLToBrowser(errorResponse);
            }
        }

        /*DataResponse = ZipHelper.ZipStringToBytes(XMLresponse);
               XMLresponse = null;  // release memory

               if (Settings.Default.DebugServer)
                   Functions.WriteLineToLogFile("Zipped String to bytes.  Length:" + DataResponse.Length.ToString() + " bytes.");

               if (DataResponse == null)
               {
                   XMLresponse = "";  // in case null
                   Functions.WriteLineToLogFile("NULL xmlresponse - nothing to send out to Silverlight client.  Sending empty string.");
                   browserSender.SendXMLToBrowser("");
               }
               else
               {
                   browserSender.SendDataToBrowser("application/zip", DataResponse);
                   DataResponse = null;  // release memory immediately
               } */
        

        private string WebSvcCancelRequest(string ID)
        {
            if (!EPGManager.isWMCOnline)
                return "TV is not configured.";
           
            // Parameters
            long requestID;
            try { requestID = long.Parse(ID); }
            catch { return "Invalid Request ID"; }

            // Get request with this ID
            RPRequest rpr;
            if (!EPGManager.AllRequests.TryGetValue(requestID, out rpr))
                return "This series or keyword recording no longer exists.";

            try
            {
                if (EPGManager.mcData.CancelRequest(rpr.ID))
                    return "OK";
                else
                    return "FAILED";
            }
            catch (Exception e)
            {
                return "Error - could not cancel recording request: " + e.Message;
            }
        }
        private string 
            WebSvcCancelRecording(string ID)
        {
            if (!EPGManager.isWMCOnline)
                return "TV is not configured.";

            // Parameters
            long recordingid;
            try { recordingid = long.Parse(ID); }
            catch { return "Invalid Recording ID"; }

            // Get request with this ID
            RPRecording rpr;
            if (!EPGManager.AllRecordings.TryGetValue(recordingid, out rpr))
                return "This recording no longer exists.";

            try
            {
                if (EPGManager.mcData.CancelRecording(rpr.Id))
                    return "OK";
                else
                    return "FAILED";
            }
            catch (Exception e)
            {
                return "Error - could not cancel recording: " + e.Message;
            }
        }
        bool fileNameStartsWithAnyRecordedTVFolder(string txtFilename)
        {
            foreach (string recTVFolder in Settings.Default.RecordedTVFolders)
            {
                txtFilename = txtFilename.Replace(@"\",@"/");
                string recTVFolderInv = recTVFolder.Replace(@"\", @"/");

                if (txtFilename.ToLowerInvariant().StartsWith(recTVFolderInv.ToLowerInvariant()))
                    return true;
            }
            return false;
        }
        private string WebSvcDeleteFileByFilePath(string filename)
        {
            filename = HttpUtility.UrlDecode(filename);

            // Replace slashes for compatibility
            // security - must be in rec tv path
            if (! fileNameStartsWithAnyRecordedTVFolder(filename))
            {
                Functions.WriteLineToLogFile("Error - Deletion of '" + filename + "' not in any Recorded TV Folder.");
                return "Error - not in Recorded TV folder";
            }

            try
            {
                FileInfo fi = new FileInfo(filename);
                if (Settings.Default.DebugAdvanced)
                    Functions.WriteLineToLogFile("Deleting file: " + fi);
                fi.Delete();

            }
            catch (Exception e)
            {
                return "Error - could not delete programme: " + e.Message;
            }

            return "OK";
        }

        private void ProcessHTTPLSURL(string txtAction, ref BrowserSender browserSender)
        {
            // GET STREAMER ID
            int ID;
            if (txtAction.StartsWith("httplivestream/"))
            {
                txtAction = txtAction.Replace("httplivestream/","");

                int nextSlash = txtAction.IndexOf("/");
                if (nextSlash == -1)
                {
                    browserSender.Send404Page();
                    return;
                }

                string strMediaStreamerID = txtAction.Substring(0, nextSlash);
                
                if (! int.TryParse(strMediaStreamerID, out ID))
                {
                    browserSender.Send404Page();
                    return;
                }

                // Make txtAction the final portion
                txtAction = txtAction.Replace(ID.ToString() + "/","");
            }
            else
            {
                browserSender.Send404Page();
                return;
            }

            if (txtAction.EndsWith("m3u8"))
            {
                string indexFile = StreamingManager.Default.IndexFileForStreamer(ID);
                browserSender.SendStringToBrowser(indexFile, "application/vnd.apple.mpegurl");
                return;
            }

            if (txtAction.StartsWith("seg"))
            {
                txtAction = txtAction.Replace(".ts", ""); // remove extension

                // Get segment number
                string strSegNumber;
                List<string> parts = txtAction.Split('-').ToList();
                if (parts.Count > 1)
                    strSegNumber = parts[1];
                else
                {
                    browserSender.Send404Page();
                    return;
                }

                int iSegNumber = 0;
                if (!int.TryParse(strSegNumber, out iSegNumber))
                {
                    browserSender.Send404Page();
                    return;
                }

                byte[] TSdata = new byte[]{};
                string txtError = "";
                if (StreamingManager.Default.SegmentFromStreamer(ID, iSegNumber, ref TSdata, ref txtError))
                {
                    browserSender.SendDataToBrowser("video/mp2t", TSdata);
                    return;
                }
                else
                {
                    Functions.WriteLineToLogFileIfSetting(Settings.Default.DebugStreaming, "Could not get streaming segment number " + strSegNumber + ":" + txtError);
                    browserSender.Send404Page();
                }
            }

        }

        
        #endregion

        #region Task Processors - HTML
        void DoLogOut()
        {
            Cookie cookToken = new Cookie("RemotePotatoToken", "");
            Response.AppendCookie(cookToken);

            txtResponse = "You have successfully logged out of Remote Potato.";
        }
        private bool SendSongToBrowser(ref BrowserSender browserSender, bool isBase64Encoded, bool sendChunked, bool isDownload)
        {
            if (! (qsParams.HasParameter("id"))) return false;

            string songID = HttpUtility.UrlDecode(qsParams["id"]);

            if (isBase64Encoded)
                songID = Functions.DecodeFromBase64(songID);

            string FN;
            using (WMPManager manager = new WMPManager())
            {
                FN = manager.FileNameForWMPItem(songID);
                if (string.IsNullOrEmpty(FN)) return false;
            }

            // Send CHUNKED  (not implemented)
            browserSender.SendFileToBrowser(FN, false, sendChunked, isDownload);
            return true;
        }
        private void GetSongThumbnail(ref BrowserSender browserSender, bool isBase64Encoded)
        {
            // Any parameters?
            bool validated = true;
            validated &= qsParams.HasParameter("id");
            validated &= qsParams.HasParameter("size");

            if (!validated)
            {
                bool foo = browserSender.Send404Page();
                return;
            }

            string itemID = HttpUtility.UrlDecode(qsParams["id"]);
            if (isBase64Encoded)
                itemID = Functions.DecodeFromBase64(itemID);

            string size = qsParams["size"];
            WMPManager.Thumbnail_Sizes tSize = (WMPManager.Thumbnail_Sizes)Enum.Parse(typeof(WMPManager.Thumbnail_Sizes), size, true);

            using (WMPManager picManager = new WMPManager())
            {
                string MimeType = "";
                byte[] picBytes = picManager.ThumbnailForWMPItemAsByte("TrackingID", itemID, true, tSize, out MimeType);
                if ((picBytes == null) || (picBytes.Length < 1))
                    browserSender.Send404Page();
                else
                    browserSender.SendDataToBrowser(MimeType, picBytes);
            }
        }
        private void GetAlbumThumbnail(ref BrowserSender browserSender, bool isBase64Encoded)
        {
            // Any parameters?
            bool validated = true;
            validated &= qsParams.HasParameter("id");
            validated &= qsParams.HasParameter("size");

            if (!validated)
            {
                bool foo = browserSender.Send404Page();
                return;
            }

            
            string itemID = HttpUtility.UrlDecode(qsParams["id"]);
            if (isBase64Encoded)
                itemID = Functions.DecodeFromBase64(itemID);

            string size = qsParams["size"];
            WMPManager.Thumbnail_Sizes tSize = (WMPManager.Thumbnail_Sizes)Enum.Parse(typeof(WMPManager.Thumbnail_Sizes), size, true);

            using (WMPManager picManager = new WMPManager())
            {
                string MimeType = "";
                byte[] picBytes = picManager.ThumbnailForWMPItemAsByte("AlbumID", itemID, true, tSize, out MimeType);
                if ((picBytes == null) || (picBytes.Length < 1))
                    browserSender.Send404Page();
                else
                    browserSender.SendDataToBrowser(MimeType, picBytes);
            }
        }
        private void GetRecTVThumbnail(ref BrowserSender browserSender, bool isBase64Encoded)
        {
            // Any parameters?
            bool validated = true;

            validated &= qsParams.HasParameter("filename");

            if (!validated)
            {
                bool foo = browserSender.Send404Page();
                return;
            }

            // This can fail with non-ASCII
            string qsFN = qsParams["filename"];
            string fileName = HttpUtility.UrlDecode( qsFN );
            if (isBase64Encoded)
                fileName = Functions.DecodeFromBase64(fileName, Encoding.UTF8);  // http uses UTF8 encoding

            // Find file?
            string filePath = "";
            bool foundFilePath = false;
            if (File.Exists(fileName))  // Fully qualified path was passed
            {
                foundFilePath = true;
                filePath = fileName;
            }
            else
            {
                foreach (string recTVFolder in Settings.Default.RecordedTVFolders)
                {
                    filePath = Path.Combine(recTVFolder, fileName);  // If just a filename is provided, then this appends it to the default record path from the registry
                    if (File.Exists(filePath))
                    {
                        foundFilePath = true;
                        break;
                    }
                }
            }

            if (! foundFilePath)
            {
                bool foo = browserSender.Send404Page();
                return;
            }

            
            SendFileThumbnail(filePath, FatAttitude.ThumbnailSizes.Medium, ref browserSender);
            

            
            }        
        private void GetFileThumbnailUsingQueryString(ref BrowserSender browserSender, bool isBase64Encoded)
        {
            // Any parameters?
            bool validated = true;
            validated &= qsParams.HasParameter("filename");
            validated &= qsParams.HasParameter("size");

            if (!validated)
            {
                bool foo = browserSender.Send404Page();
                return;
            }

            string size = qsParams["size"];

            string qsFN = qsParams["filename"];
            string fileName = HttpUtility.UrlDecode( qsFN );
            if (isBase64Encoded)
                fileName = Functions.DecodeFromBase64(fileName, Encoding.UTF8);  // http uses UTF8 encoding
           
            FatAttitude.ThumbnailSizes tSize = (FatAttitude.ThumbnailSizes)Enum.Parse(typeof(FatAttitude.ThumbnailSizes), size, true);
            
            SendFileThumbnail(fileName, tSize, ref browserSender);
        }
        private void SendFileThumbnail(string fileName, FatAttitude.ThumbnailSizes thumbSize, ref BrowserSender browserSender)
        {
            // Find file or folder?
            if (
                (! File.Exists(fileName)) && (! Directory.Exists(fileName)) 
                )
            {
                bool foo = browserSender.Send404Page();
                return;
            }

            FatAttitude.ShellHelper sh = new FatAttitude.ShellHelper();
            string strLog = ""; // ignore
            Bitmap thumb = sh.ThumbnailForFile(fileName, thumbSize, ref strLog);
            if (thumb == null)
            {
                browserSender.SendFileToBrowser("thumbnail_default.png", true, false);
                return;
            }

            byte[] outputdata =  ImageResizer.ImageToByteArray(thumb, ImageFormat.Jpeg);
            
            // Send to browser
            bool foo2 = browserSender.SendDataToBrowser("image/jpeg", outputdata);
        }
        private bool TryManualRecording()
        {
            txtPageTitle = "Record by Time and Date";

            // Not enough parameters - display form
            if ((qsParams.Count) < 2)  // Due to the way QueryString class works, the Count will be 1 even if there are no actual name/value pairs
            {
                // Display Record Form
                string requestForm = FileCache.ReadSkinTextFile("page_recordmanual.htm");
                processRequestForm(ref requestForm);

                txtResponse += requestForm;
                requestForm = null;

                return true;
            }

            RecordingRequest newRR = null;

            bool failedValidation = false;
            string failedValidationReason = "";
            if (
                (!qsParams.HasParameter("channelcallsign")) |
                (!qsParams.HasParameter("datecomponent")) |
                (!qsParams.HasParameter("timecomponent")) |
                (!qsParams.HasParameter("duration"))
                )
            {
                failedValidation = true;
                failedValidationReason = "To schedule a recording, you must provide a channel, start time and duration.";
            }
            else
            {
                // Get parameters
                string serviceID = qsParams["channelcallsign"];  // it's actually the service ID

                // DATE TIME
                string dateTimeString = qsParams["datecomponent"] + " " + qsParams["timecomponent"];
                DateTime tryStartTime = new DateTime();
                if (DateTime.TryParse(dateTimeString, out tryStartTime))
                    tryStartTime = DateTime.SpecifyKind(tryStartTime, DateTimeKind.Local);
                else
                {
                    failedValidation = true; failedValidationReason += "Invalid start time or date.<br>";
                }

                // in the past?
                if (tryStartTime.ToUniversalTime() < DateTime.Now.ToUniversalTime())
                {
                    failedValidation = true; failedValidationReason += "Start time must be in the future.<br>";
                }

                // DURATION
                Int32 tryDuration = 0;
                if (Int32.TryParse(qsParams["duration"], out tryDuration))
                if ((tryDuration == 0) | (tryDuration > 720)) { failedValidation = true; failedValidationReason += "Invalid duration, must be between 1 and 720 minutes.<br>"; }

                
                // Create a new recording request
                newRR = new RecordingRequest(tryStartTime.ToUniversalTime(), long.Parse(serviceID), tryDuration, Settings.Default.DefaultManualRecordingName);
            }

            // Passed validation?
            if (failedValidation)
            {
                txtResponse += "<p class='recorderror'>Error in recording request: " + failedValidationReason + "</p>";
            }
            else
            {
                qsParams.Add("queueid", RecordingQueue.AddToQueue(newRR));
                return RecordFromQueue();
            }


            return true;
        }

        #region Pictures
        private bool ViewPicturesLibrary()
        {
            string path;
            if (qsParams.HasParameter("PATH"))
            {
                string qsFN = qsParams["PATH"];
                path = HttpUtility.UrlDecode(qsFN);
                path = Functions.DecodeFromBase64(path, Encoding.UTF8);  // http uses UTF8 encoding

                txtPageTitle = Path.GetFileNameWithoutExtension(path); // actually folder name
            }
            else
            {
                path = "PICTURES_LIBRARY";
                txtPageTitle = "Picture Library";
            }

            string txtPicTable = FileBrowseExporter.HTMLTableForPicturesLibrary(path, 7); // 7 pics horizontally on each row
            txtPicTable += "<br />";
            HTMLLink linkToZip = new HTMLLink("picfolderaszip?PATH=" + qsParams["PATH"], "Download All as Zip");
            txtPicTable += linkToZip.ToString();

            string txtOutput = FileCache.ReadSkinTextFile("page_viewlibrary.htm");
            txtOutput = txtOutput.Replace("**LIBRARYTABLE**", txtPicTable);

            // Commit to form
            txtResponse += txtOutput;


            return true;
        }
        bool GetPicturesFolderAsZip(ref BrowserSender bSender)
        {
            string path;
            if (qsParams.HasParameter("PATH"))
            {
                string qsFN = qsParams["PATH"];
                path = HttpUtility.UrlDecode(qsFN);
                path = Functions.DecodeFromBase64(path, Encoding.UTF8);  // http uses UTF8 encoding

                FileBrowseResult fbr = FileBrowseExporter.BrowsePath(path, FileBrowseExporter.MediaFileTypes.Image);
                FileBrowseExporter.SendFolderFilesAsZipFile(fbr, ref bSender);
            }
            else
            {
                bSender.Send404Page();
            }

            
            return true;
        }
        private bool ViewPicture(ref BrowserSender bSender, ref bool sentCompletePage)
        {
            string txtOutput = FileCache.ReadSkinTextFile("page_viewpicture.htm");

            StringBuilder sbHTML = new StringBuilder(100);

            string FN = "";
            string strSize = "";
            bool shouldDownload = false;
            if (
                (qsParams.HasParameter("FN")) && (qsParams.HasParameter("SIZE"))
                )
            {
                string qsFN = qsParams["FN"];
                FN = HttpUtility.UrlDecode(qsFN);
                FN = Functions.DecodeFromBase64(FN, Encoding.UTF8);  // http uses UTF8 encoding

                txtPageTitle = Path.GetFileNameWithoutExtension(FN);

                strSize = qsParams["SIZE"];

                shouldDownload = (qsParams.HasParameter("DOWNLOAD"));
            }
            else
            {
                bSender.Send404Page();
                sentCompletePage = true;
            }

            // Thumbnail?
            string imgSrc;
            if (!strSize.Equals("full"))
            {
                // Assemble path to file
                imgSrc = "getfilethumbnail64?filename=" + qsParams["FN"] + "&size=" + strSize;

                HTMLImage image = new HTMLImage(imgSrc, "picturelibraryviewedpicture");

                // Link
                string strLinkToFullImage = "viewpic?FN=" + qsParams["FN"] + "&size=full";
                HTMLLink lnk = new HTMLLink(strLinkToFullImage, image.ToString());
                
                // Commit to form
                sbHTML.Append(lnk.ToString());

                sbHTML.Append("<br /><br />");

                HTMLLink fullLink = new HTMLLink(strLinkToFullImage, "View original size");
                sbHTML.Append(fullLink.ToString());

                sbHTML.Append("&nbsp;&nbsp;|&nbsp;&nbsp;");

                fullLink = new HTMLLink(strLinkToFullImage + "&download=yes", "Download");
                sbHTML.Append(fullLink.ToString());
            }
            else
            {
                sentCompletePage = true;
                bSender.SendFileToBrowser(FN, false, false, shouldDownload);
            }

            txtOutput = txtOutput.Replace("**LIBRARYTABLE**", sbHTML.ToString());

            txtResponse += txtOutput;

            return true;
        }
        #endregion

        #region Pictures
        private bool ViewVideoLibrary()
        {
            string path;
            if (qsParams.HasParameter("PATH"))
            {
                string qsFN = qsParams["PATH"];
                path = HttpUtility.UrlDecode(qsFN);
                path = Functions.DecodeFromBase64(path, Encoding.UTF8);  // http uses UTF8 encoding

                txtPageTitle = Path.GetFileNameWithoutExtension(path); // actually folder name
            }
            else
            {
                path = "VIDEO_LIBRARY";
                txtPageTitle = "Video Library";
            }


            string txtOutput = FileCache.ReadSkinTextFile("page_viewlibrary.htm");

            txtOutput = txtOutput.Replace("**LIBRARYTABLE**", FileBrowseExporter.HTMLTableForVideoLibrary(path, 7)); // 7 columns

            // Commit to form
            txtResponse += txtOutput;


            return true;
        }
        #endregion

        #region Music
        private bool ViewMusic()
        {
            txtPageTitle = "Music Library";

            bool RefreshNow = qsParams.HasParameter("refreshnow");
            /* NOW WE USE FILESYSTEMWATCHER
             * if (RefreshNow)
                RecTV.RefreshCache(); */

            string txtOutput = "";


            txtOutput += "<div id='musicrootmenu'><ul>";
            txtOutput += "<li><a href='musicalbumartists'>Album Artists</a></li>";
            txtOutput += "<li><a href='musicartists'>All Artists</a></li>";
            txtOutput += "<li><a href='musicalbums'>All Albums</a></li>";
            txtOutput += "<li><a href='musicgenres'>All Genres</a></li></ul></div>";



            // Commit to form
            txtResponse += txtOutput;


            return true;


        }
        private bool ViewMusicAlbums()
        {
            txtPageTitle = "All Albums";

            bool RefreshNow = qsParams.HasParameter("refreshnow");
            if (RefreshNow)
                MusicCache.Default.RefreshCache();

            string txtOutput = FileCache.ReadSkinTextFile("page_music_collection.htm");

            StringBuilder sbOutput = new StringBuilder(500);
            StringBuilder sbIndex = new StringBuilder(100);

            MusicCache.Default.CheckInitialised();
            string currentIndexLetter = "";
            foreach (RPMusicAlbum album in MusicCache.Default.Framework.Albums)
            {
                string AlbumName = album.Title;
                if (string.IsNullOrWhiteSpace(AlbumName)) continue;

                string AlbumIndexLetter = IndexLetterForPhrase(AlbumName);

                if (!(AlbumIndexLetter == currentIndexLetter))
                {
                    currentIndexLetter = AlbumIndexLetter;
                    sbIndex.Append(Functions.LinkTagOpen("#" + currentIndexLetter) + AlbumIndexLetter + "</a> ");
                    sbOutput.Append("</div><div class='musiccollectionitemssection'><strong><a id=\"" + currentIndexLetter + "\">" + currentIndexLetter + "</a></strong><br />");
                }

                sbOutput.Append(Functions.LinkTagOpen("musicalbum?id=" + HttpUtility.UrlEncode(Functions.EncodeToBase64(album.ID))));
                sbOutput.Append(album.Title + "</a><br />");
            }
            sbOutput.Append("</div>");  // End of musiccollectionitemssection

            // Commit to form
            txtOutput = txtOutput.Replace("**MUSICCOLLECTIONINDEX**", sbIndex.ToString());
            txtOutput = txtOutput.Replace("**MUSICCOLLECTIONITEMS**", sbOutput.ToString());


            // Commit to response
            txtResponse += txtOutput;
            return true;


        }
        private bool ViewMusicGenres()
        {
            txtPageTitle = "All Genres";

            bool RefreshNow = qsParams.HasParameter("refreshnow");
            if (RefreshNow)
                MusicCache.Default.RefreshCache();

            string txtOutput = FileCache.ReadSkinTextFile("page_music_collection.htm");

            StringBuilder sbOutput = new StringBuilder(500);
            StringBuilder sbIndex = new StringBuilder(100);

            MusicCache.Default.CheckInitialised();
            string currentIndexLetter = "";
            foreach (RPMusicGenre genre in MusicCache.Default.Framework.Genres)
            {
                string GenreName = genre.Name;
                if (string.IsNullOrWhiteSpace(GenreName)) continue;

                string GenreIndexLetter = IndexLetterForPhrase(GenreName);

                if (!(GenreIndexLetter == currentIndexLetter))
                {
                    currentIndexLetter = GenreIndexLetter;
                    sbIndex.Append(Functions.LinkTagOpen("#" + currentIndexLetter) + GenreIndexLetter + "</a> ");
                    sbOutput.Append("</div><div class='musiccollectionitemssection'><strong><a id=\"" + currentIndexLetter + "\">" + currentIndexLetter + "</a></strong><br />");
                }

                sbOutput.Append(Functions.LinkTagOpen("musicgenre?id=" + HttpUtility.UrlEncode(Functions.EncodeToBase64(genre.ID))));
                sbOutput.Append(genre.Name + "</a><br />");
            }
            sbOutput.Append("</div>");  // End of musiccollectionitemssection

            // Commit to form
            txtOutput = txtOutput.Replace("**MUSICCOLLECTIONINDEX**", sbIndex.ToString());
            txtOutput = txtOutput.Replace("**MUSICCOLLECTIONITEMS**", sbOutput.ToString());


            // Commit to response
            txtResponse += txtOutput;

            return true;


        }
        private bool ViewMusicArtists(bool limitToAlbumArtists)
        {
            txtPageTitle = (limitToAlbumArtists) ? "Album Artists" : "All Artists";

            bool RefreshNow = qsParams.HasParameter("refreshnow");
            if (RefreshNow)
                MusicCache.Default.RefreshCache();

            string txtOutput = FileCache.ReadSkinTextFile("page_music_collection.htm");

            StringBuilder sbOutput = new StringBuilder(500);
            StringBuilder sbIndex = new StringBuilder(100);

            MusicCache.Default.CheckInitialised();
            string currentIndexLetter = "";
            foreach (RPMusicArtist artist in MusicCache.Default.Framework.Artists)
            {
                if (limitToAlbumArtists)
                    if (artist.Albums().Count < 1) continue;

                string ArtistName = artist.Name;
                if (string.IsNullOrWhiteSpace(ArtistName)) continue;
                string ArtistIndexLetter = IndexLetterForPhrase(ArtistName);

                if (!(ArtistIndexLetter == currentIndexLetter))
                {
                    currentIndexLetter = ArtistIndexLetter;

                    sbIndex.Append(Functions.LinkTagOpen("#" + currentIndexLetter) + ArtistIndexLetter + "</a> ");
                    sbOutput.Append("</div><div class='musiccollectionitemssection'><strong><a id=\"" + currentIndexLetter + "\">" + currentIndexLetter + "</a></strong><br />");
                }

                sbOutput.Append(Functions.LinkTagOpen("musicartist?id=" + HttpUtility.UrlEncode(Functions.EncodeToBase64(artist.ID))));
                sbOutput.Append(artist.Name + "</a><br />");
            }
            sbOutput.Append("</div>");  // End of musiccollectionitemssection

            // Commit to form
            txtOutput = txtOutput.Replace("**MUSICCOLLECTIONINDEX**", sbIndex.ToString());
            txtOutput = txtOutput.Replace("**MUSICCOLLECTIONITEMS**", sbOutput.ToString());


            // Commit to response
            txtResponse += txtOutput;

            return true;


        }
        string IndexLetterForPhrase(string inputPhrase)
        {
            // TODO: make more clever
            if (string.IsNullOrWhiteSpace(inputPhrase)) return "#";
            inputPhrase = inputPhrase.Trim().ToUpperInvariant();

            string firstLetter = inputPhrase.Substring(0, 1);

            string[] words = inputPhrase.Split(' ');
            if (words.Count() <= 1)
            {
                return IndexLetterForWord(words[0]);
            }

            // More than one word; is the first word 'The' ?
            string firstWord = words[0];
            string wordForIndex = "";
            if (firstWord == "THE")
                wordForIndex = words[1];
            else
                wordForIndex = firstWord;

            return IndexLetterForWord(wordForIndex);

        }
        string IndexLetterForWord(string word)
        {
            // TODO: make more clever
            string firstLetter = word.Substring(0, 1);

            if (IsEnglishLetter(Convert.ToChar(firstLetter)))
                return firstLetter;

            return "#";
        }
        bool IsEnglishLetter(char c)
        {
            return (c >= 'A' && c <= 'Z'); // || (c >= 'a' && c <= 'z'); ALREADY UPPER
        }
        private bool ViewMusicArtist()
        {
            bool validate = qsParams.HasParameter("id");
            string artistID = qsParams["id"];
            validate &= (!(string.IsNullOrWhiteSpace(artistID)));


            if (!validate)
            {
                txtPageTitle = "Invalid Artist";
                txtResponse += "An invalid or incorrect artist ID was specified.";
                return true;
            }

            // Decode
            artistID = Functions.DecodeFromBase64(HttpUtility.UrlDecode(artistID));
            RPMusicArtist Artist = MusicCache.Default.artistWithID(artistID);

            if (Artist == null)
            {
                txtPageTitle = "Invalid Artist";
                txtResponse += "An invalid or incorrect artist ID was specified.";
                return true;
            }



            List<RPMusicAlbum> albums = Artist.Albums();
            string txtOutput = null;
            if (albums.Count > 0)
            {
                txtPageTitle = "Albums by " + Artist.Name;

                txtOutput = FileCache.ReadSkinTextFile("page_music_collection.htm");

                txtOutput = txtOutput.Replace("**MUSICCOLLECTIONINDEX**", "");
                txtOutput = txtOutput.Replace("**MUSICCOLLECTIONITEMS**", listAlbums(albums, listAlbumTextFormats.TitleOnly));
            }
            else
            {
                txtPageTitle = "Songs by " + Artist.Name;

                List<RPMusicSong> artistSongs = new List<RPMusicSong>();
                using (WMPManager manager = new WMPManager())
                {
                    artistSongs = manager.GetSongsForArtist(artistID);

                    txtOutput = FileCache.ReadSkinTextFile("page_music_songs.htm");

                    txtOutput = txtOutput.Replace("**MUSICSONGS**", tableofSongs(artistSongs, listSongTextFormats.TitleOnly, false));
                }
            }



            // Commit to form
            txtResponse += txtOutput;


            return true;


        }
        private bool ViewMusicAlbum()
        {
            bool validate = qsParams.HasParameter("id");
            string albumID = qsParams["id"];
            validate &= (!(string.IsNullOrWhiteSpace(albumID)));


            if (!validate)
            {
                txtPageTitle = "Invalid Album";
                txtResponse += "An invalid or incorrect album ID was specified.";
                return true;
            }

            // Decode
            albumID = Functions.DecodeFromBase64(HttpUtility.UrlDecode(albumID));
            RPMusicAlbum Album = MusicCache.Default.albumWithID(albumID);

            if (Album == null)
            {
                txtPageTitle = "Invalid Album";
                txtResponse += "An invalid or incorrect album ID was specified.";
                return true;
            }

            if (Album.Artist() != null)
                txtPageTitle = Album.Artist().Name + " - " + Album.Title;
            else
                txtPageTitle = Album.Title;

            List<RPMusicSong> artistSongs = new List<RPMusicSong>();
            string txtOutput = null;
            using (WMPManager manager = new WMPManager())
            {
                artistSongs = manager.GetSongsForAlbum(albumID);

                txtOutput = FileCache.ReadSkinTextFile("page_music_songs.htm");
                txtOutput = txtOutput.Replace("**MUSICSONGS**", tableofSongs(artistSongs, listSongTextFormats.TitleOnly, true));
            }

            // Commit to form
            txtResponse += txtOutput;


            return true;


        }
        private bool ViewMusicGenre()
        {
            bool validate = qsParams.HasParameter("id");
            string genreID = qsParams["id"];
            validate &= (!(string.IsNullOrWhiteSpace(genreID)));


            if (!validate)
            {
                txtPageTitle = "Invalid Genre";
                txtResponse += "An invalid or incorrect genre ID was specified.";
                return true;
            }

            // Decode
            genreID = Functions.DecodeFromBase64(HttpUtility.UrlDecode(genreID));
            RPMusicGenre Genre = MusicCache.Default.genreWithID(genreID);

            if (Genre == null)
            {
                txtPageTitle = "Invalid Genre";
                txtResponse += "An invalid or incorrect genre ID was specified.";
                return true;
            }

            txtPageTitle = Genre.Name;

            List<RPMusicAlbum> albums = Genre.Albums();
            string txtOutput = null;
            if (albums.Count > 0)
            {
                txtOutput = FileCache.ReadSkinTextFile("page_music_collection.htm");

                txtOutput = txtOutput.Replace("**MUSICCOLLECTIONINDEX**", "");
                txtOutput = txtOutput.Replace("**MUSICCOLLECTIONITEMS**", listAlbums(albums, listAlbumTextFormats.TitleOnly));
            }
            else
            {

                List<RPMusicSong> artistSongs = new List<RPMusicSong>();
                using (WMPManager manager = new WMPManager())
                {
                    artistSongs = manager.GetSongsForArtist(genreID);

                    txtOutput = FileCache.ReadSkinTextFile("page_music_songs.htm");
                    txtOutput = txtOutput.Replace("**MUSICSONGS**", tableofSongs(artistSongs, listSongTextFormats.TitleOnly, false));
                }
            }



            // Commit to form
            txtResponse += txtOutput;

            return true;
        }
        private bool ViewMusicSong()
        {
            bool validate = qsParams.HasParameter("id");
            string songID = qsParams["id"];
            validate &= (!(string.IsNullOrWhiteSpace(songID)));


            if (!validate)
            {
                txtPageTitle = "Invalid Song";
                txtResponse += "An invalid or incorrect song ID was specified.";
                return true;
            }

            // Decode
            string decodedSongID = Functions.DecodeFromBase64(HttpUtility.UrlDecode(songID));

            using (WMPManager manager = new WMPManager())
            {
                string FN = manager.FileNameForWMPItem(decodedSongID);

                txtPageTitle = Path.GetFileNameWithoutExtension(FN);

            }

            StringBuilder sbOutput = new StringBuilder(500);

            string href = "streamsong64.mp3?id=" + HttpUtility.UrlEncode(songID); // Already base64 encoded
            sbOutput.Append(Functions.LinkTagOpen(href));
            sbOutput.Append("Stream song</a>");



            // Commit to form
            txtResponse += sbOutput.ToString();


            return true;
        }

        // LIST SONGS AND ALBUMS
        enum listSongTextFormats { TitleOnly, ArtistAndTitle };
        string tableofSongs(List<RPMusicSong> songs, listSongTextFormats textFormat, bool showTrackNumber)
        {
            StringBuilder sbOutput = new StringBuilder(50);
            sbOutput.Append("<table>");
            foreach (RPMusicSong song in songs)
            {
                sbOutput.Append("<tr>");

                //sbOutput.Append(Functions.LinkTagOpen("musicsong?id="  +  HttpUtility.UrlEncode(Functions.EncodeToBase64(song.ID))) );

                if (showTrackNumber)
                    sbOutput.Append("<td>" + song.TrackNumber.ToString() + "." + "</td>");

                // Title
                sbOutput.Append("<td class=\"musicsongslisttitlecolumn\">");
                if (textFormat == listSongTextFormats.ArtistAndTitle)
                    sbOutput.Append(song.Artist().Name + " - ");
                sbOutput.Append(song.Title);
                sbOutput.Append("</td>");

                // Duration
                sbOutput.Append("<td>");
                sbOutput.Append(song.ToPrettyDuration());
                sbOutput.Append("</td>");

                // Stream link
                sbOutput.Append("<td>");
                string href = "streamsong64.mp3?id=" + HttpUtility.UrlEncode(Functions.EncodeToBase64(song.ID));
                sbOutput.Append(Functions.LinkTagOpen(href, "_blank"));
                sbOutput.Append("<img src=\"static/images/btnStreamSong.png\"> ");
                sbOutput.Append("</a>");
                sbOutput.Append("</td>");

                // Download link
                sbOutput.Append("<td>");
                href = "downloadsong64.mp3?id=" + HttpUtility.UrlEncode(Functions.EncodeToBase64(song.ID));
                sbOutput.Append(Functions.LinkTagOpen(href, "_blank"));
                sbOutput.Append("<img src=\"static/images/btnSaveSong.png\"> ");
                sbOutput.Append("</a>");
                sbOutput.Append("</td>");

                sbOutput.Append("<tr>");
            }
            sbOutput.Append("</table>");

            return sbOutput.ToString();
        }
        enum listAlbumTextFormats { TitleOnly, ArtistAndTitle };
        string listAlbums(List<RPMusicAlbum> albums, listAlbumTextFormats textFormat)
        {
            StringBuilder sbOutput = new StringBuilder(50);
            sbOutput.Append("<ul>");
            foreach (RPMusicAlbum album in albums)
            {
                sbOutput.Append("<li>");
                sbOutput.Append(Functions.LinkTagOpen("musicalbum?id=" + HttpUtility.UrlEncode(Functions.EncodeToBase64(album.ID))));

                if (textFormat == listAlbumTextFormats.ArtistAndTitle)
                    sbOutput.Append(album.Artist().Name + " - ");

                sbOutput.Append(album.Title);
                sbOutput.Append("</a></li>");
            }
            sbOutput.Append("</ul>");

            return sbOutput.ToString();
        }
        #endregion


        private bool ViewRecordedTVList()
        {
            txtPageTitle = "Recorded TV";

            bool RefreshNow = qsParams.HasParameter("refreshnow");

            string txtOutput = FileCache.ReadSkinTextFile("page_viewrecordedtv.htm");
            string txtRecList = "";

            bool foundAtLeastOneEvent = false;
            DateTime dateCounter = DateTime.Now.Date.AddMonths(-1);  // Datecounter remains in local time, GuideInfo.Get... function below will convert as necessary
            for (int i = 0; i < 50; i++)
            {
                bool foundEventInThisSection = false;
                DateTime endWindow = dateCounter.AddMonths(1);

                foreach (TVProgramme tvp in RecTV.Default.RecordedTVProgrammes.Values)
                {
                    if (
                        (tvp.StartTimeDT().ToLocalTime().Date > dateCounter.Date) &&
                        (tvp.StartTimeDT().ToLocalTime().Date <= endWindow.Date)
                        )
                    {
                        if (!foundAtLeastOneEvent) foundAtLeastOneEvent = true;

                        if (!foundEventInThisSection)
                        {
                            txtRecList += "\r\n\r\n<div class='rectvlistthumbnailseparator'><br />";
                            if (i == 0)
                                txtRecList += "This month:";
                            else if (i == 1)
                                txtRecList += "One month ago:";
                            else if (i == 2)
                                txtRecList += "Two months ago:";
                            else
                                txtRecList += i.ToString() + " months ago:";
                            txtRecList += "</div>";

                            foundEventInThisSection = true;
                        }

                        
                        // Output the program details
                        txtRecList += "\r\n<div class='rectvlistthumbnailcontainer'>";
                        txtRecList += Functions.LinkTagOpen("viewepgprogramme?getfromrecordedtvfiles=true&programmeid=" + tvp.Id);

                        // Display the thumbnail
                        txtRecList += "<img src=\"/rectvthumbnail64?filename=" + HttpUtility.UrlEncode(HttpUtility.HtmlEncode( Functions.EncodeToBase64( tvp.Filename) )) + "\" class=\"rectvlistthumbnail\" />";
                        txtRecList += "<br />";

                        // Display the title
                        txtRecList += tvp.Title;

                        // Ep Title?
                        if (!String.IsNullOrWhiteSpace(tvp.EpisodeTitle))
                            txtRecList += "<br />\"" + tvp.EpisodeTitle + "\"";

                        // Close the link tag
                        txtRecList += "</a>";

                        txtRecList += "</div>";


                    }
                }

                // Increase the date
                dateCounter = dateCounter.AddMonths(-1);
            }


            if (!foundAtLeastOneEvent)
                txtRecList += "There are no past recordings.";

            txtOutput = txtOutput.Replace("**RECORDEDTVLIST**", txtRecList);

            // Commit to form
            txtResponse += txtOutput;


            return true;


        }
        private bool ViewScheduledRecordings()
        {
            txtPageTitle = "Scheduled Recordings";

            if (!EPGManager.isWMCOnline)
            {
                txtResponse += "EPG data is not available.";
                return false;
            }


            // Get template
            string txtOutput = FileCache.ReadSkinTextFile("page_viewscheduledrecordings.htm");
            string txtSchedList = "";

            // FOR NOW.. ..refresh each time, although strictly may not be necessary
            if (Settings.Default.RecordingsRetrieveAsParanoid) EPGManager.ReloadAllRecordings();

            List<RPRecording> recordings;
            DateTime dateCounter = DateTime.Now.Date;  // Datecounter remains in local time, EPGManager function below will convert as necessary
            bool foundAtLeastOneEvent = false;
            for (int i = 0; i < Convert.ToInt32(Settings.Default.ViewScheduledRecordingsDaysAhead); i++)
            {
                if (i == 0)
                    recordings = EPGManager.AllRecordingsTodayRemaining();
                else
                    recordings = EPGManager.AllRecordingsOnDate(dateCounter);

                if (recordings.Count > 0)
                {
                    foundAtLeastOneEvent = true;

                    if (dateCounter.Date.Equals(DateTime.Now.Date))
                        txtSchedList += "Today:";
                    else if ((dateCounter - DateTime.Now).TotalDays < 7)
                        txtSchedList += CultureInfo.CurrentCulture.DateTimeFormat.GetDayName(dateCounter.DayOfWeek) + ":";
                    else
                        txtSchedList += dateCounter.ToPrettyDayNameAndDate() + ":";

                    txtSchedList += "<ul class='scheduledrecordingdaygroup'>";
                    
                    foreach (RPRecording rec in recordings)
                    {
                        TVProgramme tvp = rec.TVProgramme();
                        if (tvp == null) continue;

                        txtSchedList += "<li><a href=viewepgprogramme?programmeid=" + tvp.Id + ">";
                        txtSchedList += tvp.StartTimeDT().ToLocalTime().ToShortTimeString() + ": " + tvp.Title;
                        txtSchedList += "</a></li>";
                    }
                    txtSchedList += "</ul>";

                }
                dateCounter = dateCounter.AddDays(1);
            }

            if (!foundAtLeastOneEvent)
                txtSchedList += "There are no upcoming recordings.";

            // Commit list
            txtOutput = txtOutput.Replace("**SCHEDULEDRECORDINGSLIST**", txtSchedList);

            // Commit template
            txtResponse += txtOutput;

            return true;
        }  // all recordings scheduled
        /// <summary>
        /// View a scheduled event
        /// </summary>
        /// <param name="txtResponse"></param>
        /// <returns></returns>
        /// 
        private bool ViewSeriesRequest()
        {
            txtPageTitle = "Series Details";

            if (!EPGManager.isWMCOnline)
            {
                txtResponse += "TV is not configured.";
                return false;
            }

            // Parameters
            long requestID;
            try { requestID = long.Parse(qsParams["requestid"]); }
            catch { return false; }

            // Get request with this ID
            RPRequest rpr;
            if (! EPGManager.AllRequests.TryGetValue(requestID, out rpr))
            {
                txtResponse += "This series or keyword recording no longer exists.";
                return false;
            }

            // Get table for display
            string tblSeries = FileCache.ReadSkinTextFile("page_series.htm");
            tblSeries = tblSeries.Replace("**TITLE**", rpr.Title);

            // Get TV Programmes that the request is making...
            List<RPRecording> recs = rpr.Recordings();
            
            // UPCOMING SHOWINGS
            tblSeries = tblSeries.Replace("**UPCOMINGSHOWINGS**", ListOfRecordingsAsHTML(ref recs, DateRangeType.FutureOnly, false, true, true, true));
           //tblSeries = tblSeries.Replace("**PASTSHOWINGS**", ListOfRecordingsAsHTML(ref recs, DateRangeType.PastOnly, false, true, true, true));
            tblSeries = tblSeries.Replace("**PASTSHOWINGS**", "");

            // Commit to displaying show info
            txtResponse += tblSeries;

            // Links to cancel series
            if ((rpr.RequestType == RPRequestTypes.Series) || (rpr.RequestType == RPRequestTypes.Keyword))
                txtResponse += "<br /><a " + Functions.LinkConfirmClick("Are you sure?  This will cancel the entire series.") + "href=\"cancelseriesrequest?requestid=" + rpr.ID + "\">Cancel " + rpr.RequestType.ToString() + "</a>";

            return true;

        }   // a series and associated recordings
        private bool ViewSeriesRequests()
        {
            txtPageTitle = "Manage Series";

            if (!EPGManager.isWMCOnline)
            {
                txtResponse += "EPG Data is not available.";
                return false;
            }

            // FOR NOW.. ..refresh each time, although strictly may not be necessary
            if (Settings.Default.RecordingsRetrieveAsParanoid) EPGManager.ReloadAllRecordings();

            bool foundAtLeastOneEvent = false;
            txtResponse += "<ul class='allschedulerequests'>";
            // Get series recordings
            foreach (RPRequest rpr in EPGManager.AllRequests.Values)
            {
                if (
                    (rpr.RequestType != RPRequestTypes.Series) &&
                    (rpr.RequestType != RPRequestTypes.Keyword)
                    )
                    continue;
                foundAtLeastOneEvent = true;

                txtResponse += "<li><a href=viewseriesrequest?requestid=" + rpr.ID + ">";
                txtResponse += rpr.Title;
                txtResponse += "</a></li>";
            }
            txtResponse += "</ul>";

            if (!foundAtLeastOneEvent)
                txtResponse += "There are no upcoming recordings.";

            return true;
        }  // all series requests
        private enum DateRangeType
        {
            All = 0,
            PastOnly = 1,
            FutureOnly = 2
        };
        private static string ListOfRecordingsAsHTML(ref List<RPRecording> recs, DateRangeType dateRangeToShow, bool showTitle, bool showStartTime, bool showChannel, bool linkToPage)
        {
            string txtShowings = "";
            bool hasShownAnyEvents = false;

            bool fallsWithinDateRange;
            foreach (RPRecording rec in recs)
            {
                // Get associated TV Programme
                TVProgramme tvp = rec.TVProgramme();
                if (tvp == null) continue;
                
                TVService tvs = tvp.TVService();
                if (tvs == null) continue;

                // Include this showing?
                fallsWithinDateRange = (dateRangeToShow == DateRangeType.All);

                if (dateRangeToShow == DateRangeType.FutureOnly)
                    if (tvp.StopTimeDT().ToLocalTime() >= DateTime.Now)
                        fallsWithinDateRange = true;

                if (dateRangeToShow == DateRangeType.PastOnly)
                    if (tvp.StopTimeDT().ToLocalTime() < DateTime.Now)
                        fallsWithinDateRange = true;

                if (fallsWithinDateRange)
                {
                    if (!hasShownAnyEvents)
                    {
                        txtShowings += "<ul>";
                        hasShownAnyEvents = true;
                    }
                    
                    txtShowings += "<li class='";
                    // style
                    if ( (rec.State ==  RPRecordingStates.Scheduled) ||
                        (rec.State == RPRecordingStates.Recording ) || 
                        (rec.State == RPRecordingStates.Recorded) )
                            txtShowings += "upcomingwillrecord";
                    else
                        txtShowings += "upcomingwontrecord";
                    txtShowings += "'>";
                    if (linkToPage) txtShowings += "<a href='viewepgprogramme?programmeid=" + tvp.Id + "'>";
                    if (showTitle) txtShowings += tvp.Title + ": ";
                    if (showStartTime) txtShowings += tvp.ToPrettyDate() + ", " + tvp.StartTimeDT().ToLocalTime().ToShortTimeString();

                    if (showChannel) 
                        if (tvs != null)
                            txtShowings += " (" + tvs.Callsign + ")";
                    if (linkToPage) txtShowings += "</a>";
                    txtShowings += "</li>";
                }
            }

            if (hasShownAnyEvents)
                txtShowings += "</ul>";
            else
                txtShowings += "None found.";


            return txtShowings;
        }
        private bool DeleteFileFromFilePath(bool isBase64Encoded)
        {
            txtPageTitle = "Delete Recording";

            string qsFN = qsParams["filename"];
            string fn = HttpUtility.UrlDecode(qsFN);
            if (isBase64Encoded)
                fn = Functions.DecodeFromBase64(fn, Encoding.UTF8);  // http uses UTF8 encoding

            txtResponse += "Permanently deleting the show...";
            txtResponse += WebSvcDeleteFileByFilePath(fn);

            return true;
        }
        private bool CancelRequest()
        {
            // Get Schedule Request
            long requestID;
            try { requestID = long.Parse(qsParams["requestid"]); }
            catch { return false; }

            // Get request that is generating this event
            RPRequest rpr;
            if (!EPGManager.AllRequests.TryGetValue(requestID, out rpr))
            {
                txtPageTitle = "Cancel Series";
                txtResponse += "This series or keyword recording no longer exists.";
                return false;
            }

            txtPageTitle = "Cancel " + rpr.RequestType.ToString() + " Recording";

            txtResponse += "Cancelling " + rpr.Title + ":";

            try
            {
                EPGManager.mcData.CancelRequest(rpr.ID);
                txtResponse += "<br><br>" + rpr.RequestType.ToString() + " recording cancelled.";
            }
            catch (Exception e)
            {
                txtResponse += "Error - could not cancel recording request: " + e.Message;
            }

            return true;
        }
        private bool CancelRecording()
        {
            // Get Schedule Request
            long recordingID;
            try { recordingID = long.Parse(qsParams["recordingid"]); }
            catch { return false; }

            // Get recording
            RPRecording rec;
            if (!EPGManager.AllRecordings.TryGetValue(recordingID, out rec))
            {
                txtPageTitle = "Cancel Recording";
                txtResponse += "This recording no longer exists.";
                return false;
            }

            txtPageTitle = "Cancel Single Recording";
            txtResponse += "Cancelling " + rec.Title + ":";

            // What kind of recording?  If it's a OneTime or manual, then cancel the request too.
            if ( (rec.RequestType == RPRequestTypes.OneTime) || (rec.RequestType == RPRequestTypes.Manual) )
            {
                RPRequest rq = rec.Request();
                if (rq != null)
                {
                    try
                    {
                        EPGManager.mcData.CancelRequest(rq.ID);
                    }
                    catch (Exception e)
                    {
                        txtResponse += "Error - could not cancel request generating recording to be cancelled: " + e.Message;
                    }
                }
            }
            
            // Now cancel the recording itself
            try
            {
                EPGManager.mcData.CancelRecording(rec.Id);
                txtResponse += "<br /><br />This recording of " +  rec.Title +  " was cancelled.";
            }
            catch (Exception e)
            {
                txtResponse += "Error - could not cancel recording: " + e.Message;
            }

            return true;
        }
        private bool SearchShowsByText()
        {
            if (!EPGManager.isWMCOnline)
            {
                txtResponse += "No EPG is configured, or the XML file cannot be found.<br /><br />You will need to enable this option in settings on the server, or point to a valid XML file in XMLTV format.";
                return false;
            }

            string requestForm = "";

            // POSTED FORM?           
            StringBuilder txtSearchResults = new StringBuilder(200);
            string searchTitle = "";
            if ((qsParams.HasParameter("searchtitle")))
            {
                searchTitle = qsParams["searchtitle"];
            }


            CommonEPG.EPGSearchTextType epgSearchTextType = CommonEPG.EPGSearchTextType.Title;
            if (qsParams.HasParameter("epgsearchtexttype"))
                epgSearchTextType = (CommonEPG.EPGSearchTextType)Enum.Parse(typeof(CommonEPG.EPGSearchTextType), qsParams["epgsearchtexttype"], true);

            CommonEPG.EPGSearchMatchType epgSearchMatchType = CommonEPG.EPGSearchMatchType.StartsWith;
            if (qsParams.HasParameter("epgsearchmatchtype"))
                epgSearchMatchType = (CommonEPG.EPGSearchMatchType)Enum.Parse(typeof(CommonEPG.EPGSearchMatchType), qsParams["epgsearchmatchtype"], true);


            if (
                (searchTitle.Length > 1) ||
                (epgSearchMatchType == EPGSearchMatchType.ExactMatch)  // allow 1 character searches for 'EXACT' match
                )
            {
                txtPageTitle = "Search Results";
                requestForm = FileCache.ReadSkinTextFile("page_searchshow_results.htm");

                bool resultsTruncated = false;
                List<CommonEPG.TVProgramme> myProgs = EPGManager.SearchTVProgrammes(searchTitle, epgSearchTextType, epgSearchMatchType, out resultsTruncated);

                if (myProgs.Count > 0)
                {
                    txtSearchResults.Append("<ul class='searchresults'>");
                    bool willRecord = false;
                    foreach (CommonEPG.TVProgramme tvp in myProgs)
                    {
                        RPRecording rec = tvp.Recording();
                        willRecord = (rec != null);

                        txtSearchResults.Append("<li class='");
                        txtSearchResults.Append(willRecord ? "upcomingwillrecord" : "upcomingwontrecord");
                        txtSearchResults.Append("'>");
                        txtSearchResults.Append(Functions.LinkTagOpen("viewepgprogramme?programmeid=" + tvp.Id));
                        txtSearchResults.Append(tvp.ToPrettyDate());
                        txtSearchResults.Append(", " + tvp.StartTimeDT().ToLocalTime().ToShortTimeString() + ": " + tvp.Title + " (" + tvp.MatchedChannelCallsign() + ")</a>");

                        if (willRecord)
                        {
                            string dotName = rec.IsRecurring() ? "record_dot_series" : "record_dot_onetime";
                            txtSearchResults.Append("<img id=\"searchresultsrecorddot\" src=\"/static/images/" + dotName + ".png\" />");
                        }
                        txtSearchResults.Append("</li>");
                    }
                    txtSearchResults.Append("</ul>");

                    if (resultsTruncated)
                    {
                        txtSearchResults.Append("Only the first fifty matches have been shown.  To see more results, use a more specific search term.");
                    }
                }
                else
                {
                    txtSearchResults.Append("No matching shows found.");
                }

                // Commit search results
                requestForm = requestForm.Replace("**SEARCHRESULTS**", txtSearchResults.ToString());

            }
            else
            {
                // Get form
                txtPageTitle = "Search for a show";
                requestForm = FileCache.ReadSkinTextFile("page_searchshow_title.htm");

                // Process form
                processRequestForm(ref requestForm, "", DateTime.Now, 0, DateTime.Now, 0, searchTitle);

                // just show form
                requestForm += ("Please enter a title to search for, above.  A minimum of two characters is required.");
            }

            // Commit results (if any)
            txtResponse += requestForm;
            requestForm = null;


            return true;

        }
        private bool ViewEPGList()
        {
            // Set current EPG type.
            Settings.Default.CurrentEPGType = "list";

            txtPageTitle = "TV Guide";

            if (!EPGManager.isWMCOnline)
            {
                txtResponse += "No EPG is configured.<br /><br />You will need to enable this option in settings on the server.";
                return false;
            }

            // Always display date dropdowns
            string requestForm = FileCache.ReadSkinTextFile("page_viewepg_tbl.htm");

            // POSTED FORM

            if (!(qsParams.HasParameter("channelcallsign")))
            {
                try
                {
                    qsParams.Add("channelcallsign", EPGManager.EPGDisplayedTVChannels[0].UniqueId);
                }
                catch { }
            }

            if (!(qsParams.HasParameter("datecomponent")))
                qsParams.Add("datecomponent", DateTime.Now.Date.ToShortDateString());

            if (!(qsParams.HasParameter("epgdayphase")))
            {
                if (DateTime.Now.Hour > 18)
                    qsParams.Add("epgdayphase", "3");
                else if (DateTime.Now.Hour > 11)
                    qsParams.Add("epgdayphase", "2");
                else
                    qsParams.Add("epgdayphase", "1");
            }

            string chanId = qsParams["channelcallsign"];

            int epgDayPhase;
            if (!Int32.TryParse(qsParams["epgdayphase"], out epgDayPhase))
            {
                txtResponse += "Bad parameters - check form.";
                return false;
            }

            DateTime epgDate;
            if (!DateTime.TryParse(qsParams["datecomponent"], out epgDate))
            {
                txtResponse += "Bad parameters - check form.";
                return false;
            }

            DateTime middleOfWindow, startWindow, endWindow;

            switch (epgDayPhase)
            {
                case 1:
                    middleOfWindow = DateTime.Parse(epgDate.Date.ToShortDateString() + " 08:00");
                    startWindow = middleOfWindow.AddHours(-5);
                    endWindow = middleOfWindow.AddHours(4);
                    break;

                case 2:
                    middleOfWindow = DateTime.Parse(epgDate.Date.ToShortDateString() + " 15:00");
                    startWindow = middleOfWindow.AddHours(-3);
                    endWindow = middleOfWindow.AddHours(3);
                    break;

                default:
                    middleOfWindow = DateTime.Parse(epgDate.Date.ToShortDateString() + " 22:00");
                    startWindow = middleOfWindow.AddHours(-4);
                    endWindow = middleOfWindow.AddHours(5);
                    break;
            }


            // Process form at top (always)
            processRequestForm(ref requestForm, chanId, epgDate, epgDayPhase, DateTime.Now, 0, "");
            string txtEPGList = "";

            //CommonEPG.TVChannel tvc = EPGManager.TVChannelWithIDOrNull(chanId);

            CommonEPG.DateRange myRange = new CommonEPG.DateRange(startWindow.ToUniversalTime(), endWindow.ToUniversalTime());
            List<CommonEPG.TVProgramme> myProgs = EPGManager.mcData.GetTVProgrammes(myRange, chanId, false);


            if (myProgs.Count > 0)
            {
                txtEPGList += "<ul class='epgprogrammes'>";
                bool willRecord = false;
                foreach (CommonEPG.TVProgramme tvp in myProgs)
                {
                    RPRecording rec = tvp.Recording();
                    willRecord = (rec != null);
                    txtEPGList += "<li class='";
                    txtEPGList += willRecord ? "upcomingwillrecord" : "upcomingwontrecord";
                    txtEPGList += "'>";
                    txtEPGList += Functions.LinkTagOpen("viewepgprogramme?programmeid=" + tvp.Id);
                    txtEPGList += tvp.StartTimeDT().ToLocalTime().ToShortTimeString() + ": " + tvp.Title + "</a>";
                    if (willRecord)
                    {
                        string dotName = rec.IsRecurring() ? "record_dot_series" : "record_dot_onetime";
                        txtEPGList += "<img id=\"epglistrecorddot\" src=\"/static/images/" + dotName + ".png\" />";
                    }
                    txtEPGList += "</li>";
                }
                txtEPGList += "</ul>";
            }
            else
            {
                txtEPGList += "No programmes found in EPG.";
            }


            requestForm = requestForm.Replace("**EPGLIST**", txtEPGList);
            txtResponse += requestForm;
            requestForm = null;


            return true;

        }
        private bool ViewMovies()
        {
            txtPageTitle = "Movie Guide";

            if (!EPGManager.isWMCOnline)
            {
                txtResponse += "No EPG is configured, or the XML file cannot be found.<br /><br />You will need to enable this option in settings on the server, or point to a valid XML file in XMLTV format.";
                return false;
            }

            // Always display date dropdowns
            string requestForm = FileCache.ReadSkinTextFile("page_movies.htm");

            // POSTED FORM

            if (!(qsParams.HasParameter("view")))
            {
                try
                {
                    qsParams.Add("view", "toprated");
                }
                catch { }
            }

            string movieView = qsParams["view"];

            // Process form at top (always)
            string txtMovieList = "";

            DateTime startWindow = DateTime.Now.AddHours(-3);
            DateTime endWindow = startWindow.AddDays(14);  // 2 weeks
            DateRange myRange = new DateRange(startWindow.ToUniversalTime(), endWindow.ToUniversalTime());
            List<TVProgramme> myProgs = EPGManager.GetTVMovies(myRange);

            EPGManager.CacheMoviesIfExpired(myProgs, Settings.Default.ListMoviesOnFavouriteChannelsOnly);

            if (myProgs.Count > 0)
            {
                DateTime dateCounter = DateTime.Now.Date;
                DateTime endCounter = new DateTime();
                int[] dayLeaps = new int[] { 1, 1, 5, 7 };
                string[] groupNames = new string[] { "Later Today", "Tomorrow", "Later this week", "Next week" };
                for (int i = 0; i < dayLeaps.Count(); i++)
                {

                    endCounter = dateCounter.AddDays(dayLeaps[i]);
                    // Group Title

                    bool willRecord = false;
                    bool foundAnyMatchesInGroup = false;
                    foreach (TVMovie tvm in EPGManager.CachedMovies.Values)
                    {
                        // First time round, only show movies on that haven't finished.
                        bool dateWindowMatch = (i == 0) ? (
                                            (tvm.DefaultShowing.StopTimeDT().ToLocalTime() > DateTime.Now) &&
                                            (tvm.DefaultShowing.StartTimeDT().ToLocalTime() < endCounter)
                                            ) :
                                            (
                                            (tvm.DefaultShowing.StartTimeDT().ToLocalTime() > dateCounter) &&
                                            (tvm.DefaultShowing.StartTimeDT().ToLocalTime() < endCounter)
                                            );


                        // If it's in the current group
                        if (dateWindowMatch)
                        {

                            // Filter
                            bool includeInList = true;
                            if (movieView == "recommended")
                                includeInList = (tvm.DefaultShowing.IsRecommended());
                            else if (movieView == "toprated")
                                includeInList = (tvm.DefaultShowing.IsTopRated());

                            if (includeInList)
                            {
                                if (!foundAnyMatchesInGroup)
                                {
                                    // First match - Print header
                                    txtMovieList += "<div class=\"moviesublistheader\">" + groupNames[i] + "</div>";
                                    txtMovieList += "<ul class=\"moviesublist\">";
                                    foundAnyMatchesInGroup = true;
                                }

                                willRecord = (tvm.DefaultShowing.Recording() != null);
                                txtMovieList += "<li class=\"";
                                txtMovieList += willRecord ? "upcomingwillrecord" : "upcomingwontrecord";
                                txtMovieList += "\">";
                                txtMovieList += Functions.LinkTagOpen("viewmovie?movieid=" + tvm.Id);
                                txtMovieList += tvm.Title + "</a>";
                                if (willRecord)
                                {
                                    //string dotName = isSeriesRecording ? "record_dot_series" : "record_dot_onetime";
                                    string dotName = "record_dot_onetime";
                                    txtMovieList += "<img id=\"movielistrecorddot\" src=\"/static/images/" + dotName + ".png\" />";
                                }
                                txtMovieList += "</li>";
                            }

                        }

                    }

                    // Group footer
                    if (foundAnyMatchesInGroup)
                        txtMovieList += "</ul>";

                    // Move on
                    dateCounter = endCounter;
                }
            }
            else
            {
                if ((Settings.Default.ListMoviesOnFavouriteChannelsOnly) && (EPGManager.FavoriteTVChannels.Count < 1))
                {
                    txtMovieList += "Movies are currently only displayed for your favourite channels, and none of these are selected.<br /><br />This feature has been automatically disabled, please click refresh to see movies on all channels.";
                    Settings.Default.ListMoviesOnFavouriteChannelsOnly = false;
                }
                else
                    txtMovieList += "No programmes found in EPG.";
            }


            string txtMovieViewType;
            switch (movieView)
            {
                case "toprated":
                    txtMovieViewType = "Top-Rated Movies";
                    break;

                case "recommended":
                    txtMovieViewType = "Recommended Movies";
                    break;

                default:
                    txtMovieViewType = "All Movies";
                    break;
            }
            requestForm = requestForm.Replace("**MOVIESTITLE**", txtMovieViewType);
            requestForm = requestForm.Replace("**MOVIESLIST**", txtMovieList);
            txtResponse += requestForm;
            requestForm = null;


            return true;

        }
        private bool ViewRemoteControl()
        {
            txtPageTitle = "Remote Control";

            // Commit to displaying show info
            txtResponse += FileCache.ReadSkinTextFile("page_remote.htm");

            return true;
        }
        private bool SendRemoteControlCommand(ref BrowserSender bs)
        {
            if (Themes.UsingMobileTheme)
            {
                return SendRemoteControlCommandMobile(ref bs);
            }
            else
            {
                SendRemoteControlCommandDesktop(ref bs);
                return true;
                
                
            }
        }
        private bool SendRemoteControlCommandMobile(ref BrowserSender bs)
        {
            // CHECK PROCESS IS RUNNING AND FOCUSSED?
            bool doCommand = (qsParams.HasParameter("command"));
            string strCommand = "";
            string strResponse = "No Command";

            if (doCommand)
            {
                strCommand = qsParams["command"];
                if (strCommand == "none") // Default page
                {
                    // First command - always displayed within an iFrame, even on iPhone
                    string txtHTML = FileCache.ReadSkinTextFile("page_remote_command.htm");
                    
                    strResponse = "Click a button.";

                    // Replace Caption within iFrame with response
                    txtHTML = txtHTML.Replace("**REMOTE_RESPONSE**", strResponse);

                    // Send entire page to browser.
                    bs.SendNormalHTMLPageToBrowser(txtHTML);

                    return true;
                }
                else
                {
                    string strResult = IRCommunicator.Default.SendIRCommand(strCommand); // No return (Async)
                    if (strResult == "OK")
                        strResponse = "Sent " + strCommand + " OK";
                    else
                    {
                        if (strResult == "HELPER_NOT_RUNNING")
                            strResult = "The IR Helper application is not running on the server - check your settings.";

                        strResponse = "Not OK: " + strResult;
                    }
                }
            }

            // iPhone workaround - send the whole page again. (opens it in a new window)
            txtResponse += FileCache.ReadSkinTextFile("page_remote.htm");
            txtResponse += "<br />" + strResponse;
            return false;
        }
        private void SendRemoteControlCommandDesktop(ref BrowserSender bs)
        {
            string txtHTML = FileCache.ReadSkinTextFile("page_remote_command.htm");

            // CHECK PROCESS IS RUNNING AND FOCUSSED?
            bool doCommand = (qsParams.HasParameter("command"));
            string strCommand = "";
            string strResponse = "No Command";

            if (doCommand)
            {
                strCommand = qsParams["command"];
                if (strCommand == "none")
                {
                    strResponse = "Click a button.";
                }
                else
                {
                    string strResult = IRCommunicator.Default.SendIRCommand(strCommand); // No return (Async)
                    if (strResult == "OK")
                        strResponse = "Sent " + strCommand + " OK";
                    else
                    {
                        if (strResult == "HELPER_NOT_RUNNING")
                            strResult = "The IR Helper application is not running on the server - check your settings.";

                        strResponse = "Not OK: " + strResult;
                    }
                }
            }

            // Replace Caption within iFrame with response
            txtHTML = txtHTML.Replace("**REMOTE_RESPONSE**", strResponse);

            // Send entire page to browser.
            bs.SendNormalHTMLPageToBrowser(txtHTML);
        }
        private bool ViewMovie()
        {
            txtPageTitle = "Movie Details";

            if (!EPGManager.isWMCOnline)
            {
                txtResponse = "No EPG is configured.  You will need to enable this option in settings on the server and ensure that you have a valid provider of EPG information set up.";
                return false;
            }

            if (!(qsParams.HasParameter("movieid")))
            {
                txtResponse += "No movie ID specified.";
                return false;
            }

            int movieID;
            if (!Int32.TryParse(qsParams["movieid"], out movieID))
            {
                txtResponse += "Invalid movie ID specified.";
                return false;
            }

            TVMovie matchedMovie = EPGManager.GetCachedMovieByID(movieID);
            if (matchedMovie == null)
            {
                txtResponse += "Could not find this show in the movie cache - have you visited an old link?  Click <a href=\"/movies\">here</a> to refresh.";
                return false;
            }

            // If there's just one showing - go straight to the epg prog!
            if (matchedMovie.Showings.Count == 1)
            {
                qsParams.Add("programmeid", matchedMovie.DefaultShowing.Id);
                qsParams.Remove("movieid");
                return ViewEPGProgramme();
            }


            // Get table for display
            string tblShow = FileCache.ReadSkinTextFile("page_movie.htm");
            string tagThumbnails = HTMLHelper.imgTagDefault();

            tblShow = tblShow.Replace("**THUMBNAIL**", tagThumbnails);
            tblShow = tblShow.Replace("**TITLE**", matchedMovie.Title);
            tblShow = tblShow.Replace("**DESCRIPTION**", matchedMovie.DefaultShowing.Description);


            // RATINGS
            // Star Rating
            StringBuilder sbStarRating = new StringBuilder();
            int starCounter = matchedMovie.DefaultShowing.StarRating;
            if (matchedMovie.DefaultShowing.StarRating > 0)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (starCounter > 1) // full star
                        sbStarRating.Append("<div class=\"showstarrating_star_on\"> </div>");
                    else if (starCounter == 1)  // half star
                        sbStarRating.Append("<div class=\"showstarrating_star_half\"> </div>");
                    else
                        sbStarRating.Append("<div class=\"showstarrating_star_off\"> </div>");

                    starCounter = starCounter - 2;
                }
                sbStarRating.Append("<div class=\"showstarrating_endstars\"> </div>");
            }
            tblShow = tblShow.Replace("**STARRATING**", sbStarRating.ToString());

            // MPAA Rating
            string txtMPAARating;
            if (!string.IsNullOrEmpty(matchedMovie.DefaultShowing.MPAARating))
                txtMPAARating = "<br />MPAA Rating: " + matchedMovie.DefaultShowing.MPAARating;
            else
                txtMPAARating = "";
            tblShow = tblShow.Replace("**GUIDANCERATING**", txtMPAARating);
            tblShow = tblShow.Replace("**SHOWTYPE**", Functions.imgTag("/static/images/showtypemovie.png", "showprogrammetypeimg"));

            // Future Showings
            StringBuilder sbShowings = new StringBuilder();
            sbShowings.Append("<ul class=\"futureshowingslist\">");
            CommonEPG.TVService tvc;
            foreach (CommonEPG.TVProgramme tvp in matchedMovie.Showings)
            {
                sbShowings.Append("<li>");
                sbShowings.Append(Functions.LinkTagOpen("/viewepgprogramme?programmeid=" + tvp.Id.ToString()));
                sbShowings.Append(tvp.ToPrettyDayNameAndDate() + " " + tvp.StartTimeDT().ToLocalTime().ToShortTimeString());
                tvc = EPGManager.TVServiceWithIDOrNull(tvp.ServiceID);
                if (tvc != null) sbShowings.Append(" (" + tvc.Callsign + ")");
                sbShowings.Append("</a>");

                //TODO: red record dots here

                sbShowings.Append("</li>");
            }
            sbShowings.Append("</ul>");
            tblShow = tblShow.Replace("**FUTURESHOWINGS**", sbShowings.ToString());

            // Commit to displaying show info
            txtResponse += tblShow;

            return true;

        }
        private void ViewLoginPage(string PostObjects, ref bool DidLoginSuccessfully, ref string destURL, ref string destQueryString)
        {
            
            txtPageTitle = "Login";
            
            // Always display date dropdowns
            string requestForm = FileCache.ReadSkinTextFile("page_login.htm");

            // POSTED FORM: DO LOGIN
            string strHeader = "";
            string storedDestURL = txtActionOriginalCase;
            string storedQueryString = Request.Url.Query;

            // Convert the post string into a name value collection
            NameValueCollection postParams = HttpUtility.ParseQueryString(PostObjects);
            if (
                    (!postParams.HasParameter("un")) || (!postParams.HasParameter("pw"))
                    )
            {
                // Show login form
                strHeader = "Please enter your details:";
                // ...and grab destination URL below
            }
            else
            {
                // Grab stored dest URL from form.
                storedDestURL = postParams["desturl"];

                if (postParams.HasParameter("destquerystring"))
                {
                    storedQueryString = postParams["destquerystring"];
                    storedQueryString = HttpUtility.UrlDecode(storedQueryString);
                }

                if ((!postParams["un"].Trim().Equals(Settings.Default.UserName)) ||
                    (!Functions.StringHashesToPasswordHash(postParams["pw"].Trim(), false))
                    )
                {
                    strHeader = "Incorrect details, please try again.";
                    
                }
                else
                {
                    // Success!
                    string token = AuthSessionHelper.Default.AddClient(currentClientIP);  // Store session, get token

                    // Return the dest URL  and querystring
                    destURL = storedDestURL;
                    destQueryString = storedQueryString;

                    Cookie cookToken = new Cookie("RemotePotatoToken", token);
                    Response.AppendCookie(cookToken);
                    Response.Redirect("/");

                    DidLoginSuccessfully = true;
                    return ;  // logged in
                }
            }

            requestForm = requestForm.Replace("**LOGIN_DEST_URL**", storedDestURL);
            requestForm = requestForm.Replace("**LOGIN_DEST_QUERYSTRING**", HttpUtility.UrlEncode(storedQueryString));
            requestForm = requestForm.Replace("**LOGIN_HEADER**", strHeader);
            txtResponse += requestForm;
            requestForm = null;

            DidLoginSuccessfully = false;
            return ; // not logged in

        }
        private bool EPGGridChannelAdvance()
        {
            // get old state
            if (!lastEPGQueryStringForIP.ContainsKey(currentClientIP))
            {
                txtResponse += "Old link, click <a href='viewepggrid'>here</a> to refresh.";
                return true;
            }

            qsParams = lastEPGQueryStringForIP[currentClientIP];

            int ChannelsPerPage = Convert.ToInt32(Settings.Default.EPGGridChannelsPerPage);

            // GRID CHANNEL
            int GridChannelIndex;
            if (!Int32.TryParse(qsParams["epggridchannelindex"], out GridChannelIndex))
            {
                txtResponse += "Bad channel index.";
                return true;
            }

            // Re-create querystring
            int GridChannelIndexUpNext = GridChannelIndex + ChannelsPerPage;
            // Insist on a full final page
            int highestIndexAllowed = (EPGManager.EPGDisplayedTVChannels.Count - ChannelsPerPage + 1);
            if ((GridChannelIndexUpNext) > highestIndexAllowed) GridChannelIndexUpNext = highestIndexAllowed;

            qsParams.Remove("epggridchannelindex");
            qsParams.Add("epggridchannelindex", GridChannelIndexUpNext.ToString());

            return ViewEPGGrid();
        }
        private bool EPGGridChannelRetreat()
        {
            // get old state
            if (!lastEPGQueryStringForIP.ContainsKey(currentClientIP))
            {
                txtResponse += "Old link, click <a href='viewepggrid'>here</a> to refresh.";
                return true;
            }

            // get old state
            qsParams = lastEPGQueryStringForIP[currentClientIP];

            int ChannelsPerPage = Convert.ToInt32(Settings.Default.EPGGridChannelsPerPage);

            // GRID CHANNEL
            int GridChannelIndex;
            if (!Int32.TryParse(qsParams["epggridchannelindex"], out GridChannelIndex))
            {
                txtResponse += "Bad channel index.";
                return false;
            }

            // Re-create querystring
            int GridChannelIndexDownNext = GridChannelIndex - ChannelsPerPage;
            if (GridChannelIndexDownNext < 1) GridChannelIndexDownNext = 1;

            qsParams.Remove("epggridchannelindex");
            qsParams.Add("epggridchannelindex", GridChannelIndexDownNext.ToString());

            return ViewEPGGrid();
        }
        private bool EPGGridChannelJump()
        {
            // get old state
            if (!lastEPGQueryStringForIP.ContainsKey(currentClientIP))
            {
                txtResponse += "Old link, click <a href='viewepggrid'>here</a> to refresh.";
                return true;
            }


            // First get current page from querystring
            int page;
            if (!Int32.TryParse(qsParams["page"], out page))
            {
                txtResponse += "Bad page.";
                return false;
            }

            // Now get old grid state
            qsParams = lastEPGQueryStringForIP[currentClientIP];

            int ChannelsPerPage = Convert.ToInt32(Settings.Default.EPGGridChannelsPerPage);

            // Re-create querystring
            int GridChannelIndex = ((page - 1) * ChannelsPerPage) + 1;

            qsParams.Remove("epggridchannelindex");
            qsParams.Add("epggridchannelindex", GridChannelIndex.ToString());

            return ViewEPGGrid();
        }
        private bool EPGGridChannelSetAbsolute(bool setZero, bool setHighest)
        {
            // get old state
            if (!lastEPGQueryStringForIP.ContainsKey(currentClientIP))
            {
                txtResponse += "Old link, click <a href='viewepggrid'>here</a> to refresh.";
                return true;
            }

            // get old state
            qsParams = lastEPGQueryStringForIP[currentClientIP];

            int ChannelsPerPage = Convert.ToInt32(Settings.Default.EPGGridChannelsPerPage);

            // GRID CHANNEL
            int GridChannelIndex;
            if (!Int32.TryParse(qsParams["epggridchannelindex"], out GridChannelIndex))
            {
                txtResponse += "Bad channel index.";
                return false;
            }

            // Re-create querystring
            if (setZero)
                GridChannelIndex = 1;
            if (setHighest)  // To the bottom - ensure a full page
                GridChannelIndex = (EPGManager.EPGDisplayedTVChannels.Count - ChannelsPerPage + 1);

            qsParams.Remove("epggridchannelindex");
            qsParams.Add("epggridchannelindex", GridChannelIndex.ToString());

            return ViewEPGGrid();
        }
        private bool EPGGridTimeWindowShiftByMinutes(int minutesToShift)
        {
            // get old state
            if (!lastEPGQueryStringForIP.ContainsKey(currentClientIP))
            {
                txtResponse += "Old link, click <a href='viewepggrid'>here</a> to refresh.";
                return true;
            }

            // get old state
            qsParams = lastEPGQueryStringForIP[currentClientIP];

            DateTime centerTime;
            string dateandtime = qsParams["datecomponent"] + " " + qsParams["epggridcentertime"];
            if (!DateTime.TryParse(dateandtime, out centerTime))
            {
                txtResponse += "Bad epg grid time parameter.";
                return false;
            }
            centerTime = DateTime.SpecifyKind(centerTime, DateTimeKind.Local);  // This is important as otherwise it's mucked around with and treated as non-local

            // Too early?
            if ((minutesToShift > 0) || (centerTime > DateTime.Now.AddHours(-8)))
            {
                centerTime = centerTime.AddMinutes(minutesToShift);
            }

            // Re-create querystrings
            qsParams.Remove("datecomponent");
            qsParams.Add("datecomponent", Functions.japDateFormat(centerTime));

            qsParams.Remove("epggridcentertime");
            qsParams.Add("epggridcentertime", centerTime.Hour.ToString("D2") + ":" + centerTime.Minute.ToString("D2"));

            return ViewEPGGrid();
        }
        private bool ViewEPGGrid()
        {
            // Set current EPG type.
            Settings.Default.CurrentEPGType = "grid";

            int ChannelsPerPage = Convert.ToInt32(Settings.Default.EPGGridChannelsPerPage);
            txtPageTitle = "TV Guide";

            if (!EPGManager.isWMCOnline)
            {
                txtResponse += "EPG cannot be configured.  You may need to enable this option in settings on the server.";
                return false;
            }

            // Always display date dropdowns
            string requestForm = FileCache.ReadSkinTextFile("page_viewepg_grid.htm");


            // Default parameters
            if (!qsParams.HasParameter("epggridchannelindex"))
            {
                qsParams.Add("epggridchannelindex", "1");
            }


            // GRID DATE / DEFAULT
            DateTime epgDate;
            if (!(qsParams.HasParameter("datecomponent")))
                qsParams.Add("datecomponent", DateTime.Now.Date.ToShortDateString());

            if (!DateTime.TryParse(qsParams["datecomponent"], out epgDate))
            {
                txtResponse += "Bad parameters - check form.";
                return false;
            }

            // GRID TIME WINDOW / DEFAULT
            DateTime centerTime = new DateTime();
            if (!qsParams.HasParameter("epggridcentertime"))
            {
                // TODO:  Closest to now?
                qsParams.Add("epggridcentertime", DateTime.Now.TimeOfDay.Hours.ToString("D2") + ":00");
            }
            string dateandtime = qsParams["datecomponent"] + " " + qsParams["epggridcentertime"];
            if (!DateTime.TryParse(dateandtime, out centerTime))
            {
                txtResponse += "Bad epg grid start time parameter.";
                return false;
            }

            // Set up time window
            centerTime = centerTime.ToUniversalTime();
            DateTime startWindow = centerTime.AddMinutes(Convert.ToInt32(0 - Settings.Default.EPGGridPreviewWindowSize));
            DateTime endWindow = startWindow.AddMinutes(EPGManager.TimespanMinutes);
            CommonEPG.DateRange timeWindow = new CommonEPG.DateRange(startWindow, endWindow);

            // GRID CHANNEL
            int GridChannelIndex;
            if (!Int32.TryParse(qsParams["epggridchannelindex"], out GridChannelIndex))
            {
                Functions.WriteLineToLogFile("Grid: Bad channel index. (" + qsParams["epggridchannelindex"] + ")");
                GridChannelIndex = 1;
                qsParams.Add("epggridchannelindex", "1");
            }

            // Process form at top (always)
            processRequestForm(ref requestForm, "", epgDate, 0, centerTime.ToLocalTime(), GridChannelIndex, "");
            StringBuilder txtEPGList = new StringBuilder(2500);

            // Begin Grid Code
            List<CommonEPG.TVProgramme> myProgs;
            DateTime timeCounter;
            txtEPGList.AppendLine("<div class='epggridcontainer'>");

            // COLUMN HEADER ROW - Times:  18:30, 19:00, 19:30, etc.  And left/right nav arrows
            txtEPGList.AppendLine(Functions.DivTag("epggridtopleftspacer"));
            if (GridChannelIndex < 2)
                txtEPGList.Append("<img id=\"epgchanneluparrow\" src=\"/static/images/epgchanneluparrow_disabled.png\" />");
            else
                txtEPGList.Append("<a href=\"/epgnavup\"><img id=\"epgchanneluparrow\" src=\"/static/images/epgchanneluparrow.png\" /></a>");
            txtEPGList.Append("</div>");  // Blank space at top left
            DateTime timeHeadingCounter = timeWindow.StartTime;
            double markerPeriodMins = 30;
            bool isAlternateColour = false;
            int numberOfMarkers = Convert.ToInt32((EPGManager.TimespanMinutes / markerPeriodMins));
            for (int counter = 1; counter <= numberOfMarkers; counter++)
            {
                string styleString = isAlternateColour ? "epggridcolumnheader maincolor" : "epggridcolumnheader altcolor";
                txtEPGList.AppendLine(Functions.DivTag(styleString, (markerPeriodMins * EPGManager.EPGScaleFactor) - 2, ""));
                txtEPGList.Append("<p class=\"epggridcolumnheadertext\">");
                if (counter == 1)
                {
                    // Nav left arrow?
                    if (centerTime >= DateTime.Now.ToLocalTime().AddHours(-8))
                        txtEPGList.Append("<a href=\"/epgnavleft\"><img style='float: left; margin-right: 2px;' src=\"/static/images/epgtimeleftarrow.png\" /></a>");
                }
                txtEPGList.Append(timeHeadingCounter.ToLocalTime().ToShortTimeString());
                if (counter == numberOfMarkers) txtEPGList.Append("<a href=\"/epgnavright\"><img style='float: right; margin-right: 2px;' src=\"/static/images/epgtimerightarrow.png\" /></a>");
                txtEPGList.Append("</p></div>");

                timeHeadingCounter = timeHeadingCounter.AddMinutes(markerPeriodMins);
                isAlternateColour = !isAlternateColour;
            }

            // Channel Rows
            CommonEPG.TVService currentChannel;
            List<CommonEPG.TVService> cacheEPGDisplayedChannels = EPGManager.EPGDisplayedTVChannels;

            // Get all progs in advance
            List<string> channelIDs = new List<string>();
            for (int chanCounter = GridChannelIndex; chanCounter < (GridChannelIndex + ChannelsPerPage); chanCounter++)  // e.g. 1,2,3,4,5
            {
                try
                {
                    currentChannel = (CommonEPG.TVService)cacheEPGDisplayedChannels[chanCounter - 1];
                    channelIDs.Add(currentChannel.UniqueId);
                }
                catch // channel not found / out of range / end of grid
                { break; }
            }

            // Get Programmes 
            myProgs = EPGManager.mcData.GetTVProgrammes(timeWindow, channelIDs.ToArray(), false, TVProgrammeType.All);
            if (myProgs == null)
            {
                txtResponse += "No programs found.";
                return false;
            }

            // Go through channels
            foreach (string strChannelID in channelIDs)
            {
                currentChannel = EPGManager.AllTVChannels[strChannelID];

                // CHANNEL HEADING
                txtEPGList.Append("\r\n<div class=\"epggridchannelrow\">");
                txtEPGList.Append("\r\n" + Functions.DivTag("epggridchannelheading"));
                txtEPGList.Append("<table class=\"epggridchannelheadingcontents\"><tr>");

                // CHANNEL NUMBER?
                txtEPGList.Append("<td class=\"epggridchannelheadingchannelnumber\">");
                if (Settings.Default.EPGShowChannelNumbers)
                    txtEPGList.Append(currentChannel.ChannelNumberString());
                else
                    txtEPGList.Append("&nbsp;");
                txtEPGList.Append("</td>");

                // CHANNEL LOGO
                txtEPGList.Append("<td class=\"epggridchannelheadingchannellogo\">" + currentChannel.HTMLForLogoImageOrCallsign("", true));
                txtEPGList.Append("</td></tr></table>");
                txtEPGList.Append("</div>");


                // GET PROGRAMMES
                bool matchedChannel = false;
                bool willRecord = false;
                timeCounter = timeWindow.StartTime;
                string txtDisplayText;
                foreach (CommonEPG.TVProgramme tvp in myProgs)
                {
                    if (tvp.ServiceID == strChannelID)
                    {
                        matchedChannel = true;

                        // Calculate minutes of the program remaining at this time...
                        TimeSpan timeRemaining = (tvp.StopTimeDT() - timeCounter);
                        double minsRemaining = timeRemaining.TotalMinutes;

                        // DEBUG Functions.WriteLineToLogFile(timeCounter.ToShortTimeString() + ": (" + currentChannel.Callsign + "): " + tvp.Title + " " + tvp.StartTimeDT().ToShortTimeString() + "-" + tvp.StopTimeDT().ToShortTimeString() + ", TR: " + timeRemaining.TotalSeconds + "sec");

                        // Program has already ended - it's just a MCE precision thing, so ignore
                        if (timeRemaining.TotalSeconds < 60) continue; // trivial amount

                        // Program has not started - insert a filler first
                        if (tvp.StartTimeDT() > timeCounter)
                        {
                            TimeSpan timeUntil = (tvp.StartTimeDT() - timeCounter);
                            if (timeUntil.TotalSeconds > 60)  // trivial amount
                            {
                                double minsUntil = timeUntil.TotalMinutes;
                                double transBWidth = Math.Round((minsUntil * EPGManager.EPGScaleFactor), 0);

                                txtEPGList.Append(Functions.DivTag("epggridcell epggridfiller", transBWidth - 2, ""));  // -1 to allow for border (A 1px border is 0.5px on each side)
                                txtEPGList.Append("&nbsp;</div>");

                                // Increase the time counter
                                timeCounter = tvp.StartTimeDT();
                            }
                        }

                        // THE CELL

                        // Calculate cell width
                        // Crop if it goes past end of window
                        if (tvp.StopTimeDT() > timeWindow.StopTime)
                            minsRemaining = minsRemaining - (tvp.StopTimeDT() - timeWindow.StopTime).TotalMinutes;

                        double transWidth = Math.Round((minsRemaining * EPGManager.EPGScaleFactor), 0);

                        // Display Text
                        string txtFullTitle = tvp.Title + "<br />" + tvp.ToPrettyStartStopLocalTimes();

                        if (transWidth > 80)
                        {
                            txtDisplayText = txtFullTitle;
                        }
                        else if (transWidth > 50)
                        {
                            txtDisplayText = tvp.Title;
                        }
                        else
                            txtDisplayText = "...";

                        // Extra shading?
                        string txtExtraShadingStyle = "";
                        if (Settings.Default.ShowBackgroundColoursInEPG)
                        {
                            switch (tvp.ProgramType)
                            {
                                case CommonEPG.TVProgrammeType.None:
                                    break;

                                case CommonEPG.TVProgrammeType.All:
                                    break;

                                default:
                                    txtExtraShadingStyle = " epggridcellprogrammetype" + tvp.ProgramType.ToString().ToLowerInvariant();
                                    break;
                            }
                        }

                        // Commit display text
                        txtEPGList.AppendLine(Functions.DivTag("epggridcell" + txtExtraShadingStyle, transWidth - 2, txtFullTitle.Replace("<br />", "  |  ") + " | " + HttpUtility.HtmlEncode(tvp.Description))); // -1 for border.

                        // Record dot?
                        RPRecording rec = tvp.Recording();
                        willRecord = Settings.Default.ShowEPGRecordingDots ? (rec != null) : false;

                        if (willRecord)
                        {
                            string className = tvp.Recording().IsRecurring() ? "recorddot_series" : "recorddot_onetime";
                            txtEPGList.Append("<div id=\"epggridrecorddot\" class=\"" + className + "\">&nbsp;</div>");
                        }
                        else if (Settings.Default.ShowEPGRecommendedMovies && tvp.IsRecommended())
                        {
                            txtEPGList.Append("<div id=\"epggridrecommendedstar\">&nbsp;</div>");
                        }

                        // LINK TO VIEW PROGRAMME
                        txtEPGList.Append("<div class=\"epggridcelltext\">");
                        txtEPGList.Append(Functions.LinkTagOpen("viewepgprogramme?programmeid=" + tvp.Id));
                        txtEPGList.Append(txtDisplayText + "</a></div></div>");

                        // Increase the time counter...
                        timeCounter = timeCounter.AddMinutes(minsRemaining);
                    }
                    else
                    {
                        if (matchedChannel) break;
                    }
                }


                // Were there any listings?
                if (!matchedChannel)
                {
                    txtEPGList.Append(Functions.DivTag("epggridcell nolistings", (EPGManager.EPGScaleFactor * (timeWindow.Span).TotalMinutes) - 2, "") + "&nbsp;&nbsp;&nbsp;No listings for this channel.</div>");
                }

                txtEPGList.Append("</div>");  // end of channel row


            }  // loop through channels

            // Footer 
            txtEPGList.AppendLine(Functions.DivTag("epggridbottomleftspacer"));
            if (GridChannelIndex > (EPGManager.EPGDisplayedTVChannels.Count - ChannelsPerPage))
                txtEPGList.Append("<img id=\"epgchanneldownarrow\" src=\"/static/images/epgchanneldownarrow_disabled.png\" />");
            else
                txtEPGList.Append("<a href=\"/epgnavdown\"><img id=\"epgchanneldownarrow\" src=\"/static/images/epgchanneldownarrow.png\" /></a>");
            txtEPGList.Append("</div>");

            // Page Count
            double numPages = Math.Floor((Convert.ToDouble(EPGManager.EPGDisplayedTVChannels.Count) / Convert.ToDouble(ChannelsPerPage)));
            if ((Convert.ToDouble(EPGManager.EPGDisplayedTVChannels.Count) % Convert.ToDouble(ChannelsPerPage)) > 0) numPages++;
            double highestChannelDisplayed = Convert.ToDouble(GridChannelIndex + ChannelsPerPage - 1);
            double pageNum = Math.Floor(highestChannelDisplayed / Convert.ToDouble(ChannelsPerPage));
            if ((highestChannelDisplayed % Convert.ToDouble(ChannelsPerPage)) > 0) pageNum++;

            txtEPGList.Append("<div id=\"epggridpagecount\">Page " + pageNum.ToString() + " of " + numPages.ToString() + "</div>");

            // Page jump links
            StringBuilder epgPageJumpLinks = new StringBuilder();
            if (numPages > 0)
            {
                int pageJumpCounterAdd = Convert.ToInt32(Math.Ceiling((double)numPages / 10.0));
                int pageJumpCounter = pageJumpCounterAdd;
                while (pageJumpCounter <= numPages)
                {
                    epgPageJumpLinks.Append(" ");
                    if (pageJumpCounter != pageNum)
                        epgPageJumpLinks.Append(Functions.LinkTagOpen("/epgjumptopage?page=" + pageJumpCounter.ToString()));
                    epgPageJumpLinks.Append(pageJumpCounter.ToString());
                    if (pageJumpCounter != pageNum)
                        epgPageJumpLinks.Append("</a>");

                    pageJumpCounter = pageJumpCounter + pageJumpCounterAdd;
                }
            }
            requestForm = requestForm.Replace("**EPGPAGEJUMPLINKS**", epgPageJumpLinks.ToString());


            txtEPGList.Append("</div>"); // end footer row

            requestForm = requestForm.Replace("**EPGGRID**", txtEPGList.ToString());
            txtResponse += requestForm;
            requestForm = null;

            //... and finally store the querystring to keep state
            storeEPGQuerystringForCurrentClient();

            return true;

        }
        private bool ViewEPGProgramme()
        {
            txtPageTitle = "Programme Details";

            if (!EPGManager.isWMCOnline)
            {
                txtResponse = "No EPG is configured.  You will need to enable this option in settings on the server and ensure that you have a valid provider of EPG information set up.";
                return false;
            }

            if (!(qsParams.HasParameter("programmeid")))
            {
                txtResponse += "No programme ID parameter specified.";
                return false;
            }

            bool getFromFile = (qsParams.HasParameter("getfromrecordedtvfiles"));
            string programmeID = qsParams["programmeid"];

            if (programmeID == "-10")
            {
                txtResponse = "Details of manual recordings cannot currently be viewed within Remote Potato.";
                return true;
            }

            CommonEPG.TVProgramme tvp = null;
            if (getFromFile)
                RecTV.Default.RecordedTVProgrammes.TryGetValue(programmeID, out tvp);
            else
                tvp = EPGManager.mcData.GetTVProgramme(programmeID);

            if (tvp == null)
            {
                txtResponse += "Could not find this show in the EPG.";
                return false;
            }

            CommonEPG.TVService tvs = tvp.TVService();
            if ( (!getFromFile) & ( tvs == null) )
            {
                txtResponse += "Could not find the TV channel for this programme in the EPG.";
                return false;
            }

            // Get table for display
            string tblShow = FileCache.ReadSkinTextFile("page_show.htm");
            string tagThumbnails = HTMLHelper.imgTagDefault();
            if (getFromFile)
            {
                tagThumbnails = HTMLHelper.imgTagRecordedTVProgramme(tvp);
            }

            if (Settings.Default.ShowChannelLogos)
                tagThumbnails += tvs.HTMLForLogoImageOrCallsign("showchannellogo", false);

            tblShow = tblShow.Replace("**THUMBNAIL**", tagThumbnails);
            tblShow = tblShow.Replace("**STARTTIME**", tvp.ToPrettyDayNameAndDate() + ", " + tvp.ToPrettyStartStopLocalTimes());
            tblShow = tblShow.Replace("**TITLE**", tvp.Title);
            tblShow = tblShow.Replace("**EPISODETITLE**", tvp.EpisodeTitle);
            tblShow = tblShow.Replace("**DESCRIPTION**", tvp.Description);

            // Original Air Date
            string strOriginalAirDate = "";
            if (tvp.OriginalAirDate > 0)
            {
                DateTime dtOriginalAirDate = tvp.OriginalAirDateDT();
                strOriginalAirDate = "Original Air Date: " + dtOriginalAirDate.ToPrettyDayNameAndDate();
                if (dtOriginalAirDate.ToLocalTime().Year != DateTime.Now.Year)
                    strOriginalAirDate += " " + dtOriginalAirDate.ToLocalTime().Year.ToString();
            }
            tblShow = tblShow.Replace("**ORIGINALAIRDATE**", strOriginalAirDate );

            
            if (tvs != null)
                tblShow = tblShow.Replace("**CHANNEL**", tvs.Callsign);
            else
                tblShow = tblShow.Replace("**CHANNEL**", "");

            // RATINGS
            // Star Rating
            StringBuilder sbStarRating = new StringBuilder();
            int starCounter = tvp.StarRating;
            if (tvp.StarRating > 0)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (starCounter > 1) // full star
                        sbStarRating.Append("<div class=\"showstarrating_star_on\"> </div>");
                    else if (starCounter == 1)  // half star
                        sbStarRating.Append("<div class=\"showstarrating_star_half\"> </div>");
                    else
                        sbStarRating.Append("<div class=\"showstarrating_star_off\"> </div>");

                    starCounter = starCounter - 2;
                }
                sbStarRating.Append("<div class=\"showstarrating_endstars\"> </div>");
            }
            tblShow = tblShow.Replace("**STARRATING**", sbStarRating.ToString());
            // MPAA Rating
            string txtMPAARating;
            if (!string.IsNullOrEmpty(tvp.MPAARating))
                txtMPAARating = "<br />MPAA Rating: " + tvp.MPAARating;
            else
                txtMPAARating = "";
            tblShow = tblShow.Replace("**GUIDANCERATING**", txtMPAARating);

            // PROGRAMME TYPE IMAGE
            StringBuilder sbShowProgrammeType = new StringBuilder();
            switch (tvp.ProgramType)
            {
                case CommonEPG.TVProgrammeType.None:
                    break;

                case CommonEPG.TVProgrammeType.All:
                    break;

                default:
                    sbShowProgrammeType.Append(Functions.imgTag("/static/images/showtype" + tvp.ProgramType.ToString().ToLowerInvariant() + ".png", "showprogrammetypeimg"));
                    break;
            }
            tblShow = tblShow.Replace("**SHOWTYPE**", sbShowProgrammeType.ToString());

            // PROGRAMME EXTRA INFO  (series, repeat, etc.)
            StringBuilder sbShowInfo = new StringBuilder();
            if (tvp.IsSeries) sbShowInfo.Append("Series, ");
            if (!tvp.IsFirstShowing) sbShowInfo.Append("Repeat, ");
            if (tvp.IsHD) sbShowInfo.Append("HD, ");
            if (tvp.HasSubtitles) sbShowInfo.Append("Subtitles, ");
            if (sbShowInfo.ToString().EndsWith(", "))
                sbShowInfo.Remove(sbShowInfo.Length - 2, 2);
            tblShow = tblShow.Replace("**INFORMATION**", sbShowInfo.ToString());


            // Is show from a file
            if (getFromFile)
            {
                tblShow = tblShow.Replace("**RECORDINGTYPE**", "This information is retrieved from a recorded TV file.");

                string txtDeleteLink = string.IsNullOrEmpty(tvp.Filename) ? "" :
                    " <a " + Functions.LinkConfirmClick("Are you sure?  This will permanently delete the file from disk.") + " href='deletefile64?filename=" + HttpUtility.UrlEncode( Functions.EncodeToBase64(tvp.Filename) )  + "'>" + "(Delete Show)</a>";
                
                tblShow = tblShow.Replace("**RECORDINGSTATE**", "This show was recorded." + txtDeleteLink);

                StringBuilder sbStreamLinks = new StringBuilder(250);
                sbStreamLinks.AppendLine("Stream show: <a href=\"streamprogramme?quality=0&framesize=0.32&programmeid=" + tvp.Id.ToString() + "\">low</a> | ");
                sbStreamLinks.AppendLine("<a href=\"streamprogramme?quality=1&framesize=0.4&programmeid=" + tvp.Id.ToString() + "\">normal</a> | ");
                sbStreamLinks.AppendLine("<a href=\"streamprogramme?quality=2&framesize=0.5&programmeid=" + tvp.Id.ToString() + "\">medium</a> | ");
                sbStreamLinks.AppendLine("<a href=\"streamprogramme?quality=3&framesize=0.5&programmeid=" + tvp.Id.ToString() + "\">high</a>");
                tblShow = tblShow.Replace("**STREAMLINK**", sbStreamLinks.ToString() ); // no streaming
            }
            else
            {

                tblShow = tblShow.Replace("**STREAMLINK**", ""); // no streaming

                // is show being recorded?
                RPRecording rec = tvp.Recording();
                if (rec != null)
                {
                    RPRequest rq = rec.Request();

                    // Info about the recording state & optional link to cancel recording.
                    string txtAboutShowRecording = rec.State.ToPrettyString();
                    
                    // If it's not already happened
                    if (
                        (rec.State == RPRecordingStates.Scheduled) ||
                        (rec.State == RPRecordingStates.Recording))
                    {
                        // Link to cancel this recording
                        txtAboutShowRecording += " <a " + Functions.LinkConfirmClick("Are you sure?  This will cancel the recording.") + " href='cancelrecording?recordingid=" + rec.Id.ToString() + "'>" + "(Cancel Recording)</a>";
                    }

                    // And, if a series, link to Cancel series
                    if (rec.IsRecurring())
                        txtAboutShowRecording += " <a " + Functions.LinkConfirmClick("Are you sure?  This will cancel the whole " + rec.RequestType.ToString() + " request.") + " href='cancelseriesrequest?requestid=" + rq.ID.ToString() + "'>" + "(Cancel Series)</a>";


                    // Commit ABOUT text
                    tblShow = tblShow.Replace("**RECORDINGSTATE**", txtAboutShowRecording);

                    // Info about recording request type: Series recording / etc
                    string txtRecordingTypeInfo = "";
                    if (rq != null)
                    {
                        txtRecordingTypeInfo += "This is a ";
                        if (rec.IsRecurring())
                        {
                            txtRecordingTypeInfo += "<a href='viewseriesrequest?requestid=" + rq.ID.ToString() + "'>";
                            txtRecordingTypeInfo += rq.RequestType.ToString().ToLower() + " recording.";
                            txtRecordingTypeInfo += "</a>";
                        }
                        else
                        {
                            txtRecordingTypeInfo += rq.RequestType.ToString().ToLower() + " recording.";
                        }

                        // One time recording - link to record series
                        if (rq.RequestType == RPRequestTypes.OneTime)
                        {
                            if (tvp.IsSeries)
                            {
                                RecordingRequest rrSeries2 = new RecordingRequest(long.Parse(programmeID), SeriesRequestSubTypes.ThisChannelAnyTime);
                                txtRecordingTypeInfo += " <a href=\"recordshow_series?queueid=" + RecordingQueue.AddToQueue(rrSeries2) + "\">(Record Series)</a>";
                            }
                        }

                    }
                    else
                        txtRecordingTypeInfo += "";


                    tblShow = tblShow.Replace("**RECORDINGTYPE**", txtRecordingTypeInfo);

                }
                else  // SHOW IS NOT SCHEDULED TO RECORD
                {
                    tblShow = tblShow.Replace("**RECORDINGTYPE**", "");

                    // Link to record - only if in the future
                    string txtRecordingState;
                    if (!tvp.HasEndedYet())  // (extension method)
                    {
                        txtRecordingState = "This show will not be recorded. ";

                        if (tvs == null)
                        {
                            txtRecordingState += "<br />(cannot record - no matching channel found)";
                        }
                        else
                        {
                            RecordingRequest rr = new RecordingRequest(long.Parse(programmeID));
                            txtRecordingState += " <a href=\"recordfromqueue?queueid=" + RecordingQueue.AddToQueue(rr) + "\">(Record Show)</a>";

                            if (tvp.IsSeries)
                            {
                                if (tvp.HasSeriesRequest())
                                {
                                    RPRequest req = tvp.SeriesRequest();

                                    txtRecordingState += " <a href='viewseriesrequest?requestid=" + req.ID.ToString() + "'>";
                                    txtRecordingState += "(Series Info)";
                                    txtRecordingState += "</a>";
                                }
                                else
                                {
                                    RecordingRequest rrSeries = new RecordingRequest(long.Parse(programmeID), SeriesRequestSubTypes.ThisChannelAnyTime);
                                    txtRecordingState += " <a href=\"recordshow_series?queueid=" + RecordingQueue.AddToQueue(rrSeries) + "\">(Record Series)</a>";
                                }
                            }
                        }
                    }
                    else
                    {
                        txtRecordingState = "This show was not recorded.";
                    }

                    tblShow = tblShow.Replace("**RECORDINGSTATE**", txtRecordingState);
                } // end if recording else
            }  // end if not get from file


            // Commit to displaying show info
            txtResponse += tblShow;

            return true;

        }
        private bool StreamVideo()
        {
            string FN;
            if (qsParams.HasParameter("FN"))
            {
                string qsFN = qsParams["FN"];
                FN = HttpUtility.UrlDecode(qsFN);
                FN = Functions.DecodeFromBase64(FN, Encoding.UTF8);  // http uses UTF8 encoding
            }
            else
            {
                return false;
            }

            TVProgramme tvp = EPGManager.VideoFileToTVProgramme(FN);
            return StreamVideoUsingTVProgramme(tvp, 1, 0, 320, 240);
        }
        
        private bool StreamRecordedProgramme()
        {
            if (!(qsParams.HasParameter("programmeid")))
            {
                txtResponse += "No programme ID parameter specified.";
                return false;
            }

            if (!(qsParams.HasParameter("quality")))
            {
                txtResponse += "No quality parameter specified.";
                return false;
            }

            string programmeID = qsParams["programmeid"];
            string strQuality = qsParams["quality"];
            int Quality;

            if (!Int32.TryParse(strQuality, out Quality))
            {
                txtResponse += "Invalid quality parameter specified.";
                return false;
            }

            double StartAt;
            string strStartAt = qsParams["startat"];
            if (string.IsNullOrEmpty(strStartAt))
                StartAt = 0.0;
            else
            {
                if (!Double.TryParse(strStartAt, NumberStyles.AllowDecimalPoint, CultureInfo.GetCultureInfo("EN-US"), out StartAt))
                {
                    StartAt = 0.0;
                }
            }


            double frameSize;
            string strFrameSize = qsParams["framesize"];
            if (string.IsNullOrEmpty(strFrameSize))
                frameSize = 0.26;
            else
            {
                if (!Double.TryParse(strFrameSize, NumberStyles.AllowDecimalPoint, CultureInfo.GetCultureInfo("EN-US"), out frameSize))
                {
                    frameSize = 0.26;
                }
            }
            if (frameSize > 1) frameSize = 1;
            if (frameSize < 0) frameSize = 0.26;

            // Player size
            double dPlayerWidth = (800 * frameSize);
            double dPlayerHeight = (600 * frameSize);
            int iPlayerWidth = Convert.ToInt32(dPlayerWidth);
            int iPlayerHeight = Convert.ToInt32(dPlayerHeight);
            if (iPlayerWidth < 100) iPlayerWidth = 100;
            if (iPlayerHeight < 80) iPlayerHeight = 80;

            // Get tvp
            CommonEPG.TVProgramme tvp = null;
            if (! RecTV.Default.RecordedTVProgrammes.TryGetValue(programmeID, out tvp))
            {
                txtResponse += "Could not find this show.";
                return false;
            }

            return StreamVideoUsingTVProgramme(tvp, Quality, StartAt, iPlayerWidth, iPlayerHeight);
        }
        private bool StreamVideoUsingTVProgramme(TVProgramme tvp, int Quality, double StartAt, int iPlayerWidth, int iPlayerHeight)
        {
            // Get table for display
            string tblShow = FileCache.ReadSkinTextFile("stream_show.htm");

            txtPageTitle = tvp.Title;
            tblShow = tblShow.Replace("**EPISODETITLE**", tvp.EpisodeTitle);
            tblShow = tblShow.Replace("**DESCRIPTION**", tvp.Description);

            // Set up streaming parameters
            WTVProfileQuality qual = WTVProfileQuality.Normal;
            switch (Quality)
            {
                case 0:
                    qual = WTVProfileQuality.Low;
                    break;

                case 1:
                    qual = WTVProfileQuality.Normal;
                    break;

                case 2:
                    qual = WTVProfileQuality.Med;
                    break;

                case 3:
                    qual = WTVProfileQuality.High;
                    break;

                case 4:
                    qual = WTVProfileQuality.UltraHigh;
                    break;

                default:
                    qual = WTVProfileQuality.Low;
                    break;
            }

            WTVStreamingVideoRequest svrq = new WTVStreamingVideoRequest();
            svrq.FileName = tvp.Filename;
            svrq.Quality = qual;
            svrq.StartAt = TimeSpan.FromSeconds(StartAt);
            svrq.DeInterlaceMode = 1;
            WTVStreamingVideoResult rs = DSStreamingManager.Default.StartStreamer(svrq);

            if (rs.ResultCode == DSStreamResultCodes.OK)
            {
                // Wait for streaming to start
                Thread.Sleep(3500);

                // Compose stream URL
                StringBuilder sbStreamURL = new StringBuilder(30);
                sbStreamURL.Append(@"mms://");
                sbStreamURL.Append(Request.Url.Host);
                
                sbStreamURL.Append(":" + rs.Port.ToString() );
                sbStreamURL.Append(@"/tvshow.wmv");

                tblShow = tblShow.Replace("**STREAMURL**", sbStreamURL.ToString());
                tblShow = tblShow.Replace("**STREAMWIDTH**", iPlayerWidth.ToString());
                tblShow = tblShow.Replace("**STREAMHEIGHT**", iPlayerHeight.ToString());

                //tblShow = tblShow.Replace("**STOPSTREAMLINK**", "<a href=\"stopstreamprogramme?programmeid=" + tvp.Id.ToString() + "\">Stop streaming</a>");
                tblShow = tblShow.Replace("**STOPSTREAMLINK**", "");

                
            }
            else // NOT OK
            {
                string reasonCannotStream = "Cannot stream show - ";
                if (rs.ResultCode == DSStreamResultCodes.ErrorAlreadyStreaming)
                    reasonCannotStream += "the streamer is already running; please wait a small while and then try again.";
                else if (rs.ResultCode == DSStreamResultCodes.ErrorAC3CodecNotFound)
                    reasonCannotStream += "the required codecs are not installed; please download the Remote Potato streaming pack from www.fatattitude.com";
                else if (rs.ResultCode == DSStreamResultCodes.ErrorFileNotFound)
                    reasonCannotStream += "the file was not found";
                else if (rs.ResultCode == DSStreamResultCodes.ErrorExceptionOccurred)
                    reasonCannotStream += "an exception occurred: " + rs.ResultString;
                else
                    reasonCannotStream += "an error occurred. " + rs.ResultString;

                tblShow = tblShow.Replace("**STREAMOBJECT**", reasonCannotStream);
                tblShow = tblShow.Replace("**STOPSTREAMLINK**", "");
            }



            // Commit to displaying show info
            txtResponse += tblShow;

            return true;

        }
        private bool RecordFromQueue()
        {
            txtPageTitle = "Schedule Recording";

            if (
                (!qsParams.HasParameter("queueid"))
                )
            {
                txtResponse += "This link is invalid, no id number was specified.";
                return true;
            }

            int rrID;
            string strQUID = qsParams["queueid"];
            if (!Int32.TryParse(strQUID, out rrID))
            {
                txtResponse += "A non-numerical id number was specified.";
                return true;
            }

            // Get the recording request from the queue
            RecordingRequest newRR = null;
            if (!RecordingQueue.ExtractFromQueue(rrID, ref newRR))
            {
                txtResponse += "The referenced recording request has expired - you may have followed an old link or waited too long.";
                return true;
            }

            // Add standard amounts of padding / quality / etc...
            newRR.Postpadding = Convert.ToInt32(Settings.Default.DefaultPostPadding);
            newRR.Prepadding = Convert.ToInt32(Settings.Default.DefaultPrePadding);
            newRR.FirstRunOnly = Settings.Default.DefaultRecordFirstRunOnly;

            string schedDetails = "Attempting to schedule recording... ";    
                /*as follows:<br><ul><li>Recording Type: " +
                newRR.RequestTypeAsString + "</li><li>Show Name: " + newRR.ShowName + "</li><li>Channel Callsign: " +
                newRR.ChannelCallSign + "</li><li>Start Date/Time : " + newRR.StartTime.ToLocalTime().ToLongDateString() +
                ", " + newRR.StartTime.ToLocalTime().ToShortTimeString() +
                "</li></ul>"; */

            spoolMessage(schedDetails.Replace("<br>", Environment.NewLine)); // to screen / logfile
            txtResponse += "<br />" + schedDetails; // to browser
            string txtRecSummary = "";
            // Schedule the recording
            bool recResult = scheduleRecordingForHTML(newRR, out txtRecSummary);
            // Report the result
            txtResponse += recResult ? "<p class='recordsuccess'>" : "<p class='recordfailure'>";
            spoolMessage(txtRecSummary); // to screen / logfile
            txtResponse += txtRecSummary;
            txtResponse += "</p>"; // to browser

            return true;
        }
        private bool RecordSeries()
        {
            txtPageTitle = "Record Series";

            if (
            (!qsParams.HasParameter("queueid"))
            )
            {
                txtResponse += "This link is invalid, no id number was specified.";
                return true;
            }

            int rrID;
            if (!Int32.TryParse(qsParams["queueid"], out rrID))
            {
                txtResponse += "A non-numerical id number was specified.";
                return true;
            }

            // Get the recording request from the queue
            RecordingRequest newRR = null;
            if (!RecordingQueue.ExtractFromQueue(rrID, ref newRR))
            {
                txtResponse += "The referenced recording request has expired - you may have followed an old link or waited too long.";
                return true;
            }

            // Any parameters?
            if ((qsParams.Count) < 2)  // Due to the way QueryString class works, the Count will be 1 even if there are no actual name/value pairs
            {
                // Get Form
                string requestForm = FileCache.ReadSkinTextFile("page_recordshow_series.htm");
                // Alter form:
                processRequestForm(ref requestForm);
                requestForm = requestForm.Replace("**QUEUEID**", qsParams["queueid"]);

                // Display form
                txtResponse += "Record series:<br />";
                txtResponse += requestForm;
                requestForm = null;
                return true;
            }

            bool failedValidation = false;
            string failedValidationReason = "";
            int allowAlternateOptions = 0;
            int keepuntil = 0;

            // DOES IT HAVE ENOUGH PARAMETERS
            if (

                (!qsParams.HasParameter("allowalternateoptions")) |
                (!qsParams.HasParameter("keepuntil"))
                )
            {
                failedValidation = true;
                failedValidationReason += "Not all parameters were provided.<br>";
            }
            else
            {
                if (!Int32.TryParse(qsParams["allowalternateoptions"], out allowAlternateOptions))
                {
                    failedValidation = true;
                    failedValidationReason += "Invalid parameter: allow alternate options.";
                }

                if (!Int32.TryParse(qsParams["keepuntil"], out keepuntil))
                {
                    failedValidation = true;
                    failedValidationReason += "Invalid parameter: keepuntil.";
                }

            }

            // Passed validation?
            if (failedValidation)
            {
                txtResponse += "<p class='recorderror'>Error in recording request: " + failedValidationReason + "</p>";
                return true;
            }


            // VALIDATED ============

            // First run?
            newRR.FirstRunOnly = qsParams.HasParameter("firstrunonly");

            // Keep Until
            switch (keepuntil)
            {
                case 0:
                    newRR.KeepUntil = KeepUntilTypes.UntilUserDeletes;
                    break;

                case -2:
                    newRR.KeepUntil = KeepUntilTypes.UntilUserWatched;
                    break;

                case -1:
                    newRR.KeepUntil = KeepUntilTypes.UntilEligible;
                    break;

                case -3:
                    newRR.KeepUntil = KeepUntilTypes.LatestEpisodes;
                    break;
            }

            // Series Record Options
            switch (allowAlternateOptions)
            {
                case 0:
                    newRR.SeriesRequestSubType = SeriesRequestSubTypes.ThisChannelThisTime;  // not working?
                    break;

                case 1:
                    newRR.SeriesRequestSubType = SeriesRequestSubTypes.ThisChannelAnyTime;
                    break;

                case 2:   // any time on the first channel found; this is the best MC API can do
                    newRR.SeriesRequestSubType = SeriesRequestSubTypes.AnyChannelAnyTime;
                    break;

            }

            // Add standard padding
            newRR.Prepadding = Convert.ToInt32(Settings.Default.DefaultPrePadding);
            newRR.Postpadding = Convert.ToInt32(Settings.Default.DefaultPostPadding);
            newRR.FirstRunOnly = Settings.Default.DefaultRecordFirstRunOnly;

            // Add to the queue then immediately record it
            string newQueueID = RecordingQueue.AddToQueue(newRR);
            if (qsParams.HasParameter("queueid"))
                qsParams.Remove("queueid");
            qsParams.Add("queueid", newQueueID);
            return RecordFromQueue();
        }
        private void processRequestForm(ref string requestForm)
        {
            processRequestForm(ref requestForm, "", DateTime.Parse("1995-01-01"), 1, DateTime.Now, 0, "");
        }
        private void processRequestForm(ref string requestForm, string selectedChannelCallsign, DateTime selectedDate, int selectedDayPhase, DateTime selectedGridTimeStart, int EPGGridChannelIndex, string searchShowTitle)
        {
            // Date drop down
            requestForm = requestForm.Replace("**DATEDROPDOWN**", DateDropDown(selectedDate));
            requestForm = requestForm.Replace("**TIMEDROPDOWN**", TimeDropDown());

            // Insert default values 
            requestForm = requestForm.Replace("**DEFAULTPREPADDING**", Settings.Default.DefaultPrePadding.ToString());
            requestForm = requestForm.Replace("**DEFAULTPOSTPADDING**", Settings.Default.DefaultPostPadding.ToString());
            requestForm = requestForm.Replace("**DEFAULTSEARCHSPAN**", Settings.Default.DefaultSearchSpan.ToString());

            // EPG
            requestForm = requestForm.Replace("**EPGCHANNELDROPDOWN**", EPGChannelDropDown(selectedChannelCallsign));
            requestForm = requestForm.Replace("**EPGDATEDROPDOWN**", DateDropDown(selectedDate));
            requestForm = requestForm.Replace("**EPGDAYPHASEDROPDOWN**", EPGDayPhaseDropDown(selectedDayPhase));
            requestForm = requestForm.Replace("**EPGCENTERTIMEDROPDOWN**", EPGCenterTimeDropDown(selectedGridTimeStart));
            requestForm = requestForm.Replace("**EPGGRIDCHANNELINDEX**", EPGGridChannelIndex.ToString());

            // search
            requestForm = requestForm.Replace("**SEARCHTITLE**", searchShowTitle);
        }
        private bool ExcludeMarkedSectionOfHTMLFragment(ref string fragment, string token)
        {
            string startToken = "<!-- BEGIN" + token.ToUpper() + " -->";
            string endToken = "<!-- END" + token.ToUpper() + " -->";
            int findStart = 0;
            int findEnd = 0;
            findStart = fragment.IndexOf(startToken);
            findEnd = fragment.IndexOf(endToken);
            if (findEnd < 1) return false;
            if (findStart < 1) return false;
            string portionToExclude = fragment.Substring(findStart, (findEnd - findStart));
            Console.WriteLine("Exclude portion: |" + portionToExclude + "|");
            fragment = fragment.Remove(findStart, (findEnd - findStart));
            return true;
        }
        private string EPGChannelDropDown(string selectedValue)
        {
            string txtDropDown = "";

            txtDropDown += "<SELECT NAME='channelcallsign'>";

            foreach (CommonEPG.TVService tvs in EPGManager.EPGDisplayedTVChannels)
            {
                txtDropDown += "<OPTION VALUE='" + tvs.UniqueId + "'";
                if (tvs.UniqueId.ToLowerInvariant() == selectedValue.ToLowerInvariant()) txtDropDown += " selected='selected'";
                txtDropDown += ">";

                // Channel number?
                if (tvs.ChannelNumberString().Length > 0)
                    txtDropDown += tvs.ChannelNumberString() + " ";

                txtDropDown += tvs.Callsign + "</OPTION>";
            }

            txtDropDown += "</SELECT>";
            return txtDropDown;
        }
        private string DateDropDown(DateTime selectedValue)
        {
            string txtDropDown = "";

            txtDropDown += "<SELECT NAME='datecomponent'>";

            DateTime dateCounter = DateTime.Now;
            string valDate, prettyDate;
            while (dateCounter < DateTime.Now.AddDays(25))
            {
                valDate = dateCounter.Year.ToString() + "-" + dateCounter.Month.ToString("D2") + "-" + dateCounter.Day.ToString("D2");
                prettyDate = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedDayName(dateCounter.DayOfWeek) + " " + dateCounter.Day + " " + CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(dateCounter.Month);

                if (dateCounter.Date.Equals(DateTime.Now.Date))
                    prettyDate = "Today";

                if (dateCounter.Date.Equals(DateTime.Now.AddDays(1).Date))
                    prettyDate = "Tomorrow";

                txtDropDown += "<OPTION VALUE='" + valDate + "'";
                if (selectedValue.Date.Equals(dateCounter.Date)) txtDropDown += " selected='selected'";
                txtDropDown += ">" + prettyDate + "</OPTION>";

                dateCounter = dateCounter.AddDays(1);
            }

            txtDropDown += "</SELECT>";
            return txtDropDown;
        }
        private string TimeDropDown()
        {
            // Work out a suitable future record time to suggest
            DateTime inTwoHours = DateTime.Now.AddHours(1);
            DateTime suggestedRecordTime = DateTime.Parse(inTwoHours.Hour.ToString("D2") + ":" + inTwoHours.Minute.ToString("D2"));
            while (suggestedRecordTime.Minute % 5 != 0)
            {
                suggestedRecordTime = suggestedRecordTime.AddMinutes(1);
            }

            string txtDropDown = "";

            txtDropDown += "<SELECT NAME='timecomponent'>";

            DateTime timeCounter = DateTime.Parse("00:00");
            string valDate, prettyDate;
            while (!timeCounter.Equals(DateTime.Parse("23:55")))
            {
                valDate = timeCounter.ToShortTimeString();
                prettyDate = timeCounter.ToShortTimeString();

                txtDropDown += "<OPTION ";
                if (timeCounter.Equals(suggestedRecordTime))
                    txtDropDown += "selected='selected' ";
                txtDropDown += "VALUE='" + valDate + "'>" + prettyDate + "</OPTION>";

                timeCounter = timeCounter.AddMinutes(5);
            }

            txtDropDown += "</SELECT>";
            return txtDropDown;
        }
        private string EPGDayPhaseDropDown(int selectedValue)
        {
            string txtDropDown = "";
            txtDropDown += "<select name=\"epgdayphase\"><option value=\"1\"";
            if (selectedValue == 1) txtDropDown += " selected=\"selected\"";
            txtDropDown += ">Morning</option><option value=\"2\"";
            if (selectedValue == 2) txtDropDown += " selected=\"selected\"";
            txtDropDown += ">Afternoon</option><option value=\"3\"";
            if (selectedValue == 3) txtDropDown += " selected=\"selected\"";
            txtDropDown += ">Evening</option></select>";

            return txtDropDown;
        }
        private string EPGCenterTimeDropDown(DateTime selectedValue)
        {
            string txtDropDown = "";

            txtDropDown += "<SELECT NAME='epggridcentertime'>";

            DateTime timeCounter = DateTime.Parse("00:00");
            string valDate, prettyDate;
            bool matchedSelectedValue = false;
            for (int c = 0; c < 24; c++)
            {
                valDate = timeCounter.ToShortTimeString();
                prettyDate = timeCounter.ToShortTimeString();

                txtDropDown += "<OPTION ";
                if ((timeCounter.Hour == selectedValue.Hour))  // just match the hour
                {
                    txtDropDown += "selected='selected' ";
                    matchedSelectedValue = true;
                }
                txtDropDown += "VALUE='" + valDate + "'>" + prettyDate + "</OPTION>";

                timeCounter = timeCounter.AddMinutes(60);
            }

            if (!matchedSelectedValue)
            {
                txtDropDown += "<OPTION selected='selected' VALUE='" + DateTime.Now.ToShortTimeString() + "'>--:--</OPTION>";
            }

            txtDropDown += "</SELECT>";
            return txtDropDown;
        }
        private string AdditionalStyleLinks(List<string> AdditionalStyles)
        {
            string output = "";
            foreach (string txtStyleSheetName in AdditionalStyles)
            {
                output += "<LINK href=\"**SKINFOLDER**/" + txtStyleSheetName + ".css\" rel=\"stylesheet\" type=\"text/css\">";
            }
            return output;
        }
        #endregion



        #endregion

    }


    // Event arguments for status reports
    public class MessageEventArgs : EventArgs
    {
        private string msg;

        public MessageEventArgs(string messageData)
        {
            msg = messageData;
        }
        public string Message
        {
            get { return msg; }
            set { msg = value; }
        }
    }
}
