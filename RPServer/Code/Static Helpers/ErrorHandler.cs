using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using RemotePotatoServer.Properties;
using CommonEPG;

namespace RemotePotatoServer
{
    public sealed class ErrorHandler
    {
        bool IsInitialized;
        private ErrorHandler() // don't allow
        { }

        object initLock = new object();
        public void Initialize()
        {
            Monitor.Enter(initLock);
            if (IsInitialized)
            {
                Functions.WriteLineToLogFile("ErrorHandler: Bailing out of Init, already initialized.");
                return;
            }

            // Hook up error handling events for static classes
            RecTV.Default.DebugReport += new EventHandler<DebugReportEventArgs>(RecTV_DebugReport);
            EPGManager.EPGDebugReport += new EventHandler<DebugReportEventArgs>(Object_DebugReport);
            Functions.WriteLineToLogFile("ErrorHandler: Initialised.");

            IsInitialized = true;
            Monitor.Exit(initLock);
        }

        void RecTV_DebugReport(object sender, DebugReportEventArgs e)
        {
            if (! Settings.Default.DebugRecTV) return;

            Functions.WriteLineToLogFile(e.DebugText);
            if (e.ThrownException != null)
                Functions.WriteExceptionToLogFile(e.ThrownException);
            
        }

        void Object_DebugReport(object sender, DebugReportEventArgs e)
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



        #region Singleton Methods
        static ErrorHandler instance = null;
        static readonly object padlock = new object();
        internal static ErrorHandler Default
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new ErrorHandler();
                    }
                    return instance;
                }
            }
        }
        #endregion


    }
}
