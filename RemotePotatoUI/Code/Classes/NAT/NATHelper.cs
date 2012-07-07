using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Xml;
using System.Threading;
using System.Diagnostics;
using System.IO;
using RemotePotatoServer.Properties;

namespace RemotePotatoServer.Network
{
    public class NATHelper
    {
        public TimeSpan TimeOut {get; set;}
        string GatewayURL, ServiceName, ServiceURL, SubEventURL;

        public NATHelper()
        {
            TimeOut = TimeSpan.FromSeconds(3);
        }

        #region Discovery
        public bool GatewayFound { get; set; }
        public bool Discover(ref string txtError)
        {
            ServiceName = "WANIPConnection";
            byte[] buffer;
            Socket s;
            try
            {
                s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
                s.ReceiveTimeout = Convert.ToInt32(TimeOut.TotalMilliseconds);
                string req = "M-SEARCH * HTTP/1.1\r\n" +
                    "HOST: 239.255.255.250:1900\r\n" +
                    "ST:upnp:rootdevice\r\n" +
                    "MAN:\"ssdp:discover\"\r\n" +
                    "MX:3\r\n\r\n";
                byte[] data = Encoding.ASCII.GetBytes(req);
                IPEndPoint ipe = new IPEndPoint(IPAddress.Broadcast, 1900);
                buffer = new byte[0x1000];

                s.SendTo(data, ipe);
                s.SendTo(data, ipe);
                s.SendTo(data, ipe);
            }
            catch (Exception ex)
            {
                Functions.WriteLineToLogFile("NATHelper Discover(): Exception init'g socket:");
                Functions.WriteExceptionToLogFile(ex);
                GatewayFound = false;
                return false;
            }

            int length = 0;
            do
            {
                try
                {
                    length = s.Receive(buffer);
                }
                catch (Exception ex) // e.g. timeout ?
                {
                    txtError = "Broadcast router search probably timed out";
                    Functions.WriteLineToLogFile("NatHelper: " + txtError);
                    Functions.WriteExceptionToLogFile(ex);
                    GatewayFound = false;
                    return false;
                }

                string resp = Encoding.ASCII.GetString(buffer, 0, length).ToLower();
                if (resp.Contains("upnp:rootdevice"))
                {
                    resp = resp.Substring(resp.ToLower().IndexOf("location:") + 9);
                    resp = resp.Substring(0, resp.IndexOf("\r")).Trim();

                    // Try to get service URL
                    ServiceURL = GetServiceUrl(resp);
                    if (! string.IsNullOrWhiteSpace(ServiceURL))
                    {
                        // Success - we found a gateway
                        GatewayURL = resp;
                        GatewayFound = true;

                        return true;
                    }
                }
            } while (length > 0);
            
            
            return false;
        }
        private string GetServiceUrl(string resp)
        {
#if !DEBUG
            try
            {
#endif
            XmlDocument desc = new XmlDocument();
            WebRequest r = HttpWebRequest.Create(resp);
            r.Method = "GET";
            WebResponse wres = r.GetResponse();
            Stream ress = wres.GetResponseStream();
            desc.Load(ress);
            ress.Close();

            //Debug.Print(Environment.NewLine + Environment.NewLine + resp + " >>>" + Environment.NewLine + desc.InnerXml);
            XmlNamespaceManager nsMgr = new XmlNamespaceManager(desc.NameTable);
            nsMgr.AddNamespace("tns", "urn:schemas-upnp-org:device-1-0");
            XmlNode typen = desc.SelectSingleNode("//tns:device/tns:deviceType/text()", nsMgr);
            if (!typen.Value.Contains("InternetGatewayDevice"))
                return null;
            XmlNode node = desc.SelectSingleNode("//tns:service[tns:serviceType=\"urn:schemas-upnp-org:service:" + ServiceName + ":1\"]/tns:controlURL/text()", nsMgr);
            if (node == null)
            {
                // Try PPP service instead
                ServiceName = "WANPPPConnection";
                node = desc.SelectSingleNode("//tns:service[tns:serviceType=\"urn:schemas-upnp-org:service:" + ServiceName + ":1\"]/tns:controlURL/text()", nsMgr);
            }
            if (node == null)
                return null;
            XmlNode eventnode = desc.SelectSingleNode("//tns:service[tns:serviceType=\"urn:schemas-upnp-org:service:" + ServiceName + ":1\"]/tns:eventSubURL/text()", nsMgr);
            SubEventURL = CombineUrls(resp, eventnode.Value); // Not used
            return CombineUrls(resp, node.Value);
#if !DEBUG
            }
            catch { return null; }
#endif
        }
        #endregion

