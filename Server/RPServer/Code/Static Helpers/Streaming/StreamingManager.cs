using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Web;
using System.Linq;
using System.Timers;
using FatAttitude;
using FatAttitude.MediaStreamer;
using FatAttitude.Functions;
using RemotePotatoServer.Properties;

namespace RemotePotatoServer
{
    public sealed class StreamingManager
    {
        Dictionary<int,MediaStreamer> mediaStreamers;
        Timer JanitorTimer;


        StreamingManager()
        {
            // Set up streamers
            mediaStreamers = new Dictionary<int,MediaStreamer>();

            // Delete all streaming files
            DeleteAllStreamingFiles();

            InitJanitor();
        }
        public void CleanUp()
        {
            Functions.WriteLineToLogFile("StreamingManager: Cleaning Up.");
            StopAllStreamers();
        }
        public void StopAllStreamers()
        {
            List<MediaStreamer> streamersToStop = new List<MediaStreamer>();
            foreach (MediaStreamer ms in mediaStreamers.Values)
            {
                streamersToStop.Add(ms);
            }

            foreach (MediaStreamer ms in streamersToStop)
            {
                StopStreamer(ms.ID);
            }
        }

        #region Manage Streamers
        MediaStreamer GetStreamerByID(int id)
        {
            if (mediaStreamers.ContainsKey(id))
                return mediaStreamers[id];
            else
                return null;
        }
        void AddNewStreamer(MediaStreamer newStreamer)
        {
            mediaStreamers.Add(newStreamer.ID, newStreamer);

            // Power options
            SetPowerOptions();
        }
        int newUniqueID()
        {
            int newID ;
            do
            {
                Random r = new Random();
                newID = r.Next(10000,99999);
            }
            while (mediaStreamers.ContainsKey(newID));

            return newID;
        }
        void RemoveStreamer(int id)
        {
            MediaStreamer ms = GetStreamerByID(id);
            if (ms != null)
                mediaStreamers.Remove(id);

            // Power options
            SetPowerOptions();

#if ! DEBUG
            // Delete the streaming files.  If there are no streamers left, delete all streaming files
            if (mediaStreamers.Count > 0)
                DeleteStreamingFiles(id);
            else
                DeleteAllStreamingFiles();
#endif
        }
        void SetPowerOptions()
        {
            if (mediaStreamers.Count > 0)
            {
                PowerHelper.PreventStandby();
            }
            else
            {
                PowerHelper.AllowStandby();
            }
        }
        void DeleteAllStreamingFiles()
        {
            try
            {
                Directory.Delete(Functions.StreamBaseFolder, true);
            }
            catch { }
        }
        void DeleteStreamingFiles(int id)
        {
            try
            {
                string OutputBasePath = Path.Combine(Functions.StreamBaseFolder, id.ToString());
                Directory.Delete(OutputBasePath, true);
            }
            catch { }
        }
        // Janitor, sweep up ancient streamers that may have failed
        void InitJanitor()
        {
            JanitorTimer = new Timer(3600000);
            JanitorTimer.AutoReset = true;
            JanitorTimer.Elapsed += new ElapsedEventHandler(JanitorTimer_Elapsed);
            JanitorTimer.Start();
        }

        void JanitorTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            JanitorSweepUp();
        }
        void JanitorSweepUp()
        {
            if (Settings.Default.DebugAdvanced)
                Functions.WriteLineToLogFile("MediaStream Janitor:  Checking for old streamers.");

            List<int> deletions = new List<int>();

            foreach (MediaStreamer ms in mediaStreamers.Values)
            {
                TimeSpan ts = DateTime.Now.Subtract(ms.CreationDate);

                if (ts.TotalHours > 5) // more than five hours old
                {
                    Functions.WriteLineToLogFile("MediaStream Janitor:  Sweeping up streamer " + ms.ID.ToString() + " which is " + ts.TotalHours.ToString() + " old.");
                    deletions.Add(ms.ID);
                }
            }

            // Prune old streamers
            foreach (int i in deletions)
            {
                StopStreamer(i);  // This stops and also removes it
            }
        }
        #endregion

