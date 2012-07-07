using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;		
using System.Text;
using System.Runtime.InteropServices;
using System.Xml;
using DirectShowLib;
using DirectShowLib.SBE;
using WindowsMediaLib;

// Audio subtypes
//WTV (uk, plays with ms dtv audio):  46adbd28-6fd0-4796-93b2-155c51dc048d
// WTV (us)   46adbd28-6fd0-4796-93b2-155c51dc048d
// DVRMS c4c4c4d0-0049-4e2b-98fb-9537f6ce516d

namespace FatAttitude.WTVTranscoder
{
    /// <summary>
    /// The base conversion class 
    /// Contains methods to build a DirectShow graph that transcodes a Stream Buffer source (.WTV file) with 
    /// </summary>
    public class DSTranscoderBase : IDisposable
    {
        public DateTime CreationDate { get; set; }
        public int ID { get; set; }
        protected IFilterGraph currentFilterGraph;  // The main DirectShow filter graph we'll be working with
        protected IBaseFilter currentOutputFilter;  // The current output filter; usually an AsfWriter
        protected IBaseFilter currentSBEfilter;  // The current input filter; a Stream Buffer Engine source
        protected IBaseFilter currentSourceFilter;  // The current input filter; a Stream Buffer Engine source
        protected bool UsingSBEFilter = true;
        protected DisposalCleanup dc;  // A class written by Stephen Toub to aid in cleanup / release of COM objects

        // Constructor
        public DSTranscoderBase()
        {
            // Create a new filter graph to work with, and ensure it's disposed when this object is
            dc = new DisposalCleanup();
            currentFilterGraph = (IFilterGraph)new FilterGraph();
            dc.Add(currentFilterGraph);

            CreationDate = DateTime.Now;
        }
        public void Dispose()
        {
            // Disposal dc automatically disposes here - I hope!
        }

        // Base Events
        public event EventHandler<DebugMessageEventArgs> DebugMessageGenerated;

        /// <summary>
        /// Main method - builds a DirectShow graph to transcode a Stream Buffer source file (.wtv or .dvr-ms)
        /// The graph looks like this:
        /// 
        ///                                     /--> AUDIO DECODER  \
        /// SBE INPUT FILTER => TAG/DECRYPT =>                        ==>  OUTPUT FILTER (ASFWRITER)
        ///                                     \--> VIDEO DECODER  /
        ///                                     
        /// </summary>
        /// <param name="strq"></param>
        /// <returns></returns>
        public DSStreamResultCodes InitWithFile(WTVStreamingVideoRequest strq)
        {
            FileInfo fiInputFile = new FileInfo(strq.FileName);
            string txtOutputFNPath = fiInputFile.FullName + ".wmv";
            if (
                (fiInputFile.Extension.ToLowerInvariant().Equals(".wtv")) ||
                (fiInputFile.Extension.ToLowerInvariant().Equals(".dvr-ms"))
               )
                return InitWithStreamBufferFile(strq);
            else
                return InitWithVideoFile(strq);
        }
        
