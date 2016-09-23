using System;

namespace Zen.Tasks.Wix.InstanceService.InstallerDatabase
{
    /// <summary>
    /// 
    /// </summary>
    [Flags]
    public enum TransformValidation
    {
        /// <summary>
        /// 
        /// </summary>
        None = 0,
        /// <summary>
        /// 
        /// </summary>
        Language = 1,
        /// <summary>
        /// 
        /// </summary>
        Product = 2,
        /// <summary>
        /// 
        /// </summary>
        MajorVersion = 8,
        /// <summary>
        /// 
        /// </summary>
        MinorVersion = 16,
        /// <summary>
        /// 
        /// </summary>
        UpdateVersion = 32,
        /// <summary>
        /// 
        /// </summary>
        NewLessBaseVersion = 64,
        /// <summary>
        /// 
        /// </summary>
        NewLessEqualBaseVersion = 128,
        /// <summary>
        /// 
        /// </summary>
        NewEqualBaseVersion = 256,
        /// <summary>
        /// 
        /// </summary>
        NewGreaterEqualBaseVersion = 512,
        /// <summary>
        /// 
        /// </summary>
        NewGreaterBaseVersion = 1024,
        /// <summary>
        /// 
        /// </summary>
        UpgradeCode = 2048,
    }
}