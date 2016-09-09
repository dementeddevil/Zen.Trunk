namespace Zen.Trunk.Storage
{
    public static class StorageFileConstants
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
        /// The log file device extension
        /// </summary>
        public const string LogFileDeviceExtension = ".ldf";
    }
}
