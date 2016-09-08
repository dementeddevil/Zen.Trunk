namespace Zen.Trunk.Storage.Data.Index
{
	/// <summary>
	/// RootIndexInfo defines the core root index information.
	/// </summary>
	public class RootIndexInfo : BufferFieldWrapper
	{
		#region Private Fields

	    private readonly BufferFieldIndexId _indexId;
		private readonly BufferFieldObjectId _objectId;
		private readonly BufferFieldStringFixed _name;
		private readonly BufferFieldLogicalPageId _rootLogicalPageId;
		private readonly BufferFieldByte _rootIndexDepth;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="RootIndexInfo"/> class.
		/// </summary>
		public RootIndexInfo()
			: this(IndexId.Zero)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RootIndexInfo"/> class.
		/// </summary>
		/// <param name="indexId">The index id.</param>
		public RootIndexInfo(IndexId indexId)
		{
			_indexId = new BufferFieldIndexId(indexId);
			_objectId = new BufferFieldObjectId(_indexId, ObjectId.Zero);
			_name = new BufferFieldStringFixed(_objectId, 16);
			_rootLogicalPageId = new BufferFieldLogicalPageId(_name);
			_rootIndexDepth = new BufferFieldByte(_rootLogicalPageId);
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets the index id.
		/// </summary>
		/// <value>The index id.</value>
		public IndexId IndexId => _indexId.Value;

	    /// <summary>
		/// Gets or sets the object id.
		/// </summary>
		/// <value>The owner object id.</value>
		public ObjectId ObjectId
		{
			get { return _objectId.Value; }
			set { _objectId.Value = value; }
		}

		/// <summary>
		/// Gets or sets the name.
		/// </summary>
		/// <value>The name.</value>
		public string Name
		{
			get { return _name.Value; }
			set { _name.Value = value; }
		}

	    /// <summary>
		/// Gets or sets the root logical id.
		/// </summary>
		/// <value>The root logical id.</value>
		public LogicalPageId RootLogicalPageId
		{
			get { return _rootLogicalPageId.Value; }
			set { _rootLogicalPageId.Value = value; }
		}

		/// <summary>
		/// Gets or sets the root index depth.
		/// </summary>
		/// <value>The root index depth.</value>
		public byte RootIndexDepth
		{
			get { return _rootIndexDepth.Value; }
			set { _rootIndexDepth.Value = value; }
		}

		/// <summary>
		/// Gets or sets the index file group id.
		/// </summary>
		/// <value>The index file group id.</value>
		public FileGroupId IndexFileGroupId { get; set; }
		#endregion

		#region Protected Properties
		/// <summary>
		/// Gets the first buffer field object.
		/// </summary>
		/// <value>A <see cref="T:BufferField"/> object.</value>
		protected override BufferField FirstField => _indexId;

	    /// <summary>
		/// Gets the last buffer field object.
		/// </summary>
		/// <value>A <see cref="T:BufferField"/> object.</value>
		protected override BufferField LastField => _rootIndexDepth;
	    #endregion
	}
}
