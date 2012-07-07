using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace SilverPotato
{
    public partial class LayerBlobs : LayerBase
    {
        private int NumberOfBlobs;
        private double BlobOpacity;
        private double BlobScaleFactor;

        public LayerBlobs()
        {
            InitializeComponent();
        }
        public LayerBlobs(double opacity, double blobScaleFactor, int numberBlobs, double movementAmount)
            : this()
        {
            MovementAmount = movementAmount;
            BlobOpacity = opacity;
            BlobScaleFactor = blobScaleFactor;
            NumberOfBlobs = numberBlobs;

            FillWithBlobs();
        }

        private void FillWithBlobs()
        {
            Random rnd = new Random();

            for (int i = 0; i < NumberOfBlobs; i++)
            {
                shp_Blob blob = new shp_Blob(BlobScaleFactor);
                blob.SetValue(Canvas.LeftProperty, (double)rnd.Next(0, Convert.ToInt32(cvMain.Width)));
                blob.SetValue(Canvas.TopProperty, (double)rnd.Next(0, Convert.ToInt32(cvMain.Height)));
                blob.rtRotate.Angle = rnd.Next(0, 359);

                blob.Opacity = BlobOpacity;

                cvMain.Children.Add(blob);
            }

        }
    }
}