        /// <summary>
        /// Legacy for older iOS clients
        /// </summary>
        /// <param name="streamerID"></param>
        /// <returns></returns>
        public string KeepStreamerAliveAndReturnStatus(int streamerID)
        {
            MediaStreamer mediaStreamer = GetStreamerByID(streamerID);
            if (mediaStreamer == null) return "disposed";


            return "streamavailable";  // stream is always available now

        }
        /// <summary>
        /// Stop a streamer and remove it from the local list of streamers
        /// </summary>
        /// <param name="streamerID"></param>
        /// <returns></returns>
        public bool StopStreamer(int streamerID)
        {
            if (Settings.Default.DebugStreaming)
                Functions.WriteLineToLogFile("StreamingManager: Received stop command for streamer " + streamerID.ToString() );
            try
            {

                MediaStreamer mediaStreamer = GetStreamerByID(streamerID);
                if (mediaStreamer == null) return false;

                // Abort streamer (on a different thread)
                AbortMediaStreamerAndDeleteFiles((object)mediaStreamer);
                /*
                System.Threading.ParameterizedThreadStart ts = new System.Threading.ParameterizedThreadStart(AbortMediaStreamerAndDeleteFiles);
                System.Threading.Thread t_abortStreamer = new System.Threading.Thread(ts);
                t_abortStreamer.Start(mediaStreamer); */

                // Remove from streamers
                RemoveStreamer(streamerID);

                return true;
            }
            catch (Exception ex)
            {
                Functions.WriteExceptionToLogFileIfAdvanced(ex);
            }
            return false;
        }
        void AbortMediaStreamerAndDeleteFiles(object obj)
        {
            try
            {
                MediaStreamer ms = (MediaStreamer)obj;
                ms.AbortStreaming(true);
            }
            catch (Exception ex)
            {
                // Must catch exceptions on other threads
                Functions.WriteExceptionToLogFileIfAdvanced(ex);
            }
        }
        public MediaStreamingResult StartStreamer(MediaStreamingRequest request, string HostName)
        {
            int newStreamerID = newUniqueID();

            // Universal workaround: can be removed once new iOS app introduced that sets the Client Device to 'iphone3g'
            // (desirable to remove it since this will also affect silverlive streaming)
            if (string.IsNullOrEmpty(request.ClientID))
            {
                request.ClientID = "ios";
                request.ClientDevice = "iphone3g";
            }

            try
            {
                

                // Legacy clients (e.g. iOS client) don't have any custom parameters - set them now based on 'Quality'
                if (!request.UseCustomParameters) // if there are no custom parameters
                {
                    // Create/update video encoding parameters (also transfers Aspect Ratio into child 'encoding parameters' object)
                    MediaStreamingRequest.AddVideoEncodingParametersUsingiOSQuality(ref request);
                }

                /* ************************************************************
                // Override any video encoding parameters from server settings
                 ************************************************************ */
                // 1. Audio Volume
                if (Settings.Default.StreamingVolumePercent != 100)
                    request.CustomParameters.AudioVolumePercent = Convert.ToInt32(Settings.Default.StreamingVolumePercent);

                // 2. Custom FFMPEG template
                if ( (Settings.Default.UseCustomFFMpegTemplate) &  (!string.IsNullOrWhiteSpace(Settings.Default.CustomFFMpegTemplate))  )
                        request.CustomParameters.CustomFFMpegTemplate = Settings.Default.CustomFFMpegTemplate.Trim();

                // 3. iPhone 3G requires profile constraints
                if (request.ClientDevice.ToLowerInvariant() == "iphone3g")
                {
                    request.CustomParameters.X264Level = 30;
                    request.CustomParameters.X264Profile = "baseline";
                }

                // 4. Deinterlace obvious WMC video
                if (
                    (request.InputFile.ToUpper().EndsWith("WTV")) ||
                    (request.InputFile.ToUpper().EndsWith("DVR-MS"))
                    )
                {
                    request.CustomParameters.DeInterlace = true;
                }
                
                // Create the streamer
                MediaStreamer mediaStreamer = new MediaStreamer(newStreamerID, request, Functions.ToolkitFolder, Settings.Default.MediaStreamerSecondsToKeepAlive, Settings.Default.DebugAdvancedStreaming);
                mediaStreamer.DebugMessage += new EventHandler<FatAttitude.GenericEventArgs<string>>(mediaStreamer_DebugMessage);

                mediaStreamer.AutoDied += new EventHandler(mediaStreamer_AutoDied);
                AddNewStreamer(mediaStreamer);

                Functions.WriteLineToLogFile("MediaStreamer: mediaStreamer object created.");

                // Try streaming
                MediaStreamingResult result = mediaStreamer.Configure();  // this does actually begin transcoding
                result.LiveStreamingIndexPath = "/httplivestream/" + newStreamerID.ToString() + "/index.m3u8";

                // Add streamer ID to result
                result.StreamerID = newStreamerID;

                // Return
                return result;
            }
            catch (Exception e)
            {
                Functions.WriteLineToLogFile("Exception setting up mediaStreaming object:");
                Functions.WriteExceptionToLogFile(e);
                return new MediaStreamingResult(MediaStreamingResultCodes.NamedError, e.Message);
            }
        }

        /// <summary>
        /// Raised by a streamer after around 10 minutes of inactivity when it auto dies
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void mediaStreamer_AutoDied(object sender, EventArgs e)
        {

            MediaStreamer ms = (MediaStreamer)sender;

            if (Settings.Default.DebugStreaming)
                Functions.WriteLineToLogFile("StreamingManager: Received notification that streamer " + ms.ID.ToString() + " auto-died.");

            RemoveStreamer(ms.ID);

            
        }
        void mediaStreamer_DebugMessage(object sender, FatAttitude.GenericEventArgs<string> e)
        {
            if (Settings.Default.DebugStreaming)
            {
                Functions.WriteLineToLogFile("MediaStreamer: " + e.Value);
            }
        }

