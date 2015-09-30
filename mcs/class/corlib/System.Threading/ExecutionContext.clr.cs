using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Security;

namespace System.Threading
{
    public partial class ExecutionContext
    {
        private static readonly ExecutionContext Default = new ExecutionContext();

        [ThreadStatic]
        [SecurityCritical]
        static ExecutionContext t_currentMaybeNull;

        private readonly Dictionary<IAsyncLocal, object> m_localValues;
        private readonly IAsyncLocal[] m_localChangeNotifications;

        private static readonly IAsyncLocal[] emptyArray = new IAsyncLocal[0];

        private ExecutionContext(Dictionary<IAsyncLocal, object> localValues, IAsyncLocal[] localChangeNotifications)
        {
            m_localValues = localValues;
            m_localChangeNotifications = localChangeNotifications;
        }

        [SecurityCritical]
        internal static object GetLocalValue(IAsyncLocal local)
        {
            ExecutionContext current = t_currentMaybeNull;
            if (current == null)
                return null;

            object value;
            current.m_localValues.TryGetValue(local, out value);
            return value;
        }

        [SecurityCritical]
        internal static void SetLocalValue(IAsyncLocal local, object newValue, bool needChangeNotifications)
        {
            ExecutionContext current = t_currentMaybeNull ?? ExecutionContext.Default;

            object previousValue;
            bool hadPreviousValue = current.m_localValues.TryGetValue(local, out previousValue);

            if (previousValue == newValue)
                return;

            //
            // Allocate a new Dictionary containing a copy of the old values, plus the new value.  We have to do this manually to 
            // minimize allocations of IEnumerators, etc.
            //
            Dictionary<IAsyncLocal, object> newValues = new Dictionary<IAsyncLocal, object>(current.m_localValues.Count + (hadPreviousValue ? 0 : 1));

            foreach (KeyValuePair<IAsyncLocal, object> pair in current.m_localValues)
                newValues.Add(pair.Key, pair.Value);

            newValues[local] = newValue;

            //
            // Either copy the change notification array, or create a new one, depending on whether we need to add a new item.
            //
            IAsyncLocal[] newChangeNotifications = current.m_localChangeNotifications;
            if (needChangeNotifications)
            {
                if (hadPreviousValue)
                {
                    Contract.Assert(Array.IndexOf(newChangeNotifications, local) >= 0);
                }
                else
                {
                    int newNotificationIndex = newChangeNotifications.Length;
                    Array.Resize(ref newChangeNotifications, newNotificationIndex + 1);
                    newChangeNotifications[newNotificationIndex] = local;
                }
            }

            t_currentMaybeNull = new ExecutionContext(newValues, newChangeNotifications);

            if (needChangeNotifications)
            {
                local.OnValueChanged(previousValue, newValue, false);
            }
        }
    }
}
