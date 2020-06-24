using System;
using System.IO;
using System.Threading.Tasks;
using Zen.Trunk.Storage.Data;
using Zen.Trunk.Storage.Data.Table;
using Zen.Trunk.VirtualMemory;

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

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Zen.Trunk.Storage.Data.RemoveDataDeviceParameters" />
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
        /// <param name="generateLogicalPageId">if set to <c>true</c> [assign automatic logical page identifier].</param>
        /// <param name="isNewObject">if set to <c>true</c> [is new object].</param>
        public InitFileGroupPageParameters(
            string fileGroupName, DataPage page, bool assignVirtualPageId = false, bool assignLogicalPageId = false, bool generateLogicalPageId = false, bool isNewObject = false)
            : base(page, assignVirtualPageId, assignLogicalPageId, generateLogicalPageId, isNewObject)
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

    public class DeallocateFileGroupDataPageParameters : DeallocateDataPageParameters
    {
        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="LoadFileGroupPageParameters"/> class.
        /// </summary>
        /// <param name="fileGroupName">Name of the file group.</param>
        /// <param name="page">The page.</param>
        public DeallocateFileGroupDataPageParameters(
            string fileGroupName, DataPage page)
            : base(page)
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
    /// <seealso cref="AddAudioParameters" />
    public class AddFileGroupAudioParameters : AddAudioParameters
    {
        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="AddFileGroupAudioParameters"/> class.
        /// </summary>
        /// <param name="fileGroupId">The file group identifier.</param>
        /// <param name="fileGroupName">Name of the file group.</param>
        /// <param name="audioName">Name of the table.</param>
        /// <param name="waveFileStream">The wave file stream.</param>
        public AddFileGroupAudioParameters(FileGroupId fileGroupId, string fileGroupName, string audioName, Stream waveFileStream)
            : base(audioName, waveFileStream)
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

    public class CreateObjectReferenceParameters
    {
        #region Public Constructors
        public CreateObjectReferenceParameters(
            string name,
            FileGroupId fileGroupId,
            ObjectType objectType,
            Func<ObjectId, Task<LogicalPageId>> firstPageFunc)
        {
            Name = name;
            FileGroupId = fileGroupId;
            ObjectType = objectType;
            FirstPageFunc = firstPageFunc;
        }
        #endregion

        #region Public Properties
        public string Name { get; }

        public FileGroupId FileGroupId { get; }

        public ObjectType ObjectType { get; }

        public Func<ObjectId, Task<LogicalPageId>> FirstPageFunc { get; } 
        #endregion
    }

    public class GetObjectReferenceParameters
    {
        #region Public Constructors
        public GetObjectReferenceParameters(ObjectId objectId, ObjectType? objectType)
        {
            ObjectId = objectId;
            ObjectType = objectType;
        }
        #endregion

        #region Public Properties
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
        public ObjectType? ObjectType { get; }
        #endregion
    }

    public class ObjectReferenceResult
    {
        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectReferenceResult" /> class.
        /// </summary>
        /// <param name="objectId">The object identifier.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="name">The name.</param>
        /// <param name="fileGroupId">The file group identifier.</param>
        /// <param name="firstLogicalPageId">The first logical page identifier.</param>
        public ObjectReferenceResult(
            ObjectId objectId,
            ObjectType objectType,
            string name,
            FileGroupId fileGroupId,
            LogicalPageId firstLogicalPageId)
        {
            ObjectId = objectId;
            Name = name;
            FileGroupId = fileGroupId;
            FirstLogicalPageId = firstLogicalPageId;
        }
        #endregion

        #region Public Properties
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
        /// Gets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; }

        /// <summary>
        /// Gets the file group identifier.
        /// </summary>
        /// <value>
        /// The file group identifier.
        /// </value>
        public FileGroupId FileGroupId { get; }

        /// <summary>
        /// Gets the first logical page identifier.
        /// </summary>
        /// <value>
        /// The first logical page identifier.
        /// </value>
        public LogicalPageId FirstLogicalPageId { get; } 
        #endregion
    }
}
