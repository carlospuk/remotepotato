using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Web;
using System.Threading;
using RemotePotatoServer.Properties;

namespace RemotePotatoServer
{
    public class DNSHelper
    {
        const int CheckForPublicIPChangesIntervalMinutes = 10;

        // DynDNS Client - can be instantiated (used for temporary updates) or accessed via singleton (used within RPService)
        Network.IPHelper ipHelper;
        public bool IsRunning { get; set; }
        System.Threading.Timer tmCheckDNS;
        public DNSHelper()
        {
            ipHelper = new Network.IPHelper();
            ipHelper.QueryExternalIPAsync_Completed += new EventHandler<Network.IPHelper.GetExternalIPEventArgs>(ipHelper_QueryExternalIPAsync_Completed);
        }

        
        public void Start()
        {
            if (!Settings.Default.DynDNSClientEnabled)
            {
                if (Settings.Default.DebugAdvanced)
                    Functions.WriteLineToLogFile("DNSHelper: Not starting, not enabled.");
                return;
            }

            Functions.WriteLineToLogFile("DNSHelper: Starting Dynamic DNS Client");

            if (tmCheckDNS != null)
                StopAndRemoveTimer();

            CreateAndStartTimer();

            IsRunning = true;
        }
        public void Stop()
        {
            StopAndRemoveTimer();

            IsRunning = false;
        }

        void StopAndRemoveTimer()
        {
            if (tmCheckDNS == null) return;

            tmCheckDNS.Dispose();
            tmCheckDNS = null;
        }
        void CreateAndStartTimer()
        {
            tmCheckDNS = new Timer(tmCheckDNS_Tick, null, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(CheckForPublicIPChangesIntervalMinutes));
            
        }
        void tmCheckDNS_Tick(object userInfo)
        {
            if (Settings.Default.DebugAdvanced)
                Functions.WriteLineToLogFile("DNSHelper: Checking for external IP address change...");

            ipHelper.QueryExternalIPAsync();
        }
        void ipHelper_QueryExternalIPAsync_Completed(object sender, Network.IPHelper.GetExternalIPEventArgs e)
        {
            if (e.HasChanged)
            {

                Functions.WriteLineToLogFile("DNSHelper: External IP address changed...");

                // Update DynDNS
                DynDnsUpdateResult result = DynDnsUpdateResult.LocalError;
                try
                {
                    result = NotifyDynDNS(e.IP);

                    Functions.WriteLineToLogFile("DNSHelper: Notify Dyndns result: " + result.ToString());
                }
                catch (Exception ex)
                {
                    Functions.WriteLineToLogFile("DNSHelper: Could not notify DynDNS: ");
                    Functions.WriteExceptionToLogFile(ex);
                }

            }
            else
            {
                if (Settings.Default.DebugAdvanced)
                    Functions.WriteLineToLogFile("DNSHelper: External IP address has NOT changed.");
            }
        }

        #region DynDNS Client
        public enum DynDnsUpdateResult
        {
            UpdatedIp,
            NoUpdateSameIp,
            LocalError,
            RemoteError,
            BadAuth,
            BadAgent,
            HostNotExist,
            HostNotYours,
            HostSyntaxError
        }
        public string ErrorMessageForResult(DynDnsUpdateResult result)
        {
            switch (result)
            {
                case DynDnsUpdateResult.BadAuth:
                    return "The username and/or password was incorrect.";

                case DynDnsUpdateResult.LocalError:
                    return "An error in Remote Potato stopped the request being made.";

                case DynDnsUpdateResult.HostSyntaxError:
                    return "The host name should be a fully qualified domain name, for example:  yourname.dyndns.org ";

                case DynDnsUpdateResult.NoUpdateSameIp:
                    return "Your IP address is already updated, no change was made.  Avoid doing this repeatedly.";

                case DynDnsUpdateResult.RemoteError:
                    return "An error occurred on the remote server.";

                case DynDnsUpdateResult.HostNotExist:
                    return "The host name was incorrect - please check it and try again.";

                case DynDnsUpdateResult.HostNotYours:
                    return "The host name does not belong to the account used; please check all login details and try again.";

                case DynDnsUpdateResult.BadAgent:
                    return "This user agent has been blocked - please contact enquiries@remotepotato.com to let us know about this incident.";

                case DynDnsUpdateResult.UpdatedIp:
                    return "OK";
            }

            return "Unknown result.";
        }

