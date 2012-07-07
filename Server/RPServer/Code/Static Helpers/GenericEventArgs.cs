using System;
using System.Collections.Generic;

namespace RemotePotatoServer
{
    // Event Args
    public class GenericEventArgs<T> : EventArgs
    {
        T value;
        public T Value { get { return value; } }
        public GenericEventArgs(T value) { this.value = value; }
    }
}
