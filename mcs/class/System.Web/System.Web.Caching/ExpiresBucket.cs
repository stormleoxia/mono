using System;
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
		private static readonly TimeSpan COUNT_INTERVAL = new TimeSpan(CacheExpires._tsPerBucket.Ticks / 4L);
		private readonly CacheExpires _cacheExpires;
		private readonly byte _bucket;
		private ExpiresPage[] _pages;
		private int _cEntriesInUse;
		private int _cPagesInUse;
		private int _cEntriesInFlush;
		private int _minEntriesInUse;
		private ExpiresPageList _freePageList;
		private ExpiresPageList _freeEntryList;
		private bool _blockReduce;
		private DateTime _utcMinExpires;
		private int[] _counts;
		private DateTime _utcLastCountReset;
		internal ExpiresBucket(CacheExpires cacheExpires, byte bucket, DateTime utcNow)
		{
			this._cacheExpires = cacheExpires;
			this._bucket = bucket;
			this._counts = new int[4];
			this.ResetCounts(utcNow);
			this.InitZeroPages();
		}
		private void InitZeroPages()
		{
			this._pages = null;
			this._minEntriesInUse = -1;
			this._freePageList._head = -1;
			this._freePageList._tail = -1;
			this._freeEntryList._head = -1;
			this._freeEntryList._tail = -1;
		}
		private void ResetCounts(DateTime utcNow)
		{
			this._utcLastCountReset = utcNow;
			this._utcMinExpires = DateTime.MaxValue;
			for (int i = 0; i < this._counts.Length; i++)
			{
				this._counts[i] = 0;
			}
		}
		private int GetCountIndex(DateTime utcExpires)
		{
			return Math.Max(0, (int)((utcExpires - this._utcLastCountReset).Ticks / ExpiresBucket.COUNT_INTERVAL.Ticks));
		}
		private void AddCount(DateTime utcExpires)
		{
			int countIndex = this.GetCountIndex(utcExpires);
			for (int i = this._counts.Length - 1; i >= countIndex; i--)
			{
				this._counts[i]++;
			}
			if (utcExpires < this._utcMinExpires)
			{
				this._utcMinExpires = utcExpires;
			}
		}
		private void RemoveCount(DateTime utcExpires)
		{
			int countIndex = this.GetCountIndex(utcExpires);
			for (int i = this._counts.Length - 1; i >= countIndex; i--)
			{
				this._counts[i]--;
			}
		}
		private int GetExpiresCount(DateTime utcExpires)
		{
			if (utcExpires < this._utcMinExpires)
			{
				return 0;
			}
			int countIndex = this.GetCountIndex(utcExpires);
			if (countIndex >= this._counts.Length)
			{
				return this._cEntriesInUse;
			}
			return this._counts[countIndex];
		}
		private void AddToListHead(int pageIndex, ref ExpiresPageList list)
		{
			this._pages[pageIndex]._pagePrev = -1;
			this._pages[pageIndex]._pageNext = list._head;
			if (list._head != -1)
			{
				this._pages[list._head]._pagePrev = pageIndex;
			}
			else
			{
				list._tail = pageIndex;
			}
			list._head = pageIndex;
		}
		private void AddToListTail(int pageIndex, ref ExpiresPageList list)
		{
			this._pages[pageIndex]._pageNext = -1;
			this._pages[pageIndex]._pagePrev = list._tail;
			if (list._tail != -1)
			{
				this._pages[list._tail]._pageNext = pageIndex;
			}
			else
			{
				list._head = pageIndex;
			}
			list._tail = pageIndex;
		}
		private int RemoveFromListHead(ref ExpiresPageList list)
		{
			int head = list._head;
			this.RemoveFromList(head, ref list);
			return head;
		}
		private void RemoveFromList(int pageIndex, ref ExpiresPageList list)
		{
			if (this._pages[pageIndex]._pagePrev != -1)
			{
				this._pages[this._pages[pageIndex]._pagePrev]._pageNext = this._pages[pageIndex]._pageNext;
			}
			else
			{
				list._head = this._pages[pageIndex]._pageNext;
			}
			if (this._pages[pageIndex]._pageNext != -1)
			{
				this._pages[this._pages[pageIndex]._pageNext]._pagePrev = this._pages[pageIndex]._pagePrev;
			}
			else
			{
				list._tail = this._pages[pageIndex]._pagePrev;
			}
			this._pages[pageIndex]._pagePrev = -1;
			this._pages[pageIndex]._pageNext = -1;
		}
		private void MoveToListHead(int pageIndex, ref ExpiresPageList list)
		{
			if (list._head == pageIndex)
			{
				return;
			}
			this.RemoveFromList(pageIndex, ref list);
			this.AddToListHead(pageIndex, ref list);
		}
		private void MoveToListTail(int pageIndex, ref ExpiresPageList list)
		{
			if (list._tail == pageIndex)
			{
				return;
			}
			this.RemoveFromList(pageIndex, ref list);
			this.AddToListTail(pageIndex, ref list);
		}
		private void UpdateMinEntries()
		{
			if (this._cPagesInUse <= 1)
			{
				this._minEntriesInUse = -1;
				return;
			}
			int num = this._cPagesInUse * 127;
			this._minEntriesInUse = (int)((double)num * 0.5);
			if (this._minEntriesInUse - 1 > (this._cPagesInUse - 1) * 127)
			{
				this._minEntriesInUse = -1;
			}
		}
		private void RemovePage(int pageIndex)
		{
			this.RemoveFromList(pageIndex, ref this._freeEntryList);
			this.AddToListHead(pageIndex, ref this._freePageList);
			this._pages[pageIndex]._entries = null;
			this._cPagesInUse--;
			if (this._cPagesInUse == 0)
			{
				this.InitZeroPages();
				return;
			}
			this.UpdateMinEntries();
		}
		private ExpiresEntryRef GetFreeExpiresEntry()
		{
			int head = this._freeEntryList._head;
			ExpiresEntry[] entries = this._pages[head]._entries;
			int index = entries[0]._next.Index;
			entries[0]._next = entries[index]._next;
			ExpiresEntry[] expr_54_cp_0_cp_0 = entries;
			int expr_54_cp_0_cp_1 = 0;
			expr_54_cp_0_cp_0[expr_54_cp_0_cp_1]._cFree = expr_54_cp_0_cp_0[expr_54_cp_0_cp_1]._cFree - 1;
			if (entries[0]._cFree == 0)
			{
				this.RemoveFromList(head, ref this._freeEntryList);
			}
			return new ExpiresEntryRef(head, index);
		}
		private void AddExpiresEntryToFreeList(ExpiresEntryRef entryRef)
		{
			ExpiresEntry[] entries = this._pages[entryRef.PageIndex]._entries;
			int index = entryRef.Index;
			entries[index]._cFree = 0;
			entries[index]._next = entries[0]._next;
			entries[0]._next = entryRef;
			this._cEntriesInUse--;
			int pageIndex = entryRef.PageIndex;
			ExpiresEntry[] expr_74_cp_0_cp_0 = entries;
			int expr_74_cp_0_cp_1 = 0;
			expr_74_cp_0_cp_0[expr_74_cp_0_cp_1]._cFree = expr_74_cp_0_cp_0[expr_74_cp_0_cp_1]._cFree + 1;
			if (entries[0]._cFree == 1)
			{
				this.AddToListHead(pageIndex, ref this._freeEntryList);
				return;
			}
			if (entries[0]._cFree == 127)
			{
				this.RemovePage(pageIndex);
			}
		}
		private void Expand()
		{
			if (this._freePageList._head == -1)
			{
				int num;
				if (this._pages == null)
				{
					num = 0;
				}
				else
				{
					num = this._pages.Length;
				}
				int num2 = num * 2;
				num2 = Math.Max(num + 10, num2);
				num2 = Math.Min(num2, num + 340);
				ExpiresPage[] array = new ExpiresPage[num2];
				for (int i = 0; i < num; i++)
				{
					array[i] = this._pages[i];
				}
				for (int j = num; j < array.Length; j++)
				{
					array[j]._pagePrev = j - 1;
					array[j]._pageNext = j + 1;
				}
				array[num]._pagePrev = -1;
				ExpiresPage[] expr_B8 = array;
				expr_B8[expr_B8.Length - 1]._pageNext = -1;
				this._freePageList._head = num;
				this._freePageList._tail = array.Length - 1;
				this._pages = array;
			}
			int num3 = this.RemoveFromListHead(ref this._freePageList);
			this.AddToListHead(num3, ref this._freeEntryList);
			ExpiresEntry[] array2 = new ExpiresEntry[128];
			array2[0]._cFree = 127;
			for (int k = 0; k < array2.Length - 1; k++)
			{
				array2[k]._next = new ExpiresEntryRef(num3, k + 1);
			}
			ExpiresEntry[] expr_14C = array2;
			expr_14C[expr_14C.Length - 1]._next = ExpiresEntryRef.INVALID;
			this._pages[num3]._entries = array2;
			this._cPagesInUse++;
			this.UpdateMinEntries();
		}
		private void Reduce()
		{
			if (this._cEntriesInUse >= this._minEntriesInUse || this._blockReduce)
			{
				return;
			}
			int num = 63;
			int tail = this._freeEntryList._tail;
			int num2 = this._freeEntryList._head;
			while (true)
			{
				int pageNext = this._pages[num2]._pageNext;
				if (this._pages[num2]._entries[0]._cFree > num)
				{
					this.MoveToListTail(num2, ref this._freeEntryList);
				}
				else
				{
					this.MoveToListHead(num2, ref this._freeEntryList);
				}
				if (num2 == tail)
				{
					break;
				}
				num2 = pageNext;
			}
			while (this._freeEntryList._tail != -1)
			{
				ExpiresEntry[] entries = this._pages[this._freeEntryList._tail]._entries;
				if (this._cPagesInUse * 127 - entries[0]._cFree - this._cEntriesInUse < 127 - entries[0]._cFree)
				{
					break;
				}
				for (int i = 1; i < entries.Length; i++)
				{
					if (entries[i]._cacheEntry != null)
					{
						ExpiresEntryRef freeExpiresEntry = this.GetFreeExpiresEntry();
						entries[i]._cacheEntry.ExpiresEntryRef = freeExpiresEntry;
						this._pages[freeExpiresEntry.PageIndex]._entries[freeExpiresEntry.Index] = entries[i];
						ExpiresEntry[] expr_153_cp_0_cp_0 = entries;
						int expr_153_cp_0_cp_1 = 0;
						expr_153_cp_0_cp_0[expr_153_cp_0_cp_1]._cFree = expr_153_cp_0_cp_0[expr_153_cp_0_cp_1]._cFree + 1;
					}
				}
				this.RemovePage(this._freeEntryList._tail);
			}
		}
		internal void AddCacheEntry(CacheEntry cacheEntry)
		{
			bool flag = false;
			try
			{
				Monitor.Enter(this, ref flag);
				if ((cacheEntry.State & (CacheEntry.EntryState)3) != CacheEntry.EntryState.NotInCache)
				{
					ExpiresEntryRef expiresEntryRef = cacheEntry.ExpiresEntryRef;
					if (cacheEntry.ExpiresBucket == 255 && expiresEntryRef.IsInvalid)
					{
						if (this._freeEntryList._head == -1)
						{
							this.Expand();
						}
						ExpiresEntryRef freeExpiresEntry = this.GetFreeExpiresEntry();
						cacheEntry.ExpiresBucket = this._bucket;
						cacheEntry.ExpiresEntryRef = freeExpiresEntry;
						ExpiresEntry[] arg_8B_0 = this._pages[freeExpiresEntry.PageIndex]._entries;
						int index = freeExpiresEntry.Index;
						arg_8B_0[index]._cacheEntry = cacheEntry;
						arg_8B_0[index]._utcExpires = cacheEntry.UtcExpires;
						this.AddCount(cacheEntry.UtcExpires);
						this._cEntriesInUse++;
						if ((cacheEntry.State & (CacheEntry.EntryState)3) == CacheEntry.EntryState.NotInCache)
						{
							this.RemoveCacheEntryNoLock(cacheEntry);
						}
					}
				}
			}
			finally
			{
				if (flag)
				{
					Monitor.Exit(this);
				}
			}
		}
		private void RemoveCacheEntryNoLock(CacheEntry cacheEntry)
		{
			ExpiresEntryRef expiresEntryRef = cacheEntry.ExpiresEntryRef;
			if (cacheEntry.ExpiresBucket != this._bucket || expiresEntryRef.IsInvalid)
			{
				return;
			}
			ExpiresEntry[] entries = this._pages[expiresEntryRef.PageIndex]._entries;
			int index = expiresEntryRef.Index;
			this.RemoveCount(entries[index]._utcExpires);
			cacheEntry.ExpiresBucket = 255;
			cacheEntry.ExpiresEntryRef = ExpiresEntryRef.INVALID;
			entries[index]._cacheEntry = null;
			this.AddExpiresEntryToFreeList(expiresEntryRef);
			if (this._cEntriesInUse == 0)
			{
				this.ResetCounts(DateTime.UtcNow);
			}
			this.Reduce();
		}
		internal void RemoveCacheEntry(CacheEntry cacheEntry)
		{
			bool flag = false;
			try
			{
				Monitor.Enter(this, ref flag);
				this.RemoveCacheEntryNoLock(cacheEntry);
			}
			finally
			{
				if (flag)
				{
					Monitor.Exit(this);
				}
			}
		}
		internal void UtcUpdateCacheEntry(CacheEntry cacheEntry, DateTime utcExpires)
		{
			bool flag = false;
			try
			{
				Monitor.Enter(this, ref flag);
				ExpiresEntryRef expiresEntryRef = cacheEntry.ExpiresEntryRef;
				if (cacheEntry.ExpiresBucket == this._bucket && !expiresEntryRef.IsInvalid)
				{
					ExpiresEntry[] entries = this._pages[expiresEntryRef.PageIndex]._entries;
					int index = expiresEntryRef.Index;
					this.RemoveCount(entries[index]._utcExpires);
					this.AddCount(utcExpires);
					entries[index]._utcExpires = utcExpires;
					cacheEntry.UtcExpires = utcExpires;
				}
			}
			finally
			{
				if (flag)
				{
					Monitor.Exit(this);
				}
			}
		}
		internal int FlushExpiredItems(DateTime utcNow, bool useInsertBlock)
		{
			if (this._cEntriesInUse == 0 || this.GetExpiresCount(utcNow) == 0)
			{
				return 0;
			}
			ExpiresEntryRef expiresEntryRef = ExpiresEntryRef.INVALID;
			int num = 0;
			ExpiresBucket obj = this;
			try
			{
				if (useInsertBlock)
				{
					this._cacheExpires.CacheSingle.BlockInsertIfNeeded();
				}
				bool flag = false;
				try
				{
					obj = this;
					Monitor.Enter(obj, ref flag);
					if (this._cEntriesInUse == 0 || this.GetExpiresCount(utcNow) == 0)
					{
						int result = 0;
						return result;
					}
					this.ResetCounts(utcNow);
					int num2 = this._cPagesInUse;
					for (int i = 0; i < this._pages.Length; i++)
					{
						ExpiresEntry[] entries = this._pages[i]._entries;
						if (entries != null)
						{
							int num3 = 127 - entries[0]._cFree;
							for (int j = 1; j < entries.Length; j++)
							{
								CacheEntry cacheEntry = entries[j]._cacheEntry;
								if (cacheEntry != null)
								{
									if (entries[j]._utcExpires > utcNow)
									{
										this.AddCount(entries[j]._utcExpires);
									}
									else
									{
										cacheEntry.ExpiresBucket = 255;
										cacheEntry.ExpiresEntryRef = ExpiresEntryRef.INVALID;
										entries[j]._cFree = 1;
										entries[j]._next = expiresEntryRef;
										expiresEntryRef = new ExpiresEntryRef(i, j);
										num++;
										this._cEntriesInFlush++;
									}
									num3--;
									if (num3 == 0)
									{
										break;
									}
								}
							}
							num2--;
							if (num2 == 0)
							{
								break;
							}
						}
					}
					if (num == 0)
					{
						int result = 0;
						return result;
					}
					this._blockReduce = true;
				}
				finally
				{
					if (flag)
					{
						Monitor.Exit(obj);
					}
				}
			}
			finally
			{
				if (useInsertBlock)
				{
					this._cacheExpires.CacheSingle.UnblockInsert();
				}
			}
			CacheSingle cacheSingle = this._cacheExpires.CacheSingle;
			ExpiresEntryRef entryRef = expiresEntryRef;
			while (!entryRef.IsInvalid)
			{
				ExpiresEntry[] entries = this._pages[entryRef.PageIndex]._entries;
				int index = entryRef.Index;
				ExpiresEntryRef arg_1FE_0 = entries[index]._next;
				CacheEntry cacheEntry = entries[index]._cacheEntry;
				entries[index]._cacheEntry = null;
				cacheSingle.Remove(cacheEntry, CacheItemRemovedReason.Expired);
				entryRef = arg_1FE_0;
			}
			try
			{
				if (useInsertBlock)
				{
					this._cacheExpires.CacheSingle.BlockInsertIfNeeded();
				}
				bool flag = false;
				try
				{
					obj = this;
					Monitor.Enter(obj, ref flag);
					entryRef = expiresEntryRef;
					while (!entryRef.IsInvalid)
					{
						ExpiresEntry[] entries = this._pages[entryRef.PageIndex]._entries;
						int index = entryRef.Index;
						ExpiresEntryRef arg_273_0 = entries[index]._next;
						this._cEntriesInFlush--;
						this.AddExpiresEntryToFreeList(entryRef);
						entryRef = arg_273_0;
					}
					this._blockReduce = false;
					this.Reduce();
				}
				finally
				{
					if (flag)
					{
						Monitor.Exit(obj);
					}
				}
			}
			finally
			{
				if (useInsertBlock)
				{
					this._cacheExpires.CacheSingle.UnblockInsert();
				}
			}
			return num;
		}
	}
}
