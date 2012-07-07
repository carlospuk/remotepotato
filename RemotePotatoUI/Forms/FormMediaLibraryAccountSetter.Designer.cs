namespace RemotePotatoServer
{
    partial class FormMediaLibraryAccountSetter
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormMediaLibraryAccountSetter));
            this.txtMediaLibraryUsername = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.txtMediaLibraryPassword = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnSetNewAccount = new System.Windows.Forms.Button();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.lblStatus = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // txtMediaLibraryUsername
            // 
            this.txtMediaLibraryUsername.Location = new System.Drawing.Point(217, 174);
            this.txtMediaLibraryUsername.MaxLength = 30;
            this.txtMediaLibraryUsername.Name = "txtMediaLibraryUsername";
            this.txtMediaLibraryUsername.Size = new System.Drawing.Size(181, 20);
            this.txtMediaLibraryUsername.TabIndex = 33;
            this.txtMediaLibraryUsername.Text = "Account Name";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.BackColor = System.Drawing.Color.Transparent;
            this.label5.Location = new System.Drawing.Point(153, 202);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(56, 13);
            this.label5.TabIndex = 32;
            this.label5.Text = "Password:";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.BackColor = System.Drawing.Color.Transparent;
            this.label10.Location = new System.Drawing.Point(153, 177);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(58, 13);
            this.label10.TabIndex = 31;
            this.label10.Text = "Username:";
            // 
            // txtMediaLibraryPassword
            // 
            this.txtMediaLibraryPassword.Location = new System.Drawing.Point(217, 199);
            this.txtMediaLibraryPassword.MaxLength = 30;
            this.txtMediaLibraryPassword.Name = "txtMediaLibraryPassword";
            this.txtMediaLibraryPassword.PasswordChar = '*';
            this.txtMediaLibraryPassword.Size = new System.Drawing.Size(181, 20);
            this.txtMediaLibraryPassword.TabIndex = 34;
            this.txtMediaLibraryPassword.UseSystemPasswordChar = true;
            this.txtMediaLibraryPassword.KeyUp += new System.Windows.Forms.KeyEventHandler(this.txtMediaLibraryPassword_KeyUp);
            // 
            // label4
            // 
            this.label4.BackColor = System.Drawing.Color.Transparent;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(12, 19);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(470, 128);
            this.label4.TabIndex = 30;
            this.label4.Text = resources.GetString("label4.Text");
            // 
            // btnCancel
            // 
            this.btnCancel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnCancel.Location = new System.Drawing.Point(15, 278);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(101, 24);
            this.btnCancel.TabIndex = 35;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // btnSetNewAccount
            // 
            this.btnSetNewAccount.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSetNewAccount.Location = new System.Drawing.Point(455, 278);
            this.btnSetNewAccount.Name = "btnSetNewAccount";
            this.btnSetNewAccount.Size = new System.Drawing.Size(101, 24);
            this.btnSetNewAccount.TabIndex = 36;
            this.btnSetNewAccount.Text = "Save";
            this.btnSetNewAccount.UseVisualStyleBackColor = true;
            this.btnSetNewAccount.Click += new System.EventHandler(this.btnSetNewAccount_Click);
            // 
            // pictureBox1
            // 
            this.pictureBox1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pictureBox1.Image = global::RemotePotatoServer.Properties.Resources.ProtectFormHH;
            this.pictureBox1.Location = new System.Drawing.Point(504, 12);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(63, 62);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox1.TabIndex = 37;
            this.pictureBox1.TabStop = false;
            // 
            // lblStatus
            // 
            this.lblStatus.BackColor = System.Drawing.Color.Transparent;
            this.lblStatus.ForeColor = System.Drawing.Color.Maroon;
            this.lblStatus.Location = new System.Drawing.Point(115, 233);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(318, 13);
            this.lblStatus.TabIndex = 38;
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // FormMediaLibraryAccountSetter
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackgroundImage = global::RemotePotatoServer.Properties.Resources.lightFormBG;
            this.ClientSize = new System.Drawing.Size(579, 311);
            this.ControlBox = false;
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.btnSetNewAccount);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.txtMediaLibraryUsername);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.txtMediaLibraryPassword);
            this.Controls.Add(this.label4);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "FormMediaLibraryAccountSetter";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Music Library Account";
            this.Load += new System.EventHandler(this.FormMediaLibraryAccountSetter_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtMediaLibraryUsername;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TextBox txtMediaLibraryPassword;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnSetNewAccount;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label lblStatus;
    }
}