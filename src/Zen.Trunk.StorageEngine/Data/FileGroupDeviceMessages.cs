using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Zen.Trunk.Storage.Data.Table;
using Zen.Trunk.VirtualMemory;

namespace Zen.Trunk.Storage.Data
{
    /// <summary>
    /// <c>AddDataDeviceParameters</c> encapsulates data used with a call
    /// to add a data device file to a file-group device.
    /// </summary>
    /// <seealso cref="AddDeviceParameters" />
    /// <seealso cref="FileGroupDevice.AddDataDeviceAsync(AddDataDeviceParameters)"/>
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
        public bool UpdateRootPage { get; }
        #endregion
    }

    /// <summary>
    /// <c>RemoveDataDeviceParameters</c> encapsulates data used with a call
    /// to remove a data device file from a file-group device.
    /// </summary>
    /// <seealso cref="RemoveDeviceParameters" />
    /// <seealso cref="FileGroupDevice.RemoveDataDeviceAsync(RemoveDataDeviceParameters)"/>
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

    /// <summary>
    /// 
    /// </summary>
    public class InitDataPageParameters
    {
        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="InitDataPageParameters"/> class.
        /// </summary>
        /// <param name="page">The page.</param>
        /// <param name="assignVirtualPageId">if set to <c>true</c> [assign virtual unique identifier].</param>
        /// <param name="assignLogicalPageId">if set to <c>true</c> [assign logical unique identifier].</param>
        /// <param name="generateLogicalPageId">if set to <c>true</c> [assign automatic logical unique identifier].</param>
        /// <param name="isNewObject">if set to <c>true</c> [is new object].</param>
        public InitDataPageParameters(DataPage page, bool assignVirtualPageId = false, bool assignLogicalPageId = false, bool generateLogicalPageId = false, bool isNewObject = false)
        {
            Page = page;
            AssignVirtualPageId = assignVirtualPageId;
            AssignLogicalPageId = assignLogicalPageId;
            GenerateLogicalPageId = generateLogicalPageId;
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
        /// Gets a value indicating whether to assign virtual unique identifier.
        /// This value controls whether an entry will be placed in a distribution
        /// page.
        /// </summary>
        /// <value>
        /// <c>true</c> the virtual page will be determined by checking all 
        /// associated distribution pages and allocating a free page; otherwise, 
        /// <c>false</c> and the virtual page identifier associated with the page
        /// will be used.
        /// </value>
        /// <remarks>
        /// Set this value to <c>true</c> for all pages except root pages and
        /// distribution pages.
        /// </remarks>
        public bool AssignVirtualPageId
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
        /// Gets a value indicating whether to generate a logical page identifier.
        /// </summary>
        /// <value>
        /// <c>true</c> a new logical page identifier will be generated for the page; otherwise, <c>false</c>.
        /// </value>
        public bool GenerateLogicalPageId
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

    /// <summary>
    /// 
    /// </summary>
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
        /// Gets a value indicating whether the virtual page unique identifier is valid.
        /// </summary>
        /// <value>
        /// <c>true</c> if the virtual page unique identifier is valid; otherwise, <c>false</c>.
        /// </value>
        public bool VirtualPageIdValid { get; }

        /// <summary>
        /// Gets a value indicating whether the logical page unique identifier is valid.
        /// </summary>
        /// <value>
        /// <c>true</c> if the logical page unique identifier is valid; otherwise, <c>false</c>.
        /// </value>
        public bool LogicalPageIdValid { get; }
        #endregion
    }

    /// <summary>
    /// 
    /// </summary>
    public class AllocateDataPageParameters
    {
        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="AllocateDataPageParameters"/> class.
        /// </summary>
        /// <param name="logicalId">The logical identifier.</param>
        /// <param name="objectId">The object identifier.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="mixedExtent">if set to <c>true</c> [mixed extent].</param>
        /// <param name="onlyUsePrimaryDevice">if set to <c>true</c> [only use primary device].</param>
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
        /// <summary>
        /// Gets the logical page identifier.
        /// </summary>
        /// <value>
        /// The logical page identifier.
        /// </value>
        public LogicalPageId LogicalPageId { get; }

        /// <summary>
        /// Gets the object identifier.
        /// </summary>
        /// <value>
        /// The object identifier.
        /// </value>
        public ObjectId ObjectId { get; }

        /// <summary>
        /// Gets the type of the object.
        /// </summary>
        /// <value>
        /// The type of the object.
        /// </value>
        public ObjectType ObjectType { get; }

        /// <summary>
        /// Gets a value indicating whether [mixed extent].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [mixed extent]; otherwise, <c>false</c>.
        /// </value>
        public bool MixedExtent { get; }

        /// <summary>
        /// Gets a value indicating whether [only use primary device].
        /// </summary>
        /// <value>
        /// <c>true</c> if [only use primary device]; otherwise, <c>false</c>.
        /// </value>
        public bool OnlyUsePrimaryDevice { get; }
        #endregion
    }

    /// <summary>
    /// 
    /// </summary>
    public class DeallocateDataPageParameters
    {
        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="DeallocateDataPageParameters"/> class.
        /// </summary>
        /// <param name="page">The page.</param>
        public DeallocateDataPageParameters(DataPage page)
        {
            if (page == null)
            {
                throw new ArgumentNullException(nameof(page));
            }

            VirtualPageId = page.VirtualPageId;
            var logicalPage = page as LogicalPage;
            if (logicalPage != null)
            {
                LogicalPageId = logicalPage.LogicalPageId;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeallocateDataPageParameters"/> class.
        /// </summary>
        /// <param name="virtualPageId">The virtual page identifier.</param>
        /// <param name="logicalPageId">The logical page identifier.</param>
        public DeallocateDataPageParameters(VirtualPageId virtualPageId, LogicalPageId logicalPageId)
        {
            VirtualPageId = virtualPageId;
            LogicalPageId = logicalPageId;
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the virtual page identifier.
        /// </summary>
        /// <value>
        /// The virtual page identifier.
        /// </value>
        public VirtualPageId VirtualPageId { get; }

        /// <summary>
        /// Gets the logical page identifier.
        /// </summary>
        /// <value>
        /// The logical page identifier.
        /// </value>
        public LogicalPageId LogicalPageId { get; }
        #endregion
    }

    /// <summary>
    /// 
    /// </summary>
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

    /// <summary>
    /// 
    /// </summary>
    public class AddTableParameters
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AddTableParameters"/> class.
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="columns">The columns.</param>
        /// <exception cref="ArgumentException">
        /// Table name is required.
        /// or
        /// Table must have at least one column definition.
        /// </exception>
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
            foreach (var column in columns)
            {
                Columns.Add(column);
            }
        }

        /// <summary>
        /// Gets the name of the table.
        /// </summary>
        /// <value>
        /// The name of the table.
        /// </value>
        public string TableName { get; }

        /// <summary>
        /// Gets the columns.
        /// </summary>
        /// <value>
        /// The columns.
        /// </value>
        public IList<TableColumnInfo> Columns { get; } = new List<TableColumnInfo>();
    }

    /// <summary>
    /// 
    /// </summary>
    public class AddTableIndexParameters
    {
        private readonly IDictionary<string, TableIndexSortDirection> _columns =
            new Dictionary<string, TableIndexSortDirection>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Initializes a new instance of the <see cref="AddTableIndexParameters"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="indexSubType">Type of the index sub.</param>
        /// <param name="objectId">The object identifier.</param>
        public AddTableIndexParameters(string name, TableIndexSubType indexSubType, ObjectId objectId)
        {
            Name = name;
            IndexSubType = indexSubType;
            ObjectId = objectId;
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; }

        /// <summary>
        /// Gets the type of the index sub.
        /// </summary>
        /// <value>
        /// The type of the index sub.
        /// </value>
        public TableIndexSubType IndexSubType { get; }

        /// <summary>
        /// Gets the object identifier for the associated table.
        /// </summary>
        /// <value>
        /// The object identifier.
        /// </value>
        public ObjectId ObjectId { get; }

        /// <summary>
        /// Gets the columns.
        /// </summary>
        /// <value>
        /// The columns.
        /// </value>
        public IDictionary<string, TableIndexSortDirection> Columns => _columns;

        /// <summary>
        /// Adds the column and sort direction to the index definition.
        /// </summary>
        /// <param name="columnName">Name of the column.</param>
        /// <param name="direction">The direction.</param>
        public void AddColumnAndSortDirection(string columnName, TableIndexSortDirection direction)
        {
            _columns.Add(columnName, direction);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class InsertReferenceInformationRequestParameters
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InsertReferenceInformationRequestParameters"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="firstPageFunc">The first page function.</param>
        public InsertReferenceInformationRequestParameters(
            ICollection<DeviceInfo> devices,
            ICollection<ObjectRefInfo> objects,
            string name, 
            ObjectType objectType, 
            Func<ObjectId, Task<LogicalPageId>> firstPageFunc)
        {
            Name = name;
            ObjectType = objectType;
            FirstPageFunc = firstPageFunc;
            Devices = devices;
            Objects = objects;
        }

        public ICollection<DeviceInfo> Devices { get; }

        public ICollection<ObjectRefInfo> Objects { get; }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; }

        /// <summary>
        /// Gets the type of the object.
        /// </summary>
        /// <value>
        /// The type of the object.
        /// </value>
        public ObjectType ObjectType { get; }

        /// <summary>
        /// Gets the function that when called with an <see cref="ObjectId"/>
        /// returns a <see cref="Task{LogicalPageId}"/> resolves to the first
        /// logical page identifier for the object.
        /// </summary>
        /// <value>
        /// The first page function.
        /// </value>
        public Func<ObjectId, Task<LogicalPageId>> FirstPageFunc { get; }
    }
}
