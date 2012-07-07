using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommonEPG
{
    public class RPPictureItem
    {

        public string ID { get; set; }
        public string Title { get; set; }
        public string FileName { get; set; }
        public DateTime DateTaken { get; set; } 

        public RPPictureItem()
        {
            ID = string.Empty;
            Title = string.Empty;
            FileName = string.Empty;
            DateTaken = new DateTime();
        }

    }
}
