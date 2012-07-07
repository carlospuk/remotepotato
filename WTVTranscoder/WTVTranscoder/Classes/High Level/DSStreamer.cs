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
using System.Runtime.ExceptionServices;

namespace FatAttitude.WTVTranscoder
{

    /// <summary>
    /// A class to transcode a SBE source file (using the base class) and then stream it out over the network
    /// </summary>
    public class DSStreamer : DSTranscoderBase
    {
        // Members
        int StreamPort;
        bool RemoveReferenceClock;
        public WTVStreamingVideoRequest StreamingRequest;
        public Thread thStreamThread;  // future use: could abort this
        
        IWMWriterNetworkSink NetworkSink;
        Queue<StreamCommand> PendingCommands;  // To allow remote interaction with the graph while it is running

        /// <summary>Whether there is currently a cancellation request pending.</summary>
        private volatile bool _cancellationPending = false;

        // Constants
        const int TIMEOUT_SECONDS = 30;

        // Constructor
        public DSStreamer() : base()
        {
            StreamingRequest = null;
            StreamPort = 9081;
            RemoveReferenceClock = false;
            PendingCommands = new Queue<StreamCommand>();
        }

        #region Public events and methods
        // Events
        public event EventHandler<ProgressChangedEventArgs> ConversionProgressChanged;
        public event EventHandler ConversionCompleted;
        public event EventHandler<ConversionEndedEventArgs> Finished;

        // Main public method - Begin Streaming
        public WTVStreamingVideoResult StreamWithFileAndPort(WTVStreamingVideoRequest svrq, int streamPort, bool removeReferenceClock, bool autodetectPal)
        {
            WTVStreamingVideoResult result = new WTVStreamingVideoResult();
            // Attempting this port
            result.Port = streamPort.ToString();
            
            // Store values locally for use by other class methods
            StreamingRequest = svrq;
            StreamPort = streamPort;
            RemoveReferenceClock = removeReferenceClock;

            // Build the graph using the base class
            DSStreamResultCodes resultCode = base.InitWithFile( StreamingRequest);
            if (resultCode != DSStreamResultCodes.OK)
                return new WTVStreamingVideoResult(resultCode);

            // Add streaming
            try
            {
                AddStreamSinkToCurrentOutputFilter(StreamPort);
            }
            catch (Exception ex)
            {
                result.ResultCode = DSStreamResultCodes.ErrorAlreadyStreaming;
                result.ResultString = ex.Message;
                return result;
            }

            // Remove clock?
            try
            {
                if (RemoveReferenceClock)
                    RemoveRefClockFromGraph();
            }
            catch (Exception ex)
            {
                result.ResultCode = DSStreamResultCodes.Error;
                result.ResultString = "Error removing reference clock: " + ex.Message;
                return result;
            }

            // Seek?
            try
            {
                if (StreamingRequest.StartAt.TotalSeconds > 0)
                    SeekGraphToTime(StreamingRequest.StartAt);
                else
                    SeekGraphToTime(TimeSpan.FromSeconds(5)); // EXPERIMENTAL
            }
            catch
            {
                // Ignore non-seeking errors for now
            }


            // Begin streaming the graph in a separate thread
            graphStartedEvent.Reset();
            thStreamThread = new Thread(new ThreadStart(DoStreamWithFileAndPort));
            thStreamThread.SetApartmentState(ApartmentState.MTA);  // NetworkSink fails in STA threads - avoid!
            thStreamThread.Name = "StreamFile1";
            thStreamThread.Start();

            // Wait for graph to attempt to start so we can report back
            bool gotSignal = ( graphStartedEvent.WaitOne(6000));

            if (gotSignal)
            {
                if (graphIsRunning)
                {
                    result.ResultCode = DSStreamResultCodes.OK;
                    return result;
                }
                else
                {
                    result.ResultCode = DSStreamResultCodes.Error;
                    result.ResultString = "DSStreamer: Graph could not run - see server log for more information.";
                    return result;
                }
            }
            else
            {
                result.ResultCode = DSStreamResultCodes.Error;
                result.ResultString = "DSStreamer: Timed out waiting for graph to run - there may be more information in the server log.";
                return result;
            }
        }
        private void DoStreamWithFileAndPort()
        {
            try
            {
                // Run graph
                RunGraph((IGraphBuilder)currentFilterGraph, currentOutputFilter);
            }
            catch (Exception ex)
            {
                if (Finished != null) Finished(new object(), new ConversionEndedEventArgs(true, ex.Message));
            }
        }
        
