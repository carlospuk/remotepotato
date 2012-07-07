using System;
using System.Net;
using System.Windows;
using FatAttitude.WTVTranscoder;
using FatAttitude.MediaStreamer;
using RemotePotatoServer;
using System.Collections.Generic;

namespace SilverPotato
{
    public static class StreamingManager
    {

        // public static event EventHandler<GenericEventArgs<string>> StreamIsReady;

        #region WMSP
        public static event EventHandler<GenericEventArgs<WTVStreamingVideoResult>> StartStreamingFile_Completed;
        public static void StartStreamingFromWMSPStreamingRequest(WTVStreamingVideoRequest svrq)
        {
            Functions.WriteLineToLogFile("Requesting WMSP stream...");
            RPWebClient client = new RPWebClient();
            client.GetStringByPostingCompleted += new EventHandler<UploadStringCompletedEventArgs>(client_UploadRPStringCompleted);
            client.GetStringByPostingObject("/xml/stream/start", svrq);
        }
        static void client_UploadRPStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            
            if (e.Error != null)
            {
                Functions.WriteLineToLogFile("Error contacting server.");
                Functions.WriteExceptionToLogFile(e.Error);

                WTVStreamingVideoResult badstreamResult = new WTVStreamingVideoResult(DSStreamResultCodes.Error, "Error contacting server.");
                if (StartStreamingFile_Completed != null) StartStreamingFile_Completed(new object(), new GenericEventArgs<WTVStreamingVideoResult>(badstreamResult));
                return;
            }

            // Deserialize...
            WTVStreamingVideoResult streamResult = XMLHelper.Deserialize<WTVStreamingVideoResult>(e.Result);
            if (streamResult == null)
            {
                Functions.WriteLineToLogFile("Error deserializing stream result object.");

                WTVStreamingVideoResult badstreamResult = new WTVStreamingVideoResult(DSStreamResultCodes.Error, "Error deserializing stream result.");
                if (StartStreamingFile_Completed != null) StartStreamingFile_Completed(new object(), new GenericEventArgs<WTVStreamingVideoResult>(badstreamResult));
                return;
            }

            Functions.WriteLineToLogFile("Stream request returned.  (Response was '" + streamResult.ResultCode.ToString() + "')");

            // Return the result
            if (StartStreamingFile_Completed != null) StartStreamingFile_Completed(new object(), new GenericEventArgs<WTVStreamingVideoResult>(streamResult));
            
        }
        #endregion

        #region HLS
        public static event EventHandler<GenericEventArgs<MediaStreamingResult>> StartStreamingFileByHLS_Completed;
        public static void StartStreamingFromHLSStreamingRequest(MediaStreamingRequest msrq)
        {
            Functions.WriteLineToLogFile("Requesting file HLS...");
            RPWebClient client = new RPWebClient();
            client.GetStringByPostingCompleted += new EventHandler<UploadStringCompletedEventArgs>(client_HLS_UploadRPStringCompleted);
            client.GetStringByPostingObject("xml/mediastream/start/bymediastreamingrequest", msrq);
        }
        static void client_HLS_UploadRPStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {

            if (e.Error != null)
            {
                Functions.WriteLineToLogFile("Error contacting server.");
                Functions.WriteExceptionToLogFile(e.Error);

                MediaStreamingResult badstreamResult = new MediaStreamingResult(MediaStreamingResultCodes.NamedError,  "Error contacting server.");
                if (StartStreamingFileByHLS_Completed != null) StartStreamingFileByHLS_Completed(new object(), new GenericEventArgs<MediaStreamingResult>(badstreamResult));
                return;
            }

            // Deserialize...
            MediaStreamingResult streamResult = XMLHelper.Deserialize<MediaStreamingResult>(e.Result);
            if (streamResult == null)
            {
                Functions.WriteLineToLogFile("Error deserializing stream result object.");

                MediaStreamingResult badstreamResult = new MediaStreamingResult(MediaStreamingResultCodes.NamedError, "Error deserializing stream result.");
                if (StartStreamingFileByHLS_Completed != null) StartStreamingFileByHLS_Completed(new object(), new GenericEventArgs<MediaStreamingResult>(badstreamResult));
                return;
            }

            Functions.WriteLineToLogFile("Stream request returned.  (Response was '" + streamResult.ResultCode.ToString() + "')");

            // Return the result
            if (StartStreamingFileByHLS_Completed != null) StartStreamingFileByHLS_Completed(new object(), new GenericEventArgs<MediaStreamingResult>(streamResult));

        }

        public static event EventHandler<ProbeFileResultEventArgs> ProbeFile_Completed;
        public static void ProbeFile(string FN)
        {
            Functions.WriteLineToLogFile("Probing file on server...");
            RPWebClient client = new RPWebClient();
            client.GetStringByPostingCompleted += new EventHandler<UploadStringCompletedEventArgs>(probeFile_GetStringByPostingCompleted);

            string b64FN = Functions.EncodeToBase64(FN);
            string escFN = Uri.EscapeUriString(b64FN);

            
            client.GetStringByPostingString("xml/mediastream/probe/byfilename64", escFN);
        }

        static void probeFile_GetStringByPostingCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            ProbeFileResultEventArgs args = null;

            if (e.Error != null)
            {
                Functions.WriteLineToLogFile("Error contacting server to probe file.");
                Functions.WriteExceptionToLogFile(e.Error);

                args = new ProbeFileResultEventArgs(e.Error.Message);
            }
            else
            {

                // Deserialize...
                List<AVStream> streams = new List<AVStream>();
                streams = XMLHelper.Deserialize<List<AVStream>>(e.Result);
                if (streams == null)
                {
                    string errText = "Error deserializing probe streams object.";
                    Functions.WriteLineToLogFile(errText);
                    args = new ProbeFileResultEventArgs(errText);
                }
                else
                {
                    Functions.WriteLineToLogFile("Probe returned.  (Number of streams: " + streams.Count.ToString() + ")");
                    args = new ProbeFileResultEventArgs(streams);
                }
            }

            // Return the result
            if (ProbeFile_Completed != null) ProbeFile_Completed(new object(), args);
        }
        #endregion

        

        /// <summary>
        /// Stop an HTTP Live Stream.  Doesn't return
        /// </summary>
        /// <param name="streamerID"></param>
        public static void StopStreamingFromHLSID(int streamerID)
        {
            Functions.WriteLineToLogFile("Stopping HLS Stream...");
            RPWebClient client = new RPWebClient();
            client.GetStringByGetting("xml/mediastream/stop/" + streamerID.ToString());
        }

        // Settings
        public static WTVProfileQuality DefaultStreamingQuality
        {
            get
            {
                return (FatAttitude.WTVTranscoder.WTVProfileQuality)SettingsImporter.SettingAsIntOrZero("SilverlightStreamingQuality");
            }
        }
        public static WTVProfileTVSystem DefaultTVSystem
        {
            get
            {
                bool defaultToPal = SettingsImporter.SettingIsTrue("SilverlightStreamingVideoIsPAL");
                return defaultToPal ? WTVProfileTVSystem.PAL : WTVProfileTVSystem.NTSC;
            }
        }

    }

    public class ProbeFileResultEventArgs : EventArgs
    {
        public bool Success { get; set; }
        public string ErrorText { get; set; }
        public List<AVStream> Streams { get; set; }

        public ProbeFileResultEventArgs(string txtError)
        {
            Streams = new List<AVStream>();
            Success = false;
            ErrorText = txtError;
        }
        public ProbeFileResultEventArgs(List<AVStream> _streams)
        {
            Success = true;
            Streams = _streams;
            ErrorText = "";
        }
    }

}
