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
    public static class ErrorManager
    {

        public static void DisplayError(string txtError)
        {
            MessageBox.Show(txtError, "Remote Potato", MessageBoxButton.OK);
        }
        public static void DisplayError(Exception e)
        {
            MessageBox.Show(e.Message);
        }

        public static void DisplayAndLogError(string txtError)
        {
            DisplayError(txtError);
            Functions.WriteLineToLogFile(txtError);
        }
        public static void DisplayAndLogError(Exception e)
        {
            DisplayError(e.Message);
            Functions.WriteExceptionToLogFile(e);
        }

    }
}
