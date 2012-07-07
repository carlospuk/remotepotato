using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using RemotePotatoServer.Properties;


namespace RemotePotatoServer
{
    /// <summary>
    /// class ThreadController
    /// The ThreadController class manages the creation, starting, and stopping
    /// of the http request processing thread.  All the functions are encapsulated
    /// so the class can be used from either a WinForms app or a Windows Service.
    /// By using once class to service both types of apps, code development can
    /// be done with a WinForms app for easy debugging.
    /// </summary>
    public class ThreadController
    {
        RPWebServer mainServer;       // the main server object that does the http setup and processing
        Thread theThread;           // the new thread the main server will live on
        System.Timers.Timer checkThreadTimer;  // checks if the thread is alive

        public ThreadController()
        {

            // DONT Initialize server here -- could delay service startup (will only proceed if not already initialized)
            // instead do it in rpwebserver() below, and flag so it only INITs once

            mainServer = new RPWebServer("Main server", Convert.ToInt32(Settings.Default.Port), "/", false);   // create the main server object
            mainServer.AccessDenied += new EventHandler(theServer_AccessDenied);
 
            ServerStoppedReason = ServerStoppedReasons.UserStopped;

            checkThreadTimer = new System.Timers.Timer(1000);
            checkThreadTimer.Interval = 1000;
            checkThreadTimer.AutoReset = true;
            checkThreadTimer.Elapsed += new System.Timers.ElapsedEventHandler(checkThreadTimer_Elapsed);
            checkThreadTimer.Start();
            
        }

        void theServer_AccessDenied(object sender, EventArgs e)
        {
            // default reason why the server stopped.
            ServerStoppedReason = ServerStoppedReasons.AccessDenied;
        }

 
        // Public methods: start / stop
        public void Start()
        {
            Functions.WriteLineToLogFile("ThreadController: Start");

            if (theThread != null)
            {
                if (theThread.IsAlive) return;
                theThread = null;
            }

            // Record why the server stopped.
            ServerStoppedReason = ServerStoppedReasons.None;

            // start the thread
            Functions.WriteLineToLogFile("ThreadController: Starting server thread.");
            ThreadStart mainThreadStart = new ThreadStart(mainServer.Start);
            theThread = new Thread(mainThreadStart); 
            // 2011-01-18: EXPERIMENTAL Use STA only.  
            //theThread.SetApartmentState(ApartmentState.STA);             // Clearly the experiment was unsuccessful.
            theThread.Start();

        }
        public void Stop()
        {
            if (theThread != null)
            {
                if (theThread.IsAlive)
                {
                    // Record why the server stopped.
                    ServerStoppedReason = ServerStoppedReasons.UserStopped;

                    // tell the server to stop.  When the ThreadEntry function exits, the thread exits
                    mainServer.Stop();
                }
            }

            // Static classes: any cleanup etc.
            try
            {
                StreamingManager.Default.CleanUp();
            }
            catch { }

        }

        // Monitor thread state changes
        public event EventHandler IsRunningChanged;
        public bool CurrentThreadAliveState;
        void checkThreadTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (IsRunning != CurrentThreadAliveState)
            {
                CurrentThreadAliveState = IsRunning;
                if (IsRunningChanged != null) IsRunningChanged(this, new EventArgs());
            }
        }


        // Properties
        public bool IsRunning
        {
            get
            {
                if (theThread == null) return false;

                return theThread.IsAlive;
            }
        }
        public ServerStoppedReasons ServerStoppedReason { get; set; }


        public enum ServerStoppedReasons
        {
            UserStopped,
            AccessDenied,
            None
        }

    }
}
