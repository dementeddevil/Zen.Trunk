﻿using Zen.Trunk.Storage.Data.Table;

namespace Zen.Trunk.Storage.Data
{
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
		public FileGroupId FileGroupId { get; }

		public string FileGroupName { get; }

		public bool FileGroupIdValid => FileGroupId != FileGroupId.Invalid;

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

	public class InitFileGroupPageParameters : InitDataPageParameters
	{
		#region Public Constructors
		public InitFileGroupPageParameters(
			string fileGroupName, DataPage page, bool assignVirtualPageId = false, bool assignLogicalPageId = false, bool assignAutomaticLogicalPageId = false, bool isNewObject = false)
			: base(page, assignVirtualPageId, assignLogicalPageId, assignAutomaticLogicalPageId, isNewObject)
		{
			FileGroupId = page.FileGroupId;
			FileGroupName = fileGroupName;
		}
		#endregion

		#region Public Properties
		public FileGroupId FileGroupId
		{
			get;
		}

		public string FileGroupName
		{
			get;
		}

		public bool FileGroupIdValid => FileGroupId != FileGroupId.Invalid;

	    #endregion
	}

	public class LoadFileGroupPageParameters : LoadDataPageParameters
	{
		#region Public Constructors
		public LoadFileGroupPageParameters(
			string fileGroupName, DataPage page, bool virtualPageIdValid = false, bool logicalPageIdValid = false)
			: base(page, virtualPageIdValid, logicalPageIdValid)
		{
			FileGroupId = page.FileGroupId;
			FileGroupName = fileGroupName;
		}
		#endregion

		#region Public Properties
		public FileGroupId FileGroupId { get; }

		public string FileGroupName { get; }

		public bool FileGroupIdValid => FileGroupId != FileGroupId.Invalid;
	    #endregion
	}

	public class AddFileGroupTableParameters : AddTableParameters
	{
        public AddFileGroupTableParameters(FileGroupId fileGroupId, string fileGroupName, string tableName, params TableColumnInfo[] columns)
			: base(tableName, columns)
		{
			FileGroupId = fileGroupId;
			FileGroupName = fileGroupName;
		}

		public FileGroupId FileGroupId { get; }

		public string FileGroupName { get; }

		public bool FileGroupIdValid => FileGroupId != FileGroupId.Invalid;
	}

	public class AddFileGroupTableIndexParameters : AddTableIndexParameters
	{
		public AddFileGroupTableIndexParameters(FileGroupId fileGroupId, string fileGroupName, string name, TableIndexSubType indexSubType, ObjectId objectId)
			: base(name, indexSubType, objectId)
		{
			FileGroupId = fileGroupId;
			FileGroupName = fileGroupName;
		}

		public FileGroupId FileGroupId { get; }

		public string FileGroupName { get; }

		public bool FileGroupIdValid => FileGroupId != FileGroupId.Invalid;
	}
}
