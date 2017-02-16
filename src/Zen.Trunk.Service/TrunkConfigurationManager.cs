using System;
using Microsoft.Win32;
using Zen.Trunk.Storage.Configuration;

namespace Zen.Trunk.Service
{
    /// <summary>
    /// 
    /// </summary>
    public class TrunkConfigurationManager : ITrunkConfigurationManager
    {
        private const string GlobalRootRegistryKeyPathBase = "Software\\Zen Design Software\\Trunk";

        /// <summary>
        /// Initializes a new instance of the <see cref="TrunkConfigurationManager" /> class.
        /// </summary>
        /// <param name="serviceName">Name of the service.</param>
        /// <param name="writable">if set to <c>true</c> [writable].</param>
        public TrunkConfigurationManager(string serviceName, bool writable)
        {
            var globalMachineKeyRoot = Registry.LocalMachine.OpenSubKey(
                GlobalRootRegistryKeyPathBase + "\\Global", writable);
            var instanceMachineKeyRoot = Registry.LocalMachine.OpenSubKey(
                GlobalRootRegistryKeyPathBase + "\\Instances\\" + serviceName, writable);
            Root = new TrunkConfigurationSection(instanceMachineKeyRoot, globalMachineKeyRoot, writable);
            IsReadOnly = !writable;
        }

        /// <summary>
        /// Gets the root.
        /// </summary>
        /// <value>
        /// The root.
        /// </value>
        public ITrunkConfigurationSection Root { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this instance is read only.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is read only; otherwise, <c>false</c>.
        /// </value>
        public bool IsReadOnly { get; }

        /// <summary>
        /// Performs application-defined tasks associated with freeing,
        /// releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing">
        /// <c>true</c> to release both managed and unmanaged resources;
        /// <c>false</c> to release only unmanaged resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Root.Dispose();
            }
            Root = null;
        }
    }
}