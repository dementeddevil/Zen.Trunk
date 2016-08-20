namespace Zen.Trunk.Storage.Data.Index
{
	/// <summary>
	/// RootIndexInfo defines the core root index information.
	/// </summary>
	public class RootIndexInfo : BufferFieldWrapper
	{
		#region Private Fields
		private FileGroupId _indexFileGroupId;	// not serialized
		private readonly BufferFieldUInt32 _ObjectId;
		private readonly BufferFieldUInt32 _ownerObjectId;
		private readonly BufferFieldStringFixed _name;
		private readonly BufferFieldUInt64 _rootLogicalId;
		private readonly BufferFieldByte _rootIndexDepth;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="RootIndexInfo"/> class.
		/// </summary>
		public RootIndexInfo()
			: this(0)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RootIndexInfo"/> class.
		/// </summary>
		/// <param name="ObjectId">The index id.</param>
		public RootIndexInfo(uint ObjectId)
		{
			_ObjectId = new BufferFieldUInt32(ObjectId);
			_ownerObjectId = new BufferFieldUInt32(_ObjectId);
			_name = new BufferFieldStringFixed(_ownerObjectId, 16);
			_rootLogicalId = new BufferFieldUInt64(_name);
			_rootIndexDepth = new BufferFieldByte(_rootLogicalId);
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets the index id.
		/// </summary>
		/// <value>The index id.</value>
		public ObjectId ObjectId => new ObjectId(_ObjectId.Value);

	    /// <summary>
		/// Gets or sets the owner object id.
		/// </summary>
		/// <value>The owner object id.</value>
		public ObjectId OwnerObjectId
		{
			get
			{
				return new ObjectId(_ownerObjectId.Value);
			}
			set
			{
				_ownerObjectId.Value = value.Value;
			}
		}

		/// <summary>
		/// Gets or sets the name.
		/// </summary>
		/// <value>The name.</value>
		public string Name
		{
			get
			{
				return _name.Value;
			}
			set
			{
				_name.Value = value;
			}
		}

		/// <summary>
		/// Gets or sets the index file group id.
		/// </summary>
		/// <value>The index file group id.</value>
		public FileGroupId IndexFileGroupId
		{
			get
			{
				return _indexFileGroupId;
			}
			set
			{
				_indexFileGroupId = value;
			}
		}

		/// <summary>
		/// Gets or sets the root logical id.
		/// </summary>
		/// <value>The root logical id.</value>
		public LogicalPageId RootLogicalId
		{
			get
			{
				return new LogicalPageId(_rootLogicalId.Value);
			}
			set
			{
				_rootLogicalId.Value = value.Value;
			}
		}

		/// <summary>
		/// Gets or sets the root index depth.
		/// </summary>
		/// <value>The root index depth.</value>
		public byte RootIndexDepth
		{
			get
			{
				return _rootIndexDepth.Value;
			}
			set
			{
				_rootIndexDepth.Value = value;
			}
		}
		#endregion

		#region Protected Properties
		/// <summary>
		/// Gets the first buffer field object.
		/// </summary>
		/// <value>A <see cref="T:BufferField"/> object.</value>
		protected override BufferField FirstField => _ObjectId;

	    /// <summary>
		/// Gets the last buffer field object.
		/// </summary>
		/// <value>A <see cref="T:BufferField"/> object.</value>
		protected override BufferField LastField => _rootIndexDepth;

	    #endregion
	}
}
