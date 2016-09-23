using System;
using Microsoft.Win32;

namespace Zen.Trunk.StorageEngine.Service
{
    /// <summary>
    /// 
    /// </summary>
    public class TrunkConfigurationManager : ITrunkConfigurationManager
    {
        private const string GlobalRootRegistryKeyPathBase = "Software\\Zen Design Software\\Trunk";

        /// <summary>
        /// Initializes a new instance of the <see cref="TrunkConfigurationManager"/> class.
        /// </summary>
        /// <param name="serviceName">Name of the service.</param>
        public TrunkConfigurationManager(string serviceName)
        {
            var globalMachineKeyRoot = Registry.LocalMachine.OpenSubKey(
                GlobalRootRegistryKeyPathBase + "\\Global");
            var instanceMachineKeyRoot = Registry.LocalMachine.OpenSubKey(
                GlobalRootRegistryKeyPathBase + "\\Instances\\" + serviceName);
            Root = new TrunkConfigurationSection(instanceMachineKeyRoot, globalMachineKeyRoot);
        }

        /// <summary>
        /// Gets the root.
        /// </summary>
        /// <value>
        /// The root.
        /// </value>
        public TrunkConfigurationSection Root { get; private set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

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