
using System.Diagnostics.Contracts;
using System.Security;

namespace System.Runtime.CompilerServices {

	[FriendAccessAllowed]
	internal static class JitHelpers
	{
		static internal T UnsafeCast<T>(Object o) where T : class
		{
			return (T)o;
		}

        [SecurityCritical]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [MonoTODO("Implement in VM")]
        extern static bool IsAddressInStack(IntPtr ptr);

        // Internal method for getting a raw pointer for handles in JitHelpers.
        // The reference has to point into a local stack variable in order so it can not be moved by the GC.
        [SecurityCritical]
        static internal IntPtr UnsafeCastToStackPointer<T>(ref T val)
        {
            IntPtr p = UnsafeCastToStackPointerInternal<T>(ref val);
            Contract.Assert(IsAddressInStack(p), "Pointer not in the stack!");
            return p;
        }

        [SecurityCritical]
        static private IntPtr UnsafeCastToStackPointerInternal<T>(ref T val)
        {
            // The body of this function will be replaced by the EE with unsafe code that just returns val!!!
            // See getILIntrinsicImplementation for how this happens.  
            throw new InvalidOperationException();
        }
	}
}