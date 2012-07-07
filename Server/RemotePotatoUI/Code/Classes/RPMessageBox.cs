using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace RemotePotatoServer
{
    class RPMessageBox
    {

        // STATIC: methods to call it
        public static void Show(string txtMessage)
        {
            Show(txtMessage, "Remote Potato", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        public static void Show(string txtMessage, string txtCaption)
        {
            Show(txtMessage, txtCaption, System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
        }
        public static void Show(string txtMessage, string txtCaption, System.Windows.Forms.MessageBoxButtons buttons)
        {
            Show(txtMessage, txtCaption, buttons, System.Windows.Forms.MessageBoxIcon.Information);
        }
        public static void Show(string txtMessage, string txtCaption, System.Windows.Forms.MessageBoxButtons buttons, System.Windows.Forms.MessageBoxIcon icon)
        {
            System.Windows.Forms.MessageBox.Show(txtMessage, txtCaption, buttons, icon);
        }

        public static void ShowAlert(string txtMessage)
        {
            Show(txtMessage,"Remote Potato", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Exclamation);
        }
        public static void ShowWarning(string txtMessage)
        {
            Show(txtMessage, "Remote Potato", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
        }
        public static DialogResult ShowQuestion(string txtMessage, string txtCaption)
        {
            return MessageBox.Show(txtMessage, txtCaption, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        }
        public static DialogResult ShowQuestionWithTimeout(string txtMessage, string txtCaption, uint timeOutMS)
        {
            return MessageBoxWithTimeout.Show(txtMessage, txtCaption, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2, timeOutMS);
        }

        // INSTANCE: the actual box.

    }

    
}
