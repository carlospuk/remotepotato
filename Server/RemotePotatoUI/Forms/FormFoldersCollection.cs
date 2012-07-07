using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Collections.Specialized;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using RemotePotatoServer.Properties;

namespace RemotePotatoServer
{
    public partial class FormFoldersCollection : Form
    {
        public StringCollection Folders { get; set; }
        string CollectionName;

        const int MAX_FOLDERS = 100;

        public FormFoldersCollection(StringCollection initialFolders, bool showRecurseCheckbox, bool recurseCheckboxDefaultValue, bool showDefaultRecTVButton, bool showWin7LibraryCheckbox, bool Win7LibraryCheckboxDefaultValue,
            string strLabelText, string strFormCaption, string strCollectionName)
        {
            InitializeComponent();

            BindControls();

            CollectionName = strCollectionName;

            // GUI
            cbUseWin7LibraryInstead.Visible = showWin7LibraryCheckbox;
            btnHelpWin7Libraries.Visible = showWin7LibraryCheckbox;
            if (showWin7LibraryCheckbox)
                cbUseWin7LibraryInstead.Checked = Win7LibraryCheckboxDefaultValue;

            lblCaption.Text = strLabelText;
            cbRecurseFolders.Visible = showRecurseCheckbox;
            if (showRecurseCheckbox)
                cbRecurseFolders.Checked = recurseCheckboxDefaultValue;
            
            btnSetDefault.Visible = showDefaultRecTVButton;
            this.Text = strFormCaption;

            // Clone
            Folders = new StringCollection();
            if (initialFolders != null)
            {
                foreach (string str in initialFolders)
                {
                    Folders.Add(str);
                }
            }

            populateListBox();

        }

        private void populateListBox()
        {
            lvRecTVFolders.Items.Clear();
            foreach (string s in Folders)
            {
                lvRecTVFolders.Items.Add(s);
            }
        }

        // Add a folder and refresh box
        void addFolder(string txtPath)
        {
            if (string.IsNullOrEmpty(txtPath)) return;
            if (listviewContainsText(txtPath))
            {
                RPMessageBox.ShowAlert("You have already added this folder.");
                return;
            }

            Folders.Add(txtPath);
            lvRecTVFolders.Items.Add(txtPath);
        }
        bool listviewContainsText(string txtSearch)
        {
            foreach (ListViewItem lvi in lvRecTVFolders.Items)
            {
                if (lvi.Text.Equals(txtSearch)) return true;
            }
            return false;
        }

        void removeSelectedFolder()
        {
            if (lvRecTVFolders.SelectedIndices.Count < 1) return; // nothing selected

            int index = lvRecTVFolders.SelectedIndices[0];
            string strItem = lvRecTVFolders.SelectedItems[0].Text;

            StringCollection newCollection = new StringCollection();
            foreach (string s in Folders)
            {
                if (!s.Equals(strItem))
                    newCollection.Add(s);
            }

            Folders.Clear();
            foreach (string s in newCollection) { Folders.Add(s); }

            // Check there's at least one
            if (Folders.Count < 1)
                Folders.Add(@"C:\");

            // Replace list
            populateListBox();
        }

        void AddFolder()
        {
            if (lvRecTVFolders.Items.Count > (MAX_FOLDERS - 1) )
            {
                RPMessageBox.ShowAlert("You may only add up to " + MAX_FOLDERS.ToString() + " folders.");
                return;
            }

            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.ShowNewFolderButton = true;

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                addFolder(dialog.SelectedPath);
            }
        }
        void AddPath()
        {
            if (lvRecTVFolders.Items.Count > (MAX_FOLDERS - 1))
            {
                RPMessageBox.ShowAlert("You may only add up to " + MAX_FOLDERS.ToString() + " folders.");
                return;
            }

          
            FormInputBox fInputBox = new FormInputBox("Add path to folder", "Enter the path to a folder - e.g. \\\\SERVER\\SHARENAME", "\\SERVER\\SHARE_NAME");

            if (fInputBox.ShowDialog() == DialogResult.OK)
            {
                if (! (string.IsNullOrWhiteSpace(  fInputBox.Value) ) )
                    addFolder(fInputBox.Value);
            }
        }



