namespace RemotePotatoServer
{
    partial class FormConnectionInformation
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
            this.ucConnectionSummary1 = new RemotePotatoServer.ucConnectionSummary();
            this.SuspendLayout();
            // 
            // ucConnectionSummary1
            // 
            this.ucConnectionSummary1.Location = new System.Drawing.Point(6, 2);
            this.ucConnectionSummary1.Name = "ucConnectionSummary1";
            this.ucConnectionSummary1.Size = new System.Drawing.Size(722, 697);
            this.ucConnectionSummary1.TabIndex = 0;
            // 
            // FormConnectionInformation
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(738, 601);
            this.Controls.Add(this.ucConnectionSummary1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormConnectionInformation";
            this.ShowInTaskbar = false;
            this.Text = "Connection Information";
            this.ResumeLayout(false);

        }

        #endregion

        private ucConnectionSummary ucConnectionSummary1;
    }
}