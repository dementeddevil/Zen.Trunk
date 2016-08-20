namespace Zen.Trunk.Storage.Data.Table
{
	using System;
	using System.Collections;
	using Zen.Trunk.Storage.IO;

	/// <summary>
	/// The <b>BufferFieldColumn</b> encapsulates persisting a table column 
	/// data value.
	/// </summary>
	public class BufferFieldColumn : SimpleBufferField<object>
	{
		#region Private Fields
		private TableColumnInfo _fieldDef;
		private bool _indexMode;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="BufferFieldColumn"/> class.
		/// </summary>
		public BufferFieldColumn()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BufferFieldColumn"/> class.
		/// </summary>
		/// <param name="prev">The previous.</param>
		public BufferFieldColumn(BufferField prev)
			: this(prev, null)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BufferFieldColumn"/> class.
		/// </summary>
		/// <param name="prev">The previous.</param>
		/// <param name="value">The value.</param>
		public BufferFieldColumn(BufferField prev, object value)
			: base(prev, value)
		{
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets or sets the column information.
		/// </summary>
		/// <value>
		/// The column information.
		/// </value>
		public TableColumnInfo ColumnInfo
		{
			get
			{
				return _fieldDef;
			}
			set
			{
				_fieldDef = value;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether to persist using index 
		/// semantics.
		/// </summary>
		/// <value>
		/// <c>true</c> to use index mode; otherwise, <c>false</c>.
		/// </value>
		/// <remarks><seealso cref="FieldLength"/></remarks>
		public bool IndexMode
		{
			get
			{
				return _indexMode;
			}
			set
			{
				_indexMode = value;
			}
		}

		/// <summary>
		/// Gets the size of a single element of this field
		/// </summary>
		/// <value>
		/// The size of the data.
		/// </value>
		public override int DataSize => _fieldDef.MaxDataSize;

	    /// <summary>
		/// Gets the maximum length of this field.
		/// </summary>
		/// <value>
		/// The length of the field.
		/// </value>
		/// <remarks>
		/// If IndexMode is <c>false</c> then the value returned is the actual
		/// length of the value assigned to this column instance.
		/// If IndexMode is <c>true</c> then the value returned is the maximum
		/// length as defined by the column instance.
		/// </remarks>
		public override int FieldLength
		{
			get
			{
				if (IndexMode)
				{
					return _fieldDef.Length;
				}
				else
				{
					return _fieldDef.GetActualDataSize(Value);
				}
			}
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Writes the data.
		/// </summary>
		/// <param name="streamManager">The stream manager.</param>
		public void WriteData(BufferReaderWriter streamManager)
		{
			_fieldDef.WriteData(streamManager, Value);
		}

		/// <summary>
		/// Reads the data.
		/// </summary>
		/// <param name="streamManager">The stream manager.</param>
		public void ReadData(BufferReaderWriter streamManager)
		{
			Value = _fieldDef.ReadData(streamManager);
		}
		#endregion

		#region Protected Methods
		/// <summary>
		/// Raises the <see cref="E:Changing" /> event.
		/// </summary>
		/// <param name="e">The 
		/// <see cref="T:BufferFieldChangingEventArgs" /> 
		/// instance containing the event data.</param>
		/// <returns>
		/// <c>true</c> if the change has been allowed; otherwise,
		/// <c>false</c> if the change has been cancelled.
		/// </returns>
		/// <exception cref="System.ArgumentException">
		/// value
		/// or
		/// value
		/// </exception>
		protected override bool OnValueChanging(BufferFieldChangingEventArgs e)
		{
			if (_fieldDef != null)
			{
				// Check for nulls.
				if (e.NewValue == null)
				{
					if (!_fieldDef.Nullable)
					{
						throw new ArgumentException(
							string.Format("Column {0} does not allow nulls.",
							_fieldDef.Name), "value");
					}
					e.NewValue = DBNull.Value;
				}

				// If not null then check for matching type information.
				else if (!_fieldDef.ColumnType.IsAssignableFrom(e.NewValue.GetType()))
				{
					throw new ArgumentException(string.Format(
						"Column {0} expects CLR type {1} and {2} is not a convertable type.",
						_fieldDef.Name, _fieldDef.ColumnType.FullName,
						e.NewValue.GetType().FullName), "value");
				}

				// Truncate or pad fixed length fields
				if (e.NewValue != DBNull.Value)
				{
					if (_fieldDef.DataType == TableColumnDataType.Char ||
						_fieldDef.DataType == TableColumnDataType.NChar)
					{
						var tempValue = (string)e.NewValue;
						if (tempValue.Length > _fieldDef.Length)
						{
							e.NewValue = tempValue.Substring(0, _fieldDef.Length);
						}
						else if (tempValue.Length < _fieldDef.Length)
						{
							e.NewValue += new string(' ', _fieldDef.Length - tempValue.Length);
						}
					}

					// Truncate variable length fields
					if (_fieldDef.DataType == TableColumnDataType.VarChar ||
						_fieldDef.DataType == TableColumnDataType.NVarChar)
					{
						var tempValue = (string)e.NewValue;
						if (tempValue.Length > _fieldDef.Length)
						{
							e.NewValue = tempValue.Substring(0, _fieldDef.Length);
						}
					}
				}
			}

			return base.OnValueChanging(e);
		}

		/// <summary>
		/// Called when reading from the specified stream manager.
		/// </summary>
		/// <param name="streamManager"></param>
		/// <remarks>
		/// Derived classes must provide an implementation for this method.
		/// </remarks>
		protected override void OnRead(BufferReaderWriter streamManager)
		{
			Value = _fieldDef.ReadData(streamManager);
		}

		/// <summary>
		/// Called when writing to the specified stream manager.
		/// </summary>
		/// <param name="streamManager"></param>
		/// <remarks>
		/// Derived classes must provide an implementation for this method.
		/// </remarks>
		protected override void OnWrite(BufferReaderWriter streamManager)
		{
			_fieldDef.WriteData(streamManager, Value);
		}
		#endregion
	}

	public class BufferFieldTableRow : BufferFieldWrapper
	{
		#region Private Fields
		private readonly BufferFieldColumn[] _keys;
		private bool _hasContext;
		private bool _indexMode;
		private bool _hasRowSize;
		private int _rowSize;
		#endregion

		#region Public Constructors
		public BufferFieldTableRow(int keySize)
		{
			_keys = new BufferFieldColumn[keySize];

			BufferFieldColumn prevBufferField = null;
			for (var index = 0; index < keySize; ++index)
			{
				_keys[index] = new BufferFieldColumn(prevBufferField);
				prevBufferField = _keys[index];
			}
		}

		public BufferFieldTableRow(object[] keys)
		{
			_keys = new BufferFieldColumn[keys.Length];

			BufferFieldColumn prevBufferField = null;
			for (var index = 0; index < keys.Length; ++index)
			{
				_keys[index] = new BufferFieldColumn(prevBufferField, keys[index]);
				prevBufferField = _keys[index];
			}
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Represents the key this object represents.
		/// </summary>
		public object this[int index]
		{
			get
			{
				return _keys[index].Value;
			}
			set
			{
				_keys[index].Value = value;
			}
		}

		public bool IndexMode
		{
			get
			{
				return _indexMode;
			}
			set
			{
				if (_indexMode != value)
				{
					_indexMode = value;
					foreach (var column in _keys)
					{
						column.IndexMode = _indexMode;
					}
					_hasRowSize = false;
				}
			}
		}

		public int KeyLength => _keys.Length;

	    public int RowSize
		{
			get
			{
				if (!_hasRowSize)
				{
					CheckHasContext();
					_rowSize = 0;
					foreach (var col in _keys)
					{
						_rowSize += col.FieldLength;
					}
					_hasRowSize = true;
				}
				return _rowSize;
			}
		}
		#endregion

		#region Public Methods
		public void SetContext(TableColumnInfo[] columns)
		{
			if (columns != null && columns.Length != _keys.Length)
			{
				throw new ArgumentException("Root information of differing key length.");
			}
			for (var index = 0; index < _keys.Length; ++index)
			{
				if (columns != null)
				{
					_keys[index].ColumnInfo = columns[index];
				}
				else
				{
					_keys[index].ColumnInfo = null;
				}
			}
			if (columns != null)
			{
				_hasContext = true;
			}
			else
			{
				_hasContext = false;
			}
		}

		public IComparer GetComparer(int index)
		{
			return _keys[index].ColumnInfo as IComparer;
		}

		public void WriteData(BufferReaderWriter streamManager)
		{
			CheckHasContext();
			foreach (var column in _keys)
			{
				column.WriteData(streamManager);
			}
		}

		public void ReadData(BufferReaderWriter streamManager)
		{
			CheckHasContext();
			foreach (var column in _keys)
			{
				column.ReadData(streamManager);
			}
		}
		#endregion

		#region Protected Properties
		protected override BufferField FirstField => _keys[0];

	    protected override BufferField LastField => _keys[_keys.Length - 1];

	    #endregion

		#region Private Methods
		private void CheckHasContext()
		{
			if (!_hasContext)
			{
				throw new InvalidOperationException("No context.");
			}
		}
		#endregion
	}
}
