using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommonEPG
{
    public class DebugReportEventArgs : EventArgs
    {

    // Event arguments for status reports
    
        public string DebugText { get; set; }
        public int Severity { get; set; }
        public Exception ThrownException { get; set; }

        /// <summary>
        /// A class to hold debug report event information
        /// </summary>
        /// <param name="debugText">The text to be written to the the debug log</param>
        /// <param name="severity">The severity of the report.  Specify a value of ten or greater to write the report to the debug log.  A value below ten will only be logged if Advanced Debugging is enabled.</param>
        /// <param name="thrownException">(Optional) An inner exception, details of which will be included if a report is written to the debug log.  Pass null for no exception.</param>
        public DebugReportEventArgs(string debugText, int severity, Exception thrownException)
        {
            Severity = severity;
            DebugText = debugText;
            ThrownException = thrownException;
        }
        
    
    }
}
