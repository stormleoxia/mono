//
// System.Web.Caching.CacheExpires.cs
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
using System.Threading;
using System.Web.Util;

namespace System.Web.Caching
{
  internal sealed class CacheExpires
  {
    internal static readonly TimeSpan MIN_UPDATE_DELTA = new TimeSpan(0, 0, 1);
    internal static readonly TimeSpan MIN_FLUSH_INTERVAL = new TimeSpan(0, 0, 1);
    internal static readonly TimeSpan _tsPerBucket = new TimeSpan(0, 0, 20);
    private static readonly TimeSpan _tsPerCycle = new TimeSpan(30L * CacheExpires._tsPerBucket.Ticks);
    private const int NUMBUCKETS = 30;
    private readonly CacheSingle _cacheSingle;
    private readonly ExpiresBucket[] _buckets;
    private DisposableGCHandleRef<Timer> _timerHandleRef;
    private DateTime _utcLastFlush;
    private int _inFlush;

    internal CacheSingle CacheSingle
    {
      get
      {
        return this._cacheSingle;
      }
    }

    internal CacheExpires(CacheSingle cacheSingle)
    {
      DateTime utcNow = DateTime.UtcNow;
      this._cacheSingle = cacheSingle;
      this._buckets = new ExpiresBucket[30];
      for (byte bucket = (byte) 0; (int) bucket < this._buckets.Length; ++bucket)
        this._buckets[(int) bucket] = new ExpiresBucket(this, bucket, utcNow);
    }

    private int UtcCalcExpiresBucket(DateTime utcDate)
    {
      return (int) ((utcDate.Ticks % CacheExpires._tsPerCycle.Ticks / CacheExpires._tsPerBucket.Ticks + 1L) % 30L);
    }

    private int FlushExpiredItems(bool checkDelta, bool useInsertBlock)
    {
      int num = 0;
      if (Interlocked.Exchange(ref this._inFlush, 1) == 0)
      {
        try
        {
          if (this._timerHandleRef == null)
            return 0;
          DateTime utcNow = DateTime.UtcNow;
          if (checkDelta && !(utcNow - this._utcLastFlush >= CacheExpires.MIN_FLUSH_INTERVAL))
          {
            if (!(utcNow < this._utcLastFlush))
              goto label_9;
          }
          this._utcLastFlush = utcNow;
          foreach (ExpiresBucket expiresBucket in this._buckets)
            num += expiresBucket.FlushExpiredItems(utcNow, useInsertBlock);
        }
        finally
        {
          Interlocked.Exchange(ref this._inFlush, 0);
        }
      }
label_9:
      return num;
    }

    internal int FlushExpiredItems(bool useInsertBlock)
    {
      return this.FlushExpiredItems(true, useInsertBlock);
    }

    private void TimerCallback(object state)
    {
      this.FlushExpiredItems(false, false);
    }

    internal void EnableExpirationTimer(bool enable)
    {
      if (enable)
      {
        if (this._timerHandleRef != null)
          return;
        DateTime utcNow = DateTime.UtcNow;
        this._timerHandleRef = new DisposableGCHandleRef<Timer>(new Timer(new TimerCallback(this.TimerCallback), (object) null, (CacheExpires._tsPerBucket - new TimeSpan(utcNow.Ticks % CacheExpires._tsPerBucket.Ticks)).Ticks / 10000L, CacheExpires._tsPerBucket.Ticks / 10000L));
      }
      else
      {
        DisposableGCHandleRef<Timer> comparand = this._timerHandleRef;
        if (comparand == null || Interlocked.CompareExchange<DisposableGCHandleRef<Timer>>(ref this._timerHandleRef, (DisposableGCHandleRef<Timer>) null, comparand) != comparand)
          return;
        comparand.Dispose();
        while (this._inFlush != 0)
          Thread.Sleep(100);
      }
    }

    internal void Add(CacheEntry cacheEntry)
    {
      DateTime utcNow = DateTime.UtcNow;
      if (utcNow > cacheEntry.UtcExpires)
        cacheEntry.UtcExpires = utcNow;
      this._buckets[this.UtcCalcExpiresBucket(cacheEntry.UtcExpires)].AddCacheEntry(cacheEntry);
    }

    internal void Remove(CacheEntry cacheEntry)
    {
      byte expiresBucket = cacheEntry.ExpiresBucket;
      if ((int) expiresBucket == (int) byte.MaxValue)
        return;
      this._buckets[(int) expiresBucket].RemoveCacheEntry(cacheEntry);
    }

    internal void UtcUpdate(CacheEntry cacheEntry, DateTime utcNewExpires)
    {
      int index1 = (int) cacheEntry.ExpiresBucket;
      int index2 = this.UtcCalcExpiresBucket(utcNewExpires);
      if (index1 != index2)
      {
        if (index1 == (int) byte.MaxValue)
          return;
        this._buckets[index1].RemoveCacheEntry(cacheEntry);
        cacheEntry.UtcExpires = utcNewExpires;
        this._buckets[index2].AddCacheEntry(cacheEntry);
      }
      else
      {
        if (index1 == (int) byte.MaxValue)
          return;
        this._buckets[index1].UtcUpdateCacheEntry(cacheEntry, utcNewExpires);
      }
    }
  }
}
