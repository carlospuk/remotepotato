using System;
using System.Collections.Generic;

namespace CommonEPG
{
    public class RPRecordingsBlob
    {
        public List<RPRequest> RPRequests;
        public List<RPRecording> RPRecordings;
        public List<TVProgramme> TVProgrammes;

        public RPRecordingsBlob()
        {
            RPRequests = new List<RPRequest>();
            RPRecordings = new List<RPRecording>();
            TVProgrammes = new List<TVProgramme>();
        }
        public RPRecordingsBlob(List<RPRequest> _rpRequests, List<RPRecording> _rpRecordings, List<TVProgramme> _tvprogs)
        {
            RPRequests = _rpRequests;
            RPRecordings = _rpRecordings;
            TVProgrammes = _tvprogs;
        }


    }
}
