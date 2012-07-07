using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Timers;
using System.Threading;
using FatAttitude;
using FatAttitude.MediaStreamer.HLS;
using FatAttitude.Collections;
using System.Collections.Concurrent;

namespace FatAttitude.MediaStreamer
{
    public class MediaStreamer
    {

        // Public
        public int ID { get; set; }
        public DateTime CreationDate { get; set; }
        public string AdditionalStatusInfo { get; set; }
        bool SettingsDefaultDebugAdvanced;

        // Private
        public MediaStreamingRequest Request;
        SegmentStoreBroker broker;
        // Keepalive
        System.Timers.Timer lifeTimer;

        /// <summary>
        /// Note that everything should be passed into the constructor that depends upon encoding, since settings public
        /// fields afterwards would result in them not being passed to lower objects that are set up in the constructor.
        /// If the constructor grows really bloated, suggest a separate constructor and Configure() method.
        /// </summary>
        /// <param name="_ID"></param>
        /// <param name="request"></param>
        /// <param name="pathToTools"></param>
        /// <param name="timeToKeepAlive"></param>
        /// <param name="debugAdvanced"></param>
        public MediaStreamer(int _ID, MediaStreamingRequest request, string pathToTools, int timeToKeepAlive, bool debugAdvanced)
        {
            // Store variables
            ID = _ID;
            Request = request;
            SettingsDefaultDebugAdvanced = debugAdvanced;

            // Set up life timer
            lifeTimer = new System.Timers.Timer(1000);
            lifeTimer.AutoReset = true;
            lifeTimer.Elapsed += new ElapsedEventHandler(lifeTimer_Elapsed);
            lifeTimer.Start();

            // Creation date
            CreationDate = DateTime.Now;

            // Status
            AdditionalStatusInfo = "";

            // Create broker - and hook it to runner events
            broker = new SegmentStoreBroker(this.ID.ToString(), request, pathToTools);
            broker.SettingsDefaultDebugAdvanced = this.SettingsDefaultDebugAdvanced;
            broker.DebugMessage += new EventHandler<GenericEventArgs<string>>(broker_DebugMessage);
        }
        void lifeTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // Any regularly called methods go here, e.g. auto stop streaming, prune segments, etc.
            AutoPause_Tick();
        }
        

        #region Top level Public
        bool everStartedRunner = false;
        bool IsConfigured;
        /// <summary>
        /// Set up the streamer and (current implementation) begin transcoding, to get ahead of the game for future segment requests
        /// </summary>
        /// <returns></returns>
        public MediaStreamingResult Configure()
        {
            // File exists?
            if (!File.Exists(Request.InputFile))
            {
                MediaStreamingResult badResult = new MediaStreamingResult(MediaStreamingResultCodes.FileNotFound, "File not found: " + Request.InputFile);
                return badResult;
            }

            // OK let's try
            SendDebugMessage("MediaStreamer: Configuring streaming.");

            // Used in auto-die
            lastContactAtTime = DateTime.Now;
            everStartedRunner = true;  // used in auto-die

            // We did it
            IsConfigured = true;

            // Return positive result
            MediaStreamingResult result = new MediaStreamingResult();
            result.FrameWidth = Request.CustomParameters.FrameWidth;
            result.FrameHeight = Request.CustomParameters.FrameHeight;
            result.ResultCode = MediaStreamingResultCodes.OK;
            result.Completed = true;
            result.Success = true;
            return result;
        }

        
        
        bool isAborted;
        public void AbortStreaming(bool removeFilesFromDisk)
        {
            if (isAborted) return;

            try
            {
                SendDebugMessage("MediaStreamer: Stopping streaming (and killing child process).");

                // Kill the broker
                broker.Stop(removeFilesFromDisk);

                // Stop timing our life
                if (lifeTimer != null) lifeTimer.Stop();
                lifeTimer = null;
            }
            catch (Exception ex)
            {
                SendDebugMessage("Couldn't abort cleanly: " + ex.Message);
            }

            isAborted = true;
        }
        // KeepAlive is in a region below
        #endregion


        #region Segment Requests
        public bool GetSegment(int SegmentNumber, ref byte[] Data, ref string txtError)
        {
            if (!IsConfigured)
            {
                txtError = "Not configured.";
                return false;
            }

            // Used in Auto stop
            lastContactAtTime = DateTime.Now;

            return broker.GetSegment(0, SegmentNumber, ref Data, ref txtError);
        }

        #endregion


        #region Auto Stop
        DateTime lastContactAtTime;
        const int SECONDS_BEFORE_AUTO_PAUSE = 35;
        const int SECONDS_BEFORE_AUTO_DIE = 6000; // 10 minutes
        bool isPaused;
        public event EventHandler AutoDied;
        void AutoPause_Tick()
        {
            if (! everStartedRunner) return;  // Don't die before we've even started!

            if (!isAborted)
            {
                TimeSpan timeSinceLastContact = DateTime.Now.Subtract(lastContactAtTime);
                if (!isPaused)
                {
                    if (timeSinceLastContact.TotalSeconds > SECONDS_BEFORE_AUTO_PAUSE)
                    {
                        SendDebugMessage(timeSinceLastContact.TotalSeconds.ToString() + "sec since last segment request - auto pausing streamer.");

                        isPaused = true;
                        broker.Stop(false);
                    }
                }
                else
                {
                    // We're paused... ...should we die?
                    if (timeSinceLastContact.TotalSeconds > SECONDS_BEFORE_AUTO_DIE)
                    {
                        AbortStreaming(true);

                        if (AutoDied != null) AutoDied(this, new EventArgs());
                    }
                }
            }
        }
        #endregion


        #region Incoming Events
        void broker_DebugMessage(object sender, GenericEventArgs<string> e)
        {
            SendDebugMessage(e.Value);
        }
        #endregion


        #region Status / Debug
        public bool IsLiveStreamAvailable
        {
            get
            {
                // This counts as contact from the server
                lastContactAtTime = DateTime.Now;

                // LEGACY SUPPORT:  Live stream is always available now;
                return true;
            }
        }

        //Debug
        public event EventHandler<GenericEventArgs<string>> DebugMessage;
        void SendDebugMessage(string txtDebug)
        {
            // Add our ID
            txtDebug = "[" + this.ID.ToString() + "]" + txtDebug;

            if (DebugMessage != null)
                DebugMessage(this, new GenericEventArgs<string>(txtDebug));

            System.Diagnostics.Debug.Print(txtDebug);
        }
        #endregion
    }



}
