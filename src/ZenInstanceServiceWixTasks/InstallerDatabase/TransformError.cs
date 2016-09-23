using System;

namespace Zen.Tasks.Wix.InstanceService.InstallerDatabase
{
    /// <summary>
    /// 
    /// </summary>
    [Flags]
    public enum TransformError
    {
        /// <summary>
        /// 
        /// </summary>
        None = 0,
        /// <summary>
        /// 
        /// </summary>
        AddExistingRow = 1,
        /// <summary>
        /// 
        /// </summary>
        DeleteMissingRow = 2,
        /// <summary>
        /// 
        /// </summary>
        AddExistingTable = 4,
        /// <summary>
        /// 
        /// </summary>
        DeleteMissingTable = 8,
        /// <summary>
        /// 
        /// </summary>
        UpdateMissingRow = 16,
        /// <summary>
        /// 
        /// </summary>
        ChangeCodePage = 32,
    }
}