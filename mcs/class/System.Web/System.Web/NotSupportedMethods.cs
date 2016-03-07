using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Configuration;

namespace System.Web
{
    class NotSupportedMethods
    {
        public static void StartPrefetchActivity(uint getStringHashCode)
        {
        }

        public static IntPtr GetModuleHandle(object engineFullName)
        {
            return IntPtr.Zero;            
        }

        public static IntPtr LoadLibrary(string fullPath)
        {
            return IntPtr.Zero;            
        }

        public static void InitializeLibrary(bool b)
        {

        }

        public static void PerfCounterInitialize()
        {
           
        }

        public static void EndPrefetchActivity(uint getHashCode)
        {
            
        }

        public static System.Configuration.Configuration Create(string remotewebconfigurationhost, WebLevel webLevel, object o, string getVirtualPathString, string site, string locationSubPath, string server, string userName, string password, IntPtr tokenHandle)
        {
            throw new NotSupportedException("RemoteWebConfiguration is not supported on Mono");
        }

        public static IntPtr GetExtensionlessUrlAppendage()
        {
            throw new NotSupportedException("Feature only available in IIS - so not supported on Mono");
        }

        public static void RaiseFileMonitoringEventlogEvent(string s, string path, string appDomainAppVirtualPath, int hr)
        {
            
        }

        [MonoTODO("No equivalent to event logging on Windows")]
        public static void ReportUnhandledException(string formatExceptionMessage)
        {
            
        }

        public static int GetDirMonConfiguration(out int fcnMode)
        {
            fcnMode = 0;
            return System.Web.Util.HResults.S_OK;
        }

        public static void LogWebeventProviderFailure(string appDomainAppVirtualPath, string name, string toString)
        {
        }

        public static int RaiseEventlogEvent(int eventType, string[] toArray, int count)
        {
            return System.Web.Util.HResults.S_OK;
        }

        public static int InitializeWmiManager()
        {
           return System.Web.Util.HResults.S_OK;
        }

        public static int RaiseWmiEvent(ref UnsafeNativeMethods.WmiData wmiData, bool isInAspCompatMode)
        {
            return System.Web.Util.HResults.S_OK;
        }
    }
}
