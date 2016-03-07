//
// System.Web.Caching.UsageBucket.cs
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

using System.Threading;

namespace System.Web.Caching
{
    internal sealed class UsageBucket
    {
        internal const int NUM_ENTRIES = 127;
        private const int LENGTH_ENTRIES = 128;
        private const int MIN_PAGES_INCREMENT = 10;
        private const int MAX_PAGES_INCREMENT = 340;
        private const double MIN_LOAD_FACTOR = 0.5;
        private UsageEntryRef _addRef2Head;
        private bool _blockReduce;
        private byte _bucket;
        private readonly CacheUsage _cacheUsage;
        private int _cEntriesInFlush;
        private int _cEntriesInUse;
        private int _cPagesInUse;
        private UsagePageList _freeEntryList;
        private UsagePageList _freePageList;
        private UsageEntryRef _lastRefHead;
        private UsageEntryRef _lastRefTail;
        private int _minEntriesInUse;
        private UsagePage[] _pages;

        internal UsageBucket(CacheUsage cacheUsage, byte bucket)
        {
            _cacheUsage = cacheUsage;
            _bucket = bucket;
            InitZeroPages();
        }

        private void InitZeroPages()
        {
            _pages = (UsagePage[]) null;
            _minEntriesInUse = -1;
            _freePageList._head = -1;
            _freePageList._tail = -1;
            _freeEntryList._head = -1;
            _freeEntryList._tail = -1;
        }

        private void AddToListHead(int pageIndex, ref UsagePageList list)
        {
            _pages[pageIndex]._pagePrev = -1;
            _pages[pageIndex]._pageNext = list._head;
            if (list._head != -1)
                _pages[list._head]._pagePrev = pageIndex;
            else
                list._tail = pageIndex;
            list._head = pageIndex;
        }

        private void AddToListTail(int pageIndex, ref UsagePageList list)
        {
            _pages[pageIndex]._pageNext = -1;
            _pages[pageIndex]._pagePrev = list._tail;
            if (list._tail != -1)
                _pages[list._tail]._pageNext = pageIndex;
            else
                list._head = pageIndex;
            list._tail = pageIndex;
        }

        private int RemoveFromListHead(ref UsagePageList list)
        {
            int pageIndex = list._head;
            RemoveFromList(pageIndex, ref list);
            return pageIndex;
        }

        private void RemoveFromList(int pageIndex, ref UsagePageList list)
        {
            if (_pages[pageIndex]._pagePrev != -1)
                _pages[_pages[pageIndex]._pagePrev]._pageNext = _pages[pageIndex]._pageNext;
            else
                list._head = _pages[pageIndex]._pageNext;
            if (_pages[pageIndex]._pageNext != -1)
                _pages[_pages[pageIndex]._pageNext]._pagePrev = _pages[pageIndex]._pagePrev;
            else
                list._tail = _pages[pageIndex]._pagePrev;
            _pages[pageIndex]._pagePrev = -1;
            _pages[pageIndex]._pageNext = -1;
        }

        private void MoveToListHead(int pageIndex, ref UsagePageList list)
        {
            if (list._head == pageIndex)
                return;
            RemoveFromList(pageIndex, ref list);
            AddToListHead(pageIndex, ref list);
        }

        private void MoveToListTail(int pageIndex, ref UsagePageList list)
        {
            if (list._tail == pageIndex)
                return;
            RemoveFromList(pageIndex, ref list);
            AddToListTail(pageIndex, ref list);
        }

        private void UpdateMinEntries()
        {
            if (_cPagesInUse <= 1)
            {
                _minEntriesInUse = -1;
            }
            else
            {
                _minEntriesInUse = (int) (_cPagesInUse*sbyte.MaxValue*0.5);
                if (_minEntriesInUse - 1 <= (_cPagesInUse - 1)*sbyte.MaxValue)
                    return;
                _minEntriesInUse = -1;
            }
        }

        private void RemovePage(int pageIndex)
        {
            RemoveFromList(pageIndex, ref _freeEntryList);
            AddToListHead(pageIndex, ref _freePageList);
            _pages[pageIndex]._entries = (UsageEntry[]) null;
            _cPagesInUse = _cPagesInUse - 1;
            if (_cPagesInUse == 0)
                InitZeroPages();
            else
                UpdateMinEntries();
        }

