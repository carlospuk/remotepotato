using System;
using System.Net;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Windows;

namespace SilverPotato
{
    public class ServerFolder
    {
        public ObservableCollection<ServerFolder> Items { get; set; }
        public string Key { get; set; }
        public string Path { get; set; }

        public ServerFolder(string _key, string _path, params ServerFolder[] myPictureFolders)
        {
            Key = _key;
            Path = _path;

            ObservableCollection<ServerFolder> itemsObservableCollection = new ObservableCollection<ServerFolder>();
            foreach (var item in myPictureFolders)
                itemsObservableCollection.Add(item);
            Items = itemsObservableCollection;
        }

        public bool TryGetItemWithKey(string key, out ServerFolder foundPF)
        {
            foundPF = null;
            foreach (ServerFolder pf in Items)
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
