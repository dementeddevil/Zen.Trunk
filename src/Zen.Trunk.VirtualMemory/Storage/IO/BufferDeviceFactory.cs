namespace Zen.Trunk.Storage.IO
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Zen.Trunk.Storage.IBufferDeviceFactory" />
    public class BufferDeviceFactory : IBufferDeviceFactory
    {
        private readonly IVirtualBufferFactory _bufferFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="BufferDeviceFactory"/> class.
        /// </summary>
        /// <param name="bufferFactory">The buffer factory.</param>
        public BufferDeviceFactory(IVirtualBufferFactory bufferFactory)
        {
            _bufferFactory = bufferFactory;
        }

        /// <summary>
        /// Creates the single buffer device.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="pathname">The pathname.</param>
        /// <param name="createPageCount">The create page count.</param>
        /// <param name="enableScatterGatherIo">if set to <c>true</c> [enable scatter gather io].</param>
        /// <returns></returns>
        public ISingleBufferDevice CreateSingleBufferDevice(
            string name, string pathname, uint createPageCount, bool enableScatterGatherIo)
        {
            return new SingleBufferDevice(
                _bufferFactory, name, pathname, createPageCount, enableScatterGatherIo);
        }

        /// <summary>
        /// Creates the multiple buffer device.
        /// </summary>
        /// <param name="enableScatterGatherIo">if set to <c>true</c> [enable scatter gather io].</param>
        /// <returns></returns>
        public IMultipleBufferDevice CreateMultipleBufferDevice(bool enableScatterGatherIo)
        {
            return new MultipleBufferDevice(_bufferFactory, this, enableScatterGatherIo);
        }
    }
}
