//
// System.Web.Caching.CacheUsage.cs
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
    internal class CacheUsage
    {
        private const byte NUMBUCKETS = 5;
        private const int MAX_REMOVE = 1024;
        internal static readonly TimeSpan NEWADD_INTERVAL = new TimeSpan(0, 0, 10);
        internal static readonly TimeSpan CORRELATED_REQUEST_TIMEOUT = new TimeSpan(0, 0, 1);
        internal static readonly TimeSpan MIN_LIFETIME_FOR_USAGE = NEWADD_INTERVAL;
        internal readonly UsageBucket[] _buckets;
        private readonly CacheSingle _cacheSingle;
        private int _inFlush;

        internal CacheUsage(CacheSingle cacheSingle)
        {
            _cacheSingle = cacheSingle;
            _buckets = new UsageBucket[5];
            for (byte bucket = 0; (int) bucket < _buckets.Length; ++bucket)
                _buckets[bucket] = new UsageBucket(this, bucket);
        }

        internal CacheSingle CacheSingle
        {
            get { return _cacheSingle; }
        }

        internal void Add(CacheEntry cacheEntry)
        {
            _buckets[cacheEntry.UsageBucket].AddCacheEntry(cacheEntry);
        }

        internal void Remove(CacheEntry cacheEntry)
        {
            var usageBucket = cacheEntry.UsageBucket;
            if (usageBucket == byte.MaxValue)
                return;
            _buckets[usageBucket].RemoveCacheEntry(cacheEntry);
        }

        internal void Update(CacheEntry cacheEntry)
        {
            var usageBucket = cacheEntry.UsageBucket;
            if (usageBucket == byte.MaxValue)
                return;
            _buckets[usageBucket].UpdateCacheEntry(cacheEntry);
        }

        internal int FlushUnderUsedItems(int toFlush, ref int publicEntriesFlushed, ref int ocEntriesFlushed)
        {
            var num1 = 0;
            if (Interlocked.Exchange(ref _inFlush, 1) == 0)
            {
                try
                {
                    foreach (UsageBucket usageBucket in _buckets)
                    {
                        int num2 = usageBucket.FlushUnderUsedItems(toFlush - num1, false, ref publicEntriesFlushed,
                            ref ocEntriesFlushed);
                        num1 += num2;
                        if (num1 >= toFlush)
                            break;
                    }
                    if (num1 < toFlush)
                    {
                        foreach (UsageBucket usageBucket in _buckets)
                        {
                            int num2 = usageBucket.FlushUnderUsedItems(toFlush - num1, true, ref publicEntriesFlushed,
                                ref ocEntriesFlushed);
                            num1 += num2;
                            if (num1 >= toFlush)
                                break;
                        }
                    }
                }
                finally
                {
                    Interlocked.Exchange(ref _inFlush, 0);
                }
            }
            return num1;
        }
    }
}