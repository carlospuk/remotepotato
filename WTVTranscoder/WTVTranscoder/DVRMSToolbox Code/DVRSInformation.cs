#region license
/*
DvrmsToolbox - Perform actions on Windows Media Center files automatically
Copyright (C) 2009 andy vt
http://babvant.com

This license governs use of the accompanying software. If you use the software, you accept this license. If you do not accept the license, do not use the software.

1. Definitions
The terms "reproduce," "reproduction," "derivative works," and "distribution" have the same meaning here as under U.S. copyright law.
A "contribution" is the original software, or any additions or changes to the software.
A "contributor" is any person that distributes its contribution under this license.
"Licensed patents" are a contributor's patent claims that read directly on its contribution.

2. Grant of Rights
(A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to reproduce its contribution, prepare derivative works of its contribution, and distribute its contribution or any derivative works that you create.
(B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed patents to make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its contribution in the software or derivative works of the contribution in the software.

3. Conditions and Limitations
(A) Reciprocal Grants- For any file you distribute that contains code from the software (in source code or binary format), you must provide recipients the source code to that file along with a copy of this license, which license will govern that file. Code that links to or derives from the software must be released under an OSI-certified open source license.
(B) No Trademark License- This license does not grant you rights to use any contributors' name, logo, or trademarks.
(C) If you bring a patent claim against any contributor over patents that you claim are infringed by the software, your patent license from such contributor to the software ends automatically.
(D) If you distribute any portion of the software, you must retain all copyright, patent, trademark, and attribution notices that are present in the software.
(E) If you distribute any portion of the software in source code form, you may do so only under this license by including a complete copy of this license with your distribution. If you distribute any portion of the software in compiled or object code form, you may only do so under a license that complies with this license.
(F) The software is licensed "as-is." You bear the risk of using it. The contributors give no express warranties, guarantees or conditions. You may have additional consumer rights under your local laws which this license cannot change. To the extent permitted under your local laws, the contributors exclude the implied warranties of merchantability, fitness for a particular purpose and non-infringement.
*/

#endregion

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Drawing;
using DirectShowLib;
using DirectShowLib.SBE;

namespace FatAttitude.WTVTranscoder
{

    public class StreamInfo
    {
        private Guid _mediaType;
        private Guid _subType;
        private VideoInfoHeader2 _vih;
        private WaveFormatEx _aih;
        //private List<MetadataItem> _oinfo = new List<MetadataItem>();
        private string _simpleType;

        public Guid MediaType
        {
            get
            {
                return _mediaType;
            }
            set
            {
                _mediaType = value;
            }
        }

        public Guid SubType
        {
            get
            {
                return _subType;
            }
            set
            {
                _subType = value;
            }
        }

        public VideoInfoHeader2 VideoInfo
        {
            get
            {
                return _vih;
            }
            set
            {
                _vih = value;
            }
        }

        public WaveFormatEx AudioInfo
        {
            get
            {
                return _aih;
            }
            set
            {
                _aih = value;
            }
        }

        public string SimpleType
        {
            get
            {
                return _simpleType;
            }
            set
            {
                _simpleType = value;
            }
        }

        /*
        public List<MetadataItem> OtherInfo
        {
            get
            {
                return _oinfo;
            }
        }
         */
    }

    public class FileInformation
    {
        private Dictionary<int, StreamInfo> _mediaTypes;

        public Dictionary<int, StreamInfo> MediaTypes
        {
            get
            {
                return _mediaTypes;
            }
        }

        private FileInformation()
        {
            _mediaTypes = new Dictionary<int, StreamInfo>();
        }

