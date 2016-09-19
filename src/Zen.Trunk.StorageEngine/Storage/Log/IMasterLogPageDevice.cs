using System.Collections.Generic;
using System.Threading.Tasks;

namespace Zen.Trunk.Storage.Log
{
    /// <summary>
    /// <c>IMasterLogPageDevice</c> extends <see cref="ILogPageDevice"/>
    /// to provide overarching support for tracking active log file and offset.
    /// </summary>
    /// <seealso cref="Zen.Trunk.Storage.Log.ILogPageDevice" />
    public interface IMasterLogPageDevice : ILogPageDevice
    {
        /// <summary>
        /// Gets the current log file identifier.
        /// </summary>
        /// <value>
        /// The current log file identifier.
        /// </value>
        LogFileId CurrentLogFileId { get; }

        /// <summary>
        /// Gets the current log file offset.
        /// </summary>
        /// <value>
        /// The current log file offset.
        /// </value>
        uint CurrentLogFileOffset { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is truncating log.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is truncating log; otherwise, <c>false</c>.
        /// </value>
        bool IsTruncatingLog { get; }

        /// <summary>
        /// Adds a log device based on supplied parameters.
        /// </summary>
        /// <param name="deviceParams">The device parameters.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        Task<DeviceId> AddDeviceAsync(AddLogDeviceParameters deviceParams);

        /// <summary>
        /// Removes the device based on the supplied parameters.
        /// </summary>
        /// <param name="deviceParams">The device parameters.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        Task RemoveDeviceAsync(RemoveLogDeviceParameters deviceParams);

        /// <summary>
        /// Writes the entry.
        /// </summary>
        /// <param name="entry">The entry.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        /// <exception cref="BufferDeviceShuttingDownException"></exception>
        Task WriteEntryAsync(LogEntry entry);

        /// <summary>
        /// Performs database recovery.
        /// </summary>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        /// <exception cref="BufferDeviceShuttingDownException"></exception>
        Task PerformRecoveryAsync();

        /// <summary>
        /// Gets the next transaction identifier.
        /// </summary>
        /// <returns></returns>
        TransactionId GetNextTransactionId();

        /// <summary>
        /// Performs rollforward operations on a list of transaction log entries.
        /// </summary>
        /// <remarks>
        /// The transactions are rolled back in reverse order.
        /// </remarks>
        /// <param name="transactions"></param>
        Task CommitTransactionsAsync(List<TransactionLogEntry> transactions);

        /// <summary>
        /// Performs rollback operations on a list of transaction log entries.
        /// </summary>
        /// <remarks>
        /// The transactions are rolled back in reverse order.
        /// </remarks>
        /// <param name="transactions"></param>
        Task RollbackTransactionsAsync(List<TransactionLogEntry> transactions);
    }
}