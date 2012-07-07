using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace RemotePotatoServer
{
    public partial class FormInputBox : Form
    {
        public string Value { get; set; }

        public FormInputBox()
        {
            InitializeComponent();
        }

        public FormInputBox(string strWindowTitle, string strCaption, string strDefaultValue)
            : this()
        {
            this.Text = strWindowTitle;
            lblCaption.Text = strCaption;
            if (string.IsNullOrWhiteSpace(strDefaultValue))
                txtInputBox.Text = "";
            else
                txtInputBox.Text = strDefaultValue;
        }

        

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
        }

        private void txtInputBox_TextChanged(object sender, EventArgs e)
        {
            this.Value = txtInputBox.Text;
        }





    }
}
