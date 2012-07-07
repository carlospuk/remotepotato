using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

namespace CommonEPG
{
    public class EPGSearch
    {
        public EPGSearchMatchType MatchType { set; get; }
        public EPGSearchTextType TextType { set; get; }
        public string TextToSearch { set; get; }
        public DateRange DateRange { set; get; }
        public bool LimitToDateRange { set; get; }


        public static EPGSearch FromXML(string theXML)
        {
            EPGSearch newSearch = new EPGSearch();
            XmlSerializer serializer = new XmlSerializer(newSearch.GetType());
            StringReader sr = new StringReader(theXML);
            try
            {
                return (EPGSearch)serializer.Deserialize(sr);
            }
            catch
            {
                return null;
            }
        }


    }
}
