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
                XmlSerializer serializer = new XmlSerializer(typeof(T));

                using (StringWriter sw = new StringWriter())
                {
#if SILVERLIGHT                    
                        serializer.Serialize(sw, obj);
                        sw.Flush();
#else
                    SafeXmlWriter writer = new SafeXmlWriter(sw);
                    serializer.Serialize(writer, obj);
#endif
                    //sw.Flush();
                    return sw.ToString();
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

        public static string XMLReponseWithOutputString(string txtOutputString)
        {
            return XMLHelper.Serialize<string>(txtOutputString);
            //return  "<?xml><response>" + txtOutputString + "</response>"; 
        }

    }
}
