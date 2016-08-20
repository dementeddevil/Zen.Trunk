namespace Zen.Trunk.Storage.Data.Index
{
	using System;

	/// <summary>
	/// <c>IndexPage</c> is a base class for pages needing indexing capability.
	/// </summary>
	/// <remarks>
	/// Index page handles a binary tree spread across multiple logical pages. 
	/// </remarks>
	public abstract class IndexPage : ObjectDataPage
	{
		#region Private Fields
		private BufferFieldUInt64 _leftLogicalPageId;
		private BufferFieldUInt64 _rightLogicalPageId;
		private BufferFieldUInt64 _parentLogicalPageId;
		private BufferFieldByte _depth;
		#endregion

		#region Protected Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="IndexPage"/> class.
		/// </summary>
		protected IndexPage()
		{
			_leftLogicalPageId = new BufferFieldUInt64(base.LastHeaderField);
			_rightLogicalPageId = new BufferFieldUInt64(_leftLogicalPageId);
			_parentLogicalPageId = new BufferFieldUInt64(_rightLogicalPageId);
			_depth = new BufferFieldByte(_parentLogicalPageId);

			this.PageType = PageType.Index;
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets the index manager.
		/// </summary>
		/// <value>The index manager.</value>
		public abstract IndexManager IndexManager
		{
			get;
		}

		/// <summary>
		/// Gets/sets the page type.
		/// </summary>
		/// <value></value>
		public override PageType PageType
		{
			set
			{
				// Check 
				if (value != PageType.New && value != PageType.Index)
				{
					throw new ArgumentException("Invalid page type for index.");
				}
				base.PageType = value;
			}
		}

		/// <summary>
		/// Gets the max index entries.
		/// </summary>
		/// <value>The max index entries.</value>
		public abstract ushort MaxIndexEntries
		{
			get;
		}

		/// <summary>
		/// Gets or sets the left logical page id.
		/// </summary>
		/// <value>The left logical page id.</value>
		public ulong LeftLogicalPageId
		{
			get
			{
				return _leftLogicalPageId.Value;
			}
			set
			{
				CheckReadOnly();
				if (_leftLogicalPageId.Value != value)
				{
					_leftLogicalPageId.Value = value;
					SetHeaderDirty();
				}
			}
		}

		/// <summary>
		/// Gets or sets the right logical page id.
		/// </summary>
		/// <value>The right logical page id.</value>
		public ulong RightLogicalPageId
		{
			get
			{
				return _rightLogicalPageId.Value;
			}
			set
			{
				CheckReadOnly();
				if (_rightLogicalPageId.Value != value)
				{
					_rightLogicalPageId.Value = value;
					SetHeaderDirty();
				}
			}
		}

		/// <summary>
		/// Gets or sets the parent logical page id.
		/// </summary>
		/// <value>The parent logical page id.</value>
		public ulong ParentLogicalPageId
		{
			get
			{
				return _parentLogicalPageId.Value;
			}
			set
			{
				CheckReadOnly();
				if (_parentLogicalPageId.Value != value)
				{
					_parentLogicalPageId.Value = value;
					SetHeaderDirty();
				}
			}
		}

		/// <summary>
		/// Gets or sets the index page depth.
		/// </summary>
		/// <value>The depth.</value>
		/// <remarks>
		/// Index pages with a depth of zero are index leaf index pages.
		/// Everything else is an intermediate index index page with the 
		/// exception of the root index page (which also has the highest depth
		/// value)
		/// Once an index page is created, it's depth value will never change.
		/// </remarks>
		public byte Depth
		{
			get
			{
				return _depth.Value;
			}
			set
			{
				CheckReadOnly();
				if (_depth.Value != value)
				{
					_depth.Value = value;
					SetHeaderDirty();
				}
			}
		}
		#endregion

		#region Protected Properties
		/// <summary>
		/// Gets the last header field.
		/// </summary>
		/// <value>The last header field.</value>
		protected override BufferField LastHeaderField
		{
			get
			{
				return _depth;
			}
		}
		#endregion

		#region Public Methods
		#endregion
	}
}
