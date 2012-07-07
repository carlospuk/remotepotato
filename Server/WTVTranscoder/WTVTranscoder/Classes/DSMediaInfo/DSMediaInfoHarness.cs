using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FatAttitude
{
    public class DSMediaInfoHarness
    {
        public TimeSpan GetMediaDuration(string fileName)
        {
            using (DSMediaInfo dsi = new DSMediaInfo())
            {
                try
                {

                    dsi.LoadMedia(fileName);

                    TimeSpan audioLength = TimeSpan.FromSeconds(0);
                    TimeSpan videoLength = TimeSpan.FromSeconds(0);
                    audioLength = dsi.AudioStreamLength; // 0 if no audio                    
                    videoLength = dsi.VideoStreamLength; // 0 if no video

                    if (audioLength > videoLength)
                        return audioLength;
                    else
                        return videoLength;
                }
                catch
                {
                    return TimeSpan.FromSeconds(0);
                }
            }
        }
    }
}
