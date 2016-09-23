using System;

namespace Zen.Tasks.Wix.InstanceService.InstallerDatabase
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public class MsiRecord : IDisposable
	{
		#region Public Objects
		/// <summary>
		/// MsiField represents an MSI field.
		/// </summary>
		public class MsiField
		{
			#region Private Fields
			private MsiRecord _owner;
			private int _fieldIndex;
			#endregion

			#region Internal Constructors
			internal MsiField(MsiRecord record, int fieldIndex)
			{
				_owner = record;
				_fieldIndex = fieldIndex;
			}
			#endregion

			#region Public Properties
			/// <summary>
			/// Gets a value indicating whether this instance is null.
			/// </summary>
			/// <value><c>true</c> if this instance is null; otherwise, <c>false</c>.</value>
			public bool IsNull => _owner.RecordIsNull(_fieldIndex);
		    #endregion

			#region Public Methods
			/// <summary>
			/// Gets the int.
			/// </summary>
			/// <returns></returns>
			public int GetInt()
			{
				return _owner.RecordGetInteger(_fieldIndex);
			}

			/// <summary>
			/// Sets the int.
			/// </summary>
			/// <param name="value">The value.</param>
			public void SetInt(int value)
			{
				_owner.RecordSetInteger(_fieldIndex, value);
			}

			/// <summary>
			/// Gets the string.
			/// </summary>
			/// <returns></returns>
			public string GetString()
			{
				return _owner.RecordGetString(_fieldIndex);
			}

			/// <summary>
			/// Sets the string.
			/// </summary>
			/// <param name="value">The value.</param>
			public void SetString(string value)
			{
				_owner.RecordSetString(_fieldIndex, value);
			}

			/// <summary>
			/// Reads the stream.
			/// </summary>
			/// <param name="bufferSize">Size of the buffer.</param>
			/// <returns></returns>
			public byte[] ReadStream(int bufferSize)
			{
				return _owner.RecordReadStream(_fieldIndex, bufferSize);
			}

			/// <summary>
			/// Reads the stream direct.
			/// </summary>
			/// <param name="buffer">The buffer.</param>
			public void ReadStreamDirect(byte[] buffer)
			{
				_owner.RecordReadStreamDirect(_fieldIndex, buffer, buffer.Length);
			}

			/// <summary>
			/// Sets the stream.
			/// </summary>
			/// <param name="filePath">The file path.</param>
			public void SetStream(string filePath)
			{
				_owner.RecordSetStream(_fieldIndex, filePath);
			}
			#endregion

			#region Protected Methods
			#endregion

			#region Private Methods
			#endregion
		}
		#endregion

		#region Private Fields
		private SafeMsiHandle _handle;
		private MsiField[] _fields;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="MsiRecord"/> class.
		/// </summary>
		/// <param name="parameterCount">The parameter count.</param>
		public MsiRecord(int parameterCount)
		{
			_handle = Win32.MsiCreateRecord(parameterCount);
			_fields = new MsiField[parameterCount + 1];
		}
		#endregion

		#region Internal Constructors
		internal MsiRecord(SafeMsiHandle record)
		{
			_handle = record;
			_fields = new MsiField[FieldCount + 1];
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets the <see cref="MsiRecord.MsiField"/> at the specified index.
		/// </summary>
		/// <value></value>
		public MsiField this[int index]
		{
			get
			{
				if (_fields == null)
				{
					throw new ObjectDisposedException(GetType().FullName);
				}
				if (index < 0 || index > _fields.Length)
				{
					throw new ArgumentOutOfRangeException(nameof(index));
				}

			    return _fields[index] ?? (_fields[index] = new MsiField(this, index));
			}
		}

		/// <summary>
		/// Gets the field count.
		/// </summary>
		/// <value>The field count.</value>
		public int FieldCount
		{
			get
			{
				CheckHandleValid();
				return Win32.MsiRecordGetFieldCount(_handle);
			}
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Clears this instance.
		/// </summary>
		public void Clear()
		{
			CheckHandleValid();
			Win32.MsiRecordClearData(_handle);
		}
		#endregion

		#region Protected Properties
		/// <summary>
		/// Gets the handle.
		/// </summary>
		/// <value>The handle.</value>
		protected internal SafeMsiHandle Handle => _handle;
	    #endregion

		#region Protected Methods
		/// <summary>
		/// Records the is null.
		/// </summary>
		/// <param name="fieldIndex">Index of the field.</param>
		/// <returns></returns>
		protected virtual bool RecordIsNull(int fieldIndex)
		{
			CheckHandleValid();
			return Win32.MsiRecordIsNull(_handle, fieldIndex);
		}

		/// <summary>
		/// Records the get integer.
		/// </summary>
		/// <param name="fieldIndex">Index of the field.</param>
		/// <returns></returns>
		protected virtual int RecordGetInteger(int fieldIndex)
		{
			CheckHandleValid();
			return Win32.MsiRecordGetInteger(_handle, fieldIndex);
		}

		/// <summary>
		/// Records the set integer.
		/// </summary>
		/// <param name="fieldIndex">Index of the field.</param>
		/// <param name="value">The value.</param>
		protected virtual void RecordSetInteger(int fieldIndex, int value)
		{
			CheckHandleValid();
			Win32.MsiRecordSetInteger(_handle, fieldIndex, value);
		}

		/// <summary>
		/// Records the get string.
		/// </summary>
		/// <param name="fieldIndex">Index of the field.</param>
		/// <returns></returns>
		protected virtual string RecordGetString(int fieldIndex)
		{
			CheckHandleValid();
			return Win32.MsiRecordGetString(_handle, fieldIndex);
		}

		/// <summary>
		/// Records the set string.
		/// </summary>
		/// <param name="fieldIndex">Index of the field.</param>
		/// <param name="value">The value.</param>
		protected virtual void RecordSetString(int fieldIndex, string value)
		{
			CheckHandleValid();
			Win32.MsiRecordSetString(_handle, fieldIndex, value);
		}

		/// <summary>
		/// Records the read stream.
		/// </summary>
		/// <param name="fieldIndex">Index of the field.</param>
		/// <param name="bufferSize">Size of the buffer.</param>
		/// <returns></returns>
		protected virtual byte[] RecordReadStream(int fieldIndex, int bufferSize)
		{
			CheckHandleValid();
			var buffer = new byte[bufferSize];
			Win32.MsiRecordReadStream(_handle, fieldIndex, buffer, bufferSize);
			return buffer;
		}

		/// <summary>
		/// Records the read stream direct.
		/// </summary>
		/// <param name="fieldIndex">Index of the field.</param>
		/// <param name="buffer">The buffer.</param>
		/// <param name="bufferSize">Size of the buffer.</param>
		protected virtual void RecordReadStreamDirect(int fieldIndex, byte[] buffer, int bufferSize)
		{
			CheckHandleValid();
			Win32.MsiRecordReadStream(_handle, fieldIndex, buffer, bufferSize);
		}

		/// <summary>
		/// Records the set stream.
		/// </summary>
		/// <param name="fieldIndex">Index of the field.</param>
		/// <param name="filePath">The file path.</param>
		protected virtual void RecordSetStream(int fieldIndex, string filePath)
		{
			CheckHandleValid();
			Win32.MsiRecordSetStream(_handle, fieldIndex, filePath);
		}
		#endregion

		#region Internal Methods
		internal bool FetchInternal(SafeMsiHandle view)
		{
			return Win32.MsiViewFetch(view, ref _handle);
		}
		#endregion

		#region Private Methods
		private void CheckHandleValid()
		{
			if (_handle == null || _handle.IsClosed || _handle.IsInvalid)
			{
				throw new ObjectDisposedException(GetType().FullName);
			}
		}
		#endregion

		#region IDisposable Members
		/// <summary>
		/// Releases unmanaged resources and performs other cleanup operations before the
		/// <see cref="MsiRecord"/> is reclaimed by garbage collection.
		/// </summary>
		~MsiRecord()
		{
			Dispose(false);
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, 
		/// releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Releases unmanaged and - optionally - managed resources
		/// </summary>
		/// <param name="disposing">
		/// <c>true</c> to release both managed and unmanaged resources; 
		/// <c>false</c> to release only unmanaged resources.
		/// </param>
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
			    _handle?.Dispose();
			}

			_handle = null;
			_fields = null;
		}
		#endregion
	}
}
