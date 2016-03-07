//
// System.Web.Util.SystemInfo.cs
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

using System.Diagnostics;

namespace System.Web.Util
{
    internal static class SystemInfo
    {
        private static int _trueNumberOfProcessors;

        internal static int GetNumProcessCPUs()
        {
            if (_trueNumberOfProcessors == 0)
            {
                var process = Process.GetCurrentProcess();
                var numProcessors = 0;
                var processAffinityMask = process.ProcessorAffinity;
                if (((int) processAffinityMask) > 0)
                {
                    if (IntPtr.Size == 4)
                    {
                        var mask = (uint) processAffinityMask;
                        for (; mask != 0; mask >>= 1)
                        {
                            if ((mask & 1) == 1)
                            {
                                ++numProcessors;
                            }
                        }
                    }
                    else
                    {
                        var mask = (ulong) processAffinityMask;
                        for (; mask != 0; mask >>= 1)
                        {
                            if ((mask & 1) == 1)
                            {
                                ++numProcessors;
                            }
                        }
                    }
                }
                _trueNumberOfProcessors = Math.Max(Environment.ProcessorCount, numProcessors);
            }
            return _trueNumberOfProcessors;
        }
    }
}