using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;
using FatAttitude.WTVTranscoder;
using FatAttitude;


namespace WTVTranscoding
{
    /// <summary>
    /// Test harness for the WTVStreamer and WTVFileWriter classes
    /// </summary>
    public partial class Form1 : Form
    {
        // The two objects to test
        List<DSStreamer> streamers;
        DSFileWriter MyFileWriter;

        // Conversion settings
        WTVProfileQuality Quality;
        int CustomFrameWidth = 340;
        int CustomFrameHeight = 250;
        int CustomVidBitrate = 100000;
        int CustomSmoothness = 50;
        int CustomEncoderFPS = 30;


        // Constructor / Init
        public Form1()
        {
            InitializeComponent();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            // Init objects
            InitTestObjects();

            // Init Gui
            InitGUI();
        }
        void InitTestObjects()
        {
            if (MyFileWriter != null) MyFileWriter = null;

            // Initialise up the two test objects
            MyFileWriter = new DSFileWriter();
            MyFileWriter.ProgressChanged += new EventHandler<DSTranscoderBase.ProgressChangedEventArgs>(MyFileWriter_ProgressChanged);
            MyFileWriter.DebugMessageGenerated += new EventHandler<DSTranscoderBase.DebugMessageEventArgs>(WTVObject_DebugMessageGenerated);
            MyFileWriter.Completed += new EventHandler(MyFileWriter_Completed);


            streamers = new List<DSStreamer>();
            
        }
        void InitGUI()
        {
            cmbQuality.SelectedIndex = (int)WTVProfileQuality.Test;  // Which, in turn, initialises the Quality variable in the ComboBox SelectedIndexChanged event
        }

        #region Form Events
        // Button Clicks
        private void btnStartTranscodeToStream_Click_1(object sender, EventArgs e)
        {
            StartTranscodeToStream();
        }
        private void btnCancelStreaming_Click(object sender, EventArgs e)
        {
            CancelStreamerOrTranscoder();
        }
        private void btnStartTranscodeToFile_Click_1(object sender, EventArgs e)
        {
            StartTranscodeToFile();
        }
        private void btnCancelFileTranscode_Click(object sender, EventArgs e)
        {
            CancelStreamerOrTranscoder();
        }

        // Combo box to set conversion quality

        private void cmbQuality_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbQuality == null) return;

