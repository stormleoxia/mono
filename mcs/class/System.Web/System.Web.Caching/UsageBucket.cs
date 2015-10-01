using System;
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
		private CacheUsage _cacheUsage;
		private byte _bucket;
		private UsagePage[] _pages;
		private int _cEntriesInUse;
		private int _cPagesInUse;
		private int _cEntriesInFlush;
		private int _minEntriesInUse;
		private UsagePageList _freePageList;
		private UsagePageList _freeEntryList;
		private UsageEntryRef _lastRefHead;
		private UsageEntryRef _lastRefTail;
		private UsageEntryRef _addRef2Head;
		private bool _blockReduce;
		internal UsageBucket(CacheUsage cacheUsage, byte bucket)
		{
			this._cacheUsage = cacheUsage;
			this._bucket = bucket;
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
		private void AddToListHead(int pageIndex, ref UsagePageList list)
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
		private void AddToListTail(int pageIndex, ref UsagePageList list)
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
		private int RemoveFromListHead(ref UsagePageList list)
		{
			int head = list._head;
			this.RemoveFromList(head, ref list);
			return head;
		}
		private void RemoveFromList(int pageIndex, ref UsagePageList list)
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
		private void MoveToListHead(int pageIndex, ref UsagePageList list)
		{
			if (list._head == pageIndex)
			{
				return;
			}
			this.RemoveFromList(pageIndex, ref list);
			this.AddToListHead(pageIndex, ref list);
		}
		private void MoveToListTail(int pageIndex, ref UsagePageList list)
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
		private UsageEntryRef GetFreeUsageEntry()
		{
			int head = this._freeEntryList._head;
			UsageEntry[] entries = this._pages[head]._entries;
			int ref1Index = entries[0]._ref1._next.Ref1Index;
			entries[0]._ref1._next = entries[ref1Index]._ref1._next;
			UsageEntry[] expr_63_cp_0_cp_0 = entries;
			int expr_63_cp_0_cp_1 = 0;
			expr_63_cp_0_cp_0[expr_63_cp_0_cp_1]._cFree = expr_63_cp_0_cp_0[expr_63_cp_0_cp_1]._cFree - 1;
			if (entries[0]._cFree == 0)
			{
				this.RemoveFromList(head, ref this._freeEntryList);
			}
			return new UsageEntryRef(head, ref1Index);
		}
		private void AddUsageEntryToFreeList(UsageEntryRef entryRef)
		{
			UsageEntry[] entries = this._pages[entryRef.PageIndex]._entries;
			int ref1Index = entryRef.Ref1Index;
			entries[ref1Index]._utcDate = DateTime.MinValue;
			entries[ref1Index]._ref1._prev = UsageEntryRef.INVALID;
			entries[ref1Index]._ref2._next = UsageEntryRef.INVALID;
			entries[ref1Index]._ref2._prev = UsageEntryRef.INVALID;
			entries[ref1Index]._ref1._next = entries[0]._ref1._next;
			entries[0]._ref1._next = entryRef;
			this._cEntriesInUse--;
			int pageIndex = entryRef.PageIndex;
			UsageEntry[] expr_C9_cp_0_cp_0 = entries;
			int expr_C9_cp_0_cp_1 = 0;
			expr_C9_cp_0_cp_0[expr_C9_cp_0_cp_1]._cFree = expr_C9_cp_0_cp_0[expr_C9_cp_0_cp_1]._cFree + 1;
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
				UsagePage[] array = new UsagePage[num2];
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
				UsagePage[] expr_B8 = array;
				expr_B8[expr_B8.Length - 1]._pageNext = -1;
				this._freePageList._head = num;
				this._freePageList._tail = array.Length - 1;
				this._pages = array;
			}
			int num3 = this.RemoveFromListHead(ref this._freePageList);
			this.AddToListHead(num3, ref this._freeEntryList);
			UsageEntry[] array2 = new UsageEntry[128];
			array2[0]._cFree = 127;
			for (int k = 0; k < array2.Length - 1; k++)
			{
				array2[k]._ref1._next = new UsageEntryRef(num3, k + 1);
			}
			UsageEntry[] expr_151 = array2;
			expr_151[expr_151.Length - 1]._ref1._next = UsageEntryRef.INVALID;
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
				UsageEntry[] entries = this._pages[this._freeEntryList._tail]._entries;
				if (this._cPagesInUse * 127 - entries[0]._cFree - this._cEntriesInUse < 127 - entries[0]._cFree)
				{
					break;
				}
				for (int i = 1; i < entries.Length; i++)
				{
					if (entries[i]._cacheEntry != null)
					{
						UsageEntryRef freeUsageEntry = this.GetFreeUsageEntry();
						UsageEntryRef usageEntryRef = new UsageEntryRef(freeUsageEntry.PageIndex, -freeUsageEntry.Ref1Index);
						UsageEntryRef r = new UsageEntryRef(this._freeEntryList._tail, i);
						UsageEntryRef r2 = new UsageEntryRef(r.PageIndex, -r.Ref1Index);
						entries[i]._cacheEntry.UsageEntryRef = freeUsageEntry;
						UsageEntry[] expr_177 = this._pages[freeUsageEntry.PageIndex]._entries;
						expr_177[freeUsageEntry.Ref1Index] = entries[i];
						UsageEntry[] expr_19A_cp_0_cp_0 = entries;
						int expr_19A_cp_0_cp_1 = 0;
						expr_19A_cp_0_cp_0[expr_19A_cp_0_cp_1]._cFree = expr_19A_cp_0_cp_0[expr_19A_cp_0_cp_1]._cFree + 1;
						UsageEntryRef r3 = expr_177[freeUsageEntry.Ref1Index]._ref1._prev;
						UsageEntryRef r4 = expr_177[freeUsageEntry.Ref1Index]._ref1._next;
						if (r4 == r2)
						{
							r4 = usageEntryRef;
						}
						if (r3.IsRef1)
						{
							this._pages[r3.PageIndex]._entries[r3.Ref1Index]._ref1._next = freeUsageEntry;
						}
						else
						{
							if (r3.IsRef2)
							{
								this._pages[r3.PageIndex]._entries[r3.Ref2Index]._ref2._next = freeUsageEntry;
							}
							else
							{
								this._lastRefHead = freeUsageEntry;
							}
						}
						if (r4.IsRef1)
						{
							this._pages[r4.PageIndex]._entries[r4.Ref1Index]._ref1._prev = freeUsageEntry;
						}
						else
						{
							if (r4.IsRef2)
							{
								this._pages[r4.PageIndex]._entries[r4.Ref2Index]._ref2._prev = freeUsageEntry;
							}
							else
							{
								this._lastRefTail = freeUsageEntry;
							}
						}
						r3 = expr_177[freeUsageEntry.Ref1Index]._ref2._prev;
						if (r3 == r)
						{
							r3 = freeUsageEntry;
						}
						r4 = expr_177[freeUsageEntry.Ref1Index]._ref2._next;
						if (r3.IsRef1)
						{
							this._pages[r3.PageIndex]._entries[r3.Ref1Index]._ref1._next = usageEntryRef;
						}
						else
						{
							if (r3.IsRef2)
							{
								this._pages[r3.PageIndex]._entries[r3.Ref2Index]._ref2._next = usageEntryRef;
							}
							else
							{
								this._lastRefHead = usageEntryRef;
							}
						}
						if (r4.IsRef1)
						{
							this._pages[r4.PageIndex]._entries[r4.Ref1Index]._ref1._prev = usageEntryRef;
						}
						else
						{
							if (r4.IsRef2)
							{
								this._pages[r4.PageIndex]._entries[r4.Ref2Index]._ref2._prev = usageEntryRef;
							}
							else
							{
								this._lastRefTail = usageEntryRef;
							}
						}
						if (this._addRef2Head == r2)
						{
							this._addRef2Head = usageEntryRef;
						}
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
				if (this._freeEntryList._head == -1)
				{
					this.Expand();
				}
				UsageEntryRef freeUsageEntry = this.GetFreeUsageEntry();
				UsageEntryRef usageEntryRef = new UsageEntryRef(freeUsageEntry.PageIndex, -freeUsageEntry.Ref1Index);
				cacheEntry.UsageEntryRef = freeUsageEntry;
				UsageEntry[] entries = this._pages[freeUsageEntry.PageIndex]._entries;
				int ref1Index = freeUsageEntry.Ref1Index;
				entries[ref1Index]._cacheEntry = cacheEntry;
				entries[ref1Index]._utcDate = DateTime.UtcNow;
				entries[ref1Index]._ref1._prev = UsageEntryRef.INVALID;
				entries[ref1Index]._ref2._next = this._addRef2Head;
				if (this._lastRefHead.IsInvalid)
				{
					entries[ref1Index]._ref1._next = usageEntryRef;
					entries[ref1Index]._ref2._prev = freeUsageEntry;
					this._lastRefTail = usageEntryRef;
				}
				else
				{
					entries[ref1Index]._ref1._next = this._lastRefHead;
					if (this._lastRefHead.IsRef1)
					{
						this._pages[this._lastRefHead.PageIndex]._entries[this._lastRefHead.Ref1Index]._ref1._prev = freeUsageEntry;
					}
					else
					{
						if (this._lastRefHead.IsRef2)
						{
							this._pages[this._lastRefHead.PageIndex]._entries[this._lastRefHead.Ref2Index]._ref2._prev = freeUsageEntry;
						}
						else
						{
							this._lastRefTail = freeUsageEntry;
						}
					}
					UsageEntryRef prev;
					UsageEntryRef usageEntryRef2;
					if (this._addRef2Head.IsInvalid)
					{
						prev = this._lastRefTail;
						usageEntryRef2 = UsageEntryRef.INVALID;
					}
					else
					{
						prev = this._pages[this._addRef2Head.PageIndex]._entries[this._addRef2Head.Ref2Index]._ref2._prev;
						usageEntryRef2 = this._addRef2Head;
					}
					entries[ref1Index]._ref2._prev = prev;
					if (prev.IsRef1)
					{
						this._pages[prev.PageIndex]._entries[prev.Ref1Index]._ref1._next = usageEntryRef;
					}
					else
					{
						if (prev.IsRef2)
						{
							this._pages[prev.PageIndex]._entries[prev.Ref2Index]._ref2._next = usageEntryRef;
						}
						else
						{
							this._lastRefHead = usageEntryRef;
						}
					}
					if (usageEntryRef2.IsRef1)
					{
						this._pages[usageEntryRef2.PageIndex]._entries[usageEntryRef2.Ref1Index]._ref1._prev = usageEntryRef;
					}
					else
					{
						if (usageEntryRef2.IsRef2)
						{
							this._pages[usageEntryRef2.PageIndex]._entries[usageEntryRef2.Ref2Index]._ref2._prev = usageEntryRef;
						}
						else
						{
							this._lastRefTail = usageEntryRef;
						}
					}
				}
				this._lastRefHead = freeUsageEntry;
				this._addRef2Head = usageEntryRef;
				this._cEntriesInUse++;
			}
			finally
			{
				if (flag)
				{
					Monitor.Exit(this);
				}
			}
		}
		private void RemoveEntryFromLastRefList(UsageEntryRef entryRef)
		{
			UsageEntry[] arg_1F_0 = this._pages[entryRef.PageIndex]._entries;
			int ref1Index = entryRef.Ref1Index;
			UsageEntryRef prev = arg_1F_0[ref1Index]._ref1._prev;
			UsageEntryRef next = arg_1F_0[ref1Index]._ref1._next;
			if (prev.IsRef1)
			{
				this._pages[prev.PageIndex]._entries[prev.Ref1Index]._ref1._next = next;
			}
			else
			{
				if (prev.IsRef2)
				{
					this._pages[prev.PageIndex]._entries[prev.Ref2Index]._ref2._next = next;
				}
				else
				{
					this._lastRefHead = next;
				}
			}
			if (next.IsRef1)
			{
				this._pages[next.PageIndex]._entries[next.Ref1Index]._ref1._prev = prev;
			}
			else
			{
				if (next.IsRef2)
				{
					this._pages[next.PageIndex]._entries[next.Ref2Index]._ref2._prev = prev;
				}
				else
				{
					this._lastRefTail = prev;
				}
			}
			prev = arg_1F_0[ref1Index]._ref2._prev;
			next = arg_1F_0[ref1Index]._ref2._next;
			UsageEntryRef r = new UsageEntryRef(entryRef.PageIndex, -entryRef.Ref1Index);
			if (prev.IsRef1)
			{
				this._pages[prev.PageIndex]._entries[prev.Ref1Index]._ref1._next = next;
			}
			else
			{
				if (prev.IsRef2)
				{
					this._pages[prev.PageIndex]._entries[prev.Ref2Index]._ref2._next = next;
				}
				else
				{
					this._lastRefHead = next;
				}
			}
			if (next.IsRef1)
			{
				this._pages[next.PageIndex]._entries[next.Ref1Index]._ref1._prev = prev;
			}
			else
			{
				if (next.IsRef2)
				{
					this._pages[next.PageIndex]._entries[next.Ref2Index]._ref2._prev = prev;
				}
				else
				{
					this._lastRefTail = prev;
				}
			}
			if (this._addRef2Head == r)
			{
				this._addRef2Head = next;
			}
		}
		internal void RemoveCacheEntry(CacheEntry cacheEntry)
		{
			bool flag = false;
			try
			{
				Monitor.Enter(this, ref flag);
				UsageEntryRef usageEntryRef = cacheEntry.UsageEntryRef;
				if (!usageEntryRef.IsInvalid)
				{
					UsageEntry[] arg_49_0 = this._pages[usageEntryRef.PageIndex]._entries;
					int ref1Index = usageEntryRef.Ref1Index;
					cacheEntry.UsageEntryRef = UsageEntryRef.INVALID;
					arg_49_0[ref1Index]._cacheEntry = null;
					this.RemoveEntryFromLastRefList(usageEntryRef);
					this.AddUsageEntryToFreeList(usageEntryRef);
					this.Reduce();
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
		internal void UpdateCacheEntry(CacheEntry cacheEntry)
		{
			bool flag = false;
			try
			{
				Monitor.Enter(this, ref flag);
				UsageEntryRef usageEntryRef = cacheEntry.UsageEntryRef;
				if (!usageEntryRef.IsInvalid)
				{
					UsageEntry[] entries = this._pages[usageEntryRef.PageIndex]._entries;
					int ref1Index = usageEntryRef.Ref1Index;
					UsageEntryRef usageEntryRef2 = new UsageEntryRef(usageEntryRef.PageIndex, -usageEntryRef.Ref1Index);
					UsageEntryRef prev = entries[ref1Index]._ref2._prev;
					UsageEntryRef next = entries[ref1Index]._ref2._next;
					if (prev.IsRef1)
					{
						this._pages[prev.PageIndex]._entries[prev.Ref1Index]._ref1._next = next;
					}
					else
					{
						if (prev.IsRef2)
						{
							this._pages[prev.PageIndex]._entries[prev.Ref2Index]._ref2._next = next;
						}
						else
						{
							this._lastRefHead = next;
						}
					}
					if (next.IsRef1)
					{
						this._pages[next.PageIndex]._entries[next.Ref1Index]._ref1._prev = prev;
					}
					else
					{
						if (next.IsRef2)
						{
							this._pages[next.PageIndex]._entries[next.Ref2Index]._ref2._prev = prev;
						}
						else
						{
							this._lastRefTail = prev;
						}
					}
					if (this._addRef2Head == usageEntryRef2)
					{
						this._addRef2Head = next;
					}
					entries[ref1Index]._ref2 = entries[ref1Index]._ref1;
					prev = entries[ref1Index]._ref2._prev;
					next = entries[ref1Index]._ref2._next;
					if (prev.IsRef1)
					{
						this._pages[prev.PageIndex]._entries[prev.Ref1Index]._ref1._next = usageEntryRef2;
					}
					else
					{
						if (prev.IsRef2)
						{
							this._pages[prev.PageIndex]._entries[prev.Ref2Index]._ref2._next = usageEntryRef2;
						}
						else
						{
							this._lastRefHead = usageEntryRef2;
						}
					}
					if (next.IsRef1)
					{
						this._pages[next.PageIndex]._entries[next.Ref1Index]._ref1._prev = usageEntryRef2;
					}
					else
					{
						if (next.IsRef2)
						{
							this._pages[next.PageIndex]._entries[next.Ref2Index]._ref2._prev = usageEntryRef2;
						}
						else
						{
							this._lastRefTail = usageEntryRef2;
						}
					}
					entries[ref1Index]._ref1._prev = UsageEntryRef.INVALID;
					entries[ref1Index]._ref1._next = this._lastRefHead;
					if (this._lastRefHead.IsRef1)
					{
						this._pages[this._lastRefHead.PageIndex]._entries[this._lastRefHead.Ref1Index]._ref1._prev = usageEntryRef;
					}
					else
					{
						if (this._lastRefHead.IsRef2)
						{
							this._pages[this._lastRefHead.PageIndex]._entries[this._lastRefHead.Ref2Index]._ref2._prev = usageEntryRef;
						}
						else
						{
							this._lastRefTail = usageEntryRef;
						}
					}
					this._lastRefHead = usageEntryRef;
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
		internal int FlushUnderUsedItems(int maxFlush, bool force, ref int publicEntriesFlushed, ref int ocEntriesFlushed)
		{
			if (this._cEntriesInUse == 0)
			{
				return 0;
			}
			UsageEntryRef usageEntryRef = UsageEntryRef.INVALID;
			int num = 0;
			UsageBucket obj = this;
			try
			{
				this._cacheUsage.CacheSingle.BlockInsertIfNeeded();
				bool flag = false;
				try
				{
					obj = this;
					Monitor.Enter(obj, ref flag);
					if (this._cEntriesInUse == 0)
					{
						int result = 0;
						return result;
					}
					DateTime utcNow = DateTime.UtcNow;
					UsageEntryRef usageEntryRef2 = this._lastRefTail;
					while (this._cEntriesInFlush < maxFlush && !usageEntryRef2.IsInvalid)
					{
						UsageEntryRef prev = this._pages[usageEntryRef2.PageIndex]._entries[usageEntryRef2.Ref2Index]._ref2._prev;
						while (prev.IsRef1)
						{
							prev = this._pages[prev.PageIndex]._entries[prev.Ref1Index]._ref1._prev;
						}
						UsageEntry[] entries = this._pages[usageEntryRef2.PageIndex]._entries;
						int num2 = usageEntryRef2.Ref2Index;
						if (force)
						{
							goto IL_111;
						}
						DateTime utcDate = entries[num2]._utcDate;
						if (!(utcNow - utcDate <= CacheUsage.NEWADD_INTERVAL) || !(utcNow >= utcDate))
						{
							goto IL_111;
						}
						IL_196:
						usageEntryRef2 = prev;
						continue;
						IL_111:
						UsageEntryRef usageEntryRef3 = new UsageEntryRef(usageEntryRef2.PageIndex, usageEntryRef2.Ref2Index);
						CacheEntry cacheEntry = entries[num2]._cacheEntry;
						cacheEntry.UsageEntryRef = UsageEntryRef.INVALID;
						if (cacheEntry.IsPublic)
						{
							publicEntriesFlushed++;
						}
						else
						{
							if (cacheEntry.IsOutputCache)
							{
								ocEntriesFlushed++;
							}
						}
						this.RemoveEntryFromLastRefList(usageEntryRef3);
						entries[num2]._ref1._next = usageEntryRef;
						usageEntryRef = usageEntryRef3;
						num++;
						this._cEntriesInFlush++;
						goto IL_196;
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
				this._cacheUsage.CacheSingle.UnblockInsert();
			}
			CacheSingle cacheSingle = this._cacheUsage.CacheSingle;
			UsageEntryRef entryRef = usageEntryRef;
			while (!entryRef.IsInvalid)
			{
				UsageEntry[] entries = this._pages[entryRef.PageIndex]._entries;
				int num2 = entryRef.Ref1Index;
				UsageEntryRef next = entries[num2]._ref1._next;
				CacheEntry cacheEntry = entries[num2]._cacheEntry;
				entries[num2]._cacheEntry = null;
				cacheSingle.Remove(cacheEntry, CacheItemRemovedReason.Underused);
				entryRef = next;
			}
			try
			{
				this._cacheUsage.CacheSingle.BlockInsertIfNeeded();
				bool flag = false;
				try
				{
					obj = this;
					Monitor.Enter(obj, ref flag);
					entryRef = usageEntryRef;
					while (!entryRef.IsInvalid)
					{
						UsageEntry[] entries = this._pages[entryRef.PageIndex]._entries;
						int num2 = entryRef.Ref1Index;
						UsageEntryRef next = entries[num2]._ref1._next;
						this._cEntriesInFlush--;
						this.AddUsageEntryToFreeList(entryRef);
						entryRef = next;
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
				this._cacheUsage.CacheSingle.UnblockInsert();
			}
			return num;
		}
	}
}
