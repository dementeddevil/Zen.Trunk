namespace Zen.Trunk.Storage.Data
{
	using System;
	using System.Threading;

	/// <summary>
	/// <c>LogicalPage</c> extends <see cref="DataPage"/> by exposing a logical
	/// ID for a given page and providing pointers to previous and next pages.
	/// </summary>
	/// <remarks>
	/// Logical pages enable an abstraction of page ID and physical location
	/// that allows the database engine to reorganise the layout of pages as
	/// appropriate. The changes of layout would involve writing the underlying
	/// buffer to a different location and updating the appropriate distribution 
	/// page or pages to reflect the change.
	/// </remarks>
	public class LogicalPage : DataPage
	{
		#region Private Fields
		private LogicalPageId _logicalId;
		private readonly BufferFieldUInt64 _prevLogicalId;
		private readonly BufferFieldUInt64 _nextLogicalId;
		#endregion

		#region Public Constructors
		public LogicalPage()
		{
			_prevLogicalId = new BufferFieldUInt64(base.LastHeaderField, 0);
			_nextLogicalId = new BufferFieldUInt64(_prevLogicalId, 0);
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets the minimum number of bytes required for the header block.
		/// </summary>
		public override uint MinHeaderSize => base.MinHeaderSize + 16;

	    /// <summary>
		/// Gets/sets the logical page ID.
		/// </summary>
		/// <value>Logical ID expressed as UInt32.</value>
		public LogicalPageId LogicalId
		{
			get
			{
				if (DataBuffer == null)
				{
					return _logicalId;
				}
				return DataBuffer.LogicalId;
			}
			set
			{
				if (DataBuffer != null)
				{
					CheckReadOnly();
					if (DataBuffer.LogicalId != value)
					{
						DataBuffer.LogicalId = value;
					}
				}
				else
				{
					_logicalId = value;
				}
			}
		}

		/// <summary>
		/// Gets/sets the previous logical page ID.
		/// </summary>
		/// <value>Logical ID expressed as UInt64.</value>
		public LogicalPageId PrevLogicalId
		{
			get
			{
				return new LogicalPageId(_prevLogicalId.Value);
			}
			set
			{
				CheckReadOnly();
				if (_prevLogicalId.Value != value.Value)
				{
					_prevLogicalId.Value = value.Value;
					SetHeaderDirty();
				}
			}
		}

		/// <summary>
		/// Gets/sets the next logical page ID.
		/// </summary>
		/// <value>Logical ID expressed as UInt64.</value>
		public LogicalPageId NextLogicalId
		{
			get
			{
				return new LogicalPageId(_nextLogicalId.Value);
			}
			set
			{
				CheckReadOnly();
				if (_nextLogicalId.Value != value.Value)
				{
					_nextLogicalId.Value = value.Value;
					SetHeaderDirty();
				}
			}
		}
		#endregion

		#region Protected Properties
		protected override BufferField LastHeaderField => _nextLogicalId;

	    #endregion
	}
}