        #region Retrieve Segments from Streamer
        public bool SegmentFromStreamer(int streamerID, int segmentNumber, ref byte[] Data, ref string txtError)
        {
            MediaStreamer ms = GetStreamerByID(streamerID);
            if (ms == null)
            {
                txtError = "No such streamer.";
                return false;
            }

            return (ms.GetSegment(segmentNumber, ref Data, ref txtError));
        }
        #endregion

        #region Index File
        public string IndexFileForStreamer(int StreamerID)
        {
            MediaStreamer ms = GetStreamerByID(StreamerID);
            TimeSpan mediaDuration = FileBrowseExporter.DurationOfMediaFile_OSSpecific(ms.Request.InputFile);
            int msSegmentDuration = ms.Request.ActualSegmentDuration;
            
            StringBuilder sbIndexFile = new StringBuilder(1000);

            sbIndexFile.AppendLine("#EXTM3U");
            sbIndexFile.AppendLine("#EXT-X-TARGETDURATION:" + msSegmentDuration.ToString());  // maximum duration of any one file, in seconds
            sbIndexFile.AppendLine("#EXT-X-ALLOW-CACHE:YES"); // allow client to cache files


            double dNumberSegments = mediaDuration.TotalSeconds / Convert.ToDouble(msSegmentDuration);
            int WholeNumberSegments = Convert.ToInt32(Math.Floor(dNumberSegments));
            int i;
            for (i = 0; i < WholeNumberSegments; i++)
            {
                sbIndexFile.AppendLine("#EXTINF:" + msSegmentDuration.ToString() + ",");
                string strSegID = "seg-" + i.ToString() + ".ts";
                sbIndexFile.AppendLine(strSegID);
            }

            // Duration of final segment?
            double dFinalSegTime = mediaDuration.TotalSeconds % Convert.ToDouble(msSegmentDuration);
            int iFinalSegTime = Convert.ToInt32(dFinalSegTime);
            sbIndexFile.AppendLine("#EXTINF:" + iFinalSegTime.ToString() + ",");
            string strFinalSegID = "seg-" + i.ToString() + ".ts";
            sbIndexFile.AppendLine(strFinalSegID);

            sbIndexFile.AppendLine("#EXT-X-ENDLIST");

            return sbIndexFile.ToString();
        }
        
        #endregion

        #region Probing
        public TimeSpan GetMediaDuration(string fileName)
        {
            MediaInfoGrabber grabber = new MediaInfoGrabber(Functions.ToolkitFolder, Path.Combine(Functions.StreamBaseFolder, "probe_results"), fileName);
            grabber.DebugMessage += new EventHandler<FatAttitude.GenericEventArgs<string>>(grabber_DebugMessage);
            grabber.GetInfo();
            grabber.DebugMessage -= grabber_DebugMessage;

            TimeSpan duration =  (grabber.Info.Success) ? grabber.Info.Duration : new TimeSpan(0);

            return duration;
        }

        void grabber_DebugMessage(object sender, FatAttitude.GenericEventArgs<string> e)
        {
            Functions.WriteLineToLogFileIfSetting(Settings.Default.DebugStreaming, e.Value);   
        }
        public List<AVStream> ProbeFile(string fileName)
        {
            FFMPGProber prober = new FFMPGProber();
            
            string strTempDirName = "probe_results";
            string OutputBasePath = Path.Combine(Functions.StreamBaseFolder, strTempDirName);
            prober.DebugMessage += new EventHandler<FatAttitude.GenericEventArgs<string>>(prober_DebugMessage);
            bool result = prober.Probe(Functions.ToolkitFolder, fileName, OutputBasePath);
            prober.DebugMessage -= new EventHandler<FatAttitude.GenericEventArgs<string>>(prober_DebugMessage);
            if (!result)
                return new List<AVStream>();

            return prober.AVAudioAndVideoStreams;
        }
        void prober_DebugMessage(object sender, FatAttitude.GenericEventArgs<string> e)
        {
            Functions.WriteLineToLogFileIfSetting(Settings.Default.DebugStreaming, e.Value);
        }
        #endregion


        #region Singleton Methods
        static StreamingManager instance = null;
        static readonly object padlock = new object();
        public static StreamingManager Default
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new StreamingManager();
                    }
                    return instance;
                }
            }
        }
        #endregion
    }
}


/*

public sealed class Singleton
{
    static Singleton instance=null;
    static readonly object padlock = new object();

    Singleton()
    {
    }

    public static Singleton Instance
    {
        get
        {
            lock (padlock)
            {
                if (instance==null)
                {
                    instance = new Singleton();
                }
                return instance;
            }
        }
    }
}
*/