        private UsageEntryRef GetFreeUsageEntry()
        {
            int pageIndex = _freeEntryList._head;
            UsageEntry[] usageEntryArray = _pages[pageIndex]._entries;
            int ref1Index = usageEntryArray[0]._ref1._next.Ref1Index;
            usageEntryArray[0]._ref1._next = usageEntryArray[ref1Index]._ref1._next;
            --usageEntryArray[0]._cFree;
            if (usageEntryArray[0]._cFree == 0)
                RemoveFromList(pageIndex, ref _freeEntryList);
            return new UsageEntryRef(pageIndex, ref1Index);
        }

        private void AddUsageEntryToFreeList(UsageEntryRef entryRef)
        {
            UsageEntry[] usageEntryArray = _pages[entryRef.PageIndex]._entries;
            var ref1Index = entryRef.Ref1Index;
            usageEntryArray[ref1Index]._utcDate = DateTime.MinValue;
            usageEntryArray[ref1Index]._ref1._prev = UsageEntryRef.INVALID;
            usageEntryArray[ref1Index]._ref2._next = UsageEntryRef.INVALID;
            usageEntryArray[ref1Index]._ref2._prev = UsageEntryRef.INVALID;
            usageEntryArray[ref1Index]._ref1._next = usageEntryArray[0]._ref1._next;
            usageEntryArray[0]._ref1._next = entryRef;
            _cEntriesInUse = _cEntriesInUse - 1;
            var pageIndex = entryRef.PageIndex;
            ++usageEntryArray[0]._cFree;
            if (usageEntryArray[0]._cFree == 1)
            {
                AddToListHead(pageIndex, ref _freeEntryList);
            }
            else
            {
                if (usageEntryArray[0]._cFree != sbyte.MaxValue)
                    return;
                RemovePage(pageIndex);
            }
        }

        private void Expand()
        {
            if (_freePageList._head == -1)
            {
                var index1 = _pages != null ? _pages.Length : 0;
                var val2 = index1*2;
                UsagePage[] usagePageArray1 = new UsagePage[Math.Min(Math.Max(index1 + 10, val2), index1 + 340)];
                for (var index2 = 0; index2 < index1; ++index2)
                    usagePageArray1[index2] = _pages[index2];
                for (var index2 = index1; index2 < usagePageArray1.Length; ++index2)
                {
                    usagePageArray1[index2]._pagePrev = index2 - 1;
                    usagePageArray1[index2]._pageNext = index2 + 1;
                }
                usagePageArray1[index1]._pagePrev = -1;
                UsagePage[] usagePageArray2 = usagePageArray1;
                var index3 = usagePageArray2.Length - 1;
                usagePageArray2[index3]._pageNext = -1;
                _freePageList._head = index1;
                _freePageList._tail = usagePageArray1.Length - 1;
                _pages = usagePageArray1;
            }
            var pageIndex = RemoveFromListHead(ref _freePageList);
            AddToListHead(pageIndex, ref _freeEntryList);
            UsageEntry[] usageEntryArray1 = new UsageEntry[128];
            usageEntryArray1[0]._cFree = (int) sbyte.MaxValue;
            for (var index = 0; index < usageEntryArray1.Length - 1; ++index)
                usageEntryArray1[index]._ref1._next = new UsageEntryRef(pageIndex, index + 1);
            UsageEntry[] usageEntryArray2 = usageEntryArray1;
            var index4 = usageEntryArray2.Length - 1;
            usageEntryArray2[index4]._ref1._next = UsageEntryRef.INVALID;
            _pages[pageIndex]._entries = usageEntryArray1;
            _cPagesInUse = _cPagesInUse + 1;
            UpdateMinEntries();
        }

