using Zen.Trunk.Utils;

namespace Zen.Trunk.Storage.IO
{
	using System;

    /// <summary>
    /// 
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
        public uint PhysicalPageId
		{
			get;
			private set;
		}

        /// <summary>
        /// Gets the buffer.
        /// </summary>
        /// <value>
        /// The buffer.
        /// </value>
        public IVirtualBuffer Buffer
		{
			get;
			private set;
		}
	}
}