        public static StreamInfo GetStreamInfo(AMMediaType cMt)
        {
            StreamInfo si = new StreamInfo();
            si.MediaType = cMt.majorType;
            si.SubType = cMt.subType;

            if (cMt.majorType == MediaType.Video)
            {
                if (cMt.formatType == FormatType.VideoInfo2)
                {
                    si.VideoInfo = (VideoInfoHeader2)Marshal.PtrToStructure(cMt.formatPtr, typeof(VideoInfoHeader2));
                }
                else if (cMt.formatType == FormatType.VideoInfo)
                {
                    VideoInfoHeader2 vih = (VideoInfoHeader2)Marshal.PtrToStructure(cMt.formatPtr, typeof(VideoInfoHeader));
                    si.VideoInfo = vih;
                }
                else if (cMt.formatType == FormatType.Mpeg2Video)
                {
                    si.VideoInfo = (VideoInfoHeader2)Marshal.PtrToStructure(cMt.formatPtr, typeof(VideoInfoHeader2));
                }
                si.SimpleType = FromFourCC(si.VideoInfo.BmiHeader.Compression);
            }
            else if (cMt.majorType == MediaType.Audio)
            {
                if (cMt.formatType == FormatType.WaveEx)
                {
                    si.AudioInfo = (WaveFormatEx)Marshal.PtrToStructure(cMt.formatPtr, typeof(WaveFormatEx));
                }

                if (cMt.subType == MediaSubType.DolbyAC3 || cMt.subType == MediaSubType.DOLBY_AC3_SPDIF)
                    si.SimpleType = "AC-3";
                else if (cMt.subType == MediaSubType.MPEG1AudioPayload || cMt.subType == MediaSubType.MPEG1Audio || cMt.subType == MediaSubType.Mpeg2Audio)
                    si.SimpleType = "MPEG Audio";
                //else if (cMt.subType == MediaSubType.DTS_Audio || cMt.subType == MediaSubType.MKV_DTS_Audio)
                //    si.SimpleType = "DTS";
                else if (si.AudioInfo != null)
                {
                    WaveFormat wf = (WaveFormat)si.AudioInfo.wFormatTag;  // an enum (see bottom of doc)
                    si.SimpleType = wf.ToString();
                }
            }
            else if (cMt.majorType == MediaType.MSTVCaption || cMt.majorType == MediaType.AuxLine21Data)
            {
                si.MediaType = MediaType.MSTVCaption;
            }
            return si;
        }

