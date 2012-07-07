using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Text;
using System.Windows;
using CommonEPG;
using System.Xml.Serialization;
using System.Xml;


namespace SilverPotato
{
    public static class PictureImporter
    {
        public static event EventHandler<GenericEventArgs<RPPictureBatch>> ImportPictureBatchCompleted;

        public static void ImportPictureBatch(int startAt)
        {
            RPWebClient client = new RPWebClient();
            client.GetStringByGettingCompleted += new EventHandler<UploadStringCompletedEventArgs>(ImportPictureBatch_GetStringByGettingCompleted);
            client.GetStringByGetting("xml/pictures/batch/" + startAt.ToString() + Settings.ZipDataStreamsAddendum);
        }
        static void ImportPictureBatch_GetStringByGettingCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                ErrorManager.DisplayAndLogError("Could not get picture batch from server.");
                Functions.WriteExceptionToLogFile(e.Error);
                ImportPictureBatchCompleted(new object(), new GenericEventArgs<RPPictureBatch>(new RPPictureBatch()));
                return;
            }

            string strOut = e.Result;
            if (Settings.ZipDataStreams)
            {
                if (!ZipManager.UnzipString(ref strOut))
                {
                    ErrorManager.DisplayAndLogError("Could not unzip downloaded picture batch from server.");
                    ImportPictureBatchCompleted(new object(), new GenericEventArgs<RPPictureBatch>(new RPPictureBatch()));
                    return;
                }
            }

            // Prepare to deserialize
            RPPictureBatch pics = new RPPictureBatch();
            XmlSerializer serializer = new XmlSerializer(pics.GetType());

            // Replace nulls - cannot be deserialized
            StringReader sr = new StringReader(strOut);

            // Dont check characters
            XmlReaderSettings xset = new XmlReaderSettings();
            xset.CheckCharacters = false;
            XmlReader xread = XmlReader.Create(sr, xset);

            // Deserialize
            pics = (RPPictureBatch)serializer.Deserialize(xread);
            strOut = null;

            // Success
            ImportPictureBatchCompleted(new object(), new GenericEventArgs<RPPictureBatch>(pics));
        }
        /// <summary>
        /// Remove illegal XML characters from a string.
        /// </summary>
        public static string SanitizeXmlString(string xml)
        {
            if (xml == null)
            {
                throw new ArgumentNullException("xml");
            }

            StringBuilder buffer = new StringBuilder(xml.Length);

            foreach (char c in xml)
            {
                if (IsLegalXmlChar(c))
                {
                    buffer.Append(c);
                }
            }

            return buffer.ToString();
        }
        /// <summary>
        /// Whether a given character is allowed by XML 1.0.
        /// </summary>
        public static bool IsLegalXmlChar(int character)
        {
            return
            (
                 character == 0x9 /* == '\t' == 9   */          ||
                 character == 0xA /* == '\n' == 10  */          ||
                 character == 0xD /* == '\r' == 13  */          ||
                (character >= 0x20 && character <= 0xD7FF) ||
                (character >= 0xE000 && character <= 0xFFFD) ||
                (character >= 0x10000 && character <= 0x10FFFF)
            );
        }



    }
}