        DSStreamResultCodes InitWithVideoFile(WTVStreamingVideoRequest strq)
        {
            UsingSBEFilter = false;  // Not using stream buffer

            // Init variables
            IPin[] pin = new IPin[1];
            string dPin = string.Empty;
            string sName = string.Empty;
            string dName = string.Empty;
            string sPin = string.Empty;
            FileInfo fiInputFile = new FileInfo(strq.FileName);
            string txtOutputFNPath = fiInputFile.FullName + ".wmv";
            if (
                (fiInputFile.Extension.ToLowerInvariant().Equals(".wtv")) ||
                (fiInputFile.Extension.ToLowerInvariant().Equals(".dvr-ms"))
               ) return DSStreamResultCodes.ErrorInvalidFileType;

            int hr = 0;
            try
            {
                // Get the graphbuilder interface
                SendDebugMessage("Creating Graph Object", 0);
                IGraphBuilder graphbuilder = (IGraphBuilder)currentFilterGraph;

                // Create an ASF writer filter 
                SendDebugMessage("Creating ASF Writer", 0);
                WMAsfWriter asf_filter = new WMAsfWriter();
                dc.Add(asf_filter); // CHECK FOR ERRORS
                currentOutputFilter = (IBaseFilter)asf_filter;  // class variable
                // Add the ASF filter to the graph
                hr = graphbuilder.AddFilter((IBaseFilter)asf_filter, "WM Asf Writer");
                DsError.ThrowExceptionForHR(hr);

                // Set the filename
                SendDebugMessage("Setting filename", 0);
                IFileSinkFilter sinkFilter = (IFileSinkFilter)asf_filter;
                string destPathFN = fiInputFile.FullName + ".wmv";
                hr = sinkFilter.SetFileName(destPathFN, null);
                DsError.ThrowExceptionForHR(hr);

                // Handy to have an ACM Wrapper filter hanging around for AVI files with MP3 audio
                SendDebugMessage("Adding ACM Wrapper", 0);
                IBaseFilter ACMFilter =  FilterDefinition.AddToFilterGraph(FilterDefinitions.Other.ACMWrapperFilter, ref graphbuilder);
                dc.Add(ACMFilter);

                // Render file - then build graph
                SendDebugMessage("Rendering file", 0);
                graphbuilder.RenderFile(fiInputFile.FullName, null);
                SendDebugMessage("Saving graph", 0);
                FilterGraphTools.SaveGraphFile(graphbuilder, "C:\\ProgramData\\RemotePotato\\lastfiltergraph.grf");

                // Are both our ASF pins connected?
                IPin ASFVidInputPin = FilterGraphTools.FindPinByMediaType((IBaseFilter)asf_filter, PinDirection.Input, MediaType.Video, MediaSubType.Null);
                IPin ASFAudInputPin = FilterGraphTools.FindPinByMediaType((IBaseFilter)asf_filter, PinDirection.Input, MediaType.Audio, MediaSubType.Null);

                // Run graph [can use this also to get media type => see, e.g. dvrmstowmvhd by Babgvant]
                SendDebugMessage("Run graph for testing purposes", 0);
                IMediaControl tempControl = (IMediaControl)graphbuilder;
                IMediaEvent tempEvent = (IMediaEvent)graphbuilder;
                DsError.ThrowExceptionForHR(tempControl.Pause());
                EventCode pEventCode;
                hr = tempEvent.WaitForCompletion(1000, out pEventCode);

                // Get media type from vid input pin for ASF writer
                AMMediaType pmt = new AMMediaType();
                hr = ASFVidInputPin.ConnectionMediaType(pmt);

                FrameSize SourceFrameSize = null;
                if (pmt.formatType == FormatType.VideoInfo2)
                {
                    // Now graph has been run and stopped we can get the video width and height from the output pin of the main video decoder
                    VideoInfoHeader2 pvih2 = new VideoInfoHeader2();
                    Marshal.PtrToStructure(pmt.formatPtr, pvih2);
                    SourceFrameSize = new FrameSize(pvih2.BmiHeader.Width, pvih2.BmiHeader.Height);
                }
                else if (pmt.formatType == FormatType.VideoInfo)  //{05589f80-c356-11ce-bf01-00aa0055595a}
                {
                    VideoInfoHeader pvih = new VideoInfoHeader();
                    Marshal.PtrToStructure(pmt.formatPtr, pvih);
                    SourceFrameSize = new FrameSize(pvih.BmiHeader.Width, pvih.BmiHeader.Height);
                }
                else
                    SourceFrameSize = new FrameSize(200, 200); // SQUARE

                // Stop graph if necessary
                FilterState pFS;
                hr = tempControl.GetState(1000, out pFS);
                if (pFS != FilterState.Stopped)
                    DsError.ThrowExceptionForHR(tempControl.Stop());
                // Free up media type
                DsUtils.FreeAMMediaType(pmt); pmt = null;

                // (re)Configure the ASF writer with the selected WM Profile
                ConfigureASFWriter(asf_filter, strq, SourceFrameSize);

                // Release pins
                SendDebugMessage("Releasing COM objects (pins)", 0);
                // source
                Marshal.ReleaseComObject(ASFVidInputPin); ASFVidInputPin = null;
                Marshal.ReleaseComObject(ASFAudInputPin); ASFAudInputPin = null;
            }
            catch (Exception ex)
            {
                SendDebugMessageWithException(ex.Message, ex);
                return DSStreamResultCodes.ErrorExceptionOccurred;
            }

            return DSStreamResultCodes.OK;
        }
        DSStreamResultCodes InitWithStreamBufferFile(WTVStreamingVideoRequest strq)
        {
            // Init variables
            //IPin[] pin = new IPin[1];
            IBaseFilter DecFilterAudio = null;
            IBaseFilter DecFilterVideo = null;
            IBaseFilter MainAudioDecoder = null;
            IBaseFilter MainVideoDecoder = null;
            string dPin = string.Empty;
            string sName = string.Empty;
            string dName = string.Empty;
            string sPin = string.Empty;
            FileInfo fiInputFile = new FileInfo(strq.FileName);
            string txtOutputFNPath = fiInputFile.FullName + ".wmv";
            if (
                (!fiInputFile.Extension.ToLowerInvariant().Equals(".wtv")) &&
                (!fiInputFile.Extension.ToLowerInvariant().Equals(".dvr-ms"))
               ) return DSStreamResultCodes.ErrorInvalidFileType;

            int hr = 0;
            try
            {
                // Get the graphbuilder interface
                SendDebugMessage("Creating Graph Object",0);
                IGraphBuilder graphbuilder = (IGraphBuilder)currentFilterGraph;

                // Add the DVRMS/WTV file / filter to the graph
                SendDebugMessage("Add SBE Source Filter", 0);
                
                hr = graphbuilder.AddSourceFilter(fiInputFile.FullName, "SBE Filter", out currentSBEfilter); // class variable
                DsError.ThrowExceptionForHR(hr);
                dc.Add(currentSBEfilter);

                // Get the SBE audio and video out pins
                IPin SBEVidOutPin, SBEAudOutPin;
                SBEAudOutPin = FilterGraphTools.FindPinByMediaType(currentSBEfilter, PinDirection.Output, MediaType.Audio, MediaSubType.Null);
                SBEVidOutPin = FilterGraphTools.FindPinByMediaType(currentSBEfilter, PinDirection.Output, MediaType.Video, MediaSubType.Null);

                // Set up two decrypt filters according to file extension (assume audio and video both present )
                if (fiInputFile.Extension.ToLowerInvariant().Equals(".dvr-ms"))
                {
                    // Add DVR-MS decrypt filters
                    SendDebugMessage("Add DVRMS (bda) decryption", 0);
                    DecFilterAudio = (IBaseFilter)new DTFilter();  // THESE ARE FOR DVR-MS (BDA DTFilters)
                    DecFilterVideo = (IBaseFilter)new DTFilter();
                    graphbuilder.AddFilter(DecFilterAudio, "Decrypt / Tag");
                    graphbuilder.AddFilter(DecFilterVideo, "Decrypt / Tag 0001");
                }
                else  // Add WTV decrypt filters
                {
                    SendDebugMessage("Add WTV (pbda) decryption", 0);
                    DecFilterAudio = FilterDefinition.AddToFilterGraph(FilterDefinitions.Decrypt.DTFilterPBDA, ref graphbuilder);
                    DecFilterVideo = FilterDefinition.AddToFilterGraph(FilterDefinitions.Decrypt.DTFilterPBDA, ref graphbuilder, "PBDA DTFilter 0001");
                    
                }
                dc.Add(DecFilterAudio);
                dc.Add(DecFilterVideo);

                // Make the first link in the graph: SBE => Decrypts
                SendDebugMessage("Connect SBE => Decrypt filters", 0);
                IPin DecVideoInPin = DsFindPin.ByDirection(DecFilterVideo, PinDirection.Input, 0);
                FilterGraphTools.ConnectFilters(graphbuilder, SBEVidOutPin, DecVideoInPin, false);
                IPin DecAudioInPin = DsFindPin.ByDirection(DecFilterAudio, PinDirection.Input, 0);
                if (DecAudioInPin == null)
                    SendDebugMessage("WARNING: No Audio Input to decrypt filter.");
                else
                    FilterGraphTools.ConnectFilters(graphbuilder, SBEAudOutPin, DecAudioInPin, false);
                
                // Get Dec Audio Out pin
                IPin DecAudioOutPin = DsFindPin.ByDirection(DecFilterAudio, PinDirection.Output, 0);

                // Examine Dec Audio out for audio format
                SendDebugMessage("Examining source audio", 0);
                AMMediaType AudioMediaType = null;
                getPinMediaType(DecAudioOutPin, MediaType.Audio, Guid.Empty, Guid.Empty, ref AudioMediaType);
                SendDebugMessage("Audio media subtype: " + AudioMediaType.subType.ToString());
                SendDebugMessage("Examining Audio StreamInfo");
                StreamInfo si = FileInformation.GetStreamInfo(AudioMediaType);
                bool AudioIsAC3 = (si.SimpleType == "AC-3");
                if (AudioIsAC3)
                    SendDebugMessage("Audio type is AC3");
                else
                    SendDebugMessage("Audio type is not AC3");
                si = null;
                DsUtils.FreeAMMediaType(AudioMediaType);

                // Add an appropriate audio decoder
                if (AudioIsAC3)
                {
                    if (!FilterGraphTools.IsThisComObjectInstalled(FilterDefinitions.Audio.AudioDecoderMPCHC.CLSID))
                    {
                        SendDebugMessage("Missing AC3 Audio Decoder, and AC3 audio detected.");
                        return DSStreamResultCodes.ErrorAC3CodecNotFound;
                    }
                    else
                    {
                        MainAudioDecoder = FilterDefinition.AddToFilterGraph(FilterDefinitions.Audio.AudioDecoderMPCHC, ref graphbuilder);   //MainAudioDecoder = FatAttitude.WTVTranscoder.FilterDefinitions.Audio.AudioDecoderFFDShow.AddToFilterGraph(ref graph);                    
                        Guid tmpGuid; MainAudioDecoder.GetClassID(out tmpGuid);
                        SendDebugMessage("Main Audio decoder CLSID is " + tmpGuid.ToString());
                    }
                }
                else
                    MainAudioDecoder = FilterDefinition.AddToFilterGraph(FilterDefinitions.Audio.AudioDecoderMSDTV, ref graphbuilder);

                // Add a video decoder
                SendDebugMessage("Add DTV decoder", 0);
                MainVideoDecoder = FilterDefinition.AddToFilterGraph(FilterDefinitions.Video.VideoDecoderMSDTV, ref graphbuilder);
                dc.Add(MainAudioDecoder);
                dc.Add(MainVideoDecoder);

                //SetAudioDecoderOutputToPCMStereo(MainAudioDecoder);
                
                // Add a null renderer
                SendDebugMessage("Add null renderer", 0);
                NullRenderer MyNullRenderer = new NullRenderer();
                dc.Add(MyNullRenderer);
                hr = graphbuilder.AddFilter((IBaseFilter)MyNullRenderer, @"Null Renderer");
                DsError.ThrowExceptionForHR(hr);

                // Link up video through to null renderer
                SendDebugMessage("Connect video to null renderer", 0);
                // Make the second link:  Decrypts => DTV
                IPin DecVideoOutPin = DsFindPin.ByDirection(DecFilterVideo, PinDirection.Output, 0);
                IPin DTVVideoInPin = DsFindPin.ByName(MainVideoDecoder, @"Video Input");  // IPin DTVVideoInPin = DsFindPin.ByDirection(DTVVideoDecoder, PinDirection.Input, 0);  // first one should be video input?  //
                FilterGraphTools.ConnectFilters(graphbuilder, DecVideoOutPin, DTVVideoInPin, false);
                // 3. DTV => Null renderer
                IPin NullRInPin = DsFindPin.ByDirection((IBaseFilter)MyNullRenderer, PinDirection.Input, 0);
                IPin DTVVideoOutPin = FilterGraphTools.FindPinByMediaType(MainVideoDecoder, PinDirection.Output, MediaType.Video, MediaSubType.Null);
                FilterGraphTools.ConnectFilters(graphbuilder, DTVVideoOutPin, NullRInPin, false);
                Marshal.ReleaseComObject(NullRInPin); NullRInPin = null;

                // Run graph [can use this also to get media type => see, e.g. dvrmstowmvhd by Babgvant]
                SendDebugMessage("Run graph for testing purposes", 0);
                IMediaControl tempControl = (IMediaControl)graphbuilder;
                IMediaEvent tempEvent = (IMediaEvent)graphbuilder;
                DsError.ThrowExceptionForHR(tempControl.Pause());
                DsError.ThrowExceptionForHR(tempControl.Run());
                EventCode pEventCode;
                hr = tempEvent.WaitForCompletion(1000, out pEventCode);
                //DsError.ThrowExceptionForHR(hr);  // DO *NOT* DO THIS HERE!  THERE MAY WELL BE AN ERROR DUE TO EVENTS RAISED BY THE STREAM BUFFER ENGINE, THIS IS A DELIBERATE TEST RUN OF THE GRAPH
                // Stop graph if necessary
                FilterState pFS;
                hr = tempControl.GetState(1000, out pFS);
                if (pFS == FilterState.Running)
                    DsError.ThrowExceptionForHR(tempControl.Stop());

                // Remove null renderer
                hr = graphbuilder.RemoveFilter((IBaseFilter)MyNullRenderer);

                // Now graph has been run and stopped we can get the video width and height from the output pin of the main video decoder
                AMMediaType pmt = null;
                getPinMediaType(DTVVideoOutPin, MediaType.Video, MediaSubType.YUY2, Guid.Empty, ref pmt);
                FrameSize SourceFrameSize;
                if (pmt.formatType == FormatType.VideoInfo2)
                {
                    VideoInfoHeader2 pvih2 = new VideoInfoHeader2();
                    Marshal.PtrToStructure(pmt.formatPtr, pvih2);
                    int VideoWidth = pvih2.BmiHeader.Width;
                    int VideoHeight = pvih2.BmiHeader.Height;
                    SourceFrameSize = new FrameSize(VideoWidth, VideoHeight);
                }
                else
                    SourceFrameSize = new FrameSize(320, 240);

                // Free up
                DsUtils.FreeAMMediaType(pmt); pmt = null;

                // Link up audio
                // 2. Audio Decrypt -> Audio decoder
                IPin MainAudioInPin = DsFindPin.ByDirection(MainAudioDecoder, PinDirection.Input, 0);
                FilterGraphTools.ConnectFilters(graphbuilder, DecAudioOutPin, MainAudioInPin, false);

                // Add ASF Writer
                // Create an ASF writer filter 
                SendDebugMessage("Creating ASF Writer", 0);
                WMAsfWriter asf_filter = new WMAsfWriter();
                dc.Add(asf_filter); // CHECK FOR ERRORS
                currentOutputFilter = (IBaseFilter)asf_filter;  // class variable
                // Add the ASF filter to the graph
                hr = graphbuilder.AddFilter((IBaseFilter)asf_filter, "WM Asf Writer");
                DsError.ThrowExceptionForHR(hr);

                // Set the filename
                IFileSinkFilter sinkFilter = (IFileSinkFilter)asf_filter;
                string destPathFN = fiInputFile.FullName + ".wmv";
                hr = sinkFilter.SetFileName(destPathFN, null);
                DsError.ThrowExceptionForHR(hr);

                // Make the final links:  DTV => writer 
                SendDebugMessage("Linking audio/video through to decoder and writer", 0);
                IPin DTVAudioOutPin = DsFindPin.ByDirection(MainAudioDecoder, PinDirection.Output, 0);
                IPin ASFAudioInputPin = FilterGraphTools.FindPinByMediaType((IBaseFilter)asf_filter, PinDirection.Input, MediaType.Audio, MediaSubType.Null);
                IPin ASFVideoInputPin = FilterGraphTools.FindPinByMediaType((IBaseFilter)asf_filter, PinDirection.Input, MediaType.Video, MediaSubType.Null);
                FilterGraphTools.ConnectFilters(graphbuilder, DTVAudioOutPin, ASFAudioInputPin, false);
                if (ASFVideoInputPin != null)
                    FilterGraphTools.ConnectFilters(graphbuilder, DTVVideoOutPin, ASFVideoInputPin, false);

                // Configure ASFWriter
                ConfigureASFWriter(asf_filter, strq, SourceFrameSize);

                // Release pins
                SendDebugMessage("Releasing COM objects (pins)", 0);
                    // dec
                Marshal.ReleaseComObject(DecAudioInPin); DecAudioInPin = null;
                Marshal.ReleaseComObject(DecVideoInPin); DecVideoInPin = null;
                Marshal.ReleaseComObject(DecVideoOutPin); DecVideoOutPin = null;
                Marshal.ReleaseComObject(DecAudioOutPin); DecAudioOutPin = null;
                    // dtv
                Marshal.ReleaseComObject(MainAudioInPin); MainAudioInPin = null;
                Marshal.ReleaseComObject(DTVVideoInPin); DTVVideoInPin = null;
                Marshal.ReleaseComObject(DTVVideoOutPin); DTVVideoOutPin = null;
                Marshal.ReleaseComObject(DTVAudioOutPin); DTVAudioOutPin = null;
                    // asf
                Marshal.ReleaseComObject(ASFAudioInputPin); ASFAudioInputPin = null;
                Marshal.ReleaseComObject(ASFVideoInputPin); ASFVideoInputPin = null;
            }
            catch (Exception ex)
            {
                SendDebugMessageWithException(ex.Message, ex);
                return DSStreamResultCodes.ErrorExceptionOccurred;
            }

            return DSStreamResultCodes.OK;
        }
        void ConfigureASFWriter(WMAsfWriter asf_filter, WTVStreamingVideoRequest strq, FrameSize SourceFrameSize)
        {
            int hr;

            // Now it's added to the graph, configure it with the selected WM Profile
            SendDebugMessage("Getting WM profile with quality of " + strq.Quality.ToString(), 0);
            WindowsMediaLib.IWMProfileManager profileManager;
            WMUtils.WMCreateProfileManager(out profileManager);
            IWMProfile wmProfile;
            string txtPrxProfile = getPRXProfileForQuality(strq.Quality);
            if (!(string.IsNullOrEmpty(txtPrxProfile)))
            {
                SendDebugMessage("Adjusting WM profile to fit video within designated frame size", 0);
                // SET VIDEO SIZE TO FIT WITHIN THE RIGHT FRAME
                SendDebugMessage("Source video size is " + SourceFrameSize.ToString(), 0);
                FrameSize containerSize = frameSizeForStreamRequest(strq);
                SendDebugMessage("Container size is " + containerSize.ToString() , 0);
                FrameSize newVideoSize = new FrameSize(SourceFrameSize, containerSize);
                SendDebugMessage("Output size is " + newVideoSize.ToString(), 0);
                SetProfileFrameSize(ref txtPrxProfile, newVideoSize);
                SetProfileCustomSettings(ref txtPrxProfile, ref strq); // returns immediately if not custom quality
                SendDebugMessage("Configuring ASF Writer with profile", 0);
                profileManager.LoadProfileByData(txtPrxProfile, out wmProfile);
                WindowsMediaLib.IConfigAsfWriter configWriter = (WindowsMediaLib.IConfigAsfWriter)asf_filter;
                configWriter.ConfigureFilterUsingProfile(wmProfile);
                configWriter.SetIndexMode(true);  // yes index - DEFAULT
                /* Additional config  - TEST
                //DirectShowLib.IConfigAsfWriter2 configAsfWriter2 = (DirectShowLib.IConfigAsfWriter2)asf_filter;
                //configAsfWriter2.SetParam(ASFWriterConfig.AutoIndex, 0, 0);  // IT IS DEFAULT */







                // (NOT WORKING)
                // SET ANAMORPHIC VIDEO MARKERS WITHIN STREAM (ASPECT RATIO) *******************************
                UInt32 uiAspectX = (UInt32)SourceFrameSize.Width;
                byte[] bAspectX = BitConverter.GetBytes(uiAspectX);
                UInt32 uiAspectY = (UInt32)SourceFrameSize.Height;
                byte[] bAspectY = BitConverter.GetBytes(uiAspectY);

                DirectShowLib.IServiceProvider pServiceProvider;  // http://msdn.microsoft.com/en-us/library/dd390985%28VS.85%29.aspx  
                pServiceProvider = (DirectShowLib.IServiceProvider)asf_filter;
                DsGuid dsgIWMHeaderinfo = DsGuid.FromGuid(new Guid(GUIDs.IWMWriterAdvanced2));
                object o3 = null;
                hr = pServiceProvider.QueryService(dsgIWMHeaderinfo, dsgIWMHeaderinfo, out o3);  // FAILS IN A STA THREAD
                DsError.ThrowExceptionForHR(hr);
                IWMHeaderInfo headerinfo = (IWMHeaderInfo)o3;
                
                // Get access to WMwriterAdvanced2 object using pServiceProvider  (poss not futureproof)  (see http://groups.google.com/group/microsoft.public.win32.programmer.directx.video/browse_thread/thread/36b154d41cb76ffd/c571d6ef56de11af?#c571d6ef56de11af )
                DsGuid dsgWMwriterAdvanced2 = DsGuid.FromGuid(new Guid(GUIDs.IWMWriterAdvanced2));
                object o = null;
                hr = pServiceProvider.QueryService(dsgWMwriterAdvanced2, dsgWMwriterAdvanced2, out o);  // FAILS IN A STA THREAD
                DsError.ThrowExceptionForHR(hr);
                IWMWriterAdvanced2 WMWriterAdvanced2 = null;
                WMWriterAdvanced2 = (IWMWriterAdvanced2)o;

                // Get Access to IWMHeaderInfo3 through WMWriterAdvanced2
                object o2 = null;                
                //pServiceProvider = (DirectShowLib.IServiceProvider)WMWriterAdvanced2;
                DsGuid dsgIWMHeaderInfo3 = DsGuid.FromGuid(new Guid(GUIDs.IWMHeaderInfo3));
                hr = pServiceProvider.QueryService(dsgWMwriterAdvanced2, dsgIWMHeaderInfo3, out o2); // LET'S SEE
                DsError.ThrowExceptionForHR(hr);
                IWMHeaderInfo3 WMHeaderInfo3 = null;
                WMHeaderInfo3 = (IWMHeaderInfo3)o2;
                short pwIndex;
                
                // Add Aspect Ratio information 
                WMHeaderInfo3.AddAttribute(2, "AspectRatioX", out pwIndex, AttrDataType.DWORD, 0, bAspectX, bAspectX.Length);
                WMHeaderInfo3.AddAttribute(2, "AspectRatioY", out pwIndex, AttrDataType.DWORD, 0, bAspectY, bAspectY.Length);
                
                // Try with other interface too
                headerinfo.SetAttribute(2, "AspectRatioX", AttrDataType.DWORD, bAspectX, Convert.ToInt16(bAspectX.Length));
                headerinfo.SetAttribute(2, "AspectRatioY", AttrDataType.DWORD, bAspectY, Convert.ToInt16(bAspectY.Length));











                // ************ DEINTERLACE (experimental)
                if (strq.DeInterlaceMode > 0)
                {
                    DeInterlaceModes dimode = DeInterlaceModes.WM_DM_NOTINTERLACED;
                    // Deinterlace Mode
                    if (strq.DeInterlaceMode == 1)
                        dimode = DeInterlaceModes.WM_DM_DEINTERLACE_NORMAL;
                    else if (strq.DeInterlaceMode == 2)
                        dimode = DeInterlaceModes.WM_DM_DEINTERLACE_HALFSIZE;

                    // Index of video pin
                    int pinIndex = FilterGraphTools.FindPinIndexByMediaType(currentOutputFilter, PinDirection.Input, MediaType.Video, MediaSubType.Null);


                    
                    byte[] bDiMode = BitConverter.GetBytes((int)dimode);
                    short szOf = (short)bDiMode.Length;

                    // Set to use deinterlace mode
                    try
                    {
                        WMWriterAdvanced2.SetInputSetting(pinIndex, g_wszDeinterlaceMode, AttrDataType.DWORD, bDiMode, szOf);
                    }
                    catch (Exception ex)
                    {
                        SendDebugMessageWithException("Could not set interlace mode:", ex);
                    }
                    
                    
                }




            }
            else
            {
                SendDebugMessage("Warning - PRX Profile string was empty; using default WM config.");
            }

        }

