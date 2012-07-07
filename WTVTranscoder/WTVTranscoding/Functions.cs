using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemotePotatoServer
{
    public static class Functions
    {
        // Debug
        public static void WriteLineToLogFile(string txt)
        {
            Console.WriteLine(txt);
        }
        public static void WriteExceptionToLogFile(Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}
