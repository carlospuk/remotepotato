using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FatAttitude.MediaStreamer.HLS
{
    public class Segment
    {
        public int Number { get; set; }
        public bool EverRequested { get; set; }
        public byte[] Data { get; set; }

        public Segment()
        {
            Number = -1;
            EverRequested = false;
            Data = null;
        }

    }
}
