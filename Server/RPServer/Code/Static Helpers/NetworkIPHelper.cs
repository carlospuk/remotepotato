using System;
using System.Collections.Generic;
using System.Text;
using RemotePotatoServer.Properties;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Threading;

namespace RemotePotatoServer.Network
{
    public class IPHelper
    {

        public static ManualResetEvent allDone = new ManualResetEvent(false);
        const int BUFFER_SIZE = 1024;
        const int DefaultTimeout = 10 * 1000; // 10 sec timeout

        public IPHelper()
        {
            if (Settings.Default.LastGotPublicIP == null)
                Settings.Default.LastGotPublicIP = DateTime.Now.Subtract(TimeSpan.FromHours(1));
        }

        #region Detect IP Addresses
        TimeSpan minimumPublicIPUpdateInterval = TimeSpan.FromMinutes(10); // DYNDNS requires 10 minutes between calling CheckIP

        /// <summary>
        /// Returns Local IP used for Internet Communications by opening a socket to a named URL
        /// If that fails, it tries a secondary one
        /// Next it reads the LocalEndPoint on connection, and strips of port information.
        /// We do this in case we have multiple NICs to see which one goes out to Internet.
        /// </summary>
        /// <returns></returns>
        public string GetLocalIP()
        {
            bool bConnected;
            EndPoint ep;
            string sip;

            ep = null;
            TcpClient client = new TcpClient();
            try
            {
                client.Connect("www.google.com", 80);
                ep = client.Client.LocalEndPoint;
                client.Close();
                bConnected = true;
            }
            catch (Exception)
            {
                bConnected = false;
            }
            if (!bConnected)
            {
                try
                {
                    client.Connect("www.yahoo.com", 80);
                    ep = client.Client.LocalEndPoint;
                    client.Close();
                    bConnected = true;
                }
                catch (Exception)
                {
                    bConnected = false;
                }
            }


            if (!bConnected) return null;
            if (ep != null)
            {
                sip = ep.ToString();
                int end = sip.IndexOf(":");
                return (sip.Remove(end));
            }
            return null;
        }

        public event EventHandler<GetExternalIPEventArgs> QueryExternalIPAsync_Completed;
        public void QueryExternalIPAsync()
        {
            // DynDNS service requires 10 minutes between requests for public IP
            if (Settings.Default.LastGotPublicIP != null)
            {
                TimeSpan timeSinceLastUpdate = DateTime.Now.Subtract(Settings.Default.LastGotPublicIP);
                if (timeSinceLastUpdate < minimumPublicIPUpdateInterval)
                {
                    RaiseIPQueryCompletedEvent(false, false, Settings.Default.LastPublicIP);
                    return;
                }
            }

            // Getpublic IP
            try
            {
                // Create a HttpWebrequest object to the desired URL. 
                HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create("http://checkip.dyndns.org/");

                // Create an instance of the RequestState and assign the previous myHttpWebRequest object to its request field.  
                RequestState myRequestState = new RequestState();
                myRequestState.request = myHttpWebRequest;

                // Start the asynchronous request.
                IAsyncResult result = (IAsyncResult)myHttpWebRequest.BeginGetResponse(new AsyncCallback(ResponseCallback), myRequestState);

                // Timeout: if there is a timeout, the callback fires and the request becomes aborted
                ThreadPool.RegisterWaitForSingleObject(result.AsyncWaitHandle, new WaitOrTimerCallback(TimeoutCallback), myHttpWebRequest, DefaultTimeout, true);

                // The response came in the allowed time. The work processing will happen in the callback function.
                allDone.WaitOne();

                // Release the HttpWebResponse resource.
                if (myRequestState != null)
                    myRequestState.response.Close();
            }
            catch (Exception e)
            {
                Functions.WriteLineToLogFile("GetExternalIPAsync Exception raised");
                Functions.WriteExceptionToLogFile(e);
            }


        }

