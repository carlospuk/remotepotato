using System;
using System.Text;
using System.Linq;
using System.Diagnostics;

namespace RemotePotatoService
{
    public static class ProcessKiller
    {

            public static bool KillProcessByName(string strName)
            {
                Process[] processes = Process.GetProcessesByName(strName);

                
                foreach (Process process in processes)
                {
                    process.Kill();
                }

                return (processes.Count() > 0);
            }
    }
}
