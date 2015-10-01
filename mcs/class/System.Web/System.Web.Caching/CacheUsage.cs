using System;
using System.Threading;
namespace System.Web.Caching
{
	internal class CacheUsage
	{
		internal static readonly TimeSpan NEWADD_INTERVAL = new TimeSpan(0, 0, 10);
		internal static readonly TimeSpan CORRELATED_REQUEST_TIMEOUT = new TimeSpan(0, 0, 1);
		internal static readonly TimeSpan MIN_LIFETIME_FOR_USAGE = CacheUsage.NEWADD_INTERVAL;
		private const byte NUMBUCKETS = 5;
		private const int MAX_REMOVE = 1024;
		private readonly CacheSingle _cacheSingle;
		internal readonly UsageBucket[] _buckets;
		private int _inFlush;
		internal CacheSingle CacheSingle
		{
			get
			{
				return this._cacheSingle;
			}
		}
		internal CacheUsage(CacheSingle cacheSingle)
		{
			this._cacheSingle = cacheSingle;
			this._buckets = new UsageBucket[5];
			byte b = 0;
			while ((int)b < this._buckets.Length)
			{
				this._buckets[(int)b] = new UsageBucket(this, b);
				b += 1;
			}
		}
		internal void Add(CacheEntry cacheEntry)
		{
			byte usageBucket = cacheEntry.UsageBucket;
			this._buckets[(int)usageBucket].AddCacheEntry(cacheEntry);
		}
		internal void Remove(CacheEntry cacheEntry)
		{
			byte usageBucket = cacheEntry.UsageBucket;
			if (usageBucket != 255)
			{
				this._buckets[(int)usageBucket].RemoveCacheEntry(cacheEntry);
			}
		}
		internal void Update(CacheEntry cacheEntry)
		{
			byte usageBucket = cacheEntry.UsageBucket;
			if (usageBucket != 255)
			{
				this._buckets[(int)usageBucket].UpdateCacheEntry(cacheEntry);
			}
		}
		internal int FlushUnderUsedItems(int toFlush, ref int publicEntriesFlushed, ref int ocEntriesFlushed)
		{
			int num = 0;
			if (Interlocked.Exchange(ref this._inFlush, 1) == 0)
			{
				try
				{
					UsageBucket[] buckets = this._buckets;
					for (int i = 0; i < buckets.Length; i++)
					{
						int num2 = buckets[i].FlushUnderUsedItems(toFlush - num, false, ref publicEntriesFlushed, ref ocEntriesFlushed);
						num += num2;
						if (num >= toFlush)
						{
							break;
						}
					}
					if (num < toFlush)
					{
						buckets = this._buckets;
						for (int i = 0; i < buckets.Length; i++)
						{
							int num3 = buckets[i].FlushUnderUsedItems(toFlush - num, true, ref publicEntriesFlushed, ref ocEntriesFlushed);
							num += num3;
							if (num >= toFlush)
							{
								break;
							}
						}
					}
				}
				finally
				{
					Interlocked.Exchange(ref this._inFlush, 0);
				}
			}
			return num;
		}
	}
}
