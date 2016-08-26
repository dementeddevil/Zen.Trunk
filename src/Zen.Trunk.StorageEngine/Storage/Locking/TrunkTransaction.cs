using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Autofac;
using Autofac.Core;
using Zen.Trunk.Storage.Data;
using Zen.Trunk.Storage.Log;

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
        private class DBPrepare : PreparingPageEnlistment
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

        private class DBNotify : PageEnlistment
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
        private readonly ILifetimeScope _lifetimeScope;
        private readonly List<IPageEnlistmentNotification> _subEnlistments = new List<IPageEnlistmentNotification>();
        private readonly List<TransactionLogEntry> _transactionLogs = new List<TransactionLogEntry>();
        private TransactionLockOwnerBlock _lockOwner;
        private TransactionOptions _options;
        private bool _isBeginLogWritten = false;
        private uint _transactionId = 1;
        private int _transactionCount = 1;
        private MasterLogPageDevice _logDevice = null;
        private bool _nestedRollbackTriggered = false;
        private bool _isCompleting = false;
        private bool _isCompleted = false;
        private readonly ITracer _tracer;
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
            _tracer = TS.CreateCoreTracer("DatabaseTransaction");

            LockManager = _lifetimeScope.Resolve<IDatabaseLockManager>();
            TransactionLocks = new TransactionLockOwnerBlock(LockManager);
            TryEnlistInTransaction();
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the database transaction ID for this transaction.
        /// </summary>
        public uint TransactionId => _transactionId;

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
        /// Gets the logging device from the database device.
        /// </summary>
        public MasterLogPageDevice LoggingDevice
        {
            get
            {
                if (_logDevice == null)
                {
                    TryEnlistInTransaction();
                }
                return _logDevice;
            }
        }

        private void TryEnlistInTransaction()
        {
            try
            {
                if (_lifetimeScope.TryResolve<MasterLogPageDevice>(out _logDevice))
                {
                    _transactionId = _logDevice.GetNextTransactionId();
                }
            }
            catch (DependencyResolutionException)
            {
            }
        }

        public TransactionLockOwnerBlock TransactionLocks { get; }

        public IDatabaseLockManager LockManager { get; }

        public bool IsCompleted => _isCompleted;

        #endregion

        #region Public Methods
        public void Dispose()
        {
            DisposeManagedObjects();
        }

        public void BeginNestedTransaction()
        {
            Interlocked.Increment(ref _transactionCount);
        }

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

        public async Task WriteLogEntry(TransactionLogEntry entry)
        {
            // Ensure begin log record has been written
            if (!_isBeginLogWritten)
            {
                _tracer.WriteVerboseLine("Writing begin xact {0} to log", _transactionId);
                await WriteBeginXact().ConfigureAwait(false);
            }

            // Update log entry transaction ID as needed.
            if (entry.TransactionId != _transactionId)
            {
                entry.RewriteTransactionId(_transactionId);
            }

            if (LoggingDevice != null)
            {
                _transactionLogs.Add(entry);
                await LoggingDevice.WriteEntry(entry).ConfigureAwait(false);
            }
        }

        public async Task<bool> Commit()
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
                Release();
                return true;
            }

            try
            {
                _isCompleting = true;

                var performCommit = true;
                var prepTasks = new List<Task<bool>>();
                if (_nestedRollbackTriggered)
                {
                    _tracer.WriteVerboseLine(
                        "Rolling back {0} sub-enlistments due to nested transaction failure.",
                        _subEnlistments.Count);
                    performCommit = false;
                }
                else
                {
                    _tracer.WriteVerboseLine(
                        "Preparing commit on {0} sub-enlistments",
                        _subEnlistments.Count);

                    // Prepare our sub-enlistments (pages) for commit operation
                    foreach (var sub in _subEnlistments)
                    {
                        var prepInfo = new DBPrepare();
                        prepTasks.Add(prepInfo.Task);
                        try
                        {
                            sub.Prepare(prepInfo);
                        }
                        catch (Exception e)
                        {
                            _tracer.WriteVerboseLine(
                                "Prepare failed - rolling back\n\t{0}",
                                e.Message);
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
                            .WhenAllOrEmpty<bool>(prepTasks.ToArray())
                            .ConfigureAwait(false);
                        if (result != null)
                        {
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
                        _tracer.WriteVerboseLine("Prepare failed - rolling back\n\t{0}",
                            e.Message);
                        performCommit = false;
                    }
                }

                // If rollback was not forced nor error encountered then we can
                //	perform the second phase of the commit process.
                if (performCommit)
                {
                    // Notify all prepared objects that want to commit
                    _tracer.WriteVerboseLine("Committing {0} sub-enlistments",
                        commitList.Count);
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
                            // We only track page buffers
                            var page = sub as PageBuffer;
                            if (page != null)
                            {
                                if (tracker.Contains(page.PageId))
                                {
                                    continue;
                                }

                                tracker.Add(page.PageId);
                                var notifyInfo = new DBNotify();
                                commitTasks.Add(notifyInfo.Task);
                                sub.Commit(notifyInfo);
                            }
                        }

                        // Wait for commit operations to complete
                        await TaskExtra
                            .WhenAllOrEmpty(commitTasks.ToArray())
                            .ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        _tracer.WriteVerboseLine(
                            "Commit failed - rolling back\n\t{0}",
                            e.Message);
                        performCommit = false;
                    }
                }

                if (!performCommit)
                {
                    // Rollback all objects in the commit list and throw
                    var rollbackTasks = new List<Task>();
                    foreach (var rb in commitList)
                    {
                        var rollback = new DBNotify();
                        try
                        {
                            rollbackTasks.Add(rollback.Task);
                            rb.Rollback(rollback);
                        }
                        catch
                        {
                            // Ignore exceptions during rollback
                        }
                    }

                    // Wait for rollback operations to complete
                    TaskExtra.WhenAllOrEmpty(rollbackTasks.ToArray())
                        .WithTimeout(TimeSpan.FromSeconds(5))
                        .Wait();
                }

                // Write journal entry for transaction state
                _tracer.WriteVerboseLine("Writing end xact {0} to log", _transactionId);
                await WriteEndXact(performCommit).ConfigureAwait(false);

                // Notify candidates that transaction has completed
                var completeList =
                    new List<IPageEnlistmentNotification>();
                if (commitList.Count > 0)
                {
                    completeList.AddRange(commitList);
                }
                else
                {
                    completeList.AddRange(_subEnlistments);
                }
                foreach (var rb in completeList)
                {
                    try
                    {
                        rb.Complete();
                    }
                    catch
                    {
                    }
                }

                _tracer.WriteVerboseLine("Commit completed - discarding xact scope");
            }
            catch (Exception)
            {
                // If commit fails then mark as aborted.
            }
            finally
            {
                // Release other objects
                Release();
            }
            return true;
        }

        public async Task<bool> Rollback()
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
            //	since we haven't done any transactable work
            if (!_isBeginLogWritten && _subEnlistments.Count == 0)
            {
                return true;
            }

            try
            {
                _isCompleting = true;
                _tracer.WriteVerboseLine("Preparing rollback on {0} sub-enlistments",
                    _subEnlistments.Count);
                var rollbackTasks = new List<Task>();
                foreach (var sub in _subEnlistments)
                {
                    var notify = new DBNotify();
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
                TaskExtra.WhenAllOrEmpty(rollbackTasks.ToArray())
                    .WithTimeout(TimeSpan.FromSeconds(5))
                    .Wait();

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
                        .RollbackTransactions(_transactionLogs)
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
                Release();
            }
            return true;
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Releases managed resources
        /// </summary>
        protected virtual void DisposeManagedObjects()
        {
            if (!_isCompleted && !_isCompleting)
            {
                _tracer.WriteWarningLine(
                    "In-progress transaction disposed - performing implicit rollback");

                // Force rollback of the current transaction
                Rollback().Wait(Timeout);
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

        private void Release()
        {
            // Should already be set but just make sure...
            _isCompleting = true;

            // Clear transaction logs
            _transactionLogs.Clear();

            // Release all locks
            if (_lockOwner != null)
            {
                _lockOwner.ReleaseAll();
                _lockOwner = null;
            }

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
            _transactionId = 0;
            //LockManager = null;
            _logDevice = null;

            // Perform final state update
            _isCompleted = true;
            _isCompleting = false;
        }

        private async Task WriteBeginXact()
        {
            if (!_isBeginLogWritten)
            {
                if (LoggingDevice != null)
                {
                    // Write begin transaction entry
                    await LoggingDevice.WriteEntry(new BeginTransactionLogEntry(_transactionId)).ConfigureAwait(false);
                }
                _isBeginLogWritten = true;
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
                LogEntry entry = null;
                if (commit)
                {
                    entry = new CommitTransactionLogEntry(_transactionId);
                }
                else
                {
                    entry = new RollbackTransactionLogEntry(_transactionId);
                }
                await LoggingDevice.WriteEntry(entry).ConfigureAwait(false);
            }
        }
        #endregion
    }
}
