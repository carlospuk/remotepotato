using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Resources;
using System.Windows.Media.Animation;
using System.Windows.Shapes;


namespace SilverPotato
{
    public static class ImageManager
    {

        public static BitmapImage bmpRecordDotOneTime;
        public static BitmapImage bmpRecordDotSeries;
        public static BitmapImage bmpHighDefLogo;
        public static BitmapImage bmpStarOff;
        public static BitmapImage bmpStarOn;
        public static BitmapImage bmpStarHalf;
        public static BitmapImage bmpThumbnailDefault;
        public static BitmapImage bmpThumbnailDefaultPictures;
        public static BitmapImage bmpBtnBack;
        public static BitmapImage bmpBtnBackOn;
        public static BitmapImage bmpBtnHome;
        public static BitmapImage bmpBtnHomeOn;

        public static void Initialize()
        {
            

            LoadImagesFromResources();
        }
        public static BitmapImage LoadImageFromContentPath(string path)
        {
            return new BitmapImage(new Uri(path, UriKind.Relative));
        }
        private static void LoadImagesFromResources()
        {

            bmpRecordDotOneTime = LoadImageFromContentPath("/Images/record_dot_onetime.png");
            bmpRecordDotSeries = LoadImageFromContentPath("/Images/record_dot_series.png");
            bmpHighDefLogo = LoadImageFromContentPath("/Images/showhdlogo.png");
            bmpStarOn = LoadImageFromContentPath("/Images/starOn.png");
            bmpStarHalf = LoadImageFromContentPath("/Images/starHalf.png");
            bmpStarOff = LoadImageFromContentPath("/Images/starOff.png");
            bmpThumbnailDefault = LoadImageFromContentPath("/Images/thumbnail_default.png");
            bmpThumbnailDefaultPictures = LoadImageFromContentPath("/Images/imgDefaultPic.png");           
            
            bmpBtnBackOn = LoadImageFromContentPath("/Images/btnBack_On.png");
            bmpBtnBack = LoadImageFromContentPath("/Images/btnBack.png");
            bmpBtnHome = LoadImageFromContentPath("/Images/logoPotato.png");
            bmpBtnHomeOn = LoadImageFromContentPath("/Images/logoPotato_On.png");
        }

        
    }
}
