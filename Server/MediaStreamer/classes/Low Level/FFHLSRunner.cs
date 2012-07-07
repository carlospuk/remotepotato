using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.IO;
using System.Threading;
using FatAttitude.MediaStreamer.HLS;


namespace FatAttitude.MediaStreamer
{
    internal class FFHLSRunner 
    {
        public string InputFile { get; set; }
        public string WorkingDirectory{ get; set; }
        public string AdditionalArgsString { get; set; }
        public bool SettingsDefaultDebugAdvanced { get; set; }
        public bool Transcode { get; set; }
        public bool IsRunning;
        int StartAtSeconds;
        
        // Private
        SegmentStore Store;
        private ShellCmdRunner shellRunner;
        private CommandArguments cmdArguments;
        
        private CommandArguments segmentArguments;
        public VideoEncodingParameters EncodingParameters;
        public string MapArgumentsString;
        private string PathToTools;
        

        public FFHLSRunner(string pathToTools, SegmentStore segStore)
        {
            // From parameters
            PathToTools = pathToTools;
            Store = segStore;

            // Defaults
            EncodingParameters = new VideoEncodingParameters();
            AudioSyncAmount = 1;  // 2 can create streaming issues
        }
        public FFHLSRunner(string pathToTools, SegmentStore store, VideoEncodingParameters vidParameters)
            : this(pathToTools, store)
        {
            EncodingParameters = vidParameters;
        }

        #region Top Level - Start/Stop/IsRunning
        public bool IsReStarting = false;
        public bool Start(int _startAtSegment, ref string txtResult)
        {
            AwaitingSegmentNumber = _startAtSegment; // need to set this pretty sharpish
            StartAtSeconds = _startAtSegment * EncodingParameters.SegmentDuration;

            if (IsRunning)
            {
                IsReStarting = true;
                Abort();
            }

            Initialise();

            IsReStarting = false;
            IsRunning = shellRunner.Start(ref txtResult);
            return IsRunning;
        }
        void Initialise()
        {
            shellRunner = new ShellCmdRunner();
            shellRunner.ProcessFinished += new EventHandler<GenericEventArgs<processfinishedEventArgs>>(shellRunner_ProcessFinished);
            shellRunner.FileName = Path.Combine(PathToTools, "ffmpeg.exe");
            shellRunner.StandardErrorReceivedLine += new EventHandler<GenericEventArgs<string>>(shellRunner_StandardErrorReceivedLine);
            shellRunner.StandardOutputReceived += new EventHandler<GenericEventArgs<byte[]>>(shellRunner_StandardOutputReceived);

            // Incoming data from ffmpeg STDOUt - mangements
            InitTempWriter();

            // Set up objects
            cmdArguments = new CommandArguments();
            segmentArguments = new CommandArguments();

            // Arguments
            ConstructArguments();
            shellRunner.Arguments = cmdArguments.ToString();
        }
        public void Abort()
        {
            if (!IsRunning) return;

            // insert kill file into the directory to stop the streamer
            SendDebugMessage("FF Runner: Abort signalled.");

            if (shellRunner != null)
            {
                lock (shellRunner.StandardOutputReceivedLock)
                {
                    // Unhook events
                    shellRunner.StandardErrorReceivedLine -= new EventHandler<GenericEventArgs<string>>(shellRunner_StandardErrorReceivedLine);
                    shellRunner.StandardOutputReceived -= new EventHandler<GenericEventArgs<byte[]>>(shellRunner_StandardOutputReceived);
                    shellRunner.ProcessFinished -= new EventHandler<GenericEventArgs<processfinishedEventArgs>>(shellRunner_ProcessFinished);
                }

                // Kill the shell runner 
                shellRunner.KillNow();
            }

            shellRunner = null;
            IsRunning = false;
        }
        #endregion

        #region Incoming Events
        void shellRunner_ProcessFinished(object sender, GenericEventArgs<processfinishedEventArgs> e)
        {
            if (sender != shellRunner) return;

            if (!e.Value.WasAborted) // If finished normally, write the [final] segment to disk
            {
                processRemainingBytes();
                finaliseCurrentSegment();
            }

            SendDebugMessage("FF Runner: Shell process is finished.  (");
            shellRunner = null;
            IsRunning = false;
        }
        #endregion


        #region Incoming Segments
        // Members
        BinaryWriter bw;
        List<byte> byteHoldingBuffer;
        int delimiterLength;
        public int AwaitingSegmentNumber { get; set; }
        byte[] Delimiter = null;