        #region Web Callbacks
        private void ResponseCallback(IAsyncResult asynchronousResult)
        {
            try
            {
                // Get RequestState object, then corresponding Webrequest
                RequestState myRequestState = (RequestState)asynchronousResult.AsyncState;
                HttpWebRequest myHttpWebRequest = myRequestState.request;
                myRequestState.response = (HttpWebResponse)myHttpWebRequest.EndGetResponse(asynchronousResult);

                // Read the response into a Stream object.
                Stream responseStream = myRequestState.response.GetResponseStream();
                myRequestState.streamResponse = responseStream;

                // Begin the Reading of the contents of the HTML page
                IAsyncResult asynchronousInputRead = responseStream.BeginRead(myRequestState.BufferRead, 0, BUFFER_SIZE, new AsyncCallback(ReadCallBack), myRequestState);
                return;
            }
            catch (WebException e)
            {
                Functions.WriteLineToLogFile("ResponseCallback Exception raised");
                Functions.WriteExceptionToLogFile(e);
            }
            allDone.Set();
        }
        private void ReadCallBack(IAsyncResult asyncResult)
        {
            try
            {

                RequestState myRequestState = (RequestState)asyncResult.AsyncState;
                Stream responseStream = myRequestState.streamResponse;
                int read = responseStream.EndRead(asyncResult);

                if (read > 0) // Not completed yet
                {
                    myRequestState.requestData.Append(Encoding.ASCII.GetString(myRequestState.BufferRead, 0, read));
                    IAsyncResult asynchronousResult = responseStream.BeginRead(myRequestState.BufferRead, 0, BUFFER_SIZE, new AsyncCallback(ReadCallBack), myRequestState);
                    return;
                }
                else // Ready to process
                {
                    if (myRequestState.requestData.Length > 1)
                    {
                        
                        string stringContent;
                        stringContent = myRequestState.requestData.ToString();
                        ProcessHTMLResponse(stringContent);
                    }

                    responseStream.Close();
                }

            }
            catch (WebException e)
            {
                Functions.WriteLineToLogFile("ReadCallback Exception raised");
                Functions.WriteExceptionToLogFile(e);
            }
            allDone.Set();

        }
        #endregion

        void ProcessHTMLResponse(string responseText)
        {
            string newIP = "";

            try
            {
                // Search for the ip in the html 
                int first = responseText.IndexOf("Address: ") + 9;
                int last = responseText.LastIndexOf("</body>");
                newIP = responseText.Substring(first, last - first);
            }
            catch (Exception ex)
            {
                // Do nothing
                Functions.WriteLineToLogFile("Error getting public IP address.");
                Functions.WriteExceptionToLogFile(ex);
            }

            // Store
            string oldIP = Settings.Default.LastPublicIP;
            Settings.Default.LastPublicIP = newIP;
            bool hasChanged = (! String.Equals(oldIP, newIP));
            Settings.Default.LastGotPublicIP = DateTime.Now;

            RaiseIPQueryCompletedEvent(hasChanged, true, newIP);   
        }
        void RaiseIPQueryCompletedEvent(bool hasChanged, bool didCheckInternet, string txtIP)
        {
            if (QueryExternalIPAsync_Completed != null)
                QueryExternalIPAsync_Completed(this, new GetExternalIPEventArgs(hasChanged, didCheckInternet, txtIP));
        }
        #endregion

        #region Helpers
        public class RequestState
        {
            // This class stores the State of the request.
            const int BUFFER_SIZE = 1024;
            public StringBuilder requestData;
            public byte[] BufferRead;
            public HttpWebRequest request;
            public HttpWebResponse response;
            public Stream streamResponse;

            public RequestState()
            {
                BufferRead = new byte[BUFFER_SIZE];
                requestData = new StringBuilder("");
                request = null;
                streamResponse = null;
            }
        }

        public class GetExternalIPEventArgs : EventArgs
        {

            public bool HasChanged { get; set; }
            public bool DidCheckInternet { get; set; }
            public string IP { get; set; }

            public GetExternalIPEventArgs(bool _ipChanged, bool _didCheckInternet, string _IP)
            {
                HasChanged = _ipChanged;
                IP = _IP;
                DidCheckInternet = _didCheckInternet;
            }

        }


        // Abort the request if the timer fires.
        private static void TimeoutCallback(object state, bool timedOut)
        {
            if (timedOut)
            {
                HttpWebRequest request = state as HttpWebRequest;
                if (request != null)
                {
                    request.Abort();
                }
            }
        }



        #endregion

    }

}
