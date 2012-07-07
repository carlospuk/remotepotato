using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System.Threading;
using FatAttitude.Functions;

namespace FatAttitude.MediaStreamer
{
    public class ShellCmdRunner
    {
        // Public
        public string FileName { get; set; }
        public string Arguments {get; set;}
        public bool DontCloseWindow { get; set; }

        // Private members
        public bool IsRunning;
        private Process runningProcess;
        Thread thrdReadStandardOut;
        object RunningProcessLock = new object();

        public ShellCmdRunner()
        {
        }

        public bool Start(ref string txtResult)
        {
            if (IsRunning) return false;

            // Create Process
            // Start Info
            ProcessStartInfo psi = new ProcessStartInfo();            
            psi.UseShellExecute = false;
            psi.CreateNoWindow = (! this.DontCloseWindow);

            string shortFN = Functions.FileWriter.GetShortPathName(this.FileName);
            string strQuotedFileName = @"""" + shortFN + @"""";
            psi.FileName = strQuotedFileName;
            psi.Arguments = this.Arguments;

            // Redirect error
            psi.RedirectStandardError = true;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardInput = false;

            // Events / Handlers
            runningProcess = new Process();
            runningProcess.EnableRaisingEvents = true;
            runningProcess.Exited += new EventHandler(runningProcess_Exited);
            runningProcess.ErrorDataReceived += new DataReceivedEventHandler(runningProcess_ErrorDataReceived);

            // Go
            Debug.Print("Running: " + psi.FileName + " " + psi.Arguments);
            runningProcess.StartInfo = psi;
            runningProcess.Start();
            runningProcess.PriorityClass = ProcessPriorityClass.BelowNormal;
            IsRunning = true;

            
            /*StdOutBuffer = new byte[200000] ;
            runningProcess.StandardOutput.BaseStream.BeginRead(StdOutBuffer, 0, 256, ReadStdOut, null); 
             THIS ISN'T WORKING; FFMPEG REFUSES TO WRITE TO STANDARD OUTPUT WHEN THIS ASYNC METHOD IS READING IT
             */
            // Read standard output on a new thread
            thrdReadStandardOut = new Thread(new ThreadStart(ReadStandardOutput));
            thrdReadStandardOut.Priority = ThreadPriority.Lowest;
            thrdReadStandardOut.Start(); 

            runningProcess.BeginErrorReadLine(); // receive standard error asynchronously

            return true;
        }
        bool CreateBatchFile(string batchFilePath)
        {
            StringBuilder sbBatchFile = new StringBuilder(150);

            // Short path name
            string shortFN = Functions.FileWriter.GetShortPathName(this.FileName);
            string strQuotedFileName = @"""" + shortFN + @"""";
            string strFileNameAndArguments = strQuotedFileName + " " + this.Arguments;
            sbBatchFile.AppendLine(strFileNameAndArguments);

            return FileWriter.WriteTextFileToDisk(batchFilePath, sbBatchFile.ToString(), Encoding.UTF8);
        }


        #region Kill  
        volatile bool WasKilledManually = false;
        public void KillNow()
        {
            if (!IsRunning) return;
            WasKilledManually = true;
            
            // Stop looking for timeouts (if indeed we are)
            EndTimeoutDetection();

            KillStandardOutputReadingThread();

            CloseOrKillRunningProcess();



            IsRunning = false;
            RaiseProcessFinishedEvent(true); // flag that it was aborted, e.g. so the FFMPGrunner doesn't write a half-read segment to disk
            
        }

        // test
        [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions]
        private void CloseOrKillRunningProcess()
        {
            lock (RunningProcessLock)
            {
                try // Raises error
                {
                    if (!runningProcess.HasExited)
                    {
                        // Does not work runningProcess.CloseMainWindow();
                        //runningProcess.Close();
                        runningProcess.Kill();
                    }
                    
                }
                catch (InvalidOperationException) // already killed
                {
                    // Do nothing
                }
                catch
                { }
            }
        }


        #endregion

        
        bool raisedProcessFinishedEvent = false;
        public event EventHandler<GenericEventArgs<processfinishedEventArgs>> ProcessFinished;
        void runningProcess_Exited(object sender, EventArgs e)
        {
            // We're done
            Debug.Print("Process finished.");

            if (!WasKilledManually)
            {
                // Stop looking for timeouts (if indeed we are)
                EndTimeoutDetection();

                KillStandardOutputReadingThread();
            }

            IsRunning = false;

            RaiseProcessFinishedEvent(false);
        }

