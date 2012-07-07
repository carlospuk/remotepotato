using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FatAttitude.WTVTranscoder
{
    /// <summary>
    /// Represents a command that can be sent to a running stream
    /// </summary>
    class StreamCommand
    {
        public enum CommandNames
        {
            Cancel,
            Seek,  // Seek is not implemented
            SetRate
        }
        public CommandNames CommandName { get; set; }
        public int Param1 { get; set; }
        public string Param2 { get; set; }
        public object ParamObject { get; set; }


        public StreamCommand(CommandNames commandName) : this(commandName, 0, "") { }
        public StreamCommand(CommandNames commandName, int param) : this(commandName, param, "") { }
        public StreamCommand(CommandNames commandName, string param) : this(commandName, 0, param) { }       
        public StreamCommand(CommandNames commandName, int param1, string param2)
        {
            CommandName = commandName;
            Param1 = param1;
            Param2 = param2;
        }
        public StreamCommand(CommandNames commandName, object param1)
        {
            CommandName = commandName;
            ParamObject = param1;
        }

    }
}
