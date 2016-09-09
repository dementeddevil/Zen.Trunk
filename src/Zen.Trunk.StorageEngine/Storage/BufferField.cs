using System;
using System.Collections.Specialized;
using System.IO;
using Zen.Trunk.Storage.IO;
using Zen.Trunk.Storage.Log;
// ReSharper disable MissingXmlDoc

namespace Zen.Trunk.Storage
{
	public class BufferFieldChangingEventArgs : EventArgs
	{
	    public BufferFieldChangingEventArgs(object oldValue, object newValue)
		{
			OldValue = oldValue;
			NewValue = newValue;
		}

		public object OldValue { get; }

	    public object NewValue { get; set; }

	    public bool Cancel { get; set; }
	}

	/// <summary>
	/// Represents a field stored in a buffer.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Fields can be locked or unlocked.
	/// Locked fields never write themselves when saving to a buffer.
	/// </para>
	/// </remarks>
	public abstract class BufferField
	{
		#region Private Fields
		private bool _isWriteable = true;
		private BufferField _next;
		#endregion

		#region Public Events
		public event EventHandler<BufferFieldChangingEventArgs> Changing;
		public event EventHandler Changed;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="BufferField"/> class.
		/// </summary>
		public BufferField()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BufferField"/> class.
		/// </summary>
		/// <param name="prev">The prev.</param>
		public BufferField(BufferField prev)
		{
			if (prev != null)
			{
				prev.SetNextField(this);
			}
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets a value indicating whether this instance is writeable.
		/// </summary>
		/// <value><c>true</c> if this instance is writeable; otherwise, <c>false</c>.</value>
		public bool IsWriteable => _isWriteable;

	    /// <summary>
		/// Gets the size of a single element of this field
		/// </summary>
		/// <value>The size of the data.</value>
		public abstract int DataSize
		{
			get;
		}

		/// <summary>
		/// Gets the number of discrete elements tracked in this object
		/// </summary>
		/// <value>The max number of elements.</value>
		/// <remarks>
		/// By default this property returns 1.
		/// </remarks>
		[CLSCompliant(false)]
		public virtual ushort MaxElements => 1;

	    /// <summary>
		/// Gets the maximum length of this field.
		/// </summary>
		/// <value>The length of the field.</value>
		/// <remarks>
		/// By default this is the <see cref="P:DataSize"/> value multiplied
		/// by the <see cref="P:MaxElements"/> value.
		/// </remarks>
		public virtual int FieldLength => (DataSize * MaxElements);

	    /// <summary>
		/// Gets the next field.
		/// </summary>
		/// <value>The next <see cref="T:BufferField"/> field object.</value>
		public BufferField NextField => _next;

	    #endregion

		#region Public Methods
		/// <summary>
		/// Locks this instance.
		/// </summary>
		public void Lock()
		{
			_isWriteable = true;
		}

		/// <summary>
		/// Unlocks this instance.
		/// </summary>
		public void Unlock()
		{
			_isWriteable = false;
		}

		/// <summary>
		/// Reads this instance from the specified steam manager.
		/// </summary>
		/// <param name="streamManager">A <see cref="T:BufferReaderWriter"/> object.</param>
		public void Read(BufferReaderWriter streamManager)
		{
			OnRead(streamManager);
			if (_next != null && _next.CanContinue(true))
			{
				_next.Read(streamManager);
			}
		}

		/// <summary>
		/// Writes the specified stream manager.
		/// </summary>
		/// <param name="streamManager">A <see cref="T:BufferReaderWriter"/> object.</param>
		public void Write(BufferReaderWriter streamManager)
		{
			streamManager.IsWritable = IsWriteable;
			OnWrite(streamManager);
			if (_next != null && _next.CanContinue(false))
			{
				_next.Write(streamManager);
			}
		}
		#endregion

		#region Protected Methods
		/// <summary>
		/// Determines whether this instance can continue persistence.
		/// </summary>
		/// <param name="isReading">if set to <c>true</c> then instance is being read;
		/// otherwise <c>false</c> and the instance is being written.</param>
		/// <returns>
		/// <c>true</c> if this instance can continue persistence; otherwise, <c>false</c>.
		/// </returns>
		/// <remarks>
		/// By default this method returns <c>true</c>.
		/// </remarks>
		protected virtual bool CanContinue(bool isReading)
		{
			return true;
		}

		/// <summary>
		/// Raises the <see cref="E:Changing"/> event.
		/// </summary>
		/// <param name="e">The <see cref="T:BufferFieldChangingEventArgs"/> 
		/// instance containing the event data.</param>
		/// <returns><c>true</c> if the change has been allowed; otherwise,
		/// <c>false</c> if the change has been cancelled.</returns>
		protected virtual bool OnValueChanging(BufferFieldChangingEventArgs e)
		{
			var handler = Changing;
			if (handler != null)
			{
			    // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
				foreach (EventHandler<BufferFieldChangingEventArgs>
					handlerInstance in handler.GetInvocationList())
				{
					handlerInstance(this, e);
					if (e.Cancel)
					{
						return false;
					}
				}
			}
			return true;
		}

		/// <summary>
		/// Raises the <see cref="E:Changed"/> event.
		/// </summary>
		/// <param name="e">The <see cref="T:EventArgs"/> instance containing
		/// the event data.</param>
		protected virtual void OnValueChanged(EventArgs e)
		{
			var handler = Changed;
			if (handler != null)
			{
				handler(this, e);
			}
		}

		/// <summary>
		/// Called when reading from the specified stream manager.
		/// </summary>
		/// <param name="streamManager">A <see cref="T:BufferReaderWriter"/> object.</param>
		/// <remarks>
		/// Derived classes must provide an implementation for this method.
		/// </remarks>
		protected abstract void OnRead(BufferReaderWriter streamManager);

		/// <summary>
		/// Called when writing to the specified stream manager.
		/// </summary>
		/// <param name="streamManager">A <see cref="T:BufferReaderWriter"/> object.</param>
		/// <remarks>
		/// Derived classes must provide an implementation for this method.
		/// </remarks>
		protected abstract void OnWrite(BufferReaderWriter streamManager);
		#endregion

		#region Private Methods
		private void SetNextField(BufferField next)
		{
			if (_next != null)
			{
				throw new InvalidOperationException("Already chained.");
			}
			_next = next;
		}
		#endregion
	}

