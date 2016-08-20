namespace Zen.Trunk.Storage.Log
{
	using System;

	[Serializable]
	public class ActiveTransaction
	{
		private uint _transactionId;
		private uint _fileId;
		private uint _fileOffset;
		private uint _firstLogId;

		public ActiveTransaction (uint transactionId, uint fileId, 
			uint fileOffset, uint firstLogId)
		{
			_transactionId = transactionId;
			_fileId = fileId;
			_fileOffset = fileOffset;
			_firstLogId = firstLogId;
		}

		public uint TransactionId
		{
			get
			{
				return _transactionId;
			}
		}

		public uint FileId
		{
			get
			{
				return _fileId;
			}
		}

		public uint FileOffset
		{
			get
			{
				return _fileOffset;
			}
		}

		public uint FirstLogId
		{
			get
			{
				return _firstLogId;
			}
		}
	}
}
