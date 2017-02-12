namespace Zen.Trunk.VirtualMemory
{
    /// <summary>
    /// 
    /// </summary>
    public interface IBufferDeviceInfo
    {
        /// <summary>
        /// Gets the device identifier.
        /// </summary>
        /// <value>
        /// The device identifier.
        /// </value>
        DeviceId DeviceId
        {
            get;
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        string Name
        {
            get;
        }

        /// <summary>
        /// Gets the page count.
        /// </summary>
        /// <value>
        /// The page count.
        /// </value>
        uint PageCount
        {
            get;
        }
    }
}