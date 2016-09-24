using System;

namespace Zen.Trunk.Storage.Configuration
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
        ITrunkConfigurationSection Root { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is read only.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is read only; otherwise, <c>false</c>.
        /// </value>
        bool IsReadOnly { get; }
    }
}