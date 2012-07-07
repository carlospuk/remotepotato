using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonEPG;
using System.Xml.Serialization;
using System.Xml;
using System.IO;

namespace CommonEPG
{
    public class RecordingResult
    {
        public bool Completed;
        public bool Success;
        public RequestResults RequestResult;
        public string ErrorMessage;
        public bool WereConflicts;
        public string ConflictInfo;
        public RPRecordingsBlob GeneratedRecordingsBlob;

        public enum RequestResults
        {
            Unset,
            FailedWithError,
            Conflicts,
            NoProgrammesFound,
            AlreadyScheduled,
            ExceededMaxRequests,
            OK
        }

        public RecordingResult()
        {
            RequestResult = RequestResults.Unset;
            GeneratedRecordingsBlob = new RPRecordingsBlob();
            Completed = false;
            Success = false;
        }

        public string ToXML()
        {
            System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(this.GetType());
            StringWriter sw = new StringWriter();
            serializer.Serialize(sw, this);
            return sw.ToString();
        }
        public static RecordingResult FromXML(string theXML)
        {
            RecordingResult newRR = new RecordingResult();
            XmlSerializer serializer = new XmlSerializer(newRR.GetType());
            StringReader sr = new StringReader(theXML);
            try
            {
                return (RecordingResult)serializer.Deserialize(sr);
            }
            catch
            {
                return newRR;
            }
        }

        // Helpers
        public static string FriendlyFailureReason(RecordingResult rr)
        {
            if (rr == null) return "RecordingResult was null";
            
            string txtFailureReason = "An unknown failure occurred.  Sorry, we wish we could be more helpful there.";

            if (rr.RequestResult != RequestResults.OK)
            {
                switch (rr.RequestResult)
                {
                    case RequestResults.NoProgrammesFound:
                        txtFailureReason = "Recording not scheduled - The channel or programme could not be found.";
                        break;

                    case RequestResults.FailedWithError:
                        if (!string.IsNullOrEmpty(rr.ErrorMessage))
                            return rr.ErrorMessage;
                        else
                            return "Recording not scheduled due to an unspecified error.";

                    case RequestResults.AlreadyScheduled:
                        txtFailureReason = "Recording not scheduled - this showing is already scheduled to be recorded.";
                        break;

                    case RequestResults.Conflicts:
                        txtFailureReason = "Recording not scheduled as it would conflict with an existing recording.";
                        break;

                    case RequestResults.ExceededMaxRequests:
                        txtFailureReason = "Recording not scheduled - the number of programmes that would be recorded is above the maximum of 50.";
                        break;

                    case RequestResults.OK:
                        txtFailureReason = "Recording was OK and did not fail!  This message should not have been displayed.";
                        break;

                    default:
                        break;
                }

                return txtFailureReason;
            }





            return txtFailureReason;
        }
        public static string FriendlySuccessReport(RecordingResult rr)
        {
            string txtReport = "Unknown result.";


            if (rr.WereConflicts)
            {
                if (String.IsNullOrEmpty(rr.ConflictInfo))
                    return  "There were one or more conflicts on some of the requested recordings.";
                else
                    return rr.ConflictInfo;
            }
           

            if (
                (rr.GeneratedRecordingsBlob == null) ||
                (rr.GeneratedRecordingsBlob.RPRecordings == null) ||
                (rr.GeneratedRecordingsBlob.RPRecordings.Count == 0)
                )
            {
                return "No shows were found or scheduled to record.";
            }
            else if (rr.GeneratedRecordingsBlob.RPRecordings.Count == 1)
            {
                return "The scheduling was successful - one recording has been scheduled.";
            }
            else if (rr.GeneratedRecordingsBlob.RPRecordings.Count > 1)
            {
                return "The scheduling was successful - " +
                    rr.GeneratedRecordingsBlob.RPRecordings.Count.ToString() + 
                    " recordings have been scheduled.";
            }

            // Default
            return txtReport;
        }
    }
}