        public const string g_wszDeinterlaceMode = "DeinterlaceMode"; //http://read.pudn.com/downloads67/sourcecode/multimedia/streaming/241434/ManWMF/yeti.wmfsdk/WMFFunctions.cs__.htm
        enum DeInterlaceModes
        {
            WM_DM_NOTINTERLACED = 0,
            WM_DM_DEINTERLACE_NORMAL = 1,
            WM_DM_DEINTERLACE_HALFSIZE = 2,
            WM_DM_DEINTERLACE_HALFSIZEDOUBLERATE = 3,
            WM_DM_DEINTERLACE_INVERSETELECINE = 4,
            WM_DM_DEINTERLACE_VERTICALHALFSIZEDOUBLERATE = 5
        }

        // Set the position of the graph to a specified TimeSpan
        protected void SeekGraphToTime(TimeSpan seekTime)
        {
            SendDebugMessage("Seeking graph to time...");
            int hr;
            IMediaControl mc = (IMediaControl)currentFilterGraph;
            // Stop graph if not stopped
            FilterState fs;
            mc.GetState(50, out fs);
            if (fs != FilterState.Stopped)
            {
                if (fs != FilterState.Stopped)
                    mc.Stop();
            }

            long timeInSeconds = (long)seekTime.TotalSeconds;
            DsLong dsTimeIn100NanoSeconds = DsLong.FromInt64(timeInSeconds * 10000000);
            SendDebugMessage("Setting position to " + dsTimeIn100NanoSeconds.ToInt64().ToString());
            long pos;

            if (UsingSBEFilter)
            {
                // IStreamBufferMediaSeeking is used directly on the source filter   http://msdn.microsoft.com/en-us/library/dd694950(v=vs.85).aspx
                IStreamBufferMediaSeeking mSeek = (IStreamBufferMediaSeeking)currentSBEfilter;
                hr = mSeek.SetPositions(dsTimeIn100NanoSeconds, AMSeekingSeekingFlags.AbsolutePositioning, 0, AMSeekingSeekingFlags.NoPositioning);
                DsError.ThrowExceptionForHR(hr);
                mSeek.GetCurrentPosition(out pos);
            }
            else
            {
                // IMediaSeeking is used on the filter graph which distributes the calls
                IMediaSeeking mSeek = (IMediaSeeking)currentFilterGraph;
                hr = mSeek.SetPositions(dsTimeIn100NanoSeconds, AMSeekingSeekingFlags.AbsolutePositioning, 0, AMSeekingSeekingFlags.NoPositioning);
                DsError.ThrowExceptionForHR(hr);                
                mSeek.GetCurrentPosition(out pos);
            }

            SendDebugMessage("New pos is " + pos.ToString());
        }

