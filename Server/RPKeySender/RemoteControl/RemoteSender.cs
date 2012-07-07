/***********************************************************
 * 
 * Remote Sender Library V 1.0
 * Copyright (c) 2009 Michael Hurnaus - michbex@live.com
 * Feel free to use this source for whatever you want. Drop me a line if you found it useful!
 * http://www.michbex.com
 */

using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
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
                    SendKeyDown(VK.VK_CONTROL);
                    SendKeyDown(VK.VK_SHIFT);
                    SendKeyStroke(VK.VK_LETTER_P);
                    SendKeyUp(VK.VK_SHIFT);
                    SendKeyUp(VK.VK_CONTROL);
                break;

                case MCCommands.Pause:
                    SendKeyDown(VK.VK_CONTROL);
                    SendKeyStroke(VK.VK_LETTER_P);
                    SendKeyUp(VK.VK_CONTROL);
                break;

                case  MCCommands.Stop:
                    SendKeyDown(VK.VK_CONTROL);
                    SendKeyDown(VK.VK_SHIFT);
                    SendKeyStroke(VK.VK_LETTER_S);
                    SendKeyUp(VK.VK_SHIFT);
                    SendKeyUp(VK.VK_CONTROL);
                break;

                case  MCCommands.Ffw:
                    SendKeyDown(VK.VK_CONTROL);
                    SendKeyDown(VK.VK_SHIFT);
                    SendKeyStroke(VK.VK_LETTER_F);
                    SendKeyUp(VK.VK_SHIFT);
                    SendKeyUp(VK.VK_CONTROL);
                break;

                case  MCCommands.Rew:
                    SendKeyDown(VK.VK_CONTROL);
                    SendKeyDown(VK.VK_SHIFT);
                    SendKeyStroke(VK.VK_LETTER_R);
                    SendKeyUp(VK.VK_SHIFT);
                    SendKeyUp(VK.VK_CONTROL);
                break;

                case  MCCommands.SkipFwd:
                    SendKeyDown(VK.VK_CONTROL);
                    SendKeyStroke(VK.VK_LETTER_F);
                    SendKeyUp(VK.VK_CONTROL);
                break;

                case  MCCommands.SkipBack:
                    SendKeyDown(VK.VK_CONTROL);
                    SendKeyStroke(VK.VK_LETTER_B);
                    SendKeyUp(VK.VK_CONTROL);
                break;

                case  MCCommands.Record:
                    SendKeyDown(VK.VK_CONTROL);
                    SendKeyStroke(VK.VK_LETTER_R);
                    SendKeyUp(VK.VK_CONTROL);
                break;

                case  MCCommands.NavUp:
                    SendKeyStroke(VK.VK_UP);
                break;

                case  MCCommands.NavDown:
                    SendKeyStroke(VK.VK_DOWN);
                break;

                case  MCCommands.NavLeft:
                    SendKeyStroke(VK.VK_LEFT);
                break;

                case  MCCommands.NavRight:
                    SendKeyStroke(VK.VK_RIGHT);
                break;

                case  MCCommands.NavBack:
                    SendKeyStroke(VK.VK_BACK);
                break;

                case  MCCommands.Menu:
                    SendKeyDown(VK.VK_LWIN);
                    SendKeyDown(VK.VK_MENU);
                    SendKeyStroke(VK.VK_RETURN);
                    SendKeyUp(VK.VK_LWIN);
                    SendKeyUp(VK.VK_MENU);
                break;

                case  MCCommands.Info:
                    SendKeyDown(VK.VK_CONTROL);
                    SendKeyStroke(VK.VK_LETTER_D);
                    SendKeyUp(VK.VK_CONTROL);
                break;

                case  MCCommands.DVDMenu:
                    SendKeyDown(VK.VK_CONTROL);
                    SendKeyDown(VK.VK_SHIFT);
                    SendKeyStroke(VK.VK_LETTER_M);
                    SendKeyUp(VK.VK_SHIFT);
                    SendKeyUp(VK.VK_CONTROL);
                break;

                case  MCCommands.OK:
                    SendKeyStroke(VK.VK_RETURN);
                break;

                case  MCCommands.Clear:

                break;

                case  MCCommands.Enter:
                    SendKeyStroke(VK.VK_RETURN);
                break;

                case  MCCommands.VolUp:
                    SendKeyStroke(VK.VK_VOLUME_UP);
                break;

                case  MCCommands.VolDown:
                    SendKeyStroke(VK.VK_VOLUME_DOWN);
                break;

                case  MCCommands.VolMute:
                    SendKeyStroke(VK.VK_VOLUME_MUTE);
                break;

                case  MCCommands.ChanUp:
                    SendKeyStroke(VK.VK_PRIOR);
                break;

                case  MCCommands.ChanDown:
                    SendKeyStroke(VK.VK_NEXT);
                break;

                case  MCCommands.Num0:
                SendKeyStroke(VK.VK_NUMPAD0);
                break;

                case  MCCommands.Num1:
                SendKeyStroke(VK.VK_NUMPAD1);
                break;

                case  MCCommands.Num2:
                SendKeyStroke(VK.VK_NUMPAD2);
                break;

                case  MCCommands.Num3:
                SendKeyStroke(VK.VK_NUMPAD3);
                break;

                case  MCCommands.Num4:
                SendKeyStroke(VK.VK_NUMPAD4);
                break;

                case  MCCommands.Num5:
                SendKeyStroke(VK.VK_NUMPAD5);
                break;

                case  MCCommands.Num6:
                SendKeyStroke(VK.VK_NUMPAD6);
                break;

                case  MCCommands.Num7:
                SendKeyStroke(VK.VK_NUMPAD7);
                break;

                case  MCCommands.Num8:
                SendKeyStroke(VK.VK_NUMPAD8);
                break;

                case  MCCommands.Num9:
                SendKeyStroke(VK.VK_NUMPAD9);
                break;

                case  MCCommands.NumHash:
                
                break;

                case  MCCommands.NumStar:
                SendKeyStroke(VK.VK_MULTIPLY);
                break;

                case  MCCommands.Text:

                break;

                case  MCCommands.TextRed:

                break;

                case  MCCommands.TextGreen:

                break;

                case  MCCommands.TextYellow:

                break;

                case  MCCommands.TextBlue:

                break;

                case  MCCommands.Subtitles:
                    SendKeyDown(VK.VK_CONTROL);
                    SendKeyStroke(VK.VK_LETTER_U);
                    SendKeyUp(VK.VK_CONTROL);
                break;

                case  MCCommands.GotoLiveTV:
                    SendKeyDown(VK.VK_CONTROL);
                    SendKeyStroke(VK.VK_LETTER_T);
                    SendKeyUp(VK.VK_CONTROL);
                break;

                case MCCommands.GotoGuide:
                    SendKeyDown(VK.VK_CONTROL);
                    SendKeyStroke(VK.VK_LETTER_G);
                    SendKeyUp(VK.VK_CONTROL);
                break;

                case  MCCommands.GotoRecTV:
                    SendKeyDown(VK.VK_CONTROL);
                    SendKeyStroke(VK.VK_LETTER_O);
                    SendKeyUp(VK.VK_CONTROL);
                break;

                case  MCCommands.GotoPictures:
                    SendKeyDown(VK.VK_CONTROL);
                    SendKeyStroke(VK.VK_LETTER_I);
                    SendKeyUp(VK.VK_CONTROL);
                break;

                case MCCommands.GotoVideos:
                    SendKeyDown(VK.VK_CONTROL);
                    SendKeyStroke(VK.VK_LETTER_E);
                    SendKeyUp(VK.VK_CONTROL);
                break;

                case  MCCommands.GotoMusic:
                    SendKeyDown(VK.VK_CONTROL);
                    SendKeyStroke(VK.VK_LETTER_M);
                    SendKeyUp(VK.VK_CONTROL);
                break;

                case  MCCommands.GotoMovies:
                    SendKeyDown(VK.VK_CONTROL);
                    SendKeyDown(VK.VK_SHIFT);
                    SendKeyStroke(VK.VK_LETTER_M);
                    SendKeyUp(VK.VK_SHIFT);
                    SendKeyUp(VK.VK_CONTROL);
                break;

                case  MCCommands.GotoRadio:
                    SendKeyDown(VK.VK_CONTROL);
                    SendKeyStroke(VK.VK_LETTER_A);
                    SendKeyUp(VK.VK_CONTROL);
                break;

                case  MCCommands.GreenButton:
                    SendKeyDown(VK.VK_LWIN);
                    SendKeyDown(VK.VK_MENU);
                    SendKeyStroke(VK.VK_RETURN);
                    SendKeyUp(VK.VK_LWIN);
                    SendKeyUp(VK.VK_MENU);
                break;

                case  MCCommands.Power:
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

        #region Low Level Key Sending

        static void SendKeyStroke(VK sendEvent)
        {
            SendKeyDown(sendEvent);
            SendKeyUp(sendEvent);
        }
        static void SendKeyDown(VK sendEvent)
        {
            
            IntPtr winHandle = NativeMethods.FindWindowByCaption(IntPtr.Zero,  "Windows Media Center");

            NativeMethods.SafeSendMessage(winHandle, NativeMethods.WM_KEYDOWN, new IntPtr((int)sendEvent), IntPtr.Zero);
            //object foo = new object();
            //NativeMethods.SafePostMessage(new HandleRef(foo, winHandle), NativeMethods.WM_KEYDOWN, new IntPtr((int)sendEvent), IntPtr.Zero);
            

            return;
            uint intReturn; // ersin was here!
            NativeMethods.INPUT structInput;
            structInput = new NativeMethods.INPUT();
            structInput.type = NativeMethods.INPUT_KEYBOARD;

            // Key down shift, ctrl, and/or alt
            structInput.ki.wScan = 0;
            structInput.ki.time = 0;
            structInput.ki.dwFlags = 0;
            structInput.ki.dwExtraInfo = NativeMethods.GetMessageExtraInfo();
            structInput.ki.wVk = (ushort)sendEvent;

            intReturn = NativeMethods.SendInput(1, ref structInput, Marshal.SizeOf(structInput));
            Debug.Print("SENDKEYDOWN: " + intReturn.ToString());
        }
        static void SendKeyUp(VK sendEvent)
        {
            IntPtr winHandle = NativeMethods.FindWindowByCaption(IntPtr.Zero, "Windows Media Center");
            NativeMethods.SafeSendMessage(winHandle, NativeMethods.WM_KEYUP, new IntPtr((int)sendEvent), IntPtr.Zero);
            
            //object foo = new object();
            //NativeMethods.SafePostMessage(new HandleRef(foo, winHandle), NativeMethods.WM_KEYUP, new IntPtr((int)sendEvent), IntPtr.Zero);
            return;

            uint intReturn; // ersin was here!
            NativeMethods.INPUT structInput;
            structInput = new NativeMethods.INPUT();
            structInput.type = NativeMethods.INPUT_KEYBOARD;

            // Key down shift, ctrl, and/or alt
            structInput.ki.wScan = 0;
            structInput.ki.time = 0;
            structInput.ki.dwExtraInfo = NativeMethods.GetMessageExtraInfo();
            structInput.ki.wVk = (ushort)sendEvent;

            structInput.ki.dwFlags = NativeMethods.KEYEVENTF_KEYUP;
            intReturn = NativeMethods.SendInput(1, ref structInput, Marshal.SizeOf(structInput));
            Debug.Print("SENDKEYUP: " + intReturn.ToString());
        }
        #endregion

    } 
}