        private void Reduce()
        {
            if (_cEntriesInUse >= _minEntriesInUse || _blockReduce)
                return;
            var num1 = 63;
            int num2 = _freeEntryList._tail;
            int pageIndex = _freeEntryList._head;
            while (true)
            {
                int num3 = _pages[pageIndex]._pageNext;
                if (_pages[pageIndex]._entries[0]._cFree > num1)
                    MoveToListTail(pageIndex, ref _freeEntryList);
                else
                    MoveToListHead(pageIndex, ref _freeEntryList);
                if (pageIndex != num2)
                    pageIndex = num3;
                else
                    break;
            }
            while (_freeEntryList._tail != -1)
            {
                UsageEntry[] usageEntryArray1 = _pages[_freeEntryList._tail]._entries;
                if (_cPagesInUse*sbyte.MaxValue - usageEntryArray1[0]._cFree - _cEntriesInUse <
                    sbyte.MaxValue - usageEntryArray1[0]._cFree)
                    break;
                for (var entryIndex = 1; entryIndex < usageEntryArray1.Length; ++entryIndex)
                {
                    if (usageEntryArray1[entryIndex]._cacheEntry != null)
                    {
                        var freeUsageEntry = GetFreeUsageEntry();
                        var usageEntryRef1 = new UsageEntryRef(freeUsageEntry.PageIndex, -freeUsageEntry.Ref1Index);
                        var usageEntryRef2 = new UsageEntryRef(_freeEntryList._tail, entryIndex);
                        var usageEntryRef3 = new UsageEntryRef(usageEntryRef2.PageIndex, -usageEntryRef2.Ref1Index);
                        usageEntryArray1[entryIndex]._cacheEntry.UsageEntryRef = freeUsageEntry;
                        UsageEntry[] usageEntryArray2 = _pages[freeUsageEntry.PageIndex]._entries;
                        var ref1Index1 = freeUsageEntry.Ref1Index;
                        UsageEntry usageEntry = usageEntryArray1[entryIndex];
                        usageEntryArray2[ref1Index1] = usageEntry;
                        ++usageEntryArray1[0]._cFree;
                        var ref1Index2 = freeUsageEntry.Ref1Index;
                        UsageEntryRef usageEntryRef4 = usageEntryArray2[ref1Index2]._ref1._prev;
                        var ref1Index3 = freeUsageEntry.Ref1Index;
                        UsageEntryRef usageEntryRef5 = usageEntryArray2[ref1Index3]._ref1._next;
                        if (usageEntryRef5 == usageEntryRef3)
                            usageEntryRef5 = usageEntryRef1;
                        if (usageEntryRef4.IsRef1)
                            _pages[usageEntryRef4.PageIndex]._entries[usageEntryRef4.Ref1Index]._ref1._next =
                                freeUsageEntry;
                        else if (usageEntryRef4.IsRef2)
                            _pages[usageEntryRef4.PageIndex]._entries[usageEntryRef4.Ref2Index]._ref2._next =
                                freeUsageEntry;
                        else
                            _lastRefHead = freeUsageEntry;
                        if (usageEntryRef5.IsRef1)
                            _pages[usageEntryRef5.PageIndex]._entries[usageEntryRef5.Ref1Index]._ref1._prev =
                                freeUsageEntry;
                        else if (usageEntryRef5.IsRef2)
                            _pages[usageEntryRef5.PageIndex]._entries[usageEntryRef5.Ref2Index]._ref2._prev =
                                freeUsageEntry;
                        else
                            _lastRefTail = freeUsageEntry;
                        var ref1Index4 = freeUsageEntry.Ref1Index;
                        usageEntryRef4 = usageEntryArray2[ref1Index4]._ref2._prev;
                        if (usageEntryRef4 == usageEntryRef2)
                            usageEntryRef4 = freeUsageEntry;
                        var ref1Index5 = freeUsageEntry.Ref1Index;
                        usageEntryRef5 = usageEntryArray2[ref1Index5]._ref2._next;
                        if (usageEntryRef4.IsRef1)
                            _pages[usageEntryRef4.PageIndex]._entries[usageEntryRef4.Ref1Index]._ref1._next =
                                usageEntryRef1;
                        else if (usageEntryRef4.IsRef2)
                            _pages[usageEntryRef4.PageIndex]._entries[usageEntryRef4.Ref2Index]._ref2._next =
                                usageEntryRef1;
                        else
                            _lastRefHead = usageEntryRef1;
                        if (usageEntryRef5.IsRef1)
                            _pages[usageEntryRef5.PageIndex]._entries[usageEntryRef5.Ref1Index]._ref1._prev =
                                usageEntryRef1;
                        else if (usageEntryRef5.IsRef2)
                            _pages[usageEntryRef5.PageIndex]._entries[usageEntryRef5.Ref2Index]._ref2._prev =
                                usageEntryRef1;
                        else
                            _lastRefTail = usageEntryRef1;
                        if (_addRef2Head == usageEntryRef3)
                            _addRef2Head = usageEntryRef1;
                    }
                }
                RemovePage(_freeEntryList._tail);
            }
        }

