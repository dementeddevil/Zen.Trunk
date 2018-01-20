namespace Zen.Trunk.Storage
{
    /// <summary>
    /// <c>StorageConstants</c> defines constants used throughout the storage engine
    /// </summary>
    public static class StorageConstants
    {
        /// <summary>
        /// The primary file group primary device file extension
        /// </summary>
        /// <remarks>
        /// Only used for the primary device on the primary file-group in a database.
        /// 
        /// </remarks>
        public const string PrimaryFileGroupPrimaryDeviceFileExtension = ".mddf";

        /// <summary>
        /// The primary device file extension
        /// </summary>
        public const string PrimaryDeviceFileExtension = ".mfdf";

        /// <summary>
        /// The secondary device file extension
        /// </summary>
        public const string SecondaryDeviceFileExtension = ".sdf";

        /// <summary>
        /// The primary file group => primary device filename
        /// </summary>
        public const string PrimaryFileGroupPrimaryDeviceFilename = "master";

        /// <summary>
        /// The primary file group => primary device name
        /// </summary>
        public const string PrimaryFileGroupPrimaryDeviceName = "MASTER";

        /// <summary>
        /// The primary file group name
        /// </summary>
        public const string PrimaryFileGroupName = "PRIMARY";

        /// <summary>
        /// The data filename suffix
        /// </summary>
        public const string DataFilenameSuffix = "_data";

        /// <summary>
        /// The log filename suffix
        /// </summary>
        public const string LogFilenameSuffix = "_log";
        
        /// <summary>
        /// The master log file device extension
        /// </summary>
        public const string MasterLogFileDeviceExtension = ".mlf";

        /// <summary>
        /// The master log file device extension
        /// </summary>
        public const string SlaveLogFileDeviceExtension = ".slf";

        /// <summary>
        /// The master database name
        /// </summary>
        public const string MasterDatabaseName = "master";

        /// <summary>
        /// The model database name
        /// </summary>
        public const string ModelDatabaseName = "model";

        /// <summary>
        /// The temporary database name
        /// </summary>
        public const string TemporaryDatabaseName = "tempdb";
    }
}