        #region DirectShow Helpers
        private void SetAudioDecoderOutputToPCMStereo(IBaseFilter audiodecoder)
        {
            // Set audio decoder to output 2 channel PCM  // wm/asf writer doesn't support multi-channel audio
            int hr;
            Guid AVDecCommonOutputFormat = new Guid(0x3c790028, 0xc0ce, 0x4256, 0xb1, 0xa2, 0x1b, 0x0f, 0xc8, 0xb1, 0xdc, 0xdc);
            ICodecAPI audioConfig = audiodecoder as ICodecAPI;
            if (audioConfig != null)
            {
                object pValue = 0;

                hr = audioConfig.GetValue(AVDecCommonOutputFormat, out pValue);
                DsError.ThrowExceptionForHR(hr);

                hr = audioConfig.IsModifiable(AVDecCommonOutputFormat);

                if (hr == 0)
                {
                    Guid AVDecAudioOutputFormat_PCM_Stereo_Auto = new Guid(0x696e1d35, 0x548f, 0x4036, 0x82, 0x5f, 0x70, 0x26, 0xc6, 0x00, 0x11, 0xbd);
                    pValue = AVDecAudioOutputFormat_PCM_Stereo_Auto.ToString("B");
                    hr = audioConfig.SetValue(AVDecCommonOutputFormat, ref pValue);
                    DsError.ThrowExceptionForHR(hr);
                }
            }
        }
        private int getPinMediaType(IPin pPin, Guid majorType, Guid minorType, Guid formatType, ref AMMediaType mediaType)
        {
            if (pPin == null) throw new Exception("No pin");

            int hr;

            IEnumMediaTypes pEnum = null;
            
            hr = pPin.EnumMediaTypes(out pEnum);
            DsError.ThrowExceptionForHR(hr);
            bool found = false;
            IntPtr fetched = IntPtr.Zero;
            AMMediaType[] pMT = new AMMediaType[1];
            hr = pEnum.Next(1, pMT, fetched);
            while (hr == 0)
            {
                if ((majorType.Equals(Guid.Empty)) || (majorType.Equals(pMT[0].majorType)))
                    if ((minorType.Equals(Guid.Empty)) || (minorType.Equals(pMT[0].subType)))
                        if ((formatType.Equals(Guid.Empty)) || (formatType.Equals(pMT[0].formatType)))
                        // Match
                        {
                            found = true;
                            mediaType = pMT[0];
                            break;
                        }

                DsUtils.FreeAMMediaType(pMT[0]);

                hr = pEnum.Next(1, pMT, fetched);
            }

            // End
            Marshal.ReleaseComObject(pEnum);
            if (found)
                return 0;
            else
                return -1;
        }
        #endregion

