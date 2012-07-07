using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace RemotePotatoServer
{
    public class IRCommunicator
    {
        public IRCommunicator()
        {

        }

        public string SendIRCommand(string txtCmd)
        {
            // TODO: Is server running?
            if (IsRemoteHelperRunning)
            {
                SendStringToServer(txtCmd);
                return "OK";
            }
            else
            {
                return "HELPER_NOT_RUNNING";
            }
        }
        void SendStringToServer(string txtCmd)
        {
            TcpClient client = new TcpClient();
            ClientStateObject obj = new ClientStateObject(client, txtCmd);
            
            System.Timers.Timer timTimeOut = new System.Timers.Timer(600);

            System.Threading.Timer tmTimeOut = new System.Threading.Timer(new System.Threading.TimerCallback(tmTimeOut_Elapsed), client, 1000, System.Threading.Timeout.Infinite);
            
            IAsyncResult foo = client.BeginConnect(IPAddress.Loopback, 19080, new AsyncCallback(ConnectCallback), obj);
        }

        void tmTimeOut_Elapsed(object state)
        {
            if (state == null) return;

            if (state is TcpClient)
            {
                TcpClient client = (TcpClient) state;

                try
                {
                    client.Close();
                }
                catch 
                { 
                // do nothing
                }
            }
        }
        void ConnectCallback(IAsyncResult ar)
        {
            Functions.WriteLineToLogFileIfAdvanced("IRComm: connected to IR Server");

            try
            {
                ClientStateObject obj = (ClientStateObject)ar.AsyncState;
                TcpClient client = obj.Client;
                NetworkStream stream = client.GetStream();

                // Buffer for reading data
                Byte[] bytes = new Byte[256];
                String strCommand = string.Empty;

                Functions.WriteLineToLogFileIfAdvanced("IRComm: Sending: [" + obj.Command + "]");
                byte[] streamBytes = Encoding.ASCII.GetBytes(obj.Command);
                stream.Write(streamBytes, 0, streamBytes.Length);
                Functions.WriteLineToLogFileIfAdvanced("IRComm: Sent.");

                client.Close();
            }
            catch { }  // ObjectDisposedException if socket is already closed

            
        }

        class ClientStateObject
        {
            public TcpClient Client { get; set; }
            public string Command { get; set; }

            public ClientStateObject(TcpClient _client, string _command)
            {
                Client = _client;
                Command = _command;
            }
        }

        public bool IsRemoteHelperRunning
        {
            get
            {
                try
                {
                    using (System.Threading.Mutex m = System.Threading.Mutex.OpenExisting("Global\\RPKeySender"))
                    { }
                    return true;
                }
                catch (System.Threading.WaitHandleCannotBeOpenedException)
                {
                    return false;
                }
                catch
                {
                    return false;
                }

            }
        }

        #region Singleton Methods
        static IRCommunicator instance = null;
        static readonly object padlock = new object();
        public static IRCommunicator Default
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new IRCommunicator();
                    }
                    return instance;
                }
            }
        }
        #endregion

    }
}
