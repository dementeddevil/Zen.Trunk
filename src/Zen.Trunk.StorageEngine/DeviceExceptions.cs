using System;
using System.Runtime.Serialization;
using Zen.Trunk.VirtualMemory;

namespace Zen.Trunk.Storage
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Zen.Trunk.Storage.StorageEngineException" />
    [Serializable]
    public class DeviceException : StorageEngineException
    {
        #region Private Fields
        private readonly DeviceId _deviceId;
        #endregion

        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceException"/> class.
        /// </summary>
        /// <param name="deviceId">The device id.</param>
        public DeviceException(DeviceId deviceId) :
            this(deviceId, "Device exception occurred")
        {
            _deviceId = deviceId;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceException"/> class.
        /// </summary>
        /// <param name="deviceId">The device id.</param>
        /// <param name="message">The message.</param>
        public DeviceException(DeviceId deviceId, string message) :
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
        public DeviceException(DeviceId deviceId, string message, Exception innerException) :
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
            _deviceId = new DeviceId(info.GetUInt16("DeviceId"));
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets a value indicating the device ID that threw the exception.
        /// </summary>
        public DeviceId DeviceId => _deviceId;

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
                throw new ArgumentNullException(nameof(info));
            }
            base.GetObjectData(info, context);
            info.AddValue("DeviceId", _deviceId.Value);
        }
        #endregion
    }

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Zen.Trunk.Storage.DeviceException" />
    [Serializable]
    public class DeviceFullException : DeviceException
    {
        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceFullException"/> class.
        /// </summary>
        /// <param name="deviceId">The device id.</param>
        public DeviceFullException(DeviceId deviceId) :
            base(deviceId, "Device full")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceFullException"/> class.
        /// </summary>
        /// <param name="deviceId">The device id.</param>
        /// <param name="message">The message.</param>
        public DeviceFullException(DeviceId deviceId, string message) :
            base(deviceId, message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceFullException"/> class.
        /// </summary>
        /// <param name="deviceId">The device id.</param>
        /// <param name="message">The message.</param>
        /// <param name="innerException">The inner exception.</param>
        public DeviceFullException(DeviceId deviceId, string message, Exception innerException) :
            base(deviceId, message, innerException)
        {
        }
        #endregion
    }

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Zen.Trunk.Storage.DeviceException" />
    [Serializable]
    public class FileGroupException : DeviceException
    {
        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="FileGroupException"/> class.
        /// </summary>
        /// <param name="deviceId">The device identifier.</param>
        /// <param name="fileGroupId">The file group identifier.</param>
        /// <param name="fileGroupName">Name of the file group.</param>
        public FileGroupException(DeviceId deviceId, FileGroupId fileGroupId, string fileGroupName)
            : base(deviceId)
        {
            FileGroupId = fileGroupId;
            FileGroupName = fileGroupName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileGroupException"/> class.
        /// </summary>
        /// <param name="deviceId">The device identifier.</param>
        /// <param name="fileGroupId">The file group identifier.</param>
        /// <param name="fileGroupName">Name of the file group.</param>
        /// <param name="message">The message.</param>
        public FileGroupException(
            DeviceId deviceId, FileGroupId fileGroupId, string fileGroupName, string message)
            : base(deviceId, message)
        {
            FileGroupId = fileGroupId;
            FileGroupName = fileGroupName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileGroupException"/> class.
        /// </summary>
        /// <param name="deviceId">The device identifier.</param>
        /// <param name="fileGroupId">The file group identifier.</param>
        /// <param name="fileGroupName">Name of the file group.</param>
        /// <param name="message">The message.</param>
        /// <param name="innerException">The inner exception.</param>
        public FileGroupException(
            DeviceId deviceId,
            FileGroupId fileGroupId,
            string fileGroupName,
            string message,
            Exception innerException)
            : base(deviceId, message, innerException)
        {
            FileGroupId = fileGroupId;
            FileGroupName = fileGroupName;
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the file group id.
        /// </summary>
        /// <value>The file group id.</value>
        public FileGroupId FileGroupId { get; }

        /// <summary>
        /// Gets the file group.
        /// </summary>
        /// <value>The file group name.</value>
        public string FileGroupName { get; }
        #endregion
    }

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Zen.Trunk.Storage.FileGroupException" />
    [Serializable]
    public class FileGroupInvalidException : FileGroupException
    {
        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="FileGroupInvalidException"/> class.
        /// </summary>
        /// <param name="deviceId">The device identifier.</param>
        /// <param name="fileGroupId">The file group identifier.</param>
        /// <param name="fileGroupName">Name of the file group.</param>
        public FileGroupInvalidException(DeviceId deviceId, FileGroupId fileGroupId, string fileGroupName)
            : base(deviceId, fileGroupId, fileGroupName)
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="FileGroupInvalidException" /> class.
        /// </summary>
        /// <param name="deviceId">The device identifier.</param>
        /// <param name="fileGroupId">The file group identifier.</param>
        /// <param name="fileGroupName">Name of the file group.</param>
        /// <param name="message">The message.</param>
        public FileGroupInvalidException(
            DeviceId deviceId, FileGroupId fileGroupId, string fileGroupName, string message)
            : base(deviceId, fileGroupId, fileGroupName, message)
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="FileGroupInvalidException" /> class.
        /// </summary>
        /// <param name="deviceId">The device identifier.</param>
        /// <param name="fileGroupId">The file group identifier.</param>
        /// <param name="fileGroupName">Name of the file group.</param>
        /// <param name="message">The message.</param>
        /// <param name="innerException">The inner exception.</param>
        public FileGroupInvalidException(
            DeviceId deviceId,
            FileGroupId fileGroupId,
            string fileGroupName,
            string message,
            Exception innerException)
            : base(deviceId, fileGroupId, fileGroupName, message, innerException)
        {
        }
        #endregion
    }

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Zen.Trunk.Storage.FileGroupException" />
    [Serializable]
    public class FileGroupFullException : FileGroupException
    {
        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="FileGroupFullException"/> class.
        /// </summary>
        /// <param name="deviceId">The device identifier.</param>
        /// <param name="fileGroupId">The file group identifier.</param>
        /// <param name="fileGroupName">Name of the file group.</param>
        public FileGroupFullException(DeviceId deviceId, FileGroupId fileGroupId, string fileGroupName)
            : base(deviceId, fileGroupId, fileGroupName)
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="FileGroupFullException"/> class.
        /// </summary>
        /// <param name="deviceId">The device identifier.</param>
        /// <param name="fileGroupId">The file group identifier.</param>
        /// <param name="fileGroupName">Name of the file group.</param>
        /// <param name="message">The message.</param>
        public FileGroupFullException(
            DeviceId deviceId, FileGroupId fileGroupId, string fileGroupName, string message)
            : base(deviceId, fileGroupId, fileGroupName, message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileGroupFullException"/> class.
        /// </summary>
        /// <param name="deviceId">The device identifier.</param>
        /// <param name="fileGroupId">The file group identifier.</param>
        /// <param name="fileGroupName">Name of the file group.</param>
        /// <param name="message">The message.</param>
        /// <param name="innerException">The inner exception.</param>
        public FileGroupFullException(
            DeviceId deviceId,
            FileGroupId fileGroupId,
            string fileGroupName,
            string message,
            Exception innerException)
            : base(deviceId, fileGroupId, fileGroupName, message, innerException)
        {
        }
        #endregion
    }
}
