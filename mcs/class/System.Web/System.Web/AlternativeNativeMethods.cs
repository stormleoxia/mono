using System.Diagnostics;
using System.Drawing;
using System.Security.Permissions;
using System.Text;

namespace System.Web
{
    // Replacement in terms of feature to UnsafeNativeMethods
    internal static class AlternativeNativeMethods
    {
        public static int GetModuleFileName(IntPtr zero, StringBuilder buf, int i)
        {
            string moduleName = Process.GetCurrentProcess().MainModule.ModuleName;
            buf.Append(moduleName);
            return moduleName.Length;
        }

        public static uint DoesKeyContainerExist(string containerName, string csp, int i)
        {
            return Util.HResults.S_OK;
        }

        public static int ChangeAccessToKeyContainer(string containerName, string account, string csp, int flags)
        {
            return Util.HResults.S_OK;
        }

        public static void DeleteShadowCache(string cachePath, string applicationName)
        {
            
        }

        public static int GetCachePath(int i, StringBuilder buf, ref int iSize)
        {
            // TODO provide path to GAC
            var path = @"/usr/lib/mono";
            buf.Append(path);
            return path.Length;
        }
    }
}