        #region PRX Profile (XML) Helpers
        private string getPRXProfileForQuality(WTVProfileQuality _quality)
        {
            string resourcePath = "FatAttitude.WTVTranscoder.prx.";
            switch (_quality)
            {
                case WTVProfileQuality.Low:
                    resourcePath += "ultralow";
                    break;
                    
                case WTVProfileQuality.Normal:
                    resourcePath += "low";
                    break;

                case WTVProfileQuality.Med:
                    resourcePath += "normal";
                    break;

                case WTVProfileQuality.High:
                    resourcePath += "high";
                    break;

                case WTVProfileQuality.UltraHigh:
                    resourcePath += "ultrahigh";
                    break;

                case WTVProfileQuality.Test:
                    resourcePath += "test";
                    break;

                case WTVProfileQuality.Custom:
                    resourcePath += "custom";
                    break;

                default:
                    resourcePath += "low";
                    break;
            }
            resourcePath += ".prx";

            try
            {
                Assembly _assembly = Assembly.GetExecutingAssembly();
                StreamReader sr = new StreamReader(_assembly.GetManifestResourceStream(resourcePath));
                return sr.ReadToEnd();
            }
            catch
            {
                SendDebugMessage("Couldnt read profile from DLL embedded resources: " + resourcePath);
                throw new ApplicationException("Couldnt read profile from DLL embedded resources (" + resourcePath + ")");
            }
        }
        FrameSize frameSizeForStreamRequest(WTVStreamingVideoRequest strq)
        {
            switch (strq.Quality)
            {
                case WTVProfileQuality.Low:
                    return new FrameSize(160, 120);

                case WTVProfileQuality.Normal:
                    return new FrameSize(160, 120);

                case WTVProfileQuality.Med:
                    return new FrameSize(320, 240);

                case WTVProfileQuality.High:
                    return new FrameSize(320, 240);

                case WTVProfileQuality.UltraHigh:
                    return new FrameSize(512, 384);

                case WTVProfileQuality.Test:
                    return new FrameSize(320, 240);

                case WTVProfileQuality.Custom:
                    return new FrameSize(strq.CustomFrameWidth, strq.CustomFrameHeight);

                default:
                    return new FrameSize(320, 240);
            }
        }
        /// <summary>
        /// Use XMLDocument to go through a PRX string and change the frame size
        /// </summary>
        /// <param name="txtWMPrf"></param>
        /// <param name="fsize"></param>
        private void SetProfileFrameSize(ref string txtWMPrf, FrameSize fsize)
        {         
            SendDebugMessage("Setting WMProfile Frame size to: " + fsize.Width.ToString() + " x " + fsize.Height.ToString());

            XmlDocument xDoc = new XmlDocument();
            xDoc.LoadXml(txtWMPrf);
            XmlNode root = xDoc.DocumentElement;

            // Configure first located video stream
            foreach (XmlNode rootchild in root.ChildNodes)
            {
                if (rootchild.Name == "streamconfig")
                {
                    if (AttributeEqualsValue(rootchild.Attributes["majortype"], PRX_GuidVideoStream))
                    {
                        XmlNode passChild = rootchild;
                        AddFrameSizesToVideoStreamConfig(ref passChild, fsize);
                        break;
                    }
                }
            }

            // SAVE
            txtWMPrf = xDoc.InnerXml;
        }
        void AddFrameSizesToVideoStreamConfig(ref XmlNode streamconfig, FrameSize newFrameSize)
        {
            bool foo;
            XmlNode xnodewmmediatype;
            XmlNode xnodevidinfoheader;
            XmlNode xnode;
            if (FindChildByName(streamconfig, "wmmediatype", out xnodewmmediatype))
            {
                if (FindChildByName(xnodewmmediatype, "videoinfoheader", out xnodevidinfoheader))
                {
                    if (FindChildByName(xnodevidinfoheader, "rcsource", out xnode))
                    {
                        foo = SetAttributeIfFound(xnode, "right", newFrameSize.Width);
                        foo = SetAttributeIfFound(xnode, "bottom", newFrameSize.Height);
                    }

                    if (FindChildByName(xnodevidinfoheader, "rctarget", out xnode))
                    {
                        foo = SetAttributeIfFound(xnode, "right", newFrameSize.Width);
                        foo = SetAttributeIfFound(xnode, "bottom", newFrameSize.Height);
                    }

                    if (FindChildByName(xnodevidinfoheader, "bitmapinfoheader", out xnode))
                    {
                        foo = SetAttributeIfFound(xnode, "biwidth", newFrameSize.Width);
                        foo = SetAttributeIfFound(xnode, "biheight", newFrameSize.Height);
                    }

                }
            }
        }
        /// <summary>
        /// Use XMLDocument to go through a PRX string and change the video encode settings
        /// </summary>
        /// <param name="txtWMPrf"></param>
        /// <param name="strq"></param>
        private void SetProfileCustomSettings(ref string txtWMPrf, ref WTVStreamingVideoRequest strq)
        {
            if (strq.Quality != WTVProfileQuality.Custom) return;

            SendDebugMessage("Setting WMProfile Custom settings: Video Bitrate:" + strq.CustomVideoBitrate.ToString() + "bps and Smoothness: " + strq.CustomEncoderSmoothness.ToString());

            XmlDocument xDoc = new XmlDocument();
            xDoc.LoadXml(txtWMPrf);
            XmlNode root = xDoc.DocumentElement;

            // Configure first located video stream
            foreach (XmlNode rootchild in root.ChildNodes)
            {
                if (rootchild.Name == "streamconfig")
                {
                    if (AttributeEqualsValue(rootchild.Attributes["majortype"], PRX_GuidVideoStream))
                    {
                        XmlNode passChild = rootchild;
                        AddCustomValuesToVideoStreamConfig(ref passChild, ref strq);
                        break;
                    }
                }
            }

            // SAVE
            txtWMPrf = xDoc.InnerXml;

        }
        void AddCustomValuesToVideoStreamConfig(ref XmlNode streamconfig, ref WTVStreamingVideoRequest strq)
        {
            // Sanity Checks
            if (strq.CustomVideoBitrate > 1500000) strq.CustomVideoBitrate = 1500000;
            if (strq.CustomEncoderSmoothness > 100) strq.CustomEncoderSmoothness = 100;
            if (strq.CustomVideoBitrate < 10000) strq.CustomVideoBitrate = 10000;
            if (strq.CustomEncoderSmoothness < 5) strq.CustomEncoderSmoothness = 5;
            // TODO: More sanity checks
            
            // BITRATE (1 of 2)
            XmlNode xnode;
            bool foo = SetAttributeIfFound(streamconfig, "bitrate", strq.CustomVideoBitrate );
            // QUALITY
            if (FindChildByName(streamconfig, "videomediaprops", out xnode))
                foo = SetAttributeIfFound(xnode, "quality", strq.CustomEncoderSmoothness);
            XmlNode xnodewmmediatype;
            if (FindChildByName(streamconfig, "wmmediatype", out xnodewmmediatype))
            {
                if (FindChildByName(xnodewmmediatype, "videoinfoheader", out xnode))
                {
                    // BITRATE (2 of 2)
                    foo = SetAttributeIfFound(xnode, "dwbitrate", strq.CustomVideoBitrate);

                    // FPS
                    double avgtime = 10000000 / strq.CustomEncoderFPS;
                    avgtime = Math.Round(avgtime, 0);
                    foo = SetAttributeIfFound(xnode, "avgtimeperframe", avgtime);  // TODO: String format
                }
            }

        }

