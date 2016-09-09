using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Zen.Trunk.Storage
{
    /// <summary>
    /// <c>AttachDatabaseParameters</c> defines the message payload used when
    /// attaching a database to the Master Database Device.
    /// </summary>
    public class AttachDatabaseParameters
    {
        #region Private Fields
        private readonly IDictionary<string, IList<FileSpec>> _fileGroups =
            new Dictionary<string, IList<FileSpec>>(StringComparer.OrdinalIgnoreCase);
        private readonly List<FileSpec> _logFiles = new List<FileSpec>();
        #endregion

        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="AttachDatabaseParameters"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="isCreate">if set to <c>true</c> [is create].</param>
        public AttachDatabaseParameters(string name, bool isCreate = false)
        {
            Name = name;
            IsCreate = isCreate;
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the database name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is create.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is create; otherwise, <c>false</c>.
        /// </value>
        public bool IsCreate { get; }

        /// <summary>
        /// Gets a value indicating whether this instance has primary file group.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance has primary file group; otherwise, <c>false</c>.
        /// </value>
        public bool HasPrimaryFileGroup => _fileGroups.ContainsKey(StorageConstants.PrimaryFileGroupName);

        /// <summary>
        /// Gets the file groups.
        /// </summary>
        /// <value>
        /// The file groups.
        /// </value>
        public IDictionary<string, IList<FileSpec>> FileGroups => new ReadOnlyDictionary<string, IList<FileSpec>>(_fileGroups);

        /// <summary>
        /// Gets the log files.
        /// </summary>
        /// <value>
        /// The log files.
        /// </value>
        public ICollection<FileSpec> LogFiles => _logFiles.AsReadOnly();
        #endregion

        #region Public Methods
        /// <summary>
        /// Adds the data file.
        /// </summary>
        /// <param name="fileGroup">The file group.</param>
        /// <param name="file">The file.</param>
        /// <exception cref="ArgumentException">
        /// Data file must have unique filename.
        /// or
        /// Data file must have unique logical name.
        /// </exception>
        public void AddDataFile(string fileGroup, FileSpec file)
        {
            // Find or create filegroup entry
            IList<FileSpec> files;
            if (!_fileGroups.TryGetValue(fileGroup, out files))
            {
                files = new List<FileSpec>();
                _fileGroups.Add(fileGroup, files);
            }

            // Validate files have unique filename
            if (files.Any(item => string.Equals(item.FileName, file.FileName, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException("Data file must have unique filename.");
            }

            // Validate files have unique name
            if (files.Any(item => string.Equals(item.Name, file.Name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException("Data file must have unique logical name.");
            }

            files.Add(file);
        }

        /// <summary>
        /// Adds the log file.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <exception cref="ArgumentException">
        /// Log file must have unique filename.
        /// or
        /// Log file must have unique logical name.
        /// </exception>
        public void AddLogFile(FileSpec file)
        {
            // Validate files have unique filename
            if (_logFiles.Any(item => string.Equals(item.FileName, file.FileName, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException("Log file must have unique filename.");
            }

            // Validate files have unique name
            if (_logFiles.Any(item => string.Equals(item.Name, file.Name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException("Log file must have unique logical name.");
            }

            _logFiles.Add(file);
        } 
        #endregion
    }

    /// <summary>
    /// 
    /// </summary>
    public class ChangeDatabaseStatusParameters
    {
        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeDatabaseStatusParameters"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="isOnline">if set to <c>true</c> [is online].</param>
        public ChangeDatabaseStatusParameters(string name, bool isOnline)
        {
            Name = name;
            IsOnline = isOnline;
        } 
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is online.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is online; otherwise, <c>false</c>.
        /// </value>
        public bool IsOnline { get; } 
        #endregion
    }
}