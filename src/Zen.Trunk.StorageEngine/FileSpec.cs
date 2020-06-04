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

    public class FileSpecBuilder
    {
        private FileSpec _fileSpec;

        public FileSpecBuilder(string name, string fileName)
        {
            _fileSpec =
                new FileSpec
                {
                    Name = name,
                    FileName = fileName
                };
        }

        public FileSpecBuilder HavingSize(FileSize size)
        {
            _fileSpec.Size = size;
            return this;
        }

        public FileSpecBuilder HavingMaxSize(FileSize size)
        {
            _fileSpec.MaxSize = size;
            return this;
        }

        public FileSpecBuilder HavingFileGrowth(FileSize size)
        {
            _fileSpec.FileGrowth = size;
            return this;
        }

        public FileSpec Build()
        {
            return _fileSpec;
        }
    }
}
