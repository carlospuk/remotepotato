using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Diagnostics;

namespace RemotePotatoServer
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // If the installer has run this (i.e. as a custom action after installation) it will have
            // passed the product name to us, which is also the name of the installer window.
            // If so wait until the installer has finished.
            Process installProc = null;
            string[] args = Environment.GetCommandLineArgs();

            if (args.Length == 2)
            {
                Process[] localAll = Process.GetProcesses();
                foreach (Process p in localAll)
                    if (p.MainWindowTitle == args[1])
                    {
                        installProc = p;
                        break;
                    }
                
                if (installProc != null)
                    installProc.WaitForExit();
            }

            // Start app - create new window
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
