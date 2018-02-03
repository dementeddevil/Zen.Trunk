using System;

namespace Zen.Trunk.VirtualMemory
{
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
        /// <remarks>
        /// When this value is greater than zero then the request is interpreted
        /// as a request to create the underlying file when the owner device is
        /// opened.
        /// <seealso cref="IsCreate"/>
        /// </remarks>
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
        /// <value>
        /// <c>true</c> if this parameter denotes create-new; otherwise,
        /// <c>false</c> if open-existing.
        /// </value>
        public bool IsCreate => CreatePageCount != 0;
        #endregion
    }
}