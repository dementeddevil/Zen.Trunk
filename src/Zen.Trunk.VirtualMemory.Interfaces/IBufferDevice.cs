using System;
using System.Threading.Tasks;

namespace Zen.Trunk.VirtualMemory
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    [CLSCompliant(false)]
	public interface IBufferDevice : IDisposable
	{
        /// <summary>
        /// Gets the buffer factory.
        /// </summary>
        /// <value>
        /// The buffer factory.
        /// </value>
        IVirtualBufferFactory BufferFactory
		{
			get;
		}

        /// <summary>
        /// Opens the asynchronous.
        /// </summary>
        /// <returns></returns>
        Task OpenAsync();

        /// <summary>
        /// Closes the asynchronous.
        /// </summary>
        /// <returns></returns>
        Task CloseAsync();
	}
}
