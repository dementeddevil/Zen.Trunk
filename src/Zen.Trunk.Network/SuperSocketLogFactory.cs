using SuperSocket.SocketBase.Logging;

namespace Zen.Trunk.Network
{
    /// <summary>
    /// <c>SuperSocketLogFactory</c> acts as a link to our liblog logging block
    /// </summary>
    /// <seealso cref="SuperSocket.SocketBase.Logging.ILogFactory" />
    public class SuperSocketLogFactory : ILogFactory
    {
        /// <summary>
        /// Gets the log by name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public ILog GetLog(string name)
        {
            return new SuperSocketLog(Zen.Trunk.Logging.LogProvider.GetLogger(name));
        }
    }
}