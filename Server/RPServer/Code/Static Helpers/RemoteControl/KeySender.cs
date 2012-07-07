// SEND KEYS USING WINFORMS SENDKEYS() METHOD  - NO GOOD COS IT CAN'T DO WINDOWS KEY HELD DOWN
// (C)2011 FatAttitude

using System.Windows.Forms;
using System.IO;

namespace RemotePotatoServer.RemoteInput
{
    public class RemoteSender
    {
        public const string MEDIA_CENTER_PATH = @"C:\Windows\ehome\ehshell.exe";

        public static void SendMediaCenterCommand(MCCommands cmd)
        {
            switch (cmd)
            {
                case MCCommands.Play: // Ctrl Shift P
                    SendKeyStroke("P", true, true);
                    break;

                case MCCommands.Pause:
                    SendKeyStroke("P", true);
                    break;

                case MCCommands.Stop:
                    SendKeyDown(VK.VK_CONTROL);
                    SendKeyDown(VK.VK_SHIFT);
                    SendKeyStroke(VK.VK_LETTER_S);
                    SendKeyUp(VK.VK_SHIFT);
                    SendKeyUp(VK.VK_CONTROL);
                    break;

                case MCCommands.Ffw:
                    SendKeyDown(VK.VK_CONTROL);
                    SendKeyDown(VK.VK_SHIFT);
                    SendKeyStroke(VK.VK_LETTER_F);
                    SendKeyUp(VK.VK_SHIFT);
                    SendKeyUp(VK.VK_CONTROL);
                    break;

                case MCCommands.Rew:
                    SendKeyDown(VK.VK_CONTROL);
                    SendKeyDown(VK.VK_SHIFT);
                    SendKeyStroke(VK.VK_LETTER_R);
                    SendKeyUp(VK.VK_SHIFT);
                    SendKeyUp(VK.VK_CONTROL);
                    break;

                case MCCommands.SkipFwd:
                    SendKeyDown(VK.VK_CONTROL);
                    SendKeyStroke(VK.VK_LETTER_F);
                    SendKeyUp(VK.VK_CONTROL);
                    break;

                case MCCommands.SkipBack:
                    SendKeyDown(VK.VK_CONTROL);
                    SendKeyStroke(VK.VK_LETTER_B);
                    SendKeyUp(VK.VK_CONTROL);
                    break;

                case MCCommands.Record:
                    SendKeyDown(VK.VK_CONTROL);
                    SendKeyStroke(VK.VK_LETTER_R);
                    SendKeyUp(VK.VK_CONTROL);
                    break;

                case MCCommands.NavUp:
                    SendKeyStroke("{UP}");
                    break;

                case MCCommands.NavDown:
                    SendKeyStroke("{DOWN}");
                    break;

                case MCCommands.NavLeft:
                    SendKeyStroke("{LEFT}");
                    break;

                case MCCommands.NavRight:
                    SendKeyStroke("{RIGHT}");
                    break;

                case MCCommands.NavBack:
                    SendKeyStroke(VK.VK_BACK);
                    break;

                case MCCommands.Menu:
                    
                    SendKeyStroke("({ESC}{ENTER})", true, false, true);
                    break;

                case MCCommands.Info:
                    SendKeyDown(VK.VK_CONTROL);
                    SendKeyStroke(VK.VK_LETTER_D);
                    SendKeyUp(VK.VK_CONTROL);
                    break;

                case MCCommands.DVDMenu:
                    SendKeyDown(VK.VK_CONTROL);
                    SendKeyDown(VK.VK_SHIFT);
                    SendKeyStroke(VK.VK_LETTER_M);
                    SendKeyUp(VK.VK_SHIFT);
                    SendKeyUp(VK.VK_CONTROL);
                    break;

                case MCCommands.OK:
                    SendKeyStroke(VK.VK_RETURN);
                    break;

                case MCCommands.Clear:

                    break;

                case MCCommands.Enter:
                    SendKeyStroke(VK.VK_RETURN);
                    break;

                case MCCommands.VolUp:
                    SendKeyStroke(VK.VK_VOLUME_UP);
                    break;

                case MCCommands.VolDown:
                    SendKeyStroke(VK.VK_VOLUME_DOWN);
                    break;

                case MCCommands.VolMute:
                    SendKeyStroke(VK.VK_VOLUME_MUTE);
                    break;

                case MCCommands.ChanUp:
                    SendKeyStroke(VK.VK_PRIOR);
                    break;

                case MCCommands.ChanDown:
                    SendKeyStroke(VK.VK_NEXT);
                    break;

                case MCCommands.Num0:
                    SendKeyStroke(VK.VK_NUMPAD0);
                    break;

                case MCCommands.Num1:
                    SendKeyStroke(VK.VK_NUMPAD1);
                    break;

                case MCCommands.Num2:
                    SendKeyStroke(VK.VK_NUMPAD2);
                    break;

                case MCCommands.Num3:
                    SendKeyStroke(VK.VK_NUMPAD3);
                    break;

                case MCCommands.Num4:
                    SendKeyStroke(VK.VK_NUMPAD4);
                    break;

                case MCCommands.Num5:
                    SendKeyStroke(VK.VK_NUMPAD5);
                    break;

                case MCCommands.Num6:
                    SendKeyStroke(VK.VK_NUMPAD6);
                    break;

                case MCCommands.Num7:
                    SendKeyStroke(VK.VK_NUMPAD7);
                    break;

                case MCCommands.Num8:
                    SendKeyStroke(VK.VK_NUMPAD8);
                    break;

                case MCCommands.Num9:
                    SendKeyStroke(VK.VK_NUMPAD9);
                    break;

                case MCCommands.NumHash:

                    break;

                case MCCommands.NumStar:
                    SendKeyStroke(VK.VK_MULTIPLY);
                    break;

                case MCCommands.Text:

                    break;

                case MCCommands.TextRed:

                    break;

                case MCCommands.TextGreen:

                    break;

                case MCCommands.TextYellow:

                    break;

                case MCCommands.TextBlue:

                    break;

                case MCCommands.Subtitles:
                    SendKeyDown(VK.VK_CONTROL);
                    SendKeyStroke(VK.VK_LETTER_U);
                    SendKeyUp(VK.VK_CONTROL);
                    break;

                case MCCommands.GotoLiveTV:
                    SendKeyDown(VK.VK_CONTROL);
                    SendKeyStroke(VK.VK_LETTER_T);
                    SendKeyUp(VK.VK_CONTROL);
                    break;

                case MCCommands.GotoGuide:
                    SendKeyDown(VK.VK_CONTROL);
                    SendKeyStroke(VK.VK_LETTER_G);
                    SendKeyUp(VK.VK_CONTROL);
                    break;

                case MCCommands.GotoRecTV:
                    SendKeyDown(VK.VK_CONTROL);
                    SendKeyStroke(VK.VK_LETTER_O);
                    SendKeyUp(VK.VK_CONTROL);
                    break;

                case MCCommands.GotoPictures:
                    SendKeyDown(VK.VK_CONTROL);
                    SendKeyStroke(VK.VK_LETTER_I);
                    SendKeyUp(VK.VK_CONTROL);
                    break;

                case MCCommands.GotoVideos:
                    SendKeyStroke("E", true);
                    break;

                case MCCommands.GotoMusic:
                    SendKeyStroke("M", true);
                    break;

                case MCCommands.GotoMovies:
                    SendKeyDown(VK.VK_CONTROL);
                    SendKeyDown(VK.VK_SHIFT);
                    SendKeyStroke(VK.VK_LETTER_M);
                    SendKeyUp(VK.VK_SHIFT);
                    SendKeyUp(VK.VK_CONTROL);
                    break;

                case MCCommands.GotoRadio:
                    SendKeyDown(VK.VK_CONTROL);
                    SendKeyStroke(VK.VK_LETTER_A);
                    SendKeyUp(VK.VK_CONTROL);
                    break;

                case MCCommands.GreenButton:
                    SendKeyDown(VK.VK_LWIN);
                    SendKeyDown(VK.VK_MENU);
                    SendKeyStroke(VK.VK_RETURN);
                    SendKeyUp(VK.VK_LWIN);
                    SendKeyUp(VK.VK_MENU);
                    break;

                case MCCommands.Power:
                    if (File.Exists(MEDIA_CENTER_PATH))
                    {
                        System.Diagnostics.Process.Start(MEDIA_CENTER_PATH);
                    }
                    /*
                     *             SendKeyDown(VK.VK_MENU);
            SendKeyStroke(VK.VK_F4);
            SendKeyUp(VK.VK_MENU);
                     */
                    break;
            }
        }

        static void SendKeyDown(VK k)
        {
        }
        static void SendKeyStroke(VK k)
        {
        }
        static void SendKeyUp(VK k)
        {
        }


        #region Low Level Key Sending

        static void SendKeyStroke(string strKey)
        {
            SendKeyStroke(strKey, false, false, false);
        }
        static void SendKeyStroke(string strKey, bool Ctrl)
        {
            SendKeyStroke(strKey, Ctrl, false, false);
        }
        static void SendKeyStroke(string strKey, bool Ctrl, bool Shift)
        {
            SendKeyStroke(strKey, Ctrl, Shift, false);
        }
        static void SendKeyStroke(string strKey, bool Ctrl, bool Shift, bool Alt)
        {
            string keyString = "";
            if (Ctrl) keyString = "^" + keyString;
            if (Shift) keyString = "+" + keyString;
            if (Alt) keyString = "%" + keyString;
            keyString += strKey;

            SendKeys.SendWait(keyString);
        }
        #endregion

    }
}