        // Methods to cancel or seek the graph
        public void Cancel()
        {
            if (_cancellationPending) return;

            _cancellationPending = true;
            PendingCommands.Enqueue(new StreamCommand(StreamCommand.CommandNames.Cancel) );
        }
        public void Seek(TimeSpan span)
        {
            PendingCommands.Enqueue(new StreamCommand(StreamCommand.CommandNames.Seek,  span ));
        }
        public void SetRate(double newRate)
        {
            object ORate = (object)newRate;
            PendingCommands.Enqueue(new StreamCommand(StreamCommand.CommandNames.SetRate, ORate));
        }
        #endregion


        object graphIsRunningLock = new object();
        bool graphIsRunning;
        ManualResetEvent graphStartedEvent = new ManualResetEvent(false);
        /// <summary>Runs the graph to begin streaming the file - includes methods to check for timeout, client disconnection, etc.
        /// Also processes any commands
        /// </summary>
        /// <param name="graphBuilder">The graph to be run.</param>
        /// <param name="seekableFilter">The filter to use for computing percent complete. Must implement IMediaSeeking.</param>
        [HandleProcessCorruptedStateExceptions] // Some filters cause AccessViolations; NET 4 doesn't catch these without this flag see http://msdn.microsoft.com/en-us/magazine/dd419661.aspx#id0070035
        protected void RunGraph(IGraphBuilder graphBuilder, IBaseFilter seekableFilter)
        {
            bool shouldTerminateGraphLoop = false;

            // Get the media seeking interface to use for computing status and progress updates
            IMediaSeeking mediaSeeking = (IMediaSeeking)currentOutputFilter;
            if (!CanGetPositionAndDuration(mediaSeeking))
            {
                // Try to seek using the main graph
                mediaSeeking = (IMediaSeeking)currentFilterGraph;
                if (!CanGetPositionAndDuration(mediaSeeking)) 
                        mediaSeeking = null;
            }
            
            // Run the graph
            int hr = 0;
            IMediaControl mediaControl = (IMediaControl)graphBuilder;
            IMediaEvent mediaEvent = (IMediaEvent)graphBuilder;
            EventCode statusCode;
            DateTime startingTime = DateTime.Now;
            TerminationReason whyDidITerminate = TerminationReason.None;

            try
            {
                hr = mediaControl.Pause();
                hr = mediaControl.Run();
                DsError.ThrowExceptionForHR(hr);

                // Signal (if first time) that graph is running OK
                SignalGraphStartedEvent(true);

                bool anyClientsYet = false;
                bool conversionComplete = false;
                while (!shouldTerminateGraphLoop) // continue until we're done/cancelled
                {
                    // Any commands?  (e.g. seek / cancel)
                    ProcessAnyCommands(ref shouldTerminateGraphLoop, ref whyDidITerminate);

                    // Check graph conversion progress
                    if (conversionComplete)
                    {
                        // stall to avoid 100% loop
                        hr = mediaEvent.WaitForCompletion(250, out statusCode);
                    }
                    else
                    {
                        conversionComplete = CheckGraphConversion(ref mediaSeeking);

                        if (conversionComplete)
                        {
                            if (ConversionProgressChanged != null)
                                ConversionProgressChanged(new object(), new ProgressChangedEventArgs(100)); // final progress update stating 100% done

                            if (ConversionCompleted != null)
                                ConversionCompleted(new object(), new EventArgs());
                        }
                    }

                    // Check number of clients 
                    int numClients = NumberOfConnectedClients();
                    if ((numClients > 0) && (!anyClientsYet))
                    {
                        anyClientsYet = true;  // A client connected
                    }
                    else if ((numClients == 0))
                    {
                        if (anyClientsYet)
                        {
                            // There were clients, but All clients have disconnected
                            shouldTerminateGraphLoop = true;
                            whyDidITerminate = TerminationReason.AllClientsDisconnected;
                        }
                        else
                        {
                            // There aren't any clients and never have been - timeout?
                            TimeSpan timeElapsed = DateTime.Now - startingTime;
                            if (timeElapsed.TotalSeconds > TIMEOUT_SECONDS)
                            {
                                shouldTerminateGraphLoop = true;
                                whyDidITerminate = TerminationReason.NoClientsTimeout;
                            }
                        }
                    }

                }

            }
            catch (Exception ex)
            {
                SendDebugMessageWithException("Error running graph: ", ex);
                whyDidITerminate = TerminationReason.Error;

                SignalGraphStartedEvent(false);
            }
            finally
            {
                try
                {
                    // Raise 'done' event first, before any possible AccessExceptions
                    switch (whyDidITerminate)
                    {
                        case TerminationReason.AllClientsDisconnected:
                            if (Finished != null) Finished(this, new ConversionEndedEventArgs(false, "All clients disconnected."));
                            break;

                        case TerminationReason.NoClientsTimeout:
                            if (Finished != null) Finished(this, new ConversionEndedEventArgs(false, "No clients ever connected."));
                            break;

                        case TerminationReason.UserCancelled:
                            if (Finished != null) Finished(this, new ConversionEndedEventArgs(false, "User cancelled."));
                            break;

                        case TerminationReason.Error:
                            if (Finished != null) Finished(this, new ConversionEndedEventArgs(true, "Unspecified error."));
                            break;

                        default:
                            if (Finished != null) Finished(this, new ConversionEndedEventArgs(false, "Finished but no additional info."));
                            break;
                    }




                    // Stop graph
                    FilterState graphState;
                    IMediaControl mediaControl2 = (IMediaControl)graphBuilder;
                    mediaControl2.GetState(50, out graphState);
                    if (graphState == FilterState.Running)
                    {

                        mediaControl2.Pause();
                        mediaControl2.Stop();  // Throwing AccessViolationException: attempted to read or write protected memory  (probably a badly written filter somewhere)
                    }
                }
                catch  (AccessViolationException )
                {
                    SendDebugMessage("Ignoring expected AViolationException", 0);
                }  
                catch  (Exception ex)
                {
                    SendDebugMessageWithException("Ignoring exception when closing graph: ", ex);
                }  

                // Close sink - can take a loooong time 
                CloseNetworkSink();

                /*//so do in a separate thread...
                Thread t = new Thread(CloseNetworkSink);
                t.Start();*/
            }


            
        }
        void SignalGraphStartedEvent(bool graphRunOK)
        {
            Monitor.Enter(graphIsRunningLock);

            if (graphRunOK)
                if (!graphIsRunning)
                    graphIsRunning = true;
            
            // Allow sync calling thread to proceed
            graphStartedEvent.Set();
            
            Monitor.Exit(graphIsRunningLock);
        }
        void CloseNetworkSink()
        {
            NetworkSink.Close();
        }
        private bool CheckGraphConversion(ref IMediaSeeking mediaSeeking)
        {
            int hr;
            IMediaEvent mediaEvent = (IMediaEvent)currentFilterGraph;

            // Check the graph / conversion is going ok, and raise any progress events
            EventCode statusCode;
            hr = mediaEvent.WaitForCompletion(100, out statusCode);
            switch (statusCode)
            {
                case EventCode.Complete:
                    return true;
                case 0:  // Still going - fire event with an update on where we are 
                    if (mediaSeeking != null)
                    {
                        long curPos;
                        mediaSeeking.GetCurrentPosition(out curPos);
                        long length;
                        mediaSeeking.GetDuration(out length);
                        double progress = curPos * 100.0 / (double)length;
                        if (ConversionProgressChanged != null)
                            ConversionProgressChanged(new object(), new ProgressChangedEventArgs(progress));
                    }
                    return false;
                default:  // Error
                    EventCode tryCode;
                    IntPtr lp1, lp2;
                    hr = mediaEvent.GetEvent(out tryCode, out lp1, out lp2, 200);
                    DsError.ThrowExceptionForHR(hr);
                    throw new Exception(statusCode.ToString());
            }
        }
        private void ProcessAnyCommands(ref bool shouldTerminateGraphConversionLoop, ref TerminationReason whyDidYouTerminate)
        {
            if (PendingCommands.Count < 1) return;

            StreamCommand CurrentCommand = PendingCommands.Dequeue();

            switch (CurrentCommand.CommandName)
            {
                case StreamCommand.CommandNames.Cancel:
                    shouldTerminateGraphConversionLoop = true;
                    whyDidYouTerminate = TerminationReason.UserCancelled;
                    break;

                case StreamCommand.CommandNames.Seek: // NON-FUNCTIONAL
                    TimeSpan seekTime = (TimeSpan)CurrentCommand.ParamObject;
                    DoSeekToTime(seekTime);
                    break;

                case StreamCommand.CommandNames.SetRate:
                    double newRate = (double)CurrentCommand.ParamObject;
                    DoSetRate(newRate);
                    break;

                default:
                    break;
            }

            // Continue and run the graph for another iteration before performing any further pending commands.
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
        /// <summary>
        /// Adds a IWMWriterNetworkSink to the current AsfWriter output filter
        /// </summary>
        /// <param name="streamPort">The port out of which to stream the transcoded file</param>
        private void AddStreamSinkToCurrentOutputFilter(int streamPort)
        {
            int hr;
            DirectShowLib.IServiceProvider pServiceProvider;  // http://msdn.microsoft.com/en-us/library/dd390985%28VS.85%29.aspx  
            WMAsfWriter asfwriter = (WMAsfWriter)currentOutputFilter;
            pServiceProvider = (DirectShowLib.IServiceProvider)asfwriter;

            // Get access to WMwriterAdvanced2 object using pServiceProvider  (poss not futureproof)  (see http://groups.google.com/group/microsoft.public.win32.programmer.directx.video/browse_thread/thread/36b154d41cb76ffd/c571d6ef56de11af?#c571d6ef56de11af )
            DsGuid dsgWMwriterAdvanced2 = DsGuid.FromGuid(new Guid(GUIDs.IWMWriterAdvanced2));
            IWMWriterAdvanced2 WMWriterAdvanced2 = null;            
            object o = null;
            hr = pServiceProvider.QueryService(dsgWMwriterAdvanced2, dsgWMwriterAdvanced2, out o);  // FAILS IN A STA THREAD
            DsError.ThrowExceptionForHR(hr);  
            WMWriterAdvanced2 = (IWMWriterAdvanced2)o;
            
            IWMWriterNetworkSink nsink;
            WMUtils.WMCreateWriterNetworkSink(out nsink);
            NetworkSink = nsink;
            dc.Add(nsink);
            nsink.SetMaximumClients(1);
            nsink.SetNetworkProtocol(NetProtocol.HTTP); 
            
            NetworkSink.Open(ref streamPort);  // Will throw exception if port is in use

          

            int nSinks;
            WMWriterAdvanced2.GetSinkCount(out nSinks);
            if (nSinks > 0)
            {
                IWMWriterSink pSink = null;
                WMWriterAdvanced2.GetSink(0, out pSink);
                if (pSink != null)
                    WMWriterAdvanced2.RemoveSink(pSink);
                Marshal.ReleaseComObject(pSink);  pSink = null;
            }
            WMWriterAdvanced2.AddSink(NetworkSink);
        }


        private void RemoveRefClockFromGraph()
        {
            IMediaFilter mf = (IMediaFilter)currentFilterGraph;
            int hr = mf.SetSyncSource(null);
            DsError.ThrowExceptionForHR(hr);
        }
        private int NumberOfConnectedClients()
        {
            IWMClientConnections ClientConns = (IWMClientConnections)NetworkSink;
            int numClients;
            ClientConns.GetClientCount(out numClients);
            ClientConns = null;  // release ?
            return numClients;
        }

        #region Commands
        // Seeking - Experimental, non-functional
        private void DoSeekToTime(TimeSpan seekTime)
        {
            int hr;
            IMediaControl mc = (IMediaControl)currentFilterGraph;
            // Stop
            hr = mc.Stop();
            DsError.ThrowExceptionForHR(hr);
            // Stop ASFWriter
            hr = currentOutputFilter.Stop();
            DsError.ThrowExceptionForHR(hr);
            // Seek
            Int64 seekTimeNanoSeconds = Convert.ToInt64( seekTime.TotalSeconds * 10000000 );
            DsLong dsTime = DsLong.FromInt64(seekTimeNanoSeconds);
            
            if (UsingSBEFilter)
            {
                IStreamBufferMediaSeeking mSeek = (IStreamBufferMediaSeeking)currentSBEfilter;  // StreamBufferMediaSeeking is used on the Source Filter, NOT the graph - see MSDN
                hr = mSeek.SetPositions(dsTime, AMSeekingSeekingFlags.AbsolutePositioning, 0, AMSeekingSeekingFlags.NoPositioning);
                DsError.ThrowExceptionForHR(hr);
            }
            else
            {
                // IMediaSeeking is used on the filter graph which distributes the calls
                IMediaSeeking mSeek = (IMediaSeeking)currentFilterGraph;
                hr = mSeek.SetPositions(dsTime, AMSeekingSeekingFlags.AbsolutePositioning, 0, AMSeekingSeekingFlags.NoPositioning);
                DsError.ThrowExceptionForHR(hr);                
            }


            // Start ASF
            hr = currentOutputFilter.Run(0);
            DsError.ThrowExceptionForHR(hr);
            // Run again
            hr = mc.Run();
            DsError.ThrowExceptionForHR(hr);
            

        }
        private void DoSetRate(double newRate)
        {
            int hr;
            IMediaControl mc = (IMediaControl)currentFilterGraph;
            // Stop
            hr = mc.Stop();
            DsError.ThrowExceptionForHR(hr);
            // Stop ASFWriter
            hr = currentOutputFilter.Stop();
            DsError.ThrowExceptionForHR(hr);

            if (UsingSBEFilter)
            {
                IStreamBufferMediaSeeking mSeek = (IStreamBufferMediaSeeking)currentSBEfilter;
                DsLong lDouble = DsLong.FromInt64(Convert.ToInt64(newRate));
                hr = mSeek.SetRate(lDouble);
                DsError.ThrowExceptionForHR(hr);
            }
            else
            {
                // IMediaSeeking is used on the filter graph which distributes the calls
                IMediaSeeking mSeek = (IMediaSeeking)currentFilterGraph;
                hr = mSeek.SetRate(newRate);
                DsError.ThrowExceptionForHR(hr);
            }

            // Start ASF
            hr = currentOutputFilter.Run(0);
            DsError.ThrowExceptionForHR(hr);
            // Run again
            hr = mc.Run();
            DsError.ThrowExceptionForHR(hr);
        }
        #endregion


        // Why did the graph stop running
        enum TerminationReason
        {
            UserCancelled,
            NoClientsTimeout,
            AllClientsDisconnected,
            Error,
            None
        }
    }
}
