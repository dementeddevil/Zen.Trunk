namespace Zen.Tasks.Wix.InstanceService.InstallerDatabase
{
    /// <summary>
    /// Defines the summary property IDs applicable to a Windows Installer
    /// summary information stream.
    /// </summary>
    public enum SummaryProperty
    {
        /// <summary>
        /// Defines the code page id used in the summary information stream.
        /// </summary>
        /// <remarks>
        /// VT_I2
        /// </remarks>
        CodePage = 1,

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// VT_
        /// </remarks>
        Title = 2,

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// VT_LPSTR
        /// </remarks>
        Subject = 3,

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// VT_LPSTR
        /// </remarks>
        Author = 4,

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// VT_LPSTR
        /// </remarks>
        Keywords = 5,

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// VT_LPSTR
        /// </remarks>
        Comments = 6,

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// VT_LPSTR
        /// </remarks>
        Template = 7,

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// VT_LPSTR
        /// </remarks>
        LastSavedBy = 8,

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// VT_LPSTR
        /// </remarks>
        RevisionNumber = 9,

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// VT_FILETIME
        /// </remarks>
        LastPrinted = 11,

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// VT_FILETIME
        /// </remarks>
        CreateTime = 12,

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// VT_FILETIME
        /// </remarks>
        LastSaveTime = 13,

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// VT_I4
        /// </remarks>
        PageCount = 14,

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// VT_I4
        /// </remarks>
        WordCount = 15,

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// VT_I4
        /// </remarks>
        CharacterCount = 16,

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// VT_LPSTR
        /// </remarks>
        CreatingApplication = 18,

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// VT_I4
        /// </remarks>
        Security = 19
    }
}