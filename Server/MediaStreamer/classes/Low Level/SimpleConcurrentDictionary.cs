using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FatAttitude.Collections
{
    public class SimpleConcurrentDictionary<T, U> 
    {
        Dictionary<T, U> dictionary;

        public SimpleConcurrentDictionary()
        {
            dictionary = new Dictionary<T, U>();
        }

        public void Add(T key, U value)
        {
            lock (dictionary)
            {
                dictionary.Add(key, value);
            }
        }
        public bool Remove(T key)
        {
            lock (dictionary)
            {
                return dictionary.Remove(key);
            }
        }
        public bool TryGetValue(T key, out U value)
        {
            lock (dictionary)
            {
                return dictionary.TryGetValue(key, out value);
            }
        }
        public void Clear()
        {
            lock (dictionary)
            {
                dictionary.Clear();
            }
        }
        public bool ContainsKey(T key)
        {
            lock (dictionary)
            {
                return dictionary.ContainsKey(key);
            }
        }
        public List<U> SafeGetValuesAndClear()
        {
            lock (dictionary)
            {
                List<U> values = dictionary.Values.ToList();
                dictionary.Clear();
                return values;
            }
        }

    }
}
