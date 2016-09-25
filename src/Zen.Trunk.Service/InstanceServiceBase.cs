using System;
using System.ComponentModel;
using System.ServiceProcess;

namespace Zen.Trunk.Service
{
    /// <summary>
	/// <c>InstanceServiceBase</c> extends <see cref="T:ServiceBase"/>
	/// and <see cref="T:ServiceBase"/> by allowing the creation of instance based
	/// NT services.
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
	public class InstanceServiceBase : ServiceBase
	{
		#region Private Fields
		private string _defaultServiceName = string.Empty;
		private string _serviceNamePrefix = string.Empty;
		private string _instanceName = string.Empty;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="InstanceServiceBase"/> class.
		/// </summary>
		public InstanceServiceBase()
		{
		}
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
		/// The <see cref="P:ServiceName" /> property is invalid.
		/// </exception>
		/// <exception cref="T:InvalidOperationException">
		/// The service has already been started. The <see cref="P:ServiceName"/>
		/// property cannot be changed once the service has started. </exception>
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
					serviceName = _defaultServiceName;
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

		#region Public Methods
		/// <summary>
		/// Initializes the service process class prior to being passed to
		/// the Service Control Manager (SCM).
		/// </summary>
		/// <param name="args">The service startup arguments.</param>
		public virtual void Initialize(string[] args)
		{
			var updateServiceName = false;
			foreach (var arg in args)
			{
				// Strip any quote markers
				var processedArg = arg;
				if (processedArg[0] == '\"' && processedArg[processedArg.Length - 1] == '\"')
				{
					processedArg = arg.Substring(1, processedArg.Length - 2).Trim();
				}
				if (string.IsNullOrEmpty(processedArg))
				{
					continue;
				}

				// Look for argument switch prefix
				if (processedArg[0] == '/' || processedArg[0] == '-')
				{
					processedArg = processedArg.Substring(1).Trim();

					if (processedArg.StartsWith("ServiceName=", StringComparison.OrdinalIgnoreCase))
					{
						ServiceName = processedArg.Substring(12).Trim();
						updateServiceName = true;
						break;
					}
					if (processedArg.StartsWith("S=", StringComparison.OrdinalIgnoreCase))
					{
						ServiceName = processedArg.Substring(2).Trim();
						updateServiceName = true;
						break;
					}

					if (processedArg.StartsWith("InstanceName=", StringComparison.OrdinalIgnoreCase))
					{
						InstanceName = processedArg.Substring(13).Trim();
						updateServiceName = true;
						break;
					}
					if (processedArg.StartsWith("I=", StringComparison.OrdinalIgnoreCase))
					{
						InstanceName = processedArg.Substring(2).Trim();
						updateServiceName = true;
						break;
					}
				}
			}

			if (updateServiceName)
			{
				// Force a naming update
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
