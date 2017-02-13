using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;

namespace Zen.Trunk.Service
{
    /// <summary>
    /// <c>CommandLineServiceBase</c> extends <see cref="T:ServiceBase"/>
    /// by supporting parsing of command line parameters.
    /// </summary>
    /// <seealso cref="System.ServiceProcess.ServiceBase" />
    public class CommandLineServiceBase : ServiceBase
    {
        #region Protected Properties
        /// <summary>
        /// Gets the command line arguments.
        /// </summary>
        /// <value>
        /// The command line arguments.
        /// </value>
        protected virtual IEnumerable<CommandLineArgument> CommandLineArguments => new CommandLineArgument[0];
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
            while (true)
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
                        ? (IEnumerable<string>)new string[0]
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
        }
        #endregion
    }
}