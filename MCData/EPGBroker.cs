using System;
using System.Text;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections;
using Microsoft.MediaCenter.Guide;
using Microsoft.MediaCenter.Store;
using Microsoft.MediaCenter.Pvr;
using CommonEPG;

namespace CommonEPG
{
    // Talks to the object store manager and converts its classes back to CommonEPG classes...
    class EPGBroker : IDisposable
    {
        // Objects
        ObjectStoreManager storeManager;

        // Events
        public event EventHandler<DebugReportEventArgs> DebugReport;

        #region Constructor / Dispose
        public EPGBroker() { }
        public bool Initialise()
        {
            storeManager = new ObjectStoreManager();
            bool result = storeManager.Init();
            DebugNormal("Initialise result: " + result.ToString());
            return result;
        }
        public void Dispose()
        {
            storeManager.Dispose();
            storeManager = null;
        }
        #endregion


        #region Main Broker Methods - Talking to storeManager / Converting
        public Dictionary<string, TVChannel> GetAllChannels()
        {
            List<Channel> channels = storeManager.GetChannelsInFirstLineup();
            Dictionary<string, TVChannel> output = new Dictionary<string, TVChannel>();

            foreach (Channel c in channels)
            {
                TVChannel tvc = Conversion.TVChannelFromChannel(c);
                output.Add(tvc.Id, tvc);
            }

            return output;
        }

        public List<TVProgramme> GetTVProgrammes(DateRange dateRange, string[] channelIds)
        {
            List<TVProgramme> output = new List<TVProgramme>();

            List<Program> progs = storeManager.GetProgramsOnChannel();


            foreach (Program p in progs)
            {
                //TVChannel tvc = Conversion.TVChannelFromChannel(c);
                //output.Add(tvc.Id, tvc);
            }

            return output;

        }

        #endregion



        #region Debug
        void DebugNormal(string msg)
        {
            if (DebugReport != null) DebugReport(this, new DebugReportEventArgs(msg, 0, null));
        }
        #endregion

    }
}
