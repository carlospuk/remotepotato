using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Web;
using System.Linq;
using System.Timers;
using FatAttitude;
using FatAttitude.Functions;
using RemotePotatoServer.Properties;
using FatAttitude.WTVTranscoder;

namespace RemotePotatoServer
{
    public sealed class DSStreamingManager
    {
        Dictionary<int,DSStreamer> mediaStreamers;
        Timer JanitorTimer;
        const int MAXIMUM_STREAMERS = 8;
        
        DSStreamingManager()
        {
            // Set up streamers
            mediaStreamers = new Dictionary<int, DSStreamer>();

            InitJanitor();
        }
        public void CleanUp()
        {
            Functions.WriteLineToLogFile("StreamingManager: Cleaning Up.");
            StopAllStreamers();
        }
        public void StopAllStreamers()
        {
            List<DSStreamer> streamersToStop = new List<DSStreamer>();
            foreach (DSStreamer ms in mediaStreamers.Values)
            {
                streamersToStop.Add(ms);
            }

            foreach (DSStreamer ms in streamersToStop)
            {
                StopStreamer(ms.ID);
            }
        }

        #region Manage Streamers
        DSStreamer GetStreamerByID(int id)
        {
            if (mediaStreamers.ContainsKey(id))
                return mediaStreamers[id];
            else
                return null;
        }
        int AddNewStreamer(DSStreamer newStreamer)
        {
            int newID ;
            do
            {
                Random r = new Random();
                newID = r.Next(10000,99999);
            }
            while (mediaStreamers.ContainsKey(newID));

            mediaStreamers.Add(newID, newStreamer);

            // Power options
            SetPowerOptions();

            return newID;
        }
        void RemoveStreamer(DSStreamer ms)
        {
            if (ms != null)
            {
                CleanupDSStreamerFiles(ms);
                mediaStreamers.Remove(ms.ID);

                try
                {
                    ms.Dispose();
                    ms = null;
                }
                catch (Exception ex)
                {
                    Functions.WriteLineToLogFile("Couldn't dispose DSStreamer:");
                    Functions.WriteExceptionToLogFile(ex);
                }

            }

            // Power options
            SetPowerOptions();
        }
        void CleanupDSStreamerFiles(DSStreamer ms)
        {
            if (string.IsNullOrEmpty(ms.StreamingRequest.FileName)) return;

            // Cleanup file
            string fileName = ms.StreamingRequest.FileName + ".wmv";
            if (File.Exists(fileName))
            {
                if (Settings.Default.DebugAdvanced)
                    Functions.WriteLineToLogFile("Cleaning up old streaming file " + fileName);
                try
                {
                    File.Delete(fileName);
                }
                catch { }
            }

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
                Functions.WriteLineToLogFile("DSMediaStream Janitor:  Checking for old streamers.");

            List<int> deletions = new List<int>();

            foreach (DSStreamer ms in mediaStreamers.Values)
            {
                TimeSpan ts = DateTime.Now.Subtract(ms.CreationDate);

                if (ts.TotalHours > 5) // more than five hours old
                {
                    Functions.WriteLineToLogFile("DSMediaStream Janitor:  Sweeping up streamer " + ms.ID.ToString() + " which is " + ts.TotalHours.ToString() + " old.");
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


        
        public WTVStreamingVideoResult StartStreamer(WTVStreamingVideoRequest strq)
        {
            int newStreamerID = -1;

            // Too many streamers?
            if (mediaStreamers.Count > MAXIMUM_STREAMERS)
            {
                Functions.WriteLineToLogFile("DSStreamingManager: too many streamers (" + mediaStreamers.Count.ToString() + " streamers are running, which is above the maximum of " + MAXIMUM_STREAMERS.ToString() + ")" );
                return new WTVStreamingVideoResult(DSStreamResultCodes.ErrorTooManyStreamers);
            }

            // For now, some overrides and assumptions
            if (!File.Exists(strq.FileName))
            {
                Functions.WriteLineToLogFile("WebSvc Start Streaming FAIL: File not found: " + strq.FileName);
                return new WTVStreamingVideoResult(DSStreamResultCodes.ErrorFileNotFound);
            }

            try
            {
                DSStreamer mediaStreamer = new DSStreamer();
                mediaStreamer = new FatAttitude.WTVTranscoder.DSStreamer();
                mediaStreamer.Finished += new EventHandler<DSTranscoderBase.ConversionEndedEventArgs>(mediaStreamer_Finished);
                mediaStreamer.ConversionCompleted += new EventHandler(mediaStreamer_ConversionCompleted);
                mediaStreamer.DebugMessageGenerated += new EventHandler<DSTranscoderBase.DebugMessageEventArgs>(mediaStreamer_DebugMessageGenerated);
                Functions.WriteLineToLogFile("DSStreamingManager: DSStreamer object created.");

                // Which port should we use?
                int portToTry = GetNextFreePort();

                // Try streaming  (Async)
                Functions.WriteLineToLogFileIfSetting(Settings.Default.DebugStreaming, "DSStreamingManager: Attempting to stream using port " + portToTry.ToString());
                WTVStreamingVideoResult streamResult = mediaStreamer.StreamWithFileAndPort(strq, portToTry, false, true);

                if (streamResult.ResultCode == DSStreamResultCodes.OK)
                {
                    // Add to local streamers
                    newStreamerID = AddNewStreamer(mediaStreamer);
                    mediaStreamer.ID = newStreamerID;

                    // Add streamer ID to result code too
                    streamResult.StreamerID = newStreamerID.ToString();
                }
                // Return
                return streamResult;
            }
            catch (Exception e)
            {
                Functions.WriteLineToLogFile("Exception setting up mediaStreaming object:");
                Functions.WriteExceptionToLogFile(e);
                return new WTVStreamingVideoResult(DSStreamResultCodes.ErrorExceptionOccurred, "Error setting up mediastreaming object: " + e.Message + " (see server log for more details)");
            }
        }
        int lastStreamedPort = 0;
        int GetNextFreePort()
        {
            int minimumPort = Convert.ToInt32(Settings.Default.SilverlightStreamingPort);
            int maximumPort = minimumPort + (Settings.Default.SilverlightStreamingNumberOfPorts) - 1;

            int usePortNumber = 0;

            if (
                (mediaStreamers.Count < 1) ||   // backward-compatibility: always try 9081 first
                (lastStreamedPort == 0)
                )
                usePortNumber = minimumPort;  
            else
                usePortNumber = ++lastStreamedPort;

            // Wrap around
            if (usePortNumber > maximumPort)
                usePortNumber = minimumPort;

            // Record for next time
            lastStreamedPort = usePortNumber;
            return usePortNumber;
        }


        /// <summary>
        /// Stop a streamer and remove it from the local list of streamers
        /// </summary>
        /// <param name="streamerID"></param>
        /// <returns></returns>
        public bool StopStreamer(int streamerID)
        {
            if (Settings.Default.DebugStreaming)
                Functions.WriteLineToLogFile("DSStreamingManager: Received stop command for streamer " + streamerID.ToString());

            DSStreamer mediaStreamer = GetStreamerByID(streamerID);
            if (mediaStreamer == null) return false;

            mediaStreamer.Cancel();

            // Remove from streamers
            RemoveStreamer(mediaStreamer);

            return true;
        }

        #region DSStreamer Events
        void mediaStreamer_DebugMessageGenerated(object sender, DSTranscoderBase.DebugMessageEventArgs e)
        {
            if (! Settings.Default.DebugBasic) return;
            if ((e.Severity < 10) && (!Settings.Default.DebugStreaming)) return;  // ignore non-severe unless advanced debug is on

            Functions.WriteLineToLogFile("Video Streamer: " + e.DebugMessage);
            if (e.HasException)
                Functions.WriteExceptionToLogFile(e.InnerException);
        }
        void mediaStreamer_ConversionCompleted(object sender, EventArgs e)
        {
            // do nothing
        }
        void mediaStreamer_Finished(object sender, DSTranscoderBase.ConversionEndedEventArgs e)
        {
            if (e.WasError)
                Functions.WriteLineToLogFile("DSStreamingManager: DSStreamer finished, error occured: " + e.Message);
            else
                Functions.WriteLineToLogFile("DSStreamingManager: DSStreamer finished: " + e.Message);

            if (sender == null) return;
            if (!(sender is DSStreamer)) return;

            DSStreamer streamer = (DSStreamer)sender;
            RemoveStreamer(streamer); // CLears up any files, resumes power standby and removes from local array
        }
        #endregion


        void mediaStreamer_DebugMessage(object sender, FatAttitude.GenericEventArgs<string> e)
        {
            if (Settings.Default.DebugStreaming)
            {
                Functions.WriteLineToLogFile("MediaStreamer: " + e.Value);
            }
        }


        #region Singleton Methods
        static DSStreamingManager instance = null;
        static readonly object padlock = new object();
        public static DSStreamingManager Default
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new DSStreamingManager();
                    }
                    return instance;
                }
            }
        }
        #endregion
    }
}

