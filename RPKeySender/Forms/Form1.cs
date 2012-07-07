using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using RPKeySender.Properties;

namespace RPKeySender
{
    public partial class Form1 : Form
    {
        const int IRListenerPort = 19080;
        public Form1()
        {
            InitializeComponent();

            Load += new EventHandler(Form1_Load);
            FormClosing += new FormClosingEventHandler(Form1_FormClosing);
            Shown += new EventHandler(Form1_Shown);
            Resize += new EventHandler(Form1_Resize);   
        }

        void Form1_Resize(object sender, EventArgs e)
        {
            ShowHideNotifyIcon();
        }

        private void ShowHideNotifyIcon()
        {
            if (FormWindowState.Minimized == this.WindowState)
            {
                this.ShowInTaskbar = false;
                notifyIcon1.Visible = true;
                //notifyIcon1.ShowBalloonTip(500);
                this.Hide();
            }
            else if (FormWindowState.Normal == this.WindowState)
            {
                this.ShowInTaskbar = true;
                notifyIcon1.Visible = false;
            }
        }

        void Form1_Shown(object sender, EventArgs e)
        {
            StartServer();

            ShowHideNotifyIcon();
        }
        void Form1_Load(object sender, EventArgs e)
        {
            Functions.WriteLineToLogFile("RPKeySender: Starting Up.");

            // Bind
            BindControls();


        }
        void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                if (!ForceCloseApp)
                {
                    e.Cancel = true;
                    this.WindowState = FormWindowState.Minimized;
                }
            }
        }


        #region Server
        void StartServer()
        {
            // Fire up server
            if (!IRServer.Default.StartServer(IRListenerPort))
                MessageBox.Show("Could not start IR Server.");
        }
        #endregion


        #region Settings
        void BindControls()
        {
            this.checkBox1.DataBindings.Add(new System.Windows.Forms.Binding("Checked", Settings.Default, "LogKeys", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));           
        }
        void SaveSettings()
        {
            Settings.Default.Save();
        }
        #endregion

        private void btnShowLog_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (System.IO.File.Exists(Functions.DebugLogFileFN))
                System.Diagnostics.Process.Start(Functions.DebugLogFileFN);
            else
                MessageBox.Show("No log file found.");
        }

        private void btnQuit_Click(object sender, EventArgs e)
        {
            QuitApp();
        }
        bool ForceCloseApp = false;
        void QuitApp()
        {
            try
            {
                IRServer.Default.StopServer();
                SaveSettings();
            }
            catch (Exception ex)
            {
                Functions.WriteExceptionToLogFile(ex);
            }

            Functions.WriteLineToLogFile("RPKeySender: Exiting.");
            ForceCloseApp = true;
            this.Close();
        }

        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            System.Threading.Thread.Sleep(3000);
            CommandSender.SendMediaCenterCommand("navdown");
        }



    }
}
