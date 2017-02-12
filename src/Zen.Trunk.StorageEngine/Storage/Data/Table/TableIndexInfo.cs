using System;
using Zen.Trunk.IO;
using Zen.Trunk.Storage.Data.Index;

namespace Zen.Trunk.Storage.Data.Table
{
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
        /// <summary>
        /// Sets the context.
        /// </summary>
        /// <param name="def">The definition.</param>
        /// <param name="rootInfo">The root information.</param>
        /// <exception cref="ArgumentException">Root information of differing key length.</exception>
        /// <exception cref="InvalidOperationException">Column ID not found in index.</exception>
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

        /// <summary>
        /// Compares the current instance with another object of the same type
        /// and returns an integer that indicates whether the current instance
        /// precedes, follows, or occurs in the same position in the sort order
        /// as the other object.
        /// </summary>
        /// <param name="rhs">An object to compare with this instance.</param>
        /// <returns>
        /// A value that indicates the relative order of the objects being
        /// compared. The return value has these meanings:
        /// <list type="bulleted"><listheader>
        /// Value Meaning
        /// </listheader><item>
        /// Less than zero This instance is less than <paramref name="rhs" />.
        /// </item><item>
        /// Zero This instance is equal to <paramref name="rhs" />.
        /// </item><item>
        /// Greater than zero This instance is greater than <paramref name="rhs" />.
        /// </item></list>
        /// </returns>
        /// <exception cref="ArgumentException">Key length mismatch.</exception>
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
        /// <param name="reader">A <see cref="T:SwitchingBinaryReader" /> object.</param>
        protected override void OnRead(SwitchingBinaryReader reader)
		{
			// Wire up columns
			base.OnRead(reader);
			_keyRow.Read(reader);
		}

        /// <summary>
        /// Writes the field chain to the specified stream manager.
        /// </summary>
        /// <param name="writer">A <see cref="T:SwitchingBinaryWriter" /> object.</param>
        protected override void OnWrite(SwitchingBinaryWriter writer)
		{
			base.OnWrite(writer);
			_keyRow.Write(writer);
		}
		#endregion
	}
}
