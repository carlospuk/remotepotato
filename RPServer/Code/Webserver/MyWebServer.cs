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
using RemotePotatoServer.Properties;
using CommonEPG;

namespace RemotePotatoServer
{
    class MyWebServer : IDisposable
    {
        public bool Active = false;
        private HttpListener myHttpListener;

        // Throttling of server poll time / milliseconds
        const int INITIAL_POLL_TIME = 500;
        const int FASTEST_POLL_TIME = 10;
        const int SLOWEST_POLL_TIME = 4000;  // longer delays webserver responses but saves CPU time

        // Constructor / Destructor
        public MyWebServer()
        {
            // Default values / init
            RequestProcessor.StatusReport += new EventHandler<MessageEventArgs>(requestProcessor_StatusReport);
        }
        public void Dispose()
        {
            if (Active)
            {
                StopServer();
            }
        }

        #region Start / Stop Webserver
        public event EventHandler AccessDenied;
        public void StartServer(int usePort)
        {
            if (Active) return;

            if (myHttpListener == null)
                myHttpListener = new HttpListener();

            try
            {
                // Start listening
                myHttpListener.Prefixes.Clear();
                myHttpListener.Prefixes.Add("http://+:" + Settings.Default.Port.ToString() + "/");
                if (Settings.Default.RequirePassword)
                    myHttpListener.AuthenticationSchemes = AuthenticationSchemes.Basic;
                
                // Try to make active
                myHttpListener.Start();
                IAsyncResult result = myHttpListener.BeginGetContext(new AsyncCallback(WebRequestCallback), myHttpListener);
                
                // Now we're active
                Active = true;
                ServerStatusChange(this, new MessageEventArgs("Server is running (v" + Functions.VersionText + ")"));
                spoolMessage("Web Server listening on port " + usePort.ToString());
            }
            catch (Exception e)
            {
                myHttpListener = null;

                Functions.WriteLineToLogFile("Webserver: Could not start:");
                Functions.WriteExceptionToLogFile(e);                
                    ServerStatusChange(this, new MessageEventArgs("Server is stopped (v" + Functions.VersionText + ")"));

                if (e.Message.ToLower().Contains("denied"))
                {
                    if (AccessDenied != null) AccessDenied(this, new EventArgs());
                }
            }
        }
        public void StopServer()
        {
            if (!Active) return;


            // Stop listening...
            // End
            spoolMessage("Web Server is stopped.");
            myHttpListener.Abort();
            myHttpListener = null;
            Active = false;  // Must go before status change event as it's used by the callback in Form1
            ServerStatusChange(this, new MessageEventArgs("Server is stopped (v" + Functions.VersionText + ")"));
        }
        #endregion

        #region New Web Server Responses
        protected void WebRequestCallback(IAsyncResult result)
        {
            if (myHttpListener == null) return;
            if (!Active) return;

            try
            {
                // Get out the context object
                HttpListenerContext context = myHttpListener.EndGetContext(result);

                // *** Immediately set up the next context
                myHttpListener.BeginGetContext(new AsyncCallback(WebRequestCallback), myHttpListener);

                RequestProcessor rp = new RequestProcessor(context);
                rp.UserAgentConnected += new EventHandler<MessageEventArgs>(rp_UserAgentConnected);
                rp.Run();
                rp.Dispose();
                rp = null;
                // done
                //RPStackSizeChanged(this, new GenericEventArgs<int>(--numberOfRequestProcessors));
            }
            catch (Exception ex)
            {
                Functions.WriteLineToLogFile("Exception thrown processing web server request: ");
                Functions.WriteExceptionToLogFile(ex);
            }

            return;
        }
        #endregion

        #region Web Server Responses
        #region RequestProcessor Callbacks / Stack
        public event EventHandler<MessageEventArgs> UserAgentConnected;
        void rp_UserAgentConnected(object sender, MessageEventArgs e)
        {
            UserAgentConnected(new object(), new MessageEventArgs(e.Message));
        }
        void requestProcessor_StatusReport(object sender, MessageEventArgs e) // Static
        {
            spoolMessage(e.Message);
        }
        #endregion

        #endregion

        // Event to send back Status Reports
        public event EventHandler<MessageEventArgs> StatusReport;
        private void spoolMessage(string msg)
        {
            Console.WriteLine(msg);
         
            if (StatusReport != null)
                StatusReport(this, new MessageEventArgs(msg));
        }
        // Event to send back server start/stop messages
        public event EventHandler<MessageEventArgs> ServerStatusChange;

    }



}
