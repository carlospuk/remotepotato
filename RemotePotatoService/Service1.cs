using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using RemotePotatoServer;

namespace RemotePotatoService
{
    public partial class Service1 : ServiceBase
    {
        ThreadController theController;

        public Service1()
        {
            InitializeComponent();

            theController = new ThreadController();
        }

        protected override void OnStart(string[] args)
        {
            if (!theController.IsRunning)
                
                theController.Start();
        }

        protected override void OnStop()
        {
            if (theController.IsRunning)
                theController.Stop();
        }




    }
}