	/// <summary>
	/// <c>BufferFieldWrapperBase</c> is an abstract class usedto wrap a chain
	/// of <see cref="T:BufferField"/> derived classes.
	/// </summary>
	public class BufferFieldWrapperBase
	{
		#region Private Fields
		private bool _gotTotalLength;
		private int _totalLength;
		#endregion

		#region Protected Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="BufferFieldWrapperBase"/> class.
		/// </summary>
		protected BufferFieldWrapperBase()
		{
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets the total length of the field chain.
		/// </summary>
		/// <value>The total length of the field.</value>
		public int TotalFieldLength
		{
			get
			{
				if (!_gotTotalLength)
				{
					_totalLength = 0;
					var field = FirstField;
					while (field != null)
					{
						_totalLength += field.FieldLength;
						field = field.NextField;
					}
					_gotTotalLength = true;
				}
				return _totalLength;
			}
		}
		#endregion

		#region Protected Properties
		/// <summary>
		/// Gets the first buffer field object.
		/// </summary>
		/// <value>A <see cref="T:BufferField"/> object.</value>
		protected virtual BufferField FirstField => null;

	    /// <summary>
		/// Gets the last buffer field object.
		/// </summary>
		/// <value>A <see cref="T:BufferField"/> object.</value>
		protected virtual BufferField LastField => null;

	    #endregion

		#region Protected Methods
		/// <summary>
		/// Reads the field chain from the specified stream manager.
		/// </summary>
		/// <param name="streamManager">A <see cref="T:BufferReaderWriter"/> object.</param>
		protected virtual void DoRead(BufferReaderWriter streamManager)
		{
			if (FirstField != null)
			{
				FirstField.Read(streamManager);
			}
		}

		/// <summary>
		/// Writes the field chain to the specified stream manager.
		/// </summary>
		/// <param name="streamManager">A <see cref="T:BufferReaderWriter"/> object.</param>
		protected virtual void DoWrite(BufferReaderWriter streamManager)
		{
			if (FirstField != null)
			{
				FirstField.Write(streamManager);
			}
		}
		#endregion
	}

