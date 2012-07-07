using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading;
using FatAttitude.Collections;

namespace FatAttitude.MediaStreamer.HLS
{
    internal partial class SegmentStoreBroker
    {
        public bool SettingsDefaultDebugAdvanced { set; get; }
        MediaStreamingRequest Request;
        SegmentStore store;
        string PathToTools;
        string MapArguments;
        string WorkingDirectory;
        FFHLSRunner Runner;

        const int NUMBER_OF_SEGMENTS_CONSIDERED_COMING_SOON = 5;

        // Constructor
        internal SegmentStoreBroker(string ID, MediaStreamingRequest _request, string pathToTools)
        {
            Request = _request;
            store = new SegmentStore(ID);
            PathToTools = pathToTools;

            string rpPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "RemotePotato");
            WorkingDirectory = Path.Combine(rpPath, "static\\mediastreams\\" + ID.ToString());
            if (!Directory.Exists(WorkingDirectory)) Directory.CreateDirectory(WorkingDirectory);

            // Probe / Map Audio streams:
            if (Request.UseAudioStreamIndex >= 0)
            {
                SendDebugMessage("MediaStreamer: Mapping streams using requested audio stream " + Request.UseAudioStreamIndex.ToString());
                // We still have to probe again to discover the index of the video stream, as both A and V MUST be mapped, can't just map one
                MapArguments = GetProbeMapArguments(Request.UseAudioStreamIndex);
                if (string.IsNullOrEmpty(MapArguments)) // May take a small while
                    throw new Exception("Probing failed");
            }
            /*  QUICKSTART CODE
            // So far, so good.  If the required segment is available then don't bother starting transcoding; it could already be cached to disk.
            // Otherwise, let's start transcoding, as the client is likely to request the segment soon.
            double dStartingSegment = Math.Floor(Convert.ToDouble(Request.StartAt) / Convert.ToDouble(Runner.EncodingParameters.SegmentDuration));
            int iStartingSegment = Convert.ToInt32(dStartingSegment);
            if (! CanGetSegment(iStartingSegment))  // cant get segment
            {
                string txtResult = "";
                if (!Runner.Start(iStartingSegment, ref txtResult))
                    throw new Exception("The FFRunner failed to start : " + txtResult);
            }*/
        }

        bool Start(int segmentNumber, ref string txtResult)
        {
            lock (createNewRunnerLock)
            {
                CreateNewRunner();

                return Runner.Start(segmentNumber, ref txtResult);
            }
        }
        internal void Stop(bool deleteFiles)
        {
            // Stop and destroy the current runner
            if (Runner != null)
                DestroyRunner();

            if (deleteFiles)
                DeleteAllSegmentsFromDisk();
        }

        #region Runner
        object createNewRunnerLock = new object();
        void CreateNewRunner()
        {

            SendDebugMessage("Broker] Cancelling waiting segments");
            store.CancelWaitingSegments();

            if (Runner != null)
                DestroyRunner();

            SendDebugMessage("broker] Creating new runner.");

            Runner = new FFHLSRunner(PathToTools, store, Request.CustomParameters);

            // Set runner variables
            Runner.SettingsDefaultDebugAdvanced = SettingsDefaultDebugAdvanced;
            Runner.MapArgumentsString = MapArguments;
            Runner.WorkingDirectory = WorkingDirectory;
            Runner.InputFile = Request.InputFile;

            // Hook runner events
            Runner.DebugMessage += new EventHandler<GenericEventArgs<string>>(Runner_DebugMessage);
        }
        void DestroyRunner()
        {
            lock (createNewRunnerLock)
            {
                if (Runner == null) return;

                SendDebugMessage("broker] Destroying old runner.");

                Runner.Abort();

                UnwireRunner();
                Runner = null;
            }
        }
        void UnwireRunner()
        {
            if (Runner == null) return;

            Runner.DebugMessage -= new EventHandler<GenericEventArgs<string>>(Runner_DebugMessage);
        }
        #endregion

