using System;
using System.Threading.Tasks;

namespace Zen.Trunk.Storage
{
    /// <summary>
    /// 
    /// </summary>
	public interface IMountableDevice
	{
        /// <summary>
        /// Opens the device asynchronously.
        /// </summary>
        /// <param name="isCreate">if set to <c>true</c> [is create].</param>
        /// <returns></returns>
        Task OpenAsync(bool isCreate);

        /// <summary>
        /// Closes the device asynchronously.
        /// </summary>
        /// <returns></returns>
        Task CloseAsync();
	}
}
