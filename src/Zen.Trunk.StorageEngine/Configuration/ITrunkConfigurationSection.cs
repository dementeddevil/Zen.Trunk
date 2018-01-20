using System;

namespace Zen.Trunk.Storage.Configuration
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public interface ITrunkConfigurationSection : IDisposable
    {
        /// <summary>
        /// Gets the <see cref="ITrunkConfigurationSection"/> with the specified sub section.
        /// </summary>
        /// <value>
        /// The <see cref="ITrunkConfigurationSection"/>.
        /// </value>
        /// <param name="subSection">The sub section.</param>
        /// <returns></returns>
        ITrunkConfigurationSection this[string subSection] { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is read only.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is read only; otherwise, <c>false</c>.
        /// </value>
        bool IsReadOnly { get; }

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

        /// <summary>
        /// Gets the instance value.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="keyName">Name of the key.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns></returns>
        TResult GetInstanceValue<TResult>(
            string keyName,
            TResult defaultValue = default(TResult));

        /// <summary>
        /// Gets the global value.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="keyName">Name of the key.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns></returns>
        TResult GetGlobalValue<TResult>(
            string keyName,
            TResult defaultValue = default(TResult));

        /// <summary>
        /// Sets the instance value.
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="keyName">Name of the key.</param>
        /// <param name="value">The value.</param>
        void SetInstanceValue<TValue>(string keyName, TValue value);

        /// <summary>
        /// Sets the global value.
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="keyName">Name of the key.</param>
        /// <param name="value">The value.</param>
        void SetGlobalValue<TValue>(string keyName, TValue value);
    }
}