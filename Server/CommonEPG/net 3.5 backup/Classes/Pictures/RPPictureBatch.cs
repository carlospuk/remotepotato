using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommonEPG
{
    public class RPPictureBatch
    {
        public List<RPPictureItem> Pictures { get; set; }
        public int TotalPicturesInLibrary { get; set; }

        public RPPictureBatch()
        {
            Pictures = new List<RPPictureItem>();
        }

    }
}