        // TOP LEVEL
        internal bool GetSegment(int recurseLevel, int SegmentNumber, ref byte[] Data, ref string txtError) // wrapped because of the stackoverflow variable
        {
            //lock (createNewRunnerLock)  // Mustnt try to retrieve a segment while we're restarting the runner
            {

                SendDebugMessage("Segment " + SegmentNumber.ToString() + " requested: trying to get...");
                Segment retrievedSegment = null; SegmentAvailabilities availability = SegmentAvailabilities.IsAvailable;
                if (TryGetSegment(SegmentNumber, ref retrievedSegment, ref availability))
                {
                    Data = retrievedSegment.Data;
                    SendDebugMessage("RETURNING segment " + SegmentNumber.ToString());
                    return true;
                }
                else
                {
                    // What happened
                    if (availability == SegmentAvailabilities.IsError)
                    {
                        SendDebugMessage("Segment " + SegmentNumber.ToString() + " errored.");
                        txtError = "Segment could not be retrieved due to an error.";
                        return false;
                    }
                    else if (availability == SegmentAvailabilities.Cancelled)
                    {
                        SendDebugMessage("Segment " + SegmentNumber.ToString() + " cancelled.");
                        txtError = "Segment was cancelled, possibly due to a seek request.";
                        return false;
                    }
                    else if (availability == SegmentAvailabilities.RequiresSeek)
                    {
                        SendDebugMessage("Segment " + SegmentNumber.ToString() + " requires a seek - (re)starting runner.");

                        // Create a new runner and start it  (cancels any waiting segments)
                        string txtResult = "";
                        if (!Start(SegmentNumber, ref txtResult))
                        {
                            txtError = "Segment could not be retrieved due to the FFRunner failing to start : " + txtResult;
                            return false;
                        }

                        // RUNNER re-started, so let's recurse as the segment availability will now be 'coming soon'
                        // Recurse
                        if ((recurseLevel++) < 4)
                            return GetSegment(recurseLevel, SegmentNumber, ref Data, ref txtError);
                        else
                        {
                            txtError = "Recursion level overflow.";
                            return false;
                        }
                    }

                    // Shouldnt get here
                    return false;
                }
            }
        }
        bool TryGetSegment(int segmentNumber, ref Segment retrievedSegment, ref SegmentAvailabilities segAvailability)
        {
            
            if (store.HasSegment(segmentNumber))
            {
                SendDebugMessage("Broker] Segment " + segmentNumber.ToString() + " is available in store - retrieving");
                bool foo = store.TryGetSegmentByNumber(segmentNumber, ref retrievedSegment); // shouldn't block, as it's in the store
                segAvailability = SegmentAvailabilities.IsAvailable;
                return true;
            }
            

            // Is there a runner
            if ( Runner == null) 
            {
                SendDebugMessage("Broker] require seek (runner stopped)");
                segAvailability = SegmentAvailabilities.RequiresSeek;  // require, in fact!
                return false;
            }

            
            // Store does not have segment.  Is it coming soon?
            int difference = (segmentNumber - Runner.AwaitingSegmentNumber);
            if (difference < 0) // requested segment is in past
            {
                SendDebugMessage("Broker] Seg " + segmentNumber.ToString() + " is in the past - require seek.");
                segAvailability = SegmentAvailabilities.RequiresSeek;
                return false;
            }
            if (difference >= NUMBER_OF_SEGMENTS_CONSIDERED_COMING_SOON)
            {
                SendDebugMessage("Broker] Seg " + segmentNumber.ToString() + " is a huge " + difference + " segs away from arrival - require seek.");
                segAvailability = SegmentAvailabilities.RequiresSeek;
                return false;
            }

            // WAIT FOR A SEGMENT **************************************************************
            SendDebugMessage("Broker] Seg " + segmentNumber.ToString() + " is only " + difference + " away from arrival - requesting from store, which will block...");
            
            bool didGet = (store.TryGetSegmentByNumber(segmentNumber, ref retrievedSegment));
            segAvailability = didGet ? SegmentAvailabilities.IsAvailable : SegmentAvailabilities.Cancelled;
            return didGet;
        }       
        internal bool CanGetSegment(int segmentNumber)
        {
            return (store.HasSegment(segmentNumber));
        }
        internal void DeleteAllSegmentsFromDisk()
        {
            store.DeleteAllStoredSegmentsFromDisk();
        }


#region Helpers
         string GetProbeMapArguments(int preferredAudioStreamIndex)
        {     
            FFMPGProber prober = new FFMPGProber();
            bool result = prober.Probe(PathToTools, Request.InputFile, WorkingDirectory, preferredAudioStreamIndex);

            if (!result)
                return "";

            return prober.mapArguments.ToString();
        }
#endregion

        #region Debug
        void Runner_DebugMessage(object sender, GenericEventArgs<string> e)
        {
            // Pass up
            SendDebugMessage(e.Value);
        }
        public event EventHandler<GenericEventArgs<string>> DebugMessage;
        void SendDebugMessage(string txtDebug)
        {
            if (DebugMessage != null)
                DebugMessage(this, new GenericEventArgs<string>(txtDebug));
        }
        #endregion



    }
}
