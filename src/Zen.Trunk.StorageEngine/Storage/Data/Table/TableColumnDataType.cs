namespace Zen.Trunk.Storage.Data.Table
{
    /// <summary>
    /// 
    /// </summary>
    public enum TableColumnDataType
    {
        /// <summary>
        /// Error trapping state
        /// </summary>
        None = 0,

        /// <summary>
        /// Bit - consumes eight bytes unless grouped with other bit fields
        /// </summary>
        Bit = 1,

        /// <summary>
        /// Byte - consumes 1 byte
        /// </summary>
        Byte = 2,

        /// <summary>
        /// Short - consumes 2 bytes
        /// </summary>
        Short = 3,

        /// <summary>
        /// Int - consumes 4 bytes
        /// </summary>
        Int = 4,

        /// <summary>
        /// Long - consumes 8 bytes
        /// </summary>
        Long = 5,

        /// <summary>
        /// Float - single precision number 6 bytes
        /// </summary>
        Float = 6,

        /// <summary>
        /// Double - double precision number 8 bytes
        /// </summary>
        Double = 7,

        /// <summary>
        /// Money - currency format 8 bytes
        /// </summary>
        Money = 8,

        /// <summary>
        /// DateTime - consumes 8 bytes
        /// </summary>
        DateTime = 9,

        /// <summary>
        /// Timestamp - consumes 8 bytes
        /// </summary>
        Timestamp = 10,

        /// <summary>
        /// Char - consumes 1 byte per character and fills the entire block
        /// </summary>
        Char = 11,

        /// <summary>
        /// VarChar - variable length storage at 1 byte per character
        /// </summary>
        VarChar = 12,

        /// <summary>
        /// NChar - consumes 2 bytes per character and fills the entire block
        /// </summary>
        NChar = 13,

        /// <summary>
        /// NVarChar - variable length storage at 2 bytes per character
        /// </summary>
        NVarChar = 14,

        /// <summary>
        /// Guid - 128 bits
        /// </summary>
        Guid = 15,
    }
}