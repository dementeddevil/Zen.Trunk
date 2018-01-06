using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Autofac;
using Autofac.Core;
using Zen.Trunk.Extensions;
using Zen.Trunk.Logging;
using Zen.Trunk.Storage.Data;
using Zen.Trunk.Storage.Log;
using Zen.Trunk.Utils;
using Zen.Trunk.VirtualMemory;

namespace Zen.Trunk.Storage.Locking
{
    /// <summary>
    /// The <b>TrunkTransaction</b> object maintains the context of a
    /// transaction within the database. To achieve this the transaction
    /// object tracks the modified pages together with the related 
    /// transaction log records and this enables the object to control
    /// commit and rollback behaviour.
    /// </summary>
    internal class TrunkTransaction : MarshalByRefObject, ITrunkTransactionPrivate
    {
        #region Private Types
        private class DbPrepare : PreparingPageEnlistment
        {
            private readonly TaskCompletionSource<bool> _tcs =
                new TaskCompletionSource<bool>();

            internal Task<bool> Task => _tcs.Task;

            public override void ForceRollback()
            {
                _tcs.TrySetException(new TransactionAbortedException(
                    "Transaction rollback forced by participant."));
            }

            public override void ForceRollback(Exception error)
            {
                _tcs.TrySetException(error);
            }

            public override void Prepared()
            {
                _tcs.TrySetResult(true);
            }

            public override void Done()
            {
                _tcs.TrySetResult(false);
            }
        }

        private class DbNotify : PageEnlistment
        {
            private readonly TaskCompletionSource<object> _tcs =
                new TaskCompletionSource<object>();

            internal Task Task => _tcs.Task;

            public override void Done()
            {
                _tcs.TrySetResult(null);
            }
        }

        /*internal class BufferTimestamp
		{
			private PageBuffer _buffer;
			private long _timestamp;

			public BufferTimestamp(PageBuffer buffer, long timestamp)
			{
				_buffer = buffer;
				_timestamp = timestamp;
			}

			public PageBuffer Buffer
			{
				get
				{
					return _buffer;
				}
			}

			public long Timestamp
			{
				get
				{
					return _timestamp;
				}
			}

			public void Commit()
			{
				_buffer.Commit(_timestamp);
			}

			public void UpdateTimestamp(long newTimestamp)
			{
				Interlocked.Exchange(ref _timestamp, newTimestamp);
			}
		}*/
        #endregion

        #region Private Fields
        private static readonly ILog Logger = LogProvider.For<TrunkTransaction>();

