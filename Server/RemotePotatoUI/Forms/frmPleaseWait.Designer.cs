namespace RemotePotatoServer
{
    partial class frmPleaseWait
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.pbProgress = new System.Windows.Forms.ProgressBar();
            this.lblActivity = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.BackColor = System.Drawing.Color.Transparent;
            this.groupBox1.Controls.Add(this.pbProgress);
            this.groupBox1.Controls.Add(this.lblActivity);
            this.groupBox1.Location = new System.Drawing.Point(6, 0);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(283, 98);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            // 
            // pbProgress
            // 
            this.pbProgress.Location = new System.Drawing.Point(29, 58);
            this.pbProgress.MarqueeAnimationSpeed = 1000;
            this.pbProgress.Name = "pbProgress";
            this.pbProgress.Size = new System.Drawing.Size(222, 20);
            this.pbProgress.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.pbProgress.TabIndex = 1;
            this.pbProgress.UseWaitCursor = true;
            // 
            // lblActivity
            // 
            this.lblActivity.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblActivity.ForeColor = System.Drawing.SystemColors.ButtonFace;
            this.lblActivity.Location = new System.Drawing.Point(29, 16);
            this.lblActivity.Name = "lblActivity";
            this.lblActivity.Size = new System.Drawing.Size(222, 29);
            this.lblActivity.TabIndex = 0;
            this.lblActivity.Text = "Please Wait";
            this.lblActivity.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // frmPleaseWait
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImage = global::RemotePotatoServer.Properties.Resources.formMainBG;
            this.ClientSize = new System.Drawing.Size(294, 104);
            this.ControlBox = false;
            this.Controls.Add(this.groupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "frmPleaseWait";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Load += new System.EventHandler(this.frmPleaseWait_Load);
            this.groupBox1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.ProgressBar pbProgress;
        private System.Windows.Forms.Label lblActivity;
    }
}