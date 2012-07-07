namespace WTVTranscoding
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.btnStartTranscodeToFile = new System.Windows.Forms.Button();
            this.txtSpool = new System.Windows.Forms.TextBox();
            this.pbProgress = new System.Windows.Forms.ProgressBar();
            this.btnCancelFileTranscode = new System.Windows.Forms.Button();
            this.btnStartTranscodeToStream = new System.Windows.Forms.Button();
            this.cbPlayInWMP = new System.Windows.Forms.CheckBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.btn1x = new System.Windows.Forms.Button();
            this.btn2x = new System.Windows.Forms.Button();
            this.btnSeek = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.cmbQuality = new System.Windows.Forms.ComboBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.cbDeinterlace = new System.Windows.Forms.CheckBox();
            this.txtFileName = new System.Windows.Forms.TextBox();
            this.btnChooseFile = new System.Windows.Forms.Button();
            this.btnGetMediaDuration = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnStartTranscodeToFile
            // 
            this.btnStartTranscodeToFile.Location = new System.Drawing.Point(23, 19);
            this.btnStartTranscodeToFile.Name = "btnStartTranscodeToFile";
            this.btnStartTranscodeToFile.Size = new System.Drawing.Size(141, 37);
            this.btnStartTranscodeToFile.TabIndex = 0;
            this.btnStartTranscodeToFile.Text = "Transcode to file";
            this.btnStartTranscodeToFile.UseVisualStyleBackColor = true;
            this.btnStartTranscodeToFile.Click += new System.EventHandler(this.btnStartTranscodeToFile_Click_1);
            // 
            // txtSpool
            // 
            this.txtSpool.Location = new System.Drawing.Point(11, 278);
            this.txtSpool.Multiline = true;
            this.txtSpool.Name = "txtSpool";
            this.txtSpool.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtSpool.Size = new System.Drawing.Size(393, 178);
            this.txtSpool.TabIndex = 1;
            // 
            // pbProgress
            // 
            this.pbProgress.Location = new System.Drawing.Point(11, 253);
            this.pbProgress.Name = "pbProgress";
            this.pbProgress.Size = new System.Drawing.Size(393, 19);
            this.pbProgress.TabIndex = 2;
            // 
            // btnCancelFileTranscode
            // 
            this.btnCancelFileTranscode.Location = new System.Drawing.Point(23, 73);
            this.btnCancelFileTranscode.Name = "btnCancelFileTranscode";
            this.btnCancelFileTranscode.Size = new System.Drawing.Size(141, 32);
            this.btnCancelFileTranscode.TabIndex = 3;
            this.btnCancelFileTranscode.Text = "Cancel";
            this.btnCancelFileTranscode.UseVisualStyleBackColor = true;
            this.btnCancelFileTranscode.Click += new System.EventHandler(this.btnCancelFileTranscode_Click);
            // 
            // btnStartTranscodeToStream
            // 
            this.btnStartTranscodeToStream.Location = new System.Drawing.Point(14, 19);
            this.btnStartTranscodeToStream.Name = "btnStartTranscodeToStream";
            this.btnStartTranscodeToStream.Size = new System.Drawing.Size(173, 34);
            this.btnStartTranscodeToStream.TabIndex = 4;
            this.btnStartTranscodeToStream.Text = "Transcode to stream";
            this.btnStartTranscodeToStream.UseVisualStyleBackColor = true;
            this.btnStartTranscodeToStream.Click += new System.EventHandler(this.btnStartTranscodeToStream_Click_1);
            // 
            // cbPlayInWMP
            // 
            this.cbPlayInWMP.AutoSize = true;
            this.cbPlayInWMP.Location = new System.Drawing.Point(35, 59);
            this.cbPlayInWMP.Name = "cbPlayInWMP";
            this.cbPlayInWMP.Size = new System.Drawing.Size(151, 17);
            this.cbPlayInWMP.TabIndex = 7;
            this.cbPlayInWMP.Text = "and launch WMP to play it";
            this.cbPlayInWMP.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.btnStartTranscodeToFile);
            this.groupBox1.Controls.Add(this.btnCancelFileTranscode);
            this.groupBox1.Location = new System.Drawing.Point(11, 13);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(187, 125);
            this.groupBox1.TabIndex = 11;
            this.groupBox1.TabStop = false;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.btn1x);
            this.groupBox2.Controls.Add(this.btn2x);
            this.groupBox2.Controls.Add(this.btnSeek);
            this.groupBox2.Controls.Add(this.btnStartTranscodeToStream);
            this.groupBox2.Controls.Add(this.cbPlayInWMP);
            this.groupBox2.Location = new System.Drawing.Point(204, 13);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(200, 125);
            this.groupBox2.TabIndex = 12;
            this.groupBox2.TabStop = false;
            // 
            // btn1x
            // 
            this.btn1x.Location = new System.Drawing.Point(140, 82);
            this.btn1x.Name = "btn1x";
            this.btn1x.Size = new System.Drawing.Size(47, 29);
            this.btn1x.TabIndex = 10;
            this.btn1x.Text = "1x";
            this.btn1x.UseVisualStyleBackColor = true;
            this.btn1x.Click += new System.EventHandler(this.btn1x_Click);
            // 
            // btn2x
            // 
            this.btn2x.Location = new System.Drawing.Point(94, 82);
            this.btn2x.Name = "btn2x";
            this.btn2x.Size = new System.Drawing.Size(47, 29);
            this.btn2x.TabIndex = 9;
            this.btn2x.Text = "3x";
            this.btn2x.UseVisualStyleBackColor = true;
            this.btn2x.Click += new System.EventHandler(this.btn2x_Click);
            // 
            // btnSeek
            // 
            this.btnSeek.Location = new System.Drawing.Point(14, 82);
            this.btnSeek.Name = "btnSeek";
            this.btnSeek.Size = new System.Drawing.Size(73, 29);
            this.btnSeek.TabIndex = 8;
            this.btnSeek.Text = "Seek";
            this.btnSeek.UseVisualStyleBackColor = true;
            this.btnSeek.Click += new System.EventHandler(this.btnSeek_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(18, 22);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(42, 13);
            this.label2.TabIndex = 13;
            this.label2.Text = "Quality:";
            // 
            // cmbQuality
            // 
            this.cmbQuality.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbQuality.FormattingEnabled = true;
            this.cmbQuality.Items.AddRange(new object[] {
            "Low",
            "Normal",
            "Medium",
            "High",
            "Ultra-High",
            "Test",
            "Custom"});
            this.cmbQuality.Location = new System.Drawing.Point(70, 19);
            this.cmbQuality.Name = "cmbQuality";
            this.cmbQuality.Size = new System.Drawing.Size(131, 21);
            this.cmbQuality.TabIndex = 14;
            this.cmbQuality.SelectedIndexChanged += new System.EventHandler(this.cmbQuality_SelectedIndexChanged);
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.cbDeinterlace);
            this.groupBox3.Controls.Add(this.cmbQuality);
            this.groupBox3.Controls.Add(this.label2);
            this.groupBox3.Location = new System.Drawing.Point(187, 144);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(217, 103);
            this.groupBox3.TabIndex = 16;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Settings";
            // 
            // cbDeinterlace
            // 
            this.cbDeinterlace.AutoSize = true;
            this.cbDeinterlace.Location = new System.Drawing.Point(72, 54);
            this.cbDeinterlace.Name = "cbDeinterlace";
            this.cbDeinterlace.Size = new System.Drawing.Size(80, 17);
            this.cbDeinterlace.TabIndex = 15;
            this.cbDeinterlace.Text = "Deinterlace";
            this.cbDeinterlace.UseVisualStyleBackColor = true;
            // 
            // txtFileName
            // 
            this.txtFileName.Location = new System.Drawing.Point(13, 158);
            this.txtFileName.Name = "txtFileName";
            this.txtFileName.Size = new System.Drawing.Size(111, 20);
            this.txtFileName.TabIndex = 17;
            // 
            // btnChooseFile
            // 
            this.btnChooseFile.Location = new System.Drawing.Point(130, 158);
            this.btnChooseFile.Name = "btnChooseFile";
            this.btnChooseFile.Size = new System.Drawing.Size(33, 20);
            this.btnChooseFile.TabIndex = 18;
            this.btnChooseFile.Text = "...";
            this.btnChooseFile.UseVisualStyleBackColor = true;
            this.btnChooseFile.Click += new System.EventHandler(this.btnChooseFile_Click);
            // 
            // btnGetMediaDuration
            // 
            this.btnGetMediaDuration.Location = new System.Drawing.Point(13, 189);
            this.btnGetMediaDuration.Name = "btnGetMediaDuration";
            this.btnGetMediaDuration.Size = new System.Drawing.Size(145, 25);
            this.btnGetMediaDuration.TabIndex = 19;
            this.btnGetMediaDuration.Text = "Get Media Duration";
            this.btnGetMediaDuration.UseVisualStyleBackColor = true;
            this.btnGetMediaDuration.Click += new System.EventHandler(this.btnGetMediaDuration_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(420, 464);
            this.Controls.Add(this.btnGetMediaDuration);
            this.Controls.Add(this.btnChooseFile);
            this.Controls.Add(this.txtFileName);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.pbProgress);
            this.Controls.Add(this.txtSpool);
            this.Name = "Form1";
            this.Text = "WTV Transcode Test Harness";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnStartTranscodeToFile;
        private System.Windows.Forms.TextBox txtSpool;
        private System.Windows.Forms.ProgressBar pbProgress;
        private System.Windows.Forms.Button btnCancelFileTranscode;
        private System.Windows.Forms.Button btnStartTranscodeToStream;
        private System.Windows.Forms.CheckBox cbPlayInWMP;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox cmbQuality;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Button btnSeek;
        private System.Windows.Forms.Button btn1x;
        private System.Windows.Forms.Button btn2x;
        private System.Windows.Forms.CheckBox cbDeinterlace;
        private System.Windows.Forms.TextBox txtFileName;
        private System.Windows.Forms.Button btnChooseFile;
        private System.Windows.Forms.Button btnGetMediaDuration;
    }
}

