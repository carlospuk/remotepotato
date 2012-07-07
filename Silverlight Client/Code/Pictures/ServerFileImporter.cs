using System;
using System.Net;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Xml.Serialization;
using System.Xml;
using System.IO;
using CommonEPG;

namespace SilverPotato
{
    public static class ServerFileImporter
    {

        public static event EventHandler<GenericEventArgs<FileBrowseResult>> BrowseToBrowseRequest_Completed;

        public static void BrowseToBrowseRequest(FileBrowseRequest rq)
        {
            RPWebClient client = new RPWebClient();
            
            client.GetStringByPostingCompleted += new EventHandler<UploadStringCompletedEventArgs>(client_GetStringByPostingCompleted);
            client.GetStringByPostingObject("xml/filebrowse/dir" + Settings.ZipDataStreamsAddendum, rq);
        }

        

        static void client_GetStringByPostingCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                ErrorManager.DisplayAndLogError("Could not get directory listing from server.");
                Functions.WriteExceptionToLogFile(e.Error);
                FileBrowseResult badResult = new FileBrowseResult();
                badResult.Success = false;
                badResult.ErrorText = e.Error.Message;
                if (BrowseToBrowseRequest_Completed != null)
                    BrowseToBrowseRequest_Completed(new object(), new GenericEventArgs<FileBrowseResult>(badResult));
                return;
            }

            string strOut = e.Result;
            if (Settings.ZipDataStreams)
            {
                if (!ZipManager.UnzipString(ref strOut))
                {
                    ErrorManager.DisplayAndLogError("Could not unzip downloaded directory listing batch from server.");

                    FileBrowseResult badResult = new FileBrowseResult();
                    badResult.Success = false;
                    badResult.ErrorText = "Could not unzip.";

                    if (BrowseToBrowseRequest_Completed != null)
                        BrowseToBrowseRequest_Completed(new object(), new GenericEventArgs<FileBrowseResult>(badResult));
                    return;
                }
            }

            // Prepare to deserialize
            FileBrowseResult fbr  = new FileBrowseResult();
            XmlSerializer serializer = new XmlSerializer(fbr.GetType());

            // Replace nulls - cannot be deserialized
            StringReader sr = new StringReader(strOut);

            // Dont check characters
            XmlReaderSettings xset = new XmlReaderSettings();
            xset.CheckCharacters = false;
            XmlReader xread = XmlReader.Create(sr, xset);

            // Deserialize
            fbr = (FileBrowseResult)serializer.Deserialize(xread);
            strOut = null;

            // Success
            if (BrowseToBrowseRequest_Completed != null)
                BrowseToBrowseRequest_Completed(new object(), new GenericEventArgs<FileBrowseResult>(fbr));
        }

    }
}