        // Methods
        void InitTempWriter()
        {
            byteHoldingBuffer = new List<byte>();
            
            initDelimiter();

            beginNextSegment();
        }
        void initDelimiter()
        {
            //if (Delimiter == null) Delimiter = System.Text.UTF8Encoding.UTF8.GetBytes(  "-------SEGMENT-BREAK-------");
            if (Delimiter == null) Delimiter = System.Text.UTF8Encoding.UTF8.GetBytes("-SEGBREAK-");
            delimiterLength = 10;
        }
        void shellRunner_StandardOutputReceived(object sender, GenericEventArgs<byte[]> e)
        {
            if (sender != shellRunner) return;  // an old shell runner

            processByteBuffer(e.Value);
        }
        void processByteBuffer(byte[] bytes)
        {
            foreach (byte b in bytes)
            {
                processByte(b);
            }
        }
        bool startedPumping = false;
        void processByte(byte b)
        {
            lock (byteHoldingBuffer)
            {
                byteHoldingBuffer.Add(b);
                if (byteHoldingBuffer.Count == delimiterLength )
                {
                    if (!startedPumping) startedPumping = true;

                    // Check for a match - if it matches then dump the byte buffer and start a new file
                    if (byteHoldingBuffer.SequenceEqual(Delimiter))
                    {
                        byteHoldingBuffer.Clear();
                        AwaitingSegmentNumber++;
                        switchToNextSegment();
                    }
                    else
                    {
                        if (bw.BaseStream.Position < MaxIncomingDataSize)
                        {
                            // dequeue the first byte (FIFO) and write it to disk
                            bw.Write(byteHoldingBuffer[0]);
                            byteHoldingBuffer.RemoveAt(0);
                        }
                        else
                        {
                            SendDebugMessage("WARNING: Data spill; segment exceeded max (" + MaxIncomingDataSize.ToString() + ") size.");
                            return;
                        }
                    }
                }

            }
        }
        void processRemainingBytes()
        {
            while (byteHoldingBuffer.Count > 0)
            {
                if (bw.BaseStream.Position < MaxIncomingDataSize)
                {
                    // dequeue the first byte (FIFO) and write it to disk
                    bw.Write(byteHoldingBuffer[0]);
                    byteHoldingBuffer.RemoveAt(0);
                }
                else
                    SendDebugMessage("WARNING: Data spill; segment exceeded max (" + MaxIncomingDataSize.ToString() + ") size.");
            }
        }
        void switchToNextSegment()
        {
            finaliseCurrentSegment();

            beginNextSegment();
            
        }
        void finaliseCurrentSegment()
        {
            // Transfer the data across from the buffer
            incomingSegment.Data = new byte[bw.BaseStream.Position];
            bw.Flush();
            bw.Close();

            Array.Copy(incomingSegmentDataBuffer, incomingSegment.Data, incomingSegment.Data.Length);
            incomingSegmentDataBuffer = null;  // clear/dispose

            StoreSegment();
        }
        private void StoreSegment()
        {
            // Store the segment
            Store.StoreSegment(incomingSegment);
        }
        Segment incomingSegment = null;
        const int MaxIncomingDataSize = 2000000;  // 2Mb per segment maximum size
        byte[] incomingSegmentDataBuffer;
        void beginNextSegment()
        {
            incomingSegment = new Segment();
            incomingSegment.Number = AwaitingSegmentNumber;

            incomingSegmentDataBuffer = new byte[MaxIncomingDataSize]; 
            MemoryStream ms = new MemoryStream(incomingSegmentDataBuffer);
            bw = new BinaryWriter(ms);
        }
        #endregion

        #region Parameters
        public int AudioSyncAmount { get; set; }