        internal void AddCacheEntry(CacheEntry cacheEntry)
        {
            var usageBucket = this;
            var lockTaken = false;
            try
            {
                Monitor.Enter(usageBucket, ref lockTaken);
                if (_freeEntryList._head == -1)
                    Expand();
                var freeUsageEntry = GetFreeUsageEntry();
                var usageEntryRef1 = new UsageEntryRef(freeUsageEntry.PageIndex, -freeUsageEntry.Ref1Index);
                cacheEntry.UsageEntryRef = freeUsageEntry;
                UsageEntry[] usageEntryArray = _pages[freeUsageEntry.PageIndex]._entries;
                var ref1Index = freeUsageEntry.Ref1Index;
                usageEntryArray[ref1Index]._cacheEntry = cacheEntry;
                usageEntryArray[ref1Index]._utcDate = DateTime.UtcNow;
                usageEntryArray[ref1Index]._ref1._prev = UsageEntryRef.INVALID;
                usageEntryArray[ref1Index]._ref2._next = _addRef2Head;
                if (_lastRefHead.IsInvalid)
                {
                    usageEntryArray[ref1Index]._ref1._next = usageEntryRef1;
                    usageEntryArray[ref1Index]._ref2._prev = freeUsageEntry;
                    _lastRefTail = usageEntryRef1;
                }
                else
                {
                    usageEntryArray[ref1Index]._ref1._next = _lastRefHead;
                    if (_lastRefHead.IsRef1)
                        _pages[_lastRefHead.PageIndex]._entries[_lastRefHead.Ref1Index]._ref1._prev = freeUsageEntry;
                    else if (_lastRefHead.IsRef2)
                        _pages[_lastRefHead.PageIndex]._entries[_lastRefHead.Ref2Index]._ref2._prev = freeUsageEntry;
                    else
                        _lastRefTail = freeUsageEntry;
                    UsageEntryRef usageEntryRef2;
                    UsageEntryRef usageEntryRef3;
                    if (_addRef2Head.IsInvalid)
                    {
                        usageEntryRef2 = _lastRefTail;
                        usageEntryRef3 = UsageEntryRef.INVALID;
                    }
                    else
                    {
                        usageEntryRef2 = _pages[_addRef2Head.PageIndex]._entries[_addRef2Head.Ref2Index]._ref2._prev;
                        usageEntryRef3 = _addRef2Head;
                    }
                    usageEntryArray[ref1Index]._ref2._prev = usageEntryRef2;
                    if (usageEntryRef2.IsRef1)
                        _pages[usageEntryRef2.PageIndex]._entries[usageEntryRef2.Ref1Index]._ref1._next = usageEntryRef1;
                    else if (usageEntryRef2.IsRef2)
                        _pages[usageEntryRef2.PageIndex]._entries[usageEntryRef2.Ref2Index]._ref2._next = usageEntryRef1;
                    else
                        _lastRefHead = usageEntryRef1;
                    if (usageEntryRef3.IsRef1)
                        _pages[usageEntryRef3.PageIndex]._entries[usageEntryRef3.Ref1Index]._ref1._prev = usageEntryRef1;
                    else if (usageEntryRef3.IsRef2)
                        _pages[usageEntryRef3.PageIndex]._entries[usageEntryRef3.Ref2Index]._ref2._prev = usageEntryRef1;
                    else
                        _lastRefTail = usageEntryRef1;
                }
                _lastRefHead = freeUsageEntry;
                _addRef2Head = usageEntryRef1;
                _cEntriesInUse = _cEntriesInUse + 1;
            }
            finally
            {
                if (lockTaken)
                    Monitor.Exit(usageBucket);
            }
        }

