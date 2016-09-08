using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Zen.Trunk.Storage.IO;

namespace Zen.Trunk.Storage.Data.Table
{

	public enum RowConstraintType
	{
		Default,
		Check,
		Timestamp,
		Identity
	}

	public class RowConstraint : BufferFieldWrapper
	{
		private readonly BufferFieldUInt16 _columnId;
		private readonly BufferFieldByte _constraintType;
		private readonly BufferFieldStringFixed _constraintData;

		public RowConstraint()
		{
			_columnId = new BufferFieldUInt16();
			_constraintType = new BufferFieldByte(_columnId);
			_constraintData = new BufferFieldStringFixed(_constraintType, 64);
		}

		public RowConstraint(ushort columnId, RowConstraintType constraintType)
		{
			ColumnId = columnId;
			ConstraintType = constraintType;
		}

		public RowConstraint(ushort columnId, RowConstraintType constraintType,
			string constraintData)
		{
			ColumnId = columnId;
			ConstraintType = constraintType;
			ConstraintData = constraintData;
		}

		public ushort ColumnId
		{
			get
			{
				return _columnId.Value;
			}
			set
			{
				_columnId.Value = value;
			}
		}

		public RowConstraintType ConstraintType
		{
			get
			{
				return (RowConstraintType)_constraintType.Value;
			}
			set
			{
				_constraintType.Value = (byte)value;
			}
		}

		public string ConstraintData
		{
			get
			{
				return _constraintData.Value;
			}
			set
			{
				_constraintData.Value = value;
			}
		}

		protected override BufferField FirstField => _columnId;

	    protected override BufferField LastField => _constraintData;
	}

	internal abstract class IRowConstraintExecute
	{
		public abstract RowConstraintType ConstraintType
		{
			get;
		}

		public abstract void ValidateRow(RowConstraint constraint, TableColumnInfo[] columnDef,
			object[] rowData);
	}

	public class RowInfo
	{
		public ushort Offset;		// Stored
		public ushort Length;		// Calculated
	}

	/// <summary>
	/// The table schema page object encapsulates a table schema definition.
	/// </summary>
	public class TableSchemaPage : ObjectSchemaPage
	{
		#region Private Fields
		// Header fields
		private readonly BufferFieldByte _columnCount;
		private readonly BufferFieldByte _constraintCount;
		private readonly BufferFieldByte _indexCount;

		// Data fields
		private PageItemCollection<TableColumnInfo> _columns;
		private PageItemCollection<RowConstraint> _constraints;
		private PageItemCollection<RootTableIndexInfo> _indices;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="TableSchemaPage"/> class.
		/// </summary>
		public TableSchemaPage()
		{
			_columnCount = new BufferFieldByte(base.LastHeaderField);
			_constraintCount = new BufferFieldByte(_columnCount);
			_indexCount = new BufferFieldByte(_constraintCount);
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets/sets the page type.
		/// </summary>
		/// <value></value>
		public override PageType PageType
		{
			set
			{
				if (value != PageType.New &&
					value != PageType.Table)
				{
					throw new ArgumentException("PageType must be New or Table.");
				}
				base.PageType = value;
			}
		}

		/// <summary>
		/// Gets the minimum number of bytes required for the header block.
		/// </summary>
		public override uint MinHeaderSize => base.MinHeaderSize + 3;

	    /// <summary>
		/// Gets the column collection for this page.
		/// </summary>
		public IList<TableColumnInfo> Columns
		{
			get
			{
				if (_columns == null)
				{
					_columns = new PageItemCollection<TableColumnInfo>(this);
				}
				return _columns;
			}
		}

		/// <summary>
		/// Gets the constraint collection for this page.
		/// </summary>
		public IList<RowConstraint> Constraints
		{
			get
			{
				if (_constraints == null)
				{
					_constraints = new PageItemCollection<RowConstraint>(this);
				}
				return _constraints;
			}
		}

		/// <summary>
		/// Gets the root index collection for this page.
		/// </summary>
		public IList<RootTableIndexInfo> Indices
		{
			get
			{
				if (_indices == null)
				{
					_indices = new PageItemCollection<RootTableIndexInfo>(this);
				}
				return _indices;
			}
		}

		/// <summary>
		/// Gets the minimum row size
		/// </summary>
		public ushort MinRowSize
		{
			get
			{
				ushort keySize = 0;
				foreach (var column in Columns)
				{
					// Min length for variable length columns is two bytes
					var columnLength = column.Length;
					if (column.IsVariableLength)
					{
						columnLength = 2;
					}

					// Update total key size
					keySize += columnLength;
				}
				return keySize;
			}
		}

		/// <summary>
		/// Gets the maximum row size
		/// </summary>
		public ushort MaxRowSize
		{
			get
			{
				ushort keySize = 0;
				foreach (var column in Columns)
				{
					// Max length for variable length columns is plus two bytes
					var columnLength = column.Length;
					if (column.IsVariableLength)
					{
						columnLength += 2;
					}

					// Update total key size
					keySize += columnLength;
				}
				return keySize;
			}
		}
		#endregion

		#region Protected Properties
		/// <summary>
		/// Gets the last header field.
		/// </summary>
		/// <value>
		/// The last header field.
		/// </value>
		protected override BufferField LastHeaderField => _indexCount;
	    #endregion

		#region Public Methods
		/// <summary>
		/// Gets the index key size based on the specified columns.
		/// </summary>
		/// <param name="columnIndices"></param>
		/// <returns></returns>
		public ushort GetKeySize(byte[] columnIndices)
		{
			ushort keySize = 0;
			foreach (var index in columnIndices)
			{
				// Restrict variable length columns to first 10 bytes
				var columnLength = Columns[index].Length;
				if (Columns[index].IsVariableLength && columnLength > 10)
				{
					columnLength = 10;
				}

				// Update total key size
				keySize += columnLength;
			}
			return keySize;
		}
		#endregion

		#region Protected Methods
		/// <summary>
		/// Overridden. Initialises the page instance.
		/// </summary>
		/// <param name="e"></param>
		/// <remarks>
		/// Sets the page type to "Table".
		/// </remarks>
		protected override Task OnInitAsync(EventArgs e)
		{
			PageType = PageType.Table;
			return base.OnInitAsync(e);
		}

        /// <summary>
        /// Writes the page header block to the specified buffer writer.
        /// </summary>
        /// <param name="streamManager">The stream manager.</param>
        protected override void WriteHeader(BufferReaderWriter streamManager)
		{
			_columnCount.Value = (byte)(_columns != null ? _columns.Count : 0);
			_constraintCount.Value = (byte)(_constraints != null ? _constraints.Count : 0);
			_indexCount.Value = (byte)(_indices != null ? _indices.Count : 0);
			base.WriteHeader(streamManager);
		}

        /// <summary>
        /// Reads the page data block from the specified buffer reader.
        /// </summary>
        /// <param name="streamManager">The stream manager.</param>
        protected override void ReadData(BufferReaderWriter streamManager)
		{
			_columns.Clear();
			for (byte index = 0; index < _columnCount.Value; ++index)
			{
				var column = new TableColumnInfo();
				column.Read(streamManager);
				_columns.Add(column);
			}
			_constraints.Clear();
			for (byte index = 0; index < _constraintCount.Value; ++index)
			{
				var constraint = new RowConstraint();
				constraint.Read(streamManager);
				_constraints.Add(constraint);
			}
			_indices.Clear();
			for (byte index = 0; index < _indexCount.Value; ++index)
			{
				var rootIndex = new RootTableIndexInfo();
				rootIndex.Read(streamManager);
				_indices.Add(rootIndex);
			}
		}

        /// <summary>
        /// Writes the page data block to the specified buffer writer.
        /// </summary>
        /// <param name="streamManager">The stream manager.</param>
        protected override void WriteData(BufferReaderWriter streamManager)
		{
			if (_columns != null)
			{
				foreach (var column in _columns)
				{
					column.Write(streamManager);
				}
			}
			if (_constraints != null)
			{
				foreach (var constraint in _constraints)
				{
					constraint.Write(streamManager);
				}
			}
			if (_indices != null)
			{
				foreach (var index in _indices)
				{
					index.Write(streamManager);
				}
			}
		}
		#endregion
	}

