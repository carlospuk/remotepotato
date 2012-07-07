using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FatAttitude
{
    /// <summary>
    /// Handles a growing string of parameters which can be added to in pairs, or single items,
    /// while ensuring that a valid, space-delimited command line string is built
    /// </summary>
    public class CommandArguments
    {
        StringBuilder sbOutput;

        public CommandArguments()
        {
            sbOutput = new StringBuilder(50);
        }

        public override string ToString()
        {
            return sbOutput.ToString();
        }

        /// <summary>
        /// Adds a single argument
        /// </summary>
        /// <param name="arg"></param>
        public void AddArg(string arg)
        {
            DoEndSpace();
            sbOutput.Append(arg);
        }

        /// <summary>
        /// Adds an option, then a space, then a value
        /// </summary>
        /// <param name="option"></param>
        /// <param name="value"></param>
        public void AddArgCouple(string option, string value)
        {
            DoEndSpace();
            AddArg(option);
            AddArg(value);
        }

        /// <summary>
        /// Adds a key, then =, then a value
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void AddArgKV(string key, string value)
        {
            DoEndSpace();
            AddArg(key);
            sbOutput.Append("=");
            sbOutput.Append(value);
        }

        void DoEndSpace()
        {
            if (sbOutput.Length < 1) return;

            if (!sbOutput.ToString().EndsWith(" "))
                sbOutput.Append(" ");
        }


    }

}
