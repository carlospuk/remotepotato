using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Text;

namespace RPKeySender
{
    public sealed class IRServer
    {


        private IRServer()
        {

        }


        public static ManualResetEvent allDone = new ManualResetEvent(false);
        //const int BUFFER_SIZE = 1024;
        //const int DefaultTimeout = 20 * 1000; // 20 sec timeout for service  (service can take up to 5 seconds while it waits for its own socket to time out)

        
        #region Simple Server
        TcpListener listener = null;
        public bool StartServer(int port)
        {
            Functions.WriteLineToLogFile("IRServer: Starting server on port " + port.ToString());

            try
            {
                listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, port);
                listener.Start();

                Thread t = new Thread(ServerListenLoop);
                t.Start();
            }
            catch
            {
                return false;
            }

            return true;
        }

        void ServerListenLoop()
        {

            while (listener != null)
            {
                allDone.Reset();

                try
                {
                    listener.BeginAcceptTcpClient(new AsyncCallback(acceptCallback), listener);
                    allDone.WaitOne();
                }
                catch
                { }

            }
            
        }
        void acceptCallback(IAsyncResult ar)
        {
            Functions.WriteLineToLogFileIfLoggingKeys("IRServer: Client connected");

            try
            {
                TcpListener listener = (TcpListener)ar.AsyncState;
                using (TcpClient client = listener.EndAcceptTcpClient(ar))
                {
                    NetworkStream stream = client.GetStream();

                    // Buffer for reading data
                    Byte[] bytes = new Byte[256];
                    String strCommand = string.Empty;
                    int i;

                    // Loop to receive all the data sent by the client.
                    while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                    {
                        // Translate data bytes to a ASCII string.
                        string thisData = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                        Console.WriteLine(String.Format("Received: {0}", strCommand));

                        strCommand += thisData;
                    }

                    // Process Command
                    ProcessCommand(strCommand);


                    client.Close();
                }
            }
            catch { }  // ObjectDisposedException if socket is already closed

            allDone.Set();
        }
        public void StopServer()
        {
            Functions.WriteLineToLogFile("IRServer: Stopping server.");

            if (listener!= null)
                listener.Stop(); // IMPORTANT: This triggers acceptCallback() if there's been no incoming connection

            listener = null;
        }
        void ProcessCommand(string cmd)
        {
            CommandSender.SendMediaCenterCommand(cmd);   
        }
        #endregion






        #region Singleton Methods
        static IRServer instance = null;
        static readonly object padlock = new object();
        public static IRServer Default
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new IRServer();
                    }
                    return instance;
                }
            }
        }
        #endregion

    }
}
