namespace Zen.Trunk.Storage.Log
{
	using System;

	[Serializable]
	public class ActiveTransaction
	{
		private readonly uint _transactionId;
		private readonly uint _fileId;
		private readonly uint _fileOffset;
		private readonly uint _firstLogId;

		public ActiveTransaction (uint transactionId, uint fileId, 
			uint fileOffset, uint firstLogId)
		{
			_transactionId = transactionId;
			_fileId = fileId;
			_fileOffset = fileOffset;
			_firstLogId = firstLogId;
		}

		public uint TransactionId => _transactionId;

	    public uint FileId => _fileId;

	    public uint FileOffset => _fileOffset;

	    public uint FirstLogId => _firstLogId;
	}
}
