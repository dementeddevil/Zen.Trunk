using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Zen.Trunk.Extensions;
using Zen.Trunk.IO;
using Zen.Trunk.Storage.BufferFields;
using Zen.Trunk.Storage.Data;

namespace Zen.Trunk.Storage.Log
{
	/// <summary>
	/// Identifies the valid types of <see cref="T:LogEntry"/> records
	/// that can be written or read from a transaction log.
	/// </summary>
	public enum LogEntryType
	{
        /// <summary>
        /// The no op
        /// </summary>
        NoOp = 0,
        /// <summary>
        /// The begin checkpoint
        /// </summary>
        BeginCheckpoint = 1,
        /// <summary>
        /// The end checkpoint
        /// </summary>
        EndCheckpoint = 2,
        /// <summary>
        /// The begin xact
        /// </summary>
        BeginXact = 3,
        /// <summary>
        /// The commit xact
        /// </summary>
        CommitXact = 4,
        /// <summary>
        /// The rollback xact
        /// </summary>
        RollbackXact = 5,
        /// <summary>
        /// The create page
        /// </summary>
        CreatePage = 6,
        /// <summary>
        /// The modify page
        /// </summary>
        ModifyPage = 7,
        /// <summary>
        /// The delete page
        /// </summary>
        DeletePage = 8,
	}

	/// <summary>
	/// Defines basic information required for every transaction log entry.
	/// </summary>
	[Serializable]
	public class LogEntry : BufferFieldWrapper
	{
		#region Private Fields
		private LogEntryType _logType;

		private readonly BufferFieldUInt32 _logId;
		private readonly BufferFieldUInt32 _lastLog;
        #endregion

        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="LogEntry"/> class.
        /// </summary>
        /// <param name="logType">Type of the log.</param>
        public LogEntry(LogEntryType logType = LogEntryType.NoOp)
		{
			_logType = logType;
			_logId = new BufferFieldUInt32();
			_lastLog = new BufferFieldUInt32(_logId);
		}
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the raw size of this log entry record.
        /// </summary>
        /// <value>
        /// The size of this record in bytes.
        /// </value>
        public virtual uint RawSize => (uint)(TotalFieldLength + 1);

        /// <summary>
        /// Gets or sets the log identifier.
        /// </summary>
        /// <value>
        /// The log identifier.
        /// </value>
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

        /// <summary>
        /// Gets or sets the last log.
        /// </summary>
        /// <value>
        /// The last log.
        /// </value>
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

        /// <summary>
        /// Gets the type of the log.
        /// </summary>
        /// <value>
        /// The type of the log.
        /// </value>
        public LogEntryType LogType => _logType;
        #endregion

