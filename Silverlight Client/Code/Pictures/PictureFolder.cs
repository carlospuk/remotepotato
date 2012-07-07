using System;
using System.Net;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Windows;

namespace SilverPotato
{
    public class PictureFolder
    {
        public ObservableCollection<PictureFolder> Items { get; set; }
        public string Key { get; set; }
        public string Path { get; set; }

        public PictureFolder(string _key, string _path, params PictureFolder[] myPictureFolders)
        {
            Key = _key;
            Path = _path;

            ObservableCollection<PictureFolder> itemsObservableCollection = new ObservableCollection<PictureFolder>();
            foreach (var item in myPictureFolders)
                itemsObservableCollection.Add(item);
            Items = itemsObservableCollection;
        }

        public bool TryGetItemWithKey(string key, out PictureFolder foundPF)
        {
            foundPF = null;
            foreach (PictureFolder pf in Items)
            {
                if (pf.Key == key)
                {
                    foundPF = pf;
                    return true;
                }
            }

            return false;
        }
            
    }
}
