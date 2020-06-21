using Zen.Trunk.Storage.BufferFields;
using Zen.Trunk.Storage.Data.Index;

namespace Zen.Trunk.Storage.Data.Audio
{
    /// <summary>
    /// RootAudioIndexInfo defines root index information for audio indices.
    /// </summary>
    public class RootAudioIndexInfo : RootIndexInfo
    {
        #region Private Fields
        private readonly BufferFieldByte _indexSubType;
        #endregion

        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="RootAudioIndexInfo"/> class.
        /// </summary>
        public RootAudioIndexInfo()
        {
            _indexSubType = new BufferFieldByte(base.LastField);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RootAudioIndexInfo"/> class.
        /// </summary>
        /// <param name="indexId">The index identifier.</param>
        public RootAudioIndexInfo(IndexId indexId)
            : base(indexId)
        {
            _indexSubType = new BufferFieldByte(base.LastField);
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets or sets the sub-type of the table index.
        /// </summary>
        /// <value>The type of the index sub.</value>
        public AudioIndexSubType IndexSubType
        {
            get => (AudioIndexSubType)_indexSubType.Value;
            set => _indexSubType.Value = (byte)value;
        }
        #endregion

        #region Protected Properties
        /// <summary>
        /// Gets the last buffer field object.
        /// </summary>
        /// <value>
        /// A <see cref="T:BufferField" /> object.
        /// </value>
        protected override BufferField LastField => _indexSubType;
        #endregion
    }
}