	/// <summary>
	/// Represents a fixed width field.
	/// </summary>
	/// <typeparam name="T">The underlying type.</typeparam>
	public abstract class SimpleBufferField<T> : BufferField
	{
		private T _value;

		public SimpleBufferField()
		{
		}

		public SimpleBufferField(T value)
		{
			_value = value;
		}

		public SimpleBufferField(BufferField prev)
			: base(prev)
		{
		}

		public SimpleBufferField(BufferField prev, T value)
			: base(prev)
		{
			_value = value;
		}


		/// <summary>
		/// Gets/sets the underlying value.
		/// </summary>
		public T Value
		{
			get
			{
				return _value;
			}
			set
			{
				if ((_value == null && value != null) ||
					(_value != null && !_value.Equals(value)))
				{
					var e =
						new BufferFieldChangingEventArgs(_value, value);
					if (OnValueChanging(e))
					{
						_value = (T)e.NewValue;
						OnValueChanged(EventArgs.Empty);
					}
				}
			}
		}
	}

	/// <summary>
	/// Represents a fixed array of fixed length fields.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	[CLSCompliant(false)]
	public abstract class ArrayBufferField<T> : SimpleBufferField<T>
	{
		private readonly ushort _maxElements;

		public ArrayBufferField(ushort maxElements)
		{
			_maxElements = maxElements;
		}

		public ArrayBufferField(ushort maxElements, T value)
			: base(value)
		{
			_maxElements = maxElements;
		}

		public ArrayBufferField(BufferField prev, ushort maxElements)
			: base(prev)
		{
			_maxElements = maxElements;
		}

		public ArrayBufferField(BufferField prev, ushort maxElements, T value)
			: base(prev, value)
		{
			_maxElements = maxElements;
		}

		public override ushort MaxElements => _maxElements;
	}

	/// <summary>
	/// Represents a variable array of fixed length fields.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	[CLSCompliant(false)]
	public abstract class VariableBufferField<T> : ArrayBufferField<T>
	{
		public VariableBufferField(ushort maxElements)
			: base(maxElements)
		{
		}

		public VariableBufferField(ushort maxElements, T value)
			: base(maxElements, value)
		{
		}

		public VariableBufferField(BufferField prev, ushort maxElements)
			: base(prev, maxElements)
		{
		}

		public VariableBufferField(BufferField prev, ushort maxElements, T value)
			: base(prev, maxElements, value)
		{
		}

		public override int FieldLength => base.FieldLength + 2;
	}

	public class BufferFieldByte : SimpleBufferField<byte>
	{
		public BufferFieldByte()
		{
		}

		public BufferFieldByte(byte value)
			: base(value)
		{
		}

		public BufferFieldByte(BufferField prev)
			: base(prev)
		{
		}

		public BufferFieldByte(BufferField prev, byte value)
			: base(prev, value)
		{
		}

		#region Public Properties
		public override int DataSize => 1;

	    #endregion

		protected override void OnRead(BufferReaderWriter streamManager)
		{
			Value = streamManager.ReadByte();
		}

		protected override void OnWrite(BufferReaderWriter streamManager)
		{
			streamManager.Write(Value);
		}
	}

	public class BufferFieldByteArrayUnbounded : SimpleBufferField<byte[]>
	{
		#region Public Constructors
		public BufferFieldByteArrayUnbounded()
		{
		}

		public BufferFieldByteArrayUnbounded(byte[] value)
			: base(value)
		{
		}

		public BufferFieldByteArrayUnbounded(BufferField prev)
			: base(prev)
		{
		}

		public BufferFieldByteArrayUnbounded(BufferField prev, byte[] value)
			: base(prev, value)
		{
		}
		#endregion

		#region Public Properties
		[CLSCompliant(false)]
		public override ushort MaxElements
		{
			get
			{
				if (Value == null)
				{
					return 0;
				}
				return (ushort)Value.Length;
			}
		}

		public override int DataSize => 1;

	    public override int FieldLength => base.FieldLength + 1;

	    #endregion

