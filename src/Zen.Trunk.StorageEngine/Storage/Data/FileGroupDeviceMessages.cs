using System.Threading.Tasks;

namespace Zen.Trunk.Storage.Data
{
	using System;
	using System.Collections.Generic;
	using Storage;
	using Table;

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
		/// <param name="assignLogicalPageId">if set to <c>true</c> [assign logical unique identifier].</param>
		/// <param name="assignAutomaticLogicalPageId">if set to <c>true</c> [assign automatic logical unique identifier].</param>
		/// <param name="isNewObject">if set to <c>true</c> [is new object].</param>
		public InitDataPageParameters(DataPage page, bool assignVirtualId = false, bool assignLogicalPageId = false, bool assignAutomaticLogicalPageId = false, bool isNewObject = false)
		{
			Page = page;
			AssignVirtualId = assignVirtualId;
			AssignLogicalPageId = assignLogicalPageId;
			AssignAutomaticLogicalPageId = assignAutomaticLogicalPageId;
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
		public bool AssignLogicalPageId
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
		public bool AssignAutomaticLogicalPageId
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
        /// <param name="virtualPageIdValid">
        /// Set to <c>true</c> when the virtual page id property on the page is valid;
        /// otherwise <c>false</c>.
        /// </param>
        /// <param name="logicalPageIdValid">
        /// Set to <c>true</c> when the logical page id property on the page is valid;
        /// otherwise <c>false</c>.
        /// </param>
        public LoadDataPageParameters(DataPage page, bool virtualPageIdValid = false, bool logicalPageIdValid = false)
		{
			Page = page;
			VirtualPageIdValid = virtualPageIdValid;
			LogicalPageIdValid = logicalPageIdValid;
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets the page.
		/// </summary>
		/// <value>
		/// The page.
		/// </value>
		public DataPage Page { get; }

		/// <summary>
		/// Gets a value indicating whether [virtual page unique identifier valid].
		/// </summary>
		/// <value>
		/// <c>true</c> if [virtual page unique identifier valid]; otherwise, <c>false</c>.
		/// </value>
		public bool VirtualPageIdValid { get; }

		/// <summary>
		/// Gets a value indicating whether [logical page unique identifier valid].
		/// </summary>
		/// <value>
		/// <c>true</c> if [logical page unique identifier valid]; otherwise, <c>false</c>.
		/// </value>
		public bool LogicalPageIdValid { get; }
		#endregion
	}

	public class AllocateDataPageParameters
	{
        #region Public Constructors
        public AllocateDataPageParameters(LogicalPageId logicalId, ObjectId objectId, ObjectType objectType, bool mixedExtent, bool onlyUsePrimaryDevice)
        {
            LogicalPageId = logicalId;
            ObjectId = objectId;
            ObjectType = objectType;
            MixedExtent = mixedExtent;
            OnlyUsePrimaryDevice = onlyUsePrimaryDevice;
        }
        #endregion

        #region Public Properties
        public LogicalPageId LogicalPageId { get; }

        public ObjectId ObjectId { get; }

        public ObjectType ObjectType { get; }

        public bool MixedExtent { get; }

        public bool OnlyUsePrimaryDevice { get; }
        #endregion
    }

    public class ExpandDataDeviceParameters
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExpandDataDeviceParameters" /> class.
        /// </summary>
        /// <param name="deviceId">The device id.</param>
        /// <param name="pageCount">The page count.</param>
        public ExpandDataDeviceParameters(DeviceId deviceId, uint pageCount)
        {
            DeviceId = deviceId;
            PageCount = pageCount;
        }

        /// <summary>
        /// Gets or sets the device id.
        /// </summary>
        /// <value>The device id.</value>
        public DeviceId DeviceId { get; }

        /// <summary>
        /// Gets or sets an integer that will be added to the existing page 
        /// count of the target device to determine the new page capacity.
        /// </summary>
        /// <value>The page count.</value>
        public uint PageCount { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the device id is valid.
        /// </summary>
        /// <value>
        /// <c>true</c> if the device id is valid; otherwise, <c>false</c>.
        /// </value>
        public bool IsDeviceIdValid => DeviceId != DeviceId.Zero;
    }

    public class AddTableParameters
	{
		private readonly List<TableColumnInfo> _columns = new List<TableColumnInfo>();

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
    		_columns.AddRange(columns);
		}

		public string TableName { get; }

		public IList<TableColumnInfo> Columns => _columns;
	}

	public class AddTableIndexParameters
	{
	    private readonly IDictionary<string, TableIndexSortDirection> _columns =
	        new Dictionary<string, TableIndexSortDirection>(StringComparer.OrdinalIgnoreCase);

		public AddTableIndexParameters(string name, TableIndexSubType indexSubType, ObjectId objectId)
		{
			Name = name;
			IndexSubType = indexSubType;
			ObjectId = objectId;
		}

		public string Name { get; }

        public TableIndexSubType IndexSubType { get; }

		public ObjectId ObjectId { get; }

        public IDictionary<string, TableIndexSortDirection> Columns => _columns;

	    public void AddColumn(string columnName, TableIndexSortDirection direction)
		{
			_columns.Add(columnName, direction);
		}
	}

    public class CreateObjectReferenceParameters
    {
        public CreateObjectReferenceParameters(string name, ObjectType objectType, Func<ObjectId, Task<LogicalPageId>> firstPageFunc)
        {
            Name = name;
            ObjectType = objectType;
            FirstPageFunc = firstPageFunc;
        }

        public string Name { get; }

        public ObjectType ObjectType { get; }

        public Func<ObjectId, Task<LogicalPageId>> FirstPageFunc { get; }
    }
}
