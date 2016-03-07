//
// System.Web.Util.DisposableGCHandleRef.cs
//
// Authors:
//	Fabien Bondi (fbondi@leoxia.com)
//
// Copyright (C) 2015-2016 Leoxia, Inc (http://www.leoxia.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace System.Web.Util
{
  internal class DisposableGCHandleRef<T> : IDisposable where T : class, IDisposable
  {
    private GCHandle _handle;

    public T Target
    {
      [PermissionSet(SecurityAction.Assert, Unrestricted = true)] get
      {
        return (T) this._handle.Target;
      }
    }

    [PermissionSet(SecurityAction.Assert, Unrestricted = true)]
    public DisposableGCHandleRef(T t)
    {
      this._handle = GCHandle.Alloc((object) t);
    }

    [PermissionSet(SecurityAction.Assert, Unrestricted = true)]
    public void Dispose()
    {
      this.Target.Dispose();
      if (!this._handle.IsAllocated)
        return;
      this._handle.Free();
    }
  }
}
