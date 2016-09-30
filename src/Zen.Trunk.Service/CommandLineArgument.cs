namespace Zen.Trunk.Service
{
    public class CommandLineArgument
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLineArgument" /> class.
        /// </summary>
        /// <param name="shortName">The short name.</param>
        /// <param name="longName">The long name.</param>
        /// <param name="isRequired">if set to <c>true</c> [is required].</param>
        /// <param name="hasValueAfterCommandName">if set to <c>true</c> [has value after command name].</param>
        /// <param name="additionalArgumentCount">The additional command count.</param>
        public CommandLineArgument(string shortName, string longName = null, bool isRequired = false, bool hasValueAfterCommandName = false, int additionalArgumentCount = 0)
        {
            ShortName = shortName;
            LongName = longName;
            IsRequired = isRequired;
            HasValueAfterCommandName = hasValueAfterCommandName;
            AdditionalArgumentCount = additionalArgumentCount;
        }

        /// <summary>
        /// Gets the short name.
        /// </summary>
        /// <value>
        /// The short name.
        /// </value>
        public string ShortName { get; }

        /// <summary>
        /// Gets the long name.
        /// </summary>
        /// <value>
        /// The long name.
        /// </value>
        public string LongName { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is required.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is required; otherwise, <c>false</c>.
        /// </value>
        public bool IsRequired { get; }

        /// <summary>
        /// Gets a value indicating whether the command name is followed by a value
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance has value after command name; otherwise, <c>false</c>.
        /// </value>
        public bool HasValueAfterCommandName { get; }

        /// <summary>
        /// Gets the number of additional arguments that follow the command.
        /// </summary>
        /// <value>
        /// The additional command count.
        /// </value>
        public int AdditionalArgumentCount { get; }
    }
}