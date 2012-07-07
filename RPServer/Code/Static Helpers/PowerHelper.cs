using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using RemotePotatoServer.Properties;

namespace RemotePotatoServer
{
    public static class PowerHelper
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);
        
        [FlagsAttribute]
        public enum EXECUTION_STATE : uint
        {
            ES_SYSTEM_REQUIRED = 0x00000001,
            ES_DISPLAY_REQUIRED = 0x00000002,
            // Legacy flag, should not be used.
            // ES_USER_PRESENT   = 0x00000004,
            ES_CONTINUOUS = 0x80000000,
        }


        public static void PreventStandby()
        {
            if (Settings.Default.DebugBasic)
                Functions.WriteLineToLogFile("PowerHelper:  Preventing Standby.");

            SetThreadExecutionState(EXECUTION_STATE.ES_SYSTEM_REQUIRED |
                                EXECUTION_STATE.ES_CONTINUOUS);
        }
        public static void AllowStandby()
        {
            if (Settings.Default.DebugBasic)
                Functions.WriteLineToLogFile("PowerHelper:  Re-enabling Standby.");

            SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS);
        }

    }

    

}
