using System;
using System.Collections.Generic;
using System.Web;
using System.Text;
using CommonEPG;


namespace RemotePotatoServer
{
    public static class HTMLHelper
    {

        public static string imgTagDefault()
        {
            return "<img src=\"/skin/thumbnail_default.png\" class=\"showthumbnail\"/>";
        }
        public static string imgTagRecordedTVProgramme(TVProgramme tvp)
        {
            return "<img src=\"/rectvthumbnail?filename=" +  HttpUtility.UrlEncode(tvp.Filename) +  "\" class=\"showthumbnail\"/>";
        }

       
    }
}