        public bool RPMappingsExist(string LocalIP)
        {
            if (!GatewayFound) return false;

            List<PortMappingEntry> mappings = GetStaticMappings();

            bool foundPort1 = false;
            //bool foundPort2 = false;

            foreach (PortMappingEntry portMapping in mappings)
            {
                if (
                    (portMapping.InternalClient == LocalIP) &&
                    (portMapping.InternalPort == Settings.Default.Port)
                    )
                    foundPort1 = true;
              /*  else if (
                    (portMapping.InternalClient == LocalIP) &&
                    (portMapping.InternalPort == Settings.Default.SilverlightStreamingPort)
                    )
                    foundPort2 = true; */
            }

            // TODO:  Look for all ports in range return (foundPort1 && foundPort2); 
            return foundPort1;
        }
        List<PortMappingEntry> GetStaticMappings()
        {
            List<PortMappingEntry> entries = new List<PortMappingEntry>();

            int counter = 0;
            PortMappingEntry pm;
            while (getMappingEntryWithIndex(counter++, out pm))
            {
                entries.Add(pm);
            }

            // Trim dynamic mappings (non-zero leases)
            List<PortMappingEntry> dynamics = new List<PortMappingEntry>();
            foreach (PortMappingEntry mapping in entries)
            {
                if (mapping.LeaseDuration > 0)
                    dynamics.Add(mapping);
            }
            foreach (PortMappingEntry mapping in dynamics)
            {
                entries.Remove(mapping);
            }

            return entries;
        }
        public string ListMappings()
        {
            if (!GatewayFound) return "No router could be found.";

            List<PortMappingEntry> mappings = GetStaticMappings();
            

            StringBuilder sbOutput = new StringBuilder(50);
            sbOutput.AppendLine("Port Forwarding Rules:");
            foreach (PortMappingEntry portMapping in mappings)
            {
                sbOutput.AppendLine("Rule: " + portMapping.Description + ": " + portMapping.ExternalPort.ToString() + " > " + portMapping.InternalClient + ":" + portMapping.InternalPort.ToString() + " (" + portMapping.Protocol + ")");
            }

            return sbOutput.ToString();
        }
        bool getMappingEntryWithIndex(int index, out PortMappingEntry pm)
        {
            pm = new PortMappingEntry();

            XmlDocument xResponse = null;
            string txtError = null;

            string SOAPbody = "<u:GetGenericPortMappingEntry xmlns:u=\"urn:schemas-upnp-org:service:" + ServiceName + ":1\">" +
                    "<NewPortMappingIndex>" + index.ToString().Trim() + "</NewPortMappingIndex>" + 
                    "</u:GetGenericPortMappingEntry>";           
            string SOAPfunction = "GetGenericPortMappingEntry";

            if (TrySubmitSOAPRequest(SOAPbody,
                SOAPfunction,
                ref txtError,
                ref xResponse) != NatHelperReponseCodes.OK)
            {
                return false;  // end of entries?
            }
            
            try
            {
                XmlNamespaceManager nsMgr = new XmlNamespaceManager(xResponse.NameTable);
                nsMgr.AddNamespace("tns", "urn:schemas-upnp-org:device-1-0");
                XmlNode nd = null;
                int i = 0;

                // External Port
                nd = xResponse.SelectSingleNode("//NewExternalPort/text()", nsMgr);
                if ( (nd != null) && (!(string.IsNullOrWhiteSpace( nd.Value ))) )
                {
                    if (Int32.TryParse(nd.Value, out i))
                        pm.ExternalPort = i;
                }

                // Internal Port
                nd = xResponse.SelectSingleNode("//NewInternalPort/text()", nsMgr);
                if ((nd != null) && (!(string.IsNullOrWhiteSpace(nd.Value))))
                {
                    if (Int32.TryParse(nd.Value, out i))
                        pm.InternalPort = i;
                }

                // Internal Client
                nd = xResponse.SelectSingleNode("//NewInternalClient/text()", nsMgr);
                if ((nd != null) && (!(string.IsNullOrWhiteSpace(nd.Value))))
                    pm.InternalClient = nd.Value;

                // Description
                nd = xResponse.SelectSingleNode("//NewPortMappingDescription/text()", nsMgr);
                if ((nd != null) && (!(string.IsNullOrWhiteSpace(nd.Value))))
                    pm.Description = nd.Value;

                // Protocol
                nd = xResponse.SelectSingleNode("//NewProtocol/text()", nsMgr);
                if ((nd != null) && (!(string.IsNullOrWhiteSpace(nd.Value))))
                {
                    PortMappingEntry.PortMappingEntryProtocolTypes result = 0;
                    if (Enum.TryParse<PortMappingEntry.PortMappingEntryProtocolTypes>(nd.Value, out result))
                        pm.Protocol = result;
                }

                // Enabled
                nd = xResponse.SelectSingleNode("//NewEnabled/text()", nsMgr);
                if ((nd != null) && (!(string.IsNullOrWhiteSpace(nd.Value))))
                {
                    bool b = false;
                    // ENUM PARSE
                    if (bool.TryParse(nd.Value, out b))
                        pm.Enabled = b;
                    else if (Int32.TryParse(nd.Value, out i))
                        pm.Enabled = (i == 1);
                }


                // Lease
                nd = xResponse.SelectSingleNode("//NewLeaseDuration/text()", nsMgr);
                if ((nd != null) && (!(string.IsNullOrWhiteSpace(nd.Value))))
                {
                    if (Int32.TryParse(nd.Value, out i))
                        pm.LeaseDuration = i;
                }

                return true;

            }
            catch {
                Functions.WriteLineToLogFileIfAdvanced("NATHelper: SOAP: Couldn't get or parse port mapping entry.  InnerXML: " + xResponse.InnerXml);
                return false;
            }
            
        }
        public NatHelperReponseCodes ForwardRPPorts(string LocalIP)
        {
            NatHelperReponseCodes foo = DeleteForwardingRule(Convert.ToInt32(Settings.Default.Port), ProtocolType.Tcp);
            foo = DeleteForwardingRule(Convert.ToInt32(Settings.Default.SilverlightStreamingPort), ProtocolType.Tcp);
            foo = DeleteForwardingRule(Convert.ToInt32(Settings.Default.Port), ProtocolType.Udp);
            foo = DeleteForwardingRule(Convert.ToInt32(Settings.Default.SilverlightStreamingPort), ProtocolType.Udp);

            NatHelperReponseCodes outputCode2 = NatHelperReponseCodes.OK;
            int basePort = Convert.ToInt32(Settings.Default.SilverlightStreamingPort);
            int highestPort = basePort + Settings.Default.SilverlightStreamingNumberOfPorts - 1;
            int counter = 1;
            for (int port = basePort; port <= highestPort; port++)
            {
                outputCode2 = ForwardPort(port, LocalIP, ProtocolType.Tcp, "RPStream" + counter.ToString() );
                counter++;
            }
            
            NatHelperReponseCodes outputCode1 = ForwardPort(Convert.ToInt32(Settings.Default.Port), LocalIP, ProtocolType.Tcp, "RPhttp");
            if (outputCode1 != NatHelperReponseCodes.OK) return outputCode1;
            if (outputCode2 != NatHelperReponseCodes.OK) return outputCode2;
            return NatHelperReponseCodes.OK;
                
        }
        public NatHelperReponseCodes ForwardPort(int port, string localIP, ProtocolType protocol, string description)
        {
            XmlDocument xResponse = null;
            string txtError = null;
            string SOAPbody = "<u:AddPortMapping xmlns:u=\"urn:schemas-upnp-org:service:" + ServiceName + ":1\">" +
                    "<NewRemoteHost></NewRemoteHost><NewExternalPort>" + port.ToString() + "</NewExternalPort><NewProtocol>" + protocol.ToString().ToUpper() + "</NewProtocol>" +
                    "<NewInternalPort>" + port.ToString() + "</NewInternalPort><NewInternalClient>" + localIP +
                    "</NewInternalClient><NewEnabled>1</NewEnabled><NewPortMappingDescription>" + description +
                    "</NewPortMappingDescription><NewLeaseDuration>0</NewLeaseDuration></u:AddPortMapping>";
            string SOAPfunction = "AddPortMapping";

            return TrySubmitSOAPRequest(SOAPbody,
                SOAPfunction,
                ref txtError,
                ref xResponse);
        }
        public NatHelperReponseCodes DeleteForwardingRule(int port, ProtocolType protocol)
        {
            XmlDocument xResponse = null;
            string txtError = null;
            string SOAPbody = "<u:DeletePortMapping xmlns:u=\"urn:schemas-upnp-org:service:" + ServiceName + ":1\">" +
                    "<NewRemoteHost>" +
                    "</NewRemoteHost>" +
                    "<NewExternalPort>" + port + "</NewExternalPort>" +
                    "<NewProtocol>" + protocol.ToString().ToUpper() + "</NewProtocol>" +
                    "</u:DeletePortMapping>";
            string SOAPfunction = "DeletePortMapping";

            return TrySubmitSOAPRequest(SOAPbody,
                SOAPfunction,
                ref txtError,
                ref xResponse);
        }
        public string GetExternalIP()
        {
            XmlDocument xResponse = null;
            string txtError = null;
            string SOAPbody = "<u:GetExternalIPAddress xmlns:u=\"urn:schemas-upnp-org:service:" + ServiceName + ":1\">" + "</u:GetExternalIPAddress>";
            string SOAPfunction = "GetExternalIPAddress";


            if (TrySubmitSOAPRequest(SOAPbody,
                SOAPfunction,
                ref txtError,
                ref xResponse) != NatHelperReponseCodes.OK)
            {
                return null;
            }

            try
            {
                XmlNamespaceManager nsMgr = new XmlNamespaceManager(xResponse.NameTable);
                nsMgr.AddNamespace("tns", "urn:schemas-upnp-org:device-1-0");
                string IP = xResponse.SelectSingleNode("//NewExternalIPAddress/text()", nsMgr).Value;
                return IP;
            }
            catch {
                Functions.WriteLineToLogFileIfAdvanced("NATHelper: SOAP: Couldn't get External IP from soap response.");
            }
            return null;
        }