        private void RaiseProcessFinishedEvent(bool wasAborted)
        {
            if (raisedProcessFinishedEvent) return;

            raisedProcessFinishedEvent = true;

            // Raise event
            if (ProcessFinished != null)
                ProcessFinished(this, new GenericEventArgs<processfinishedEventArgs>(new processfinishedEventArgs(wasAborted)));

            IsRunning = false;
            runningProcess = null;
        }
        

        #region Standard Output stream capture
        public event EventHandler<GenericEventArgs<byte[]>> StandardOutputReceived;
        public object StandardOutputReceivedLock = new object();
        void ReadStandardOutput()
        {
            BinaryReader br;

            lock (RunningProcessLock)
            {
                 br  = new BinaryReader(runningProcess.StandardOutput.BaseStream);
            }
                bool abort = false;
            
            // Time out this reader when the stream dries up; there is no other way to detect an EOS to my knowledge
            BeginTimeoutDetection();

            while (!abort)
            {
                try
                {
                    byte[] bytes;
                    lock (RunningProcessLock)
                    {
                        bytes = br.ReadBytes(1);  // Best keep at one, so when we hit EOS we've always got all the data out when it times out
                    }

                    lock (lastReadStandardOutputLock)  
                    {
                        lastReadStandardOutput = DateTime.Now;  // Track the time out
                    }

                    lock (StandardOutputReceivedLock)
                    {
                        if (StandardOutputReceived != null)
                            StandardOutputReceived(this, new GenericEventArgs<byte[]>(bytes));
                    }
                }
                catch (EndOfStreamException)
                {
                    abort = true;
                }
            }

            
        }
        #region TimeOut
        private DateTime lastReadStandardOutput; // used to time out the reading thread 
        object lastReadStandardOutputLock = new object();
        Timer binaryReaderTimeOutTimer;
#if DEBUG
        const int ASSUME_FFMPEG_FINISHED_AFTER_TIMEOUT = 100;  // after 10 seconds of no output, we assume that we're hung on ReadBytes(1); above and at the end of the stream
#else
        const int ASSUME_FFMPEG_FINISHED_AFTER_TIMEOUT = 10;  // after 10 seconds of no output, we assume that we're hung on ReadBytes(1); above and at the end of the stream
#endif
        void BeginTimeoutDetection()
        {
            // Important - set this to prevent an immediate time out
            lock (lastReadStandardOutputLock)
            {
                lastReadStandardOutput = DateTime.Now;
            }
            
            TimerCallback tcb = new TimerCallback(TimeoutDetect_Tick);
            binaryReaderTimeOutTimer = new Timer(tcb, null, TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(500));

            
        }
        void TimeoutDetect_Tick(object stateObject)
        {
            lock (lastReadStandardOutputLock)
            {
                TimeSpan timeSinceReadOutput = DateTime.Now.Subtract(lastReadStandardOutput);

                if (timeSinceReadOutput.TotalSeconds >= ASSUME_FFMPEG_FINISHED_AFTER_TIMEOUT)
                {
                    EndTimeoutDetection();
                    DoStandardOutputTimeOut();

                }
            }
        }
        void DoStandardOutputTimeOut()
        {
            Debug.Print("*** SHELLCMDRUNNER TIMEOUT :  Standard Output stopped for more than " + ASSUME_FFMPEG_FINISHED_AFTER_TIMEOUT.ToString() +  " seconds - timing out.");

            // Kill the whole process, which will raise the Process Finished event
            if (!IsRunning) return;

            // Stop looking for timeouts (if indeed we are)
            EndTimeoutDetection();

            // Kill thread and/or process
            KillStandardOutputReadingThread();
            CloseOrKillRunningProcess();

            RaiseProcessFinishedEvent(false); // flag that it was NOT aborted, so the FFMPGrunner gets its final data segment

            IsRunning = false;
        }
        private void KillStandardOutputReadingThread()
        {
            // Kill STDOUT reading thread
            if (thrdReadStandardOut != null)
            {
                try
                {
                    thrdReadStandardOut.Abort();
                    thrdReadStandardOut = null;
                }
                catch { }
            }
        }
        void EndTimeoutDetection()
        {
            if (binaryReaderTimeOutTimer == null) return;

            // End timer
            binaryReaderTimeOutTimer.Dispose();
            binaryReaderTimeOutTimer = null;
        }
        #endregion

        #endregion

        #region Standard Error Redirection
        public event EventHandler<GenericEventArgs<string>> StandardErrorReceivedLine;
        void runningProcess_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (StandardErrorReceivedLine != null)
                StandardErrorReceivedLine(this, new GenericEventArgs<string>(e.Data));
        }
        #endregion
    }

    public class processfinishedEventArgs
    {
        public bool WasAborted { get; set; }

        public processfinishedEventArgs(bool wasAbort)
        {
            WasAborted = wasAbort;
        }
    }
    
}