        public static VideoInfoHeader2 GetSBEFrameSize(string pathToFile)
        {
            int hr = 0;
            IGraphBuilder graph = null;
            IBaseFilter capFilter = null;
            IBaseFilter nRender = null;

            try
            {
                graph = (IGraphBuilder)new FilterGraph();

                hr = graph.AddSourceFilter(pathToFile, "Source", out capFilter);
                DsError.ThrowExceptionForHR(hr);

#if DEBUG
                using (DsROTEntry rot = new DsROTEntry(graph))
                {
#endif

                    IPin vPin = null;
                    IBaseFilter dec = null;
                    IPin sgIn = null;
                    IBaseFilter mpegDec = null;

                    try
                    {
                        dec = (IBaseFilter)new DTFilter();

                        hr = graph.AddFilter(dec, "Decrypt");
                        DsError.ThrowExceptionForHR(hr);

                        nRender = (IBaseFilter)new NullRenderer();

                        hr = graph.AddFilter((IBaseFilter)nRender, "Video Null Renderer");
                        DsError.ThrowExceptionForHR(hr);


                        IBaseFilter dec1 = FilterDefinition.AddToFilterGraph(FatAttitude.WTVTranscoder.FilterDefinitions.Decrypt.DTFilterPBDA, ref graph, "Decrypt1");
                        if (dec1 != null)
                            Marshal.ReleaseComObject(dec1);
                        dec1 = null;

                        mpegDec = FilterDefinition.AddToFilterGraph(FatAttitude.WTVTranscoder.FilterDefinitions.Video.VideoDecoderMpeg, ref graph, "MS MPEG Decoder");

                        sgIn = DsFindPin.ByDirection(mpegDec, PinDirection.Input, 0);

                        IEnumPins ppEnum;
                        IPin[] pPins = new IPin[1];

                        hr = capFilter.EnumPins(out ppEnum);
                        DsError.ThrowExceptionForHR(hr);

                        try
                        {
                            while (ppEnum.Next(1, pPins, IntPtr.Zero) == 0)
                            {
                                IEnumMediaTypes emtDvr = null;
                                AMMediaType[] amtDvr = new AMMediaType[1];

                                try
                                {
                                    pPins[0].EnumMediaTypes(out emtDvr);

                                    hr = emtDvr.Next(1, amtDvr, IntPtr.Zero);
                                    DsError.ThrowExceptionForHR(hr);

                                    if (amtDvr[0].majorType == MediaType.Video)
                                    {
                                        if (graph.Connect(pPins[0], sgIn) >= 0)
                                        {
                                            vPin = pPins[0];
                                            break;
                                        }
                                    }
                                    if (pPins[0] != null)
                                        Marshal.ReleaseComObject(pPins[0]);
                                }
                                finally
                                {
                                    if (emtDvr != null)
                                        Marshal.ReleaseComObject(emtDvr);
                                    DsUtils.FreeAMMediaType(amtDvr[0]);
                                }
                            }
                        }
                        finally
                        {
                            if (ppEnum != null)
                                Marshal.ReleaseComObject(ppEnum);
                        }

                        FilterGraphTools.RenderPin(graph, mpegDec, "Video Output 1");
                    }
                    finally
                    {
                        if (vPin != null)
                            Marshal.ReleaseComObject(vPin);

                        if (dec != null)
                            Marshal.ReleaseComObject(dec);

                        if (sgIn != null)
                            Marshal.ReleaseComObject(sgIn);

                        if (mpegDec != null)
                            Marshal.ReleaseComObject(mpegDec);
                    }

                    EventCode ec;

                    IMediaControl mControl = graph as IMediaControl;
                    IMediaEvent mEvent = graph as IMediaEvent;

                    hr = mControl.Pause();
                    DsError.ThrowExceptionForHR(hr);

                    hr = mControl.Run();
                    DsError.ThrowExceptionForHR(hr);

                    hr = mEvent.WaitForCompletion(1000, out ec);
                    //DsError.ThrowExceptionForHR(hr);

                    hr = mControl.Pause();
                    DsError.ThrowExceptionForHR(hr);

                    hr = mControl.Stop();
                    DsError.ThrowExceptionForHR(hr);

                    IPin mpgOut = null;
                    sgIn = null;
                    AMMediaType mt = new AMMediaType();

                    try
                    {
                        sgIn = DsFindPin.ByDirection(nRender, PinDirection.Input, 0);

                        if (sgIn != null)
                        {
                            hr = sgIn.ConnectedTo(out mpgOut);
                            DsError.ThrowExceptionForHR(hr);

                            hr = graph.RemoveFilter(nRender);
                            DsError.ThrowExceptionForHR(hr);

                            Marshal.ReleaseComObject(nRender);
                            nRender = null;

                            nRender = (IBaseFilter)new NullRenderer();
                            hr = graph.AddFilter((IBaseFilter)nRender, "Video Null Renderer");
                            DsError.ThrowExceptionForHR(hr);

                            hr = graph.Render(mpgOut);
                            DsError.ThrowExceptionForHR(hr);

                            hr = mpgOut.ConnectionMediaType(mt);
                            DsError.ThrowExceptionForHR(hr);

                            if (mt.formatType == FormatType.VideoInfo2)
                            {
                                VideoInfoHeader2 vih = (VideoInfoHeader2)Marshal.PtrToStructure(mt.formatPtr, typeof(VideoInfoHeader2));
                                return vih;
                            }
                        }
                    }
                    finally
                    {
                        DsUtils.FreeAMMediaType(mt);

                        if (mpgOut != null)
                            Marshal.ReleaseComObject(mpgOut);
                        if (sgIn != null)
                            Marshal.ReleaseComObject(sgIn);
                    }
#if DEBUG
                }
#endif
            }
            finally
            {
                if (nRender != null)
                    Marshal.ReleaseComObject(nRender);
                if (capFilter != null)
                    Marshal.ReleaseComObject(capFilter);
                if (graph != null)
                    while (Marshal.ReleaseComObject(graph) > 0) ;
            }
            return null;
        }

