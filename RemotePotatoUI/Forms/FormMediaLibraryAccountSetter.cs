using System;
using System.Data;
using System.Text;
using System.Windows.Forms;
using RemotePotatoServer.Properties;
using System.DirectoryServices.AccountManagement;
 


namespace RemotePotatoServer
{
    public partial class FormMediaLibraryAccountSetter : Form
    {
        string LastUsername;

        public FormMediaLibraryAccountSetter()
        {
            InitializeComponent();
        }

        private void FormMediaLibraryAccountSetter_Load(object sender, EventArgs e)
        {
            LastUsername = ServiceManager.RPServiceAccountName;
            lblStatus.Text = "Current username: " + (LastUsername.Equals("LocalUserAccount") ? "<unset>" : LastUsername);

            if (LastUsername.Equals("LocalSystem"))
            {
                if (Environment.UserName != "SYSTEM")
                    txtMediaLibraryUsername.Text = Environment.UserName;
            }
            else
                txtMediaLibraryUsername.Text = LastUsername;

            txtMediaLibraryPassword.Text = string.Empty;
        }


        void BindControls()
        {
            //this.txtMediaLibraryUsername.DataBindings.Add(new System.Windows.Forms.Binding("Text", Settings.Default, "MediaLibraryUsername", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
        }


        #region Control Buttons
        private void btnCancel_Click(object sender, EventArgs e)
        {
            RevertAndClose();
        }
        private void btnSetNewAccount_Click(object sender, EventArgs e)
        {
            TryToChangeMediaLibraryAccount();
        }
        private void txtMediaLibraryPassword_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                TryToChangeMediaLibraryAccount();
        }
        #endregion

        #region Set Service Logon


        bool ValidateCredentials(string Username, string Password)
        {
            if (Functions.isXP)
            {
                // For now just return true
                return true;
            }
            else
            {

                using (PrincipalContext context = new PrincipalContext(ContextType.Machine))
                {
                    try
                    {
                        return context.ValidateCredentials(Username, Password);
                    }
                    catch (PrincipalOperationException pex)
                    {
                        // Blank password - that's ok
                        if (pex.ErrorCode == -2147023569)
                        {
                            if (string.IsNullOrEmpty(Password))
                            {
                                Functions.WriteLineToLogFile("ValidateCredentials: Exception relating to blank password - returning TRUE...");
                                return true;
                            }
                        }
                    }
                }

                
            }

            return false;
        }

        void TryToChangeMediaLibraryAccount()
        {
            lblStatus.Text = "Please wait - making changes...";
            Application.DoEvents();

            string AccountName = txtMediaLibraryUsername.Text.Trim();
            string Password = txtMediaLibraryPassword.Text.Trim();
            if (string.IsNullOrEmpty(AccountName))
            {
                lblStatus.Text = "You must enter the details of a user of this machine.";
                return;
            }

            if (string.IsNullOrEmpty(Password))
            {
                // Services require a login with a password if limited in registry  see http://stackoverflow.com/questions/1047854/cant-run-a-service-under-an-account-which-has-no-password
                if (Functions.IsBlankPasswordLimitedOnMachine) 
                {
                    if (RPMessageBox.ShowQuestion("Blank passwords for services are currently limited on this machine.\r\n\r\nDo you wish to lift this security restriction so that Remote Potato can run?", "Allow blank passwords on machine?")
                        == DialogResult.Yes)
                    {
                        Functions.SetBlankPasswordLimitOnMachine(false);

                    }
                    else
                    {
                        RPMessageBox.Show("To allow Remote Potato to run, please use an account that has a Username and a Password");
                        return;
                    }
                }
            }
            else  // re-instate blank password limit?
            {
                if (!Functions.IsBlankPasswordLimitedOnMachine)
                {
                    if (RPMessageBox.ShowQuestion("Blank passwords for services are currently allowed on this machine.  Unless you require this security relaxation for other services or remotely access user accounts on this machine that have no password, you should re-enable this security restriction.\r\n\r\nDo you wish to forbid blank passwords for services?", "Forbid blank passwords on machine?")
    == DialogResult.Yes)
                    {
                        Functions.SetBlankPasswordLimitOnMachine(true);

                    }
                }
            }

            if (! ValidateCredentials(AccountName, Password))
            {
                lblStatus.Text = "The log on details that you entered were incorrect.";
                return;
            }



            //if (!SetServiceLogon("Remote Potato Service", AccountName, Password, true)) return;
            string strErrorText = string.Empty;
            if (! ServiceManager.SetRPServiceLogon(AccountName, Password, false, ref strErrorText ))
            {
                lblStatus.Text = "Could not use this account - please check the details.";
                return;
            }

            if (! SetLogOnAsServiceRight(AccountName))
            {
                RPMessageBox.ShowAlert("Could not add required security priviliges to this account - please check that you running as an Administrator, and that you have entered the details correctly.");
                lblStatus.Text = "Error setting priviliges.";
                return;
            }

            lblStatus.Text = string.Empty;
            CommitAndClose();
        }

        bool SetLogOnAsServiceRight(string strAccountName)
        {
            return (AccountTools.LsaUtility.SetRight(strAccountName, "SeServiceLogonRight") == 0);
        }

        void CommitAndClose()
        {
            this.DialogResult = DialogResult.Yes;
        }
        #endregion

        void RevertAndClose()
        {
            this.DialogResult = DialogResult.Cancel;
        }

        
    }
}
