namespace Zen.Trunk.Storage.Log
{
    /// <summary>
    /// 
    /// </summary>
	public class ActiveTransaction : BufferFieldWrapper
    {
        private readonly BufferFieldUInt32 _transactionId;
		private readonly BufferFieldLogFileId _fileId;
		private readonly BufferFieldUInt32 _fileOffset;
		private readonly BufferFieldUInt32 _firstLogId;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActiveTransaction"/> class.
        /// </summary>
        public ActiveTransaction()
        {
            _transactionId = new BufferFieldUInt32();
            _fileId = new BufferFieldLogFileId(_transactionId);
            _fileOffset = new BufferFieldUInt32(_fileId);
            _firstLogId = new BufferFieldUInt32(_fileOffset);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ActiveTransaction"/> class.
        /// </summary>
        /// <param name="transactionId">The transaction identifier.</param>
        /// <param name="fileId">The file identifier.</param>
        /// <param name="fileOffset">The file offset.</param>
        /// <param name="firstLogId">The first log identifier.</param>
        public ActiveTransaction (
            uint transactionId,
            LogFileId fileId, 
			uint fileOffset,
            uint firstLogId)
		{
			_transactionId = new BufferFieldUInt32(transactionId);
			_fileId = new BufferFieldLogFileId(_transactionId, fileId);
			_fileOffset = new BufferFieldUInt32(_fileId, fileOffset);
			_firstLogId = new BufferFieldUInt32(_fileOffset, firstLogId);
		}

        /// <summary>
        /// Gets the transaction identifier.
        /// </summary>
        /// <value>
        /// The transaction identifier.
        /// </value>
        public uint TransactionId => _transactionId.Value;

        /// <summary>
        /// Gets the file identifier.
        /// </summary>
        /// <value>
        /// The file identifier.
        /// </value>
        public LogFileId FileId => _fileId.Value;

        /// <summary>
        /// Gets the file offset.
        /// </summary>
        /// <value>
        /// The file offset.
        /// </value>
        public uint FileOffset => _fileOffset.Value;

        /// <summary>
        /// Gets the first log identifier.
        /// </summary>
        /// <value>
        /// The first log identifier.
        /// </value>
        public uint FirstLogId => _firstLogId.Value;

        /// <summary>
        /// Gets the first buffer field object.
        /// </summary>
        /// <value>
        /// A <see cref="T:BufferField" /> object.
        /// </value>
        protected override BufferField FirstField => _transactionId;

        /// <summary>
        /// Gets the last buffer field object.
        /// </summary>
        /// <value>
        /// A <see cref="T:BufferField" /> object.
        /// </value>
        protected override BufferField LastField => _firstLogId;
    }
}
