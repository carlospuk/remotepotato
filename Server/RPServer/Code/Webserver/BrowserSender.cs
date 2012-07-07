using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Web;
using System.Net;
using System.IO;
using RemotePotatoServer.Properties;

namespace RemotePotatoServer
{
    public class BrowserSender
    {
        const int PAGES_EXPIRE_SECONDS = 1200; // 20 mins

        HttpListenerContext Context;
        HttpListenerRequest Request;
        HttpListenerResponse Response;
        System.Security.Principal.IPrincipal User;

        public BrowserSender(HttpListenerContext context)
        {
            Context = context;
            Request = context.Request;
            Response = context.Response;
            User = context.User;


        }

        public bool SendAuthenticationRequiredPage()
        {
            string mna = statusCodePage("401 Unauthorised", "Authentication is required in order to discover the true love of the potato.<br /><br />If you are continually receiving this message instead of a login prompt, clear your browser's cache of any active logins and try again.");
            wipeHeader();
            //addToHeader("WWW-Authenticate", "Basic realm=\"Remote Potato Server\"");
            addToHeaderMimeType("text/html");
            Response.StatusCode = 401;
            return SendToBrowser(mna, true);
        }
        public bool Send404Page()
        {
            return SendGenericStatusCodePage("404", "Not found.");
        }
        public bool SendGenericStatusCodePage(string statusCode, string statusCodeMessage)
        {
            string mna = statusCodePage(statusCode, statusCodeMessage);

            wipeHeader();
            addToHeaderMimeType("text/html");
            Response.StatusCode = Convert.ToInt32(statusCode);
            return SendToBrowser(mna, true);
        }
        private string statusCodePage(string statusCode, string statusCodeMessage)
        {
            return "<!DOCTYPE HTML PUBLIC \" -//W3C//DTD HTML 4.0 Transitional//EN\">\r\n<html><head></head><body><h1>" + statusCode + "</h1><p>" + statusCodeMessage + "</p></body></html>";
        }
        public bool SendXMLToBrowser(string body)
        {
            Functions.logAPIoutputString(body);

            wipeHeader();
            addToHeaderMimeType("application/xml;charset=utf-8");
            Response.StatusCode = 200;
            return SendToBrowser(body, true);
        }
        public bool SendStringToBrowser(string body)
        {
            return SendStringToBrowser(body, "text/plain;charset=utf-8");
        }
        public bool SendStringToBrowser(string body, string MimeType)
        {
            wipeHeader();
            addToHeaderMimeType(MimeType);
            Response.StatusCode = 200;
            return SendToBrowser(body, true);
        }
        public bool SendZipStringToBrowser(string body)
        {
            wipeHeader();
            addToHeaderMimeType("text/plain;charset=utf-8");
            Response.StatusCode = 200;
            return SendToBrowser(body, true);
        }

        
        public bool SendNormalHTMLPageToBrowser(string body)
        {
            wipeHeader();
            addToHeaderMimeType("text/html;charset=utf-8");
            Response.StatusCode = 200;
            return SendToBrowser(body, true);
        }
        public bool SendDataToBrowser(string mimeType, byte[] data)
        {
            wipeHeader();
            addToHeaderMimeType(mimeType);

            // Trim to any range request (content status code will be set to 206)
            if (Request.Headers.HasParameter("Range"))
            {
                if ((Settings.Default.DebugServer) && (Settings.Default.DebugAdvanced))
                    Functions.WriteLineToLogFile("Sending byte range..");

                bool shouldReturnContentRange = true;
                data = TrimBytesToRange(data, ref shouldReturnContentRange);

                if (!shouldReturnContentRange)  // in case it wasn't syntactically correct, return normal file (RFC 2616)
                    Response.StatusCode = 200;
            }
            else
            {
                // Send header
                Response.StatusCode = 200;
            }

            return SendToBrowser(data, true);
        }
        public bool SendFileToBrowser(string localFilePath)
        {
            return SendFileToBrowser(localFilePath, false, false);
        }
        public bool SendFileToBrowser(string localFilePath, bool remapToSkin, bool sendChunked)
        {
            return SendFileToBrowser(localFilePath, remapToSkin, sendChunked, false);
        }
        public bool SendFileToBrowser(string localFilePath, bool remapToSkin, bool sendChunked, bool makeDownloadable)
        {
            // File location
            if (remapToSkin)
            {
                localFilePath = localFilePath.Replace("skin/", "");
                while (localFilePath.StartsWith("/"))
                {
                    localFilePath = localFilePath.Substring(1);
                }
                localFilePath = localFilePath.Replace("/", "\\");

                localFilePath = Path.Combine(Themes.ActiveThemeFolder, localFilePath);
            }

            // Read file  (this MAY remap a relative path to an absolute path, so don't check if file exists ahead of this)
            byte[] bytes = FileCache.ReadBinaryFile(localFilePath);

            if (bytes.Length == 0)
            {
                if (Settings.Default.DebugAdvanced)
                    Functions.WriteLineToLogFile("SendFileToBrowser: File doesn't exist: " + localFilePath);

                return Send404Page();
            }

            // Get mime type
            wipeHeader();
            addToHeaderMimeType(Functions.MimeTypeForFileName(localFilePath));

            // Trim to any range request
            if (Request.Headers.HasParameter("Range"))
            {
                if ((Settings.Default.DebugServer) && (Settings.Default.DebugAdvanced))
                    Functions.WriteLineToLogFile("Sending byte range..");
                bool shouldReturnContentRange = true;
                bytes = TrimBytesToRange(bytes, ref shouldReturnContentRange);

                if (!shouldReturnContentRange)  // in case it wasn't syntactically correct, return normal file (RFC 2616)
                    Response.StatusCode = 200;
            }
            else
            {
                // Send header
                Response.StatusCode = 200;
            }

            // Downloadable?
            if (makeDownloadable)
            {
                string dlFilename = Path.GetFileName(localFilePath);
                addToHeader("content-disposition", "attachment; filename=" + HttpUtility.UrlEncode(dlFilename));
            }

            // Send bytes
            return SendToBrowser(bytes, true);
        }
        private byte[] TrimBytesToRange(byte[] bytes, ref bool returnPartial)
        {
            List<string> rangeBounds = new List<string>();
            
            string strRangeHeader = Request.Headers["Range"];
            strRangeHeader = strRangeHeader.ToLowerInvariant().Replace("range:", "").Trim();

            // Assume valid range
            bool syntacticRange = true;
            strRangeHeader = strRangeHeader.Replace("bytes=", "");
            
            // Get bounds of range
            rangeBounds = strRangeHeader.Split(new char[] { '-' }).ToList();
            // Check there are two values
            syntacticRange &= (rangeBounds.Count > 1);
            
            // Check the values are numbers
            int rangeStart, rangeEnd;
            syntacticRange &= int.TryParse(rangeBounds[0], out rangeStart);
            syntacticRange &= int.TryParse(rangeBounds[1], out rangeEnd);

            // Unsyntactic range SHOULD be treated as if it does not exist (RFC 2616)
            if (!syntacticRange)
            {
                returnPartial = false;
                return bytes;
            }

            // Syntactically correct request; we'll be returning a content range
            returnPartial = true;

            // Check valid range
            bool validRange = true;
            // Too long?
            int rangeLength = (rangeEnd - rangeStart) + 1;  // e.g. byte 0-0 has a length of 1
            validRange &= (rangeStart < bytes.Length);
            validRange &= (rangeStart >= 0);
            validRange &= (rangeEnd < bytes.Length);
            validRange &= (rangeEnd >= rangeStart);

            // Invalid request?
            if (! validRange)
            {
                // Return no data and invalid header
                Response.StatusCode = 416;
                byte[] blankBytes = new byte[] { };
                return blankBytes;
            }

            // Valid request - trim bytes
            byte[] newBytes = new byte[rangeLength];
            Buffer.BlockCopy(bytes, rangeStart, newBytes, 0, rangeLength);

            // Set header to 'partial content'
            Response.StatusCode = 206;

            // Set content header
            string strContentRangeHeaderValue = string.Format("bytes {0}-{1}/{2}", rangeStart, rangeEnd, bytes.Length);
            Response.Headers.Add("Content-Range", strContentRangeHeaderValue);

            return newBytes;
        }
        public bool SendLogoToBrowser(string logoSvcID)
        {
            if (String.IsNullOrEmpty(logoSvcID)) return false;
            logoSvcID = logoSvcID.Trim();

            byte[] bytes = new byte[] { };
            if (! EPGManager.GetLogoDataForCallsign(logoSvcID, 50, 50, out bytes))
                return Send404Page();

            return SendDataToBrowser("image/png", bytes);
        }
        private void addToHeader(string theKey, string theValue)
        {
            
            Response.Headers.Add(theKey, theValue);
        }
        private void addToHeaderMimeType(string mimeType)
        {
            addToHeader("Content-Type", mimeType);
        }
        private void addToHeaderDate(DateTime theDate)
        {
            //addToHeader("Date", theDate.ToUniversalTime().ToLongDateString() + " " + theDate.ToUniversalTime().ToLongTimeString());
            // avoid foreign characters
            addToHeader("Date", theDate.ToUniversalTime().ToString("yyyy-MM-dd HH:mm"));
        }
        private void wipeHeader()
        {
            Response.Headers.Clear();
            addToHeader("Server", "remote-potato-v1");
            addToHeader("Accept-Ranges", "bytes");
            //addToHeader("Cache-Control", "no-cache");
            addToHeader("Expires", DateTime.Now.ToUniversalTime().AddSeconds(PAGES_EXPIRE_SECONDS).ToString("yyyy-MM-dd HH:mm"));
            Response.Headers.Add(HttpResponseHeader.Date, DateTime.Now.ToString());
        }
        private string txtHeaderKVP(KeyValuePair<string, string> kvp)
        {
            return kvp.Key + ": " + kvp.Value;
        }
        
