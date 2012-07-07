using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace SilverPotato
{
    public partial class LoginPage : UserControl
    {
        public event EventHandler<LoginPageCompleteEventArgs> LoginPageDone;

        public LoginPage()
        {
            InitializeComponent();
            Loaded += new RoutedEventHandler(LoginPage_Loaded);
        }

        void LoginPage_Loaded(object sender, RoutedEventArgs e)
        {
            RetrieveCredentialsIfNecessary();
        }



        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {

            StoreRememberMe();
            DoLogin();

            FileManager.IncreaseStorageAsNecessary();  // Must be done on a button click
        }

        void RetrieveCredentialsIfNecessary()
        {
            if (Settings.RememberUserCredentials)
            {
                cbRememberMe.IsChecked = true;
                txtUsername.Text = Settings.StoredCredentialUsername;
                txtPassword.Password = Settings.StoredCredentialPassword;
            }
            else
            {
                cbRememberMe.IsChecked = false;
            }
        }
        void StoreRememberMe()
        {

            if (cbRememberMe.IsChecked.Value)
            {
                Settings.RememberUserCredentials = true;
                Settings.StoredCredentialUsername = txtUsername.Text.Trim();
                Settings.StoredCredentialPassword = txtPassword.Password.Trim();
            }
            else
            {
                Settings.RememberUserCredentials = false;
                Settings.StoredCredentialPassword = "";
                Settings.StoredCredentialUsername = "";
            }
        }

        void DoLogin()
        {
            if (LoginPageDone != null)
                LoginPageDone(this, new LoginPageCompleteEventArgs(txtUsername.Text.Trim(), txtPassword.Password.Trim()));
            
        }

        private void txtPassword_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                StoreRememberMe();
                DoLogin();
            }
        }





        
    }
}
