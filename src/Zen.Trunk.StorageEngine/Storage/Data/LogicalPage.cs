using Zen.Trunk.Storage.BufferFields;

namespace Zen.Trunk.Storage.Data
{
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
		private readonly BufferFieldLogicalPageId _prevLogicalPageId;
		private readonly BufferFieldLogicalPageId _nextLogicalPageId;
        #endregion

        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="LogicalPage"/> class.
        /// </summary>
        public LogicalPage()
		{
			_prevLogicalPageId = new BufferFieldLogicalPageId(base.LastHeaderField, LogicalPageId.Zero);
			_nextLogicalPageId = new BufferFieldLogicalPageId(_prevLogicalPageId, LogicalPageId.Zero);
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
		public LogicalPageId LogicalPageId
		{
			get
			{
				if (DataBuffer == null)
				{
					return _logicalId;
				}
				return DataBuffer.LogicalPageId;
			}
			set
			{
				if (DataBuffer != null)
				{
					CheckReadOnly();
					if (DataBuffer.LogicalPageId != value)
					{
						DataBuffer.LogicalPageId = value;
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
		public LogicalPageId PrevLogicalPageId
		{
			get => _prevLogicalPageId.Value;
		    set
			{
				CheckReadOnly();
				if (_prevLogicalPageId.Value != value)
				{
					_prevLogicalPageId.Value = value;
					SetHeaderDirty();
				}
			}
		}

		/// <summary>
		/// Gets/sets the next logical page ID.
		/// </summary>
		/// <value>Logical ID expressed as UInt64.</value>
		public LogicalPageId NextLogicalPageId
		{
			get => _nextLogicalPageId.Value;
		    set
			{
				CheckReadOnly();
				if (_nextLogicalPageId.Value != value)
				{
					_nextLogicalPageId.Value = value;
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
        protected override BufferField LastHeaderField => _nextLogicalPageId;
	    #endregion
	}
}