		protected override bool OnValueChanging(BufferFieldChangingEventArgs e)
		{
			var data = (byte[])e.NewValue;
			if (data != null && data.Length > 255)
			{
				throw new InvalidOperationException("Array too long (255 elem max).");
			}
			return base.OnValueChanging(e);
		}
		protected override void OnRead(BufferReaderWriter streamManager)
		{
			var length = streamManager.ReadByte();
			if (length > 0)
			{
				Value = streamManager.ReadBytes(length);
			}
			else
			{
				Value = null;
			}
		}

		protected override void OnWrite(BufferReaderWriter streamManager)
		{
			streamManager.Write((byte)MaxElements);
			if (MaxElements > 0)
			{
				streamManager.Write(Value);
			}
		}
	}

	[CLSCompliant(false)]
	public class BufferFieldUInt16 : SimpleBufferField<ushort>
	{
		#region Public Constructors
		public BufferFieldUInt16()
		{
		}

		public BufferFieldUInt16(ushort value)
			: base(value)
		{
		}

		public BufferFieldUInt16(BufferField prev)
			: base(prev)
		{
		}

		public BufferFieldUInt16(BufferField prev, ushort value)
			: base(prev, value)
		{
		}
		#endregion

		#region Public Properties
		public override int DataSize => 2;

	    #endregion

		protected override void OnRead(BufferReaderWriter streamManager)
		{
			Value = streamManager.ReadUInt16();
		}

		protected override void OnWrite(BufferReaderWriter streamManager)
		{
			streamManager.Write(Value);
		}
	}

	[CLSCompliant(false)]
	public class BufferFieldUInt32 : SimpleBufferField<uint>
	{
		public BufferFieldUInt32()
		{
		}

		public BufferFieldUInt32(uint value)
			: base(value)
		{
		}

		public BufferFieldUInt32(BufferField prev)
			: base(prev)
		{
		}

		public BufferFieldUInt32(BufferField prev, uint value)
			: base(prev, value)
		{
		}

		#region Public Properties
		public override int DataSize => 4;

	    #endregion

		protected override void OnRead(BufferReaderWriter streamManager)
		{
			Value = streamManager.ReadUInt32();
		}

		protected override void OnWrite(BufferReaderWriter streamManager)
		{
			streamManager.Write(Value);
		}
	}

	[CLSCompliant(false)]
	public class BufferFieldUInt64 : SimpleBufferField<ulong>
	{
		public BufferFieldUInt64()
		{
		}

		public BufferFieldUInt64(ulong value)
			: base(value)
		{
		}

		public BufferFieldUInt64(BufferField prev)
			: base(prev)
		{
		}

		public BufferFieldUInt64(BufferField prev, ulong value)
			: base(prev, value)
		{
		}

		#region Public Properties
		public override int DataSize => 8;

	    #endregion

		protected override void OnRead(BufferReaderWriter streamManager)
		{
			Value = streamManager.ReadUInt64();
		}

		protected override void OnWrite(BufferReaderWriter streamManager)
		{
			streamManager.Write(Value);
		}
	}

	public class BufferFieldInt16 : SimpleBufferField<short>
	{
		public BufferFieldInt16()
		{
		}

		public BufferFieldInt16(short value)
			: base(value)
		{
		}

		public BufferFieldInt16(BufferField prev)
			: base(prev)
		{
		}

		public BufferFieldInt16(BufferField prev, short value)
			: base(prev, value)
		{
		}

		#region Public Properties
		public override int DataSize => 2;

	    #endregion

		protected override void OnRead(BufferReaderWriter streamManager)
		{
			Value = streamManager.ReadInt16();
		}

		protected override void OnWrite(BufferReaderWriter streamManager)
		{
			streamManager.Write(Value);
		}
	}

	public class BufferFieldInt32 : SimpleBufferField<int>
	{
		public BufferFieldInt32()
		{
		}

		public BufferFieldInt32(int value)
			: base(value)
		{
		}

		public BufferFieldInt32(BufferField prev)
			: base(prev)
		{
		}

		public BufferFieldInt32(BufferField prev, int value)
			: base(prev, value)
		{
		}

		#region Public Properties
		public override int DataSize => 4;

	    #endregion

		protected override void OnRead(BufferReaderWriter streamManager)
		{
			Value = streamManager.ReadInt32();
		}

