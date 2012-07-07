using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using DirectShowLib;
using DirectShowLib.SBE;
using WindowsMediaLib;
using System.Threading;

namespace FatAttitude.WTVTranscoder
{
    /// <summary>
    /// A class to transcode a .WTV or .DVR-MS file and write it to a file
    /// </summary>
    public class DSFileWriter : DSTranscoderBase
    {
        private string FileName;
        private WTVProfileQuality Quality;

        private const int WMGraphNotify = 0x0400 + 13;
        private const int VolumeFull = 0;
        private const int VolumeSilence = -10000;

        public DSFileWriter() : base()
        {
            FileName = "";
        }

        public DSStreamResultCodes TranscodeFileAsync(string fileName, WTVProfileQuality quality)
        {
            FileName = fileName;
            Quality = quality;

            WTVStreamingVideoRequest strq = new WTVStreamingVideoRequest(FileName, quality, TimeSpan.FromSeconds(0));

            DSStreamResultCodes result = InitWithFile(strq);
            if (result != DSStreamResultCodes.OK)
                return result;  // ...and stop

            Thread th = new Thread(new ThreadStart(DoTranscodeFileAsync));
            th.Name = "TranscodeFile1";
            th.Start();
            return DSStreamResultCodes.OK;
        }
        private void DoTranscodeFileAsync()
        {
            // Run the graph to completion
            IGraphBuilder graph = (IGraphBuilder)currentFilterGraph;
            RunGraph(graph, (IBaseFilter)currentOutputFilter);
        }
        public void Cancel()
        {
            if (CancellationPending) return;

            _cancellationPending = true;
        }


        // Events
        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;
        public event EventHandler Completed;

        /// <summary>Gets whether this is currently a cancellation request pending.</summary>
        protected bool CancellationPending { get { return _cancellationPending; } }
        /// <summary>Whether this is currently a cancellation request pending.</summary>
        private volatile bool _cancellationPending = false;

        /// <summary>Runs the graph</summary>
        /// <param name="graphBuilder">The graph to be run.</param>
        /// <param name="seekableFilter">The filter to use for computing percent complete. Must implement IMediaSeeking.</param>
        protected void RunGraph(IGraphBuilder graphBuilder, IBaseFilter seekableFilter)
        {
            // Get the necessary control and event interfaces
            IMediaControl mediaControl = (IMediaControl)graphBuilder;
            IMediaEvent mediaEvent = (IMediaEvent)graphBuilder;

            // Get the media seeking interface to use for computing status and progress updates
            IMediaSeeking mediaSeeking = seekableFilter as IMediaSeeking;
            if (!CanGetPositionAndDuration(mediaSeeking))
            {
                mediaSeeking = graphBuilder as IMediaSeeking;
                if (!CanGetPositionAndDuration(mediaSeeking)) mediaSeeking = null;
            }

            // Publish the graph to the running object table and to a temporary file for examination/debugging purposes
            //using (new GraphPublisher(graphBuilder, "C:\\vidtests\\grf\\" + Guid.NewGuid().ToString("N") + ".grf"))
            {
                // Run the graph
                int hr = 0;
                hr = mediaControl.Pause();
                hr = mediaControl.Run();
                DsError.ThrowExceptionForHR(hr);

                try
                {
                    ProgressChanged(new object(), new ProgressChangedEventArgs(0.0)); // initial progress update stating 0% done
                    bool done = false;
                    while (!CancellationPending && !done) // continue until we're done/cancelled
                    {
                        // Poll to see how we're doing
                        EventCode statusCode;

                        hr = mediaEvent.WaitForCompletion(200, out statusCode);
                        Console.Write(" <" + statusCode.ToString() + ">,");
                        switch (statusCode)
                        {
                            case EventCode.Complete:
                                done = true;
                                break;
                            case 0:
                                // Get an update on where we are with the conversion
                                if (mediaSeeking != null)
                                {
                                    long curPos;
                                    mediaSeeking.GetCurrentPosition(out curPos);
                                    long length;
                                    mediaSeeking.GetDuration(out length);
                                    double progress = curPos * 100.0 / (double)length;
                                    if (progress > 0) ProgressChanged(new object(), new ProgressChangedEventArgs(progress));
                                }
                                break;
                            default:
                                // Error, so throw exception
                                EventCode tryCode;
                                IntPtr lp1, lp2;
                                hr = mediaEvent.GetEvent(out tryCode, out lp1, out lp2, 200);
                                DsError.ThrowExceptionForHR(hr);
                                throw new Exception(statusCode.ToString());
                        }
                    }
                    ProgressChanged(new object(), new ProgressChangedEventArgs(100)); // final progress update stating 100% done
                }
                finally
                {
                    // We're done converting, so stop the graph
                    FilterState graphState;
                    mediaControl.GetState(100, out graphState);
                    if (graphState == FilterState.Running)
                        mediaControl.Pause();
                    mediaControl.Stop();


                    // Return done
                    Completed(new object(), new EventArgs());
                }
            }
        }
        /// <summary>Determines whether the specified IMediaSeeking can be used to retrieve duration and current position.</summary>
        /// <param name="seeking">The interface to check.</param>
        /// <returns>true if it can be used to retrieve duration and current position; false, otherwise.</returns>
        private static bool CanGetPositionAndDuration(IMediaSeeking seeking)
        {
            if (seeking == null) return false;

            AMSeekingSeekingCapabilities caps;
            int hr = 0;
            hr = seeking.GetCapabilities(out caps);
            DsError.ThrowExceptionForHR(hr);
            if ((caps & AMSeekingSeekingCapabilities.CanGetDuration) != AMSeekingSeekingCapabilities.CanGetDuration) return false;
            if ((caps & AMSeekingSeekingCapabilities.CanGetCurrentPos) != AMSeekingSeekingCapabilities.CanGetCurrentPos) return false;
            return true;
        }


    }
}
