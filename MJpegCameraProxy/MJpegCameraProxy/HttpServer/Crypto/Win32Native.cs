//
// This code was written by Keith Brown, and may be freely used.
// Want to learn more about .NET? Visit pluralsight.com today!
//
using System;
using System.Runtime.InteropServices;

namespace Pluralsight.Crypto
{
    internal class Win32Native
    {
        [DllImport("AdvApi32.dll", ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CryptReleaseContext(IntPtr ctx, int flags);

        [DllImport("AdvApi32.dll", EntryPoint="CryptAcquireContextW", ExactSpelling = true, CharSet=CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CryptAcquireContext(
            out IntPtr providerContext,
            string containerName,
            string providerName,
            int providerType,
            int flags);

        [DllImport("AdvApi32.dll", ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CryptDestroyKey(IntPtr cryptKeyHandle);

        [DllImport("AdvApi32.dll", ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CryptGenKey(
            IntPtr providerContext,
            int algorithmId,
            uint flags,
            out IntPtr cryptKeyHandle);

        [DllImport("Crypt32.dll", ExactSpelling = true, SetLastError = true)]
        internal static extern IntPtr CertCreateSelfSignCertificate(
            IntPtr providerHandle,
            [In] CryptoApiBlob subjectIssuerBlob,
            int flags,
            [In] CryptKeyProviderInformation keyProviderInfo,
            IntPtr signatureAlgorithm,
            [In] SystemTime startTime,
            [In] SystemTime endTime,
            IntPtr extensions);

        [DllImport("Crypt32.dll", ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CertFreeCertificateContext(IntPtr certContext);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FileTimeToSystemTime(
            [In] ref long fileTime,
            [Out] SystemTime systemTime);

        [StructLayout(LayoutKind.Sequential)]
        internal class CryptoApiBlob
        {
            public int DataLength;
            public IntPtr Data;

            public CryptoApiBlob(int dataLength, IntPtr data)
            {
                this.DataLength = dataLength;
                this.Data = data;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal class SystemTime
        {
            public short Year;
            public short Month;
            public short DayOfWeek;
            public short Day;
            public short Hour;
            public short Minute;
            public short Second;
            public short Milliseconds;
        }

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
        internal class CryptKeyProviderInformation
        {
            public string ContainerName;
            public string ProviderName;
            public int ProviderType;
            public int Flags;
            public int ProviderParameterCount;
            public IntPtr ProviderParameters;
            public int KeySpec;
        }
    }
}
