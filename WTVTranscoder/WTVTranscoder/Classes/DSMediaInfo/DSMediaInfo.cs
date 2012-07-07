using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using DirectShowLib;
using DirectShowLib.DES;

namespace FatAttitude
{
    /// <summary>
    /// The MediaDetector class allows to query meta data from audio/video media files.
    /// This includes CODEC information and the ability to grab video snapshots.
    /// </summary>
    public class DSMediaInfo : IDisposable
    {
        #region Locals
        private bool disposed = false;
        private IMediaDet m_mediaDet;
        private int m_audioBitsPerSample;
        private int m_audioChannels;
        private int m_audioSamplesPerSecond;
        private double m_audioStreamLength;
        private Guid m_audioSubType;
        private Bitmap m_bitmap;
        private string m_filename;
        private string m_fourCC;
        private int m_streamCount;
        private int m_videoBitsPerPixel;
        private Size m_videoResolution;
        private double m_videoStreamLength;
        private Guid m_videoSubType;
        #endregion

        #region IDisposable
        //Implement IDisposable.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Free other state (managed objects).
                }

                FreeResources();

                // Set large fields to null.
                disposed = true;
            }
        }

        // Use C# destructor syntax for finalization code.
        ~DSMediaInfo()
        {
            // Simply call Dispose(false).
            Dispose (false);
        }
        #endregion

        /// <summary>
        /// The video CODEC tag
        /// </summary>
        public string FourCC
        {
            get { return m_fourCC; }
        }

        /// <summary>
        /// The bits per pixel of the video
        /// </summary>
        public int VideoBitsPerPixel
        {
            get { return m_videoBitsPerPixel; }
        }

        /// <summary>
        /// The length of the video stream
        /// </summary>
        public TimeSpan VideoStreamLength
        {
            get { return TimeSpan.FromSeconds(m_videoStreamLength); }
        }

        /// <summary>
        /// The length of the audio stream
        /// </summary>
        public TimeSpan AudioStreamLength
        {
            get { return TimeSpan.FromSeconds(m_audioStreamLength); }
        }

        public Guid AudioSubType
        {
            get { return m_audioSubType; }
        }

        public Guid VideoSubType
        {
            get { return m_videoSubType; }
        }

        /// <summary>
        /// The number of bits per sample in the audio stream
        /// </summary>
        public int AudioBitsPerSample
        {
            get { return m_audioBitsPerSample; }
        }

        /// <summary>
        /// The HZ of the audio samples
        /// </summary>
        public int AudioSamplesPerSecond
        {
            get { return m_audioSamplesPerSecond; }
        }

        /// <summary>
        /// The number of audio channels in audio stream
        /// </summary>
        public int AudioChannels
        {
            get { return m_audioChannels; }
        }

        /// <summary>
        /// The filename of the loaded media
        /// </summary>
        public string Filename
        {
            get { return m_filename; }
        }

        /// <summary>
        /// The native pixel size of the video, if a
        /// video stream exists
        /// </summary>
        public Size VideoResolution
        {
            get { return m_videoResolution; }
        }

        /// <summary>
        /// The total amount of streams that exist in the media
        /// </summary>
        public int StreamCount
        {
            get { return m_streamCount; }
        }

        /// <summary>
        /// Is true if the loaded media has an audio stream
        /// </summary>
        public bool HasAudio { get; private set; }

        /// <summary>
        /// Is true if the loaded media has a video stream
        /// </summary>
        public bool HasVideo { get; private set; }



        [DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory")]
        private static extern void CopyMemory(IntPtr Destination, IntPtr Source, int Length);

        /// <summary>
        /// Frees any memory and resets to a default state
        /// </summary>
        private void FreeResources()
        {
            m_audioBitsPerSample = 0;
            m_audioChannels = 0;
            m_audioSamplesPerSecond = 0;
            m_audioStreamLength = 0;
            m_audioSubType = Guid.Empty;
            m_filename = "";
            m_fourCC = "";
            m_streamCount = 0;
            m_videoBitsPerPixel = 0;
            m_videoResolution = Size.Empty;
            m_videoStreamLength = 0;
            m_videoSubType = Guid.Empty;
            HasAudio = false;
            HasVideo = false;

            if (m_bitmap != null)
                m_bitmap.Dispose();

            m_bitmap = null;

            if (m_mediaDet != null)
            {
                Marshal.ReleaseComObject(m_mediaDet);
                m_mediaDet = null;
            }
        }

        /// <summary>
        /// Converts a FourCC code to a string
        /// </summary>
        private static string ConvertFourCC(int fourcc)
        {
            return Encoding.ASCII.GetString(BitConverter.GetBytes(fourcc));
        }

        /// <summary>
        /// Loads a media file.
        /// </summary>
        /// <param name="filename">The full path of the media file to load</param>
        public void LoadMedia(string filename)
        {
            FreeResources();

            m_filename = filename;

            try
            {
                if (string.IsNullOrEmpty(m_filename))
                    return;

                LoadMedia();
            }
            catch (Exception)
            {
                FreeResources();
                throw new Exception("Failed to load " + filename);
            }
        }

        private void LoadMedia()
        {
            /*Create the COM object and query the IMediaDet interface */
            m_mediaDet = new MediaDet() as IMediaDet;

            if (m_mediaDet == null)
                throw new NullReferenceException("Could not create an instance of MediaDet COM");

            int hr = m_mediaDet.put_Filename(m_filename);
            DsError.ThrowExceptionForHR(hr);

            /* We find out how many streams exist in the
             * media file.  These can be audio, video, etc */
            hr = m_mediaDet.get_OutputStreams(out m_streamCount);
            DsError.ThrowExceptionForHR(hr);

            /* Loop over each of the streams and extract info from them */
            for (int i = 0; i < m_streamCount; i++)
            {
                /* Set the interface to look at a specific stream */
                hr = m_mediaDet.put_CurrentStream(i);
                DsError.ThrowExceptionForHR(hr);

                Guid majorType;

                /* Get the major type of the media */
                hr = m_mediaDet.get_StreamType(out majorType);
                DsError.ThrowExceptionForHR(hr);

                var mediaType = new AMMediaType();

                /* Gets the AMMediaType so we can read some
                 * metadata on the stream */
                hr = m_mediaDet.get_StreamMediaType(mediaType);
                DsError.ThrowExceptionForHR(hr);

                if (majorType == MediaType.Video)
                {
                    ReadVideoFormat(mediaType);
                }
                else if (majorType == MediaType.Audio)
                {
                    ReadAudioFormat(mediaType);
                }

                /* We need to free this with the helper
                 * because it has an unmanaged pointer 
                 * and we don't want any leaks */
                DsUtils.FreeAMMediaType(mediaType);
            }
        }

        /// <summary>
        /// Reads the audio stream information from the media file
        /// </summary>
        private void ReadAudioFormat(AMMediaType mediaType)
        {
            m_audioSubType = mediaType.subType;

            int hr = m_mediaDet.get_StreamLength(out m_audioStreamLength);
            DsError.ThrowExceptionForHR(hr);

            if (mediaType.formatType == FormatType.WaveEx)
            {
                HasAudio = true;
                var waveFormatEx = (WaveFormatEx)Marshal.PtrToStructure(mediaType.formatPtr, typeof(WaveFormatEx));
                m_audioChannels = waveFormatEx.nChannels;
                m_audioSamplesPerSecond = (waveFormatEx.nSamplesPerSec);
                m_audioBitsPerSample = waveFormatEx.wBitsPerSample;
            }
        }

        /// <summary>
        /// Reads the video stream information for the media file
        /// </summary>
        private void ReadVideoFormat(AMMediaType mediaType)
        {
            m_videoSubType = mediaType.subType;

            int hr = m_mediaDet.get_StreamLength(out m_videoStreamLength);
            DsError.ThrowExceptionForHR(hr);

            if (mediaType.formatType == FormatType.VideoInfo) /* Most common video major type */
            {
                HasVideo = true;

                /* 'Cast' the unmanaged pointer to our managed struct so we can read the meta data */
                var header = (VideoInfoHeader)Marshal.PtrToStructure(mediaType.formatPtr, typeof(VideoInfoHeader));
                m_fourCC = ConvertFourCC(header.BmiHeader.Compression);
                m_videoBitsPerPixel = header.BmiHeader.BitCount;
                m_videoResolution = new Size(header.BmiHeader.Width, header.BmiHeader.Height);
            }
            else if (mediaType.formatType == FormatType.VideoInfo2) /* Usually for interlaced video */
            {
                HasVideo = true;

                /* 'Cast' the unmanaged pointer to our managed struct so we can read the meta data */
                var header = (VideoInfoHeader2)Marshal.PtrToStructure(mediaType.formatPtr, typeof(VideoInfoHeader2));
                m_fourCC = ConvertFourCC(header.BmiHeader.Compression);
                m_videoResolution = new Size(header.BmiHeader.Width, header.BmiHeader.Height);
                m_videoBitsPerPixel = header.BmiHeader.BitCount;
                /* TODO: Pull out VideoInfoHeader2 specifics */
            }
        }

    }
}