// SEND KEYS USING WINFORMS SENDKEYS() METHOD  - NO GOOD COS IT CAN'T DO WINDOWS KEY HELD DOWN
// (C)2011 FatAttitude

using System.Windows.Forms;
using System.IO;
using WindowsInput;

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
                    InputSimulator.SimulateModifiedKeyStroke(new[] { VirtualKeyCode.CONTROL, VirtualKeyCode.SHIFT }, VirtualKeyCode.VK_P);
                    break;

                case MCCommands.Pause:
                    InputSimulator.SimulateModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_P);
                    break;

                case MCCommands.Stop:
                    InputSimulator.SimulateModifiedKeyStroke(new[] { VirtualKeyCode.CONTROL, VirtualKeyCode.SHIFT }, VirtualKeyCode.VK_S);
                    break;

                case MCCommands.Ffw:
                    InputSimulator.SimulateModifiedKeyStroke(new[] { VirtualKeyCode.CONTROL, VirtualKeyCode.SHIFT }, VirtualKeyCode.VK_F);
                    break;

                case MCCommands.Rew:
                    InputSimulator.SimulateModifiedKeyStroke(new[] { VirtualKeyCode.CONTROL, VirtualKeyCode.SHIFT }, VirtualKeyCode.VK_B);
                    break;

                case MCCommands.SkipFwd:
                    InputSimulator.SimulateModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_F);
                    break;

                case MCCommands.SkipBack:
                    InputSimulator.SimulateModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_B);
                    break;

                case MCCommands.Record:
                    InputSimulator.SimulateModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_R);
                    break;

                case MCCommands.NavUp:
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.UP);
                    break;

                case MCCommands.NavDown:
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.DOWN);
                    break;

                case MCCommands.NavLeft:
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.LEFT);
                    break;

                case MCCommands.NavRight:
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RIGHT);
                    break;

                case MCCommands.NavBack:
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.BACK);
                    break;

                case MCCommands.Menu:
                    InputSimulator.SimulateKeyDown(VirtualKeyCode.LWIN);
                    InputSimulator.SimulateKeyDown(VirtualKeyCode.MENU);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RETURN);
                    InputSimulator.SimulateKeyUp(VirtualKeyCode.MENU);
                    InputSimulator.SimulateKeyUp(VirtualKeyCode.LWIN);
                    break;

                case MCCommands.Info:
                    InputSimulator.SimulateModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_D);
                    break;

                case MCCommands.DVDMenu:
                    InputSimulator.SimulateModifiedKeyStroke(new[] { VirtualKeyCode.CONTROL, VirtualKeyCode.SHIFT}, VirtualKeyCode.VK_M);
                    break;

                case MCCommands.DVDAudio:
                    InputSimulator.SimulateModifiedKeyStroke(new[] { VirtualKeyCode.CONTROL, VirtualKeyCode.SHIFT }, VirtualKeyCode.VK_A);
                    break;

                case MCCommands.OK:
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RETURN);
                    break;

                case MCCommands.Clear:

                    break;

                case MCCommands.Enter:
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RETURN);
                    break;

                case MCCommands.VolUp:
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.VOLUME_UP);
                    break;

                case MCCommands.VolDown:
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.VOLUME_DOWN);
                    break;

                case MCCommands.VolMute:
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.VOLUME_MUTE);
                    break;

                case MCCommands.ChanUp:
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.PRIOR);
                    break;

                case MCCommands.ChanDown:
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.NEXT);
                    break;

                case MCCommands.Num0:
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.VK_9);
                    break;

                case MCCommands.Num1:
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.VK_9);
                    break;

                case MCCommands.Num2:
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.VK_9);
                    break;

                case MCCommands.Num3:
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.VK_9);
                    break;

                case MCCommands.Num4:
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.VK_9);
                    break;

                case MCCommands.Num5:
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.VK_9);
                    break;

                case MCCommands.Num6:
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.VK_9);
                    break;

                case MCCommands.Num7:
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.VK_9);
                    break;

                case MCCommands.Num8:
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.VK_9);
                    break;

                case MCCommands.Num9:
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.VK_9);
                    break;

                case MCCommands.NumHash:
                    InputSimulator.SimulateTextEntry("#");
                    break;

                case MCCommands.NumStar:
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.MULTIPLY);
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
                    InputSimulator.SimulateModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_U);
                    break;

                case MCCommands.GotoLiveTV:
                    InputSimulator.SimulateModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_T);
                    break;

                case MCCommands.GotoGuide:
                    InputSimulator.SimulateModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_G);
                    break;

                case MCCommands.GotoRecTV:
                    InputSimulator.SimulateModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_O);
                    break;

                case MCCommands.GotoPictures:
                    InputSimulator.SimulateModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_I);
                    break;

                case MCCommands.GotoVideos:
                    InputSimulator.SimulateModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_E);
                    break;

                case MCCommands.GotoMusic:
                    InputSimulator.SimulateModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_M);
                    break;

                case MCCommands.GotoMovies:
                    InputSimulator.SimulateModifiedKeyStroke(new [] {VirtualKeyCode.CONTROL, VirtualKeyCode.SHIFT}, VirtualKeyCode.VK_M);
                    break;

                case MCCommands.GotoRadio:
                    InputSimulator.SimulateModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_A);
                    break;


                case MCCommands.GotoExtras:
                    InputSimulator.SimulateModifiedKeyStroke(new[] { VirtualKeyCode.CONTROL, VirtualKeyCode.SHIFT }, VirtualKeyCode.VK_R);
                    break;

                case MCCommands.GreenButton:
                    InputSimulator.SimulateKeyDown(VirtualKeyCode.LWIN);
                    InputSimulator.SimulateKeyDown(VirtualKeyCode.MENU);
                    InputSimulator.SimulateKeyPress(VirtualKeyCode.RETURN);
                    InputSimulator.SimulateKeyUp(VirtualKeyCode.MENU);
                    InputSimulator.SimulateKeyUp(VirtualKeyCode.LWIN);
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

    }
}
