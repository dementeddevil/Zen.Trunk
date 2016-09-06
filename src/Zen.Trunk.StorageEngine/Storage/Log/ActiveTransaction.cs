namespace Zen.Trunk.Storage.Log
{
	using System;

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
	public class ActiveTransaction
	{
		private readonly uint _transactionId;
		private readonly uint _fileId;
		private readonly uint _fileOffset;
		private readonly uint _firstLogId;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActiveTransaction"/> class.
        /// </summary>
        /// <param name="transactionId">The transaction identifier.</param>
        /// <param name="fileId">The file identifier.</param>
        /// <param name="fileOffset">The file offset.</param>
        /// <param name="firstLogId">The first log identifier.</param>
        public ActiveTransaction (uint transactionId, uint fileId, 
			uint fileOffset, uint firstLogId)
		{
			_transactionId = transactionId;
			_fileId = fileId;
			_fileOffset = fileOffset;
			_firstLogId = firstLogId;
		}

        /// <summary>
        /// Gets the transaction identifier.
        /// </summary>
        /// <value>
        /// The transaction identifier.
        /// </value>
        public uint TransactionId => _transactionId;

        /// <summary>
        /// Gets the file identifier.
        /// </summary>
        /// <value>
        /// The file identifier.
        /// </value>
        public uint FileId => _fileId;

        /// <summary>
        /// Gets the file offset.
        /// </summary>
        /// <value>
        /// The file offset.
        /// </value>
        public uint FileOffset => _fileOffset;

        /// <summary>
        /// Gets the first log identifier.
        /// </summary>
        /// <value>
        /// The first log identifier.
        /// </value>
        public uint FirstLogId => _firstLogId;
	}
}
