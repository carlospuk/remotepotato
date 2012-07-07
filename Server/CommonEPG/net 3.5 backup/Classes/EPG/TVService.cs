using System;
using System.Collections.Generic;

namespace CommonEPG
{
    public class TVService
    {
        // Private Members
        public string UniqueId { get; set; }  // A unique identifier for the channel.  For channels sourced from XMLTV files, see http://xmltv.cvs.sourceforge.net/*checkout*/xmltv/xmltv/xmltv.dtd &  http://www.faqs.org/rfcs/rfc2838.html
        /// <summary>
        /// The best internal channel ID within media center on which this service is located
        /// </summary>
        public long MCChannelID { get; set; }
        public string Callsign { get; set; }
        public bool IsFavorite { get; set; }
        public double MCChannelNumber { get; set; }
        public double MCSubChannelNumber { get; set; }
        public int UserSortOrder { get; set; }
        public string LogoUri { get; set; }
        public string FavoriteLineUpNames { get; set; }

        // Constructor
        public TVService()
        {}

        // Methods
        public bool HasCallsign
        {
            get
            {
                return (String.IsNullOrEmpty(Callsign));
            }
        }

        // Helpers
        public void AddToFavoriteLineUp(string faveName)
        {
            if (IsInFavoriteLineUp(faveName)) return;

            if (string.IsNullOrEmpty(FavoriteLineUpNames))
                FavoriteLineUpNames += faveName;
            else
            {
                if (!FavoriteLineUpNames.EndsWith("^"))
                    FavoriteLineUpNames += "^";

                FavoriteLineUpNames += faveName;
            }
        }
        public bool IsInFavoriteLineUp(string faveName)
        {
            List<string> faveNamesArray = FavoriteLineUpNamesList;
            return (faveNamesArray.Contains(faveName));
        }
        public void RemoveFromAllFavoriteLineUps()
        {
            FavoriteLineUpNames = "";
        }
        public List<string> FavoriteLineUpNamesList
        {
            get
            {
                List<string> output = new List<string>();
                if (string.IsNullOrEmpty(FavoriteLineUpNames)) return output;

                String[] faveNames = FavoriteLineUpNames.Split(new char[] { '^' });
                foreach (string s in faveNames)
                {
                    output.Add(s.ToUpper() );
                }
                return output;
            }
        }
    }
}
