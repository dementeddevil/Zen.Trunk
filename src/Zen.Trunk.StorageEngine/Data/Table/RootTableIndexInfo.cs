using System;
using Zen.Trunk.Storage.BufferFields;
using Zen.Trunk.Storage.Data.Index;

namespace Zen.Trunk.Storage.Data.Table
{
	/// <summary>
	/// TableIndexSubType defines the sub-type of table index.
	/// </summary>
	[Flags]
	public enum TableIndexSubType : byte
	{
		/// <summary>
		/// Index is a normal index.
		/// </summary>
		Normal = 1,

		/// <summary>
		/// Index is a clustered index
		/// </summary>
		Clustered = 2,

		/// <summary>
		/// Index does not allow duplicate keys.
		/// </summary>
		Unique = 4,

		/// <summary>
		/// Index is the primary index for the table.
		/// </summary>
		Primary = 8,
	}

	/// <summary>
	/// 
	/// </summary>
	public enum TableIndexSortDirection : byte
	{
		/// <summary>
		/// Index column sorts ascending
		/// </summary>
		Ascending = 0,

		/// <summary>
		/// Index column sorts descending
		/// </summary>
		Descending = 1,
	}

	/// <summary>
	/// RootTableIndexInfo defines root index information for table indices.
	/// </summary>
	public class RootTableIndexInfo : RootIndexInfo
	{
		#region Private Fields
		private readonly BufferFieldByte _indexSubType;
		private readonly BufferFieldByteArrayUnbounded _columnIDs;
		private readonly BufferFieldByteArrayUnbounded _directions;
		private readonly BufferFieldUInt16 _keySize;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="RootTableIndexInfo"/> class.
		/// </summary>
		public RootTableIndexInfo()
		{
			_indexSubType = new BufferFieldByte(base.LastField);
			_columnIDs = new BufferFieldByteArrayUnbounded(_indexSubType);
			_directions = new BufferFieldByteArrayUnbounded(_columnIDs);
			_keySize = new BufferFieldUInt16(_directions);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RootTableIndexInfo"/> class.
		/// </summary>
		/// <param name="indexId">The index identifier.</param>
		public RootTableIndexInfo(IndexId indexId)
			: base(indexId)
		{
			_indexSubType = new BufferFieldByte(base.LastField);
			_columnIDs = new BufferFieldByteArrayUnbounded(_indexSubType);
			_directions = new BufferFieldByteArrayUnbounded(_columnIDs);
			_keySize = new BufferFieldUInt16(_directions);
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets or sets the sub-type of the table index.
		/// </summary>
		/// <value>The type of the index sub.</value>
		public TableIndexSubType IndexSubType
		{
			get => (TableIndexSubType)_indexSubType.Value;
		    set => _indexSubType.Value = (byte)value;
		}

		/// <summary>
		/// Gets or sets the collection of column id that comprise the index.
		/// </summary>
		/// <value>The column id array.</value>
		public byte[] ColumnIDs
		{
			get => _columnIDs.Value;
		    set => _columnIDs.Value = value;
		}

		/// <summary>
		/// Gets or sets the column directions.
		/// </summary>
		/// <value>
		/// The column directions.
		/// </value>
		public TableIndexSortDirection[] ColumnDirections
		{
			get
			{
				var values = _directions.Value;
				var translatedValues = new TableIndexSortDirection[values.Length];
				for (var index = 0; index < values.Length; ++index)
				{
					translatedValues[index] = (TableIndexSortDirection)values[index];
				}
				return translatedValues;
			}
			set
			{
				var translatedValues = new byte[value.Length];
				for (var index = 0; index < value.Length; ++index)
				{
					translatedValues[index] = (byte)value[index];
				}
				_directions.Value = translatedValues;
			}
		}

		/// <summary>
		/// Gets the size of each index key.
		/// </summary>
		/// <value>The size of the key.</value>
		public ushort KeySize => _keySize.Value;

	    #endregion

		#region Public Methods
		/// <summary>
		/// Determines the key size given an appropriate 
		/// <see cref="T:DatabaseTable"/> owner object.
		/// </summary>
		/// <param name="owner"></param>
		public void UpdateKeySize(DatabaseTable owner)
		{
			// Sanity checks
			if (owner == null)
			{
				throw new ArgumentNullException(nameof(owner));
			}

			ushort keySize = 0;
			foreach (ushort columnId in _columnIDs.Value)
			{
				keySize = owner.FindColumn(columnId).MaxDataSize;
			}
			_keySize.Value = keySize;
		}
		#endregion

		#region Protected Properties
		/// <summary>
		/// Gets the last buffer field object.
		/// </summary>
		/// <value>
		/// A <see cref="T:BufferField" /> object.
		/// </value>
		protected override BufferField LastField => _keySize;

	    #endregion
	}
}
