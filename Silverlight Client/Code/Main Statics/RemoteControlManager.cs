using System;
using System.Net;
using System.Xml;
using System.Xml.Serialization;
using System.Windows;
using System.Text;
using System.IO;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.Generic;
using RemotePotatoServer;
using CommonEPG;

namespace SilverPotato
{
    public static class RemoteControlManager
    {

        

        // Create Recordings
        public static event EventHandler<SendRemoteControlCommandEventArgs> SendRemoteControlCommand_Completed;
        public static void SendRemoteControlCommand(RPKeySender.MCCommands command)
        {
            
            RPWebClient client = new RPWebClient();
            client.GetStringByGettingCompleted += new EventHandler<UploadStringCompletedEventArgs>(client_GetStringByGettingCompleted);
            client.GetStringByGetting("/xml/sendremotekey/" + command.ToString());
        }

        static void client_GetStringByGettingCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                Functions.WriteLineToLogFile("RemoteControlManager: Failed to send remote control key to server:");
                Functions.WriteExceptionToLogFile(e.Error);

                if (SendRemoteControlCommand_Completed != null)
                    SendRemoteControlCommand_Completed(new object(), new SendRemoteControlCommandEventArgs("Could not connect to server - see log for more information."));
                return;
            }

            string strXML = e.Result;
            string strResponse = XMLHelper.Deserialize<string>(strXML);
            
            if (SendRemoteControlCommand_Completed != null)
                SendRemoteControlCommand_Completed(new object(), new SendRemoteControlCommandEventArgs(strResponse));
        }

       
    }


    public class SendRemoteControlCommandEventArgs : EventArgs
    {
        public string ResultText { get; set; }
        public bool Success { get; set; }

        public SendRemoteControlCommandEventArgs(string resultString)
        {
            if (string.IsNullOrEmpty(resultString))
            {
                Success = false;
                ResultText = "<null>";
            }
            else
            {
                Success = (resultString == "OK");
                ResultText = resultString;
            }
        }
    }
}