        private void RemoveEntryFromLastRefList(UsageEntryRef entryRef)
        {
            UsageEntry[] usageEntryArray = _pages[entryRef.PageIndex]._entries;
            var ref1Index = entryRef.Ref1Index;
            var index1 = ref1Index;
            UsageEntryRef usageEntryRef1 = usageEntryArray[index1]._ref1._prev;
            var index2 = ref1Index;
            UsageEntryRef usageEntryRef2 = usageEntryArray[index2]._ref1._next;
            if (usageEntryRef1.IsRef1)
                _pages[usageEntryRef1.PageIndex]._entries[usageEntryRef1.Ref1Index]._ref1._next = usageEntryRef2;
            else if (usageEntryRef1.IsRef2)
                _pages[usageEntryRef1.PageIndex]._entries[usageEntryRef1.Ref2Index]._ref2._next = usageEntryRef2;
            else
                _lastRefHead = usageEntryRef2;
            if (usageEntryRef2.IsRef1)
                _pages[usageEntryRef2.PageIndex]._entries[usageEntryRef2.Ref1Index]._ref1._prev = usageEntryRef1;
            else if (usageEntryRef2.IsRef2)
                _pages[usageEntryRef2.PageIndex]._entries[usageEntryRef2.Ref2Index]._ref2._prev = usageEntryRef1;
            else
                _lastRefTail = usageEntryRef1;
            var index3 = ref1Index;
            usageEntryRef1 = usageEntryArray[index3]._ref2._prev;
            var index4 = ref1Index;
            usageEntryRef2 = usageEntryArray[index4]._ref2._next;
            var usageEntryRef3 = new UsageEntryRef(entryRef.PageIndex, -entryRef.Ref1Index);
            if (usageEntryRef1.IsRef1)
                _pages[usageEntryRef1.PageIndex]._entries[usageEntryRef1.Ref1Index]._ref1._next = usageEntryRef2;
            else if (usageEntryRef1.IsRef2)
                _pages[usageEntryRef1.PageIndex]._entries[usageEntryRef1.Ref2Index]._ref2._next = usageEntryRef2;
            else
                _lastRefHead = usageEntryRef2;
            if (usageEntryRef2.IsRef1)
                _pages[usageEntryRef2.PageIndex]._entries[usageEntryRef2.Ref1Index]._ref1._prev = usageEntryRef1;
            else if (usageEntryRef2.IsRef2)
                _pages[usageEntryRef2.PageIndex]._entries[usageEntryRef2.Ref2Index]._ref2._prev = usageEntryRef1;
            else
                _lastRefTail = usageEntryRef1;
            if (!(_addRef2Head == usageEntryRef3))
                return;
            _addRef2Head = usageEntryRef2;
        }

        internal void RemoveCacheEntry(CacheEntry cacheEntry)
        {
            var usageBucket = this;
            var lockTaken = false;
            try
            {
                Monitor.Enter(usageBucket, ref lockTaken);
                var usageEntryRef = cacheEntry.UsageEntryRef;
                if (usageEntryRef.IsInvalid)
                    return;
                UsageEntry[] usageEntryArray = _pages[usageEntryRef.PageIndex]._entries;
                var ref1Index = usageEntryRef.Ref1Index;
                cacheEntry.UsageEntryRef = UsageEntryRef.INVALID;
                var index = ref1Index;
                usageEntryArray[index]._cacheEntry = (CacheEntry) null;
                RemoveEntryFromLastRefList(usageEntryRef);
                AddUsageEntryToFreeList(usageEntryRef);
                Reduce();
            }
            finally
            {
                if (lockTaken)
                    Monitor.Exit(usageBucket);
            }
        }