	/// <summary>
	/// TableSchemaRootPage extends <see cref="TableSchemaPage"/> for holding
	/// extra information needed for recording table extends.
	/// </summary>
	/// <remarks>
	/// The table schema root page object maintains two additional properties;
	/// 1. The first data page
	/// 2. The last data page
	/// </remarks>
	public class TableSchemaRootPage : TableSchemaPage
	{
		#region Private Fields
		private readonly BufferFieldUInt64 _dataFirstLogicalPageId;
		private readonly BufferFieldUInt64 _dataLastLogicalPageId;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="TableSchemaRootPage"/> class.
		/// </summary>
		public TableSchemaRootPage()
		{
			_dataFirstLogicalPageId = new BufferFieldUInt64(base.LastHeaderField);
			_dataLastLogicalPageId = new BufferFieldUInt64(_dataFirstLogicalPageId);
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets the minimum size of the header.
		/// </summary>
		/// <value>
		/// The minimum size of the header.
		/// </value>
		public override uint MinHeaderSize => base.MinHeaderSize + 16;

	    /// <summary>
		/// Gets or sets the logical page identifier of the first data page for the table.
		/// </summary>
		/// <value>
		/// The logical page identifier.
		/// </value>
		public LogicalPageId DataFirstLogicalPageId
		{
			get
			{
				return new LogicalPageId(_dataFirstLogicalPageId.Value);
			}
			set
			{
				if (_dataFirstLogicalPageId.Value != value.Value)
				{
					_dataFirstLogicalPageId.Value = value.Value;
					SetHeaderDirty();
				}
			}
		}

		/// <summary>
		/// Gets or sets the logical page identifier of the last data page for the table.
		/// </summary>
		/// <value>
		/// The logical page identifier.
		/// </value>
		public LogicalPageId DataLastLogicalPageId
		{
			get
			{
				return new LogicalPageId(_dataLastLogicalPageId.Value);
			}
			set
			{
				if (_dataLastLogicalPageId.Value != value.Value)
				{
					_dataLastLogicalPageId.Value = value.Value;
					SetHeaderDirty();
				}
			}
		}
		#endregion

		#region Protected Properties
		/// <summary>
		/// Gets the last header field.
		/// </summary>
		/// <value>
		/// The last header field.
		/// </value>
		protected override BufferField LastHeaderField => _dataLastLogicalPageId;

	    #endregion
	}
}
