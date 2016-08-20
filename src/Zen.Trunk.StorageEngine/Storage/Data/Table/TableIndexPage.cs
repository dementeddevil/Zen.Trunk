namespace Zen.Trunk.Storage.Data.Table
{
	using System;
	using Zen.Trunk.Storage.Data.Index;

	/// <summary>
	/// Class containing the implementation for index page splitting for
	/// database tables.
	/// </summary>
	public class TableIndexPage : IndexPage<TableIndexInfo, RootTableIndexInfo>
	{
		#region Private Fields
		private DatabaseTable _ownerTable;
		private RootTableIndexInfo _rootIndex;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="TableIndexPage"/> class.
		/// </summary>
		public TableIndexPage()
		{
		}
		#endregion

		#region Public Properties
		/*public RootTableIndexInfo RootIndex
		{
			get
			{
				return _rootIndex;
			}
			set
			{
				if (_rootIndex != value)
				{
					if (_rootIndex != null)
					{
						ObjectId = 0;
					}
					_rootIndex = value;
					if (_rootIndex != null)
					{
						ObjectId = _rootIndex.IndexId;
					}
				}
			}
		}*/

		/// <summary>
		/// Overridden. Gets the index manager.
		/// </summary>
		/// <value>The index manager.</value>
		public override IndexManager IndexManager
		{
			get
			{
				return (IndexManager)GetService(typeof(TableIndexManager));
			}
		}

		/// <summary>
		/// Gets the max index entries.
		/// </summary>
		/// <value>The max index entries.</value>
		public override ushort MaxIndexEntries
		{
			get
			{
				return (ushort)(DataSize / KeySize);
			}
		}

		/// <summary>
		/// Gets the size of the key for this page.
		/// </summary>
		/// <value>The size of the key.</value>
		public ushort KeySize
		{
			get
			{
				if (_rootIndex == null)
				{
					throw new InvalidOperationException("Attempt to get key size without assigning RootIndex object.");
				}

				if (IsLeafIndex)
				{
					if (IsNormalOverClusteredIndex)
					{
						// Index Key Size + Clustered Key Size
						return (ushort)(_rootIndex.KeySize + _ownerTable.ClusteredIndex.KeySize);
					}
					else
					{
						// Index Key Size + Logical Page Id + Page Row Id
						return (ushort)(_rootIndex.KeySize + 10);
					}
				}
				else
				{
					// Index Key Size + Logical Page Id
					return (ushort)(_rootIndex.KeySize + 8);
				}
			}
		}

		/// <summary>
		/// Gets the size of the min header.
		/// </summary>
		/// <value>The size of the min header.</value>
		public override uint MinHeaderSize
		{
			get
			{
				return base.MinHeaderSize + 0;
			}
		}

		/// <summary>
		/// Gets a value indicating whether the index this page is attached to
		/// is a normal index operating over a clustered index
		/// </summary>
		/// <value>
		/// 	<c>true</c> if [is normal over clustered index]; otherwise, <c>false</c>.
		/// </value>
		public bool IsNormalOverClusteredIndex
		{
			get
			{
				if (_ownerTable.IsHeap || (_rootIndex.IndexSubType & TableIndexSubType.Clustered) != 0)
				{
					return false;
				}
				else
				{
					return true;
				}
			}
		}
		#endregion

		#region Public Methods
		public void SetContext(DatabaseTable def, RootTableIndexInfo rootIndex)
		{
			_ownerTable = def;
			_rootIndex = rootIndex;
			for (int index = 0; index < IndexCount; ++index)
			{
				IndexEntries[index].SetContext(def, rootIndex);
			}
		}

		public int CompareIndex(int index, object[] keys)
		{
			TableIndexInfo lhs = IndexEntries[index];
			TableIndexInfo rhs = new TableIndexInfo(keys);
			return lhs.CompareTo(rhs);
		}
		#endregion

		#region Protected Methods
		/// <summary>
		/// Creates an index link to the first entry in this index page
		/// </summary>
		/// <param name="link"></param>
		protected override TableIndexInfo CreateLinkToPage(
			IndexPage<TableIndexInfo, RootTableIndexInfo> page)
		{
			object[] keys = page.IndexEntries[0].Keys;
			return new TableIndexLogicalInfo(keys, page.LogicalId);
		}

		protected override TableIndexInfo CreateIndexEntry()
		{
			if (IsLeafIndex)
			{
				if (IsNormalOverClusteredIndex)
				{
					// Normal index over table with clustered key have something different
					// clustered key length is based on the number of fields in the clustered index
					//	we need a pointer to that information in the owner table
					return new TableIndexNormalOverClusteredLeafInfo(
						IndexEntries[0].KeyLength, _ownerTable.ClusteredIndex.ColumnIDs.Length);
				}
				else
				{
					// Heaps and clustered index have leaf info pointing to row index 
					return new TableIndexNormalOrClusteredLeafInfo(IndexEntries[0].KeyLength);
				}
			}
			else
			{
				return new TableIndexLogicalInfo(IndexEntries[0].KeyLength);
			}
		}
		#endregion
	}
}
