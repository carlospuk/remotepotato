using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Austin.HttpApi;
using System.Security.Principal;

namespace RemotePotatoServer
{
    public partial class URLReserver
    {
        private int Port;
        private bool AddingSecurity;
        public int ExitCode;


        public URLReserver()
        { }
        public int ReserveUrl(int port, string path, bool addSecurity)
        {
            Port = port;
            AddingSecurity = addSecurity;
       
            string action = AddingSecurity ? "set" : "removed";

            string result = "";
            if (!ChangeUrlReservation(Port, path, AddingSecurity, out result))
            {
                string logEntry = "The required Url Reservation for port " + Port.ToString() + " has NOT been " + action + ":" + result;
                Functions.WriteLineToLogFile(logEntry);
                return 99;
            }
            
            Console.WriteLine("The required Url Reservation for port " + Port.ToString() + " has been " + action + " for the Remote Potato webserver.");
            Functions.WriteLineToLogFile("URLReserver: Url was " + action + " OK.");
            return 0;
        }

        bool ChangeUrlReservation(int port, string path, bool add, out string txtResult)
        {
            // ADD USER ACCOUNT
            //NTAccount act;
            SecurityIdentifier sid;
            try
            {
                sid = new SecurityIdentifier("S-1-1-0");  // Everyone
            }
            catch (Exception ex)
            {
                txtResult = "Couldn't create security identifier for 'Everyone':" + ex.Message;
                Functions.WriteLineToLogFile("URLReserver: Couldn't create security identifier for 'Everyone':");
                Functions.WriteExceptionToLogFile(ex);
                return false;
            }

            string urlRequired = "http://+:" + port.ToString() + path;
            Functions.WriteLineToLogFile("URLReserver: Reserving Url " + urlRequired);

            try
            {
                UrlReservation rev = new UrlReservation(urlRequired);
                Functions.WriteLineToLogFile("URLReserver: Created reservation object.");
                rev.AddSecurityIdentifier(sid);
                Functions.WriteLineToLogFile("URLReserver: Added security identifier.");

                if (add)
                {
                    Functions.WriteLineToLogFile("URLReserver: Trying to reserve Url...");
                    rev.Create();
                    Functions.WriteLineToLogFile("URLReserver: Reserved Url OK.");
                }
                else
                {
                    Functions.WriteLineToLogFile("URLReserver: Trying to remove Url...");
                    rev.Delete();
                    Functions.WriteLineToLogFile("URLReserver: Removed Url OK.");
                }
            }
            catch (Exception ex)
            {
                txtResult = "Couldn't make reservation object or couldn't add SID:  " +  ex.Message;
                Functions.WriteExceptionToLogFile(ex);
                return false;
            }


            txtResult = "OK";
            return true;
        }


    }
}
