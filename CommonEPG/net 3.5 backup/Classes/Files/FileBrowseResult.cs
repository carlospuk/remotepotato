using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommonEPG
{
    public class FileBrowseResult
    {
        public bool Success { get; set; }
        public string ErrorText { get; set; }
        public string BaseDirectory { get; set; }
        public List<BrowseItem> Directories { get; set; }
        public List<BrowseItem> Files { get; set; }

        public FileBrowseResult()
        {
            Success = false;
            ErrorText = "";
            BaseDirectory = "";
            Directories = new List<BrowseItem>();
            Files = new List<BrowseItem>();
        }
    }
}