        #region XmlDocHelpers
        string PRX_GuidVideoStream = "{73646976-0000-0010-8000-00AA00389B71}";
        bool AttributeEqualsValue(XmlAttribute xat, string xval)
        {
            if (xat == null) return false;
            return (xat.Value == xval);
        }
        bool SetAttributeIfFound(XmlNode xnode, string atName, object atValue)
        {
            XmlAttribute xat = xnode.Attributes[atName];
            if (xat != null)
            {
                xat.InnerXml = atValue.ToString();
                return true;
            }
            else
                return false;
        }
        bool FindChildByName(XmlNode parent, string elementName, out XmlNode childNode)
        {
            foreach (XmlNode child in parent.ChildNodes)
            {
                if (child.Name == elementName)
                {
                    childNode = child;
                    return true;
                }
            }

            childNode = null;
            return false;
        }
        #endregion
        #endregion

        internal class FrameSize
        {
            int  _width;
            int _height;
            public int Width 
            {
                get
                {
                    return _width;
                }
                set
                {
                    if ((value % 2) != 0)
                        _width = (value + 1);
                    else
                        _width = value;
                }
            }
            public int Height
            {
                get
                {
                    return _height;
                }
                set
                {
                    if ((value % 2) != 0)
                        _height = (value + 1);
                    else
                        _height = value;
                }
            }
            public double AspectRatio
            {
                get
                {
                    return (double)Height / Width;
                }
            }

