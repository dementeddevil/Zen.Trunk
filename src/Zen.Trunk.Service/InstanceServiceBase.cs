using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
        protected virtual IEnumerable<CommandLineArgument> CommandLineArguments
        {
            get
            {
                return new[]
                {
                    new CommandLineArgument("S", "ServiceName", false, true),
                    new CommandLineArgument("I", "InstanceName", false, true)
                };
            }
        }
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
		    var index = 0;
		    var commands = CommandLineArguments.ToList();
		    var processedCommands = new HashSet<CommandLineArgument>();
            while(true)
            {
                // Get command from array
                var arg = args[index].Trim('\"');
				if (string.IsNullOrEmpty(arg))
				{
					continue;
				}

				// Look for command switch prefix
                if (arg[0] == '/' || arg[0] == '-')
                {
                    arg = arg.Substring(1);
                    string value = null;
                    CommandLineArgument command = null;
                    foreach (var candidateCommand in commands)
                    {
                        if (candidateCommand.HasValueAfterCommandName &&
                            arg.StartsWith($"{candidateCommand.ShortName}=", StringComparison.OrdinalIgnoreCase))
                        {
                            value = arg.Substring($"{candidateCommand.ShortName}=".Length);
                            command = candidateCommand;
                        }
                        else if (candidateCommand.HasValueAfterCommandName &&
                                 arg.StartsWith($"{candidateCommand.LongName}=", StringComparison.OrdinalIgnoreCase))
                        {
                            value = arg.Substring($"{candidateCommand.LongName}=".Length);
                            command = candidateCommand;
                        }
                        else if (!candidateCommand.HasValueAfterCommandName &&
                                 (arg.Equals(candidateCommand.ShortName, StringComparison.OrdinalIgnoreCase) ||
                                  arg.Equals(candidateCommand.LongName, StringComparison.OrdinalIgnoreCase)))
                        {
                            command = candidateCommand;
                        }

                        if (command != null)
                        {
                            break;
                        }
                    }

                    if (command == null)
                    {
                        throw new ArgumentException("Unsupported command command encountered", arg);
                    }

                    // Check whether additional arguments are available
                    if ((index + 1 + command.AdditionalArgumentCount) < args.Length)
                    {
                        throw new ArgumentException(
                            $"Argument {arg} requires {command.AdditionalArgumentCount} extra parameters that have not been given.",
                            arg);
                    }

                    // Determine additional arguments
                    var additionalArguments = (command.AdditionalArgumentCount == 0)
                        ? (IEnumerable<string>) new string[0]
                        : (IEnumerable<string>)
                        new ArraySegment<string>(args, index + 1, command.AdditionalArgumentCount);

                    // Call function to process each command (remembering to trim additional arguments)
                    ProcessCommand(
                        command,
                        value,
                        additionalArguments
                            .Select(a => a.Trim('\"'))
                            .ToArray());

                    // Update index, skipping any additional parameters
                    index += (1 + command.AdditionalArgumentCount);
                    processedCommands.Add(command);
                }
                else
                {
                    ProcessArgument(arg);

                    // Update index
                    ++index;
                }

                // Terminate command processing loop when we reach the end
				if (index >= args.Length)
				{
                    break;
				}
			}

            // Have all required parameters been specified?
		    foreach (var requiredCommand in commands.Where(c => c.IsRequired))
		    {
		        if (!processedCommands.Contains(requiredCommand))
		        {
		            throw new ArgumentException("Required command is missing", requiredCommand.ShortName);
		        }
		    }
		}
        #endregion

        #region Protected Methods
        /// <summary>
        /// Processes the argument.
        /// </summary>
        /// <param name="argument">The argument.</param>
        protected virtual void ProcessArgument(string argument)
	    {
	    }

        /// <summary>
        /// Processes the command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="value">The value.</param>
        /// <param name="additionalParameters">The additional parameters.</param>
        protected virtual void ProcessCommand(CommandLineArgument command, string value, string[] additionalParameters)
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
