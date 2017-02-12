using System;
using Zen.Trunk.Storage.BufferFields;
using Zen.Trunk.IO;

namespace Zen.Trunk.Storage.Data.Table
{
	/// <summary>
	/// <c>TableIndexLeafInfo</c> serves as a base class for all leaf index
    /// information.
	/// </summary>
	public class TableIndexLeafInfo : TableIndexLogicalInfo
	{
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

	    /// <summary>
	    /// Initializes a new instance of the <see cref="TableIndexLeafInfo"/> class.
	    /// </summary>
	    /// <param name="keySize">Size of the key.</param>
	    /// <param name="logicalId">The logical page id.</param>
	    public TableIndexLeafInfo(int keySize, LogicalPageId logicalId)
            : base(keySize, logicalId)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TableIndexLeafInfo" /> class.
        /// </summary>
        /// <param name="keys">The keys.</param>
	    /// <param name="logicalId">The logical page id.</param>
        public TableIndexLeafInfo(object[] keys, LogicalPageId logicalId)
            : base(keys, logicalId)
        {
        }
        #endregion
    }

    /// <summary>
    /// <c>TableIndexClusteredLeafInfo</c> describes clustered index information
    /// for a table that contains a clustered index.
    /// </summary>
    public class TableIndexClusteredLeafInfo : TableIndexLeafInfo
    {
        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="TableIndexClusteredLeafInfo"/> class.
        /// </summary>
        /// <param name="keySize">Size of the key.</param>
        public TableIndexClusteredLeafInfo(int keySize)
			: base(keySize)
		{
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TableIndexClusteredLeafInfo" /> class.
        /// </summary>
        /// <param name="keys">The keys.</param>
        public TableIndexClusteredLeafInfo(object[] keys)
			: base(keys)
		{
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TableIndexClusteredLeafInfo"/> class.
        /// </summary>
        /// <param name="keySize">Size of the key.</param>
	    /// <param name="logicalId">The logical page id.</param>
        public TableIndexClusteredLeafInfo(int keySize, LogicalPageId logicalId)
            : base(keySize, logicalId)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TableIndexClusteredLeafInfo" /> class.
        /// </summary>
        /// <param name="keys">The keys.</param>
	    /// <param name="logicalId">The logical page id.</param>
        public TableIndexClusteredLeafInfo(object[] keys, LogicalPageId logicalId)
            : base(keys, logicalId)
        {
        }
        #endregion
    }

    /// <summary>
    /// <c>TableIndexNormalLeafInfo</c> describes leaf index information for a
    /// table that does not contain a clustered index.
    /// </summary>
	public class TableIndexNormalLeafInfo : TableIndexLeafInfo
	{
		#region Private Fields
		private readonly BufferFieldUInt16 _rowId;
        #endregion

        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="TableIndexNormalLeafInfo"/> class.
        /// </summary>
        /// <param name="keySize">Size of the key.</param>
        public TableIndexNormalLeafInfo(int keySize)
			: base(keySize)
		{
			_rowId = new BufferFieldUInt16(base.LastField);
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="TableIndexNormalLeafInfo"/> class.
        /// </summary>
        /// <param name="keys">The keys.</param>
        /// <param name="logicalId">The logical identifier.</param>
        /// <param name="rowId">The row identifier.</param>
        public TableIndexNormalLeafInfo(object[] keys, LogicalPageId logicalId, ushort rowId = 0)
			: base(keys, logicalId)
		{
			_rowId = new BufferFieldUInt16(base.LastField, rowId);
		}
		#endregion

		#region Public Properties
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

    /// <summary>
    /// <c>TableIndexNormalOverClusteredLeafInfo</c> describes a normal index
    /// for a table that contains a clustered index.
    /// </summary>
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
	    /// <param name="clusteredKeySize">Size of the clustered index.</param>
	    public TableIndexNormalOverClusteredLeafInfo(int keySize, int clusteredKeySize)
			: base(keySize)
		{
			_clusteredKey = new BufferFieldTableRow(clusteredKeySize);
		}

	    /// <summary>
	    /// Initializes a new instance of the <see cref="TableIndexNormalOverClusteredLeafInfo"/> class.
	    /// </summary>
	    /// <param name="keys">The keys.</param>
	    /// <param name="clusteredKeys">Clustered key entries.</param>
	    public TableIndexNormalOverClusteredLeafInfo(object[] keys, object[] clusteredKeys)
			: base(keys)
		{
			_clusteredKey = new BufferFieldTableRow(clusteredKeys);
		}
        #endregion

        #region Public Properties
        #endregion

        #region Public Methods
        /// <summary>
        /// Sets the context.
        /// </summary>
        /// <param name="def">The definition.</param>
        /// <param name="rootInfo">The root information.</param>
        /// <exception cref="InvalidOperationException">Column ID not found in index.</exception>
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
        /// <param name="reader">A <see cref="T:SwitchingBinaryReader" /> object.</param>
        protected override void OnRead(SwitchingBinaryReader reader)
		{
			// Wire up columns
			base.OnRead(reader);
			_clusteredKey.Read(reader);
		}

        /// <summary>
        /// Writes the field chain to the specified stream manager.
        /// </summary>
        /// <param name="writer">A <see cref="T:SwitchingBinaryWriter" /> object.</param>
        protected override void OnWrite(SwitchingBinaryWriter writer)
		{
			base.OnWrite(writer);
			_clusteredKey.Write(writer);
		}
		#endregion
	}
}