            public override string ToString()
            {
                return "(" + Width.ToString() + " x " + Height.ToString() + ")";
            }

            #region Constructors
            /// <summary>
            /// Create a frame with supplied width and height values
            /// </summary>
            /// <param name="_width"></param>
            /// <param name="_height"></param>
            public FrameSize(int _width, int _height)
            {
                Width = _width;// rounds to even number
                Height = _height; // rounds to even number
            }
            /// <summary>
            /// Create a frame based on a scaling an existing frame
            /// </summary>
            /// <param name="basedOnSize"></param>
            /// <param name="dMultiplier"></param>
            public FrameSize(FrameSize basedOnSize, double dMultiplier)
            {
                if (dMultiplier == 0) dMultiplier = 0.2;
                if (
                    (basedOnSize.Width == 0) || (basedOnSize.Height == 0)
                    )
                {
                    basedOnSize.Width = 320;
                    basedOnSize.Height = 240;
                }
                Width = Convert.ToInt32(basedOnSize.Width * dMultiplier );
                Height = Convert.ToInt32(basedOnSize.Height * dMultiplier);
            }
            /// <summary>
            /// Fit a frame within a container
            /// </summary>
            /// <param name="sourceSize"></param>
            /// <param name="fitWithinSize"></param>
            public FrameSize(FrameSize sourceSize, FrameSize fitWithinSize)
            {
                if (sourceSize.AspectRatio >= 1)
                {
                    Width = fitWithinSize.Width;
                    Height = Convert.ToInt32(Width * sourceSize.AspectRatio);
                }
                else
                {
                    Height = fitWithinSize.Height;
                    Width = Convert.ToInt32(Height / sourceSize.AspectRatio);
                }
            }
            #endregion
        }

