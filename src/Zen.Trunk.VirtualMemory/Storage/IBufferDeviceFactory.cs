namespace Zen.Trunk.Storage
{
    /// <summary>
    /// 
    /// </summary>
    public interface IBufferDeviceFactory
    {
        /// <summary>
        /// Creates the multiple buffer device.
        /// </summary>
        /// <param name="enableScatterGatherIo">if set to <c>true</c> [enable scatter gather io].</param>
        /// <returns></returns>
        IMultipleBufferDevice CreateMultipleBufferDevice(bool enableScatterGatherIo);

        /// <summary>
        /// Creates the single buffer device.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="pathname">The pathname.</param>
        /// <param name="createPageCount">The create page count.</param>
        /// <param name="enableScatterGatherIo">if set to <c>true</c> [enable scatter gather io].</param>
        /// <returns></returns>
        ISingleBufferDevice CreateSingleBufferDevice(string name, string pathname, uint createPageCount, bool enableScatterGatherIo);
    }
}