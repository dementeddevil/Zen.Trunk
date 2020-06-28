// ReSharper disable MissingXmlDoc

namespace Zen.Trunk.Storage.Configuration
{
    public static class ConfigurationNames
    {
        public const string DefaultDataFolder = "DefaultDataFolder";
        public const string DefaultLogFolder = "DefaultLogFolder";
        public const string MasterDataPathname = "MasterDataPathname";
        public const string MasterLogPathname = "MasterLogPathname";
        public const string ErrorLogPathname = "ErrorLogPathname";

        public static class Logging
        {
            public const string Section = "Logging";

            public const string GlobalLoggingSwitch = "Global";
            public const string VirtualMemoryLoggingSwitch = "VirtualMemory";
            public const string DataLoggingSwitch = "Data";
            public const string LockingLoggingSwitch = "Locking";
            public const string LogWriterLoggingSwitch = "LogWriter";
        }

        public static class VirtualMemory
        {
            public const string Section = "VirtualMemory";

            public const string ReservationPageCount = "ReservationPageCount";
            public const string PagesPerCacheBlock = "PagesPerCacheBlock";
        }
    }
}
