using System;
using System.Collections;
using Zen.Trunk.Storage.IO;

namespace Zen.Trunk.Storage.Data.Table
{
	/// <summary>
	/// The <b>BufferFieldColumn</b> encapsulates persisting a table column 
	/// data value.
	/// </summary>
	public class BufferFieldColumn : SimpleBufferField<object>
	{
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
		/// <param name="value">The value.</param>
		public BufferFieldColumn(BufferField prev, object value = null)
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
		public TableColumnInfo ColumnInfo { get; set; }

	    /// <summary>
		/// Gets or sets a value indicating whether to persist using index 
		/// semantics.
		/// </summary>
		/// <value>
		/// <c>true</c> to use index mode; otherwise, <c>false</c>.
		/// </value>
		/// <remarks><seealso cref="FieldLength"/></remarks>
		public bool IndexMode { get; set; }

	    /// <summary>
		/// Gets the size of a single element of this field
		/// </summary>
		/// <value>
		/// The size of the data.
		/// </value>
		public override int DataSize => ColumnInfo.MaxDataSize;

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
					return ColumnInfo.Length;
				}
				else
				{
					return ColumnInfo.GetActualDataSize(Value);
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
			ColumnInfo.WriteData(streamManager, Value);
		}

		/// <summary>
		/// Reads the data.
		/// </summary>
		/// <param name="streamManager">The stream manager.</param>
		public void ReadData(BufferReaderWriter streamManager)
		{
			Value = ColumnInfo.ReadData(streamManager);
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
			if (ColumnInfo != null)
			{
				// Check for nulls.
				if (e.NewValue == null)
				{
					if (!ColumnInfo.Nullable)
					{
						throw new ArgumentException(
						    $"Column {ColumnInfo.Name} does not allow nulls.", "value");
					}
					e.NewValue = DBNull.Value;
				}

				// If not null then check for matching type information.
				else if (!ColumnInfo.ColumnType.IsInstanceOfType(e.NewValue))
				{
					throw new ArgumentException(
					    $"Column {ColumnInfo.Name} expects CLR type {ColumnInfo.ColumnType.FullName} and {e.NewValue.GetType().FullName} is not a convertable type.", "value");
				}

				// Truncate or pad fixed length fields
				if (e.NewValue != DBNull.Value)
				{
					if (ColumnInfo.DataType == TableColumnDataType.Char ||
						ColumnInfo.DataType == TableColumnDataType.NChar)
					{
						var tempValue = (string)e.NewValue;
						if (tempValue.Length > ColumnInfo.Length)
						{
							e.NewValue = tempValue.Substring(0, ColumnInfo.Length);
						}
						else if (tempValue.Length < ColumnInfo.Length)
						{
							e.NewValue += new string(' ', ColumnInfo.Length - tempValue.Length);
						}
					}

					// Truncate variable length fields
					if (ColumnInfo.DataType == TableColumnDataType.VarChar ||
						ColumnInfo.DataType == TableColumnDataType.NVarChar)
					{
						var tempValue = (string)e.NewValue;
						if (tempValue.Length > ColumnInfo.Length)
						{
							e.NewValue = tempValue.Substring(0, ColumnInfo.Length);
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
			Value = ColumnInfo.ReadData(streamManager);
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
			ColumnInfo.WriteData(streamManager, Value);
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
