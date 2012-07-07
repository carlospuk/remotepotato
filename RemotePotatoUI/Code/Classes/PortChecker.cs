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
    public class PortChecker
    {
        public static ManualResetEvent allDone = new ManualResetEvent(false);
        const int BUFFER_SIZE = 1024;
        const int DefaultTimeout = 20 * 1000; // 20 sec timeout for service  (service can take up to 5 seconds while it waits for its own socket to time out)

        public PortChecker()
        {
        }

        #region Simple Server
        TcpListener simpleServer = null;
        bool StartSimpleServer(int port)
        {
            simpleServer = new System.Net.Sockets.TcpListener(IPAddress.Any, port);
            
            try
            {
                // start listening
                Monitor.Enter(SSLock);

                Socket theSocket = simpleServer.Server;
                theSocket.LingerState = new System.Net.Sockets.LingerOption(true, 0);

                simpleServer.Start();
                simpleServer.BeginAcceptSocket(new AsyncCallback(acceptCallback), simpleServer);
                SimpleServerRunning = true;
                Monitor.Exit(SSLock);

                return true;
            }
            catch
            { }
            return false;
        }
        public void acceptCallback(IAsyncResult ar)
        {
            try
            {
                TcpListener listener = (TcpListener)ar.AsyncState;
                Socket handler = listener.EndAcceptSocket(ar);
                handler.Close();
            }
            catch { }  // ObjectDisposedException if socket is already closed

            FinalSocketShutdown();
        }

        bool SimpleServerRunning = false;
        object SSLock = new object();
        void StopSimpleServer()
        {
            if (!SimpleServerRunning) return;

            Monitor.Enter(SSLock);
            if (simpleServer!= null)
            {

                simpleServer.Stop(); // IMPORTANT: This triggers acceptCallback() if there's been no incoming connection, which in turn sets flags to FALSE, nullifies socket, etc.
            }

            FinalSocketShutdown(); // safety

            Monitor.Exit(SSLock);
        }

        #region OLD way - with socket directly
        /*
                Socket simpleServerSocket = null;
        bool OldStartSimpleServer(int port)
        {
            simpleServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            simpleServerSocket.ReceiveTimeout = DefaultTimeout;

            simpleServerSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, (int)1);

            // bind the listening socket to the port
            //IPAddress hostIP = Dns.GetHostAddresses(IPAddress.Any.ToString())[0];
            //IPEndPoint ep = new IPEndPoint(IPAddress.Any, port);
            //IPEndPoint ep = new IPEndPoint(IPAddress.Any, port);

            Network.IPHelper iph = new IPHelper();
            string myIP = iph.GetLocalIP();
            iph = null;
            IPEndPoint ep = new IPEndPoint(IPAddress.Parse(myIP), port);

            try
            {
                // start listening
                LingerOption lng = new System.Net.Sockets.LingerOption(false, 0);
                simpleServerSocket.LingerState = lng;
                simpleServerSocket.Bind(ep);
                simpleServerSocket.Listen(2);

                Monitor.Enter(SSLock);
                simpleServerSocket.BeginAccept(
                    new AsyncCallback(acceptCallback),
                    simpleServerSocket);
                SimpleServerRunning = true;
                Monitor.Exit(SSLock);

                return true;
            }
            catch
            { }
            return false;
        }
        public void old_acceptCallback(IAsyncResult ar)
        {
            try
            {
                Socket listener = (Socket)ar.AsyncState;
                Socket handler = listener.EndAccept(ar);
            }
            catch { }  // ObjectDisposedException if socket is already closed

            FinalSocketShutdown();
        }

        void old_StopSimpleServer()
        {
            if (!SimpleServerRunning) return;

            Monitor.Enter(SSLock);
            if (simpleServerSocket != null)
            {
                
                simpleServerSocket.Close();  // IMPORTANT: This triggers acceptCallback() if there's been no incoming connection, which in turn sets flags to FALSE, nullifies socket, etc.
            }

            FinalSocketShutdown(); // safety

            Monitor.Exit(SSLock);
        }
         void old_FinalSocketShutdown()
        {
            SimpleServerRunning = false;
            simpleServerSocket = null;
        }
         */
        #endregion

        void FinalSocketShutdown()
        {
            SimpleServerRunning = false;
            simpleServer = null;
        }
        

        
        #endregion

        #region Detect If Ports Open
        public event EventHandler<CheckPortCompletedEventArgs> CheckPortOpenAsync_Completed;
        public void CheckPortOpenAsync(int port, bool runTestServer)
        {
            // Contact FatAttitude service to determine if port is open

            if (runTestServer)
            {
                if (!StartSimpleServer(port))
                {
                    RaiseCheckPortOpenCompletedEvent(false, false, "Could not listen on local test socket -- have you forgotten to stop the Remote Potato server first?");
                    return;
                }
            }

            try
            {
                // Create a HttpWebrequest object to the desired URL. 
                HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create("http://portcheck.remotepotato.com/?port=" + port.ToString() );

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
                myRequestState.response.Close();
            }
            catch (Exception e)
            {
                Functions.WriteLineToLogFile("PortChecker: CheckPortOpenAsync Exception raised");
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
                Functions.WriteLineToLogFile("PortChecker: ResponseCallback Exception raised");
                Functions.WriteExceptionToLogFile(e);
            }
            finally
            {
                allDone.Set();
            }
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

                        allDone.Set();
                     

                        ProcessHTMLResponse(stringContent);
                    }

                    responseStream.Close();
                }

            }
            catch (WebException e)
            {
                Functions.WriteLineToLogFile("PortChecker: ReadCallback Exception raised");
                Functions.WriteExceptionToLogFile(e);
            }
            finally
            {
                allDone.Set();

                StopSimpleServer();   // IMPORTANT
            }

        }
        #endregion

        void ProcessHTMLResponse(string responseText)
        {
            if (string.IsNullOrWhiteSpace(responseText))
            {
                RaiseCheckPortOpenCompletedEvent(false, false, "The status of your connection could not be determined as no response was received from the web service; please try later.");
                return;
            }

            responseText = responseText.Trim();
            if (responseText.Contains("PORTCHECK_OPEN"))
                    RaiseCheckPortOpenCompletedEvent(true, true, "");
            else if (responseText.Contains("PORTCHECK_TIMEOUT"))
                    RaiseCheckPortOpenCompletedEvent(true, false, "");
            else if (responseText.Contains("PORTCHECK_ERROR"))
                RaiseCheckPortOpenCompletedEvent(false, false, "The status of your connection could not be determined as the web service encountered an error; please try later.");
            else
            {
                RaiseCheckPortOpenCompletedEvent(false, false, "The status of your connection could not be determined as the web service gave an unknown response; please try later.");
                Functions.WriteLineToLogFile("PortChecker: Unknown response from web service: " + responseText);
            }
        }
        void RaiseCheckPortOpenCompletedEvent(bool didComplete, bool isOpen, string msg)
        {
            if (SimpleServerRunning)
                StopSimpleServer();

            if (CheckPortOpenAsync_Completed != null)
            {
                CheckPortOpenAsync_Completed(null, new CheckPortCompletedEventArgs(didComplete, isOpen, msg));
            }
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

        public class CheckPortCompletedEventArgs : EventArgs
        {
            public bool DidComplete { get; set; }
            public bool PortOpen { get; set; }
            public string Message {get; set;}

            public CheckPortCompletedEventArgs(bool _didcomplete, bool _portOpen, string _msg)
            {
                DidComplete = _didcomplete;
                PortOpen = _portOpen;
                Message = _msg;
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

                allDone.Set(); // experimental
            }
        }



        #endregion

    }

}
