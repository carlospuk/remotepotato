using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace RPKeySender
{
    class Program
    {


        [DllImport("user32.dll")]
[return: MarshalAs(UnmanagedType.Bool)]
static extern bool SetForegroundWindow(IntPtr hWnd);

        static void Main()
        {
            bool createdNew = true;
            using (Mutex mutex = new Mutex(true, "Global\\RPKeySender", out createdNew))
            {
                if (createdNew)
                {
                    // Start app - create new window
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    try
                    {
                        Application.Run(new Form1());
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            Functions.WriteLineToLogFile("Top level exception:");
                            Functions.WriteExceptionToLogFile(ex);
                        }
                        catch { }
                    }
                }
                else
                {
                    MessageBox.Show("The Remote Potato IR Sender App is already running.\r\nDouble click the icon in the System Tray to open it.");
                }
            }
        }
    }
}
