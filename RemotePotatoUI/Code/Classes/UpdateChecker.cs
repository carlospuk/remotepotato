using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Xml;
using System.IO;

namespace RemotePotatoServer.UI
{
    public class UpdateChecker
    {

        public bool IsUpdateAvailable(string ProductCode, Version thisVersion, ref Version newVersion, ref string newVersionDescription)
        {

            string txtUri = "http://www.fatattitude.com/software/webservice/updatecheck?productcode=" + ProductCode;

            bool wasError = false;
            string errorText = string.Empty;
            try
            {
                WebClient wc = new WebClient();
                string xmlResponse = wc.DownloadString(txtUri);

                StringReader sr = new StringReader(xmlResponse);
                XmlTextReader textReader = new XmlTextReader(sr);

                // Read until end of file

                while (textReader.Read())
                {
                    switch (textReader.NodeType)
                    {
                        case XmlNodeType.Element:
                            switch (textReader.Name)
                            {
                                case "result":
                                    string txtResult = textReader.ReadString();
                                    if ((!(string.IsNullOrEmpty(txtResult))) && (txtResult.Equals("ERROR")))
                                        wasError = true;
                                    break;

                                case "errortext":
                                    errorText = textReader.ReadString();
                                    break;

                                case "latestversion":
                                    string strVersion = textReader.ReadString();
                                    if (!(string.IsNullOrEmpty(strVersion)))
                                        newVersion = new Version(strVersion);
                                    break;

                                case "description":
                                    newVersionDescription = textReader.ReadString();
                                    break;

                                default: // unknown element name
                                    break;
                            }
                            break;

                        default:
                            break;
                    }
                }


                // Close off
                textReader.Close();
                sr.Close();
            }
            catch (Exception ex)
            {
                wasError = true;
                errorText = ex.Message;
            }

            if (wasError)
            {
                Functions.WriteLineToLogFile("UpdateChecker: Failed to check for update: " + errorText);
                return false;
            }

            // Successful check: new version?
            return (newVersion > thisVersion);
        }

    }

}