        private readonly ILifetimeScope _lifetimeScope;
        private readonly List<IPageEnlistmentNotification> _subEnlistments = new List<IPageEnlistmentNotification>();
        private readonly List<TransactionLogEntry> _transactionLogs = new List<TransactionLogEntry>();
        private readonly Dictionary<DatabaseId, TransactionLockOwnerBlock> _transactionLockOwnerBlocks = new Dictionary<DatabaseId, TransactionLockOwnerBlock>();
        private TransactionOptions _options;
        private bool _isBeginLogWritten;
        private TransactionId _transactionId = TransactionId.Zero;
        private int _transactionCount = 1;
        private IMasterLogPageDevice _logDevice;
        private bool _nestedRollbackTriggered;
        private bool _isCompleting;
        private bool _isCompleted;
        #endregion

        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="TrunkTransaction"/> class.
        /// </summary>
        /// <param name="lifetimeScope">The service provider.</param>
        internal TrunkTransaction(ILifetimeScope lifetimeScope)
            : this(lifetimeScope, IsolationLevel.ReadCommitted, TimeSpan.FromSeconds(10))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TrunkTransaction"/> class.
        /// </summary>
        /// <param name="lifetimeScope">The service provider.</param>
        /// <param name="timeout">The timeout.</param>
        internal TrunkTransaction(
            ILifetimeScope lifetimeScope, TimeSpan timeout)
            : this(lifetimeScope, IsolationLevel.ReadCommitted, timeout)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TrunkTransaction"/> class.
        /// </summary>
        /// <param name="lifetimeScope">The service provider.</param>
        /// <param name="options">The options.</param>
        internal TrunkTransaction(
            ILifetimeScope lifetimeScope, TransactionOptions options)
            : this(lifetimeScope, options.IsolationLevel, options.Timeout)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TrunkTransaction"/> class.
        /// </summary>
        /// <param name="lifetimeScope">The service provider.</param>
        /// <param name="isoLevel">The iso level.</param>
        /// <param name="timeout">The timeout.</param>
        internal TrunkTransaction(
            ILifetimeScope lifetimeScope,
            IsolationLevel isoLevel,
            TimeSpan timeout)
        {
            // Ensure minimum timeout duration
            var minTimeout = TimeSpan.FromSeconds(30);
            if (timeout < minTimeout)
            {
                timeout = minTimeout;
            }

            _lifetimeScope = lifetimeScope;
            _options = new TransactionOptions
            {
                IsolationLevel = isoLevel,
                Timeout = timeout
            };

            EnlistInTransaction();
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the transaction identifier.
        /// </summary>
        /// <value>
        /// The transaction identifier.
        /// </value>
        public TransactionId TransactionId => _transactionId;

        /// <summary>
        /// Gets the isolation level.
        /// </summary>
        /// <value>The isolation level.</value>
        public IsolationLevel IsolationLevel => _options.IsolationLevel;

        /// <summary>
        /// Gets the transaction timeout.
        /// </summary>
        /// <value>The timeout.</value>
        public TimeSpan Timeout => _options.Timeout;

        /// <summary>
        /// Gets a value indicating whether this instance is completed.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is completed; otherwise, <c>false</c>.
        /// </value>
        public bool IsCompleted => _isCompleted;
        #endregion

        #region Private Properties
        private IMasterLogPageDevice LoggingDevice
        {
            get
            {
                if (_logDevice == null)
                {
                    EnlistInTransaction();
                }
                return _logDevice;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Gets the transaction lock owner block associated with the given lock manager.
        /// </summary>
        /// <param name="lockManager">The lock manager.</param>
        /// <returns></returns>
        public TransactionLockOwnerBlock GetTransactionLockOwnerBlock(IDatabaseLockManager lockManager)
        {
            if (!_transactionLockOwnerBlocks.TryGetValue(lockManager.DatabaseId, out var block))
            {
                block = new TransactionLockOwnerBlock(lockManager);
                _transactionLockOwnerBlocks.Add(lockManager.DatabaseId, block);
            }
            return block;
        }

        /// <summary>
        /// Begins the nested transaction.
        /// </summary>
        public void BeginNestedTransaction()
        {
            Interlocked.Increment(ref _transactionCount);
        }

        /// <summary>
        /// Enlists the specified notify.
        /// </summary>
        /// <param name="notify">The notify.</param>
        /// <exception cref="InvalidOperationException">
        /// Cannot enlist after transaction has completed.
        /// or
        /// Cannot enlist during commit/rollback.
        /// </exception>
        public void Enlist(IPageEnlistmentNotification notify)
        {
            if (_isCompleted)
            {
                throw new InvalidOperationException("Cannot enlist after transaction has completed.");
            }
            if (!_subEnlistments.Contains(notify))
            {
                if (_isCompleting)
                {
                    throw new InvalidOperationException("Cannot enlist during commit/rollback.");
                }
                _subEnlistments.Add(notify);
            }
        }

        /// <summary>
        /// Writes the log entry.
        /// </summary>
        /// <param name="entry">The entry.</param>
        /// <returns></returns>
        public async Task WriteLogEntryAsync(TransactionLogEntry entry)
        {
            // Ensure begin log record has been written
            if (!_isBeginLogWritten)
            {
                await WriteBeginXact().ConfigureAwait(false);
            }

            // Update log entry transaction ID as needed.
            if (entry.TransactionId != _transactionId)
            {
                entry.RewriteTransactionId(_transactionId);
            }

            if (Logger.IsDebugEnabled())
            {
                Logger.Debug($"{_transactionId} => Writing {entry.LogType} to log");
            }
            if (LoggingDevice != null)
            {
                _transactionLogs.Add(entry);
                await LoggingDevice.WriteEntryAsync(entry).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Commits this instance.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Not in a transaction!</exception>
        public async Task<bool> CommitAsync()
        {
            CheckNotCompleted();

            // If we are not in a transaction then throw up
            if (_transactionCount == 0)
            {
                throw new InvalidOperationException("Not in a transaction!");
            }

            // If we are committing a nested transaction then ignore
            if (Interlocked.Decrement(ref _transactionCount) > 0)
            {
                return false;
            }

            // If we haven't written the start transaction mark and we don't
            //	have any enlisted pages then release locks and exit since we
            //	haven't done any transactable work
            if (!_isBeginLogWritten && _subEnlistments.Count == 0)
            {
                await ReleaseAsync().ConfigureAwait(false);
                return true;
            }

            try
            {
                // Prepare enlistments for two-phase commit unless a nested
                //  transaction was instructed to rollback.
                _isCompleting = true;
                var performCommit = true;
                var prepTasks = new List<Task<bool>>();
                if (_nestedRollbackTriggered)
                {
                    if (Logger.IsDebugEnabled())
                    {
                        Logger.Debug($"{_transactionId} => Rolling back {_subEnlistments.Count} sub-enlistments due to nested transaction failure.");
                    }
                    performCommit = false;
                }
                else
                {
                    if (Logger.IsDebugEnabled())
                    {
                        Logger.Debug($"{_transactionId} => Preparing commit on {_subEnlistments.Count} sub-enlistments.");
                    }

                    // Prepare our sub-enlistments (pages) for commit operation
                    // Each sub-enlistment can opt out of further work in the commit process if needed
                    foreach (var sub in _subEnlistments)
                    {
                        var prepInfo = new DbPrepare();
                        prepTasks.Add(prepInfo.Task);
                        try
                        {
                            sub.Prepare(prepInfo);
                        }
                        catch (Exception e)
                        {
                            if (Logger.IsDebugEnabled())
                            {
                                Logger.Debug($"{_transactionId} => Prepare failed - rolling back\n\t{e.Message}");
                            }
                            performCommit = false;
                        }
                    }
                }

                // Wait for prepare participants to complete work
                var commitList = new List<IPageEnlistmentNotification>();
                if (performCommit)
                {
                    try
                    {
                        var result = await TaskExtra
                            .WhenAllOrEmpty(prepTasks.ToArray())
                            .ConfigureAwait(false);
                        if (result != null)
                        {
                            // Walk the list of result objects and add all sub-enlistments
                            //  that have opted into the two-phase commit
                            for (var index = 0; index < result.Length; ++index)
                            {
                                if (result[index])
                                {
                                    commitList.Add(_subEnlistments[index]);
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        if (Logger.IsDebugEnabled())
                        {
                            Logger.Debug($"{_transactionId} => Prepare failed - rolling back\n\t{e.Message}");
                        }
                        performCommit = false;
                    }
                }

                // If rollback was not forced nor error encountered then we can
                //	perform the second phase of the commit process.
                if (performCommit)
                {
                    // Notify all prepared objects that want to commit
                    if (Logger.IsDebugEnabled())
                    {
                        Logger.Debug($"{_transactionId} => Committing {commitList.Count} sub-enlistments");
                    }
                    var commitTasks = new List<Task>();
                    try
                    {
                        // Commit each item in the commit list
                        // NOTE: We maintain a map of virtual page ids we have
                        //	committed to ensure we only commit each page once
                        // This may mean some page objects are prepared but not
                        //	necessarily committed...
                        var tracker = new HashSet<VirtualPageId>();
                        foreach (var sub in commitList)
                        {
                            // We only process untracked page buffers
                            if (!(sub is PageBuffer page) || tracker.Contains(page.PageId))
                            {
                                continue;
                            }

                            // Consider page tracked...
                            tracker.Add(page.PageId);

                            // Attempt asynchronous commit
                            var notifyInfo = new DbNotify();
                            commitTasks.Add(notifyInfo.Task);
                            sub.Commit(notifyInfo);
                        }

                        // Wait for commit operations to complete
                        await TaskExtra
                            .WhenAllOrEmpty(commitTasks.ToArray())
                            .ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        if (Logger.IsDebugEnabled())
                        {
                            Logger.Debug($"Commit failed - rolling back\n\t{e.Message}");
                        }
                        performCommit = false;
                    }
                }

                if (!performCommit)
                {
                    // Rollback all objects in the commit list and throw
                    var rollbackTasks = new List<Task>();
                    foreach (var sub in commitList)
                    {
                        var rollback = new DbNotify();
                        try
                        {
                            rollbackTasks.Add(rollback.Task);
                            sub.Rollback(rollback);
                        }
                        catch
                        {
                            // Ignore exceptions during rollback
                        }
                    }

                    // Wait for rollback operations to complete
                    await TaskExtra
                        .WhenAllOrEmpty(rollbackTasks.ToArray())
                        .WithTimeout(TimeSpan.FromSeconds(5))
                        .ConfigureAwait(false);
                }

                // Write journal entry for transaction state
                await WriteEndXact(performCommit).ConfigureAwait(false);

                // Notify candidates that transaction has completed
                var completeList = new List<IPageEnlistmentNotification>(
                    commitList.Count > 0 ? commitList : _subEnlistments);
                foreach (var sub in completeList)
                {
                    try
                    {
                        sub.Complete();
                    }
                    catch(Exception e)
                    {
                        if (Logger.IsWarnEnabled())
                        {
                            Logger.WarnException($"Exception caught during transaction completion - this will be ignored\n\t{e.Message}", e);
                        }
                    }
                }

                if (Logger.IsDebugEnabled())
                {
                    Logger.Debug($"{_transactionId} => Commit completed - discarding xact scope");
                }
            }
            catch (Exception)
            {
                // If commit fails then mark as aborted.
            }
            finally
            {
                // Release other objects
                await ReleaseAsync().ConfigureAwait(false);
            }
            return true;
        }

        /// <summary>
        /// Rollbacks this instance.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Not in a transaction!</exception>
        public async Task<bool> RollbackAsync()
        {
            CheckNotCompleted();

            // If we are not in a transaction then throw up
            if (_transactionCount == 0)
            {
                throw new InvalidOperationException("Not in a transaction!");
            }

            // If we are rolling back a nested transaction then ignore
            if (Interlocked.Decrement(ref _transactionCount) > 0)
            {
                // Signal that a nested transaction has rolled back
                //	this will stop outer transaction from committing
                _nestedRollbackTriggered = true;
                return false;
            }

            // If we haven't written the start transaction mark then exit
            //	since we haven't done any transactionable work
            if (!_isBeginLogWritten && _subEnlistments.Count == 0)
            {
                return true;
            }

            try
            {
                _isCompleting = true;
                if (Logger.IsDebugEnabled())
                {
                    Logger.Debug($"{_transactionId} => Preparing rollback on {_subEnlistments.Count} sub-enlistments");
                }
                var rollbackTasks = new List<Task>();
                foreach (var sub in _subEnlistments)
                {
                    var notify = new DbNotify();
                    try
                    {
                        rollbackTasks.Add(notify.Task);
                        sub.Rollback(notify);
                    }
                    catch
                    {
                    }
                }

                // Wait for rollback operations to complete
                await TaskExtra
                    .WhenAllOrEmpty(rollbackTasks.ToArray())
                    .WithTimeout(TimeSpan.FromSeconds(5))
                    .ConfigureAwait(false);

                // Write rollback record
                await WriteEndXact(false).ConfigureAwait(false);

                // Notify commit list - transaction has been completed
                foreach (var sub in _subEnlistments)
                {
                    sub.Complete();
                }

                if (LoggingDevice != null)
                {
                    // Rollback our transaction log entries
                    await LoggingDevice
                        .RollbackTransactionsAsync(_transactionLogs)
                        .ConfigureAwait(false);
                }
            }
            catch (Exception)
            {
                // If commit fails then mark as aborted.
            }
            finally
            {
                // Release locked data pages
                await ReleaseAsync().ConfigureAwait(false);
            }
            return true;
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing">
        /// <c>true</c> to release both managed and unmanaged resources; 
        /// <c>false</c> to release only unmanaged resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_isCompleted && !_isCompleting)
            {
                if (Logger.IsWarnEnabled())
                {
                    Logger.Warn($"{_transactionId} => In-progress transaction disposed - performing implicit rollback");
                }

                // Force rollback of the current transaction
                RollbackAsync().Wait(Timeout);
            }
        }
        #endregion

        #region Private Methods
        private void CheckNotCompleted()
        {
            if (_isCompleted)
            {
                throw new InvalidOperationException("Transaction already completed.");
            }
        }

        private async Task ReleaseAsync()
        {
            // Should already be set but just make sure...
            _isCompleting = true;

            // Clear transaction logs
            _transactionLogs.Clear();

            // Release all locks
            foreach (var transactionLock in _transactionLockOwnerBlocks.Values)
            {
                await transactionLock.ReleaseAllAsync().ConfigureAwait(false);
            }
            _transactionLockOwnerBlocks.Clear();

            // Cleanup enlistments that implement IDisposable
            foreach (var enlistment in _subEnlistments)
            {
                var disp = enlistment as IDisposable;
                if (disp != null)
                {
                    //disp.Dispose();
                }
            }
            _subEnlistments.Clear();

            // Throw away transaction context
            _transactionId = TransactionId.Zero;
            _logDevice = null;

            // Perform final state update
            _isCompleted = true;
            _isCompleting = false;
        }

        private void EnlistInTransaction()
        {
            try
            {
                _logDevice = _lifetimeScope.Resolve<IMasterLogPageDevice>();
                _transactionId = _logDevice.GetNextTransactionId();
            }
            catch (DependencyResolutionException)
            {
                if (Logger.IsWarnEnabled())
                {
                    Logger.Warn("Master log device not resolvable - faking transaction identifier");
                }

                _transactionId = new TransactionId(1);
            }

            if (Logger.IsDebugEnabled())
            {
                Logger.Debug($"{_transactionId} => Returned from TryEnlistTransaction()");
            }
        }

        private async Task WriteBeginXact()
        {
            if (!_isBeginLogWritten)
            {
                _isBeginLogWritten = true;
                if (Logger.IsDebugEnabled())
                {
                    Logger.Debug($"{_transactionId} => Writing begin xact to log");
                }
                if (LoggingDevice != null)
                {
                    // Write begin transaction entry
                    await LoggingDevice.WriteEntryAsync(new BeginTransactionLogEntry(_transactionId)).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Writes either a commit log entry or a rollback log entry
        /// to the database log device.
        /// </summary>
        /// <param name="commit">
        /// Boolean value indicating true for commit and false for rollback.
        /// </param>
        private async Task WriteEndXact(bool commit)
        {
            // No need to write end-xact if begin not written
            if (_isBeginLogWritten && LoggingDevice != null)
            {
                if (Logger.IsDebugEnabled())
                {
                    var endXactType = commit ? "commit" : "rollback";
                    Logger.Debug($"{_transactionId} => Writing end xact ({endXactType}) to log");
                }

                LogEntry entry;
                if (commit)
                {
                    entry = new CommitTransactionLogEntry(_transactionId);
                }
                else
                {
                    entry = new RollbackTransactionLogEntry(_transactionId);
                }
                await LoggingDevice.WriteEntryAsync(entry).ConfigureAwait(false);
            }
        }
        #endregion
    }
}
