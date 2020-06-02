using System.Threading.Tasks;
using Autofac;

namespace Zen.Trunk.Storage
{
    /// <summary>
    /// 
    /// </summary>
	public interface IMountableDevice
	{
        /// <summary>
        /// Associates a lifetime scope with the device
        /// </summary>
        /// <param name="scope"></param>
        void InitialiseDeviceLifetimeScope(ILifetimeScope scope);

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