        /*
         * C:\Program Files (x86)\AirVideoServer\ffmpeg.exe" 
         * --segment-length 4
         * --segment-offset 188
         * --conversion-id 548cf790-c04f-488a-96be-aae2968f272bdefd0e1d-2bdf-457d-ab15-3eb6c51ccf85 
         * --port-number 46631 
         * -threads 4 
         * -flags +loop 
         * -g 30 -keyint_min 1 
         * -bf 0 
         * -b_strategy 0 
         * -flags2 -wpred-dct8x8 -cmp +chroma -deblockalpha 0 -deblockbeta 0 -refs 1 -coder 0 -me_range 16 -subq 5 -partitions +parti4x4+parti8x8+partp8x8 
         * -trellis 0 
         * -sc_threshold 40 -i_qfactor 0.71 -qcomp 0.6 -map 0.1:0.1 -map 0.0:0.0 -ss 188.0 
         * -vf "crop=720:572:0:2, scale=568:320"
         * -aspect 720:576
         * -y
         * -async 1
         * -f mpegts
         * -vcodec libx264
         * -bufsize 1024k
         * -b 1200k
         * -bt 1300k
         * -qmax 48
         * -qmin 2
         * -r 25.0
         * -acodec libmp3lame
         * -ab 192k
         * -ar 48000
         * -ac 2 
         */
        void ConstructArguments()
        {
            // Use either the standard ffmpeg template or a custom one
            string strFFMpegTemplate = (string.IsNullOrWhiteSpace(EncodingParameters.CustomFFMpegTemplate)) ?
                @"{THREADS} {H264PROFILE} {H264LEVEL} -flags +loop -g 30 -keyint_min 1 -bf 0 -b_strategy 0 -flags2 -wpred-dct8x8 -cmp +chroma -deblockalpha 0 -deblockbeta 0 -refs 1 {MOTIONSEARCHRANGE} {SUBQ} {PARTITIONS} -trellis 0 -coder 0 -sc_threshold 40 -i_qfactor 0.71 -qcomp 0.6 -qdiff 4 -rc_eq 'blurCplx^(1-qComp)' {MAPPINGS} {STARTTIME} {INPUTFILE} {ASPECT} {FRAMESIZE} {DEINTERLACE} -y {AUDIOSYNC} -f mpegts -vcodec libx264 {VIDEOBITRATE} {VIDEOBITRATEDEVIATION} -qmax 48 -qmin 2 -r 25 {AUDIOCODEC} {AUDIOBITRATE} {AUDIOSAMPLERATE} {AUDIOCHANNELS} {VOLUMELEVEL}" :                
                EncodingParameters.CustomFFMpegTemplate;

            // Segment length and offset
            segmentArguments.AddArgCouple("--segment-length", EncodingParameters.SegmentDuration.ToString());
            segmentArguments.AddArgCouple("--segment-offset", StartAtSeconds.ToString() );
            cmdArguments.AddArg(segmentArguments.ToString());
            
            // Multi threads
            strFFMpegTemplate = strFFMpegTemplate.Replace("{THREADS}", "-threads 4");

            // Me Range
            strFFMpegTemplate = strFFMpegTemplate.Replace("{MOTIONSEARCHRANGE}", ("-me_range " + EncodingParameters.MotionSearchRange.ToString()));
            
            // SUBQ - important as setting it too high can slow things down
            strFFMpegTemplate = strFFMpegTemplate.Replace("{SUBQ}", ("-subq " + EncodingParameters.X264SubQ.ToString()) );

            // Partitions
            string strPartitions = (EncodingParameters.PartitionsFlags.Length > 0) ?
                "-partitions " + EncodingParameters.PartitionsFlags : "";
            strFFMpegTemplate = strFFMpegTemplate.Replace("{PARTITIONS}", strPartitions);

            // Add Mappings
            string strMapArgs = (string.IsNullOrEmpty(MapArgumentsString)) ? "" : MapArgumentsString;
            strFFMpegTemplate = strFFMpegTemplate.Replace("{MAPPINGS}", strMapArgs);

            // Start at : MUST BE BEFORE INPUT FILE FLAG -i *** !!! 
            string strStartTime = (StartAtSeconds <= 0) ? "" :  ("-ss " + StartAtSeconds.ToString());
            strFFMpegTemplate = strFFMpegTemplate.Replace("{STARTTIME}", strStartTime);

            // Input file - make short to avoid issues with UTF-8 in batch files  IT IS VERY IMPORTANT WHERE THIS GOES; AFTER SS BUT BEFORE VCODEC AND ACODEC
            string shortInputFile = Functions.FileWriter.GetShortPathName(InputFile);
            // Quotes around file
            string quotedInputFile = "\"" + shortInputFile + "\"";
            strFFMpegTemplate = strFFMpegTemplate.Replace("{INPUTFILE}", ("-i " + quotedInputFile));

            // Aspect ratio and frame size
            string strAspectRatio = (EncodingParameters.OutputSquarePixels) ? "-aspect 1:1" : "-aspect " + EncodingParameters.AspectRatio;
            strFFMpegTemplate = strFFMpegTemplate.Replace("{ASPECT}", strAspectRatio);
            string strFrameSize = "-s " + EncodingParameters.ConstrainedSize;
            strFFMpegTemplate = strFFMpegTemplate.Replace("{FRAMESIZE}", strFrameSize);


            // Deinterlace (experimental)
            string strDeinterlace =  (EncodingParameters.DeInterlace) ? "-deinterlace" : "";
            strFFMpegTemplate = strFFMpegTemplate.Replace("{DEINTERLACE}", strDeinterlace);

            // OPTIONAL FOR LATER:  -vf "crop=720:572:0:2, scale=568:320"
            // Think this means crop to the aspect ratio, then scale to the normal frame

            // Audio sync amount
            string strAudioSync = "-async " +  AudioSyncAmount.ToString();
            strFFMpegTemplate = strFFMpegTemplate.Replace("{AUDIOSYNC}", strAudioSync);

            // Video bitrate
            string strVideoBitRateOptions = "-bufsize " +  EncodingParameters.VideoBitRate + " -b " + EncodingParameters.VideoBitRate;  //cmdArguments.AddArgCouple("-maxrate", VideoBitRate);
            strFFMpegTemplate = strFFMpegTemplate.Replace("{VIDEOBITRATE}", strVideoBitRateOptions);

            // Max video bitrate (optional)
            string strMaxVideoBitRate = "-maxrate " + EncodingParameters.VideoBitRate;
            strFFMpegTemplate = strFFMpegTemplate.Replace("{MAXVIDEOBITRATE}", strMaxVideoBitRate);
            
            string strVideoBitRateDeviation = "-bt " + EncodingParameters.BitRateDeviation;
            strFFMpegTemplate = strFFMpegTemplate.Replace("{VIDEOBITRATEDEVIATION}", strVideoBitRateDeviation);

            
            // Restrict H264 encoding level (e.g. for iPhone 3G)
            string strH264Level = (EncodingParameters.X264Level > 0) ? ("-level " + EncodingParameters.X264Level.ToString() )  : "";
            strFFMpegTemplate = strFFMpegTemplate.Replace("{H264LEVEL}", strH264Level);
            string strH264Profile = (string.IsNullOrWhiteSpace(EncodingParameters.X264Profile)) ? "" : "-profile " + EncodingParameters.X264Profile;
            strFFMpegTemplate = strFFMpegTemplate.Replace("{H264PROFILE}", strH264Profile);

            // Audio: MP3 - must be after input file flag -i  //
            string strAudioCodecOptions = "";
            switch (EncodingParameters.AudioCodec)
            {
                case VideoEncodingParameters.AudioCodecTypes.AAC:
                    strAudioCodecOptions = "-acodec aac -strict experimental";
                    break;

                default:
                    strAudioCodecOptions = "-acodec libmp3lame";
                    break;

                //// "libfaac");
            }
            strFFMpegTemplate = strFFMpegTemplate.Replace("{AUDIOCODEC}", strAudioCodecOptions);

            // Audio Bitrate
            string strAudioBitRate = "-ab " + EncodingParameters.AudioBitRate;
            strFFMpegTemplate = strFFMpegTemplate.Replace("{AUDIOBITRATE}", strAudioBitRate);

            // Audio sample rate
            string strAudioSampleRate = "-ar " + EncodingParameters.AudioSampleRate;
            strFFMpegTemplate = strFFMpegTemplate.Replace("{AUDIOSAMPLERATE}", strAudioSampleRate);

            // Force stereo
            string strAudioChannels = "-ac 2";
            strFFMpegTemplate = strFFMpegTemplate.Replace("{AUDIOCHANNELS}", strAudioChannels);

            // Volume Level
            string strVolumeBoost = "";
            if (EncodingParameters.AudioVolumePercent != 100)
            {
                double fVolumeBytes = (256.0 * (EncodingParameters.AudioVolumePercent / 100.0));
                int iVolumeBytes = Convert.ToInt32(fVolumeBytes);
                strVolumeBoost = "-vol " + iVolumeBytes.ToString();
            }
            strFFMpegTemplate = strFFMpegTemplate.Replace("{VOLUMELEVEL}", strVolumeBoost);
            
            // Pipe to segmenter (ie send to standard output now)
            strFFMpegTemplate = strFFMpegTemplate + " -";


            // Commit - add to the arguments
            cmdArguments.AddArg(strFFMpegTemplate);
            
        }
        string croppedSizeForAspectRatio()
        {
            return "";
        }
        #endregion

        #region Debug
        public event EventHandler<GenericEventArgs<string>> DebugMessage;
        void shellRunner_StandardErrorReceivedLine(object sender, GenericEventArgs<string> e)
        {
            if (sender != shellRunner) return;
            if (e.Value == null) return; // this is actually required.  wow.

            if (SettingsDefaultDebugAdvanced)
                SendDebugMessage("[C]" + e.Value);
        }

        void SendDebugMessage(string txtDebug)
        {
            if (DebugMessage != null)
                DebugMessage(this, new GenericEventArgs<string>(txtDebug));
        }
        void prober_DebugMessage(object sender, GenericEventArgs<string> e)
        {
            SendDebugMessage(e.Value);
        }
        #endregion
    }


}
