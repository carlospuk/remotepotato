using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using System.Text;
using System.IO;

namespace CommonEPG
{
    public class EPGRequest
    {

        public static List<EPGRequest> ArrayFromXML(string theXML)
        {
            List<EPGRequest> requests = new List<EPGRequest>();
            XmlSerializer serializer = new XmlSerializer(requests.GetType());
            StringReader sr = new StringReader(theXML);
            try
            {
                return (List<EPGRequest>)serializer.Deserialize(sr);
            }
            catch
            {
                return requests;
            }
        }

        // Blank constructor (for serialization)
        public EPGRequest()
        {
        }

        public EPGRequest(string _serviceID, long startTime, long stopTime)
        {
            TVServiceID = _serviceID;
            StartTime = startTime;
            StopTime = stopTime;
        }
        public EPGRequest(string _serviceID, DateRange dateRange)
        {
            TVServiceID = _serviceID;

            StartTime = dateRange.StartTime.Ticks;
            StopTime = dateRange.StopTime.Ticks;
        }

        public string TVServiceID { get; set; }

        public long StartTime { get; set; }
        public long StopTime { get; set; }



        public string CacheUniqueFilename
        {
            get
            {
                StringBuilder sbCacheFN = new StringBuilder(11);
                sbCacheFN.Append(StartTime.ToString() + "-" + StopTime.ToString());

                
                sbCacheFN.Append(TVServiceID);

                sbCacheFN.Append(".epgr");

                return sbCacheFN.ToString();
            }
        }

    }
}
