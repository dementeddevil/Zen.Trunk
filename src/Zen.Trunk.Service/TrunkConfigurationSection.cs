using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Win32;
using Zen.Trunk.Storage.Configuration;

namespace Zen.Trunk.Service
{
    /// <summary>
    /// 
    /// </summary>
    public class TrunkConfigurationSection : ITrunkConfigurationSection
    {
        private readonly IDictionary<string, TrunkConfigurationSection> _subSections =
            new Dictionary<string, TrunkConfigurationSection>(StringComparer.OrdinalIgnoreCase);
        private RegistryKey _instanceKey;
        private RegistryKey _globalKey;

        /// <summary>
        /// Initializes a new instance of the <see cref="TrunkConfigurationSection" /> class.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <param name="global">The global.</param>
        /// <param name="writable">if set to <c>true</c> [writable].</param>
        public TrunkConfigurationSection(RegistryKey instance, RegistryKey global, bool writable)
        {
            _instanceKey = instance;
            _globalKey = global;
            IsReadOnly = !writable;
        }

        /// <summary>
        /// Gets the <see cref="TrunkConfigurationSection"/> with the specified sub section.
        /// </summary>
        /// <value>
        /// The <see cref="TrunkConfigurationSection"/>.
        /// </value>
        /// <param name="subSection">The sub section.</param>
        /// <returns></returns>
        public ITrunkConfigurationSection this[string subSection]
        {
            get
            {
                TrunkConfigurationSection section;
                if (!_subSections.TryGetValue(subSection, out section))
                {
                    // ReSharper disable once JoinDeclarationAndInitializer
                    RegistryKey subInstanceKey, subGlobalKey = null;
                    subInstanceKey = IsReadOnly
                        ? _instanceKey.OpenSubKey(subSection, false)
                        : _instanceKey.CreateSubKey(subSection, true);
                    try
                    {
                        subGlobalKey = IsReadOnly
                            ? _globalKey?.OpenSubKey(subSection, !IsReadOnly)
                            : _globalKey?.CreateSubKey(subSection, true);
                    }
                    // ReSharper disable once EmptyGeneralCatchClause
                    catch
                    {
                    }
                    section = new TrunkConfigurationSection(subInstanceKey, subGlobalKey, !IsReadOnly);
                    _subSections.Add(subSection, section);
                }
                return section;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is read only.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is read only; otherwise, <c>false</c>.
        /// </value>
        public bool IsReadOnly { get; }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

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
        public TResult GetValue<TResult>(
            string keyName,
            TResult defaultValue = default(TResult),
            bool allowFallback = true)
        {
            var value = _instanceKey.GetValue(keyName);
            if (value == null && allowFallback && _globalKey != null)
            {
                value = _globalKey.GetValue(keyName);
            }
            if (value == null)
            {
                value = defaultValue;
            }
            return (TResult)value;
        }

        /// <summary>
        /// Gets the instance value.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="keyName">Name of the key.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns></returns>
        public TResult GetInstanceValue<TResult>(string keyName, TResult defaultValue = default(TResult))
        {
            return (TResult)_instanceKey.GetValue(keyName, defaultValue);
        }

        /// <summary>
        /// Gets the global value.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="keyName">Name of the key.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns></returns>
        public TResult GetGlobalValue<TResult>(string keyName, TResult defaultValue = default(TResult))
        {
            if (_globalKey == null)
            {
                return defaultValue;
            }

            return (TResult)_globalKey.GetValue(keyName, defaultValue);
        }

        /// <summary>
        /// Sets the instance value.
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="keyName">Name of the key.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public void SetInstanceValue<TValue>(string keyName, TValue value)
        {
            if (IsReadOnly)
            {
                throw new ReadOnlyException();
            }

            _instanceKey.SetValue(keyName, value);
        }

        /// <summary>
        /// Sets the global value.
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="keyName">Name of the key.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public void SetGlobalValue<TValue>(string keyName, TValue value)
        {
            if (IsReadOnly)
            {
                throw new ReadOnlyException();
            }

            _globalKey.SetValue(keyName, value);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing">
        ///   <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var subSection in _subSections.Values)
                {
                    subSection.Dispose();
                }

                _subSections.Clear();

                _globalKey?.Dispose();
                _instanceKey?.Dispose();
            }

            _globalKey = null;
            _instanceKey = null;
        }
    }
}