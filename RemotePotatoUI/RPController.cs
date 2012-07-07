using System;
using System.Collections.Generic;
using System.Text;
using RemotePotatoServer;

namespace RemotePotatoServer
{
    /// <summary>
    /// Locate a running processor of Remote Potato, provide access to settings, start/stop methods, etc.
    /// </summary>
    public class RPController
    {
        // Holds the thread controller, which contains the web server
        ThreadController controller;

        // Constructor
        public RPController()
        {
            controller = new ThreadController();
            controller.IsRunningChanged += new EventHandler(controller_IsRunningChanged);
        }

        // Public Methods
        public void Start()
        {
            controller.Start();
        }
        public void Stop()
        {
            controller.Stop();
        }


        // Monitor server status
        // Events
        public event EventHandler ServerStatusChange;
        void controller_IsRunningChanged(object sender, EventArgs e)
        {
            if (ServerStatusChange != null) ServerStatusChange(this, new EventArgs());
        }


        // Properties
        public bool ServerIsRunning
        {
            get
            {
                return controller.IsRunning;
            }
        }


        public enum ServerRunTypes
        {
            RunAsService,
            RunAsApplication
        }

    }
}
