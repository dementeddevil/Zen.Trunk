using Zen.Trunk.Storage.Data;
using Zen.Trunk.Storage.Data.Table;

namespace Zen.Trunk.Storage
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="AddDataDeviceParameters" />
    public class AddFileGroupDeviceParameters : AddDataDeviceParameters
	{
		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="AddFileGroupDeviceParameters"/> class.
		/// </summary>
		/// <param name="fileGroupId">The file group unique identifier.</param>
		/// <param name="fileGroupName">Name of the file group.</param>
		/// <param name="name">The name.</param>
		/// <param name="pathName">Name of the path.</param>
		/// <param name="deviceId">The device unique identifier.</param>
		/// <param name="createPageCount">The create page count.</param>
		/// <param name="updateRootPage">if set to <c>true</c> [update root page].</param>
		public AddFileGroupDeviceParameters(
			FileGroupId fileGroupId,
			string fileGroupName,
			string name,
			string pathName,
            DeviceId deviceId,
			uint createPageCount = 0,
			bool updateRootPage = false)
			: base(name, pathName, deviceId, createPageCount, updateRootPage)
		{
			FileGroupId = fileGroupId;
			FileGroupName = fileGroupName;
		}
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the file group identifier.
        /// </summary>
        /// <value>
        /// The file group identifier.
        /// </value>
        public FileGroupId FileGroupId { get; }

        /// <summary>
        /// Gets the name of the file group.
        /// </summary>
        /// <value>
        /// The name of the file group.
        /// </value>
        public string FileGroupName { get; }
        #endregion
    }

    public class RemoveFileGroupDeviceParameters : RemoveDataDeviceParameters
	{
		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="RemoveFileGroupDeviceParameters"/> class.
		/// </summary>
		/// <param name="fileGroupName">Name of the file group.</param>
		/// <param name="deviceName">Name of the device.</param>
		/// <param name="updateRootPage">if set to <c>true</c> [update root page].</param>
		public RemoveFileGroupDeviceParameters(
			string fileGroupName,
			string deviceName,
			bool updateRootPage = false)
			: base(deviceName, updateRootPage)
		{
			FileGroupName = fileGroupName;
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets the name of the file group.
		/// </summary>
		/// <value>
		/// The name of the file group.
		/// </value>
		public string FileGroupName
		{
			get;
		}
		#endregion
	}

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="InitDataPageParameters" />
    public class InitFileGroupPageParameters : InitDataPageParameters
	{
        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="InitFileGroupPageParameters"/> class.
        /// </summary>
        /// <param name="fileGroupName">Name of the file group.</param>
        /// <param name="page">The page.</param>
        /// <param name="assignVirtualPageId">if set to <c>true</c> [assign virtual page identifier].</param>
        /// <param name="assignLogicalPageId">if set to <c>true</c> [assign logical page identifier].</param>
        /// <param name="assignAutomaticLogicalPageId">if set to <c>true</c> [assign automatic logical page identifier].</param>
        /// <param name="isNewObject">if set to <c>true</c> [is new object].</param>
        public InitFileGroupPageParameters(
			string fileGroupName, DataPage page, bool assignVirtualPageId = false, bool assignLogicalPageId = false, bool assignAutomaticLogicalPageId = false, bool isNewObject = false)
			: base(page, assignVirtualPageId, assignLogicalPageId, assignAutomaticLogicalPageId, isNewObject)
		{
			FileGroupId = page.FileGroupId;
			FileGroupName = fileGroupName;
		}
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the file group identifier.
        /// </summary>
        /// <value>
        /// The file group identifier.
        /// </value>
        public FileGroupId FileGroupId { get; }

        /// <summary>
        /// Gets the name of the file group.
        /// </summary>
        /// <value>
        /// The name of the file group.
        /// </value>
        public string FileGroupName { get; }
        #endregion
    }

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="LoadDataPageParameters" />
    public class LoadFileGroupPageParameters : LoadDataPageParameters
	{
        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="LoadFileGroupPageParameters"/> class.
        /// </summary>
        /// <param name="fileGroupName">Name of the file group.</param>
        /// <param name="page">The page.</param>
        /// <param name="virtualPageIdValid">if set to <c>true</c> [virtual page identifier valid].</param>
        /// <param name="logicalPageIdValid">if set to <c>true</c> [logical page identifier valid].</param>
        public LoadFileGroupPageParameters(
			string fileGroupName, DataPage page, bool virtualPageIdValid = false, bool logicalPageIdValid = false)
			: base(page, virtualPageIdValid, logicalPageIdValid)
		{
			FileGroupId = page.FileGroupId;
			FileGroupName = fileGroupName;
		}
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the file group identifier.
        /// </summary>
        /// <value>
        /// The file group identifier.
        /// </value>
        public FileGroupId FileGroupId { get; }

        /// <summary>
        /// Gets the name of the file group.
        /// </summary>
        /// <value>
        /// The name of the file group.
        /// </value>
        public string FileGroupName { get; }
        #endregion
    }

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="AddTableParameters" />
    public class AddFileGroupTableParameters : AddTableParameters
	{
        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="AddFileGroupTableParameters"/> class.
        /// </summary>
        /// <param name="fileGroupId">The file group identifier.</param>
        /// <param name="fileGroupName">Name of the file group.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="columns">The columns.</param>
        public AddFileGroupTableParameters(FileGroupId fileGroupId, string fileGroupName, string tableName, params TableColumnInfo[] columns)
            : base(tableName, columns)
        {
            FileGroupId = fileGroupId;
            FileGroupName = fileGroupName;
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the file group identifier.
        /// </summary>
        /// <value>
        /// The file group identifier.
        /// </value>
        public FileGroupId FileGroupId { get; }

        /// <summary>
        /// Gets the name of the file group.
        /// </summary>
        /// <value>
        /// The name of the file group.
        /// </value>
        public string FileGroupName { get; } 
        #endregion
    }

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="AddTableIndexParameters" />
    public class AddFileGroupTableIndexParameters : AddTableIndexParameters
	{
        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="AddFileGroupTableIndexParameters"/> class.
        /// </summary>
        /// <param name="fileGroupId">The file group identifier.</param>
        /// <param name="fileGroupName">Name of the file group.</param>
        /// <param name="name">The name.</param>
        /// <param name="indexSubType">Type of the index sub.</param>
        /// <param name="objectId">The object identifier.</param>
        public AddFileGroupTableIndexParameters(FileGroupId fileGroupId, string fileGroupName, string name, TableIndexSubType indexSubType, ObjectId objectId)
            : base(name, indexSubType, objectId)
        {
            FileGroupId = fileGroupId;
            FileGroupName = fileGroupName;
        } 
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the file group identifier.
        /// </summary>
        /// <value>
        /// The file group identifier.
        /// </value>
        public FileGroupId FileGroupId { get; }

        /// <summary>
        /// Gets the name of the file group.
        /// </summary>
        /// <value>
        /// The name of the file group.
        /// </value>
        public string FileGroupName { get; }
        #endregion
    }
}
