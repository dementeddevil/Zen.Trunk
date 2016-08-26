// -----------------------------------------------------------------------
// <copyright file="MasterLogPageDevice.cs" company="Zen Design Corp">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Autofac;
using Zen.Trunk.Storage.Data;

namespace Zen.Trunk.Storage.Log
{
	/// <summary>
	/// TODO: Update summary.
	/// </summary>
	public class MasterLogPageDevice : LogPageDevice
	{
		#region Private Types
		private class AddLogDeviceRequest : TaskRequest<AddLogDeviceParameters, DeviceId>
		{
			public AddLogDeviceRequest(AddLogDeviceParameters param)
				: base(param)
			{
			}
		}

		private class RemoveLogDeviceRequest : TaskRequest<RemoveLogDeviceParameters, bool>
		{
			public RemoveLogDeviceRequest(RemoveLogDeviceParameters param)
				: base(param)
			{
			}
		}

		private class WriteLogEntryRequest : TaskRequest<LogEntry, bool>
		{
			public WriteLogEntryRequest(LogEntry entry)
				: base(entry)
			{
			}
		}

		private class PerformRecoveryRequest : TaskRequest<bool>
		{
		}
		#endregion

		#region Private Fields
	    private readonly Dictionary<DeviceId, LogPageDevice> _secondaryDevices =
			new Dictionary<DeviceId, LogPageDevice>();

		private VirtualLogFileStream _currentStream;
		private object syncWriters = new object();
		private Dictionary<ActiveTransaction, List<TransactionLogEntry>> _activeTransactions;
		private int _nextTransactionId = 1;
		private DeviceId _nextLogDeviceId;

		private bool _trucateLog = false;
		private bool _isInRecovery = false;
		#endregion

		#region Public Constructors
	    /// <summary>
	    /// Initializes a new instance of the <see cref="LogPageDevice"/> class.
	    /// </summary>
	    /// <param name="parentLifetimeScope">The parent service provider.</param>
	    /// <param name="pathName">Pathname to log file</param>
	    public MasterLogPageDevice(ILifetimeScope parentLifetimeScope, string pathName)
			: base(parentLifetimeScope, DeviceId.Zero, pathName)
		{
	        var taskInterleave = new ConcurrentExclusiveSchedulerPair(TaskScheduler.Default);
            AddLogDevicePort = new TaskRequestActionBlock<AddLogDeviceRequest, DeviceId>(
                request => AddLogDeviceHandler(request),
                new ExecutionDataflowBlockOptions
                {
                    TaskScheduler = taskInterleave.ExclusiveScheduler
                });
            RemoveLogDevicePort = new TaskRequestActionBlock<RemoveLogDeviceRequest, bool>(
                request => RemoveLogDeviceHandler(request),
                new ExecutionDataflowBlockOptions
                {
                    TaskScheduler = taskInterleave.ExclusiveScheduler
                });
            WriteLogEntryPort = new TaskRequestActionBlock<WriteLogEntryRequest, bool>(
                request => WriteLogEntryHandler(request),
                new ExecutionDataflowBlockOptions
                {
                    TaskScheduler = taskInterleave.ExclusiveScheduler
                });
            PerformRecoveryPort = new ActionBlock<PerformRecoveryRequest>(
                request => PerformRecoveryHandler(request),
                new ExecutionDataflowBlockOptions
                {
                    TaskScheduler = taskInterleave.ExclusiveScheduler
                });
        }
        #endregion

        #region Public Properties
        public uint CurrentLogFileId => _currentStream.FileId;

	    public uint CurrentLogFileOffset => (uint)_currentStream.Position;

	    public long Position
		{
			get
			{
				return _currentStream.Position;
			}
			set
			{
				_currentStream.Position = value;
			}
		}
		/*public uint LogStartFileId
		{
			get
			{
				return _rootPage.LogStartFileId;
			}
		}
		public uint LogStartOffset
		{
			get
			{
				return _rootPage.LogStartOffset;
			}
		}
		public uint LogEndFileId
		{
			get
			{
				return _rootPage.LogEndFileId;
			}
		}
		public uint LogEndOffset
		{
			get
			{
				return _rootPage.LogEndOffset;
			}
		}*/

		public override bool IsInRecovery => _isInRecovery;

	    public bool IsTruncatingLog => _trucateLog;

	    #endregion