        private void SetDefaultRecordingPath()
        {
            string recPath = Functions.GetRecordPath();
            if (!String.IsNullOrEmpty(recPath))
            {
                Folders = new System.Collections.Specialized.StringCollection();
                Folders.Add(recPath);
            }
            else
            {
                Folders = new System.Collections.Specialized.StringCollection();
                Folders.Add(@"C:\");
            }

            populateListBox();
        }


        #region Button Clicks
        private void btnAddFolder_Click(object sender, EventArgs e)
        {
            AddFolder();
        }
        private void btnAddPath_Click(object sender, EventArgs e)
        {
            AddPath();
        }

        private void btnSaveAndClose_Click(object sender, EventArgs e)
        {
            

            
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }


        private void btnDeleteFolder_Click(object sender, EventArgs e)
        {
            removeSelectedFolder();
        }
        #endregion

        private void btnSetDefault_Click(object sender, EventArgs e)
        {
            if (RPMessageBox.ShowQuestion("This will remove all entries and replace them with the default recording folder - are you sure?", "Revert to default recording path") == DialogResult.Yes)
                SetDefaultRecordingPath();
        }

        #region Properties to retrieve control settings from parent
        public bool IsRecurseCheckboxChecked
        {
            get
            {
                return (cbRecurseFolders.Checked);
            }
        }
        public bool IsUseWin7LibraryCheckboxChecked
        {
            get
            {
                return (cbUseWin7LibraryInstead.Checked);
            }
        }
        #endregion

        #region Control Data Bindings
        void RemoveControlBindings(Control parentControl)
        {
            foreach (Control ctrl in parentControl.Controls)
            {
                if (ctrl.Controls.Count > 0)
                    RemoveControlBindings(ctrl);

                while (ctrl.DataBindings.Count > 0)
                {
                    ctrl.DataBindings.RemoveAt(0);
                }
            }
        }
        void BindControls()
        {
            Binding bind = new System.Windows.Forms.Binding("Enabled", cbUseWin7LibraryInstead, "Checked", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged);
            bind.Format += SwitchBool;
            bind.Parse += SwitchBool;
            this.lvRecTVFolders.DataBindings.Add(bind);
            
            Binding bind2 = new System.Windows.Forms.Binding("Enabled", cbUseWin7LibraryInstead, "Checked", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged);
            bind2.Format += SwitchBool;
            bind2.Parse += SwitchBool;
            this.btnAddFolder.DataBindings.Add(bind2);

            Binding bind3 = new System.Windows.Forms.Binding("Enabled", cbUseWin7LibraryInstead, "Checked", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged);
            bind3.Format += SwitchBool;
            bind3.Parse += SwitchBool;
            this.btnDeleteFolder.DataBindings.Add(bind3);

        }
        private void SwitchBool(object sender, ConvertEventArgs e)
        {
            e.Value = !((bool)e.Value);
        }

        #endregion

        private void btnHelpWin7Libraries_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (RPMessageBox.ShowQuestion("If this box is checked, Remote Potato ignores this list and uses the folders in your Windows 7 " + CollectionName + " library instead.\r\n\r\nDo you wish to edit these folders now?", "Help on Windows 7 " + CollectionName + " library") == System.Windows.Forms.DialogResult.Yes)
            {
                RPMessageBox.ShowAlert("Remote Potato will open a Windows Explorer window.  To add/remove " + CollectionName + " folders, you should look for the heading 'Libraries' in the left-hand column, then right-click the word '" + CollectionName + "' and choose 'Properties'.");
                string target = "explorer.exe";
                System.Diagnostics.Process.Start(target);
            }
        }

        



 

    }
}
