namespace Zen.Trunk.Storage.Data.Table
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using Zen.Trunk.Storage.IO;

	public class RowReaderWriter
	{
		#region Private Fields
		private readonly BufferReaderWriter _bufferReaderWriter;
		private readonly IList<TableColumnInfo> _rowDef;
		private readonly object[] _rowValues;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="RowReaderWriter"/> class.
		/// </summary>
		/// <param name="stream">The stream.</param>
		/// <param name="rowDef">The row definition.</param>
		public RowReaderWriter(Stream stream, IList<TableColumnInfo> rowDef)
		{
			_bufferReaderWriter = new BufferReaderWriter(stream);
			_rowDef = rowDef;
			_rowValues = new object[_rowDef.Count];
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets or sets the column data <see cref="System.Object"/> at the 
		/// specified index.
		/// </summary>
		/// <value></value>
		public object this[int index]
		{
			get
			{
				if (index < 0 || index >= _rowValues.Length)
				{
					throw new ArgumentOutOfRangeException(
						"index", index, "index out of range.");
				}
				return _rowValues[index];
			}
			set
			{
				if (index < 0 || index >= _rowValues.Length)
				{
					throw new ArgumentOutOfRangeException(
						"index", index, "index out of range.");
				}
				_rowValues[index] = value;
			}
		}

		/// <summary>
		/// Gets the size of the row in bytes.
		/// </summary>
		/// <value>The size of the row.</value>
		public ushort RowSize
		{
			get
			{
				return (ushort)Enumerable
					.Zip<TableColumnInfo, object, int>(
						_rowDef,
						_rowValues,
						(def, val) => (int)def.GetActualDataSize(val))
					.Sum();
			}
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Reads a row from the underlying buffer reader into this instance.
		/// </summary>
		public void Read()
		{
			for (var index = 0; index < _rowDef.Count; ++index)
			{
				_rowValues[index] = _rowDef[index].ReadData(_bufferReaderWriter);
			}
		}

		/// <summary>
		/// Writes the row data in this instance to the underlying buffer writer.
		/// </summary>
		public void Write()
		{
			for (var index = 0; index < _rowDef.Count; ++index)
			{
				_rowDef[index].WriteData(_bufferReaderWriter, _rowValues[index]);
			}
		}
		#endregion
	}
}
