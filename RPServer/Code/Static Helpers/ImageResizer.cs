using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Web;
using System.IO;
using System.Xml.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using RemotePotatoServer.Properties;
using CommonEPG;

namespace RemotePotatoServer
{
    static class ImageResizer
    {
       
        public static void Initialize()
        {}
   
        #region Importing / Resizing
        public static bool ResizePicture(string inputFilename, Size size, out byte[] outputData, ImageFormat format, bool allowGrowth)
        {
            outputData = new byte[] { };
            try
            {
                Image imgLogo = Image.FromFile(inputFilename);
                Image imgLogoResized = resizeImage(imgLogo, size, allowGrowth);

                outputData = ImageToByteArray(imgLogoResized, format);
                return true;
            }
            catch (FileNotFoundException)
            {
                Functions.WriteLineToLogFile("Error resizing image " + inputFilename + ": File Not Found");
                return false;
            }
            catch (Exception ex)
            {
                Functions.WriteLineToLogFile("Error resizing image " + inputFilename + ":");
                Functions.WriteExceptionToLogFile(ex);
                return false;
            }

        }
        public static bool ResizePicture(byte[] inputData, Size size, out byte[] outputData, bool allowGrowth)
        {
            outputData = new byte[] {};
            try
            {
                Image imgLogo = ByteArrayToImage(inputData);
                Image imgLogoResized = resizeImage(imgLogo, size, allowGrowth);
                outputData = ImageToByteArray(imgLogoResized);
                return true;
            }
            catch (Exception ex) {
                Functions.WriteLineToLogFile("Error resizing image: ");
                Functions.WriteExceptionToLogFile(ex);
                return false;
            }

        }
        public static void ResizeLogo(string inFilename, string outFilename, bool allowGrowth)
        {
            Image inImage = Image.FromFile(inFilename);
            Image outImage = resizeImage(inImage, new Size(50, 50), false);
            outImage.Save(outFilename, ImageFormat.Png);
        }
        public static Image resizeImage(Image imgToResize, Size size, bool allowGrowth)
        {
            int sourceWidth = imgToResize.Width;
            int sourceHeight = imgToResize.Height;

            float nPercent = 0;
            float nPercentW = 0;
            float nPercentH = 0;

            nPercentW = ((float)size.Width / (float)sourceWidth);
            nPercentH = ((float)size.Height / (float)sourceHeight);

            if (nPercentH < nPercentW)
                nPercent = nPercentH;
            else
                nPercent = nPercentW;


            if (!allowGrowth)
            {
                if (nPercent > 100) nPercent = 100;
            }


            int destWidth = (int)(sourceWidth * nPercent);
            int destHeight = (int)(sourceHeight * nPercent);

            Bitmap b = new Bitmap(destWidth, destHeight);
            Graphics g = Graphics.FromImage((Image)b);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            g.DrawImage(imgToResize, 0, 0, destWidth, destHeight);
            g.Dispose();

            return (Image)b;
        }
        private static Image StringToImage(string strData)
        {
            byte[] byteArrayIn = Encoding.Unicode.GetBytes(strData);
            return ByteArrayToImage(byteArrayIn);
        }
        private static Image ByteArrayToImage(byte[] byteArrayIn)
        {
            MemoryStream ms = new MemoryStream(byteArrayIn);
            Image returnImage = Image.FromStream(ms);
            return returnImage;
        }
        public static string ImageToString(System.Drawing.Image imageIn)
        {
            MemoryStream ms = new MemoryStream();
            imageIn.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            return Encoding.Unicode.GetString(ms.ToArray());            
        }
        public static byte[] ImageToByteArray(System.Drawing.Image imageIn)
        {
            return ImageToByteArray(imageIn, ImageFormat.Png);
        }
        public static byte[] ImageToByteArray(System.Drawing.Image imageIn, ImageFormat format)
        {
            MemoryStream ms = new MemoryStream();
            imageIn.Save(ms, format);
            return ms.ToArray();
        }
        #endregion

        
    }
    
}
