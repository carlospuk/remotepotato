using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace SilverPotato
{
    public class LoginPageCompleteEventArgs : EventArgs
    {
        public readonly string UN;
        public readonly string PW;

        public LoginPageCompleteEventArgs(string un, string pw)
        {
            UN = un;
            PW = pw;
        }

    }
}
