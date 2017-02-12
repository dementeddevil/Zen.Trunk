using System;

namespace Zen.Trunk.Storage
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public interface IVirtualBufferFactory : IDisposable
	{
        /// <summary>
        /// Gets a value indicating whether this instance is nearly full.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is nearly full; otherwise, <c>false</c>.
        /// </value>
        bool IsNearlyFull
		{
			get;
		}

        /// <summary>
        /// Gets the size of the buffer.
        /// </summary>
        /// <value>
        /// The size of the buffer.
        /// </value>
        int BufferSize
		{
			get;
		}

        /// <summary>
        /// Allocates the buffer.
        /// </summary>
        /// <returns></returns>
        IVirtualBuffer AllocateBuffer();
	}
}
