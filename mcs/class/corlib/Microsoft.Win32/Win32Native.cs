using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.Win32
{
    class Win32Native
    {
        internal const String KERNEL32 = "kernel32.dll";
        internal const String ADVAPI32 = "advapi32.dll";

        private const int FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200;
        private const int FORMAT_MESSAGE_FROM_SYSTEM    = 0x00001000;
        private const int FORMAT_MESSAGE_ARGUMENT_ARRAY = 0x00002000;

        [DllImport(KERNEL32, CharSet=CharSet.Auto, BestFitMapping=true)]
        internal static extern int FormatMessage(int dwFlags, IntPtr lpSource,
                    int dwMessageId, int dwLanguageId, [Out]StringBuilder lpBuffer,
                    int nSize, IntPtr va_list_arguments);

        // Gets an error message for a Win32 error code.
        internal static String GetMessage(int errorCode) {
            StringBuilder sb = StringBuilderCache.Acquire(512);
            int result = Win32Native.FormatMessage(FORMAT_MESSAGE_IGNORE_INSERTS |
                FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_ARGUMENT_ARRAY,
                IntPtr.Zero, errorCode, 0, sb, sb.Capacity, IntPtr.Zero);
            if (result != 0) {
                // result is the # of characters copied to the StringBuilder.
                return StringBuilderCache.GetStringAndRelease(sb);
            }
            else {
                StringBuilderCache.Release(sb);
                return Environment.GetResourceString("UnknownError_Num", errorCode);
            }
        }
    }
}
