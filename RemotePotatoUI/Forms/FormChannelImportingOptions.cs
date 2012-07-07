using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using CommonEPG;
using RemotePotatoServer.Properties;

namespace RemotePotatoServer
{
    public partial class FormChannelImportingOptions : Form
    {
        public FormChannelImportingOptions()
        {
            InitializeComponent();


        }
        private void FormChannelImportingOptions_Load(object sender, EventArgs e)
        {
            BindControls();

            
        }


        void BindControls()
        {
            this.cbBlockChannelsUserHidden.DataBindings.Add(new System.Windows.Forms.Binding("Checked", Settings.Default, "BlockChannelsUserHidden", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.cbImportHiddenChannels.DataBindings.Add(new System.Windows.Forms.Binding("Checked", Settings.Default, "ImportHiddenTVChannels", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.cbBlockChannelsUserMapped.DataBindings.Add(new System.Windows.Forms.Binding("Checked", Settings.Default, "BlockChannelsUserMapped", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.checkBox4.DataBindings.Add(new System.Windows.Forms.Binding("Checked", Settings.Default, "ImportInternetTVChannels", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.cbBlockChannelsUserAdded.DataBindings.Add(new System.Windows.Forms.Binding("Checked", Settings.Default, "BlockChannelsUserAdded", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.cbBlockChannelsUnknown.DataBindings.Add(new System.Windows.Forms.Binding("Checked", Settings.Default, "BlockChannelsUnknown", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.cbMergeLineUps.DataBindings.Add(new System.Windows.Forms.Binding("Checked", Settings.Default, "MergeLineUps", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
          
        }


        private void btnCloseMe_Click(object sender, EventArgs e)
        {
            CloseMe();
        }

        private void CloseMe()
        {
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
        }

      


    }
}
