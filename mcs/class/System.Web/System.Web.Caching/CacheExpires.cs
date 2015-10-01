using System;
using System.Threading;
namespace System.Web.Caching
{
	internal sealed class CacheExpires
	{
		internal static readonly TimeSpan MIN_UPDATE_DELTA = new TimeSpan(0, 0, 1);
		internal static readonly TimeSpan MIN_FLUSH_INTERVAL = new TimeSpan(0, 0, 1);
		internal static readonly TimeSpan _tsPerBucket = new TimeSpan(0, 0, 20);
		private const int NUMBUCKETS = 30;
		private static readonly TimeSpan _tsPerCycle = new TimeSpan(30L * CacheExpires._tsPerBucket.Ticks);
		private readonly CacheSingle _cacheSingle;
		private readonly ExpiresBucket[] _buckets;
		private Timer _timer;
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
			byte b = 0;
			while ((int)b < this._buckets.Length)
			{
				this._buckets[(int)b] = new ExpiresBucket(this, b, utcNow);
				b += 1;
			}
		}
		private int UtcCalcExpiresBucket(DateTime utcDate)
		{
			return (int)((utcDate.Ticks % CacheExpires._tsPerCycle.Ticks / CacheExpires._tsPerBucket.Ticks + 1L) % 30L);
		}
		private int FlushExpiredItems(bool checkDelta, bool useInsertBlock)
		{
			int num = 0;
			if (Interlocked.Exchange(ref this._inFlush, 1) == 0)
			{
				try
				{
					if (this._timer == null)
					{
						return 0;
					}
					DateTime utcNow = DateTime.UtcNow;
					if (!checkDelta || utcNow - this._utcLastFlush >= CacheExpires.MIN_FLUSH_INTERVAL || utcNow < this._utcLastFlush)
					{
						this._utcLastFlush = utcNow;
						ExpiresBucket[] buckets = this._buckets;
						for (int i = 0; i < buckets.Length; i++)
						{
							ExpiresBucket expiresBucket = buckets[i];
							num += expiresBucket.FlushExpiredItems(utcNow, useInsertBlock);
						}
					}
				}
				finally
				{
					Interlocked.Exchange(ref this._inFlush, 0);
				}
				return num;
			}
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
				if (this._timer == null)
				{
					DateTime utcNow = DateTime.UtcNow;
					TimeSpan timeSpan = CacheExpires._tsPerBucket - new TimeSpan(utcNow.Ticks % CacheExpires._tsPerBucket.Ticks);
					this._timer = new Timer(new TimerCallback(this.TimerCallback), null, timeSpan.Ticks / 10000L, CacheExpires._tsPerBucket.Ticks / 10000L);
					return;
				}
			}
			else
			{
				Timer timer = this._timer;
				if (timer != null && Interlocked.CompareExchange<Timer>(ref this._timer, null, timer) == timer)
				{
					timer.Dispose();
					while (this._inFlush != 0)
					{
						Thread.Sleep(100);
					}
				}
			}
		}
		internal void Add(CacheEntry cacheEntry)
		{
			DateTime utcNow = DateTime.UtcNow;
			if (utcNow > cacheEntry.UtcExpires)
			{
				cacheEntry.UtcExpires = utcNow;
			}
			int num = this.UtcCalcExpiresBucket(cacheEntry.UtcExpires);
			this._buckets[num].AddCacheEntry(cacheEntry);
		}
		internal void Remove(CacheEntry cacheEntry)
		{
			byte expiresBucket = cacheEntry.ExpiresBucket;
			if (expiresBucket != 255)
			{
				this._buckets[(int)expiresBucket].RemoveCacheEntry(cacheEntry);
			}
		}
		internal void UtcUpdate(CacheEntry cacheEntry, DateTime utcNewExpires)
		{
			int expiresBucket = (int)cacheEntry.ExpiresBucket;
			int num = this.UtcCalcExpiresBucket(utcNewExpires);
			if (expiresBucket != num)
			{
				if (expiresBucket != 255)
				{
					this._buckets[expiresBucket].RemoveCacheEntry(cacheEntry);
					cacheEntry.UtcExpires = utcNewExpires;
					this._buckets[num].AddCacheEntry(cacheEntry);
					return;
				}
			}
			else
			{
				if (expiresBucket != 255)
				{
					this._buckets[expiresBucket].UtcUpdateCacheEntry(cacheEntry, utcNewExpires);
				}
			}
		}
	}
}
