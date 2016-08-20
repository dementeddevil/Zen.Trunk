using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zen.Trunk.IndexTest
{
	public class IndexEntry
	{
		public int Key
		{
			get;
			set;
		}
	}

	public class InnerIndexEntry : IndexEntry
	{
		public IndexPage Page
		{
			get;
			set;
		}
	}

	public class IndexPage
	{
		private IndexPage _parentPage;
		private IndexPage _leftPage;
		private IndexPage _rightPage;
		private int _depth;
		private List<IndexEntry> _entries;

		public virtual int MaxEntries
		{
			get
			{
				return 20;
			}
		}

		public List<IndexEntry> Entries
		{
			get
			{
				if (_entries == null)
				{
					_entries = new List<IndexEntry>();
				}
				return _entries;
			}
		}

		public IndexEntry FirstEntry
		{
			get
			{
				return Entries.FirstOrDefault();
			}
		}

		public IndexPage ParentPage
		{
			get;
			private set;
		}

		public IndexPage LeftPage
		{
			get;
			private set;
		}

		public IndexPage RightPage
		{
			get;
			private set;
		}

		public void Split()
		{
			IndexPage rhs = new IndexPage();

			int count = _entries.Count;
			int lhsCount = count / 2;
			List<IndexEntry> lhsEntries = new List<IndexEntry> (_entries.Take(count / 2));
			List<IndexEntry> rhsEntries = new List<IndexEntry> (_entries.Skip(lhsCount));

			Entries.Clear();
			Entries.AddRange(lhsEntries);
			rhs.Entries.AddRange(rhsEntries);

			// Insert split page into sibling linked list
			rhs.RightPage = RightPage;
			RightPage.LeftPage = rhs;
			RightPage = rhs;
			rhs.LeftPage = this;

			// Assume split page will be in same parent
			ParentPage.AddChildPage(rhs);
		}

		public virtual void AddChildPage(IndexPage childPage)
		{
			if (childPage.FirstEntry == null)
			{
				throw new InvalidOperationException("Child page must have at least one entry");
			}

			// Walk the list of entries and find where to insert
			int key = childPage.FirstEntry.Key;
			for (int index = 0; index < Entries.Count; ++index)
			{
				if (Entries[index].Key < key)
				{
					continue;
				}
			}
		}
	}


	public class InnerIndexPage:IndexPage
	{

	}

	public class LeafIndexPage<TKey> : IndexPage<TKey>
	{
	}
}