        private static IPin FindPinByMediaType(IBaseFilter filter, PinDirection direction, Guid mType, Guid sType)
        {
            IPin pRet = null;
            IPin tPin = null;
            int hr;
            int index = 0;

            tPin = DsFindPin.ByDirection(filter, direction, index);
            while (tPin != null)
            {

                IEnumMediaTypes emtDvr = null;
                AMMediaType[] amtDvr = new AMMediaType[1];

                try
                {
                    tPin.EnumMediaTypes(out emtDvr);

                    hr = emtDvr.Next(1, amtDvr, IntPtr.Zero);
                    DsError.ThrowExceptionForHR(hr);

                    if (amtDvr[0] != null && amtDvr[0].majorType == mType && (amtDvr[0].subType == sType || sType == MediaSubType.Null))
                    {
                        pRet = tPin;
                        break;
                    }
                }
                finally
                {
                    DsUtils.FreeAMMediaType(amtDvr[0]);
                    if (emtDvr != null)
                        Marshal.ReleaseComObject(emtDvr);
                }

                if (tPin != null)
                    Marshal.ReleaseComObject(tPin);
                tPin = null;
                index++;
                tPin = DsFindPin.ByDirection(filter, direction, index);
            }

            return pRet;
        }

        private static ISampleGrabber AddRGBSampleGrabber(IGraphBuilder graph, string filterName)
        {
            return AddSampleGrabber(graph, filterName, MediaType.Video, MediaSubType.RGB32);
        }

        private static ISampleGrabber AddSampleGrabber(IGraphBuilder graph, string filterName, Guid majorType, Guid minorType)
        {
            ISampleGrabber isg = (ISampleGrabber)new SampleGrabber();

            int hr = graph.AddFilter((IBaseFilter)isg, filterName);
            DsError.ThrowExceptionForHR(hr);

            AMMediaType mt = new AMMediaType();
            mt.majorType = majorType;
            mt.subType = minorType;

            hr = isg.SetMediaType(mt);
            DsError.ThrowExceptionForHR(hr);

            hr = isg.SetBufferSamples(true);
            DsError.ThrowExceptionForHR(hr);

            hr = isg.SetOneShot(true);
            DsError.ThrowExceptionForHR(hr);

            return isg;
        }

