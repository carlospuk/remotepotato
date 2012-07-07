using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using System.Text;

namespace CommonEPG
{
    public class FileBrowseRequest
    {

        public static FileBrowseRequest FromXML(string theXML)
        {
            FileBrowseRequest request = new FileBrowseRequest();
            XmlSerializer serializer = new XmlSerializer(request.GetType());
            StringReader sr = new StringReader(theXML);
            try
            {
                return (FileBrowseRequest)serializer.Deserialize(sr);
            }
            catch
            {
                return request;
            }
        }
        
        public string FullPath { get; set; }
        public List<string> Filters { get; set; }
        public int ThumbnailsBatch { get; set; }
        public int ThumbnailsBatchSize { get; set; }
        public bool ThumbnailsLimitToBatch { get; set; }
        public bool GetDurationOfMediaFiles { get; set; }

        public FileBrowseRequest()
        {
            Filters = new List<string>();
            FullPath = "";
            ThumbnailsBatchSize = 100;
            ThumbnailsLimitToBatch = false;
        }
    }
}
