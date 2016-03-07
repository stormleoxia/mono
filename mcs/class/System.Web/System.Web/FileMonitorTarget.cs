using System.Globalization;
using System.Text;
using System.Web.Util;

namespace System.Web
{
    internal sealed class FileMonitorTarget
    {
        internal readonly FileChangeEventHandler Callback; // the callback
        internal readonly string Alias; // the filename used to name the file
        internal readonly DateTime UtcStartMonitoring; // time we started monitoring
        private int _refs; // number of uses of callbacks

        internal FileMonitorTarget(FileChangeEventHandler callback, string alias)
        {
            Callback = callback;
            Alias = alias;
            UtcStartMonitoring = DateTime.UtcNow;
            _refs = 1;
        }

        internal int AddRef()
        {
            _refs++;
            return _refs;
        }

        internal int Release()
        {
            _refs--;
            return _refs;
        }

#if DEBUG
        internal string DebugDescription(string indent) {
            StringBuilder   sb = new StringBuilder(200);
            string          i2 = indent + "    ";

            sb.Append(indent + "FileMonitorTarget\n");
            sb.Append(i2 + "       Callback: " + Callback.Target + "(HC=" + Callback.Target.GetHashCode().ToString("x", NumberFormatInfo.InvariantInfo) + ")\n");
            sb.Append(i2 + "          Alias: " + Alias + "\n");
            sb.Append(i2 + "StartMonitoring: " + Debug.FormatUtcDate(UtcStartMonitoring) + "\n");
            sb.Append(i2 + "          _refs: " + _refs + "\n");

            return sb.ToString();
        }
#endif
    }
}