        private static Bitmap GetBitmap(IGraphBuilder graph, ISampleGrabber sg, long grabPosition, out EventCode ec)
        {
            IntPtr pBuffer = IntPtr.Zero;
            int pBufferSize = 0;
            Bitmap b = null;
            int hr = 0;

            try
            {
                IMediaSeeking ims = graph as IMediaSeeking;

                bool canDuration = false;
                bool canPos = false;
                bool canSeek = false;
                long pDuration = 0;
                long pCurrent = 0;

                if (ims != null)
                {
                    AMSeekingSeekingCapabilities caps;

                    hr = ims.GetCapabilities(out caps);
                    if ((caps & AMSeekingSeekingCapabilities.CanGetDuration) == AMSeekingSeekingCapabilities.CanGetDuration)
                        canDuration = true;
                    if ((caps & AMSeekingSeekingCapabilities.CanGetCurrentPos) == AMSeekingSeekingCapabilities.CanGetCurrentPos)
                        canPos = true;
                    if ((caps & AMSeekingSeekingCapabilities.CanSeekAbsolute) == AMSeekingSeekingCapabilities.CanSeekAbsolute)
                        canSeek = true;

                    if (canDuration)
                        hr = ims.GetDuration(out pDuration);

                    if (grabPosition > pDuration)
                        grabPosition = pDuration - 1;

                    if (canSeek)
                    {
                        hr = ims.SetPositions(new DsLong(grabPosition), AMSeekingSeekingFlags.AbsolutePositioning, 0, AMSeekingSeekingFlags.NoPositioning);
                        DsError.ThrowExceptionForHR(hr);
                    }

                    if (canPos)
                        hr = ims.GetCurrentPosition(out pCurrent);
                }

                if (canPos)
                    hr = ims.GetCurrentPosition(out pCurrent);

                IMediaControl mControl = graph as IMediaControl;
                IMediaEvent mEvent = graph as IMediaEvent;

                //ec = EventCode.SystemBase;

                hr = mControl.Pause();
                DsError.ThrowExceptionForHR(hr);

                hr = mControl.Run();
                DsError.ThrowExceptionForHR(hr);

                hr = mEvent.WaitForCompletion(int.MaxValue, out ec);
                DsError.ThrowExceptionForHR(hr);

                hr = mControl.Pause();
                DsError.ThrowExceptionForHR(hr);

                hr = mControl.Stop();
                DsError.ThrowExceptionForHR(hr);

                if (ec != EventCode.Complete)
                    return null;

                hr = sg.GetCurrentBuffer(ref pBufferSize, pBuffer);
                DsError.ThrowExceptionForHR(hr);

                pBuffer = Marshal.AllocCoTaskMem(pBufferSize);

                hr = sg.GetCurrentBuffer(ref pBufferSize, pBuffer);
                DsError.ThrowExceptionForHR(hr);

                if (pBuffer != IntPtr.Zero)
                {
                    AMMediaType sgMt = new AMMediaType();
                    int videoWidth = 0;
                    int videoHeight = 0;
                    int stride = 0;

                    try
                    {
                        hr = sg.GetConnectedMediaType(sgMt);
                        DsError.ThrowExceptionForHR(hr);

                        if (sgMt.formatPtr != IntPtr.Zero)
                        {
                            if (sgMt.formatType == FormatType.VideoInfo)
                            {
                                VideoInfoHeader vih = (VideoInfoHeader)Marshal.PtrToStructure(sgMt.formatPtr, typeof(VideoInfoHeader));
                                videoWidth = vih.BmiHeader.Width;
                                videoHeight = vih.BmiHeader.Height;
                                stride = videoWidth * (vih.BmiHeader.BitCount / 8);
                            }
                            else
                                throw new ApplicationException("Unsupported Sample");

                            b = new Bitmap(videoWidth, videoHeight, stride, System.Drawing.Imaging.PixelFormat.Format32bppRgb, pBuffer);
                            b.RotateFlip(RotateFlipType.RotateNoneFlipY);
                        }
                    }
                    finally
                    {
                        DsUtils.FreeAMMediaType(sgMt);
                    }
                }

                return b;
            }
            finally
            {
                if (pBuffer != IntPtr.Zero)
                    Marshal.FreeCoTaskMem(pBuffer);
            }
        }

        #region FourCC conversion methods

        public static string FromFourCC(Guid SubType)
        {
            return FromFourCC(Convert.ToInt32(SubType.ToString().Substring(0, 8), 16));
        }

        public static string FromFourCC(int FourCC)
        {
            char[] chars = new char[4];
            chars[0] = (char)(FourCC & 0xFF);
            chars[1] = (char)((FourCC >> 8) & 0xFF);
            chars[2] = (char)((FourCC >> 16) & 0xFF);
            chars[3] = (char)((FourCC >> 24) & 0xFF);

            return new string(chars);
        }

        public static int ToFourCC(string FourCC)
        {
            if (FourCC.Length != 4)
            {
                throw new Exception("FourCC strings must be 4 characters long " + FourCC);
            }

            int result = ((int)FourCC[3]) << 24
                        | ((int)FourCC[2]) << 16
                        | ((int)FourCC[1]) << 8
                        | ((int)FourCC[0]);

            return result;
        }

        public static int ToFourCC(char[] FourCC)
        {
            if (FourCC.Length != 4)
            {
                throw new Exception("FourCC char arrays must be 4 characters long " + new string(FourCC));
            }

            int result = ((int)FourCC[3]) << 24
                        | ((int)FourCC[2]) << 16
                        | ((int)FourCC[1]) << 8
                        | ((int)FourCC[0]);

            return result;
        }

