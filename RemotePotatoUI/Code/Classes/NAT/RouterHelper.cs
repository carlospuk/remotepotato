using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Text;
using System.Diagnostics;
using RemotePotatoServer.Properties;
using NATUPNPLib;

namespace RemotePotatoServer
{
    public class RouterHelper : IDisposable
    {

        UPnPNAT upnpnat;
        IStaticPortMappingCollection mappings;
        public RouterHelper()
        {
            
        }
        public bool InitAndFindRouter()
        {
            try
            {
                upnpnat = new UPnPNAT();
                mappings = upnpnat.StaticPortMappingCollection;

                if (mappings != null)
                    return true;
                else
                    return false;
            }
            catch
            {
                return false;
            }
        }
        public void Dispose()
        {
            upnpnat = null;
        }

        public bool RPMappingsExist(string LocalIP)
        {
            bool foundPort1 = false;
            bool foundPort2 = false;

            //IStaticPortMappingCollection mappings = upnpnat.StaticPortMappingCollection;

            foreach (NATUPNPLib.IStaticPortMapping portMapping in mappings)
            {
                if (
                    (portMapping.InternalClient == LocalIP) &&
                    (portMapping.InternalPort == Settings.Default.Port)
                    )
                    foundPort1 = true;
                else if (
                    (portMapping.InternalClient == LocalIP) &&
                    (portMapping.InternalPort == Settings.Default.SilverlightStreamingPort)
                    )
                    foundPort2 = true;
            }

            return (foundPort1 && foundPort2);
        }


        /*
        public bool AddRPMappings()
        {
            Network.IPHelper ipHelper = new Network.IPHelper();
            string myIP = ipHelper.GetLocalIP();
            //0x80040210
            RemoveRPMappings();  // if they exist

            IStaticPortMappingCollection mappings = upnpnat.StaticPortMappingCollection;

            bool success = true;
            success &= AddToMappings(mappings, "RemotePotatoServer", Convert.ToInt32(Settings.Default.Port), "TCP", Convert.ToInt32(Settings.Default.Port), myIP);
            success &= AddToMappings(mappings, "RemotePotatoStream", Convert.ToInt32(Settings.Default.SilverlightStreamingPort), "TCP", Convert.ToInt32(Settings.Default.SilverlightStreamingPort), myIP);
            return success;
        }
        public bool RemoveRPMappings()
        {
            
            IStaticPortMappingCollection mappings = upnpnat.StaticPortMappingCollection;

            bool success = true;
            success &= RemoveFromMappings(mappings, Convert.ToInt32(Settings.Default.Port), "TCP");
            success &= RemoveFromMappings(mappings, Convert.ToInt32(Settings.Default.SilverlightStreamingPort), "TCP");
            return success;
            
        }
        #region Helpers Add/Remove
        bool AddToMappings(IStaticPortMappingCollection mappings, string Description, int ExternalPort, string Protocol, int InternalPort, string InternalIP)
        {
            try
            {
                mappings.Add(ExternalPort, Protocol, InternalPort, InternalIP, true, Description);
                return true;
            }
            catch (Exception ex)
            {
                Functions.WriteLineToLogFile("Error adding UPNP Port Mapping:" + Description + " EXT:" + ExternalPort.ToString() + " INT:" + InternalIP + ":" + InternalPort.ToString() + " Proto:" + Protocol);
                Functions.WriteExceptionToLogFile(ex);
                return false;
            }
        }
        bool RemoveFromMappings(IStaticPortMappingCollection mappings, int ExternalPort, string Protocol)
        {
            try
            {
                mappings.Remove(ExternalPort, Protocol);
                return true;
            }
            catch (Exception ex)
            {
                Functions.WriteLineToLogFile("Error removing UPNP Port Mapping:  EXT:" + ExternalPort.ToString() + " Proto:" + Protocol );
                Functions.WriteExceptionToLogFile(ex);
                return false;
            }
        }
        #endregion
                bool portMappingsExistWithNames(List<string> names)
        {
            IStaticPortMappingCollection mappings = upnpnat.StaticPortMappingCollection;

            int numberFound = 0;
            foreach (NATUPNPLib.IStaticPortMapping portMapping in mappings)
            {
               // if (Settings.Default.DebugAdvanced)
                {
                    Debug.Print(portMapping.Description + ": " + portMapping.ExternalIPAddress + ":" + portMapping.ExternalPort.ToString() + " > " + portMapping.InternalClient + ":" + portMapping.InternalPort.ToString() + " prot:" + portMapping.Protocol);
                }

                if (names.Contains(portMapping.Description))
                {
                    if (++numberFound >= names.Count)
                        return true;
                }
            }

            return false;
        }
        NATUPNPLib.IStaticPortMapping portMappingWithName(string name)
        {
            UPnPNAT upnpnat = new UPnPNAT();
            IStaticPortMappingCollection mappings = upnpnat.StaticPortMappingCollection;

            foreach (NATUPNPLib.IStaticPortMapping portMapping in mappings)
            {
                if (portMapping.Description == name)
                {
                    return portMapping;
                }
            }

            return null;
        }
        */


        public string ListportMappings()
        {
            //IStaticPortMappingCollection mappings = upnpnat.StaticPortMappingCollection;

            StringBuilder sbOutput = new StringBuilder(50);
            sbOutput.AppendLine("Port Forwarding Rules:");
            foreach (NATUPNPLib.IStaticPortMapping portMapping in mappings)
            {
                sbOutput.AppendLine("Rule: " + portMapping.Description + ": " + portMapping.ExternalIPAddress + ":" + portMapping.ExternalPort.ToString() + " > " + portMapping.InternalClient + ":" + portMapping.InternalPort.ToString() + " (" + portMapping.Protocol + ")" );
            }

            return sbOutput.ToString();
        }



    }

    
}
