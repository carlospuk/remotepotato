using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace CommonEPG
{
    public class TVProgrammeInfoBlob
    {
        // Constructors
        public TVProgrammeInfoBlob()
        { }

        // Members
        public string Id { get; set; }
        public string TVProgrammeId { get; set; }
        public string Description { get; set; }
        public TVProgrammeCrew Crew { get; set; }
        public List<TVProgramme> OtherShowingsInSeries { get; set; }
        public List<TVProgramme> OtherShowingsOfThis { get; set; }
    }


}
