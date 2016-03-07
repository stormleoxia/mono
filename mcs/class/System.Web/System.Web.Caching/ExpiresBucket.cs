//
// System.Web.Caching.ExpiresBucket.cs
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
    internal sealed class ExpiresBucket
    {
        internal const int NUM_ENTRIES = 127;
        private const int LENGTH_ENTRIES = 128;
        private const int MIN_PAGES_INCREMENT = 10;
        private const int MAX_PAGES_INCREMENT = 340;
        private const double MIN_LOAD_FACTOR = 0.5;
        private const int COUNTS_LENGTH = 4;
        private static readonly TimeSpan COUNT_INTERVAL = new TimeSpan(CacheExpires._tsPerBucket.Ticks/4L);
        private readonly byte _bucket;
        private readonly CacheExpires _cacheExpires;
        private bool _blockReduce;
        private int _cEntriesInFlush;
        private int _cEntriesInUse;
        private readonly int[] _counts;
        private int _cPagesInUse;
        private ExpiresPageList _freeEntryList;
        private ExpiresPageList _freePageList;
        private int _minEntriesInUse;
        private ExpiresPage[] _pages;
        private DateTime _utcLastCountReset;
        private DateTime _utcMinExpires;

        internal ExpiresBucket(CacheExpires cacheExpires, byte bucket, DateTime utcNow)
        {
            _cacheExpires = cacheExpires;
            _bucket = bucket;
            _counts = new int[4];
            ResetCounts(utcNow);
            InitZeroPages();
        }

        private void InitZeroPages()
        {
            _pages = null;
            _minEntriesInUse = -1;
            _freePageList._head = -1;
            _freePageList._tail = -1;
            _freeEntryList._head = -1;
            _freeEntryList._tail = -1;
        }

        private void ResetCounts(DateTime utcNow)
        {
            _utcLastCountReset = utcNow;
            _utcMinExpires = DateTime.MaxValue;
            for (var index = 0; index < _counts.Length; ++index)
                _counts[index] = 0;
        }

        private int GetCountIndex(DateTime utcExpires)
        {
            var val1 = 0;
            var timeSpan = utcExpires - _utcLastCountReset;
            var ticks1 = timeSpan.Ticks;
            timeSpan = COUNT_INTERVAL;
            var ticks2 = timeSpan.Ticks;
            var val2 = (int) (ticks1/ticks2);
            return Math.Max(val1, val2);
        }

        private void AddCount(DateTime utcExpires)
        {
            var countIndex = GetCountIndex(utcExpires);
            for (var index = _counts.Length - 1; index >= countIndex; --index)
                ++_counts[index];
            if (!(utcExpires < _utcMinExpires))
                return;
            _utcMinExpires = utcExpires;
        }

        private void RemoveCount(DateTime utcExpires)
        {
            var countIndex = GetCountIndex(utcExpires);
            for (var index = _counts.Length - 1; index >= countIndex; --index)
                --_counts[index];
        }

        private int GetExpiresCount(DateTime utcExpires)
        {
            if (utcExpires < _utcMinExpires)
                return 0;
            var countIndex = GetCountIndex(utcExpires);
            if (countIndex >= _counts.Length)
                return _cEntriesInUse;
            return _counts[countIndex];
        }

        private void AddToListHead(int pageIndex, ref ExpiresPageList list)
        {
            _pages[pageIndex]._pagePrev = -1;
            _pages[pageIndex]._pageNext = list._head;
            if (list._head != -1)
                _pages[list._head]._pagePrev = pageIndex;
            else
                list._tail = pageIndex;
            list._head = pageIndex;
        }

        private void AddToListTail(int pageIndex, ref ExpiresPageList list)
        {
            _pages[pageIndex]._pageNext = -1;
            _pages[pageIndex]._pagePrev = list._tail;
            if (list._tail != -1)
                _pages[list._tail]._pageNext = pageIndex;
            else
                list._head = pageIndex;
            list._tail = pageIndex;
        }

        private int RemoveFromListHead(ref ExpiresPageList list)
        {
            var pageIndex = list._head;
            RemoveFromList(pageIndex, ref list);
            return pageIndex;
        }

        private void RemoveFromList(int pageIndex, ref ExpiresPageList list)
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

        private void MoveToListHead(int pageIndex, ref ExpiresPageList list)
        {
            if (list._head == pageIndex)
                return;
            RemoveFromList(pageIndex, ref list);
            AddToListHead(pageIndex, ref list);
        }

        private void MoveToListTail(int pageIndex, ref ExpiresPageList list)
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
            _pages[pageIndex]._entries = null;
            _cPagesInUse = _cPagesInUse - 1;
            if (_cPagesInUse == 0)
                InitZeroPages();
            else
                UpdateMinEntries();
        }

        private ExpiresEntryRef GetFreeExpiresEntry()
        {
            var pageIndex = _freeEntryList._head;
            var expiresEntryArray = _pages[pageIndex]._entries;
            var index = expiresEntryArray[0]._next.Index;
            expiresEntryArray[0]._next = expiresEntryArray[index]._next;
            --expiresEntryArray[0]._cFree;
            if (expiresEntryArray[0]._cFree == 0)
                RemoveFromList(pageIndex, ref _freeEntryList);
            return new ExpiresEntryRef(pageIndex, index);
        }

        private void AddExpiresEntryToFreeList(ExpiresEntryRef entryRef)
        {
            var expiresEntryArray = _pages[entryRef.PageIndex]._entries;
            var index = entryRef.Index;
            expiresEntryArray[index]._cFree = 0;
            expiresEntryArray[index]._next = expiresEntryArray[0]._next;
            expiresEntryArray[0]._next = entryRef;
            _cEntriesInUse = _cEntriesInUse - 1;
            var pageIndex = entryRef.PageIndex;
            ++expiresEntryArray[0]._cFree;
            if (expiresEntryArray[0]._cFree == 1)
            {
                AddToListHead(pageIndex, ref _freeEntryList);
            }
            else
            {
                if (expiresEntryArray[0]._cFree != sbyte.MaxValue)
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
                var expiresPageArray1 = new ExpiresPage[Math.Min(Math.Max(index1 + 10, val2), index1 + 340)];
                for (var index2 = 0; index2 < index1; ++index2)
                    expiresPageArray1[index2] = _pages[index2];
                for (var index2 = index1; index2 < expiresPageArray1.Length; ++index2)
                {
                    expiresPageArray1[index2]._pagePrev = index2 - 1;
                    expiresPageArray1[index2]._pageNext = index2 + 1;
                }
                expiresPageArray1[index1]._pagePrev = -1;
                var expiresPageArray2 = expiresPageArray1;
                var index3 = expiresPageArray2.Length - 1;
                expiresPageArray2[index3]._pageNext = -1;
                _freePageList._head = index1;
                _freePageList._tail = expiresPageArray1.Length - 1;
                _pages = expiresPageArray1;
            }
            var pageIndex = RemoveFromListHead(ref _freePageList);
            AddToListHead(pageIndex, ref _freeEntryList);
            var expiresEntryArray1 = new ExpiresEntry[128];
            expiresEntryArray1[0]._cFree = sbyte.MaxValue;
            for (var index = 0; index < expiresEntryArray1.Length - 1; ++index)
                expiresEntryArray1[index]._next = new ExpiresEntryRef(pageIndex, index + 1);
            var expiresEntryArray2 = expiresEntryArray1;
            var index4 = expiresEntryArray2.Length - 1;
            expiresEntryArray2[index4]._next = ExpiresEntryRef.INVALID;
            _pages[pageIndex]._entries = expiresEntryArray1;
            _cPagesInUse = _cPagesInUse + 1;
            UpdateMinEntries();
        }

        private void Reduce()
        {
            if (_cEntriesInUse >= _minEntriesInUse || _blockReduce)
                return;
            var num1 = 63;
            var num2 = _freeEntryList._tail;
            var pageIndex = _freeEntryList._head;
            while (true)
            {
                var num3 = _pages[pageIndex]._pageNext;
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
                var expiresEntryArray = _pages[_freeEntryList._tail]._entries;
                if (_cPagesInUse*sbyte.MaxValue - expiresEntryArray[0]._cFree - _cEntriesInUse <
                    sbyte.MaxValue - expiresEntryArray[0]._cFree)
                    break;
                for (var index = 1; index < expiresEntryArray.Length; ++index)
                {
                    if (expiresEntryArray[index]._cacheEntry != null)
                    {
                        var freeExpiresEntry = GetFreeExpiresEntry();
                        expiresEntryArray[index]._cacheEntry.ExpiresEntryRef = freeExpiresEntry;
                        _pages[freeExpiresEntry.PageIndex]._entries[freeExpiresEntry.Index] = expiresEntryArray[index];
                        ++expiresEntryArray[0]._cFree;
                    }
                }
                RemovePage(_freeEntryList._tail);
            }
        }

        internal void AddCacheEntry(CacheEntry cacheEntry)
        {
            var expiresBucket = this;
            var lockTaken = false;
            try
            {
                Monitor.Enter(expiresBucket, ref lockTaken);
                if ((cacheEntry.State & (CacheEntry.EntryState) 3) == CacheEntry.EntryState.NotInCache)
                    return;
                var expiresEntryRef = cacheEntry.ExpiresEntryRef;
                if (cacheEntry.ExpiresBucket != byte.MaxValue || !expiresEntryRef.IsInvalid)
                    return;
                if (_freeEntryList._head == -1)
                    Expand();
                var freeExpiresEntry = GetFreeExpiresEntry();
                cacheEntry.ExpiresBucket = _bucket;
                cacheEntry.ExpiresEntryRef = freeExpiresEntry;
                var expiresEntryArray = _pages[freeExpiresEntry.PageIndex]._entries;
                var index1 = freeExpiresEntry.Index;
                var index2 = index1;
                expiresEntryArray[index2]._cacheEntry = cacheEntry;
                var index3 = index1;
                expiresEntryArray[index3]._utcExpires = cacheEntry.UtcExpires;
                AddCount(cacheEntry.UtcExpires);
                _cEntriesInUse = _cEntriesInUse + 1;
                if ((cacheEntry.State & (CacheEntry.EntryState) 3) != CacheEntry.EntryState.NotInCache)
                    return;
                RemoveCacheEntryNoLock(cacheEntry);
            }
            finally
            {
                if (lockTaken)
                    Monitor.Exit(expiresBucket);
            }
        }

        private void RemoveCacheEntryNoLock(CacheEntry cacheEntry)
        {
            var expiresEntryRef = cacheEntry.ExpiresEntryRef;
            if (cacheEntry.ExpiresBucket != _bucket || expiresEntryRef.IsInvalid)
                return;
            var expiresEntryArray = _pages[expiresEntryRef.PageIndex]._entries;
            var index = expiresEntryRef.Index;
            RemoveCount(expiresEntryArray[index]._utcExpires);
            cacheEntry.ExpiresBucket = byte.MaxValue;
            cacheEntry.ExpiresEntryRef = ExpiresEntryRef.INVALID;
            expiresEntryArray[index]._cacheEntry = null;
            AddExpiresEntryToFreeList(expiresEntryRef);
            if (_cEntriesInUse == 0)
                ResetCounts(DateTime.UtcNow);
            Reduce();
        }

        internal void RemoveCacheEntry(CacheEntry cacheEntry)
        {
            var expiresBucket = this;
            var lockTaken = false;
            try
            {
                Monitor.Enter(expiresBucket, ref lockTaken);
                RemoveCacheEntryNoLock(cacheEntry);
            }
            finally
            {
                if (lockTaken)
                    Monitor.Exit(expiresBucket);
            }
        }

        internal void UtcUpdateCacheEntry(CacheEntry cacheEntry, DateTime utcExpires)
        {
            var expiresBucket = this;
            var lockTaken = false;
            try
            {
                Monitor.Enter(expiresBucket, ref lockTaken);
                var expiresEntryRef = cacheEntry.ExpiresEntryRef;
                if (cacheEntry.ExpiresBucket != _bucket || expiresEntryRef.IsInvalid)
                    return;
                var expiresEntryArray = _pages[expiresEntryRef.PageIndex]._entries;
                var index = expiresEntryRef.Index;
                RemoveCount(expiresEntryArray[index]._utcExpires);
                AddCount(utcExpires);
                expiresEntryArray[index]._utcExpires = utcExpires;
                cacheEntry.UtcExpires = utcExpires;
            }
            finally
            {
                if (lockTaken)
                    Monitor.Exit(expiresBucket);
            }
        }

        internal int FlushExpiredItems(DateTime utcNow, bool useInsertBlock)
        {
            if (_cEntriesInUse == 0 || GetExpiresCount(utcNow) == 0)
                return 0;
            var expiresEntryRef1 = ExpiresEntryRef.INVALID;
            var num1 = 0;
            try
            {
                if (useInsertBlock)
                    _cacheExpires.CacheSingle.BlockInsertIfNeeded();
                var expiresBucket = this;
                var lockTaken = false;
                try
                {
                    Monitor.Enter(expiresBucket, ref lockTaken);
                    if (_cEntriesInUse == 0 || GetExpiresCount(utcNow) == 0)
                        return 0;
                    ResetCounts(utcNow);
                    var num2 = _cPagesInUse;
                    for (var pageIndex = 0; pageIndex < _pages.Length; ++pageIndex)
                    {
                        var expiresEntryArray = _pages[pageIndex]._entries;
                        if (expiresEntryArray != null)
                        {
                            var num3 = sbyte.MaxValue - expiresEntryArray[0]._cFree;
                            for (var entryIndex = 1; entryIndex < expiresEntryArray.Length; ++entryIndex)
                            {
                                var cacheEntry = expiresEntryArray[entryIndex]._cacheEntry;
                                if (cacheEntry != null)
                                {
                                    if (expiresEntryArray[entryIndex]._utcExpires > utcNow)
                                    {
                                        AddCount(expiresEntryArray[entryIndex]._utcExpires);
                                    }
                                    else
                                    {
                                        cacheEntry.ExpiresBucket = byte.MaxValue;
                                        cacheEntry.ExpiresEntryRef = ExpiresEntryRef.INVALID;
                                        expiresEntryArray[entryIndex]._cFree = 1;
                                        expiresEntryArray[entryIndex]._next = expiresEntryRef1;
                                        expiresEntryRef1 = new ExpiresEntryRef(pageIndex, entryIndex);
                                        ++num1;
                                        _cEntriesInFlush = _cEntriesInFlush + 1;
                                    }
                                    --num3;
                                    if (num3 == 0)
                                        break;
                                }
                            }
                            --num2;
                            if (num2 == 0)
                                break;
                        }
                    }
                    if (num1 == 0)
                        return 0;
                    _blockReduce = true;
                }
                finally
                {
                    if (lockTaken)
                        Monitor.Exit(expiresBucket);
                }
            }
            finally
            {
                if (useInsertBlock)
                    _cacheExpires.CacheSingle.UnblockInsert();
            }
            var cacheSingle = _cacheExpires.CacheSingle;
            ExpiresEntryRef entryRef;
            ExpiresEntryRef expiresEntryRef2;
            for (entryRef = expiresEntryRef1; !entryRef.IsInvalid; entryRef = expiresEntryRef2)
            {
                var expiresEntryArray = _pages[entryRef.PageIndex]._entries;
                var index = entryRef.Index;
                expiresEntryRef2 = expiresEntryArray[index]._next;
                var cacheEntry = expiresEntryArray[index]._cacheEntry;
                expiresEntryArray[index]._cacheEntry = null;
                cacheSingle.Remove(cacheEntry, CacheItemRemovedReason.Expired);
            }
            try
            {
                if (useInsertBlock)
                    _cacheExpires.CacheSingle.BlockInsertIfNeeded();
                var expiresBucket = this;
                var lockTaken = false;
                try
                {
                    Monitor.Enter(expiresBucket, ref lockTaken);
                    ExpiresEntryRef expiresEntryRef3;
                    for (entryRef = expiresEntryRef1; !entryRef.IsInvalid; entryRef = expiresEntryRef3)
                    {
                        expiresEntryRef3 = _pages[entryRef.PageIndex]._entries[entryRef.Index]._next;
                        _cEntriesInFlush = _cEntriesInFlush - 1;
                        AddExpiresEntryToFreeList(entryRef);
                    }
                    _blockReduce = false;
                    Reduce();
                }
                finally
                {
                    if (lockTaken)
                        Monitor.Exit(expiresBucket);
                }
            }
            finally
            {
                if (useInsertBlock)
                    _cacheExpires.CacheSingle.UnblockInsert();
            }
            return num1;
        }
    }
}