        public bool SendToBrowser(String sData, bool CloseSocket)
        {
            return SendToBrowser(Encoding.UTF8.GetBytes(sData), CloseSocket);
        }
        public bool SendToBrowser(Byte[] bSendData, bool CloseSocket)
        {
            try
            {
                Response.ContentEncoding = Encoding.UTF8;
                // TEST
                Response.KeepAlive = false;
                Response.ContentLength64 = bSendData.Length;

                if (Settings.Default.DebugServer)
                    Functions.WriteLineToLogFile("SendToBrowser: No. of bytes sent: " + bSendData.Length.ToString());

                System.IO.Stream output = Response.OutputStream;
                output.Write(bSendData, 0, bSendData.Length);

                // Add some extra bytes
                if (Settings.Default.PadResponseWithExtraBytes)
                {
                    for (int i = 0; i < 30; i++)
                        Response.OutputStream.WriteByte(new byte());
                }

                //Response.OutputStream.Flush();  // prob not needed
                if (CloseSocket) output.Close();

                // Advanced debug
                if ((Settings.Default.DebugAdvanced) && (Settings.Default.DebugServer))
                {
                    Functions.WriteLineToLogFile("Headers sent to client:");
                    for (int i = 0; i < Response.Headers.Count; ++i)
                        Functions.WriteLineToLogFile(string.Format("{0}: {1}", Response.Headers.Keys[i], Response.Headers[i]));
                }

                // Success
                return true;
            }
            catch (HttpListenerException hex)  // This often happens when the client has disconnected
            {
                if (!hex.ErrorCode.Equals(64))
                {
                    Functions.WriteLineToLogFile("SendToBrowser: Unexpected HTTP Listener exception:");
                    Functions.WriteExceptionToLogFile(hex);
                }
            }
            catch (Exception e)
            {
                Functions.WriteLineToLogFile("SendToBrowser: Error Occurred : ");
                Functions.WriteExceptionToLogFile(e);
            }

            return false;
        }



    }
}

