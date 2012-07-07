using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;

namespace FatAttitude.WTVTranscoder
{
    /// <summary>Provides for easy cleanup of all COM objects used in its scope.</summary>
    public sealed class DisposalCleanup : IDisposable
    {
        /// <summary>Stores the list of items to be disposed when this instance is disposed.</summary>
        private ArrayList _toDispose = new ArrayList();

        /// <summary>Adds any number of objects to be disposed.</summary>
        /// <param name="toDispose">The list of IDisposable objects, COM objects, or interface IntPtrs to be disposed.</param>
        public void Add(params object[] toDispose)
        {
            if (_toDispose == null) throw new ObjectDisposedException(GetType().Name);
            if (toDispose != null)
            {
                foreach (object obj in toDispose)
                {
                    if (obj != null && (obj is IDisposable || obj.GetType().IsCOMObject || obj is IntPtr))
                    {
                        _toDispose.Add(obj);
                    }
                }
            }
        }

        /// <summary>Disposes of all registered resources.</summary>
        void IDisposable.Dispose()
        {
            if (_toDispose != null)
            {
                foreach (object obj in _toDispose) EnsureCleanup(obj);
                _toDispose = null;
            }
        }

        /// <summary>Disposes of the specified object.</summary>
        /// <param name="toDispose">The object to be disposed.</param>
        private void EnsureCleanup(object toDispose)
        {
            if (toDispose is IDisposable)
            {
                ((IDisposable)toDispose).Dispose();
            }
            else if (toDispose is IntPtr) // assumes IntPtrs are interface pointers
            {
                Marshal.Release((IntPtr)toDispose);
            }
            else if (toDispose.GetType().IsCOMObject)
            {
                while (Marshal.ReleaseComObject(toDispose) > 0) ;
            }
        }
    }
}
