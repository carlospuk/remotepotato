using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.IO.IsolatedStorage;
using CommonEPG;
using CommonEPG.Comparers;
using System.Text;

namespace SilverPotato
{
    // Very low level caching methods
    public class TVServiceSlice
    {
        public string TVServiceID;
        public List<TVProgramme> TVProgrammes { get; set; }
        public DateTime LocalDate;

        public TVServiceSlice()
        {
            TVProgrammes = new List<TVProgramme>();
            TVServiceID = "";
            LocalDate = DateTime.Now.Date;
        }

        public TVService TVService
        {
            get
            {
                return ScheduleManager.TVServiceWithIDOrNull(TVServiceID);
            }
        }

    }
}
