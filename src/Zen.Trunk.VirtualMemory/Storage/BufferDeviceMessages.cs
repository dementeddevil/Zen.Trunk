namespace Zen.Trunk.Storage
{
	using System;

    /// <summary>
    /// 
    /// </summary>
    [CLSCompliant(false)]
	public class FlushParameters
	{
		#region Public Constructors
		/// <summary>
		/// Initialises an instance of <see cref="T:FlushParameters" />.
		/// </summary>
		public FlushParameters()
		{
			FlushReads = true;
			FlushWrites = true;
		}

		/// <summary>
		/// Initialises an instance of <see cref="T:FlushParameters"/>.
		/// </summary>
		/// <param name="reads">if set to <c>true</c> [reads].</param>
		/// <param name="writes">if set to <c>true</c> [writes].</param>
		public FlushParameters(bool reads, bool writes)
		{
			FlushReads = reads;
			FlushWrites = writes;
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets or sets a value indicating whether to flush read requests.
		/// </summary>
		/// <value>
		/// <c>true</c> if to flush read requests; otherwise, <c>false</c>.
		/// </value>
		public bool FlushReads
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets or sets a value indicating whether to flush write requests.
		/// </summary>
		/// <value>
		/// <c>true</c> to flush write requests; otherwise, <c>false</c>.
		/// </value>
		public bool FlushWrites
		{
			get;
			private set;
		}
		#endregion
	}

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Zen.Trunk.Storage.FlushParameters" />
    [CLSCompliant(false)]
	public class FlushDeviceParameters : FlushParameters
	{
		#region Public Constructors
		/// <summary>
		/// Initialises an instance of <see cref="T:FlushDeviceParameters" />.
		/// </summary>
		public FlushDeviceParameters()
		{
		}

        /// <summary>
        /// Initialises an instance of <see cref="T:FlushDeviceParameters" />.
        /// </summary>
        /// <param name="reads">if set to <c>true</c> [reads].</param>
        /// <param name="writes">if set to <c>true</c> [writes].</param>
        public FlushDeviceParameters(bool reads, bool writes)
            : base(reads, writes)
        {
        }

        /// <summary>
        /// Initialises an instance of <see cref="T:FlushDeviceParameters" />.
        /// </summary>
        /// <param name="reads">if set to <c>true</c> [reads].</param>
        /// <param name="writes">if set to <c>true</c> [writes].</param>
        /// <param name="deviceId">The device id.</param>
        public FlushDeviceParameters(bool reads, bool writes, DeviceId deviceId)
			: base(reads, writes)
		{
			DeviceId = deviceId;
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets a value indicating whether all devices are to be flushed.
		/// </summary>
		/// <value>
		/// <c>true</c> to flush all devices; otherwise, <c>false</c>.
		/// </value>
		public bool AllDevices => (DeviceId == DeviceId.Zero);

	    /// <summary>
		/// Gets or sets the device id.
		/// </summary>
		/// <value>The device id.</value>
		public DeviceId DeviceId { get; }
		#endregion
	}

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Zen.Trunk.Storage.FlushDeviceParameters" />
    [CLSCompliant(false)]
	public class FlushCachingDeviceParameters : FlushDeviceParameters
	{
		#region Public Constructors
		/// <summary>
		/// Initialises an instance of <see cref="FlushCachingDeviceParameters" />.
		/// </summary>
		public FlushCachingDeviceParameters()
		{
		}

		/// <summary>
		/// Initialises an instance of <see cref="T:FlushDeviceBuffers"/>.
		/// </summary>
		/// <param name="deviceId">The device id.</param>
		/// <param name="reads">if set to <c>true</c> [reads].</param>
		/// <param name="writes">if set to <c>true</c> [writes].</param>
		public FlushCachingDeviceParameters(bool reads, bool writes, DeviceId deviceId)
			: base(reads, writes, deviceId)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="FlushCachingDeviceParameters"/> class.
		/// </summary>
		/// <param name="isForCheckPoint">if set to <c>true</c> [is for check point].</param>
		public FlushCachingDeviceParameters(bool isForCheckPoint)
			: base(false, true)
		{
			IsForCheckPoint = isForCheckPoint;
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets or sets a value indicating whether this instance is for check point.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is for check point; otherwise, <c>false</c>.
		/// </value>
		public bool IsForCheckPoint
		{
			get;
			private set;
		}
		#endregion
	}

    /// <summary>
    /// 
    /// </summary>
    [CLSCompliant(false)]
	public class AddDeviceParameters
	{
		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="AddDeviceParameters" /> class.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="pathName">Name of the path.</param>
		/// <param name="deviceId">The device id.</param>
		/// <param name="createPageCount">The create page count.</param>
		/// <remarks>
		/// If createPageCount = 0 then this is an open otherwise it is a create.
		/// If deviceId = 0 then device id will be auto-generated.
		/// </remarks>
		public AddDeviceParameters(
			string name,
			string pathName,
            DeviceId deviceId,
            uint createPageCount = 0)
		{
			Name = name;
			PathName = pathName;
			CreatePageCount = createPageCount;
			DeviceId = deviceId;
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets or sets the name.
		/// </summary>
		/// <value>The name.</value>
		public string Name { get; }

        /// <summary>
        /// Gets or sets the name of the path.
        /// </summary>
        /// <value>The name of the path.</value>
        public string PathName { get; }

		/// <summary>
		/// Gets or sets the create page count.
		/// </summary>
		/// <value>The create page count.</value>
		public uint CreatePageCount { get; }

		/// <summary>
		/// Gets or sets the device id.
		/// </summary>
		/// <value>The device id.</value>
		public DeviceId DeviceId { get; }

		/// <summary>
		/// Gets or sets a value indicating whether the device id is valid.
		/// </summary>
		/// <value><c>true</c> if the device id is valid; otherwise, <c>false</c>.</value>
		public bool IsDeviceIdValid => DeviceId != DeviceId.Zero;

	    /// <summary>
		/// Gets or sets a value indicating whether this instance is create.
		/// </summary>
		/// <value><c>true</c> if this instance is create; otherwise, <c>false</c>.</value>
		public bool IsCreate => CreatePageCount != 0;

	    #endregion
	}

    /// <summary>
    /// 
    /// </summary>
    [CLSCompliant(false)]
	public class RemoveDeviceParameters
	{
		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="RemoveDeviceParameters"/> class.
		/// </summary>
		/// <param name="deviceId">The device unique identifier.</param>
		public RemoveDeviceParameters(DeviceId deviceId)
		{
			DeviceId = deviceId;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RemoveDeviceParameters"/> class.
		/// </summary>
		/// <param name="name">The name.</param>
		public RemoveDeviceParameters(string name)
		{
			Name = name;
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets the name.
		/// </summary>
		/// <value>
		/// The name.
		/// </value>
		public string Name { get; }

		/// <summary>
		/// Gets the device unique identifier.
		/// </summary>
		/// <value>
		/// The device unique identifier.
		/// </value>
		public DeviceId DeviceId { get; }

		/// <summary>
		/// Gets a value indicating whether the device unique identifier is valid.
		/// </summary>
		/// <value>
		/// <c>true</c> if device unique identifier is valid; otherwise, <c>false</c>.
		/// </value>
		public bool DeviceIdValid => (DeviceId != DeviceId.Zero);

	    #endregion
	}
}
