//
// This code was written by Keith Brown, and may be freely used.
// Want to learn more about .NET? Visit pluralsight.com today!
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace Pluralsight.Crypto
{
    [StructLayout(LayoutKind.Sequential)]
    public abstract class DisposeableObject : IDisposable
    {
        private bool disposed = false;

        ~DisposeableObject()
        {
            CleanUp(false);
        }

        public void Dispose()
        {
            // note this method does not throw ObjectDisposedException
            if (!disposed)
            {
                CleanUp(true);

                disposed = true;

                GC.SuppressFinalize(this);
            }
        }

        protected abstract void CleanUp(bool viaDispose);

        /// <summary>
        /// Typical check for derived classes
        /// </summary>
        protected void ThrowIfDisposed()
        {
            ThrowIfDisposed(this.GetType().FullName);
        }

        /// <summary>
        /// Typical check for derived classes
        /// </summary>
        protected void ThrowIfDisposed(string objectName)
        {
            if (disposed)
                throw new ObjectDisposedException(objectName);
        }
    }
}
