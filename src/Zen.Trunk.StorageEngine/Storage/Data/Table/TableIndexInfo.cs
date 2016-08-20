namespace Zen.Trunk.Storage.Data.Table
{
	using System;
	using System.Collections;
	using Zen.Trunk.Storage.Data.Index;
	using Zen.Trunk.Storage.IO;

	/// <summary>
	/// 
	/// </summary>
	/// <remarks>
	/// If the index is a non-unique clustered index then the key row will
	/// have an additional uint32 key used to synthesise a unique key from
	/// a non-unique key. This unique key is only used for reference
	/// purposes from other non-clustered indices defined on the same table
	/// </remarks>
	public class TableIndexInfo : IndexInfo
	{
		#region Private Fields
		private RootTableIndexInfo _rootInfo;
		private readonly BufferFieldTableRow _keyRow;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="TableIndexInfo"/> class.
		/// </summary>
		/// <param name="keySize">Size of the key.</param>
		public TableIndexInfo(int keySize)
		{
			_keyRow = new BufferFieldTableRow(keySize);
			_keyRow.IndexMode = true;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TableIndexInfo" /> class.
		/// </summary>
		/// <param name="keys">The keys.</param>
		public TableIndexInfo(object[] keys)
		{
			_keyRow = new BufferFieldTableRow(keys);
			_keyRow.IndexMode = true;
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets or sets the key <see cref="System.Object"/> at the specified index.
		/// </summary>
		/// <value>
		/// The <see cref="System.Object"/>.
		/// </value>
		/// <param name="index">The index.</param>
		/// <returns></returns>
		public object this[int index]
		{
			get
			{
				return _keyRow[index];
			}
			set
			{
				_keyRow[index] = value;
			}
		}

		/// <summary>
		/// Gets the length of the key.
		/// </summary>
		/// <value>
		/// The length of the key.
		/// </value>
		public int KeyLength => _keyRow.KeyLength;

	    /// <summary>
		/// Gets the keys.
		/// </summary>
		/// <value>
		/// The keys.
		/// </value>
		public object[] Keys
		{
			get
			{
				var keys = new object[_keyRow.KeyLength];
				for (var index = 0; index < _keyRow.KeyLength; ++index)
				{
					keys[index] = _keyRow[index];
				}
				return keys;
			}
		}
		#endregion

		#region Public Methods
		public virtual void SetContext(DatabaseTable def, RootTableIndexInfo rootInfo)
		{
			if (rootInfo.ColumnIDs.Length != _keyRow.KeyLength)
			{
				throw new ArgumentException("Root information of differing key length.");
			}

			// Build list of table columns needed by row
			_rootInfo = rootInfo;
			var columns = new TableColumnInfo[_keyRow.KeyLength];
			for (var keyIndex = 0; keyIndex < columns.Length; ++keyIndex)
			{
				var found = false;
				foreach (var colInfo in def.Columns)
				{
					if (colInfo.Id == rootInfo.ColumnIDs[keyIndex])
					{
						columns[keyIndex] = colInfo;
						found = true;
					}
				}
				if (!found)
				{
					throw new InvalidOperationException("Column ID not found in index.");
				}
			}

			// Set row context
			_keyRow.SetContext(columns);
		}

		public override int CompareTo(IndexInfo rhs)
		{
			var tiRhs = (TableIndexInfo)rhs;
			if (tiRhs._keyRow.KeyLength != _keyRow.KeyLength)
			{
				throw new ArgumentException("Key length mismatch.");
			}

			for (var index = 0; index < _keyRow.KeyLength; ++index)
			{
				var comp = _keyRow.GetComparer(index);
				var value = comp.Compare(_keyRow[index], tiRhs._keyRow[index]);
				if (value != 0)
				{
					// When sort order is descending then flip sign
					if (_rootInfo.ColumnDirections[index] == TableIndexSortDirection.Descending)
					{
						value = -value;
					}

					// Return the value
					return value;
				}
			}

			// Indices are the same
			return 0;
		}
		#endregion

		#region Protected Methods
		/// <summary>
		/// Reads the field chain from the specified stream manager.
		/// </summary>
		/// <param name="streamManager">A <see cref="T:BufferReaderWriter" /> object.</param>
		protected override void DoRead(BufferReaderWriter streamManager)
		{
			// Wire up columns
			base.DoRead(streamManager);
			_keyRow.Read(streamManager);
		}

		/// <summary>
		/// Writes the field chain to the specified stream manager.
		/// </summary>
		/// <param name="streamManager">A <see cref="T:BufferReaderWriter" /> object.</param>
		protected override void DoWrite(BufferReaderWriter streamManager)
		{
			base.DoWrite(streamManager);
			_keyRow.Write(streamManager);
		}
		#endregion
	}

	public class TableIndexLogicalInfo : TableIndexInfo
	{
		#region Private Fields
		private readonly BufferFieldUInt64 _logicalId;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="IndexInfo" /> class.
		/// </summary>
		/// <param name="keySize">Size of the key.</param>
		public TableIndexLogicalInfo(int keySize)
			: base(keySize)
		{
			_logicalId = new BufferFieldUInt64(0);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="IndexInfo" /> class.
		/// </summary>
		/// <param name="keys">The keys.</param>
		public TableIndexLogicalInfo(object[] keys)
			: base(keys)
		{
			_logicalId = new BufferFieldUInt64(0);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="IndexInfo" /> class.
		/// </summary>
		/// <param name="keySize">Size of the key.</param>
		/// <param name="logicalId">The logical id.</param>
		public TableIndexLogicalInfo(int keySize, LogicalPageId logicalId)
			: base(keySize)
		{
			_logicalId = new BufferFieldUInt64(logicalId.Value);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="IndexInfo" /> class.
		/// </summary>
		/// <param name="keys">The keys.</param>
		/// <param name="logicalId">The logical id.</param>
		public TableIndexLogicalInfo(object[] keys, LogicalPageId logicalId)
			: base(keys)
		{
			_logicalId = new BufferFieldUInt64(logicalId.Value);
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Represents the logical ID of the child index page (root and
		/// intermediate index pages only) or the logical ID of the
		/// data page (leaf pages only).
		/// </summary>
		public LogicalPageId LogicalId
		{
			get
			{
				return new LogicalPageId(_logicalId.Value);
			}
			set
			{
				_logicalId.Value = value.Value;
			}
		}
		#endregion

		#region Protected Properties
		/// <summary>
		/// Gets the first buffer field object.
		/// </summary>
		/// <value>A <see cref="T:BufferField"/> object.</value>
		protected override BufferField FirstField => _logicalId;

	    /// <summary>
		/// Gets the last buffer field object.
		/// </summary>
		/// <value>A <see cref="T:BufferField"/> object.</value>
		protected override BufferField LastField => _logicalId;

	    #endregion

		#region Public Methods
		/// <summary>
		/// Returns a hash code for this instance.
		/// </summary>
		/// <returns>
		/// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
		/// </returns>
		public override int GetHashCode()
		{
			return _logicalId.Value.GetHashCode();
		}
		#endregion
	}
}
