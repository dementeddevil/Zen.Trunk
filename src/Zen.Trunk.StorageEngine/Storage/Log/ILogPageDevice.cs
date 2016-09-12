namespace Zen.Trunk.Storage.Log
{
    /// <summary>
    /// <c>ILogPageDevice</c> interface defines the contract for all log devices.
    /// </summary>
    /// <seealso cref="Zen.Trunk.Storage.IMountableDevice" />
    public interface ILogPageDevice : IMountableDevice
    {
        /// <summary>
        /// Gets the device identifier.
        /// </summary>
        /// <value>
        /// The device identifier.
        /// </value>
        DeviceId DeviceId { get; }

        /// <summary>
        /// Gets or sets the name of the path.
        /// </summary>
        /// <value>
        /// The name of the path.
        /// </value>
        string PathName { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is read only.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is read only; otherwise, <c>false</c>.
        /// </value>
        bool IsReadOnly { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is in recovery.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is in recovery; otherwise, <c>false</c>.
        /// </value>
        bool IsInRecovery { get; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is being created.
        /// </summary>
        /// <value><c>true</c> if this instance is create; otherwise, <c>false</c>.</value>
        bool IsCreate { get; }

        /// <summary>
        /// Initialises the virtual file table for a newly added log device.
        /// </summary>
        /// <param name="masterRootPage">The master root page.</param>
        /// <returns>
        /// A <see cref="VirtualLogFileInfo"/> representing the log file information.
        /// </returns>
        /// <remarks>
        /// This method will chain the new file table onto the current table
        /// by examining the logLastFileId and passing the related file to
        /// the Init routine.
        /// </remarks>
        VirtualLogFileInfo InitVirtualFileForDevice(
            MasterLogRootPage masterRootPage);

        /// <summary>
        /// Initialises the virtual file table for a log device.
        /// </summary>
        /// <param name="masterRootPage">The master root page.</param>
        /// <param name="lastFileInfo">The last file information.</param>
        /// <returns>
        /// A <see cref="VirtualLogFileInfo" /> representing the log file information.
        /// </returns>
        VirtualLogFileInfo InitVirtualFileForDevice(
            MasterLogRootPage masterRootPage,
            VirtualLogFileInfo lastFileInfo);

        /// <summary>
        /// Gets the virtual file by identifier.
        /// </summary>
        /// <param name="fileId">The file identifier.</param>
        /// <returns>
        /// A <see cref="VirtualLogFileInfo"/> representing the log file information.
        /// </returns>
        /// <exception cref="System.ArgumentException"></exception>
        VirtualLogFileInfo GetVirtualFileById(LogFileId fileId);

        /// <summary>
        /// Gets the virtual file stream.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <returns>
        /// A <see cref="VirtualLogFileStream"/> bound to the region of the
        /// log device that corresponds with the specified log file information.
        /// </returns>
        VirtualLogFileStream GetVirtualFileStream(VirtualLogFileInfo info);

        /// <summary>
        /// Gets the root page.
        /// </summary>
        /// <typeparam name="T">
        /// The type of log root page to create.
        /// This type must be derived from <see cref="LogRootPage"/>
        /// </typeparam>
        /// <returns>
        /// A root page object.
        /// </returns>
        T GetRootPage<T>() where T : LogRootPage;
    }
}