using System;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace RemotePotatoServer.RemoteInput
{
    public enum VK : ushort
        {
            /*
    * Virtual Keys, Standard Set */
            VK_LBUTTON = 0x01,
            VK_RBUTTON = 0x02,
            VK_CANCEL = 0x03,
            VK_MBUTTON = 0x04,    /* NOT contiguous with L & RBUTTON */

            VK_XBUTTON1 = 0x05,    /* NOT contiguous with L & RBUTTON */
            VK_XBUTTON2 = 0x06,    /* NOT contiguous with L & RBUTTON */

            /*
    * 0x07 : unassigned */
            VK_BACK = 0x08,
            VK_TAB = 0x09,

            /*
    * 0x0A - 0x0B : reserved */
            VK_CLEAR = 0x0C,
            VK_RETURN = 0x0D,

            VK_SHIFT = 0x10,
            VK_CONTROL = 0x11,
            VK_MENU = 0x12,
            VK_PAUSE = 0x13,
            VK_CAPITAL = 0x14,

            VK_KANA = 0x15,
            VK_HANGEUL = 0x15,  /* old name - should be here for compatibility */
            VK_HANGUL = 0x15,
            VK_JUNJA = 0x17,
            VK_FINAL = 0x18,
            VK_HANJA = 0x19,
            VK_KANJI = 0x19,

            VK_ESCAPE = 0x1B,

            VK_CONVERT = 0x1C,
            VK_NONCONVERT = 0x1D,
            VK_ACCEPT = 0x1E,
            VK_MODECHANGE = 0x1F,

            VK_SPACE = 0x20,
            VK_PRIOR = 0x21,
            VK_NEXT = 0x22,
            VK_END = 0x23,
            VK_HOME = 0x24,
            VK_LEFT = 0x25,
            VK_UP = 0x26,
            VK_RIGHT = 0x27,
            VK_DOWN = 0x28,
            VK_SELECT = 0x29,
            VK_PRINT = 0x2A,
            VK_EXECUTE = 0x2B,
            VK_SNAPSHOT = 0x2C,
            VK_INSERT = 0x2D,
            VK_DELETE = 0x2E,
            VK_HELP = 0x2F,

            /*
    * VK_0 - VK_9 are the same as ASCII '0' - '9' (0x30 - 0x39) * 0x40 : unassigned * VK_A - VK_Z are the same as ASCII 'A' - 'Z' (0x41 - 0x5A) */
            VK_LWIN = 0x5B,
            VK_RWIN = 0x5C,
            VK_APPS = 0x5D,

            /*
    * 0x5E : reserved */
            VK_SLEEP = 0x5F,

            VK_NUMPAD0 = 0x60,
            VK_NUMPAD1 = 0x61,
            VK_NUMPAD2 = 0x62,
            VK_NUMPAD3 = 0x63,
            VK_NUMPAD4 = 0x64,
            VK_NUMPAD5 = 0x65,
            VK_NUMPAD6 = 0x66,
            VK_NUMPAD7 = 0x67,
            VK_NUMPAD8 = 0x68,
            VK_NUMPAD9 = 0x69,
            VK_MULTIPLY = 0x6A,
            VK_ADD = 0x6B,
            VK_SEPARATOR = 0x6C,
            VK_SUBTRACT = 0x6D,
            VK_DECIMAL = 0x6E,
            VK_DIVIDE = 0x6F,
            VK_F1 = 0x70,
            VK_F2 = 0x71,
            VK_F3 = 0x72,
            VK_F4 = 0x73,
            VK_F5 = 0x74,
            VK_F6 = 0x75,
            VK_F7 = 0x76,
            VK_F8 = 0x77,
            VK_F9 = 0x78,
            VK_F10 = 0x79,
            VK_F11 = 0x7A,
            VK_F12 = 0x7B,
            VK_F13 = 0x7C,
            VK_F14 = 0x7D,
            VK_F15 = 0x7E,
            VK_F16 = 0x7F,
            VK_F17 = 0x80,
            VK_F18 = 0x81,
            VK_F19 = 0x82,
            VK_F20 = 0x83,
            VK_F21 = 0x84,
            VK_F22 = 0x85,
            VK_F23 = 0x86,
            VK_F24 = 0x87,

            /*
    * 0x88 - 0x8F : unassigned */
            VK_NUMLOCK = 0x90,
            VK_SCROLL = 0x91,

            /*
    * VK_L* & VK_R* - left and right Alt, Ctrl and Shift virtual keys. * Used only as parameters to GetAsyncKeyState() and GetKeyState(). * No other API or message will distinguish left and right keys in this way. */
            VK_LSHIFT = 0xA0,
            VK_RSHIFT = 0xA1,
            VK_LCONTROL = 0xA2,
            VK_RCONTROL = 0xA3,
            VK_LMENU = 0xA4,
            VK_RMENU = 0xA5,

            VK_BROWSER_BACK = 0xA6,
            VK_BROWSER_FORWARD = 0xA7,
            VK_BROWSER_REFRESH = 0xA8,
            VK_BROWSER_STOP = 0xA9,
            VK_BROWSER_SEARCH = 0xAA,
            VK_BROWSER_FAVORITES = 0xAB,
            VK_BROWSER_HOME = 0xAC,

            VK_VOLUME_MUTE = 0xAD,
            VK_VOLUME_DOWN = 0xAE,
            VK_VOLUME_UP = 0xAF,
            VK_MEDIA_NEXT_TRACK = 0xB0,
            VK_MEDIA_PREV_TRACK = 0xB1,
            VK_MEDIA_STOP = 0xB2,
            VK_MEDIA_PLAY_PAUSE = 0xB3,
            VK_LAUNCH_MAIL = 0xB4,
            VK_LAUNCH_MEDIA_SELECT = 0xB5,
            VK_LAUNCH_APP1 = 0xB6,
            VK_LAUNCH_APP2 = 0xB7,

            /*
    * 0xB8 - 0xB9 : reserved */
            VK_OEM_1 = 0xBA,   // ';:' for US
            VK_OEM_PLUS = 0xBB,   // '+' any country
            VK_OEM_COMMA = 0xBC,   // ',' any country
            VK_OEM_MINUS = 0xBD,   // '-' any country
            VK_OEM_PERIOD = 0xBE,   // '.' any country
            VK_OEM_2 = 0xBF,   // '/?' for US
            VK_OEM_3 = 0xC0,   // '`~' for US

            /*
    * 0xC1 - 0xD7 : reserved */
            /*
    * 0xD8 - 0xDA : unassigned */
            VK_OEM_4 = 0xDB,  //  '[{' for US
            VK_OEM_5 = 0xDC,  //  '\|' for US
            VK_OEM_6 = 0xDD,  //  ']}' for US
            VK_OEM_7 = 0xDE,  //  ''"' for US
            VK_OEM_8 = 0xDF,

            VK_LETTER_A = 0x41,
            VK_LETTER_B = 0x42,
            VK_LETTER_C = 0x43,
            VK_LETTER_D = 0x44,
            VK_LETTER_E = 0x45,
            VK_LETTER_F = 0x46,
            VK_LETTER_G = 0x47,
            VK_LETTER_H = 0x48,
            VK_LETTER_I = 0x49,
            VK_LETTER_J = 0x4A,
            VK_LETTER_K = 0x4B,
            VK_LETTER_L = 0x4C,
            VK_LETTER_M = 0x4D,
            VK_LETTER_N = 0x4E,
            VK_LETTER_O = 0x4F,
            VK_LETTER_P = 0x50,
            VK_LETTER_Q = 0x51,
            VK_LETTER_R = 0x52,
            VK_LETTER_S = 0x53,
            VK_LETTER_T = 0x54,
            VK_LETTER_U = 0x55,
            VK_LETTER_V = 0x56,
            VK_LETTER_W = 0x57,
            VK_LETTER_X = 0x58,
            VK_LETTER_Y = 0x59,
            VK_LETTER_Z = 0x5A


            /*
    * 0xE0 : reserved */
            /*
        (0x30)
0 key
 (0x31)
1 key
 (0x32)
2 key
 (0x33)
3 key
 (0x34)
4 key
 (0x35)
5 key
 (0x36)
6 key
 (0x37)
7 key
 (0x38)
8 key
 (0x39)
9 key
-  (0x3A-40)
Undefined
 (0x41)
A key
 (0x42)
B key
 (0x43)
C key
 (0x44)
D key
 (0x45)
E key
 (0x46)
F key
 (0x47)
G key
 (0x48)
H key
 (0x49)
I key
 (0x4A)
J key
 (0x4B)
K key
 (0x4C)
L key
 (0x4D)
M key
 (0x4E)
N key
 (0x4F)
O key
 (0x50)
P key
 (0x51)
Q key
 (0x52)
R key
 (0x53)
S key
 (0x54)
T key
 (0x55)
U key
 (0x56)
V key
 (0x57)
W key
 (0x58)
X key
 (0x59)
Y key
 (0x5A)
Z key*/
        }

    internal class NativeMethods
    {
        #region DllImports

        [DllImport("user32.dll")]
        public static extern bool PostMessage(HandleRef hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        public static void SafePostMessage(HandleRef hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            bool result = PostMessage(hWnd, msg, wParam, lParam);
            if (! result)
                throw new Win32Exception(Marshal.GetLastWin32Error());
            return;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

        public static void SafeSendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            IntPtr result = SendMessage(hWnd, msg, wParam, lParam);
            //if (!result)
            //    throw new Win32Exception(Marshal.GetLastWin32Error());
            return;
        }

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern uint SendInput(uint nInputs, ref INPUT pInputs, int cbSize);



        [DllImport("user32.dll")]
        internal static extern IntPtr GetMessageExtraInfo();

        [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
        internal static extern IntPtr FindWindowByCaption(IntPtr ZeroOnly, string lpWindowName);

        //[DllImport("user32.dll", CharSet = CharSet.Auto)]
        //internal static extern IntPtr FindWindow(string strClassName, int nptWindowName);

        #endregion

        #region structs

        [StructLayout(LayoutKind.Sequential)]
        struct MOUSEINPUT
        {
            int dx;
            int dy;
            uint mouseData;
            uint dwFlags;
            uint time;
            IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct HARDWAREINPUT
        {
            uint uMsg;
            ushort wParamL;
            ushort wParamH;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct INPUT
        {
            [FieldOffset(0)]
            public int type;
            [FieldOffset(4)] //*
            MOUSEINPUT mi;
            [FieldOffset(4)] //*
            public KEYBDINPUT ki;
            [FieldOffset(4)] //*
            HARDWAREINPUT hi;
        }

        #endregion

        #region constants

        public const int WM_KEYDOWN = 0x0100;
        public const int WM_KEYUP = 0x0101;

        const int INPUT_MOUSE = 0;
        internal const int INPUT_KEYBOARD = 1;
        const int INPUT_HARDWARE = 2;
        const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
        internal const uint KEYEVENTF_KEYUP = 0x0002;
        const uint KEYEVENTF_UNICODE = 0x0004;
        const uint KEYEVENTF_SCANCODE = 0x0008;
        const uint XBUTTON1 = 0x0001;
        const uint XBUTTON2 = 0x0002;
        const uint MOUSEEVENTF_MOVE = 0x0001;
        const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        const uint MOUSEEVENTF_LEFTUP = 0x0004;
        const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
        const uint MOUSEEVENTF_XDOWN = 0x0080;
        const uint MOUSEEVENTF_XUP = 0x0100;
        const uint MOUSEEVENTF_WHEEL = 0x0800;
        const uint MOUSEEVENTF_VIRTUALDESK = 0x4000;
        const uint MOUSEEVENTF_ABSOLUTE = 0x8000;

        #endregion
    }   
}