		protected override void OnWrite(BufferReaderWriter streamManager)
		{
			streamManager.Write(Value);
		}
	}

	public class BufferFieldInt64 : SimpleBufferField<long>
	{
		public BufferFieldInt64()
		{
		}

		public BufferFieldInt64(long value)
			: base(value)
		{
		}

		public BufferFieldInt64(BufferField prev)
			: base(prev)
		{
		}

		public BufferFieldInt64(BufferField prev, long value)
			: base(prev, value)
		{
		}

		#region Public Properties
		public override int DataSize => 8;

	    #endregion

		protected override void OnRead(BufferReaderWriter streamManager)
		{
			Value = streamManager.ReadInt64();
		}

		protected override void OnWrite(BufferReaderWriter streamManager)
		{
			streamManager.Write(Value);
		}
	}

	public class BufferFieldSingle : SimpleBufferField<float>
	{
		public BufferFieldSingle()
		{
		}

		public BufferFieldSingle(float value)
			: base(value)
		{
		}

		public BufferFieldSingle(BufferField prev)
			: base(prev)
		{
		}

		public BufferFieldSingle(BufferField prev, float value)
			: base(prev, value)
		{
		}

		public override int DataSize => 4;

	    protected override void OnRead(BufferReaderWriter streamManager)
		{
			Value = streamManager.ReadSingle();
		}

		protected override void OnWrite(BufferReaderWriter streamManager)
		{
			streamManager.Write(Value);
		}
	}

	public class BufferFieldDouble : SimpleBufferField<double>
	{
		public BufferFieldDouble()
		{
		}

		public BufferFieldDouble(double value)
			: base(value)
		{
		}

		public BufferFieldDouble(BufferField prev)
			: base(prev)
		{
		}

		public BufferFieldDouble(BufferField prev, double value)
			: base(prev, value)
		{
		}

		public override int DataSize => 8;

	    protected override void OnRead(BufferReaderWriter streamManager)
		{
			Value = streamManager.ReadDouble();
		}

		protected override void OnWrite(BufferReaderWriter streamManager)
		{
			streamManager.Write(Value);
		}
	}

	public class BufferFieldBitVector8 : BufferFieldByte
	{
		public BufferFieldBitVector8()
			: this(0)
		{
		}

		public BufferFieldBitVector8(byte value)
			: base(value)
		{
		}

		public BufferFieldBitVector8(BufferField prev)
			: this(prev, 0)
		{
		}

		public BufferFieldBitVector8(BufferField prev, byte value)
			: base(prev, value)
		{
		}

		#region Public Methods
		public bool GetBit(byte index)
		{
			if (index > 7)
			{
				throw new ArgumentOutOfRangeException(nameof(index), "Index out of range (0-7)");
			}
			var mask = (byte)(1 << index);
			return (Value & mask) != 0;
		}

		public void SetBit(byte index, bool on)
		{
			if (index > 7)
			{
				throw new ArgumentOutOfRangeException(nameof(index), "Index out of range (0-7)");
			}
			var mask = (byte)(1 << index);
			if (on)
			{
				Value |= mask;
			}
			else
			{
				Value &= (byte)(~mask);
			}
		}
		#endregion
	}

	public class BufferFieldBitVector32 : SimpleBufferField<BitVector32>
	{
		public BufferFieldBitVector32()
			: this(0)
		{
		}

		public BufferFieldBitVector32(int value)
			: base(new BitVector32(value))
		{
		}

		public BufferFieldBitVector32(BufferField prev)
			: this(prev, 0)
		{
		}

		public BufferFieldBitVector32(BufferField prev, int value)
			: base(prev, new BitVector32(value))
		{
		}

		#region Public Properties
		public override int DataSize => 4;

	    #endregion

		[CLSCompliant(false)]
		public bool GetBit(BitVector32.Section section, uint mask)
		{
			return (Value[section] & mask) != 0;
		}

		[CLSCompliant(false)]
		public void SetBit(BitVector32.Section section, uint mask, bool on)
		{
			var vector = Value;
			if (on)
			{
				vector[section] |= (ushort)mask;
			}
			else
			{
				vector[section] &= (ushort)(~mask);
			}
		}

