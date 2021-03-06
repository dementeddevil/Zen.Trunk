using System;
using System.Threading.Tasks;
using Zen.Trunk.VirtualMemory;

namespace Zen.Trunk.Storage.Data
{
    /// <summary>
    /// <c>ILogicalVirtualManager</c> defines the contract used for mapping
    /// <see cref="VirtualPageId"/> to <see cref="LogicalPageId"/> objects.
    /// </summary>
    public interface ILogicalVirtualManager : IDisposable
    {
        /// <summary>
        /// Gets a new logical page identifier.
        /// </summary>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation which
        /// once successfully resolved will return a <see cref="LogicalPageId"/>
        /// representing the new logical page identifier.
        /// </returns>
        Task<LogicalPageId> GetNewLogicalPageIdAsync();

        /// <summary>
        /// Adds a lookup between the specified virtual page id and logical page id.
        /// </summary>
        /// <param name="virtualPageId">
        /// A <see cref="VirtualPageId"/> representing the virtual page identifier.
        /// </param>
        /// <param name="logicalPageId">
        /// A <see cref="LogicalPageId"/> representing the logical page identifier.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        Task AddLookupAsync(VirtualPageId virtualPageId, LogicalPageId logicalPageId);

        /// <summary>
        /// Gets the logical page identifier that corresponds to the specified virtual page id.
        /// </summary>
        /// <param name="virtualPageId"></param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation which
        /// once successfully resolved will return a <see cref="LogicalPageId"/>
        /// representing the logical page identifier.
        /// </returns>
        Task<LogicalPageId> GetLogicalAsync(VirtualPageId virtualPageId);

        /// <summary>
        /// Gets the virtual page identifier that corresponds to the specified logical id.
        /// </summary>
        /// <param name="logicalPageId">
        /// A <see cref="LogicalPageId"/> representing the logical page identifier.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation which
        /// once successfully resolved will return a <see cref="VirtualPageId"/>
        /// representing the virtual page identifier.
        /// </returns>
        Task<VirtualPageId> GetVirtualAsync(LogicalPageId logicalPageId);
    }
}