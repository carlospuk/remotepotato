using System;
using System.Collections.Generic;

namespace FatAttitude.WTVTranscoder
{
    /// <summary>
    /// A class to represent event args of a generic type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class GenericEventArgs<T> : EventArgs
    {
        public GenericEventArgs(T value)
        {
            m_value = value;
        }

        private T m_value;

        public T Value
        {
            get { return m_value; }
        }
    }

}
