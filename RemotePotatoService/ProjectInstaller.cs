using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;


namespace RemotePotatoService
{
/*
 * THIS INSTALLER SETS UP THE LOG IN FOR THE SERVICE SO THAT IT CAN RETRIEVE THE CORRECT MUSIC LIBRARY INFORMATION 
 * 
 */

    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        public ProjectInstaller() : base()
        {
            InitializeComponent();
            //this.AfterInstall += new InstallEventHandler(ProjectInstaller_AfterInstall);
            this.Committing += new InstallEventHandler(ProjectInstaller_Committing);
            this.Committed += new InstallEventHandler(ProjectInstaller_Committed);

            this.BeforeUninstall += new InstallEventHandler(ProjectInstaller_BeforeUninstall);
        }

   

        // 1 - COMMITTING
        void ProjectInstaller_Committing(object sender, InstallEventArgs e)
        {
            WriteLineToLogFile("Remote Potato ProjectInstaller");
            // Do nothing
            WriteLineToLogFile("[COMMITTING]");
        }
        // 2 - COMMITTING
        void ProjectInstaller_Committed(object sender, InstallEventArgs e)
        {
            WriteLineToLogFile("[COMMITTED]");
            // Set service log on to specified username / password  (if provided)
            string AccountName = Context.Parameters["MLUSERNAME"];
            string Password = Context.Parameters["MLPASSWORD"];

            WriteLineToLogFile("Pinstaller: AccountName:" + AccountName);
            // VERIFIED OK IN COMMITTED EVENT WriteLineToLogFile("Pinstaller: Passwd:" + Password);

            if (string.IsNullOrEmpty(AccountName))
            {
                WriteLineToLogFile("No service account specified - not setting log on details.");
                return;
            }

            WriteLineToLogFile("Service account " + AccountName + " specified - setting log on details.");
            string txtError = string.Empty;
            if (!RemotePotatoServer.ServiceManager.SetRPServiceLogon(AccountName, Password, false, ref txtError))
            {
                WriteLineToLogFile("Error setting service log on: " + txtError);
            }
            else
            {
                WriteLineToLogFile("Service log on successfully changed to user account:" + AccountName);
            }
        }

        // 3- AFTER INSTALL
        void ProjectInstaller_AfterInstall(object sender, InstallEventArgs e)
        {
            // Do nothing
        }

        //  UNINSTALL
        void ProjectInstaller_BeforeUninstall(object sender, InstallEventArgs e)
        {
            // Uninstall - kill RPKeySender
            WriteLineToLogFile("[BEFORE_UNINSTALL]");
            WriteLineToLogFile("Killing RPKeySender...");
            if (ProcessKiller.KillProcessByName("RPKeySender"))
                WriteLineToLogFile("Killed.");
            else
                WriteLineToLogFile("Not found.");
        }


        public static void WriteLineToLogFile(string txtLine)
        {
            string logLine = System.String.Format("{0:G}: {1}.", System.DateTime.Now, txtLine);

            System.IO.StreamWriter sw = null;
            try
            {
                sw = System.IO.File.AppendText("C:\\InstallRPLog.txt");
                sw.WriteLine(logLine);
            }
            catch
            {
            }
            finally
            {
                sw.Close();
            }
        }



        // Override the 'Install' method.
        public override void Install(IDictionary savedState)
        {
            base.Install(savedState);
        }

        // Override the 'Commit' method.
        public override void Commit(IDictionary savedState)
        {
            base.Commit(savedState);
        }

        // Override the 'Rollback' method.
        public override void Rollback(IDictionary savedState)
        {
            base.Rollback(savedState);
        }


    }
}