        #region Low Level SOAP Requests
        NatHelperReponseCodes TrySubmitSOAPRequest(string strSOAPBody, string strSOAPFunction, ref string txtError, ref XmlDocument xResponse)
        {
            return TrySubmitSOAPRequest(ServiceURL, strSOAPBody, strSOAPFunction, ref txtError, ref xResponse);
        }
        NatHelperReponseCodes TrySubmitSOAPRequest(string strServiceURL, string strSOAPBody, string strSOAPFunction, ref string txtError, ref XmlDocument xResponse)
        {
         
            if (string.IsNullOrEmpty(strServiceURL))
            {
                txtError = "No UPnP service available or Discover() has not been called";
                return NatHelperReponseCodes.LocalError;
            }

            
            return SubmitSOAPRequest(strServiceURL, strSOAPBody, strSOAPFunction, out xResponse);  // Exception is thrown if request fails, or server gives http error (e.g. 404 / 500 etc)
        }
        object SOAPLock = new object();
        NatHelperReponseCodes SubmitSOAPRequest(string url, string soap, string function, out XmlDocument xResponse)
        {
            xResponse = new XmlDocument();

            try
            {
                Monitor.Enter(SOAPLock);
                string req = "<?xml version=\"1.0\"?>" +
                "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\" s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">" +
                "<s:Body>" +
                soap +
                "</s:Body>" +
                "</s:Envelope>";

                WebRequest r = HttpWebRequest.Create(url);
                r.Method = "POST";
                byte[] b = Encoding.UTF8.GetBytes(req);
                r.Headers.Add("SOAPACTION", "\"urn:schemas-upnp-org:service:" + ServiceName + ":1#" + function + "\"");
                r.ContentType = "text/xml; charset=\"utf-8\"";
                r.ContentLength = b.Length;
                Stream newStream = r.GetRequestStream();
                newStream.Write(b, 0, b.Length);
                newStream.Close();
                WebResponse wres = r.GetResponse();

                Stream ress = wres.GetResponseStream();
                xResponse.Load(ress);
                ress.Close();

                Monitor.Exit(SOAPLock);
                return NatHelperReponseCodes.OK;
            }
            catch (WebException we)
            {
                HttpWebResponse r = (HttpWebResponse)we.Response;

                string soapFault = "";
                try
                {
                    if (we.Response != null)
                    {
                        using (StreamReader responseReader = new StreamReader(we.Response.GetResponseStream()))
                        {
                            soapFault = responseReader.ReadToEnd();
                        }
                    }
                }
                catch { }


                if (r.StatusCode == HttpStatusCode.InternalServerError)
                {
                    Functions.WriteLineToLogFileIfAdvanced("NatHelper: WebException 500 submitting SOAP request.");
                    Functions.WriteLineToLogFileIfAdvanced(soapFault);
                    return NatHelperReponseCodes.Router500;
                }
                else
                {
                    Functions.WriteLineToLogFileIfAdvanced("NatHelper: Unknown WebException submitting SOAP request:");
                    Functions.WriteExceptionToLogFileIfAdvanced(we);
                    Functions.WriteLineToLogFileIfAdvanced(soapFault);
                    return NatHelperReponseCodes.UnknownError;
                }
            }
            catch (Exception ex)
            {
                Functions.WriteLineToLogFileIfAdvanced("NatHelper: Exception submitting SOAP request:");
                Functions.WriteExceptionToLogFileIfAdvanced(ex);
                return NatHelperReponseCodes.UnknownError;
            }
        }
        #endregion

