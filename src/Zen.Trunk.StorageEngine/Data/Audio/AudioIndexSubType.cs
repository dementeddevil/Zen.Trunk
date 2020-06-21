namespace Zen.Trunk.Storage.Data.Audio
{
    /// <summary>
    /// AudioIndexSubType defines the sub-type of an audio index.
    /// </summary>
    public enum AudioIndexSubType : byte
    {
        /// <summary>
        /// Index is based on ordinal sample value
        /// </summary>
        Sample = 1,

        /// <summary>
        /// Index is based on the timecode
        /// </summary>
        Timecode = 2
    }
}