		#region Private Properties
		private ITargetBlock<AddLogDeviceRequest> AddLogDevicePort { get; }

	    private ITargetBlock<RemoveLogDeviceRequest> RemoveLogDevicePort { get; }

	    private ITargetBlock<WriteLogEntryRequest> WriteLogEntryPort { get; }

	    private ITargetBlock<PerformRecoveryRequest> PerformRecoveryPort { get; }

	    #endregion

		#region Public Methods
		public Task<DeviceId> AddDevice(AddLogDeviceParameters deviceParams)
		{
			var request = new AddLogDeviceRequest(deviceParams);
			if (!AddLogDevicePort.Post(request))
			{
				throw new BufferDeviceShuttingDownException();
			}
			return request.Task;
		}

		public Task RemoveDevice(RemoveLogDeviceParameters deviceParams)
		{
			var request = new RemoveLogDeviceRequest(deviceParams);
			if (!RemoveLogDevicePort.Post(request))
			{
				throw new BufferDeviceShuttingDownException();
			}
			return request.Task;
		}

		public Task WriteEntry(LogEntry entry)
		{
			var request = new WriteLogEntryRequest(entry);
			if (!WriteLogEntryPort.Post(request))
			{
				throw new BufferDeviceShuttingDownException();
			}
			return request.Task;
		}

		public Task PerformRecovery()
		{
			var request = new PerformRecoveryRequest();
			if (!PerformRecoveryPort.Post(request))
			{
				throw new BufferDeviceShuttingDownException();
			}
			return request.Task;
		}

		public TransactionId GetNextTransactionId()
		{
			return new TransactionId((uint)Interlocked.Increment(ref _nextTransactionId));
		}

		/// <summary>
		/// TODO: Promote this code to a message sent via requestport
		/// </summary>
		public void TruncateLog()
		{
			// TODO Determine protected virtual files.
			// TODO Mark all other files as unallocated.
			// TODO For each device 
			// TODO Skip devices without allocations.

			// TODO find first free virtual file.

			// TODO If this is the next file after protected block then skip.

			// TODO: Consider moving protected log to start of first file
			//	unless we are in the first file (then don't bother)

			// TODO Update 
		}

		/// <summary>
		/// TODO: Promote this code to a message sent via requestport
		/// </summary>
		public void ExpandDevice()
		{
			/*// Find our best candidate device.
			PhysicalBufferDevice bestDevice = null;
			LogDeviceInfo devInfo;
			uint smallestAllocatedPageCount = uint.MaxValue;
			for (int index = 0; index < _logBufferDevice.DeviceCount; ++index)
			{
				// Locate log device info from root page
				devInfo = _rootPage.GetDeviceByIndex(index);

				// If device is expandable then check if smallest device
				if (devInfo.IsExpandable)
				{
					PhysicalBufferDevice device = _logBufferDevice[devInfo.Id];
					if (device.PageCapacity < smallestAllocatedPageCount)
					{
						bestDevice = device;
						smallestAllocatedPageCount = device.PageCapacity;
					}
				}
			}

			// If we don't have a suitable device then throw
			if (bestDevice == null)
			{
				// TODO: Throw correct exception type
				throw new DBException("Log device is full.");
			}

			// Determine the growth amount for this device.
			uint virtualFiles = 2;
			uint growthPages = _rootPage.ExtentPages * virtualFiles;
			bestDevice.PageCapacity += growthPages;

			// Create virtual file entries
			devInfo = _rootPage.GetDeviceById(bestDevice.Id);
			for (uint index = 0; index < virtualFiles; ++index)
			{
				VirtualLogFileInfo file = devInfo.AddLogFile(_rootPage.ExtentSize,
					_rootPage.LogLastFileId);
				VirtualLogFileInfo lastFile = _rootPage.GetVirtualFileById(_rootPage.LogLastFileId);
				if (lastFile.CurrentHeader.NextFileId == 0)
				{
					lastFile.CurrentHeader.NextFileId = file.FileId;
				}
				_rootPage.LogLastFileId = file.FileId;
			}

			// Save root page
			SaveRootPage();*/
		}
		#endregion