        #region Helpers and Enums
        private string CombineUrls(string gatewayURL, string subURL)
        {
            // Is Control URL an absolute URL?
            if (
                (subURL.Contains("http:")) || (subURL.Contains("."))
                )
            {
                return subURL;
            }
            else
            {
                gatewayURL = gatewayURL.Replace("http://", "");  // strip any protocol
                int n = gatewayURL.IndexOf("/");
                if (n != -1)
                    gatewayURL = gatewayURL.Substring(0, n);  // Use first portion of URL
                return "http://" + gatewayURL + subURL;
            }
        }
        public enum NatHelperReponseCodes
        {
            Router500,
            UnknownError,
            LocalError,
            OK
        }
        public class PortMappingEntry
        {
            public PortMappingEntry()
            {
                Protocol = PortMappingEntryProtocolTypes.UDP;
                Description = "Unknown Mapping";
            }

            public enum PortMappingEntryProtocolTypes { TCP, UDP }

            public int ExternalPort { get; set; }
            public int InternalPort { get; set; }
            public string InternalClient { get; set; }
            public PortMappingEntryProtocolTypes Protocol { get; set; }
            public bool Enabled { get; set; }
            public string Description { get; set; }
            public int LeaseDuration { get; set; }


        }
        #endregion


    }
}