            Quality = (WTVProfileQuality)cmbQuality.SelectedIndex;

        }
        #endregion

        #region Main Test Methods

        /// <summary>
        /// Initialise and run the WTVStreamer
        /// </summary>
        private void StartTranscodeToStream()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            //ofd.Filter = "Compatible SBE files|*.wtv;*.dvr-ms";
            ofd.Filter = "All files|*.*";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                System.Threading.Thread MyTranscodeThread = new System.Threading.Thread(DoTranscodeToStream);
                MyTranscodeThread.Start(ofd.FileName);
            }
        }
        void DoTranscodeToStream(object data)
        {
            DSStreamer MyStreamer = new DSStreamer();
            MyStreamer.ConversionProgressChanged += new EventHandler<DSTranscoderBase.ProgressChangedEventArgs>(MyStreamer_ConversionProgressChanged);
            MyStreamer.ConversionCompleted += new EventHandler(MyStreamer_ConversionCompleted);
            MyStreamer.Finished += new EventHandler<DSTranscoderBase.ConversionEndedEventArgs>(MyStreamer_Finished);
            MyStreamer.DebugMessageGenerated += new EventHandler<DSTranscoderBase.DebugMessageEventArgs>(WTVObject_DebugMessageGenerated);

            streamers.Add(MyStreamer);

            safeAddToSpool("Streaming file.");
            string FileName = (string)data;
            WTVStreamingVideoRequest strq = new WTVStreamingVideoRequest(FileName, Quality, TimeSpan.FromSeconds(0));
            if (Quality == WTVProfileQuality.Custom)
            {
                strq.CustomFrameWidth = CustomFrameWidth;
                strq.CustomFrameHeight = CustomFrameHeight;
                strq.CustomVideoBitrate = CustomVidBitrate;
                strq.CustomEncoderSmoothness = CustomSmoothness;
                strq.CustomEncoderFPS = CustomEncoderFPS;
            }
            if (cbDeinterlace.Checked)
                strq.DeInterlaceMode = 1;

            WTVStreamingVideoResult result = MyStreamer.StreamWithFileAndPort(strq, 9081, false, true);

            if (result.ResultCode == DSStreamResultCodes.OK)
            {
                if (cbPlayInWMP.Checked)
                {
                    // Wait 2 seconds then launch Windows Media Player  (Must use a System.Threading.Timer as we're in a multithread environment)
                    System.Threading.Timer t = new System.Threading.Timer(LaunchWindowsMediaPlayer, null, 6000, System.Threading.Timeout.Infinite);
                   
                }
            }
            else
            {
                MessageBox.Show("Couldn't stream file : " + result.ResultCode.ToString() + ": " + result.ResultString);
            }
        }
        void LaunchWindowsMediaPlayer(object sender)
        {
            ProcessStartInfo psi = new ProcessStartInfo("mms://localhost:9081","");
            psi.UseShellExecute = true;
            Process.Start(psi);
        }

        /// <summary>
        /// Initialise and run the WTVFileWriter
        /// </summary>
        private void StartTranscodeToFile()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "All files|*.*";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                safeAddToSpool("Transcoding file to file.");

                DSStreamResultCodes result = MyFileWriter.TranscodeFileAsync(ofd.FileName, Quality);
                if (result != DSStreamResultCodes.OK)
                {
                    MessageBox.Show("Failed to start file transcoding: " + result.ToString());
                }
            }
        }

        /// <summary>
        /// Cancels any running WTVStreamer or WTVFileWriter
        /// </summary>
        void CancelStreamerOrTranscoder()
        {
            safeAddToSpool("Attempting to cancel.");

            if (MyFileWriter != null)
                MyFileWriter.Cancel();
        }
        #endregion

        #region Incoming Events
        // File Transcoder
        void MyFileWriter_ProgressChanged(object sender, DSTranscoderBase.ProgressChangedEventArgs e)
        {
            if ((e.ProgressPercentage < 101) && (e.ProgressPercentage >= 0))
                safeUpdateProgressBar(Convert.ToInt32(e.ProgressPercentage));
        }
        void MyFileWriter_Completed(object sender, EventArgs e)
        {
            safeAddToSpool("Transcode file finished.");

            // Reset object
            InitTestObjects();
        }
        void MyStreamer_ConversionProgressChanged(object sender, DSTranscoderBase.ProgressChangedEventArgs e)
        {
            if (double.IsNaN(e.ProgressPercentage)) return;
            if (double.IsInfinity(e.ProgressPercentage)) return;

            safeUpdateProgressBar(Convert.ToInt32(e.ProgressPercentage));
        }
        void MyStreamer_Finished(object sender, DSTranscoderBase.ConversionEndedEventArgs e)
        {
            if (e.WasError)
            {
                safeAddToSpool("There was an error transcoding the stream: " + e.Message);
                return;
            }

            safeAddToSpool("Transcoding complete:  " + e.Message);

            if (sender is DSStreamer)
            {
                streamers.Remove((DSStreamer)sender);
            }

            // Reset object
            InitTestObjects();
        }

        void MyStreamer_ConversionCompleted(object sender, EventArgs e)
        {
            safeAddToSpool("(the conversion graph has finished but is still running)");
        }
        // Debug callback
        void WTVObject_DebugMessageGenerated(object sender, DSTranscoderBase.DebugMessageEventArgs e)
        {
            safeAddToSpool(e.DebugMessage);
            if (e.HasException)
            {
                safeAddToSpool(e.InnerException.Message);
                if (!string.IsNullOrEmpty(e.InnerException.StackTrace))
                    safeAddToSpool(e.InnerException.StackTrace);
                if (!string.IsNullOrEmpty(e.InnerException.Source))
                    safeAddToSpool(e.InnerException.Source);
            }
        }
        #endregion

        #region Debug Helpers
        // These delegates enable asynchronous calls for background tasks
        delegate void updateProgressBarCallBack(int value);
        delegate void addToSpoolCallBack(string value);
        private void safeAddToSpool(string txtValue)
        {
            if (txtSpool.InvokeRequired)
            {   // It's on a different thread, so use Invoke.
                addToSpoolCallBack dl = new addToSpoolCallBack(AddToSpool);
                this.Invoke(dl, new object[] { txtValue});
            }
            else
                AddToSpool(txtValue);
        }
        private void AddToSpool(string txtText)
        {
            txtSpool.Text += txtText + Environment.NewLine;
        }
        private void safeUpdateProgressBar(int newValue)
        {
            if (pbProgress.InvokeRequired)
            {   // It's on a different thread, so use Invoke.
                updateProgressBarCallBack dl = new updateProgressBarCallBack(updateProgressBar);
                this.Invoke(dl, new object[] { newValue});
            }
            else
                updateProgressBar(newValue);
        }
        private void updateProgressBar(int newValue)
        {
            if (newValue < pbProgress.Maximum)
                pbProgress.Value = newValue;
        }
        #endregion

        private void btnSeek_Click(object sender, EventArgs e)
        {
            if (streamers.Count < 1) return;

            DSStreamer st = streamers[0];

            st.Seek(TimeSpan.FromMinutes(5));
        }

        #region Set Rate
        private void btn2x_Click(object sender, EventArgs e)
        {
            setRate(3);
        }

        private void btn1x_Click(object sender, EventArgs e)
        {
            setRate(1);
        }

        void setRate(double newrate)
        {
            if (streamers.Count < 1) return;

            DSStreamer st = streamers[0];

            st.SetRate(newrate);
        }
        #endregion

        private void btnChooseFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "All files|*.*";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                txtFileName.Text = ofd.FileName;
            }
        }

        private void btnGetMediaDuration_Click(object sender, EventArgs e)
        {
            if (txtFileName.Text.Trim().Length < 1) return;

            DSMediaInfoHarness harness = new DSMediaInfoHarness();
            TimeSpan ts = harness.GetMediaDuration(txtFileName.Text);

            MessageBox.Show(ts.ToString());
        }
    }
}
