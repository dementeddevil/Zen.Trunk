namespace Zen.Trunk.Storage.Log
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Threading.Tasks;
	using Zen.Trunk.Storage.Data;
	using Zen.Trunk.Storage.IO;

	/// <summary>
	/// Identifies the valid types of <see cref="T:LogEntry"/> records
	/// that can be written or read from a transaction log.
	/// </summary>
	public enum LogEntryType
	{
		NoOp = 0,
		BeginCheckpoint = 1,
		EndCheckpoint = 2,
		BeginXact = 3,
		CommitXact = 4,
		RollbackXact = 5,
		CreatePage = 6,
		ModifyPage = 7,
		DeletePage = 8,
	}

	/// <summary>
	/// Defines basic information required for every transaction log entry.
	/// </summary>
	[Serializable]
	public class LogEntry : BufferFieldWrapper
	{
		#region Private Fields
		private LogEntryType logType;

		private readonly BufferFieldUInt32 _logId;
		private readonly BufferFieldUInt32 _lastLog;
		#endregion

		#region Public Constructors
		public LogEntry(LogEntryType logType)
			: this()
		{
			this.logType = logType;
		}

		protected LogEntry()
		{
			_logId = new BufferFieldUInt32();
			_lastLog = new BufferFieldUInt32(_logId);
		}
		#endregion

		#region Public Properties
		public virtual uint RawSize => (uint)(base.TotalFieldLength + 1);

	    public uint LogId
		{
			get
			{
				return _logId.Value;
			}
			set
			{
				_logId.Value = value;
			}
		}

		public uint LastLog
		{
			get
			{
				return _lastLog.Value;
			}
			set
			{
				_lastLog.Value = value;
			}
		}

		public LogEntryType LogType => logType;

	    #endregion

		#region Public Methods
		public static LogEntry ReadEntry(BufferReaderWriter streamManager)
		{
			LogEntry entry = null;
			var logType = ReadLogType(streamManager);
			switch (logType)
			{
				case LogEntryType.NoOp:
					entry = new NoOpLogEntry();
					break;
				case LogEntryType.BeginCheckpoint:
					entry = new BeginCheckPointLogEntry();
					break;
				case LogEntryType.EndCheckpoint:
					entry = new EndCheckPointLogEntry();
					break;
				case LogEntryType.BeginXact:
					entry = new BeginTransactionLogEntry();
					break;
				case LogEntryType.CommitXact:
					entry = new CommitTransactionLogEntry();
					break;
				case LogEntryType.RollbackXact:
					entry = new RollbackTransactionLogEntry();
					break;
				case LogEntryType.CreatePage:
					entry = new PageImageCreateLogEntry();
					break;
				case LogEntryType.ModifyPage:
					entry = new PageImageUpdateLogEntry();
					break;
				case LogEntryType.DeletePage:
					entry = new PageImageDeleteLogEntry();
					break;
				default:
					throw new InvalidOperationException("Illegal log entry type detected.");
			}
			entry.logType = logType;
			entry.Read(streamManager);
			return entry;
		}

		/// <summary>
		/// Performs the rollforward action on the associated page buffer from 
		/// this log record state.
		/// </summary>
		/// <param name="device">The device.</param>
		/// <returns></returns>
		public virtual Task RollForward(DatabaseDevice device)
		{
			return CompletedTask.Default;
		}

		/// <summary>
		/// Performs the rollback action on the associated page buffer from 
		/// this log record state.
		/// </summary>
		/// <param name="device">The device.</param>
		/// <returns></returns>
		public virtual Task RollBack(DatabaseDevice device)
		{
			return CompletedTask.Default;
		}
		#endregion

		#region Protected Properties
		protected override BufferField FirstField => _logId;

	    protected override BufferField LastField => _lastLog;

	    #endregion

		#region Protected Methods
		protected override void DoWrite(BufferReaderWriter streamManager)
		{
			streamManager.Write((byte)(int)logType);
			base.DoWrite(streamManager);
		}
		#endregion

		#region Private Methods
		private static LogEntryType ReadLogType(BufferReaderWriter streamManager)
		{
			return (LogEntryType)streamManager.ReadByte();
		}
		#endregion
	}

	/// <summary>
	/// Comparison class for testing equality of <see cref="T:LogEntry"/>
	/// objects.
	/// </summary>
	public class LogEntryComparer : IComparer<LogEntry>
	{
		#region Public Constructors
		public LogEntryComparer()
		{
		}
		#endregion

		#region IComparer<LogEntry> Members
		int IComparer<LogEntry>.Compare(LogEntry x, LogEntry y)
		{
			return x.LogId.CompareTo(y.LogId);
		}
		#endregion
	}

	/// <summary>
	/// Defines a No-Op log entry.
	/// </summary>
	/// <remarks>
	/// These log records are used exclusively by the logging sub-system
	/// when truncating a transaction log. These ensure that a log-file is
	/// filled with valid records during the process.
	/// </remarks>
	[Serializable]
	public class NoOpLogEntry : LogEntry
	{
		public NoOpLogEntry()
			: base(LogEntryType.NoOp)
		{
		}
	}

	/// <summary>
	/// Serves as a base class for a transaction log entry.
	/// </summary>
	/// <remarks>
	/// Transaction logs are stamped with the transaction Id and this Id is
	/// used to group all logs related to a given transaction to allow a
	/// transaction to be rolled forward or rolled back during recovery.
	/// </remarks>
	[Serializable]
	public class TransactionLogEntry : LogEntry
	{
		#region Private Fields
		private readonly BufferFieldUInt32 _transactionId;
		#endregion

		#region Public Constructors
		public TransactionLogEntry(LogEntryType logType)
			: base(logType)
		{
			_transactionId = new BufferFieldUInt32(base.LastField);
		}

		public TransactionLogEntry(LogEntryType logType, uint transactionId)
			: base(logType)
		{
			_transactionId = new BufferFieldUInt32(base.LastField, transactionId);
		}
		protected TransactionLogEntry()
		{
			_transactionId = new BufferFieldUInt32(base.LastField);
		}
		#endregion

		#region Public Properties
		public uint TransactionId => _transactionId.Value;

	    #endregion

		#region Protected Properties
		protected override BufferField LastField => _transactionId;

	    #endregion

		#region Internal Methods
		internal void RewriteTransactionId(uint transactionId)
		{
			_transactionId.Value = transactionId;
		}
		#endregion
	}

	/// <summary>
	/// Serves as a base class for a check-point log record.
	/// </summary>
	/// <remarks>
	/// Check-point records are used to ensure recovery does not take an
	/// excessive amount of time to be performed. During checkpointing
	/// the state of all open transactions is written to disk and all
	/// buffers which can be written to the backing store are written.
	/// </remarks>
	[Serializable]
	public class CheckPointLogEntry : LogEntry
	{
		#region Private Fields
		private List<ActiveTransaction> activeTransactions;
		#endregion

		#region Public Constructors
		public CheckPointLogEntry(LogEntryType logType)
			: base(logType)
		{
			// Sanity check
			if (logType != LogEntryType.BeginCheckpoint &&
				logType != LogEntryType.EndCheckpoint)
			{
				throw new ArgumentException("Invalid log entry type for checkpoint record.");
			}
		}
		#endregion

		#region Public Properties
		public override uint RawSize
		{
			get
			{
				var rawSize = base.RawSize + (uint)2;
				if (activeTransactions != null)
				{
					rawSize += (uint)(activeTransactions.Count * 14);
				}
				return rawSize;
			}
		}
		public int TransactionCount
		{
			get
			{
				if (activeTransactions == null)
				{
					return 0;
				}
				return activeTransactions.Count;
			}
		}

		public IEnumerable<ActiveTransaction> ActiveTransactions => activeTransactions;

	    public ActiveTransaction FirstProtectedTransaction
		{
			get
			{
				if (activeTransactions == null)
				{
					throw new InvalidOperationException("Active transactions is null.");
				}

				ActiveTransaction first = null;
				for (var index = 0; index < activeTransactions.Count; ++index)
				{
					if (index == 0 ||
						first.FirstLogId > activeTransactions[index].FirstLogId)
					{
						first = activeTransactions[index];
					}
				}
				return first;
			}
		}
		#endregion

		#region Public Methods
		public ActiveTransaction GetTransactionAt(int index)
		{
			if (activeTransactions == null)
			{
				throw new InvalidOperationException("Active transactions is null.");
			}

			return activeTransactions[index];
		}
		#endregion

		#region Protected Methods
		protected override void DoWrite(BufferReaderWriter streamManager)
		{
			base.DoWrite(streamManager);

			streamManager.Write((ushort)TransactionCount);
			for (var index = 0; index < TransactionCount; ++index)
			{
				streamManager.Write(activeTransactions[index].FileId);
				streamManager.Write(activeTransactions[index].FileOffset);
				streamManager.Write(activeTransactions[index].FirstLogId);
				streamManager.Write(activeTransactions[index].TransactionId);
			}
		}

		protected override void DoRead(BufferReaderWriter streamManager)
		{
			base.DoRead(streamManager);

			var count = streamManager.ReadUInt16();
			if (count > 0)
			{
				activeTransactions = new List<ActiveTransaction>();
				for (var index = 0; index < count; ++index)
				{
					var fileId = streamManager.ReadUInt16();
					var fileOffset = streamManager.ReadUInt32();
					var firstLogId = streamManager.ReadUInt32();
					var transactionId = streamManager.ReadUInt32();

					var tran = new ActiveTransaction(
						transactionId, fileId, fileOffset, firstLogId);
					activeTransactions.Add(tran);
				}
			}
		}
		#endregion

		#region Internal Methods
		internal void UpdateTransactions(List<ActiveTransaction> active)
		{
			// Add transactions to object.
			if (activeTransactions != null)
			{
				throw new InvalidOperationException("Active transaction collection already set.");
			}
			activeTransactions = active;
		}
		#endregion
	}

	/// <summary>
	/// Defines the start of a checkpoint operation.
	/// </summary>
	[Serializable]
	public class BeginCheckPointLogEntry : CheckPointLogEntry
	{
		public BeginCheckPointLogEntry()
			: base(LogEntryType.BeginCheckpoint)
		{
		}
	}

	/// <summary>
	/// Defines the end of a checkpoint operation.
	/// </summary>
	[Serializable]
	public class EndCheckPointLogEntry : CheckPointLogEntry
	{
		public EndCheckPointLogEntry()
			: base(LogEntryType.EndCheckpoint)
		{
		}
	}

	/// <summary>
	/// Defines the start of a transaction.
	/// </summary>
	/// <remarks>
	/// For a given transaction Id this will be the first record written
	/// using that Id.
	/// </remarks>
	[Serializable]
	public class BeginTransactionLogEntry : TransactionLogEntry
	{
		public BeginTransactionLogEntry(uint transactionId)
			: base(LogEntryType.BeginXact, transactionId)
		{
		}
		internal BeginTransactionLogEntry()
		{
		}
	}

	/// <summary>
	/// Defines the successful end of a transaction.
	/// </summary>
	[Serializable]
	public class CommitTransactionLogEntry : TransactionLogEntry
	{
		public CommitTransactionLogEntry(uint transactionId)
			: base(LogEntryType.CommitXact, transactionId)
		{
		}
		internal CommitTransactionLogEntry()
		{
		}
	}

	/// <summary>
	/// Defines the unsuccessful end of a transaction.
	/// </summary>
	[Serializable]
	public class RollbackTransactionLogEntry : TransactionLogEntry
	{
		public RollbackTransactionLogEntry(uint transactionId)
			: base(LogEntryType.RollbackXact, transactionId)
		{
		}
		internal RollbackTransactionLogEntry()
		{
		}
	}

	/// <summary>
	/// Serves as a base class for transacted operations against a database
	/// page.
	/// </summary>
	/// <remarks>
	/// For a given transaction Id these records must only occur between
	/// a <see cref="T:BeginTransactionLogEntry"/> record and either a
	/// <see cref="T:CommitTransactionLogEntry"/> or 
	/// <see cref="T:RollbackTransactionLogEntry"/> record.
	/// </remarks>
	[Serializable]
	public abstract class PageLogEntry : TransactionLogEntry
	{
		#region Private Fields
		private readonly BufferFieldUInt64 _virtualPageId;
		private readonly BufferFieldInt64 _timestamp;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="PageLogEntry"/> class.
		/// </summary>
		/// <param name="virtualPageId">The virtual page id.</param>
		/// <param name="timestamp">The timestamp.</param>
		/// <param name="logType">Type of the log.</param>
		public PageLogEntry(
			ulong virtualPageId,
			long timestamp,
			LogEntryType logType)
			: base(logType)
		{
			_virtualPageId = new BufferFieldUInt64(base.LastField, virtualPageId);
			_timestamp = new BufferFieldInt64(_virtualPageId, timestamp);
		}

		protected PageLogEntry()
		{
			_virtualPageId = new BufferFieldUInt64(base.LastField);
			_timestamp = new BufferFieldInt64(_virtualPageId);
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets the virtual page id.
		/// </summary>
		/// <value>The virtual page id.</value>
		public VirtualPageId VirtualPageId => new VirtualPageId(_virtualPageId.Value);

	    /// <summary>
		/// Gets the timestamp.
		/// </summary>
		/// <value>The timestamp.</value>
		public long Timestamp => _timestamp.Value;

	    #endregion

		#region Protected Properties
		/// <summary>
		/// Gets the last field.
		/// </summary>
		/// <value>The last field.</value>
		protected override BufferField LastField => _timestamp;

	    #endregion

		#region Public Methods
		/// <summary>
		/// Performs the rollback action on the associated page buffer from
		/// this log record state.
		/// </summary>
		/// <param name="device">The device.</param>
		/// <returns></returns>
		public override async Task RollBack(DatabaseDevice device)
		{
			var page = await LoadPageFromDevice(device);

			if (page.Timestamp != _timestamp.Value)
			{
				OnUndoChanges(page.DataBuffer);
			}
		}

		/// <summary>
		/// Performs the rollforward action on the associated page buffer from
		/// this log record state.
		/// </summary>
		/// <param name="device">The device.</param>
		/// <returns></returns>
		public override async Task RollForward(DatabaseDevice device)
		{
			var page = await LoadPageFromDevice(device);

			if (page.Timestamp != _timestamp.Value)
			{
				OnRedoChanges(page.DataBuffer);
			}
		}
		#endregion

		#region Protected Methods
		/// <summary>
		/// <b>OnUndoChanges</b> is called during recovery to undo DataBuffer
		/// changes to the given page object.
		/// </summary>
		/// <param name="page"></param>
		/// <remarks>
		/// This method is only called if a mismatch in timestamps has
		/// been detected.
		/// </remarks>
		internal abstract void OnUndoChanges(PageBuffer DataBuffer);

		/// <summary>
		/// <b>OnRedoChanges</b> is called during recovery to redo DataBuffer
		/// changes to the given page object.
		/// </summary>
		/// <param name="page"></param>
		/// <remarks>
		/// This method is only called if a mismatch in timestamps has
		/// been detected.
		/// </remarks>
		internal abstract void OnRedoChanges(PageBuffer DataBuffer);
		#endregion

		#region Private Methods
		private async Task<DataPage> LoadPageFromDevice(DatabaseDevice device)
		{
			// Create generic page object and load
		    var page = new DataPage
		    {
		        VirtualId = VirtualPageId,
		        FileGroupId = FileGroupId.Invalid
		    };
		    await device
                .LoadFileGroupPage(new LoadFileGroupPageParameters(null, page, true))
                .ConfigureAwait(false);

			// Return the page
			return page;
		}
		#endregion
	}

	/// <summary>
	/// Defines a log entry for creating or allocating a DataBuffer page.
	/// </summary>
	[Serializable]
	public class PageImageCreateLogEntry : PageLogEntry
	{
		#region Private Fields
		private byte[] _image;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="PageImageCreateLogEntry"/> class.
		/// </summary>
		/// <param name="buffer">The buffer.</param>
		/// <param name="virtualPageId">The virtual page id.</param>
		/// <param name="timestamp">The timestamp.</param>
		public PageImageCreateLogEntry(
			VirtualBuffer buffer,
			ulong virtualPageId,
			long timestamp)
			: base(virtualPageId, timestamp, LogEntryType.CreatePage)
		{
			_image = new byte[buffer.BufferSize];
			buffer.CopyTo(_image);
		}

		internal PageImageCreateLogEntry()
		{
		}
		#endregion

		#region Public Properties
		public override uint RawSize => (uint)(base.RawSize + _image.Length);

	    public byte[] Image => _image;

	    #endregion

		#region Protected Methods
		protected override void DoWrite(BufferReaderWriter streamManager)
		{
			base.DoWrite(streamManager);
			streamManager.Write(_image);
		}

		protected override void DoRead(BufferReaderWriter streamManager)
		{
			base.DoRead(streamManager);
			_image = streamManager.ReadBytes(8192);
		}
		#endregion

		#region Internal Methods
		internal override void OnUndoChanges(PageBuffer DataBuffer)
		{
			// Copy before image into page DataBuffer
			using (var stream = DataBuffer.GetBufferStream(0, 8192, false))
			{
				// NOTE: The before image in this case is empty
				var initStream = new byte[8192];
				stream.Write(initStream, 0, 8192);
				stream.Flush();
			}

			// Mark DataBuffer as dirty
			DataBuffer.SetDirtyAsync();
		}

		internal override void OnRedoChanges(PageBuffer DataBuffer)
		{
			// Copy after image into page DataBuffer
			using (var stream = DataBuffer.GetBufferStream(0, 8192, false))
			{
				stream.Write(_image, 0, 8192);
				stream.Flush();
			}

			// Mark DataBuffer as dirty
			DataBuffer.SetDirtyAsync();
		}
		#endregion
	}

	/// <summary>
	/// Defines a log entry for updating an existing DataBuffer page.
	/// </summary>
	[Serializable]
	public class PageImageUpdateLogEntry : PageLogEntry
	{
		#region Private Fields
		private byte[] _beforeImage;
		private byte[] _afterImage;
		#endregion

		#region Public Constructors
		public PageImageUpdateLogEntry(
			VirtualBuffer before,
			VirtualBuffer after,
			ulong virtualPageId,
			long timestamp)
			: base(virtualPageId, timestamp, LogEntryType.ModifyPage)
		{
			_beforeImage = new byte[before.BufferSize];
			before.CopyTo(_beforeImage);

			_afterImage = new byte[after.BufferSize];
			after.CopyTo(_afterImage);
		}
		internal PageImageUpdateLogEntry()
		{
		}
		#endregion

		#region Public Properties
		public override uint RawSize => base.RawSize + 16384;

	    public byte[] BeforeImage => _beforeImage;

	    public byte[] AfterImage => _afterImage;

	    #endregion

		#region Protected Methods
		protected override void DoWrite(BufferReaderWriter streamManager)
		{
			base.DoWrite(streamManager);
			streamManager.Write(_beforeImage);
			streamManager.Write(_afterImage);
		}

		protected override void DoRead(BufferReaderWriter streamManager)
		{
			base.DoRead(streamManager);
			_beforeImage = streamManager.ReadBytes(8192);
			_afterImage = streamManager.ReadBytes(8192);
		}
		#endregion

		#region Internal Methods
		internal override void OnUndoChanges(PageBuffer DataBuffer)
		{
			// Copy before image into page DataBuffer
			using (var stream = DataBuffer.GetBufferStream(0, 8192, false))
			{
				stream.Write(_beforeImage, 0, 8192);
				stream.Flush();
			}

			// Mark DataBuffer as dirty
			DataBuffer.SetDirtyAsync();
		}

		internal override void OnRedoChanges(PageBuffer DataBuffer)
		{
			// Copy after image into page DataBuffer
			using (var stream = DataBuffer.GetBufferStream(0, 8192, false))
			{
				stream.Write(_afterImage, 0, 8192);
				stream.Flush();
			}

			// Mark DataBuffer as dirty
			DataBuffer.SetDirtyAsync();
		}
		#endregion
	}

	/// <summary>
	/// Defines a log entry for deleting a previously allocated DataBuffer page.
	/// </summary>
	[Serializable]
	public class PageImageDeleteLogEntry : PageLogEntry
	{
		#region Private Fields
		private byte[] image;
		#endregion

		#region Public Constructors
		public PageImageDeleteLogEntry(
			VirtualBuffer buffer,
			ulong virtualPageId,
			long timestamp)
			: base(virtualPageId, timestamp, LogEntryType.DeletePage)
		{
			image = new byte[buffer.BufferSize];
			buffer.CopyTo(image);
		}
		internal PageImageDeleteLogEntry()
		{
		}
		#endregion

		#region Public Properties
		public override uint RawSize => base.RawSize + 8192;

	    public byte[] Image => image;

	    #endregion

		#region Protected Methods
		protected override void DoWrite(BufferReaderWriter streamManager)
		{
			base.DoWrite(streamManager);
			streamManager.Write(image);
		}

		protected override void DoRead(BufferReaderWriter streamManager)
		{
			base.DoRead(streamManager);
			image = streamManager.ReadBytes(8192);
		}
		#endregion

		#region Internal Methods
		internal override void OnUndoChanges(PageBuffer DataBuffer)
		{
			// Copy before image into page DataBuffer
			using (var stream = DataBuffer.GetBufferStream(0, 8192, false))
			{
				stream.Write(image, 0, 8192);
				stream.Flush();
			}

			// Mark DataBuffer as dirty
			DataBuffer.SetDirtyAsync();
		}

		internal override void OnRedoChanges(PageBuffer DataBuffer)
		{
			// Copy after image into page DataBuffer
			using (var stream = DataBuffer.GetBufferStream(0, 8192, false))
			{
				var initStream = new byte[8192];
				stream.Write(initStream, 0, 8192);
				stream.Flush();
			}

			// Mark DataBuffer as dirty
			DataBuffer.SetDirtyAsync();
		}
		#endregion
	}
}
