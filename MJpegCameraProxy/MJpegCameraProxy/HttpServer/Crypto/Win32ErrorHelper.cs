//
// This code was written by Keith Brown, and may be freely used.
// Want to learn more about .NET? Visit pluralsight.com today!
//
using System;
using System.Runtime.InteropServices;

namespace Pluralsight.Crypto
{
    internal static class Win32ErrorHelper
    {
        internal static void ThrowExceptionIfGetLastErrorIsNotZero()
        {
            int win32ErrorCode = Marshal.GetLastWin32Error();
            if (0 != win32ErrorCode)
                Marshal.ThrowExceptionForHR(HResultFromWin32(win32ErrorCode));
        }

        private static int HResultFromWin32(int win32ErrorCode)
        {
            if (win32ErrorCode > 0)
                return (int)((((uint)win32ErrorCode) & 0x0000FFFF) | 0x80070000U);
            else return win32ErrorCode;
        }
    }
}
