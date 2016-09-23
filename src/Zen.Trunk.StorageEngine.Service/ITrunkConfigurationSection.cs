using System;

namespace Zen.Trunk.StorageEngine.Service
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public interface ITrunkConfigurationSection : IDisposable
    {
        /// <summary>
        /// Gets the <see cref="TrunkConfigurationSection"/> with the specified sub section.
        /// </summary>
        /// <value>
        /// The <see cref="TrunkConfigurationSection"/>.
        /// </value>
        /// <param name="subSection">The sub section.</param>
        /// <returns></returns>
        ITrunkConfigurationSection this[string subSection] { get; }

        /// <summary>
        /// Gets the value for the configuration entry.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="keyName">Name of the configuration element.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <param name="allowFallback">
        /// if set to <c>true</c> then fallback to reading from global settings
        /// if instance value doesn't exist; otherwise <c>false</c>.
        /// </param>
        /// <returns></returns>
        TResult GetValue<TResult>(
            string keyName,
            TResult defaultValue = default(TResult),
            bool allowFallback = true);
    }
}