        internal void UpdateCacheEntry(CacheEntry cacheEntry)
        {
            var usageBucket = this;
            var lockTaken = false;
            try
            {
                Monitor.Enter(usageBucket, ref lockTaken);
                var usageEntryRef1 = cacheEntry.UsageEntryRef;
                if (usageEntryRef1.IsInvalid)
                    return;
                UsageEntry[] usageEntryArray = _pages[usageEntryRef1.PageIndex]._entries;
                var ref1Index = usageEntryRef1.Ref1Index;
                var usageEntryRef2 = new UsageEntryRef(usageEntryRef1.PageIndex, -usageEntryRef1.Ref1Index);
                UsageEntryRef usageEntryRef3 = usageEntryArray[ref1Index]._ref2._prev;
                UsageEntryRef usageEntryRef4 = usageEntryArray[ref1Index]._ref2._next;
                if (usageEntryRef3.IsRef1)
                    _pages[usageEntryRef3.PageIndex]._entries[usageEntryRef3.Ref1Index]._ref1._next = usageEntryRef4;
                else if (usageEntryRef3.IsRef2)
                    _pages[usageEntryRef3.PageIndex]._entries[usageEntryRef3.Ref2Index]._ref2._next = usageEntryRef4;
                else
                    _lastRefHead = usageEntryRef4;
                if (usageEntryRef4.IsRef1)
                    _pages[usageEntryRef4.PageIndex]._entries[usageEntryRef4.Ref1Index]._ref1._prev = usageEntryRef3;
                else if (usageEntryRef4.IsRef2)
                    _pages[usageEntryRef4.PageIndex]._entries[usageEntryRef4.Ref2Index]._ref2._prev = usageEntryRef3;
                else
                    _lastRefTail = usageEntryRef3;
                if (_addRef2Head == usageEntryRef2)
                    _addRef2Head = usageEntryRef4;
                usageEntryArray[ref1Index]._ref2 = usageEntryArray[ref1Index]._ref1;
                usageEntryRef3 = usageEntryArray[ref1Index]._ref2._prev;
                usageEntryRef4 = usageEntryArray[ref1Index]._ref2._next;
                if (usageEntryRef3.IsRef1)
                    _pages[usageEntryRef3.PageIndex]._entries[usageEntryRef3.Ref1Index]._ref1._next = usageEntryRef2;
                else if (usageEntryRef3.IsRef2)
                    _pages[usageEntryRef3.PageIndex]._entries[usageEntryRef3.Ref2Index]._ref2._next = usageEntryRef2;
                else
                    _lastRefHead = usageEntryRef2;
                if (usageEntryRef4.IsRef1)
                    _pages[usageEntryRef4.PageIndex]._entries[usageEntryRef4.Ref1Index]._ref1._prev = usageEntryRef2;
                else if (usageEntryRef4.IsRef2)
                    _pages[usageEntryRef4.PageIndex]._entries[usageEntryRef4.Ref2Index]._ref2._prev = usageEntryRef2;
                else
                    _lastRefTail = usageEntryRef2;
                usageEntryArray[ref1Index]._ref1._prev = UsageEntryRef.INVALID;
                usageEntryArray[ref1Index]._ref1._next = _lastRefHead;
                if (_lastRefHead.IsRef1)
                    _pages[_lastRefHead.PageIndex]._entries[_lastRefHead.Ref1Index]._ref1._prev = usageEntryRef1;
                else if (_lastRefHead.IsRef2)
                    _pages[_lastRefHead.PageIndex]._entries[_lastRefHead.Ref2Index]._ref2._prev = usageEntryRef1;
                else
                    _lastRefTail = usageEntryRef1;
                _lastRefHead = usageEntryRef1;
            }
            finally
            {
                if (lockTaken)
                    Monitor.Exit(usageBucket);
            }
        }

