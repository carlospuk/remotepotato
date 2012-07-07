namespace RemotePotatoServer
{
    partial class FormDonationRequest
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormDonationRequest));
            this.label1 = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.label2 = new System.Windows.Forms.Label();
            this.pbCarlPic = new System.Windows.Forms.PictureBox();
            this.btnWantToDonate = new System.Windows.Forms.Button();
            this.btnNotNow = new System.Windows.Forms.LinkLabel();
            this.btnAlreadyDonated = new System.Windows.Forms.LinkLabel();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbCarlPic)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.BackColor = System.Drawing.Color.Transparent;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(11, 18);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(277, 25);
            this.label1.TabIndex = 2;
            this.label1.Text = "Please Help My Marriage";
            // 
            // panel1
            // 
            this.panel1.BackgroundImage = global::RemotePotatoServer.Properties.Resources.formBG;
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.label1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(645, 65);
            this.panel1.TabIndex = 3;
            // 
            // label2
            // 
            this.label2.BackColor = System.Drawing.Color.Transparent;
            this.label2.Location = new System.Drawing.Point(17, 83);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(365, 248);
            this.label2.TabIndex = 4;
            this.label2.Text = resources.GetString("label2.Text");
            // 
            // pbCarlPic
            // 
            this.pbCarlPic.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pbCarlPic.Image = global::RemotePotatoServer.Properties.Resources.begging;
            this.pbCarlPic.Location = new System.Drawing.Point(429, 83);
            this.pbCarlPic.Name = "pbCarlPic";
            this.pbCarlPic.Size = new System.Drawing.Size(202, 248);
            this.pbCarlPic.TabIndex = 5;
            this.pbCarlPic.TabStop = false;
            // 
            // btnWantToDonate
            // 
            this.btnWantToDonate.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.btnWantToDonate.Location = new System.Drawing.Point(371, 350);
            this.btnWantToDonate.Name = "btnWantToDonate";
            this.btnWantToDonate.Size = new System.Drawing.Size(260, 36);
            this.btnWantToDonate.TabIndex = 6;
            this.btnWantToDonate.Text = "I\'d like to donate";
            this.btnWantToDonate.UseVisualStyleBackColor = false;
            this.btnWantToDonate.Click += new System.EventHandler(this.btnWantToDonate_Click);
            // 
            // btnNotNow
            // 
            this.btnNotNow.AutoSize = true;
            this.btnNotNow.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnNotNow.Location = new System.Drawing.Point(17, 373);
            this.btnNotNow.Name = "btnNotNow";
            this.btnNotNow.Size = new System.Drawing.Size(70, 13);
            this.btnNotNow.TabIndex = 7;
            this.btnNotNow.TabStop = true;
            this.btnNotNow.Text = "Not right now";
            this.btnNotNow.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.btnNotNow_LinkClicked);
            // 
            // btnAlreadyDonated
            // 
            this.btnAlreadyDonated.AutoSize = true;
            this.btnAlreadyDonated.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnAlreadyDonated.Location = new System.Drawing.Point(184, 373);
            this.btnAlreadyDonated.Name = "btnAlreadyDonated";
            this.btnAlreadyDonated.Size = new System.Drawing.Size(92, 13);
            this.btnAlreadyDonated.TabIndex = 8;
            this.btnAlreadyDonated.TabStop = true;
            this.btnAlreadyDonated.Text = "I already donated!";
            this.btnAlreadyDonated.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.btnAlreadyDonated_LinkClicked);
            // 
            // FormDonationRequest
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImage = global::RemotePotatoServer.Properties.Resources.tabControlBGGif;
            this.ClientSize = new System.Drawing.Size(645, 398);
            this.Controls.Add(this.btnAlreadyDonated);
            this.Controls.Add(this.btnNotNow);
            this.Controls.Add(this.btnWantToDonate);
            this.Controls.Add(this.pbCarlPic);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.panel1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "FormDonationRequest";
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbCarlPic)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.PictureBox pbCarlPic;
        private System.Windows.Forms.Button btnWantToDonate;
        private System.Windows.Forms.LinkLabel btnNotNow;
        private System.Windows.Forms.LinkLabel btnAlreadyDonated;
    }
}