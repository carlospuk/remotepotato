using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;
using System.Text;

namespace FatAttitude.MediaStreamer
{
    public class FFMPGProber
    {
        string PathToTools;
        string InputFile;

        #region Probing / Detecting Mappings

        EventWaitHandle probeHandle;
        public bool ProbeSuccess;
        public bool ProbeFinished;
        public string ProbeReport;
        private ShellCmdRunner probeRunner;
        private CommandArguments probeArguments;

        List<AVStream> AVStreams;
        public void Probe(string _pathToTools, string _inputFile)
        {
            // Wait for probing
            PathToTools = _pathToTools;
            InputFile = _inputFile;

            probeHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
            StartProbeAsync();
            probeHandle.WaitOne(8000);  // 8 second time out
        }

        public void StartProbeAsync()
        {
            ShellCmdRunner shellProber = new ShellCmdRunner();

            // From parameters
            probeRunner = new ShellCmdRunner();
            probeRunner.ProcessOutputReceived += new EventHandler<GenericEventArgs<string>>(probeRunner_ProcessOutputReceived);
            probeRunner.FileName = Path.Combine(PathToTools, "ffprobe.exe");

            // No need to monitor the children here
            probeRunner.MonitorChildren = false;

            // Set up objects
            probeArguments = new CommandArguments();
            ConstructProbeArguments();
            probeRunner.Arguments = probeArguments.ToString();
            AVStreams = new List<AVStream>();


            string txtResult = "";
            if (!probeRunner.Start(ref txtResult, true))
            {
                // Failure - return
                ProbeFinished = false; // We didnt' get to finish
                ProbeSuccess = false;
                ProbeReport = txtResult;
                probeHandle.Set();
                return;
            }

            // Doesn't really finish, I think it launches more threads... ...give it 3 seconds
            System.Threading.Thread.CurrentThread.Join(3000);

            // Probe has finished - we waited for exit.. ..let's analyse
            ProbeFinished = true;
            processOutputBuffer(); // Must do this first!
            AnalyseStreams();
        }
        void ConstructProbeArguments()
        {
            probeArguments.AddArg("-show_streams");

            // Input file; Quotes around file
            string quotedInputFile = "\"" + InputFile + "\"";
            probeArguments.AddArg(quotedInputFile);
        }
        CommandArguments mapArguments;
        void AddStreamToMap(AVStream strm)
        {
            mapArguments.AddArg("-map 0:" + strm.StreamIndex.ToString());
        }
        void AnalyseStreams()
        {
            SendDebugMessage("Analysing streams.");
            mapArguments = new CommandArguments();

            // Okay, let's look at what we got...
            // There must be at least one audio stream AND one video stream for us to add mapping parameters to ffmpeg
            if ((AVStreams == null) ||
                (AVAudioStreams.Count < 1) ||
                (AVVideoStreams.Count < 1)
                )
            {
                ProbeSuccess = false;
                ProbeReport = "Not enough audio or video streams detected to add mappings.";
                EndProbe();
                return;
            }


            // We already know there's at least one video stream
            // Use the first video stream
            if (AVVideoStreams.Count > 0)
            {
                SendDebugMessage("MediaProbe: Adding first video stream 0:" + AVVideoStreams[0].StreamIndex.ToString() + " (" + AVVideoStreams[0].CodecTag + ")");
                AddStreamToMap(AVVideoStreams[0]);
            }

            // TODO: For MP2, different behavioru with AC3?
            // We already know there's at least one audio stream
            if (AVAudioStreams.Count == 1) // If there's just one audio stream, use it.
                AddStreamToMap(AVAudioStreams[0]);
            else if (AVAudioStreamsStereo.Count > 0) // If there are some stereo streams
            {
                if (AVAudioStreamsStereo.Count == 1) // If there's just one stereo audio stream, use it
                {
                    AVStream ast = AVAudioStreamsStereo[0];
                    SendDebugMessage("MediaProbe: Adding only stereo audio stream 0:" + ast.StreamIndex.ToString() + " (" + ast.CodecTag + ")");
                    AddStreamToMap(ast);
                }
                else
                {
                    // There are multiple stereo streams: add the LAST stereo stream
                    AVStream ast = AVAudioStreamsStereo[AVAudioStreamsStereo.Count - 1];
                    SendDebugMessage("MediaProbe: Adding last stereo audio stream 0:" + ast.StreamIndex.ToString() + " (" + ast.CodecTag + ")");
                    AddStreamToMap(ast);
                }
            }
            else
            {
                // There are no stereo streamss: just add the first audio stream
                AVStream ast = AVAudioStreams[0];
                SendDebugMessage("MediaProbe: No stereo audio streams found, adding first audio stream 0:" + ast.StreamIndex.ToString() + " (" + ast.CodecTag + ")");
                AddStreamToMap(ast);
            }

            SendDebugMessage("MediaProbe: Analysis complete.  Mappings are: " + mapArguments.ToString());

            // IT's a success!
            ProbeReport = "Probe finished OK.";
            ProbeSuccess = true;
            ProbeFinished = true;

            EndProbe();
        }