        internal int FlushUnderUsedItems(int maxFlush, bool force, ref int publicEntriesFlushed,
            ref int ocEntriesFlushed)
        {
            if (_cEntriesInUse == 0)
                return 0;
            var usageEntryRef1 = UsageEntryRef.INVALID;
            var num = 0;
            try
            {
                _cacheUsage.CacheSingle.BlockInsertIfNeeded();
                var usageBucket = this;
                var lockTaken = false;
                try
                {
                    Monitor.Enter(usageBucket, ref lockTaken);
                    if (_cEntriesInUse == 0)
                        return 0;
                    var utcNow = DateTime.UtcNow;
                    UsageEntryRef usageEntryRef2;
                    for (var usageEntryRef3 = _lastRefTail;
                        _cEntriesInFlush < maxFlush && !usageEntryRef3.IsInvalid;
                        usageEntryRef3 = usageEntryRef2)
                    {
                        usageEntryRef2 = _pages[usageEntryRef3.PageIndex]._entries[usageEntryRef3.Ref2Index]._ref2._prev;
                        while (usageEntryRef2.IsRef1)
                            usageEntryRef2 =
                                _pages[usageEntryRef2.PageIndex]._entries[usageEntryRef2.Ref1Index]._ref1._prev;
                        UsageEntry[] usageEntryArray = _pages[usageEntryRef3.PageIndex]._entries;
                        var ref2Index = usageEntryRef3.Ref2Index;
                        if (!force)
                        {
                            DateTime dateTime = usageEntryArray[ref2Index]._utcDate;
                            if (utcNow - dateTime <= CacheUsage.NEWADD_INTERVAL && utcNow >= dateTime)
                                continue;
                        }
                        var entryRef = new UsageEntryRef(usageEntryRef3.PageIndex, usageEntryRef3.Ref2Index);
                        CacheEntry cacheEntry = usageEntryArray[ref2Index]._cacheEntry;
                        cacheEntry.UsageEntryRef = UsageEntryRef.INVALID;
                        if (cacheEntry.IsPublic)
                            ++publicEntriesFlushed;
                        else if (cacheEntry.IsOutputCache)
                            ++ocEntriesFlushed;
                        RemoveEntryFromLastRefList(entryRef);
                        usageEntryArray[ref2Index]._ref1._next = usageEntryRef1;
                        usageEntryRef1 = entryRef;
                        ++num;
                        _cEntriesInFlush = _cEntriesInFlush + 1;
                    }
                    if (num == 0)
                        return 0;
                    _blockReduce = true;
                }
                finally
                {
                    if (lockTaken)
                        Monitor.Exit(usageBucket);
                }
            }
            finally
            {
                _cacheUsage.CacheSingle.UnblockInsert();
            }
            var cacheSingle = _cacheUsage.CacheSingle;
            UsageEntryRef entryRef1;
            UsageEntryRef usageEntryRef4;
            for (entryRef1 = usageEntryRef1; !entryRef1.IsInvalid; entryRef1 = usageEntryRef4)
            {
                UsageEntry[] usageEntryArray = _pages[entryRef1.PageIndex]._entries;
                var ref1Index = entryRef1.Ref1Index;
                usageEntryRef4 = usageEntryArray[ref1Index]._ref1._next;
                CacheEntry cacheEntry = usageEntryArray[ref1Index]._cacheEntry;
                usageEntryArray[ref1Index]._cacheEntry = (CacheEntry) null;
                cacheSingle.Remove(cacheEntry, CacheItemRemovedReason.Underused);
            }
            try
            {
                _cacheUsage.CacheSingle.BlockInsertIfNeeded();
                var usageBucket = this;
                var lockTaken = false;
                try
                {
                    Monitor.Enter(usageBucket, ref lockTaken);
                    UsageEntryRef usageEntryRef2;
                    for (entryRef1 = usageEntryRef1; !entryRef1.IsInvalid; entryRef1 = usageEntryRef2)
                    {
                        usageEntryRef2 = _pages[entryRef1.PageIndex]._entries[entryRef1.Ref1Index]._ref1._next;
                        _cEntriesInFlush = _cEntriesInFlush - 1;
                        AddUsageEntryToFreeList(entryRef1);
                    }
                    _blockReduce = false;
                    Reduce();
                }
                finally
                {
                    if (lockTaken)
                        Monitor.Exit(usageBucket);
                }
            }
            finally
            {
                _cacheUsage.CacheSingle.UnblockInsert();
            }
            return num;
        }
    }
}