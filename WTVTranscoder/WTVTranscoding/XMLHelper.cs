using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.IO;
using System.Xml;
using System.Text;

namespace RemotePotatoServer
{
    public static class XMLHelper
    {

        public static T Deserialize<T>(string fromXML)
        {
            try
            {
                XmlSerializer xmls = new XmlSerializer(typeof(T));
                StringReader sr = new StringReader(fromXML);
                return (T)xmls.Deserialize(sr);
            }
            catch
            {
                return default(T);
            }
        }

        public static string Serialize<T>(T obj)
        {
            try
            {
                XmlSerializer xmls = new XmlSerializer(typeof(T));

                using (StringWriter stream = new StringWriter())
                {
                    xmls.Serialize(stream, obj);
                    stream.Flush();
                    return stream.ToString();
                }
            }
            catch 
            {
#if !SILVERLIGHT
                Functions.WriteLineToLogFile("Exception serializing data:");
#endif
                return null;
            }
        }


    }
}