        AVStream currentStream = null;
        const string kBeginStreamBlock = @"[stream]";
        const string kEndStreamBlock = @"[/stream]";

        /*
 * [STREAM]
index=2
codec_name=mp2
codec_long_name=MP2 (MPEG audio layer 2)
codec_type=audio
codec_time_base=0/1
codec_tag_string=P[0][0][0]
codec_tag=0x0050
sample_rate=48000.000000
channels=1
bits_per_sample=0
r_frame_rate=0/0
avg_frame_rate=125/3
time_base=1/10000000
start_time=1.364370
duration=4.191059
[/STREAM]*/
        object obProcessOutput = new object();
        List<string> outputBuffer = new List<string>();
        void probeRunner_ProcessOutputReceived(object sender, GenericEventArgs<string> e)
        {
            lock (obProcessOutput)
            {
                if (e.Value == null) return;

                outputBuffer.Add(e.Value);
            }
        }
        void processOutputBuffer()
        {
            foreach (string s in outputBuffer)
            {
                string txtOutput = s.Trim().ToLowerInvariant();

                if (txtOutput.Length < 3) continue;

                if (txtOutput.Equals(kBeginStreamBlock))
                {
                    currentStream = new AVStream();
                }

                if (currentStream == null) continue; // ignore if we're not in a stream block

                if (txtOutput.Equals(kEndStreamBlock))
                {
                    AVStreams.Add(currentStream);
                    currentStream = null; // does this reset my pointer, or the stored one??
                }

                // From this point we're only interested in Key=Value lines
                if (!txtOutput.Contains("=")) continue;

                List<string> outputParts = txtOutput.Split(new char[] { '=' }).ToList();


                if (outputParts.Count < 2) continue; // not formatted properly
                if (outputParts[0].Length < 1) continue; // no first part
                if (outputParts[1].Length < 1) continue; // no second part



                switch (outputParts[0])
                {
                    case "index":
                        int index;
                        if (int.TryParse(outputParts[1], out index))
                            currentStream.StreamIndex = index;
                        else
                            SendDebugMessage("Media Probe: Cannot parse index of stream: " + outputParts[1]);
                        break;

                    case "channels":
                        int nChannels;
                        if (int.TryParse(outputParts[1], out nChannels))
                            currentStream.Channels = nChannels;
                        break;

                    case "codec_tag":
                        currentStream.CodecTag = outputParts[1];
                        break;

                    case "codec_type":
                        if (outputParts[1].Equals("audio")) currentStream.CodecType = AVCodecType.Audio;
                        if (outputParts[1].Equals("video")) currentStream.CodecType = AVCodecType.Video;
                        break;

                    default:
                        break;
                }
            }
        }
        void EndProbe()
        {
            // Signal that we're done
            probeHandle.Set();
        }

        // Helpers / Filters
        List<AVStream> AVAudioStreamsStereo
        {
            get
            {
                List<AVStream> output = new List<AVStream>();

                foreach (AVStream s in AVAudioStreams)
                {
                    if (s.Channels > 1)
                        output.Add(s);
                }

                return output;
            }
        }
        List<AVStream> AVAudioStreams
        {
            get
            {
                return AVStreamsOfType(AVCodecType.Audio);
            }
        }
        List<AVStream> AVVideoStreams
        {
            get
            {
                return AVStreamsOfType(AVCodecType.Video);
            }
        }
        List<AVStream> AVStreamsOfType(AVCodecType ctype)
        {

            List<AVStream> output = new List<AVStream>();

            foreach (AVStream s in AVStreams)
            {
                if (s.CodecType == ctype)
                    output.Add(s);
            }

            return output;

        }
        #endregion

        #region Debug
        public event EventHandler<GenericEventArgs<string>> DebugMessage;
        void SendDebugMessage(string txtDebug)
        {
            System.Diagnostics.Debug.Print(txtDebug);

            if (DebugMessage != null)
                DebugMessage(this, new GenericEventArgs<string>(txtDebug));
        }
        #endregion

    }
}