		public void SetValue(BitVector32.Section section, int value)
		{
			var vector = Value;
			vector[section] = value;
		}

		protected override void OnRead(BufferReaderWriter streamManager)
		{
			Value = new BitVector32(streamManager.ReadInt32());
		}

		protected override void OnWrite(BufferReaderWriter streamManager)
		{
			streamManager.Write(Value.Data);
		}
	}

	[CLSCompliant(false)]
	public class BufferFieldStringFixed : ArrayBufferField<string>
	{
		private bool _useUnicode;

		public BufferFieldStringFixed(ushort maxLength)
			: this(null, maxLength, string.Empty)
		{
		}

		public BufferFieldStringFixed(BufferField prev, ushort maxLength)
			: this(prev, maxLength, string.Empty)
		{
		}

		public BufferFieldStringFixed(BufferField prev, ushort maxLength, string value)
			: base(prev, maxLength, value)
		{
		}

		#region Public Properties
		public override int DataSize => _useUnicode ? 2 : 1;

	    public bool UseUnicode
		{
			get
			{
				return _useUnicode;
			}
			set
			{
				_useUnicode = value;
			}
		}
		#endregion

		#region Protected Methods
		protected override bool OnValueChanging(BufferFieldChangingEventArgs e)
		{
			var value = (string)e.NewValue;
			if (value != null && value.Length > MaxElements)
			{
				e.NewValue = value.Substring(0, MaxElements);
			}
			else
			{
				e.NewValue = value;
			}

			return base.OnValueChanging(e);
		}

		protected override void OnRead(BufferReaderWriter streamManager)
		{
			streamManager.UseUnicode = _useUnicode;
			Value = streamManager.ReadStringExact(MaxElements);
		}

		protected override void OnWrite(BufferReaderWriter streamManager)
		{
			streamManager.UseUnicode = _useUnicode;
			streamManager.WriteStringExact(Value, MaxElements);
		}
		#endregion
	}

	[CLSCompliant(false)]
	public class BufferFieldStringVariable : VariableBufferField<string>
	{
		private bool _useUnicode;

		public BufferFieldStringVariable(ushort maxLength)
			: this(null, maxLength, string.Empty)
		{
		}

		public BufferFieldStringVariable(BufferField prev, ushort maxLength)
			: this(prev, maxLength, string.Empty)
		{
		}

		public BufferFieldStringVariable(BufferField prev, ushort maxLength, string value)
			: base(prev, maxLength, value)
		{
		}

		#region Public Properties
		public override int DataSize => _useUnicode ? 2 : 1;

	    public bool UseUnicode
		{
			get
			{
				return _useUnicode;
			}
			set
			{
				_useUnicode = value;
			}
		}

		public override int FieldLength => base.FieldLength + 2;

	    #endregion

		protected override void OnRead(BufferReaderWriter streamManager)
		{
			streamManager.UseUnicode = _useUnicode;
			Value = streamManager.ReadString();
		}

		protected override void OnWrite(BufferReaderWriter streamManager)
		{
			streamManager.UseUnicode = _useUnicode;
			streamManager.Write(Value);
		}
	}

	/// <summary>
	/// <c>BufferFieldWrapper</c> extends <see cref="T:BufferFieldWrapperBase"/>
	/// by adding public persistence operators and providing a linkage for
	/// reading from a <see cref="T:BufferBase"/> instance.
	/// </summary>
	public class BufferFieldWrapper : BufferFieldWrapperBase
	{
		#region Protected Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="BufferFieldWrapper"/> class.
		/// </summary>
		protected BufferFieldWrapper()
		{
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Reads the specified stream manager.
		/// </summary>
		/// <param name="streamManager">The stream manager.</param>
		public void Read(BufferReaderWriter streamManager)
		{
			DoRead(streamManager);
		}

		/// <summary>
		/// Writes the specified stream manager.
		/// </summary>
		/// <param name="streamManager">The stream manager.</param>
		public void Write(BufferReaderWriter streamManager)
		{
			DoWrite(streamManager);
		}
		#endregion

		#region Internal Methods
		internal void ReadFrom(Stream stream)
		{
			using (var streamManager = new BufferReaderWriter(stream))
			{
				Read(streamManager);
			}
		}
		#endregion
	}

