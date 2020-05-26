using Zen.Trunk.VirtualMemory;

namespace Zen.Trunk.Storage.Logging
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="AddDeviceParameters" />
    public class AddLogDeviceParameters : AddDeviceParameters
    {
        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="AddLogDeviceParameters"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="pathName">Name of the path.</param>
        /// <param name="deviceId">The device unique identifier.</param>
        /// <param name="createPageCount">The create page count.</param>
        /// <param name="growthPages">The growth pages.</param>
        /// <param name="maximumPages">The maximum page count.</param>
        /// <param name="updateRootPage">if set to <c>true</c> [update root page].</param>
        public AddLogDeviceParameters(string name, string pathName, DeviceId deviceId, uint createPageCount = 0, uint growthPages = 0, uint maximumPages = 0, bool updateRootPage = false)
            : base(name, pathName, deviceId, createPageCount)
        {
            GrowthPages = growthPages;
            MaximumPages = maximumPages;
            UpdateRootPage = updateRootPage;
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the growth pages.
        /// </summary>
        /// <value>
        /// The growth pages.
        /// </value>
        public uint GrowthPages { get; }

        /// <summary>
        /// Gets the maximum number of pages.
        /// </summary>
        /// <value>
        /// The maximum page count.
        /// </value>
        /// <remarks>
        /// If this value is zero then the maximum page count is unlimited.
        /// </remarks>
        public uint MaximumPages { get; }

        /// <summary>
        /// Gets a value indicating whether [update root page].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [update root page]; otherwise, <c>false</c>.
        /// </value>
        public bool UpdateRootPage { get; }
        #endregion
    }

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="RemoveDeviceParameters" />
    public class RemoveLogDeviceParameters : RemoveDeviceParameters
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RemoveLogDeviceParameters" /> class.
        /// </summary>
        /// <param name="deviceId">The device identifier.</param>
        /// <param name="updateRootPage">if set to <c>true</c> [update root page].</param>
        public RemoveLogDeviceParameters(DeviceId deviceId, bool updateRootPage = false)
            : base(deviceId)
        {
            UpdateRootPage = updateRootPage;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoveLogDeviceParameters" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="updateRootPage">if set to <c>true</c> [update root page].</param>
        public RemoveLogDeviceParameters(string name, bool updateRootPage = false)
            : base(name)
        {
            UpdateRootPage = updateRootPage;
        }

        /// <summary>
        /// Gets a value indicating whether [update root page].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [update root page]; otherwise, <c>false</c>.
        /// </value>
        public bool UpdateRootPage { get; }
    }

    public class ExpandLogDeviceParameters
    {
    }

    public class TruncateLogDeviceParameters
    {
    }
}
