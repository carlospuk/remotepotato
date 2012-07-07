using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using RemotePotatoServer.Properties;

namespace RemotePotatoServer
{

    /// <summary>
    /// RPWebServer runs on the processing thread and controls the HTTP listener
    /// and services requests.
    ///     
    /// </summary>
    public class RPWebServer
    {
        string serverName; // my name
        int serverPort; // my port
        string serverPath; // the path e.g. / or /secure/ 
        bool useAuthentication; // use auth
        HttpListener httpListener;  // the HTTP listener
        AutoResetEvent exitFlag;    // a thread-safe flag to indicate we should exit

        // Events
        public event EventHandler AccessDenied;

        // MainServer constructor
        public RPWebServer(string name, int port, string path, bool useAuth)
        {
            serverName = name;
            serverPort = port;
            serverPath = path;
            useAuthentication = useAuth;

        }


        // ThreadEntry is the starting point of the new thread created for listening for new HTTP requests
        public void Start()
        {
            // Initialize
            bool initResult = Initialization.Default.Initialize(true, true, true);

            if (!initResult)
            {
                Functions.WriteLineToLogFile("RPWebServer: Could not initialize - Initialize() return false.");
                return;
            }
            

            httpListener = new HttpListener();      // create the HTTP listeners
            exitFlag = new AutoResetEvent(true);   // create the exit flag and set it to false
            
            // Prefixes
            httpListener.Prefixes.Clear();
            httpListener.Prefixes.Add("http://+:" + serverPort.ToString() + serverPath);

            // Authentication
            if (useAuthentication)
                httpListener.AuthenticationSchemes = AuthenticationSchemes.Basic;
            else
                httpListener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;

            Functions.WriteLineToLogFile("Starting a web server on port " + serverPort.ToString() + serverPath + ".");

            // Start listening
            try
            {
                httpListener.Start();   // start listening for requests                
            }
            catch (Exception e)
            {
                Functions.WriteLineToLogFile("Webserver: Could not start:");
                Functions.WriteExceptionToLogFile(e);

                if (e.Message.ToLower().Contains("denied"))
                {
                    if (AccessDenied != null) AccessDenied(this, new EventArgs());
                }
            }
            exitFlag.Reset();

            // keep listening for requests until told to exit
            try
            {
                while (!exitFlag.WaitOne(0))   // check the status of the exit flag
                {

                    // begin listening for requests.  When a new request is received, the system will call the MainServer.ProcessHttpRequest static
                    // method asychronously (i.e. a new thread), but the system will let us know we got a request.
                    IAsyncResult result = httpListener.BeginGetContext(new AsyncCallback(RPWebServer.ProcessHttpRequest), httpListener);
                    // wait until we have a request, or the HttpListener was closed or aborted
                    result.AsyncWaitHandle.WaitOne();
                }  // goes back to While exitFlag...
            }
            catch (ObjectDisposedException)
            {
                Functions.WriteLineToLogFile("Exception listening on web server: server was disposed.");
            }
            catch (Exception ex)
            {
                Functions.WriteLineToLogFile("Exception listening on web server: ");
                Functions.WriteExceptionToLogFile(ex);
            }

            Functions.WriteLineToLogFile("Aborting web server " + serverName +  ".");
        }

        // stop the processing thread
        public void Stop()
        {
            try
            {
                if (httpListener != null)
                {
                    // check if we're listening for a request
                    if (httpListener.IsListening)
                    {
                        // tell the processing thread to exit
                        exitFlag.Set();
                        // stop listening and kill everything that may be in the queue
                        try
                        {
                            httpListener.Abort();
                        }
                        catch { }
                    }
                }
            }
            catch { }

            try
            {
                bool foo = Initialization.Default.UnInitialize();
            }
            catch (Exception ex)
            {
                Functions.WriteLineToLogFile("Webserver: Error uninitialising:");
                Functions.WriteExceptionToLogFile(ex);
            }

        }

        // ProcessHttpRequest is the async handler for new http requests
        // This function should do what it needs to do and then exit
        public static void ProcessHttpRequest(IAsyncResult result)
        {
            RequestProcessor rp = null;
            try   // (IAsyncResult)result will be invalid if we've killed the listener
            {
                HttpListener listener = (HttpListener)result.AsyncState;
                HttpListenerContext context = listener.EndGetContext(result);  // Call EndGetContext to complete the asynchronous operation.

                // Obtain a response object.
                HttpListenerResponse response = context.Response;

                // Create a new request processor
                rp = new RequestProcessor(context);

                rp.Run();
            }
            catch (System.ObjectDisposedException)
            {
                // Do nothing
            }
            catch (Exception ex)
            {
                Functions.WriteLineToLogFile("Process HttpRequest Exception:");
                Functions.WriteExceptionToLogFile(ex);
            }
            finally
            {
                if (rp != null)
                {
                    rp.Dispose();
                    rp = null;
                }
            }
        }


    }
    

}
