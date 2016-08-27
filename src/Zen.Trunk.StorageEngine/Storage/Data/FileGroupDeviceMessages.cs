namespace Zen.Trunk.Storage.Data
{
	using System;
	using System.Collections.Generic;
	using Zen.Trunk.Storage;
	using Zen.Trunk.Storage.Data.Table;

	public class AddDataDeviceParameters : AddDeviceParameters
	{
        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="AddDataDeviceParameters" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="pathName">Name of the path.</param>
        /// <param name="deviceId">The device id.</param>
        /// <param name="createPageCount">The create page count.</param>
        /// <param name="updateRootPage">
        /// Set to <c>true</c> to update the root page; otherwise <c>false</c>.
        /// </param>
        public AddDataDeviceParameters(
			string name,
			string pathName,
            DeviceId deviceId,
			uint createPageCount = 0,
			bool updateRootPage = false)
			: base(name, pathName, deviceId, createPageCount)
		{
			UpdateRootPage = updateRootPage;
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets or sets a value indicating whether [update root page].
		/// </summary>
		/// <value><c>true</c> if [update root page]; otherwise, <c>false</c>.</value>
		public bool UpdateRootPage
		{
			get;
			private set;
		}
		#endregion
	}

	public class RemoveDataDeviceParameters : RemoveDeviceParameters
	{
		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="RemoveDataDeviceParameters"/> class.
		/// </summary>
		/// <param name="deviceId">The device unique identifier.</param>
		/// <param name="updateRootPage">if set to <c>true</c> [update root page].</param>
		public RemoveDataDeviceParameters(DeviceId deviceId, bool updateRootPage = false)
			: base(deviceId)
		{
			UpdateRootPage = updateRootPage;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RemoveDataDeviceParameters"/> class.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="updateRootPage">if set to <c>true</c> [update root page].</param>
		public RemoveDataDeviceParameters(string name, bool updateRootPage = false)
			: base(name)
		{
			UpdateRootPage = updateRootPage;
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets or sets a value indicating whether [update root page].
		/// </summary>
		/// <value>
		///   <c>true</c> if [update root page]; otherwise, <c>false</c>.
		/// </value>
		public bool UpdateRootPage
		{
			get;
			private set;
		}
		#endregion
	}

	public class InitDataPageParameters
	{
		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="InitDataPageParameters"/> class.
		/// </summary>
		/// <param name="page">The page.</param>
		/// <param name="assignVirtualId">if set to <c>true</c> [assign virtual unique identifier].</param>
		/// <param name="assignLogicalId">if set to <c>true</c> [assign logical unique identifier].</param>
		/// <param name="assignAutomaticLogicalId">if set to <c>true</c> [assign automatic logical unique identifier].</param>
		/// <param name="isNewObject">if set to <c>true</c> [is new object].</param>
		public InitDataPageParameters(DataPage page, bool assignVirtualId = false, bool assignLogicalId = false, bool assignAutomaticLogicalId = false, bool isNewObject = false)
		{
			Page = page;
			AssignVirtualId = assignVirtualId;
			AssignLogicalId = assignLogicalId;
			AssignAutomaticLogicalId = assignAutomaticLogicalId;
			IsNewObject = isNewObject;
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets the page.
		/// </summary>
		/// <value>
		/// The page.
		/// </value>
		public DataPage Page
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets a value indicating whether [assign virtual unique identifier].
		/// </summary>
		/// <value>
		/// <c>true</c> if [assign virtual unique identifier]; otherwise, <c>false</c>.
		/// </value>
		public bool AssignVirtualId
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets a value indicating whether [assign logical unique identifier].
		/// </summary>
		/// <value>
		/// <c>true</c> if [assign logical unique identifier]; otherwise, <c>false</c>.
		/// </value>
		public bool AssignLogicalId
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets a value indicating whether [assign automatic logical unique identifier].
		/// </summary>
		/// <value>
		/// <c>true</c> if [assign automatic logical unique identifier]; otherwise, <c>false</c>.
		/// </value>
		public bool AssignAutomaticLogicalId
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets or sets a value indicating whether this instance is new object.
		/// </summary>
		/// <value>
		/// <c>true</c> if this instance is new object; otherwise, <c>false</c>.
		/// </value>
		///	<remarks>
		///	The term "New Object" refers to the Object ID. If the object is new
		///	then the first set of pages for the object are placed in a mixed
		///	extent.
		///	</remarks>
		public bool IsNewObject
		{
			get;
			private set;
		}
		#endregion
	}

	public class LoadDataPageParameters
	{
		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="LoadDataPageParameters"/> class.
		/// </summary>
		/// <param name="page">The page.</param>
		/// <param name="virtualPageIdValid">if set to <c>true</c> [virtual page unique identifier valid].</param>
		/// <param name="logicalPageIdValid">if set to <c>true</c> [logical page unique identifier valid].</param>
		/// <param name="assignLogicalId">if set to <c>true</c> [assign logical unique identifier].</param>
		public LoadDataPageParameters(DataPage page, bool virtualPageIdValid = false, bool logicalPageIdValid = false, bool assignLogicalId = false)
		{
			Page = page;
			VirtualPageIdValid = virtualPageIdValid;
			LogicalPageIdValid = logicalPageIdValid;
			AssignLogicalId = assignLogicalId;
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets the page.
		/// </summary>
		/// <value>
		/// The page.
		/// </value>
		public DataPage Page
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets a value indicating whether [virtual page unique identifier valid].
		/// </summary>
		/// <value>
		/// <c>true</c> if [virtual page unique identifier valid]; otherwise, <c>false</c>.
		/// </value>
		public bool VirtualPageIdValid
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets a value indicating whether [logical page unique identifier valid].
		/// </summary>
		/// <value>
		/// <c>true</c> if [logical page unique identifier valid]; otherwise, <c>false</c>.
		/// </value>
		public bool LogicalPageIdValid
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets a value indicating whether [assign logical unique identifier].
		/// </summary>
		/// <value>
		/// <c>true</c> if [assign logical unique identifier]; otherwise, <c>false</c>.
		/// </value>
		public bool AssignLogicalId
		{
			get;
			private set;
		}
		#endregion
	}

	public class AllocateDataPageParameters
	{
		public AllocateDataPageParameters(LogicalPageId logicalId, ObjectId objectId, ObjectType objectType, bool mixedExtent)
		{
			LogicalId = logicalId;
			ObjectId = objectId;
			ObjectType = objectType;
			MixedExtent = mixedExtent;
		}

		public LogicalPageId LogicalId
		{
			get;
			private set;
		}

		public ObjectId ObjectId
		{
			get;
			private set;
		}

		public ObjectType ObjectType
		{
			get;
			private set;
		}

		public bool MixedExtent
		{
			get;
			private set;
		}
	}

	public class AddTableParameters
	{
		private readonly List<TableColumnInfo> _columns = new List<TableColumnInfo>();

		public AddTableParameters()
		{
		}

		public AddTableParameters(string tableName, params TableColumnInfo[] columns)
		{
			if (string.IsNullOrWhiteSpace(tableName))
			{
				throw new ArgumentException("Table name is required.");
			}
			if (columns == null || columns.Length == 0)
			{
				throw new ArgumentException("Table must have at least one column definition.");
			}

			TableName = tableName;
			if (columns != null && columns.Length > 0)
			{
				_columns.AddRange(columns);
			}
		}

		public string TableName
		{
			get;
			set;
		}

		public IList<TableColumnInfo> Columns => _columns;
	}

	public class AddTableIndexParameters
	{
		private readonly List<Tuple<string, TableIndexSortDirection>> _columns = new List<Tuple<string, TableIndexSortDirection>>();

		public AddTableIndexParameters()
		{
		}

		public AddTableIndexParameters(string name, TableIndexSubType indexSubType, ObjectId ownerObjectId)
		{
			Name = name;
			IndexSubType = indexSubType;
			OwnerObjectId = ownerObjectId;
		}

		public string Name
		{
			get;
			set;
		}

		public TableIndexSubType IndexSubType
		{
			get;
			set;
		}

		public ObjectId OwnerObjectId
		{
			get;
			set;
		}

		public ICollection<Tuple<string, TableIndexSortDirection>> Columns => _columns;

	    public void AddColumn(string columnName, TableIndexSortDirection direction)
		{
			_columns.Add(new Tuple<string, TableIndexSortDirection>(columnName, direction));
		}
	}
}
