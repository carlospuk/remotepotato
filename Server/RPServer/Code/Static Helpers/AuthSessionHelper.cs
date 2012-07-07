using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Text;
using RemotePotatoServer.Properties;

namespace RemotePotatoServer
{
    public sealed class AuthSessionHelper
    {

        Dictionary<string, ClientAuthInfo> ClientSessions;
        Timer wipeTimer;

        public AuthSessionHelper()
        {
            ClientSessions = new Dictionary<string, ClientAuthInfo>();
            wipeTimer = new Timer(60000);
            wipeTimer.Elapsed += new ElapsedEventHandler(wipeTimer_Elapsed);
            wipeTimer.AutoReset = true;
            wipeTimer.Start();
        }
        void wipeTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            WipeOldSessions();
        }

        public string AddClient(string clientip)
        {
            string newTkn = newToken();
            ClientAuthInfo cai = new ClientAuthInfo(newTkn, clientip);
            ClientSessions.Add(newTkn, cai);
            return newTkn;
        }
        public bool AuthenticateToken(string token, string clientip)
        {
            if (ClientSessions.ContainsKey(token))
            {
                ClientAuthInfo cai = ClientSessions[token];

                if (SessionIsActive(cai))
                {
                    if (Settings.Default.EnforceClientIPSecurity)
                    {
                        if (cai.ClientIP.Equals(clientip))
                        {
                            cai.Renew();  // set timeout to be X minutes in the future.
                            return true;
                        }
                    }
                    else
                    {
                        // Not enforcing IP security
                        return true;
                    }
                }
                else
                {
                    ClientSessions.Remove(token);
                }
            }

            return false;
        }
        void WipeOldSessions()
        {
            List<ClientAuthInfo> lstClientSessions = ClientSessions.Values.ToList();
            ClientAuthInfo cai;
            for (int i = 0; i < lstClientSessions.Count; i++)
            {
                cai = null;
                cai = lstClientSessions[i];

                if (!SessionIsActive(cai))
                    ClientSessions.Remove(cai.Token);
            }

            // Quick wipe
            lstClientSessions.Clear();
            lstClientSessions = null;
        }

        bool SessionIsActive(ClientAuthInfo cai)
        {
            return (cai.Expires > DateTime.Now);
        }
        string newToken()
        {
            Guid g = System.Guid.NewGuid();
            string gs = g.ToString();
            return gs.Substring(0, 8);
        }




        #region Singleton Methods
        static AuthSessionHelper instance = null;
        static readonly object padlock = new object();
        public static AuthSessionHelper Default
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new AuthSessionHelper();
                    }
                    return instance;
                }
            }
        }
        #endregion


    }

    public class ClientAuthInfo
    {
        public readonly string Token;
        public readonly string ClientIP;
        public DateTime Expires;

        public ClientAuthInfo(string token, string ip)
        {
            ClientIP = ip;
            Token = token;
            Renew();
        }
        public void Renew()
        {
            Expires = DateTime.Now.AddMinutes(Settings.Default.AuthSessionTimeoutMinutes);
        }
    }
}
