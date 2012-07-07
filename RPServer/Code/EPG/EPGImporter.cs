using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Serialization;
using CommonEPG;

namespace RemotePotatoServer
{
    public static class EPGImporter
    {
        public static List<TVService> ChannelListFromString(string txtChannelList)
        {
            List<TVService> theChannels = new List<TVService>();
            try
            {
                XmlSerializer serializer = new XmlSerializer(theChannels.GetType());
                StringReader sr = new StringReader(txtChannelList);
                theChannels = (List<TVService>)serializer.Deserialize(sr);
            }
            catch (Exception e)
            {
                Functions.WriteLineToLogFile("Error de-serialising channel list:");
                Functions.WriteExceptionToLogFile(e);
            }
            return theChannels;
        }
    }
}
