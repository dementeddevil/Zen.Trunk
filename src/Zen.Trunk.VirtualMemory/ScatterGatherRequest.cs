using System;
using Zen.Trunk.Utils;

namespace Zen.Trunk.VirtualMemory
{
    /// <summary>
    /// <c>ScatterGatherRequest</c> encapsulates a single scatter or gather 
    /// request sent to a single buffer device.
    /// </summary>
    /// <seealso cref="TaskRequest{Object}" />
    [CLSCompliant(false)]
	public class ScatterGatherRequest : TaskRequest<object>
	{
        /// <summary>
        /// Initializes a new instance of the <see cref="ScatterGatherRequest"/> class.
        /// </summary>
        /// <param name="physicalPageId">The physical page identifier.</param>
        /// <param name="buffer">The buffer.</param>
        [CLSCompliant(false)]
		public ScatterGatherRequest(uint physicalPageId, IVirtualBuffer buffer)
		{
			PhysicalPageId = physicalPageId;
			Buffer = buffer;
		}

        /// <summary>
        /// Gets the physical page identifier.
        /// </summary>
        /// <value>
        /// The physical page identifier.
        /// </value>
        public uint PhysicalPageId { get; }

        /// <summary>
        /// Gets the buffer.
        /// </summary>
        /// <value>
        /// The buffer.
        /// </value>
        public IVirtualBuffer Buffer { get; }
    }
}
