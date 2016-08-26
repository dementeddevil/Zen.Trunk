namespace Zen.Trunk.Storage.Data.Table
{
	using System;
	using Zen.Trunk.Storage.IO;

	/// <summary>
	/// 
	/// </summary>
	/// <remarks>
	/// This class is used for leaf index entries for non-clustered indices on
	/// tables that do not have a clustered index.
	/// It is also used for leaf nodes on a clustered index.
	/// A different class must be used for leaf index entries of non-clustered
	/// indices on tables that do have a clustered index as the row id is replaced
	/// with the clustered index key.
	/// </remarks>
	public class TableIndexLeafInfo : TableIndexInfo
	{
		#region Private Fields
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="TableIndexLeafInfo"/> class.
		/// </summary>
		/// <param name="keySize">Size of the key.</param>
		public TableIndexLeafInfo(int keySize)
			: base(keySize)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TableIndexLeafInfo" /> class.
		/// </summary>
		/// <param name="keys">The keys.</param>
		public TableIndexLeafInfo(object[] keys)
			: base(keys)
		{
		}
		#endregion

		#region Public Properties
		#endregion

		#region Protected Properties
		#endregion
	}

	public class TableIndexNormalOrClusteredLeafInfo : TableIndexLeafInfo
	{
		#region Private Fields
		private readonly BufferFieldLogicalPageId _logicalId;
		private readonly BufferFieldUInt16 _rowId;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="TableIndexLeafInfo"/> class.
		/// </summary>
		/// <param name="keySize">Size of the key.</param>
		public TableIndexNormalOrClusteredLeafInfo(int keySize)
			: base(keySize)
		{
			_logicalId = new BufferFieldLogicalPageId(base.LastField);
			_rowId = new BufferFieldUInt16(_logicalId);
		}

	    /// <summary>
		/// Initializes a new instance of the <see cref="TableIndexLeafInfo"/> class.
		/// </summary>
		/// <param name="keys">The keys.</param>
		/// <param name="logicalId">The logical identifier.</param>
		/// <param name="rowId">The row identifier.</param>
		public TableIndexNormalOrClusteredLeafInfo(object[] keys, LogicalPageId logicalId, ushort rowId = 0)
			: base(keys)
		{
			_logicalId = new BufferFieldLogicalPageId(base.LastField, logicalId);
			_rowId = new BufferFieldUInt16(_logicalId, rowId);
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets or sets the logical identifier.
		/// </summary>
		/// <value>
		/// The logical identifier.
		/// </value>
		public LogicalPageId LogicalId
		{
			get
			{
				return _logicalId.Value;
			}
			set
			{
				_logicalId.Value = value;
			}
		}

		/// <summary>
		/// Gets or sets the row identifier.
		/// </summary>
		/// <value>
		/// The row identifier.
		/// </value>
		public ushort RowId
		{
			get
			{
				return _rowId.Value;
			}
			set
			{
				_rowId.Value = value;
			}
		}
		#endregion

		#region Protected Properties
		/// <summary>
		/// Gets the last buffer field object.
		/// </summary>
		/// <value>
		/// A <see cref="T:BufferField" /> object.
		/// </value>
		protected override BufferField LastField => _rowId;
	    #endregion
	}

	public class TableIndexNormalOverClusteredLeafInfo : TableIndexLeafInfo
	{
		#region Private Fields
		private readonly BufferFieldTableRow _clusteredKey;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="TableIndexNormalOverClusteredLeafInfo"/> class.
		/// </summary>
		/// <param name="keySize">Size of the key.</param>
		public TableIndexNormalOverClusteredLeafInfo(int keySize, int clusteredKeySize)
			: base(keySize)
		{
			_clusteredKey = new BufferFieldTableRow(clusteredKeySize);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TableIndexNormalOverClusteredLeafInfo"/> class.
		/// </summary>
		/// <param name="keys">The keys.</param>
		/// <param name="logicalId">The logical identifier.</param>
		public TableIndexNormalOverClusteredLeafInfo(object[] keys, object[] clusteredKeys)
			: base(keys)
		{
			_clusteredKey = new BufferFieldTableRow(clusteredKeys);
		}
		#endregion

		#region Public Properties
		#endregion

		#region Public Methods
		public override void SetContext(DatabaseTable def, RootTableIndexInfo rootInfo)
		{
			base.SetContext(def, rootInfo);

			var columns = new TableColumnInfo[_clusteredKey.KeyLength];
			for (var keyIndex = 0;
			keyIndex < columns.Length;
			++keyIndex)
			{
				// Non-unique clustered key have an additional column that
				//	enforces uniqueness among clustered keys.
				if ((def.ClusteredIndex.IndexSubType & TableIndexSubType.Unique) == 0 &&
					keyIndex == (columns.Length - 1))
				{
					columns[keyIndex] = new TableColumnInfo("ClusteredId", TableColumnDataType.Int, false);
				}
				else
				{
					var found = false;
					foreach (var colInfo in def.Columns)
					{
						if (colInfo.Id == def.ClusteredIndex.ColumnIDs[keyIndex])
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
			}

			_clusteredKey.SetContext(columns);
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
			_clusteredKey.Read(streamManager);
		}

		/// <summary>
		/// Writes the field chain to the specified stream manager.
		/// </summary>
		/// <param name="streamManager">A <see cref="T:BufferReaderWriter" /> object.</param>
		protected override void DoWrite(BufferReaderWriter streamManager)
		{
			base.DoWrite(streamManager);
			_clusteredKey.Write(streamManager);
		}
		#endregion
	}
}
