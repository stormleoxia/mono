//
// System.Web.Caching.ExpiresEntryRef.cs
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


namespace System.Web.Caching
{
    internal struct ExpiresEntryRef
    {
        internal static readonly ExpiresEntryRef INVALID = new ExpiresEntryRef(0, 0);
        private const uint ENTRY_MASK = 255U;
        private const uint PAGE_MASK = 4294967040U;
        private const int PAGE_SHIFT = 8;
        private readonly uint _ref;

        internal int PageIndex
        {
            get { return (int) (_ref >> 8); }
        }

        internal int Index
        {
            get { return (int) _ref & byte.MaxValue; }
        }

        internal bool IsInvalid
        {
            get { return (int) _ref == 0; }
        }

        internal ExpiresEntryRef(int pageIndex, int entryIndex)
        {
            _ref = (uint) (pageIndex << 8 | entryIndex & byte.MaxValue);
        }

        public static bool operator !=(ExpiresEntryRef r1, ExpiresEntryRef r2)
        {
            return (int) r1._ref != (int) r2._ref;
        }

        public static bool operator ==(ExpiresEntryRef r1, ExpiresEntryRef r2)
        {
            return (int) r1._ref == (int) r2._ref;
        }

        public override bool Equals(object value)
        {
            if (value is ExpiresEntryRef)
                return (int) _ref == (int) ((ExpiresEntryRef) value)._ref;
            return false;
        }

        public override int GetHashCode()
        {
            return (int) _ref;
        }
    }
}