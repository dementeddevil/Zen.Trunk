namespace Zen.Trunk.Storage
{
	using System;
	using System.Runtime.Serialization;
	using System.Security.Permissions;

	[Serializable]
	public class DeviceException : StorageEngineException, ISerializable
	{
		#region Private Fields
		private readonly ushort _deviceId;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="DeviceException"/> class.
		/// </summary>
		/// <param name="deviceId">The device id.</param>
		public DeviceException(ushort deviceId) :
			this(deviceId, "Device exception occurred")
		{
			_deviceId = deviceId;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DeviceException"/> class.
		/// </summary>
		/// <param name="deviceId">The device id.</param>
		/// <param name="message">The message.</param>
		public DeviceException(ushort deviceId, string message) :
			base(message + " on device [" + deviceId + "].")
		{
			_deviceId = deviceId;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DeviceException"/> class.
		/// </summary>
		/// <param name="deviceId">The device id.</param>
		/// <param name="message">The message.</param>
		/// <param name="innerException">The inner exception.</param>
		public DeviceException(ushort deviceId, string message, Exception innerException) :
			base(message + " on device [" + deviceId + "].", innerException)
		{
			_deviceId = deviceId;
		}
		#endregion

		#region Protected Constructors
		/// <summary>
		/// Creates a CoreException object from serialisation information.
		/// </summary>
		/// <param name="info"></param>
		/// <param name="context"></param>
		protected DeviceException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			_deviceId = info.GetUInt16("DeviceId");
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets a value indicating the device ID that threw the exception.
		/// </summary>
		public ushort DeviceId => _deviceId;

	    #endregion

		#region Public Methods
		/// <summary>
		/// Fills serialization information with details of this exception object.
		/// </summary>
		/// <param name="info"></param>
		/// <param name="context"></param>
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}
			base.GetObjectData(info, context);
			info.AddValue("DeviceId", _deviceId);
		}
		#endregion
	}

	[Serializable]
	public class DeviceFullException : DeviceException
	{
		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="DeviceFullException"/> class.
		/// </summary>
		/// <param name="deviceId">The device id.</param>
		public DeviceFullException(ushort deviceId) :
			base(deviceId, "Device full")
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DeviceFullException"/> class.
		/// </summary>
		/// <param name="deviceId">The device id.</param>
		/// <param name="message">The message.</param>
		public DeviceFullException(ushort deviceId, string message) :
			base(deviceId, message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DeviceFullException"/> class.
		/// </summary>
		/// <param name="deviceId">The device id.</param>
		/// <param name="message">The message.</param>
		/// <param name="innerException">The inner exception.</param>
		public DeviceFullException(ushort deviceId, string message, Exception innerException) :
			base(deviceId, message, innerException)
		{
		}
		#endregion
	}

	[Serializable]
	public class DeviceInvalidPageException : DeviceException
	{
		public DeviceInvalidPageException(byte deviceId) :
			base(deviceId, "Invalid page exception.")
		{
		}

		public DeviceInvalidPageException(byte deviceId, uint pageId, bool isLogicalPage)
			: base(deviceId, "Invalid page exception, " + (isLogicalPage ? "Logical" : "Virtual") + " ID: " + pageId)
		{
			PageId = pageId;
			IsLogicalPage = isLogicalPage;
		}

		public DeviceInvalidPageException(byte deviceId, uint pageId, bool isLogicalPage, string message)
			: base(deviceId, message)
		{
			PageId = pageId;
			IsLogicalPage = isLogicalPage;
		}

		public DeviceInvalidPageException(byte deviceId, uint pageId, bool isLogicalPage, string message, Exception innerException)
			: base(deviceId, message, innerException)
		{
			PageId = pageId;
			IsLogicalPage = isLogicalPage;
		}

		[CLSCompliant(false)]
		public ulong PageId
		{
			get;
			private set;
		}

		public bool IsLogicalPage
		{
			get;
			private set;
		}
	}

	[Serializable]
	public class FileGroupException : DeviceException
	{
		#region Public Constructors
		public FileGroupException(ushort deviceId, string fileGroup)
			: base(deviceId)
		{
			FileGroup = fileGroup;
		}
		
		public FileGroupException(
			ushort deviceId, string fileGroup, string message)
			: base(deviceId, message)
		{
			FileGroup = fileGroup;
		}
		
		public FileGroupException(
			ushort deviceId, 
			string fileGroup,
			string message, 
			Exception innerException)
			: base(deviceId, message, innerException)
		{
			FileGroup = fileGroup;
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets or sets the file group.
		/// </summary>
		/// <value>The file group.</value>
		public string FileGroup
		{
			get;
			private set;
		}
		#endregion
	}

	[Serializable]
	public class FileGroupInvalidException : FileGroupException
	{
		#region Public Constructors
		public FileGroupInvalidException(ushort deviceId, string fileGroup)
			: base(deviceId, fileGroup)
		{
		}
		public FileGroupInvalidException(
			ushort deviceId, string fileGroup, string message)
			: base(deviceId, fileGroup, message)
		{
		}
		public FileGroupInvalidException(
			ushort deviceId, 
			string fileGroup,
			string message, 
			Exception innerException)
			: base(deviceId, fileGroup, message, innerException)
		{
		}
		#endregion
	}

	[Serializable]
	public class FileGroupFullException : FileGroupException
	{
		#region Public Constructors
		public FileGroupFullException(ushort deviceId, string fileGroup)
			: base(deviceId, fileGroup)
		{
		}
		public FileGroupFullException(
			ushort deviceId, string fileGroup, string message)
			: base(deviceId, fileGroup, message)
		{
		}
		public FileGroupFullException(
			ushort deviceId, 
			string fileGroup,
			string message, 
			Exception innerException)
			: base(deviceId, fileGroup, message, innerException)
		{
		}
		#endregion
	}
}
