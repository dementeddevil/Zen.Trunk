namespace Zen.Trunk.Storage.Data.Audio
{
    public class AudioIndexLeafInfo : AudioIndexLogicalInfo
    {
        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="AudioIndexLeafInfo"/> class.
        /// </summary>
        public AudioIndexLeafInfo()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioIndexLeafInfo"/> class.
        /// </summary>
        /// <param name="value">The value.</param>
        public AudioIndexLeafInfo(long value) : base(value)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioIndexLeafInfo"/> class.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="logicalPageId">The logical page identifier.</param>
        public AudioIndexLeafInfo(long value, LogicalPageId logicalPageId) : base(value, logicalPageId)
        {
        }
        #endregion
    }
}

