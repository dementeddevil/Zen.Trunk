using System.Collections.Generic;
using System.ComponentModel;

namespace Zen.Trunk.Service
{
    /// <summary>
    /// <c>InstanceServiceBase</c> extends <see cref="T:CommandLineServiceBase"/>
    /// by allowing the creation of instance based NT services.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Service names are composed of a <see cref="P:ServiceNamePrefix"/>,
    /// <see cref="P:InstanceName"/> and <see cref="P:ServiceNameSuffix"/>.
    /// </para>
    /// <para>
    /// To aid installation UI is provided to allow specification of the instance
    /// name as well as the log-in account to be specified during setup.
    /// </para>
    /// </remarks>
    public class InstanceServiceBase : CommandLineServiceBase
    {
        #region Private Fields
        private string _defaultServiceName = string.Empty;
        private string _serviceNamePrefix = string.Empty;
        private string _instanceName = string.Empty;
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets or sets the default service name.
        /// </summary>
        /// <value>The default service name.</value>
        /// <remarks>
        /// The default service name is used when an instance name is not specified.
        /// </remarks>
        [Category("Misc")]
        [Description("Gets/sets the default service name.")]
        public string DefaultServiceName
        {
            get
            {
                return _defaultServiceName;
            }
            set
            {
                if (_defaultServiceName != value)
                {
                    _defaultServiceName = value;
                    UpdateServiceName();
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is the default
        /// service instance.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is default instance; otherwise, <c>false</c>.
        /// </value>
        [Category("Misc")]
        [Description("Gets a value indicating whether this service is the default instance.")]
        public bool IsDefaultInstance => string.IsNullOrEmpty(InstanceName);

        /// <summary>
        /// Gets or sets the service name prefix.
        /// </summary>
        /// <value>The service name prefix.</value>
        /// <exception cref="T:ArgumentException">
        /// The <see cref="P:ServiceName" /> property is invalid.
        /// </exception>
        /// <exception cref="T:InvalidOperationException">
        /// The service has already been started. The <see cref="P:ServiceName"/>
        /// property cannot be changed once the service has started. </exception>
        [Category("Misc")]
        [Description("Specifies the service name for this service.")]
        public string ServiceNamePrefix
        {
            get
            {
                return _serviceNamePrefix;
            }
            set
            {
                if (_serviceNamePrefix != value)
                {
                    _serviceNamePrefix = value;
                    UpdateServiceName();
                }
            }
        }

        /// <summary>
        /// Gets or sets the name of the instance.
        /// </summary>
        /// <value>The name of the instance.</value>
        /// <exception cref="T:ArgumentException">
        /// The <see cref="P:ServiceName" /> property is invalid.
        /// </exception>
        /// <exception cref="T:InvalidOperationException">
        /// The service has already been started. The <see cref="P:ServiceName"/>
        /// property cannot be changed once the service has started. </exception>
        [Category("Misc")]
        [Description("Specifies the instance name for this service.")]
        public string InstanceName
        {
            get
            {
                return _instanceName;
            }
            set
            {
                if (_instanceName != value)
                {
                    _instanceName = value;
                    UpdateServiceName();
                }
            }
        }

        /// <summary>
        /// Gets or sets the short name used to identify the service to the
        /// system.
        /// </summary>
        /// <value></value>
        /// <returns>The name of the service.</returns>
        /// <exception cref="T:ArgumentException">
        /// Thrown if the <see cref="P:ServiceName" /> property is invalid.
        /// </exception>
        /// <exception cref="T:InvalidOperationException">
        /// Thrown if the service has already been started.
        /// The <see cref="P:ServiceName"/> property cannot be changed once the
        /// service has started.
        /// </exception>
        [Category("Misc")]
        [Description("Specifies the service name for this service.")]
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new string ServiceName
        {
            get
            {
                var serviceName = base.ServiceName;
                if (string.IsNullOrEmpty(serviceName))
                {
                    serviceName = base.ServiceName = _defaultServiceName;
                }
                return serviceName;
            }
            set
            {
                var instanceOffset = value.IndexOf('$');
                if (instanceOffset == -1)
                {
                    if (value == DefaultServiceName)
                    {
                        InstanceName = string.Empty;
                        base.ServiceName = value;
                        return;
                    }

                    if (!string.IsNullOrEmpty(ServiceNamePrefix))
                    {
                        InstanceName = value;
                    }
                    else
                    {
                        base.ServiceName = value;
                    }
                }
                else
                {
                    var newServicePrefix = ServiceNamePrefix;
                    var newInstanceName = InstanceName;
                    if (instanceOffset != -1)
                    {
                        newServicePrefix = value.Substring(0, instanceOffset);
                        newInstanceName = value.Substring(instanceOffset + 1);
                    }

                    ServiceNamePrefix = newServicePrefix;
                    InstanceName = newInstanceName;
                }
            }
        }
        #endregion

        #region Protected Properties
        /// <summary>
        /// Gets the instance name for event logging.
        /// </summary>
        /// <value>The instance name for event log.</value>
        protected string InstanceNameForEventLog => IsDefaultInstance ? string.Empty : $" - [{InstanceName}]";

        /// <summary>
        /// Gets the command line arguments.
        /// </summary>
        /// <value>
        /// The command line arguments.
        /// </value>
        protected override IEnumerable<CommandLineArgument> CommandLineArguments => new[]
        {
            new CommandLineArgument("S", "ServiceName", false, true),
            new CommandLineArgument("I", "InstanceName", false, true)
        };
        #endregion

        #region Private Properties
        /// <summary>
        /// Gets the service name internal.
        /// </summary>
        /// <value>The service name internal.</value>
        private string ServiceNameInternal
        {
            get
            {
                // If we don't have an instance name...
                if (string.IsNullOrEmpty(_instanceName))
                {
                    return DefaultServiceName;
                }

                return string.IsNullOrEmpty(_serviceNamePrefix) ? _instanceName : $"{_serviceNamePrefix}${_instanceName}";
            }
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Processes the command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="value">The value.</param>
        /// <param name="additionalParameters">The additional parameters.</param>
        protected override void ProcessCommand(CommandLineArgument command, string value, string[] additionalParameters)
        {
            bool updateServiceName = false;

            if (command.ShortName == "S")
            {
                ServiceName = value;
                updateServiceName = true;
            }
            else if (command.ShortName == "I")
            {
                InstanceName = value;
                updateServiceName = true;
            }

            if (updateServiceName)
            {
                UpdateServiceName();
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Updates the name of the service.
        /// </summary>
        private void UpdateServiceName()
        {
            var newName = ServiceNameInternal;
            if (!string.IsNullOrEmpty(newName))
            {
                base.ServiceName = newName;
            }
        }
        #endregion
    }
}
