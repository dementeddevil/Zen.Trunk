using Zen.Trunk.Storage.Data;

namespace Zen.Trunk.Storage
{
    /// <summary>
    /// <c>FileSpec</c> defines the characteristics for a data or log file.
    /// </summary>
    public class FileSpec
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the name of the file.
        /// </summary>
        /// <value>
        /// The name of the file.
        /// </value>
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets the size.
        /// </summary>
        /// <value>
        /// The size.
        /// </value>
        public FileSize? Size { get; set; }

        /// <summary>
        /// Gets or sets the maximum size.
        /// </summary>
        /// <value>
        /// The maximum size.
        /// </value>
        public FileSize? MaxSize { get; set; }

        /// <summary>
        /// Gets or sets the file growth.
        /// </summary>
        /// <value>
        /// The file growth.
        /// </value>
        public FileSize? FileGrowth { get; set; }
    }
}