        public static int ToFourCC(char c0, char c1, char c2, char c3)
        {
            int result = ((int)c3) << 24
                        | ((int)c2) << 16
                        | ((int)c1) << 8
                        | ((int)c0);

            return result;
        }
        #endregion

        // Wave format

        enum WaveFormat
        {
            UNKNOWN = 0x0000, /* Microsoft Corporation */
            ADPCM = 0x0002, /* Microsoft Corporation */
            IEEE_FLOAT = 0x0003, /* Microsoft Corporation */
            VSELP = 0x0004, /* Compaq Computer Corp. */
            IBM_CVSD = 0x0005, /* IBM Corporation */
            ALAW = 0x0006, /* Microsoft Corporation */
            MULAW = 0x0007, /* Microsoft Corporation */
            DTS = 0x0008, /* Microsoft Corporation */
            DRM = 0x0009, /* Microsoft Corporation */
            WMAVOICE9 = 0x000A, /* Microsoft Corporation */
            WMAVOICE10 = 0x000B, /* Microsoft Corporation */
            OKI_ADPCM = 0x0010, /* OKI */
            DVI_ADPCM = 0x0011, /* Intel Corporation */
            IMA_ADPCM = (DVI_ADPCM), /*  Intel Corporation */
            MEDIASPACE_ADPCM = 0x0012, /* Videologic */
            SIERRA_ADPCM = 0x0013, /* Sierra Semiconductor Corp */
            G723_ADPCM = 0x0014, /* Antex Electronics Corporation */
            DIGISTD = 0x0015, /* DSP Solutions, Inc. */
            DIGIFIX = 0x0016, /* DSP Solutions, Inc. */
            DIALOGIC_OKI_ADPCM = 0x0017, /* Dialogic Corporation */
            MEDIAVISION_ADPCM = 0x0018, /* Media Vision, Inc. */
            CU_CODEC = 0x0019, /* Hewlett-Packard Company */
            YAMAHA_ADPCM = 0x0020, /* Yamaha Corporation of America */
            SONARC = 0x0021, /* Speech Compression */
            DSPGROUP_TRUESPEECH = 0x0022, /* DSP Group, Inc */
            ECHOSC1 = 0x0023, /* Echo Speech Corporation */
            AUDIOFILE_AF36 = 0x0024, /* Virtual Music, Inc. */
            APTX = 0x0025, /* Audio Processing Technology */
            AUDIOFILE_AF10 = 0x0026, /* Virtual Music, Inc. */
            PROSODY_1612 = 0x0027, /* Aculab plc */
            LRC = 0x0028, /* Merging Technologies S.A. */
            DOLBY_AC2 = 0x0030, /* Dolby Laboratories */
            GSM610 = 0x0031, /* Microsoft Corporation */
            MSNAUDIO = 0x0032, /* Microsoft Corporation */
            ANTEX_ADPCME = 0x0033, /* Antex Electronics Corporation */
            CONTROL_RES_VQLPC = 0x0034, /* Control Resources Limited */
            DIGIREAL = 0x0035, /* DSP Solutions, Inc. */
            DIGIADPCM = 0x0036, /* DSP Solutions, Inc. */
            CONTROL_RES_CR10 = 0x0037, /* Control Resources Limited */
            NMS_VBXADPCM = 0x0038, /* Natural MicroSystems */
            CS_IMAADPCM = 0x0039, /* Crystal Semiconductor IMA ADPCM */
            ECHOSC3 = 0x003A, /* Echo Speech Corporation */
            ROCKWELL_ADPCM = 0x003B, /* Rockwell International */
            ROCKWELL_DIGITALK = 0x003C, /* Rockwell International */
            XEBEC = 0x003D, /* Xebec Multimedia Solutions Limited */
            G721_ADPCM = 0x0040, /* Antex Electronics Corporation */
            G728_CELP = 0x0041, /* Antex Electronics Corporation */
            MSG723 = 0x0042, /* Microsoft Corporation */
            MPEG = 0x0050, /* Microsoft Corporation */
            RT24 = 0x0052, /* InSoft, Inc. */
            PAC = 0x0053, /* InSoft, Inc. */
            MPEGLAYER3 = 0x0055, /* ISO/MPEG Layer3 Format Tag */
            LUCENT_G723 = 0x0059, /* Lucent Technologies */
            CIRRUS = 0x0060, /* Cirrus Logic */
            ESPCM = 0x0061, /* ESS Technology */
            VOXWARE = 0x0062, /* Voxware Inc */
            CANOPUS_ATRAC = 0x0063, /* Canopus, co., Ltd. */
            G726_ADPCM = 0x0064, /* APICOM */
            G722_ADPCM = 0x0065, /* APICOM */
            DSAT_DISPLAY = 0x0067, /* Microsoft Corporation */
            VOXWARE_BYTE_ALIGNED = 0x0069, /* Voxware Inc */
            VOXWARE_AC8 = 0x0070, /* Voxware Inc */
            VOXWARE_AC10 = 0x0071, /* Voxware Inc */
            VOXWARE_AC16 = 0x0072, /* Voxware Inc */
            VOXWARE_AC20 = 0x0073, /* Voxware Inc */
            VOXWARE_RT24 = 0x0074, /* Voxware Inc */
            VOXWARE_RT29 = 0x0075, /* Voxware Inc */
            VOXWARE_RT29HW = 0x0076, /* Voxware Inc */
            VOXWARE_VR12 = 0x0077, /* Voxware Inc */
            VOXWARE_VR18 = 0x0078, /* Voxware Inc */
            VOXWARE_TQ40 = 0x0079, /* Voxware Inc */
            SOFTSOUND = 0x0080, /* Softsound, Ltd. */
            VOXWARE_TQ60 = 0x0081, /* Voxware Inc */
            MSRT24 = 0x0082, /* Microsoft Corporation */
            G729A = 0x0083, /* AT&T Labs, Inc. */
            MVI_MVI2 = 0x0084, /* Motion Pixels */
            DF_G726 = 0x0085, /* DataFusion Systems (Pty) (Ltd) */
            DF_GSM610 = 0x0086, /* DataFusion Systems (Pty) (Ltd) */
            ISIAUDIO = 0x0088, /* Iterated Systems, Inc. */
            ONLIVE = 0x0089, /* OnLive! Technologies, Inc. */
            SBC24 = 0x0091, /* Siemens Business Communications Sys */
            DOLBY_AC3_SPDIF = 0x0092, /* Sonic Foundry */
            MEDIASONIC_G723 = 0x0093, /* MediaSonic */
            PROSODY_8KBPS = 0x0094, /* Aculab plc */
            ZYXEL_ADPCM = 0x0097, /* ZyXEL Communications, Inc. */
            PHILIPS_LPCBB = 0x0098, /* Philips Speech Processing */
            PACKED = 0x0099, /* Studer Professional Audio AG */
            MALDEN_PHONYTALK = 0x00A0, /* Malden Electronics Ltd. */
            RHETOREX_ADPCM = 0x0100, /* Rhetorex Inc. */
            IRAT = 0x0101, /* BeCubed Software Inc. */
            VIVO_G723 = 0x0111, /* Vivo Software */
            VIVO_SIREN = 0x0112, /* Vivo Software */
            DIGITAL_G723 = 0x0123, /* Digital Equipment Corporation */
            SANYO_LD_ADPCM = 0x0125, /* Sanyo Electric Co., Ltd. */
            SIPROLAB_ACEPLNET = 0x0130, /* Sipro Lab Telecom Inc. */
            SIPROLAB_ACELP4800 = 0x0131, /* Sipro Lab Telecom Inc. */
            SIPROLAB_ACELP8V3 = 0x0132, /* Sipro Lab Telecom Inc. */
            SIPROLAB_G729 = 0x0133, /* Sipro Lab Telecom Inc. */
            SIPROLAB_G729A = 0x0134, /* Sipro Lab Telecom Inc. */
            SIPROLAB_KELVIN = 0x0135, /* Sipro Lab Telecom Inc. */
            G726ADPCM = 0x0140, /* Dictaphone Corporation */
            QUALCOMM_PUREVOICE = 0x0150, /* Qualcomm, Inc. */
            QUALCOMM_HALFRATE = 0x0151, /* Qualcomm, Inc. */
            TUBGSM = 0x0155, /* Ring Zero Systems, Inc. */
            MSAUDIO1 = 0x0160, /* Microsoft Corporation */
            WMAUDIO2 = 0x0161, /* Microsoft Corporation */
            WMAUDIO3 = 0x0162, /* Microsoft Corporation */
            WMAUDIO_LOSSLESS = 0x0163, /* Microsoft Corporation */
            WMASPDIF = 0x0164, /* Microsoft Corporation */
            UNISYS_NAP_ADPCM = 0x0170, /* Unisys Corp. */
            UNISYS_NAP_ULAW = 0x0171, /* Unisys Corp. */
            UNISYS_NAP_ALAW = 0x0172, /* Unisys Corp. */
            UNISYS_NAP_16K = 0x0173, /* Unisys Corp. */
            CREATIVE_ADPCM = 0x0200, /* Creative Labs, Inc */
            CREATIVE_FASTSPEECH8 = 0x0202, /* Creative Labs, Inc */
            CREATIVE_FASTSPEECH10 = 0x0203, /* Creative Labs, Inc */
            UHER_ADPCM = 0x0210, /* UHER informatic GmbH */
            QUARTERDECK = 0x0220, /* Quarterdeck Corporation */
            ILINK_VC = 0x0230, /* I-link Worldwide */
            RAW_SPORT = 0x0240, /* Aureal Semiconductor */
            ESST_AC3 = 0x0241, /* ESS Technology, Inc. */
            GENERIC_PASSTHRU = 0x0249,
            IPI_HSX = 0x0250, /* Interactive Products, Inc. */
            IPI_RPELP = 0x0251, /* Interactive Products, Inc. */
            CS2 = 0x0260, /* Consistent Software */
            SONY_SCX = 0x0270, /* Sony Corp. */
            FM_TOWNS_SND = 0x0300, /* Fujitsu Corp. */
            BTV_DIGITAL = 0x0400, /* Brooktree Corporation */
            QDESIGN_MUSIC = 0x0450, /* QDesign Corporation */
            VME_VMPCM = 0x0680, /* AT&T Labs, Inc. */
            TPC = 0x0681, /* AT&T Labs, Inc. */
            OLIGSM = 0x1000, /* Ing C. Olivetti & C., S.p.A. */
            OLIADPCM = 0x1001, /* Ing C. Olivetti & C., S.p.A. */
            OLICELP = 0x1002, /* Ing C. Olivetti & C., S.p.A. */
            OLISBC = 0x1003, /* Ing C. Olivetti & C., S.p.A. */
            OLIOPR = 0x1004, /* Ing C. Olivetti & C., S.p.A. */
            LH_CODEC = 0x1100, /* Lernout & Hauspie */
            NORRIS = 0x1400, /* Norris Communications, Inc. */
            SOUNDSPACE_MUSICOMPRESS = 0x1500, /* AT&T Labs, Inc. */
            MPEG_ADTS_AAC = 0x1600, /* Microsoft Corporation */
            MPEG_RAW_AAC = 0x1601, /* Microsoft Corporation */
            NOKIA_MPEG_ADTS_AAC = 0x1608, /* Microsoft Corporation */
            NOKIA_MPEG_RAW_AAC = 0x1609, /* Microsoft Corporation */
            VODAFONE_MPEG_ADTS_AAC = 0x160A, /* Microsoft Corporation */
            VODAFONE_MPEG_RAW_AAC = 0x160B, /* Microsoft Corporation */
            DVM = 0x2000 /* FAST Multimedia AG */
        }

    }
}
