using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security.AccessControl;
using System.Web.Hosting;
using System.Web.Util;

namespace System.Web
{
    internal static class FileSecurity
    {
/*        private const int DACL_INFORMATION =
            UnsafeNativeMethods.DACL_SECURITY_INFORMATION |
            UnsafeNativeMethods.GROUP_SECURITY_INFORMATION |
            UnsafeNativeMethods.OWNER_SECURITY_INFORMATION;*/

        private static readonly object syncRoot = new object();
        private static readonly Dictionary<string, FileSystemSecurity> s_interned;

        static FileSecurity()
        {
            s_interned = new Dictionary<string, FileSystemSecurity>(0);
        }

        [SuppressMessage("Microsoft.Interoperability", "CA1404:CallGetLastErrorImmediatelyAfterPInvoke",
            Justification =
                "[....]: Call to GetLastWin32Error() does follow P/Invoke call that is outside the if/else block.")]
        internal static FileSystemSecurity GetDacl(string filename)
        {
            if (HostingEnvironment.FcnSkipReadAndCacheDacls)
            {
                return null;
            }
            FileSystemSecurity accessControl;
            bool found;
            lock (syncRoot)
            {
                found = s_interned.TryGetValue(filename, out accessControl);
            }
            if (!found)
            {
                Exception exception;
                int errorCode;
                var res = FileUtil.TryGetAccessControl(filename, out accessControl, out exception, out errorCode);
                if (res != FileAccessResult.Ok)
                {
                    Debug.Trace("Get Access Control failed",
                        string.Format("Returning null dacl for {0} because : {1}", filename, exception));
                    return null;
                }
                lock (syncRoot)
                {
                    s_interned[filename] = accessControl;
                }
            }
            return accessControl;
        }
    }
}