        #region Public Methods
        /// <summary>
        /// Reads the entry.
        /// </summary>
        /// <param name="streamManager">The stream manager.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Illegal log entry type detected.</exception>
        public static LogEntry ReadEntry(SwitchingBinaryReader streamManager)
		{
			LogEntry entry;
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
			entry._logType = logType;
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
        /// <summary>
        /// Gets the first buffer field object.
        /// </summary>
        /// <value>
        /// A <see cref="T:BufferField" /> object.
        /// </value>
        protected override BufferField FirstField => _logId;

        /// <summary>
        /// Gets the last buffer field object.
        /// </summary>
        /// <value>
        /// A <see cref="T:BufferField" /> object.
        /// </value>
        protected override BufferField LastField => _lastLog;
        #endregion

        #region Protected Methods
        /// <summary>
        /// Writes the field chain to the specified stream manager.
        /// </summary>
        /// <param name="writer">A <see cref="T:BufferReaderWriter" /> object.</param>
        protected override void OnWrite(SwitchingBinaryWriter writer)
		{
			writer.Write((byte)(int)_logType);
			base.OnWrite(writer);
		}
		#endregion

		#region Private Methods
		private static LogEntryType ReadLogType(SwitchingBinaryReader streamManager)
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
        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionLogEntry"/> class.
        /// </summary>
        /// <param name="logType">Type of the log.</param>
        public TransactionLogEntry(LogEntryType logType)
			: base(logType)
		{
			_transactionId = new BufferFieldUInt32(base.LastField);
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionLogEntry"/> class.
        /// </summary>
        /// <param name="logType">Type of the log.</param>
        /// <param name="transactionId">The transaction identifier.</param>
        public TransactionLogEntry(LogEntryType logType, TransactionId transactionId)
			: base(logType)
		{
			_transactionId = new BufferFieldUInt32(base.LastField, transactionId.Value);
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionLogEntry"/> class.
        /// </summary>
        protected TransactionLogEntry()
		{
			_transactionId = new BufferFieldUInt32(base.LastField);
		}
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the transaction identifier.
        /// </summary>
        /// <value>
        /// The transaction identifier.
        /// </value>
        public TransactionId TransactionId => new TransactionId(_transactionId.Value);
        #endregion

        #region Protected Properties
        /// <summary>
        /// Gets the last buffer field object.
        /// </summary>
        /// <value>
        /// A <see cref="T:BufferField" /> object.
        /// </value>
        protected override BufferField LastField => _transactionId;
	    #endregion

		#region Internal Methods
		internal void RewriteTransactionId(TransactionId transactionId)
		{
			_transactionId.Value = transactionId.Value;
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
		private List<ActiveTransaction> _activeTransactions;
        #endregion

        #region Protected Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="CheckPointLogEntry"/> class.
        /// </summary>
        /// <param name="logType">Type of the log.</param>
        /// <exception cref="ArgumentException">Invalid log entry type for checkpoint record.</exception>
        protected CheckPointLogEntry(LogEntryType logType)
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
        /// <summary>
        /// Gets the size of the raw.
        /// </summary>
        /// <value>
        /// The size of the raw.
        /// </value>
        public override uint RawSize
		{
			get
			{
				var rawSize = base.RawSize + 2;
				if (_activeTransactions != null)
				{
					rawSize += (uint)(_activeTransactions.Count * 14);
				}
				return rawSize;
			}
		}

        /// <summary>
        /// Gets the transaction count.
        /// </summary>
        /// <value>
        /// The transaction count.
        /// </value>
        public int TransactionCount
		{
			get
			{
				if (_activeTransactions == null)
				{
					return 0;
				}
				return _activeTransactions.Count;
			}
		}

        /// <summary>
        /// Gets the active transactions.
        /// </summary>
        /// <value>
        /// The active transactions.
        /// </value>
        public IEnumerable<ActiveTransaction> ActiveTransactions => _activeTransactions;

        /// <summary>
        /// Gets the first protected transaction.
        /// </summary>
        /// <value>
        /// The first protected transaction.
        /// </value>
        /// <exception cref="InvalidOperationException">Active transactions is null.</exception>
        public ActiveTransaction FirstProtectedTransaction
		{
			get
			{
				if (_activeTransactions == null)
				{
					throw new InvalidOperationException("Active transactions is null.");
				}

				ActiveTransaction firstProtectedTransaction = null;
				foreach (var activeTransaction in _activeTransactions)
				{
				    if (firstProtectedTransaction == null || firstProtectedTransaction.FirstLogId > activeTransaction.FirstLogId)
				    {
				        firstProtectedTransaction = activeTransaction;
				    }
				}
				return firstProtectedTransaction;
			}
		}
        #endregion

        #region Public Methods
        /// <summary>
        /// Gets the transaction at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Active transactions is null.</exception>
        public ActiveTransaction GetTransactionAt(int index)
		{
			if (_activeTransactions == null)
			{
				throw new InvalidOperationException("Active transactions is null.");
			}

			return _activeTransactions[index];
		}
        #endregion

        #region Protected Methods
        /// <summary>
        /// Writes the field chain to the specified stream manager.
        /// </summary>
        /// <param name="writer">A <see cref="T:BufferReaderWriter" /> object.</param>
        protected override void OnWrite(SwitchingBinaryWriter writer)
		{
			base.OnWrite(writer);

			writer.Write((ushort)TransactionCount);
			for (var index = 0; index < TransactionCount; ++index)
			{
                _activeTransactions[index].Write(writer);
			}
		}

        /// <summary>
        /// Reads the field chain from the specified stream manager.
        /// </summary>
        /// <param name="reader">A <see cref="T:BufferReaderWriter" /> object.</param>
        protected override void OnRead(SwitchingBinaryReader reader)
		{
			base.OnRead(reader);

			var count = reader.ReadUInt16();
			if (count > 0)
			{
				_activeTransactions = new List<ActiveTransaction>();
				for (var index = 0; index < count; ++index)
				{
				    var tran = new ActiveTransaction();
                    tran.Read(reader);
					_activeTransactions.Add(tran);
				}
			}
		}
		#endregion

		#region Internal Methods
		internal void UpdateTransactions(List<ActiveTransaction> active)
		{
			// Add transactions to object.
			if (_activeTransactions != null)
			{
				throw new InvalidOperationException("Active transaction collection already set.");
			}
			_activeTransactions = active;
		}
		#endregion
	}

	/// <summary>
	/// Defines the start of a checkpoint operation.
	/// </summary>
	[Serializable]
	public class BeginCheckPointLogEntry : CheckPointLogEntry
	{
        /// <summary>
        /// Initializes a new instance of the <see cref="BeginCheckPointLogEntry"/> class.
        /// </summary>
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
        /// <summary>
        /// Initializes a new instance of the <see cref="EndCheckPointLogEntry"/> class.
        /// </summary>
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
        /// <summary>
        /// Initializes a new instance of the <see cref="BeginTransactionLogEntry"/> class.
        /// </summary>
        /// <param name="transactionId">The transaction identifier.</param>
        public BeginTransactionLogEntry(TransactionId transactionId)
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
        /// <summary>
        /// Initializes a new instance of the <see cref="CommitTransactionLogEntry"/> class.
        /// </summary>
        /// <param name="transactionId">The transaction identifier.</param>
        public CommitTransactionLogEntry(TransactionId transactionId)
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
        /// <summary>
        /// Initializes a new instance of the <see cref="RollbackTransactionLogEntry"/> class.
        /// </summary>
        /// <param name="transactionId">The transaction identifier.</param>
        public RollbackTransactionLogEntry(TransactionId transactionId)
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

        #region Protected Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="PageLogEntry" /> class.
        /// </summary>
        /// <param name="virtualPageId">The virtual page id.</param>
        /// <param name="timestamp">The timestamp.</param>
        /// <param name="logType">Type of the log.</param>
        protected PageLogEntry(
			ulong virtualPageId,
			long timestamp,
			LogEntryType logType)
			: base(logType)
		{
			_virtualPageId = new BufferFieldUInt64(base.LastField, virtualPageId);
			_timestamp = new BufferFieldInt64(_virtualPageId, timestamp);
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="PageLogEntry"/> class.
        /// </summary>
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
        /// <param name="dataBuffer"></param>
        /// <remarks>
        /// This method is only called if a mismatch in timestamps has
        /// been detected.
        /// </remarks>
        protected abstract void OnUndoChanges(PageBuffer dataBuffer);

        /// <summary>
        /// <b>OnRedoChanges</b> is called during recovery to redo DataBuffer
        /// changes to the given page object.
        /// </summary>
        /// <param name="dataBuffer"></param>
        /// <remarks>
        /// This method is only called if a mismatch in timestamps has
        /// been detected.
        /// </remarks>
        protected abstract void OnRedoChanges(PageBuffer dataBuffer);
		#endregion

		#region Private Methods
		private async Task<DataPage> LoadPageFromDevice(DatabaseDevice device)
		{
			// Create generic page object and load
		    var page = new DataPage
		    {
		        VirtualPageId = VirtualPageId,
		        FileGroupId = FileGroupId.Invalid
		    };
		    await device
                .LoadFileGroupPageAsync(new LoadFileGroupPageParameters(null, page, true))
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
			IVirtualBuffer buffer,
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
        /// <summary>
        /// Gets the raw size of this log entry record.
        /// </summary>
        /// <value>
        /// The size of this record in bytes.
        /// </value>
        public override uint RawSize => (uint)(base.RawSize + _image.Length);

        /// <summary>
        /// Gets the image.
        /// </summary>
        /// <value>
        /// The image.
        /// </value>
        public byte[] Image => _image;

        #endregion

        #region Protected Methods
        /// <summary>
        /// Writes the field chain to the specified stream manager.
        /// </summary>
        /// <param name="writer">A <see cref="T:BufferReaderWriter" /> object.</param>
        protected override void OnWrite(SwitchingBinaryWriter writer)
		{
			base.OnWrite(writer);
			writer.Write(_image);
		}

        /// <summary>
        /// Reads the field chain from the specified stream manager.
        /// </summary>
        /// <param name="reader">A <see cref="T:BufferReaderWriter" /> object.</param>
        protected override void OnRead(SwitchingBinaryReader reader)
		{
			base.OnRead(reader);
			_image = reader.ReadBytes(8192);
		}

        /// <summary>
        /// <b>OnUndoChanges</b> is called during recovery to undo DataBuffer
        /// changes to the given page object.
        /// </summary>
        /// <param name="dataBuffer"></param>
        /// <remarks>
        /// This method is only called if a mismatch in timestamps has
        /// been detected.
        /// </remarks>
        protected override void OnUndoChanges(PageBuffer dataBuffer)
		{
			// Copy before image into page DataBuffer
			using (var stream = dataBuffer.GetBufferStream(0, 8192, false))
			{
				// NOTE: The before image in this case is empty
				var initStream = new byte[8192];
				stream.Write(initStream, 0, 8192);
				stream.Flush();
			}

			// Mark DataBuffer as dirty
			dataBuffer.SetDirtyAsync();
		}

        /// <summary>
        /// <b>OnRedoChanges</b> is called during recovery to redo DataBuffer
        /// changes to the given page object.
        /// </summary>
        /// <param name="dataBuffer"></param>
        /// <remarks>
        /// This method is only called if a mismatch in timestamps has
        /// been detected.
        /// </remarks>
        protected override void OnRedoChanges(PageBuffer dataBuffer)
		{
			// Copy after image into page DataBuffer
			using (var stream = dataBuffer.GetBufferStream(0, 8192, false))
			{
				stream.Write(_image, 0, 8192);
				stream.Flush();
			}

			// Mark DataBuffer as dirty
			dataBuffer.SetDirtyAsync();
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
        /// <summary>
        /// Initializes a new instance of the <see cref="PageImageUpdateLogEntry"/> class.
        /// </summary>
        /// <param name="before">The before.</param>
        /// <param name="after">The after.</param>
        /// <param name="virtualPageId">The virtual page identifier.</param>
        /// <param name="timestamp">The timestamp.</param>
        public PageImageUpdateLogEntry(
			IVirtualBuffer before,
			IVirtualBuffer after,
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
        /// <summary>
        /// Gets the raw size of this log entry record.
        /// </summary>
        /// <value>
        /// The size of this record in bytes.
        /// </value>
        public override uint RawSize => base.RawSize + 16384;

        /// <summary>
        /// Gets the before image.
        /// </summary>
        /// <value>
        /// The before image.
        /// </value>
        public byte[] BeforeImage => _beforeImage;

        /// <summary>
        /// Gets the after image.
        /// </summary>
        /// <value>
        /// The after image.
        /// </value>
        public byte[] AfterImage => _afterImage;
        #endregion

        #region Protected Methods
        /// <summary>
        /// Writes the field chain to the specified stream manager.
        /// </summary>
        /// <param name="writer">A <see cref="T:BufferReaderWriter" /> object.</param>
        protected override void OnWrite(SwitchingBinaryWriter writer)
		{
			base.OnWrite(writer);
			writer.Write(_beforeImage);
			writer.Write(_afterImage);
		}

        /// <summary>
        /// Reads the field chain from the specified stream manager.
        /// </summary>
        /// <param name="reader">A <see cref="T:BufferReaderWriter" /> object.</param>
        protected override void OnRead(SwitchingBinaryReader reader)
		{
			base.OnRead(reader);
			_beforeImage = reader.ReadBytes(8192);
			_afterImage = reader.ReadBytes(8192);
		}

        /// <summary>
        /// <b>OnUndoChanges</b> is called during recovery to undo DataBuffer
        /// changes to the given page object.
        /// </summary>
        /// <param name="dataBuffer"></param>
        /// <remarks>
        /// This method is only called if a mismatch in timestamps has
        /// been detected.
        /// </remarks>
        protected override void OnUndoChanges(PageBuffer dataBuffer)
		{
			// Copy before image into page DataBuffer
			using (var stream = dataBuffer.GetBufferStream(0, 8192, false))
			{
				stream.Write(_beforeImage, 0, 8192);
				stream.Flush();
			}

			// Mark DataBuffer as dirty
			dataBuffer.SetDirtyAsync();
		}

        /// <summary>
        /// <b>OnRedoChanges</b> is called during recovery to redo DataBuffer
        /// changes to the given page object.
        /// </summary>
        /// <param name="dataBuffer"></param>
        /// <remarks>
        /// This method is only called if a mismatch in timestamps has
        /// been detected.
        /// </remarks>
        protected override void OnRedoChanges(PageBuffer dataBuffer)
		{
			// Copy after image into page DataBuffer
			using (var stream = dataBuffer.GetBufferStream(0, 8192, false))
			{
				stream.Write(_afterImage, 0, 8192);
				stream.Flush();
			}

			// Mark DataBuffer as dirty
			dataBuffer.SetDirtyAsync();
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
		private byte[] _image;
        #endregion

        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="PageImageDeleteLogEntry"/> class.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="virtualPageId">The virtual page identifier.</param>
        /// <param name="timestamp">The timestamp.</param>
        public PageImageDeleteLogEntry(
			IVirtualBuffer buffer,
			ulong virtualPageId,
			long timestamp)
			: base(virtualPageId, timestamp, LogEntryType.DeletePage)
		{
			_image = new byte[buffer.BufferSize];
			buffer.CopyTo(_image);
		}

		internal PageImageDeleteLogEntry()
		{
		}
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the raw size of this log entry record.
        /// </summary>
        /// <value>
        /// The size of this record in bytes.
        /// </value>
        public override uint RawSize => base.RawSize + 8192;

        /// <summary>
        /// Gets the image.
        /// </summary>
        /// <value>
        /// The image.
        /// </value>
        public byte[] Image => _image;
        #endregion

        #region Protected Methods
        /// <summary>
        /// Writes the field chain to the specified stream manager.
        /// </summary>
        /// <param name="writer">A <see cref="T:BufferReaderWriter" /> object.</param>
        protected override void OnWrite(SwitchingBinaryWriter writer)
		{
			base.OnWrite(writer);
			writer.Write(_image);
		}

        /// <summary>
        /// Reads the field chain from the specified stream manager.
        /// </summary>
        /// <param name="reader">A <see cref="T:BufferReaderWriter" /> object.</param>
        protected override void OnRead(SwitchingBinaryReader reader)
		{
			base.OnRead(reader);
			_image = reader.ReadBytes(8192);
		}

        /// <summary>
        /// <b>OnUndoChanges</b> is called during recovery to undo DataBuffer
        /// changes to the given page object.
        /// </summary>
        /// <param name="dataBuffer"></param>
        /// <remarks>
        /// This method is only called if a mismatch in timestamps has
        /// been detected.
        /// </remarks>
        protected override void OnUndoChanges(PageBuffer dataBuffer)
		{
			// Copy before image into page DataBuffer
			using (var stream = dataBuffer.GetBufferStream(0, 8192, false))
			{
				stream.Write(_image, 0, 8192);
				stream.Flush();
			}

			// Mark DataBuffer as dirty
			dataBuffer.SetDirtyAsync();
		}

        /// <summary>
        /// <b>OnRedoChanges</b> is called during recovery to redo DataBuffer
        /// changes to the given page object.
        /// </summary>
        /// <param name="dataBuffer"></param>
        /// <remarks>
        /// This method is only called if a mismatch in timestamps has
        /// been detected.
        /// </remarks>
        protected override void OnRedoChanges(PageBuffer dataBuffer)
		{
			// Copy after image into page DataBuffer
			using (var stream = dataBuffer.GetBufferStream(0, 8192, false))
			{
				var initStream = new byte[8192];
				stream.Write(initStream, 0, 8192);
				stream.Flush();
			}

			// Mark DataBuffer as dirty
			dataBuffer.SetDirtyAsync();
		}
		#endregion
	}
}
