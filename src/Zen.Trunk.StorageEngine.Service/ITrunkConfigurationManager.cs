using System;

namespace Zen.Trunk.StorageEngine.Service
{
    /// <summary>
    /// <c>ITrunkConfigurationManager</c> exposes the hierarchical configuration system.
    /// </summary>
    public interface ITrunkConfigurationManager : IDisposable
    {
        /// <summary>
        /// Gets the root configuration section.
        /// </summary>
        /// <value>
        /// The root.
        /// </value>
        TrunkConfigurationSection Root { get; }
    }
}