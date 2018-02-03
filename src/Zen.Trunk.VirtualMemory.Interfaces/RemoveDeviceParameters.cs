using System;

namespace Zen.Trunk.VirtualMemory
{
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