	public class BufferFieldLogFileId : SimpleBufferField<LogFileId>
	{
		public BufferFieldLogFileId()
			: this(LogFileId.Zero)
		{
		}

        public BufferFieldLogFileId(LogFileId value)
            : base(value)
        {
        }

		public BufferFieldLogFileId(BufferField prev)
			: this(prev, LogFileId.Zero)
		{
		}

		public BufferFieldLogFileId(BufferField prev, LogFileId value)
			: base(prev, value)
		{
		}

		#region Public Properties
		public override int DataSize => 4;

	    #endregion

		protected override void OnRead(BufferReaderWriter streamManager)
		{
			Value = new LogFileId(streamManager.ReadUInt32());
		}

		protected override void OnWrite(BufferReaderWriter streamManager)
		{
			streamManager.Write(Value.FileId);
		}
	}

    public class BufferFieldObjectId : SimpleBufferField<ObjectId>
    {
        public BufferFieldObjectId()
            : this(ObjectId.Zero)
        {
        }

        public BufferFieldObjectId(ObjectId value)
            : base(value)
        {
        }

        public BufferFieldObjectId(BufferField prev)
            : this(prev, ObjectId.Zero)
        {
        }

        public BufferFieldObjectId(BufferField prev, ObjectId value)
            : base(prev, value)
        {
        }

        #region Public Properties
        public override int DataSize => 4;

        #endregion

        protected override void OnRead(BufferReaderWriter streamManager)
        {
            Value = new ObjectId(streamManager.ReadUInt32());
        }

        protected override void OnWrite(BufferReaderWriter streamManager)
        {
            streamManager.Write(Value.Value);
        }
    }

    public class BufferFieldIndexId : SimpleBufferField<IndexId>
    {
        public BufferFieldIndexId()
            : this(IndexId.Zero)
        {
        }

        public BufferFieldIndexId(IndexId value)
            : base(value)
        {
        }

        public BufferFieldIndexId(BufferField prev)
            : this(prev, IndexId.Zero)
        {
        }

        public BufferFieldIndexId(BufferField prev, IndexId value)
            : base(prev, value)
        {
        }

        #region Public Properties
        public override int DataSize => 4;
        #endregion

        protected override void OnRead(BufferReaderWriter streamManager)
        {
            Value = new IndexId(streamManager.ReadUInt32());
        }

        protected override void OnWrite(BufferReaderWriter streamManager)
        {
            streamManager.Write(Value.Value);
        }
    }

    public class BufferFieldLogicalPageId : SimpleBufferField<LogicalPageId>
    {
        public BufferFieldLogicalPageId()
            : this(LogicalPageId.Zero)
        {
        }

        public BufferFieldLogicalPageId(LogicalPageId value)
            : base(value)
        {
        }

        public BufferFieldLogicalPageId(BufferField prev)
            : this(prev, LogicalPageId.Zero)
        {
        }

        public BufferFieldLogicalPageId(BufferField prev, LogicalPageId value)
            : base(prev, value)
        {
        }

        #region Public Properties
        public override int DataSize => 8;

        #endregion

        protected override void OnRead(BufferReaderWriter streamManager)
        {
            Value = new LogicalPageId(streamManager.ReadUInt64());
        }

        protected override void OnWrite(BufferReaderWriter streamManager)
        {
            streamManager.Write(Value.Value);
        }
    }

    public class BufferFieldObjectType : SimpleBufferField<ObjectType>
    {
        public BufferFieldObjectType()
            : this(ObjectType.Unknown)
        {
        }

        public BufferFieldObjectType(ObjectType value)
            : base(value)
        {
        }

        public BufferFieldObjectType(BufferField prev)
            : this(prev, ObjectType.Unknown)
        {
        }

        public BufferFieldObjectType(BufferField prev, ObjectType value)
            : base(prev, value)
        {
        }

        #region Public Properties
        public override int DataSize => 1;

        #endregion

        protected override void OnRead(BufferReaderWriter streamManager)
        {
            Value = new ObjectType(streamManager.ReadByte());
        }

        protected override void OnWrite(BufferReaderWriter streamManager)
        {
            streamManager.Write(Value.Value);
        }
    }
}
