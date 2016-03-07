using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace System
{
    [FriendAccessAllowed]
    public enum EventTask
    {
        None,
    }

    internal struct EventData
    {
      internal long m_Ptr;
      internal int m_Size;
      internal int m_Reserved;

      public IntPtr DataPointer
      {
        get
        {
          return (IntPtr) this.m_Ptr;
        }
        set
        {
          this.m_Ptr = (long) value;
        }
      }


      public int Size
      {
        get
        {
          return this.m_Size;
        }
        set
        {
          this.m_Size = value;
        }
      }

      [SecurityCritical]
      internal unsafe void SetMetadata(byte* pointer, int size, int reserved)
      {
        this.m_Ptr = (long) (ulong) (UIntPtr) ((void*) pointer);
        this.m_Size = size;
        this.m_Reserved = reserved;
      }
    }
}
