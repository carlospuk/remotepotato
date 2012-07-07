namespace RemotePotatoServer
{
    partial class FormThemesChooser
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormThemesChooser));
            this.label4 = new System.Windows.Forms.Label();
            this.lstCurrentMobileTheme = new System.Windows.Forms.ComboBox();
            this.lstCurrentMainTheme = new System.Windows.Forms.ComboBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.txtAboutTheme = new System.Windows.Forms.TextBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.txtAboutMobileTheme = new System.Windows.Forms.TextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // label4
            // 
            this.label4.BackColor = System.Drawing.Color.Transparent;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.ForeColor = System.Drawing.Color.Black;
            this.label4.Location = new System.Drawing.Point(12, 9);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(554, 74);
            this.label4.TabIndex = 31;
            this.label4.Text = "You can change how Remote Potato looks in your web browser by picking a different" +
                " theme from the selections below.\r\n\r\nFind more themes in the \'Themes and Apps\' s" +
                "ection of the Remote Potato Forums.";
            // 
            // lstCurrentMobileTheme
            // 
            this.lstCurrentMobileTheme.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.lstCurrentMobileTheme.FormattingEnabled = true;
            this.lstCurrentMobileTheme.Location = new System.Drawing.Point(5, 19);
            this.lstCurrentMobileTheme.Name = "lstCurrentMobileTheme";
            this.lstCurrentMobileTheme.Size = new System.Drawing.Size(253, 21);
            this.lstCurrentMobileTheme.TabIndex = 33;
            this.lstCurrentMobileTheme.SelectedIndexChanged += new System.EventHandler(this.lstCurrentMobileTheme_SelectedIndexChanged);
            // 
            // lstCurrentMainTheme
            // 
            this.lstCurrentMainTheme.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.lstCurrentMainTheme.FormattingEnabled = true;
            this.lstCurrentMainTheme.Location = new System.Drawing.Point(6, 19);
            this.lstCurrentMainTheme.Name = "lstCurrentMainTheme";
            this.lstCurrentMainTheme.Size = new System.Drawing.Size(239, 21);
            this.lstCurrentMainTheme.TabIndex = 32;
            this.lstCurrentMainTheme.SelectedIndexChanged += new System.EventHandler(this.lstCurrentMainTheme_SelectedIndexChanged);
            // 
            // groupBox1
            // 
            this.groupBox1.BackColor = System.Drawing.Color.Transparent;
            this.groupBox1.Controls.Add(this.txtAboutTheme);
            this.groupBox1.Controls.Add(this.lstCurrentMainTheme);
            this.groupBox1.ForeColor = System.Drawing.Color.Black;
            this.groupBox1.Location = new System.Drawing.Point(15, 100);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(253, 210);
            this.groupBox1.TabIndex = 34;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Desktop Browsers";
            // 
            // txtAboutTheme
            // 
            this.txtAboutTheme.BackColor = System.Drawing.Color.DimGray;
            this.txtAboutTheme.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtAboutTheme.ForeColor = System.Drawing.SystemColors.Info;
            this.txtAboutTheme.Location = new System.Drawing.Point(7, 47);
            this.txtAboutTheme.Multiline = true;
            this.txtAboutTheme.Name = "txtAboutTheme";
            this.txtAboutTheme.Size = new System.Drawing.Size(238, 157);
            this.txtAboutTheme.TabIndex = 33;
            // 
            // groupBox2
            // 
            this.groupBox2.BackColor = System.Drawing.Color.Transparent;
            this.groupBox2.Controls.Add(this.txtAboutMobileTheme);
            this.groupBox2.Controls.Add(this.lstCurrentMobileTheme);
            this.groupBox2.ForeColor = System.Drawing.Color.Black;
            this.groupBox2.Location = new System.Drawing.Point(292, 100);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(265, 210);
            this.groupBox2.TabIndex = 35;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Mobile Browsers";
            // 
            // txtAboutMobileTheme
            // 
            this.txtAboutMobileTheme.BackColor = System.Drawing.Color.DimGray;
            this.txtAboutMobileTheme.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtAboutMobileTheme.ForeColor = System.Drawing.SystemColors.Info;
            this.txtAboutMobileTheme.Location = new System.Drawing.Point(5, 47);
            this.txtAboutMobileTheme.Multiline = true;
            this.txtAboutMobileTheme.Name = "txtAboutMobileTheme";
            this.txtAboutMobileTheme.Size = new System.Drawing.Size(252, 157);
            this.txtAboutMobileTheme.TabIndex = 34;
            // 
            // button1
            // 
            this.button1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button1.Location = new System.Drawing.Point(482, 321);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 27);
            this.button1.TabIndex = 36;
            this.button1.Text = "Close";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // FormThemesChooser
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImage = global::RemotePotatoServer.Properties.Resources.lightFormBG;
            this.ClientSize = new System.Drawing.Size(572, 357);
            this.ControlBox = false;
            this.Controls.Add(this.button1);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.label4);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "FormThemesChooser";
            this.Text = "Choose Themes";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox lstCurrentMobileTheme;
        private System.Windows.Forms.ComboBox lstCurrentMainTheme;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TextBox txtAboutTheme;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.TextBox txtAboutMobileTheme;
        private System.Windows.Forms.Button button1;
    }
}