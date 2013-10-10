//
// This code was written by Keith Brown, and may be freely used.
// Want to learn more about .NET? Visit pluralsight.com today!
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pluralsight.Crypto
{
    public abstract class CryptKey : DisposeableObject
    {
        CryptContext ctx;
        IntPtr handle;

        internal IntPtr Handle { get { return handle; } }

        internal CryptKey(CryptContext ctx, IntPtr handle)
        {
            this.ctx = ctx;
            this.handle = handle;
        }

        public abstract KeyType Type { get; }

        protected override void CleanUp(bool viaDispose)
        {
            // keys are invalid once CryptContext is closed,
            // so the only time I try to close an individual key is if a user
            // explicitly disposes of the key.
            if (viaDispose)
                ctx.DestroyKey(this);
        }
    }
}
