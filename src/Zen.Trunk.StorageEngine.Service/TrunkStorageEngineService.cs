using System;
using System.Collections.Generic;
using Microsoft.Win32;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Zen.Trunk.Storage;
using Zen.Trunk.Storage.Data;
using Zen.Trunk.Storage.Locking;
using Zen.Trunk.Storage.Log;

namespace Zen.Trunk.StorageEngine.Service
{
    public partial class TrunkStorageEngineService : InstanceServiceBase
    {
        private Logger _globalLogger;

        public TrunkStorageEngineService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            // TODO: Initialise our custom configuration system (registry based)

            // Initialise logging framework
            // TODO: Determine sub-system logging settings from configuration system.
            var globalLoggingSwitch = new LoggingLevelSwitch(LogEventLevel.Warning);
            var virtualMemoryLoggingSwitch = new LoggingLevelSwitch(LogEventLevel.Information);
            var dataMemoryLoggingSwitch = new LoggingLevelSwitch(LogEventLevel.Information);
            var lockingLoggingSwitch = new LoggingLevelSwitch(LogEventLevel.Error);
            var logWriterLoggingSwitch = new LoggingLevelSwitch(LogEventLevel.Warning);

            var loggerConfig = new LoggerConfiguration()
                .Enrich.WithProperty("ServiceName", ServiceName)
                .MinimumLevel.ControlledBy(globalLoggingSwitch)
                .MinimumLevel.Override(typeof(VirtualPageId).Namespace, virtualMemoryLoggingSwitch)
                .MinimumLevel.Override(typeof(DataPage).Namespace, dataMemoryLoggingSwitch)
                .MinimumLevel.Override(typeof(IGlobalLockManager).Namespace, lockingLoggingSwitch)
                .MinimumLevel.Override(typeof(LogPage).Namespace, logWriterLoggingSwitch);
            _globalLogger = loggerConfig.CreateLogger();
        }

        protected override void OnStop()
        {
        }
    }

    public class TrunkConfigurationSection
    {
        private readonly IDictionary<string, TrunkConfigurationSection> _subSections =
            new Dictionary<string, TrunkConfigurationSection>(StringComparer.OrdinalIgnoreCase);
        private RegistryKey _instanceKey;
        private RegistryKey _globalKey;

        /// <summary>
        /// Initializes a new instance of the <see cref="TrunkConfigurationSection"/> class.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <param name="global">The global.</param>
        public TrunkConfigurationSection(RegistryKey instance, RegistryKey global)
        {
            _instanceKey = instance;
            _globalKey = global;
        }

        /// <summary>
        /// Gets the <see cref="TrunkConfigurationSection"/> with the specified sub section.
        /// </summary>
        /// <value>
        /// The <see cref="TrunkConfigurationSection"/>.
        /// </value>
        /// <param name="subSection">The sub section.</param>
        /// <returns></returns>
        public TrunkConfigurationSection this[string subSection]
        {
            get
            {
                TrunkConfigurationSection section;
                if (!_subSections.TryGetValue(subSection, out section))
                {
                    // ReSharper disable once JoinDeclarationAndInitializer
                    RegistryKey subInstanceKey, subGlobalKey = null;
                    subInstanceKey = _instanceKey.OpenSubKey(subSection);
                    try
                    {
                        subGlobalKey = _globalKey?.OpenSubKey(subSection);
                    }
                    catch
                    {
                    }
                    section = new TrunkConfigurationSection(subInstanceKey, subGlobalKey);
                    _subSections.Add(subSection, section);
                }
                return section;
            }
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
    }

    /// <summary>
    /// 
    /// </summary>
    public class TrunkConfigurationManager
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
        public TrunkConfigurationSection Root { get; }
    }
}