        public DynDnsUpdateResult NotifyDynDNS(string ipAddress)
        {
            byte[] data = new byte[1024];
            string response = "";
            int count;

            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPHostEntry host = System.Net.Dns.GetHostEntry("members.dyndns.org");
            if (host.AddressList.Count() < 1) return DynDnsUpdateResult.LocalError;

            socket.Connect((EndPoint)(new IPEndPoint(host.AddressList[0], 80)));

            if (!socket.Connected)
                throw new Exception("Can´t connect to dyndns service");

            string request = "GET /nic/update?" +
                "system=dyndns" +
                "&hostname=" + Settings.Default.DynamicDNSHostname +
                "&myip=" + ipAddress +
                "&offline=NO " +
                "HTTP/1.1\r\n" +
            "Host: members.dyndns.org\r\n" +
            "Authorization: Basic " +
                System.Convert.ToBase64String(ASCIIEncoding.UTF8.GetBytes(Settings.Default.DynDNSUsername + ":" + Settings.Default.DynDNSPassword)) + "\r\n" +
            "User-Agent: Remote Potato DynDNS Client\r\n\r\n";

            count = socket.Send(System.Text.UnicodeEncoding.ASCII.GetBytes(request));

            while ((count = socket.Receive(data)) != 0) // Wait for response
                response += System.Text.ASCIIEncoding.ASCII.GetString(data, 0, count);

            socket.Shutdown(SocketShutdown.Both);
            socket.Close();

            
            response = response.Substring(response.IndexOf("\r\n\r\n") + 4); // Headers end with two line breaks
            /*
            string trimResponse;
            trimResponse =  response.Substring(0, response.IndexOf(" ")).ToLower();
            trimResponse = trimResponse.Trim();
            if (trimResponse.StartsWith("11\r\n"))
                trimResponse = trimResponse.Substring(4);
            */

            if (response.Contains("good"))
                // The update was successful, and the hostname is now updated
                return DynDnsUpdateResult.UpdatedIp;
            else if (response.Contains("nochg"))
                // The update changed no settings, and is considered abusive. Additional nochg updates will cause the hostname to become blocked
                return DynDnsUpdateResult.NoUpdateSameIp;
            else if (response.Contains("badsys"))
            {
                Functions.WriteLineToLogFile("DNSHelper DynDNS Update)) The system parameter given is not valid. Valid system parameters are dyndns, statdns and custom");
                return DynDnsUpdateResult.NoUpdateSameIp;
            }
            else if (response.Contains("badagent"))
            {
                Functions.WriteLineToLogFile("DNSHelper DynDNS Update)) The user agent that was sent has been blocked for not following these specifications or no user agent was specified");
                return DynDnsUpdateResult.BadAgent;
            }
            else if (response.Contains("badauth"))
            {
                Functions.WriteLineToLogFile("DNSHelper DynDNS Update)) The username or password specified are incorrect");
                return DynDnsUpdateResult.BadAuth;
            }
            else if (response.Contains("!donator"))
            {
                Functions.WriteLineToLogFile("DNSHelper DynDNS Update)) An option available only to credited users (such as offline URL) was specified, but the user is not a credited user");
                return DynDnsUpdateResult.RemoteError;
            }
            else if (response.Contains("notfqdn"))
            {
                Functions.WriteLineToLogFile("DNSHelper DynDNS Update)) The hostname specified is not a fully-qualified domain name (not in the form hostname.dyndns.org or domain.com)");
                return DynDnsUpdateResult.HostSyntaxError;
            }
            else if (response.Contains("nohost"))
            {
                Functions.WriteLineToLogFile("DNSHelper DynDNS Update)) The hostname specified does not exist (or is not in the service specified in the system parameter)");
                return DynDnsUpdateResult.HostNotExist;
            }
            else if (response.Contains("!yours"))
            {
                Functions.WriteLineToLogFile("DNSHelper DynDNS Update)) The hostname specified exists, but not under the username specified");
                return DynDnsUpdateResult.HostNotYours;
            }
            else if (response.Contains("abuse"))
            {
                Functions.WriteLineToLogFile("DNSHelper DynDNS Update)) The hostname specified is blocked for update abuse");
                return DynDnsUpdateResult.BadAgent;
            }
            else if (response.Contains("numhost"))
            {
                Functions.WriteLineToLogFile("DNSHelper DynDNS Update)) Too many or too few hosts found");
                return DynDnsUpdateResult.RemoteError;
            }
            else if (response.Contains("dnserr"))
            {
                Functions.WriteLineToLogFile("DNSHelper DynDNS Update)) DNS error encountered");
                return DynDnsUpdateResult.RemoteError;
            }
            else if (response.Contains("911"))
            {
                Functions.WriteLineToLogFile("DNSHelper DynDNS Update)) There is a serious problem on our side, such as a database or DNS server failure. The client should stop updating until notified via the status page that the service is back up.");
                return DynDnsUpdateResult.RemoteError;
            }
            else
            {
                Functions.WriteLineToLogFile("DNSHelper DynDNS Update)) Unknown result from dyndns service");
                return DynDnsUpdateResult.RemoteError;
            }
            

        }
        #endregion



        #region Singleton Methods
        static DNSHelper instance = null;
        static readonly object padlock = new object();
        internal static DNSHelper Default
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new DNSHelper();
                    }
                    return instance;
                }
            }
        }
        #endregion


    }
}
