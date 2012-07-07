using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Text;
using RemotePotatoServer.Properties;
using CommonEPG;

namespace RemotePotatoServer
{
    /// <summary>
    /// Handles initialization and error event handling 
    /// </summary>
    public sealed class Initialization
    {
        bool IsInitialized = false;
        static object initLock = new object();
        private Initialization()  // do not allow an instance
        { }
        internal bool Initialize(bool initErrorHandler, bool initRecTV, bool initEPG)
        {
            Monitor.Enter(initLock);
            if (IsInitialized)
            {
                Functions.WriteLineToLogFile("Init:  Server already initialized, bailing out of init.");
                Monitor.Exit(initLock);
                return true;
            }

            Functions.WriteLineToLogFile("Init: Initialise() Beginning");

            // LEgacy app running?
            if (Functions.isLegacyAppRunning())
            {
                Functions.WriteLineToLogFile("Init: Error - conflict - RemotePotato.exe (legacy app) is running.");
                Monitor.Exit(initLock);
                return false;
            }

            // Hook up events
            if (initErrorHandler)
            {
                Functions.WriteLineToLogFile("Init:  Initializing Error Handler...");
                ErrorHandler.Default.Initialize();
            }

            // RecTV
            if (initRecTV)
            {
                Functions.WriteLineToLogFile("Init:  Initializing RecTV...");
                RecTV.Default.Initialize();
            }
        
            // Force Setting//LEGACYREMOVE ?  (leave for a few months)
            if ((initEPG) && Settings.Default.EnableMediaCenterSupport)
            {
                Functions.WriteLineToLogFile("Init:  Initia1izing EPG...");
                Settings.Default.EnableEPG = true;
                if (EPGManager.Initialise())
                {
                    Functions.WriteLineToLogFile("Init:  Initialized EPG - loading EPG Data");
                    
                    //EPGManager.PopulateTVChannels(false);  
                    EPGManager.UpdateTVChannels();  // load TV channels from disk cache then update by combining with channels from media center : Includes a call to EPGManager.PopulateTVChannels(false)

                    EPGManager.ReloadAllRecordings();
                    Functions.WriteLineToLogFile("Init:  Loaded EPG Data");
                }
                else
                {
                    Functions.WriteLineToLogFile("ERROR - could not initialise the EPG Manager .");
                    IsInitialized = false;
                    Monitor.Exit(initLock);
                    return false;
                }
            }

            // DNS CLIENT
            if (Settings.Default.DynDNSClientEnabled)
            {
                Functions.WriteLineToLogFile("Init:  Initializing Dynamic DNS Client...");
                DNSHelper.Default.Start();
                Functions.WriteLineToLogFile("Init:  Initialized Dynamic DNS Client...");
            }

            IsInitialized = true;

            Functions.WriteLineToLogFile("Init: Initialise() Ending");

            Monitor.Exit(initLock);
            return true;
        }
        static object uninitLock = new object();
        internal bool UnInitialize()
        {
            Monitor.Enter(uninitLock);

            // DNS CLIENT
            if (Settings.Default.DynDNSClientEnabled)
            {
                Functions.WriteLineToLogFile("Init:  UnInitializing Dynamic DNS Client...");
                DNSHelper.Default.Stop();
                Functions.WriteLineToLogFile("Init:  UnInitialized Dynamic DNS Client...");
            }

            IsInitialized = false;

            Monitor.Exit(uninitLock);
            return false;
        }

        #region Error / Debug Handling
        static void Object_DebugReport(object sender, DebugReportEventArgs e)
        {
            bool logReport = false;

            if (e.Severity < 10)
                if (Settings.Default.DebugAdvanced)
                    logReport = true;

            if (e.Severity >= 10)
                logReport = true;

            if (logReport)
            {
                Functions.WriteLineToLogFile(e.DebugText);
                if (e.ThrownException != null)
                    Functions.WriteExceptionToLogFile(e.ThrownException);
            }
        }

        #endregion





        #region Singleton Methods
        static Initialization instance = null;
        static readonly object padlock = new object();
        internal static Initialization Default
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new Initialization();
                    }
                    return instance;
                }
            }
        }
        #endregion

       
    }
}
