using System;
using System.Collections;
using System.ComponentModel;
using Zen.Trunk.IO;
using Zen.Trunk.Storage.BufferFields;

namespace Zen.Trunk.Storage.Data.Table
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="BufferFieldWrapper" />
    /// <seealso cref="IComparable{TableColumnInfo}" />
    /// <seealso cref="IEquatable{TableColumnInfo}" />
    /// <seealso cref="INotifyPropertyChanging" />
    /// <seealso cref="INotifyPropertyChanged" />
    /// <seealso cref="IComparer" />
    /// <seealso cref="ICloneable" />
    public class TableColumnInfo : BufferFieldWrapper,
		IComparable<TableColumnInfo>,
		IEquatable<TableColumnInfo>,
		INotifyPropertyChanging,
		INotifyPropertyChanged,
		IComparer,
		ICloneable
	{
		#region Private Fields
		private readonly BufferFieldByte _id;
		private readonly BufferFieldStringFixed _name;
		private readonly BufferFieldByte _dataType;
		private readonly BufferFieldUInt16 _length;
		private readonly BufferFieldBitVector8 _flags;
		private readonly BufferFieldUInt32 _incrementSeed;
		private readonly BufferFieldUInt32 _incrementAmount;
		private readonly BufferFieldUInt32 _incrementValue;

		private Type _columnType;
        #endregion

        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="TableColumnInfo"/> class.
        /// </summary>
        public TableColumnInfo()
		{
			_id = new BufferFieldByte();
			_name = new BufferFieldStringFixed(_id, 32);
			_dataType = new BufferFieldByte(_name);
			_length = new BufferFieldUInt16(_dataType);
			_flags = new BufferFieldBitVector8(_length);
			_incrementSeed = new BufferFieldUInt32(_flags);
			_incrementAmount = new BufferFieldUInt32(_incrementSeed);
			_incrementValue = new BufferFieldUInt32(_incrementAmount);
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="TableColumnInfo"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="dataType">Type of the data.</param>
        public TableColumnInfo(
			string name,
			TableColumnDataType dataType)
			: this()
		{
			Name = name;
			DataType = dataType;
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="TableColumnInfo"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="dataType">Type of the data.</param>
        /// <param name="nullable">if set to <c>true</c> [nullable].</param>
        public TableColumnInfo(
			string name,
			TableColumnDataType dataType,
			bool nullable)
			: this()
		{
			Name = name;
			DataType = dataType;
			Nullable = nullable;
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="TableColumnInfo"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="dataType">Type of the data.</param>
        /// <param name="nullable">if set to <c>true</c> [nullable].</param>
        /// <param name="length">The length.</param>
        public TableColumnInfo(
			string name,
			TableColumnDataType dataType,
			bool nullable,
			ushort length)
			: this()
		{
			Name = name;
			DataType = dataType;
			Nullable = nullable;
			Length = length;
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="TableColumnInfo"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="dataType">Type of the data.</param>
        /// <param name="nullable">if set to <c>true</c> [nullable].</param>
        /// <param name="length">The length.</param>
        /// <param name="autoIncrSeed">The automatic incr seed.</param>
        /// <param name="autoIncrValue">The automatic incr value.</param>
        public TableColumnInfo(
			string name,
			TableColumnDataType dataType,
			bool nullable,
			ushort length,
			uint autoIncrSeed,
			uint autoIncrValue)
			: this()
		{
			Name = name;
			DataType = dataType;
			Nullable = nullable;
			Length = length;
			AutoIncrement = true;
			IncrementSeed = autoIncrSeed;
			IncrementAmount = autoIncrValue;
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets or sets the id.
		/// </summary>
		/// <value>The id.</value>
		public byte Id
		{
			get => _id.Value;
		    internal set
			{
				if (_id.Value != value)
				{
					NotifyPropertyChanging("Id");
					_id.Value = value;
					NotifyPropertyChanged("Id");
				}
			}
		}

		/// <summary>
		/// Gets or sets the name.
		/// </summary>
		/// <value>The name.</value>
		public string Name
		{
			get => _name.Value;
		    set
			{
				if (string.IsNullOrEmpty(value))
				{
					throw new ArgumentException("Name cannot be empty.");
				}
				value = value.Trim();
				if (value.IndexOfAny(new[] { ' ', '\t', '.' }) != -1)
				{
					throw new ArgumentException("Name contains illegal characters.");
				}
				if (value.Length > 32)
				{
					value = value.Substring(0, 32);
				}
				if (_name.Value != value)
				{
					var oldValue = _name.Value;
					try
					{
						NotifyPropertyChanging("Name");
						_name.Value = value;
						NotifyPropertyChanged("Name");
					}
					catch
					{
						_name.Value = oldValue;
						throw;
					}
				}
			}
		}

		/// <summary>
		/// Gets or sets the type of the data.
		/// </summary>
		/// <value>The type of the data.</value>
		public TableColumnDataType DataType
		{
			get => (TableColumnDataType)_dataType.Value;
		    set
			{
				var type = (byte)value;
				if (_dataType.Value != type)
				{
					NotifyPropertyChanging("DataType");
					_dataType.Value = type;
					switch (DataType)
					{
						case TableColumnDataType.Bit:
							SetLengthInternal(1);
							break;

						case TableColumnDataType.Byte:
							SetLengthInternal(1);
							break;

						case TableColumnDataType.Char:
							SetLengthInternal(10);
							break;

						case TableColumnDataType.VarChar:
							SetLengthInternal(50);
							break;

						case TableColumnDataType.NChar:
							SetLengthInternal(10);
							break;

						case TableColumnDataType.NVarChar:
							SetLengthInternal(50);
							break;

						case TableColumnDataType.Short:
							SetLengthInternal(2, true);
							break;

						case TableColumnDataType.Int:
						case TableColumnDataType.Float:
							SetLengthInternal(4, true);
							break;

						case TableColumnDataType.Timestamp:
						case TableColumnDataType.DateTime:
						case TableColumnDataType.Double:
						case TableColumnDataType.Long:
							SetLengthInternal(8, true);
							break;

						case TableColumnDataType.Guid:
						case TableColumnDataType.Money:
							SetLengthInternal(16, true);
							break;

						default:
							throw new InvalidOperationException();
					}
					_columnType = null;
					NotifyPropertyChanged("DataType");
				}
			}
		}

		/// <summary>
		/// Gets or sets the length.
		/// </summary>
		/// <value>The length.</value>
		public ushort Length
		{
			get => _length.Value;
		    set
			{
				switch (DataType)
				{
					case TableColumnDataType.Char:
					case TableColumnDataType.VarChar:
					case TableColumnDataType.NChar:
					case TableColumnDataType.NVarChar:
						if (_length.Value != value)
						{
							SetLengthInternal(value);
						}
						break;
				}
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="TableColumnInfo"/> is nullable.
		/// </summary>
		/// <value><c>true</c> if nullable; otherwise, <c>false</c>.</value>
		public bool Nullable
		{
			get => _flags.GetBit(1);
		    set
			{
				if (_flags.GetBit(1) != value)
				{
					NotifyPropertyChanging("Nullable");
					_flags.SetBit(1, value);
					NotifyPropertyChanged("Nullable");
				}
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether [auto increment].
		/// </summary>
		/// <value><c>true</c> if [auto increment]; otherwise, <c>false</c>.</value>
		public bool AutoIncrement
		{
			get => _flags.GetBit(2);
		    set
			{
				if (!IsIncrementSupported && value)
				{
					throw new ArgumentException("Auto-increment not supported for column data-type.");
				}
				if (_flags.GetBit(2) != value)
				{
					NotifyPropertyChanging("AutoIncrement");
					_flags.SetBit(2, value);
					NotifyPropertyChanged("AutoIncrement");
				}
			}
		}

		/// <summary>
		/// Gets or sets the increment seed.
		/// </summary>
		/// <value>The increment seed.</value>
		public uint IncrementSeed
		{
			get
			{
				if (!IsIncrementSupported)
				{
					return 1;
				}
				return _incrementSeed.Value;
			}
			set
			{
				if (!IsIncrementSupported)
				{
					throw new InvalidOperationException("Increment not supported on this datatype.");
				}
				if (_incrementSeed.Value != value)
				{
					NotifyPropertyChanging("IncrementSeed");
					_incrementSeed.Value = value;
					NotifyPropertyChanged("IncrementSeed");
				}
			}
		}

		/// <summary>
		/// Gets or sets the increment amount.
		/// </summary>
		/// <value>The increment amount.</value>
		public uint IncrementAmount
		{
			get
			{
				if (!IsIncrementSupported)
				{
					return 1;
				}
				return _incrementAmount.Value;
			}
			set
			{
				if (!IsIncrementSupported)
				{
					throw new InvalidOperationException("Increment not supported on this datatype.");
				}
				if (_incrementAmount.Value != value)
				{
					NotifyPropertyChanging("IncrementAmount");
					_incrementAmount.Value = value;
					NotifyPropertyChanged("IncrementAmount");
				}
			}
		}

		/// <summary>
		/// Gets or sets the increment value.
		/// </summary>
		/// <value>The increment value.</value>
		public uint IncrementValue
		{
			get
			{
				if (!IsIncrementSupported)
				{
					return 1;
				}
				return _incrementValue.Value;
			}
			set
			{
				if (!IsIncrementSupported)
				{
					throw new InvalidOperationException("Increment not supported on this datatype.");
				}
				if (_incrementValue.Value != value)
				{
					NotifyPropertyChanging("IncrementValue");
					_incrementValue.Value = value;
					NotifyPropertyChanged("IncrementValue");
				}
			}
		}

		/// <summary>
		/// Gets a value indicating whether this instance is variable length.
		/// </summary>
		/// <value>
		/// <c>true</c> if this instance is variable length; otherwise, <c>false</c>.
		/// </value>
		public bool IsVariableLength
		{
			get
			{
				switch (DataType)
				{
					case TableColumnDataType.VarChar:
					case TableColumnDataType.NVarChar:
						return true;
				}
				return false;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this instance is padded.
		/// </summary>
		/// <value><c>true</c> if this instance is padded; otherwise, <c>false</c>.</value>
		public bool IsPadded
		{
			get
			{
				switch (DataType)
				{
					case TableColumnDataType.NChar:
					case TableColumnDataType.Char:
						return true;
				}
				return false;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this instance is increment supported.
		/// </summary>
		/// <value>
		/// <c>true</c> if this instance is increment supported; otherwise,
		/// <c>false</c>.
		/// </value>
		public bool IsIncrementSupported
		{
			get
			{
				var isSupported = false;
				switch (DataType)
				{
					case TableColumnDataType.Byte:
						if (_length.Value == 1)
						{
							isSupported = true;
						}
						break;
					case TableColumnDataType.Int:
					case TableColumnDataType.Short:
					case TableColumnDataType.Long:
						isSupported = true;
						break;
				}
				return isSupported;
			}
		}

		/// <summary>
		/// Gets a Range object which represents the min and max data size
		/// of this column in bytes.
		/// </summary>
		public InclusiveRange DataSize => new InclusiveRange(MinDataSize, MaxDataSize);

	    /// <summary>
		/// Gets a Type object which represents the CLR version
		/// of this column data type.
		/// </summary>
		public Type ColumnType
		{
			get
			{
				if (_columnType == null)
				{
					switch (DataType)
					{
						case TableColumnDataType.Bit:
							_columnType = typeof(bool);
							break;
						case TableColumnDataType.Byte:
							if (_length.Value == 1)
							{
								_columnType = typeof(byte);
							}
							else
							{
								_columnType = typeof(byte[]);
							}
							break;
						case TableColumnDataType.Char:
						case TableColumnDataType.VarChar:
						case TableColumnDataType.NChar:
						case TableColumnDataType.NVarChar:
							if (Length == 1)
							{
								_columnType = typeof(char);
							}
							else
							{
								_columnType = typeof(string);
							}
							break;
						case TableColumnDataType.DateTime:
							_columnType = typeof(DateTime);
							break;
						case TableColumnDataType.Guid:
							_columnType = typeof(Guid);
							break;
						case TableColumnDataType.Short:
							_columnType = typeof(Int16);
							break;
						case TableColumnDataType.Int:
							_columnType = typeof(Int32);
							break;
						case TableColumnDataType.Long:
							_columnType = typeof(Int64);
							break;
						case TableColumnDataType.Float:
							_columnType = typeof(float);
							break;
						case TableColumnDataType.Double:
							_columnType = typeof(Double);
							break;
						case TableColumnDataType.Money:
							_columnType = typeof(Decimal);
							break;
						case TableColumnDataType.Timestamp:
							_columnType = typeof(ulong);
							break;
						default:
							throw new NotSupportedException();
					}
				}
				return _columnType;
			}
		}

		/// <summary>
		/// Gets the size of the min data.
		/// </summary>
		/// <value>The size of the min data.</value>
		public ushort MinDataSize
		{
			get
			{
				// Shortcut fixed length strings
				if (DataType == TableColumnDataType.Char ||
					DataType == TableColumnDataType.NChar)
				{
					return MaxDataSize;
				}

				var minLength = Length;
				if (IsVariableLength)
				{
					return 2;
				}
				return minLength;
			}
		}

		/// <summary>
		/// Gets the size of the max data.
		/// </summary>
		/// <value>The size of the max data.</value>
		public ushort MaxDataSize
		{
			get
			{
				var maxLength = Length;
				if (DataType == TableColumnDataType.NChar ||
					DataType == TableColumnDataType.NVarChar)
				{
					maxLength *= 2;
				}
				if (IsVariableLength)
				{
					maxLength += 2;
				}
				return maxLength;
			}
		}
        #endregion

        #region Public Methods
        /// <summary>
        /// Gets the actual size of the data.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Unknown column type specified.</exception>
        public int GetActualDataSize(object value)
		{
			switch (DataType)
			{
				case TableColumnDataType.Bit:
					return 1;
				case TableColumnDataType.Byte:
					return Length;
				case TableColumnDataType.DateTime:
				case TableColumnDataType.Double:
				case TableColumnDataType.Float:
				case TableColumnDataType.Guid:
				case TableColumnDataType.Int:
				case TableColumnDataType.Long:
				case TableColumnDataType.Money:
				case TableColumnDataType.Char:
				case TableColumnDataType.NChar:
				case TableColumnDataType.Short:
				case TableColumnDataType.Timestamp:
					return MaxDataSize;
				case TableColumnDataType.VarChar:
					var strVarValue = (string)value;
					return 2 + strVarValue.Length;
				case TableColumnDataType.NVarChar:
					var strNVarValue = (string)value;
					return 2 + (strNVarValue.Length * 2);
				default:
					throw new InvalidOperationException("Unknown column type specified.");
			}
		}

        /// <summary>
        /// Reads the data.
        /// </summary>
        /// <param name="streamManager">The stream manager.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException">Unknown column type specified.</exception>
        public object ReadData(SwitchingBinaryReader streamManager)
		{
			// Sanity checks
			if (streamManager == null)
			{
				throw new ArgumentNullException(nameof(streamManager));
			}

			// TODO: Deal with NULL processing
			object value;
			switch (DataType)
			{
				case TableColumnDataType.Bit:
					value = streamManager.ReadBoolean();
					break;
				case TableColumnDataType.Byte:
					if (Length == 1)
					{
						value = streamManager.ReadByte();
					}
					else
					{
						value = streamManager.ReadBytes(Length);
					}
					break;
				case TableColumnDataType.DateTime:
					value = new DateTime(streamManager.ReadInt64());
					break;
				case TableColumnDataType.Double:
					value = streamManager.ReadDouble();
					break;
				case TableColumnDataType.Float:
					value = streamManager.ReadSingle();
					break;
				case TableColumnDataType.Guid:
					value = new Guid(streamManager.ReadBytes(16));
					break;
				case TableColumnDataType.Int:
					value = streamManager.ReadInt32();
					break;
				case TableColumnDataType.Long:
					value = streamManager.ReadInt64();
					break;
				case TableColumnDataType.Money:
					value = streamManager.ReadDecimal();
					break;
				case TableColumnDataType.Char:
					streamManager.UseUnicode = false;
					if (Length == 1)
					{
						value = streamManager.ReadChar();
					}
					else
					{
						value = streamManager.ReadStringExact(Length);
					}
					break;
				case TableColumnDataType.NChar:
					streamManager.UseUnicode = true;
					if (Length == 1)
					{
						value = streamManager.ReadChar();
					}
					else
					{
						value = streamManager.ReadStringExact(Length);
					}
					break;
				case TableColumnDataType.Short:
					value = streamManager.ReadInt16();
					break;
				case TableColumnDataType.Timestamp:
					value = streamManager.ReadUInt64();
					break;
				case TableColumnDataType.VarChar:
					streamManager.UseUnicode = false;
					value = streamManager.ReadString();
					break;
				case TableColumnDataType.NVarChar:
					streamManager.UseUnicode = true;
					value = streamManager.ReadString();
					break;
				default:
					throw new InvalidOperationException("Unknown column type specified.");
			}
			return value;
		}

        /// <summary>
        /// Writes the data.
        /// </summary>
        /// <param name="streamManager">The stream manager.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException">Unknown column type specified.</exception>
        public void WriteData(SwitchingBinaryWriter streamManager, object value)
		{
			// Sanity checks
			if (streamManager == null)
			{
				throw new ArgumentNullException(nameof(streamManager));
			}

			switch (DataType)
			{
				case TableColumnDataType.Bit:
					streamManager.Write((bool)value);
					break;
				case TableColumnDataType.Byte:
					if (Length == 1)
					{
						streamManager.Write((byte)value);
					}
					else
					{
						streamManager.Write((byte[])value);
					}
					break;
				case TableColumnDataType.DateTime:
					streamManager.Write(((DateTime)value).Ticks);
					break;
				case TableColumnDataType.Double:
					streamManager.Write((double)value);
					break;
				case TableColumnDataType.Float:
					streamManager.Write((float)value);
					break;
				case TableColumnDataType.Guid:
					streamManager.Write(((Guid)value).ToByteArray());
					break;
				case TableColumnDataType.Int:
					streamManager.Write((int)value);
					break;
				case TableColumnDataType.Long:
					streamManager.Write((long)value);
					break;
				case TableColumnDataType.Money:
					streamManager.Write((Decimal)value);
					break;
				case TableColumnDataType.Char:
					streamManager.UseUnicode = false;
					if (Length == 1)
					{
						streamManager.Write((char)value);
					}
					else
					{
						streamManager.WriteStringExact((string)value, Length);
					}
					break;
				case TableColumnDataType.NChar:
					streamManager.UseUnicode = true;
					if (Length == 1)
					{
						streamManager.Write((char)value);
					}
					else
					{
						streamManager.WriteStringExact((string)value, Length);
					}
					break;
				case TableColumnDataType.Short:
					streamManager.Write((short)value);
					break;
				case TableColumnDataType.Timestamp:
					streamManager.Write((ulong)value);
					break;
				case TableColumnDataType.VarChar:
					streamManager.UseUnicode = false;
					streamManager.Write((string)value);
					break;
				case TableColumnDataType.NVarChar:
					streamManager.UseUnicode = true;
					streamManager.Write((string)value);
					break;
				default:
					throw new InvalidOperationException("Unknown column type specified.");
			}
		}
        #endregion

        #region Protected Properties
        /// <summary>
        /// Gets the first buffer field object.
        /// </summary>
        /// <value>
        /// A <see cref="T:BufferField" /> object.
        /// </value>
        protected override BufferField FirstField => _id;

        /// <summary>
        /// Gets the last buffer field object.
        /// </summary>
        /// <value>
        /// A <see cref="T:BufferField" /> object.
        /// </value>
        protected override BufferField LastField => _incrementValue;
	    #endregion

		#region Private Methods
		private void SetLengthInternal(ushort newLength)
		{
			SetLengthInternal(newLength, false);
		}
		private void SetLengthInternal(ushort newLength, bool force)
		{
			if (newLength == 0)
			{
				throw new ArgumentException("Length cannot be set to zero.");
			}
			if ((_length.Value == 0 || force) && (_length.Value != newLength))
			{
				NotifyPropertyChanging("Length");
				_length.Value = newLength;
				NotifyPropertyChanged("Length");
			}
		}
		private void NotifyPropertyChanging(string propertyName)
		{
			if (PropertyChanging != null)
			{
				PropertyChanging(this, new PropertyChangingEventArgs(propertyName));
			}
		}
		private void NotifyPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}
        #endregion

        #region IComparable<ColumnInfo> Members
        /// <summary>
        /// Compares the current instance with another object of the same type and returns an integer that indicates whether the current instance precedes, follows, or occurs in the same position in the sort order as the other object.
        /// </summary>
        /// <param name="other">An object to compare with this instance.</param>
        /// <returns>
        /// A value that indicates the relative order of the objects being compared. The return value has these meanings: Value Meaning Less than zero This instance precedes <paramref name="other" /> in the sort order.  Zero This instance occurs in the same position in the sort order as <paramref name="other" />. Greater than zero This instance follows <paramref name="other" /> in the sort order.
        /// </returns>
        public int CompareTo(TableColumnInfo other)
		{
			/*if (IsVariableLength == other.IsVariableLength)
			{
				int typeComp = DataType.CompareTo (other.DataType);
				if (typeComp != 0)
				{
					return typeComp;
				}
				return Index.CompareTo (other.Index);
			}
			if (!IsVariableLength)
			{
				return -1;
			}*/
			return 1;
		}
        #endregion

        #region IEquatable<ColumnInfo> Members
        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.
        /// </returns>
        public bool Equals(TableColumnInfo other)
		{
			//return Index == other.Index;
			return false;
		}
        #endregion

        #region INotifyPropertyChanging Members
        /// <summary>
        /// Occurs when a property value is changing.
        /// </summary>
        public event PropertyChangingEventHandler PropertyChanging;
        #endregion

        #region INotifyPropertyChanged Members
        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region ICloneable Members
        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns></returns>
        public TableColumnInfo Clone()
		{
			return (TableColumnInfo)MemberwiseClone();
		}

		object ICloneable.Clone()
		{
			return MemberwiseClone();
		}
		#endregion

		#region IComparer Members

		int IComparer.Compare(object x, object y)
		{
			// Check for nulls...
			bool lhsNull = false, rhsNull = false;
			if (x == null || x == DBNull.Value)
			{
				lhsNull = true;
			}
			if (y == null || y == DBNull.Value)
			{
				rhsNull = true;
			}

			// Special case
			// A non-unique clustered index has an additional identifier column
			//	added to the keyset to create unique entries.
			//	This means we need to support searching in one of two modes:
			//	1. Search clustered index ignoring the identifier column
			//	2. Search clustered index using the identifier column
			// We do this by treating null for a non-nullable column as
			//	being an exact match. Now we just specify null for the
			//	identifier when we want scenario 1.
			// This code-smell is mitigated by throwing an error if the
			//	query processor encounters ISNULL on a non-nullable column in
			//	a SQL query.
			if (!Nullable && (lhsNull || rhsNull))
			{
				return 0;
			}

			if (lhsNull && rhsNull)
			{
				return 0;
			}
			else if (lhsNull)
			{
				return 1;
			}
			else if (rhsNull)
			{
				return -1;
			}

			if (!(ColumnType.IsAssignableFrom(x.GetType())))
			{
				throw new ArgumentException("x not correct type.");
			}
			if (!(ColumnType.IsAssignableFrom(y.GetType())))
			{
				throw new ArgumentException("y not correct type.");
			}

			// Delegate to comparison operator for type
			switch (DataType)
			{
				case TableColumnDataType.Bit:
					return ((bool)x).CompareTo(y);
				case TableColumnDataType.Byte:
					if (Length == 1)
					{
						return ((byte)x).CompareTo(y);
					}
					else
					{
						byte[] lhs = (byte[])x, rhs = (byte[])y;
						for (var index = 0; index < Math.Min(lhs.Length, rhs.Length); ++index)
						{
							var comp = lhs[index].CompareTo(rhs[index]);
							if (comp != 0)
							{
								return comp;
							}
						}
						return lhs.Length.CompareTo(rhs.Length);
					}
				case TableColumnDataType.DateTime:
					return ((long)x).CompareTo(y);
				case TableColumnDataType.Double:
					return ((double)x).CompareTo(y);
				case TableColumnDataType.Float:
					return ((float)x).CompareTo(y);
				case TableColumnDataType.Guid:
					return ((Guid)x).CompareTo(y);
				case TableColumnDataType.Int:
					return ((int)x).CompareTo(y);
				case TableColumnDataType.Long:
					return ((long)x).CompareTo(y);
				case TableColumnDataType.Money:
					return ((Decimal)x).CompareTo(y);
				case TableColumnDataType.Char:
				case TableColumnDataType.NChar:
					if (Length == 1)
					{
						return ((char)x).CompareTo(y);
					}
					else
					{
						char[] lhs = (char[])x, rhs = (char[])y;
						for (var index = 0; index < Math.Min(lhs.Length, rhs.Length); ++index)
						{
							var comp = lhs[index].CompareTo(rhs[index]);
							if (comp != 0)
							{
								return comp;
							}
						}
						return lhs.Length.CompareTo(rhs.Length);
					}
				case TableColumnDataType.Short:
					return ((short)x).CompareTo(y);
				case TableColumnDataType.Timestamp:
					return ((ulong)x).CompareTo(y);
				case TableColumnDataType.VarChar:
				case TableColumnDataType.NVarChar:
					return ((string)x).CompareTo(y);
				default:
					throw new InvalidOperationException("Unknown column type specified.");
			}
		}

		#endregion
	}
}
