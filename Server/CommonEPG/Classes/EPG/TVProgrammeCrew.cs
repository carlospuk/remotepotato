using System;
using System.Xml.Serialization;

namespace CommonEPG
{
    public class TVProgrammeCrew
    {
        // Constructors
        public TVProgrammeCrew()
        { }

        // Private Members
        public long Id { get; set; }
        public long TVProgrammeId { get; set; }

        public string Actors { get; set; }
        public string Writers { get; set; }
        public string Directors { get; set; }
        public string Producers { get; set; }


        // Silverlight Store
        public event EventHandler Updated;
        public void FireUpdated()
        {
            if (Updated != null) Updated(this, new EventArgs());
        }
    }

}