        #region Debug
        protected void SendDebugMessage(DebugMessageEventArgs debugMsgEA)
        {
            if (DebugMessageGenerated != null)
                DebugMessageGenerated(new object(), debugMsgEA);
        }
        protected void SendDebugMessage(string msg)
        {
            if (DebugMessageGenerated != null)
                DebugMessageGenerated(new object(), new DebugMessageEventArgs(msg));
        }
        protected void SendDebugMessage(string msg, int Severity)
        {
            if (DebugMessageGenerated != null)
                DebugMessageGenerated(new object(), new DebugMessageEventArgs(msg));
        }
        protected void SendDebugException(Exception ex)
        {
            if (DebugMessageGenerated != null)
                DebugMessageGenerated(new object(), new DebugMessageEventArgs(ex));
        }
        protected void SendDebugMessageWithException(string msg, Exception ex)
        {
            if (DebugMessageGenerated != null)
                DebugMessageGenerated(new object(), new DebugMessageEventArgs(ex));
        }
        public class DebugMessageEventArgs : EventArgs
        {
            public readonly string DebugMessage;
            public readonly Exception InnerException;
            public readonly bool HasException;
            public readonly int Severity;

            public DebugMessageEventArgs()
            {
                DebugMessage = string.Empty;
                InnerException = null;
                HasException = false;
                Severity = 10;
            }
            public DebugMessageEventArgs(string debugMessage)
                : this()
            {
                DebugMessage = debugMessage;
            }
            public DebugMessageEventArgs(string debugMessage, int severity)
                : this()
            {
                DebugMessage = debugMessage;
                Severity = severity;
            }
            public DebugMessageEventArgs(Exception ex)
                : this()
            {
                InnerException = ex;
                DebugMessage = ex.Message;
                Severity = 20;
            }
            public DebugMessageEventArgs(string msg, Exception ex)
                : this()
            {
                InnerException = ex;
                DebugMessage = msg;
                Severity = 20;
            }
        }
        #endregion

        #region Event Args
        /// <summary>Event arguments for the ProcessChanged event.</summary>
        public class ProgressChangedEventArgs : EventArgs
        {
            /// <summary>Percentage of the conversion currently completed.</summary>
            public readonly double ProgressPercentage;
            /// <summary>Initialize the event args.</summary>
            /// <param name="percentage">Percentage of the conversion currently completed.</param>
            public ProgressChangedEventArgs(double percentage)
            {
                ProgressPercentage = percentage;
            }
        }
        public class ErrorEventArgs : EventArgs
        {
            public readonly string ErrorMessage;
            public ErrorEventArgs(string msg)
            {
                ErrorMessage = msg;
            }
            public ErrorEventArgs(Exception e)
            {
                ErrorMessage = e.Message;
            }
        }
        public class ConversionEndedEventArgs : EventArgs
        {
            public readonly string Message;
            public bool WasError;
            public ConversionEndedEventArgs(bool _wasError, string msg)
            {
                Message = msg;
                WasError = _wasError;
            }
        }
        #endregion
    }
}
