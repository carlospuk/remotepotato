using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.DeepZoomTools;

namespace RemotePotatoServer
{
    public static class DeepZoomManager
    {

        

        static DeepZoomManager()
        {
            
        }

        public static byte[] DeepZoomImage(string FN)
        {
            string outputFN = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            CreateDZImage(FN, outputFN);
            byte[] output = FileCache.ReadBinaryFileFromDisk(outputFN);
            
            File.Delete(outputFN);
            return output;
        }

        public static void CreateDZImage(string inputFN, string outputFN)
        {
            ImageCreator creator = new ImageCreator();
            creator.ImageQuality = 0.8;
            creator.TileFormat = ImageFormat.Jpg;
            creator.TileSize = 256;
            creator.TileOverlap = 0;
            creator.Create(inputFN, outputFN);
        }


    }
}