		#region Internal Methods
		/// <summary>
		/// Performs rollforward operations on a list of transaction log entries.
		/// </summary>
		/// <remarks>
		/// The transactions are rolled back in reverse order.
		/// </remarks>
		/// <param name="transactions"></param>
		internal async Task CommitTransactions(List<TransactionLogEntry> transactions)
		{
			// We need the database device
			var pageDevice = ResolveDeviceService<DatabaseDevice>();

			// Work through each transaction in the list
			foreach (var entry in transactions)
			{
				await entry
					.RollForward(pageDevice)
					.ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Performs rollback operations on a list of transaction log entries.
		/// </summary>
		/// <remarks>
		/// The transactions are rolled back in reverse order.
		/// </remarks>
		/// <param name="transactions"></param>
		internal async Task RollbackTransactions(List<TransactionLogEntry> transactions)
		{
            // We need the data page device
            var pageDevice = ResolveDeviceService<DatabaseDevice>();

            // We need to rollback transactions in reverse order and we don't
            //	need to worry about locks as the owner transaction is still
            //	active.
            transactions.Reverse();

			// Work through each transaction in the list
			foreach (var entry in transactions)
			{
				await entry
					.RollBack(pageDevice)
					.ConfigureAwait(false);
			}
		}
		#endregion

		#region Protected Methods
		protected override async Task OnOpen()
		{
			// Do base class work first
			await base.OnOpen().ConfigureAwait(false);

			var rootPage = GetRootPage<MasterLogRootPage>();
			if (IsCreate)
			{
				// Open each secondary device we know about
				var secondaryTasks = new List<Task>();
				foreach (var device in _secondaryDevices.Values)
				{
					secondaryTasks.Add(device.OpenAsync(true));
				}
				await TaskExtra
					.WhenAllOrEmpty(secondaryTasks.ToArray())
					.ConfigureAwait(false);

				rootPage.ReadOnly = false;

				// Initialise root page information
				rootPage.LogStartOffset = 0;
				rootPage.LogEndOffset = 0;

				// Initialise virtual file streams
				InitVirtualFiles();

				// Setup current log stream
				_currentStream = GetVirtualFileStream(rootPage.LogStartFileId);
				_currentStream.InitNew();

				// Save root page
				SaveRootPage();
			}
			else
			{
				// Open each slave device and add to device collection.
				var secondaryTasks = new List<Task>();
				for (var index = 0; index < rootPage.DeviceCount; ++index)
				{
					var info = rootPage.GetDeviceByIndex(index);

					var secondaryDevice = ResolveDeviceService<LogPageDevice>(
                        new NamedParameter("deviceId", info.Id),
                        new NamedParameter("pathName", info.PathName));
					_secondaryDevices.Add(info.Id, secondaryDevice);

					secondaryTasks.Add(secondaryDevice.OpenAsync(false));
				}
				await TaskExtra
					.WhenAllOrEmpty(secondaryTasks.ToArray())
					.ConfigureAwait(false);

				// Open the current log file stream
				_currentStream = GetVirtualFileStream(rootPage.LogStartFileId);
			}
		}

	    protected override async Task OnClose()
	    {

            // Close secondary devices first
	        foreach (var secondaryDevice in _secondaryDevices.Values)
	        {
	            await secondaryDevice.CloseAsync().ConfigureAwait(false);
	        }
	        await base.OnClose().ConfigureAwait(false);
	    }

	    protected override LogRootPage CreateRootPage()
		{
			return new MasterLogRootPage();
		}
		#endregion

		#region Private Methods
		private async Task<DeviceId> AddLogDeviceHandler(AddLogDeviceRequest request)
		{
			var masterRootPage = GetRootPage<MasterLogRootPage>();

            // Determine file-extension for LOG
            var proposedDeviceId = DeviceId.Zero;
			var proposedDeviceIdValid = false;

			var primaryLog = false;
			var extn = ".slf";

			if (DeviceState == MountableDeviceState.Closed &&
				string.IsNullOrEmpty(PathName))
			{
				extn = ".mlf";
				primaryLog = true;
				proposedDeviceIdValid = true;
			}

			// Rewrite extension as required
			var fullPathName = request.Message.PathName;
			if (string.Compare(Path.GetExtension(request.Message.PathName), extn, true) != 0)
			{
				var fileName = Path.GetFileNameWithoutExtension(request.Message.PathName) + extn;
				fullPathName = Path.Combine(Path.Combine(
					Path.GetPathRoot(request.Message.PathName),
					Path.GetDirectoryName(request.Message.PathName)), fileName);
			}

			// Determine appropriate device id as necessary
			if (!proposedDeviceIdValid)
			{
				if (!request.Message.IsDeviceIdValid)
				{
					proposedDeviceId = _nextLogDeviceId = _nextLogDeviceId.Next;
				}
				else
				{
					if (request.Message.DeviceId == Storage.DeviceId.Zero ||
						_secondaryDevices.ContainsKey(request.Message.DeviceId))
					{
						throw new ArgumentException("Log device id already allocated.");
					}

					proposedDeviceId = request.Message.DeviceId;
				}

				proposedDeviceIdValid = true;
			}

			// If this is a secondary device then create log device and
			//	update root page and collections
			LogPageDevice device;
			if (!primaryLog)
			{
                var secondaryDevice = ResolveDeviceService<LogPageDevice>(
                    new NamedParameter("deviceId", proposedDeviceId),
                    new NamedParameter("pathName", fullPathName));
				_secondaryDevices.Add(proposedDeviceId, secondaryDevice);

				masterRootPage.AddDevice(
					new DeviceInfo
					{
						Id = proposedDeviceId,
						Name = request.Message.Name,
						PathName = fullPathName
					});
				device = secondaryDevice;
			}
			else
			{
				device = this;
			}

			// Setup root page information for new device
			if (request.Message.IsCreate)
			{
				var rootPage = device.GetRootPage<LogRootPage>();
				rootPage.AllocatedPages = request.Message.CreatePageCount;
				rootPage.MaximumPages = 0;
				rootPage.GrowthPages = request.Message.GrowthPages;
			}

			// If we are mounting then open file stream
			if (DeviceState == MountableDeviceState.Open ||
				DeviceState == MountableDeviceState.Opening)
			{
				await device
					.OpenAsync(request.Message.IsCreate)
					.ConfigureAwait(false);
			}

			if (primaryLog)
			{
				// Save full path for later open
				PathName = fullPathName;
			}

			if (request.Message.IsCreate && DeviceState == MountableDeviceState.Open)
			{
				SaveRootPage();
			}

			return proposedDeviceId;
		}

		private bool RemoveLogDeviceHandler(RemoveLogDeviceRequest request)
		{
			return true;
		}

		private Dictionary<TransactionId, List<TransactionLogEntry>> GetCheckPointTransactions()
		{
			// Read last reliable checkpoint record
			var cpi = GetBestCheckpoint();

			// Determine first virtual file for check-point start.
			_currentStream = GetVirtualFileStream(cpi.BeginFileId);
			_currentStream.Position = cpi.BeginOffset;

			// Create log reader
			Dictionary<TransactionId, List<TransactionLogEntry>> transactionTable = null;
			BeginCheckPointLogEntry startCheck = null;
			EndCheckPointLogEntry endCheck = null;
			while (endCheck == null)
			{
				// Read begin checkpoint record and build list of transactions
				var entry = _currentStream.ReadEntry();
				if (entry.LogType == LogEntryType.BeginCheckpoint)
				{
					if (startCheck != null)
					{
						throw new InvalidOperationException("Start checkpoint already found.");
					}
					startCheck = (BeginCheckPointLogEntry)entry;
				}

				// Track end checkpoint to ensure we have full record
				else if (entry.LogType == LogEntryType.EndCheckpoint)
				{
					if (endCheck != null)
					{
						throw new InvalidOperationException("End checkpoint already found.");
					}
					endCheck = (EndCheckPointLogEntry)entry;
				}

				// Process all other entries
				else if (entry.LogType != LogEntryType.NoOp)
				{
					var transEntry = (TransactionLogEntry)entry;

					// We should have a begin checkpoint.
					// We should not have an end checkpoint.

					if (transactionTable == null)
					{
						transactionTable = new Dictionary<TransactionId, List<TransactionLogEntry>>();
					}

					// Create transaction array as required
					var transactionId = transEntry.TransactionId;
					if (!transactionTable.ContainsKey(transactionId))
					{
						transactionTable.Add(transactionId, new List<TransactionLogEntry>());
					}

					// Add entry to list in transaction table
					transactionTable[transactionId].Add(transEntry);
				}
			}

			// Final sanity check - we should have both checkpoint records
			if (startCheck == null || endCheck == null)
			{
				throw new InvalidOperationException("No valid checkpoint information found.");
			}
			return transactionTable;
		}

		private bool WriteLogEntryHandler(WriteLogEntryRequest request)
		{
			// Sanity check
			if (DeviceState != MountableDeviceState.Open)
			{
				throw new DeviceException(DeviceId.Zero, "Not mounted!");
			}

			var entry = request.Message;

			// Update checkpoint records with active transaction list
			if ((entry.LogType == LogEntryType.BeginCheckpoint ||
				entry.LogType == LogEntryType.EndCheckpoint) &&
				(_activeTransactions != null && _activeTransactions.Count > 0))
			{
				// Update log entry with active transactions
				var cple = entry as CheckPointLogEntry;
				var tranlist = new List<ActiveTransaction>(
					_activeTransactions.Keys);
				cple.UpdateTransactions(tranlist);
			}

			// Write log entry
			WriteEntryCore(entry);

			var rootPage = GetRootPage<MasterLogRootPage>();

			// Update active transactions for begin/end transaction
			if (entry.LogType == LogEntryType.BeginXact)
			{
				var tle = entry as TransactionLogEntry;
				var tran = new ActiveTransaction(
					tle.TransactionId.Value,
					rootPage.LogEndFileId,
					rootPage.LogEndOffset,
					tle.LogId);
				if (_activeTransactions == null)
				{
					_activeTransactions = new Dictionary<ActiveTransaction, List<TransactionLogEntry>>();
				}
				var tranEntries = new List<TransactionLogEntry>();
				_activeTransactions.Add(tran, tranEntries);
			}

			// Update root-page checkpoint info as needed
			else if (entry.LogType == LogEntryType.BeginCheckpoint ||
				entry.LogType == LogEntryType.EndCheckpoint)
			{
				rootPage.AddCheckPoint(
					rootPage.LogEndFileId,
					rootPage.LogEndOffset,
					entry.LogType == LogEntryType.BeginCheckpoint);
			}

			// Remove active transaction on commit or rollback
			else if (entry.LogType == LogEntryType.RollbackXact ||
				entry.LogType == LogEntryType.CommitXact)
			{
				var tle = entry as TransactionLogEntry;
				foreach (var tran in _activeTransactions.Keys.ToArray())
				{
					// Is this the matching transaction?
					if (tran.TransactionId == tle.TransactionId.Value)
					{
						// Remove the active transaction from the list
						_activeTransactions.Remove(tran);
						break;
					}
				}
			}

			// Add transaction records to correct list
			else if (entry is TransactionLogEntry)
			{
				var tle = entry as TransactionLogEntry;
			    var tran = _activeTransactions.Keys
			        .FirstOrDefault(item => item.TransactionId == tle.TransactionId.Value);
				if (tran != null)
				{
					// Add transaction entry to list
					_activeTransactions[tran].Add(tle);
				}
			}

			// Update root page with new information
			rootPage.LogEndFileId = _currentStream.FileId;
			rootPage.LogEndOffset = (uint)_currentStream.Position;
			SaveRootPage();

			return true;
		}

		private void WriteEntryCore(LogEntry entry)
		{
			var rootPage = GetRootPage<MasterLogRootPage>();

			// Determine whether entry fits on current stream
			var freeSpace = (uint)(_currentStream.Length - _currentStream.Position);
			if (entry.RawSize > freeSpace)
			{
				// Determine next file id
				var current = GetVirtualFileById(_currentStream.FileId);

				// If the next file is zero then attempt to expand the log device
				if (current.CurrentHeader.NextFileId == 0)
				{
					ExpandDevice();
				}

				// If next file Id is still zero then die
				// TODO If we are on minimal log setting then attempt to loop back to start
				if (current.CurrentHeader.NextFileId == 0)
				{
					// TODO: Throw correct exception type
					throw new StorageEngineException("Log device is full");
				}

				var newStream = GetVirtualFileStream(
					current.CurrentHeader.NextFileId);

				// Chain new file to old
				_currentStream.IsFull = true;
				_currentStream.NextFileId = newStream.FileId;
				_currentStream.Flush();
				newStream.PrevFileId = _currentStream.FileId;

				// Update current stream
				_currentStream = newStream;
				_currentStream.InitNew();
				_currentStream.Position = 0;
			}

			// Update last log position
			rootPage.LogEndFileId = _currentStream.FileId;
			rootPage.LogEndOffset = (uint)_currentStream.Position;

			_currentStream.WriteEntry(entry);
		}

		private async Task PerformRecoveryHandler(PerformRecoveryRequest request)
		{
			if (_isInRecovery)
			{
				request.TrySetException(new StorageEngineException(
					"Logging system is already in recovery."));
				return;
			}

			_isInRecovery = true;
			try
			{
				var workDone = false;
				List<TransactionId> rollbackList = null;

				// Process each transaction
				var transactionTable = GetCheckPointTransactions();
				if (transactionTable != null)
				{
					foreach (var tranList in transactionTable.Values.Where(tl => tl.Count > 0))
					{
						// Every transaction in the transaction table which has an end-transaction
						//	can be committed.
						if (tranList[tranList.Count - 1].LogType == LogEntryType.CommitXact)
						{
							await CommitTransactions(tranList).ConfigureAwait(false);
							workDone = true;
						}

						// Everything else must be rolled back
						else
						{
							await RollbackTransactions(tranList).ConfigureAwait(false);

							// For implicit rollbacks we need to ensure we write an explicit
							//	rollback record to the log at the end of recovery
							if (tranList[tranList.Count - 1].LogType != LogEntryType.RollbackXact)
							{
								if (rollbackList == null)
								{
									rollbackList = new List<TransactionId>();
								}
								rollbackList.Add(tranList[0].TransactionId);
							}
							workDone = true;
						}
					}
				}

				// We need to write rollback records for any transaction that was
				//	rolled back and that didn't already have a rollback record.
				if (rollbackList != null)
				{
					var subTasks = new List<Task>();
					foreach (var transactionId in rollbackList)
					{
						subTasks.Add(WriteEntry(new RollbackTransactionLogEntry(transactionId)));
					}
					await TaskExtra
						.WhenAllOrEmpty(subTasks.ToArray())
						.ConfigureAwait(false);
				}

				// Strictly speaking we should issue a checkpoint request here
				//	however we don't need to wait for it ;-)
				if (workDone)
				{
					// IssueCheckPoint ();
				}

				// Now the database device is ready to use 
				request.TrySetResult(true);
			}
			finally
			{
				_isInRecovery = false;
			}
		}

		/// <summary>
		/// Gets the best checkpoint record.
		/// </summary>
		/// <remarks>
		/// The best check-point is the latest valid checkpoint record
		/// out of a total of up to three check-point entries.
		/// </remarks>
		/// <returns>
		/// <see cref="CheckPointInfo"/> object which
		/// is guarenteed to be valid or null if no valid check-point
		/// records exist.
		/// </returns>
		private CheckPointInfo GetBestCheckpoint()
		{
			var rootPage = GetRootPage<MasterLogRootPage>();

			CheckPointInfo check = null;
			for (var index = 0; index < rootPage.CheckPointHistoryCount; ++index)
			{
				var proposed = rootPage.GetCheckPointHistory(index);
				if (proposed.Valid)
				{
					check = proposed;
				}
			}
			return check;
		}

		/// <summary>
		/// Initialises the virtual file table chain linking all log devices
		/// </summary>
		/// <remarks>
		/// This method will rewrite the virtual file table.
		/// </remarks>
		internal void InitVirtualFiles()
		{
			var rootPage = GetRootPage<MasterLogRootPage>();
			var lastFileInfo = InitVirtualFileForDevice(rootPage, null);
			foreach (var device in _secondaryDevices.Values)
			{
				lastFileInfo = device.InitVirtualFileForDevice(rootPage, lastFileInfo);
			}
		}

		internal override VirtualLogFileInfo GetVirtualFileById(uint fileId)
		{
			var file = new LogFileId(fileId);
			if (file.DeviceId == DeviceId)
			{
				return base.GetVirtualFileById(fileId);
			}
			else
			{
				var secondaryDevice = _secondaryDevices[file.DeviceId];
				return secondaryDevice.GetVirtualFileById(fileId);
			}
		}

		private VirtualLogFileStream GetVirtualFileStream(uint fileId)
		{
			var info = GetVirtualFileById(fileId);
			if (info.DeviceId == DeviceId)
			{
				return GetVirtualFileStream(info);
			}
			else
			{
				var secondaryDevice = _secondaryDevices[info.DeviceId];
				return secondaryDevice.GetVirtualFileStream(info);
			}
		}
		#endregion
	}
}
