namespace RemotePotatoServer
{
    partial class FormFoldersCollection
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormFoldersCollection));
            this.btnAddFolder = new System.Windows.Forms.Button();
            this.btnDeleteFolder = new System.Windows.Forms.Button();
            this.btnSaveAndClose = new System.Windows.Forms.Button();
            this.lblCaption = new System.Windows.Forms.Label();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnSetDefault = new System.Windows.Forms.Button();
            this.cbRecurseFolders = new System.Windows.Forms.CheckBox();
            this.cbUseWin7LibraryInstead = new System.Windows.Forms.CheckBox();
            this.btnHelpWin7Libraries = new System.Windows.Forms.LinkLabel();
            this.lvRecTVFolders = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.btnAddPath = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btnAddFolder
            // 
            this.btnAddFolder.Location = new System.Drawing.Point(12, 176);
            this.btnAddFolder.Name = "btnAddFolder";
            this.btnAddFolder.Size = new System.Drawing.Size(75, 23);
            this.btnAddFolder.TabIndex = 1;
            this.btnAddFolder.Text = "Add folder";
            this.btnAddFolder.UseVisualStyleBackColor = true;
            this.btnAddFolder.Click += new System.EventHandler(this.btnAddFolder_Click);
            // 
            // btnDeleteFolder
            // 
            this.btnDeleteFolder.Location = new System.Drawing.Point(201, 176);
            this.btnDeleteFolder.Name = "btnDeleteFolder";
            this.btnDeleteFolder.Size = new System.Drawing.Size(76, 23);
            this.btnDeleteFolder.TabIndex = 2;
            this.btnDeleteFolder.Text = "Delete folder";
            this.btnDeleteFolder.UseVisualStyleBackColor = true;
            this.btnDeleteFolder.Click += new System.EventHandler(this.btnDeleteFolder_Click);
            // 
            // btnSaveAndClose
            // 
            this.btnSaveAndClose.Location = new System.Drawing.Point(568, 230);
            this.btnSaveAndClose.Name = "btnSaveAndClose";
            this.btnSaveAndClose.Size = new System.Drawing.Size(47, 24);
            this.btnSaveAndClose.TabIndex = 3;
            this.btnSaveAndClose.Text = "OK";
            this.btnSaveAndClose.UseVisualStyleBackColor = true;
            this.btnSaveAndClose.Click += new System.EventHandler(this.btnSaveAndClose_Click);
            // 
            // lblCaption
            // 
            this.lblCaption.BackColor = System.Drawing.Color.Transparent;
            this.lblCaption.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCaption.Location = new System.Drawing.Point(13, 9);
            this.lblCaption.Name = "lblCaption";
            this.lblCaption.Size = new System.Drawing.Size(602, 24);
            this.lblCaption.TabIndex = 4;
            this.lblCaption.Text = "Choose the folders where you keep .wtv or .dvr-ms files:";
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(12, 230);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(76, 24);
            this.btnCancel.TabIndex = 5;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // btnSetDefault
            // 
            this.btnSetDefault.Location = new System.Drawing.Point(269, 230);
            this.btnSetDefault.Name = "btnSetDefault";
            this.btnSetDefault.Size = new System.Drawing.Size(75, 24);
            this.btnSetDefault.TabIndex = 6;
            this.btnSetDefault.Text = "Default";
            this.btnSetDefault.UseVisualStyleBackColor = true;
            this.btnSetDefault.Click += new System.EventHandler(this.btnSetDefault_Click);
            // 
            // cbRecurseFolders
            // 
            this.cbRecurseFolders.AutoSize = true;
            this.cbRecurseFolders.BackColor = System.Drawing.Color.Transparent;
            this.cbRecurseFolders.Location = new System.Drawing.Point(428, 180);
            this.cbRecurseFolders.Name = "cbRecurseFolders";
            this.cbRecurseFolders.Size = new System.Drawing.Size(187, 17);
            this.cbRecurseFolders.TabIndex = 7;
            this.cbRecurseFolders.Text = "Include subfolders of these folders";
            this.cbRecurseFolders.UseVisualStyleBackColor = false;
            // 
            // cbUseWin7LibraryInstead
            // 
            this.cbUseWin7LibraryInstead.AutoSize = true;
            this.cbUseWin7LibraryInstead.BackColor = System.Drawing.Color.Transparent;
            this.cbUseWin7LibraryInstead.Location = new System.Drawing.Point(319, 203);
            this.cbUseWin7LibraryInstead.Name = "cbUseWin7LibraryInstead";
            this.cbUseWin7LibraryInstead.Size = new System.Drawing.Size(230, 17);
            this.cbUseWin7LibraryInstead.TabIndex = 8;
            this.cbUseWin7LibraryInstead.Text = "Use my Windows 7 library instead of this list";
            this.cbUseWin7LibraryInstead.UseVisualStyleBackColor = false;
            // 
            // btnHelpWin7Libraries
            // 
            this.btnHelpWin7Libraries.BackColor = System.Drawing.Color.Transparent;
            this.btnHelpWin7Libraries.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnHelpWin7Libraries.Location = new System.Drawing.Point(548, 203);
            this.btnHelpWin7Libraries.Name = "btnHelpWin7Libraries";
            this.btnHelpWin7Libraries.Size = new System.Drawing.Size(66, 14);
            this.btnHelpWin7Libraries.TabIndex = 15;
            this.btnHelpWin7Libraries.TabStop = true;
            this.btnHelpWin7Libraries.Text = "What\'s this?";
            this.btnHelpWin7Libraries.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.btnHelpWin7Libraries.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.btnHelpWin7Libraries_LinkClicked);
            // 
            // lvRecTVFolders
            // 
            this.lvRecTVFolders.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lvRecTVFolders.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1});
            this.lvRecTVFolders.FullRowSelect = true;
            this.lvRecTVFolders.GridLines = true;
            this.lvRecTVFolders.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.lvRecTVFolders.HideSelection = false;
            this.lvRecTVFolders.Location = new System.Drawing.Point(12, 36);
            this.lvRecTVFolders.MultiSelect = false;
            this.lvRecTVFolders.Name = "lvRecTVFolders";
            this.lvRecTVFolders.Size = new System.Drawing.Size(603, 134);
            this.lvRecTVFolders.TabIndex = 16;
            this.lvRecTVFolders.UseCompatibleStateImageBehavior = false;
            this.lvRecTVFolders.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Name";
            this.columnHeader1.Width = 580;
            // 
            // btnAddPath
            // 
            this.btnAddPath.Location = new System.Drawing.Point(93, 176);
            this.btnAddPath.Name = "btnAddPath";
            this.btnAddPath.Size = new System.Drawing.Size(102, 23);
            this.btnAddPath.TabIndex = 17;
            this.btnAddPath.Text = "Add network path";
            this.btnAddPath.UseVisualStyleBackColor = true;
            this.btnAddPath.Click += new System.EventHandler(this.btnAddPath_Click);
            // 
            // FormFoldersCollection
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImage = global::RemotePotatoServer.Properties.Resources.tabControlBGGif;
            this.ClientSize = new System.Drawing.Size(627, 268);
            this.Controls.Add(this.btnAddPath);
            this.Controls.Add(this.lvRecTVFolders);
            this.Controls.Add(this.btnHelpWin7Libraries);
            this.Controls.Add(this.cbUseWin7LibraryInstead);
            this.Controls.Add(this.cbRecurseFolders);
            this.Controls.Add(this.btnSetDefault);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.lblCaption);
            this.Controls.Add(this.btnSaveAndClose);
            this.Controls.Add(this.btnDeleteFolder);
            this.Controls.Add(this.btnAddFolder);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormFoldersCollection";
            this.Text = "Recorded TV Folders";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnAddFolder;
        private System.Windows.Forms.Button btnDeleteFolder;
        private System.Windows.Forms.Button btnSaveAndClose;
        private System.Windows.Forms.Label lblCaption;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnSetDefault;
        private System.Windows.Forms.CheckBox cbRecurseFolders;
        private System.Windows.Forms.CheckBox cbUseWin7LibraryInstead;
        private System.Windows.Forms.LinkLabel btnHelpWin7Libraries;
        private System.Windows.Forms.ListView lvRecTVFolders;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.Button btnAddPath;
    }
}