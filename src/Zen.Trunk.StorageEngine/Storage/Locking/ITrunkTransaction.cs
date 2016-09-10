namespace Zen.Trunk.Storage.Locking
{
	using System;
	using System.Threading.Tasks;
	using System.Transactions;
	using Log;

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public interface ITrunkTransaction : IDisposable
	{
        /// <summary>
        /// Gets the transaction identifier.
        /// </summary>
        /// <value>
        /// The transaction identifier.
        /// </value>
        TransactionId TransactionId
		{
			get;
		}

        /// <summary>
        /// Gets the isolation level.
        /// </summary>
        /// <value>
        /// The isolation level.
        /// </value>
        IsolationLevel IsolationLevel
		{
			get;
		}

        /// <summary>
        /// Gets the timeout.
        /// </summary>
        /// <value>
        /// The timeout.
        /// </value>
        TimeSpan Timeout
		{
			get;
		}
	}

	internal interface ITrunkTransactionPrivate : ITrunkTransaction
	{
		MasterLogPageDevice LoggingDevice
		{
			get;
		}

		TransactionLockOwnerBlock TransactionLocks
		{
			get;
		}

		IDatabaseLockManager LockManager
		{
			get;
		}

		bool IsCompleted
		{
			get;
		}

		void BeginNestedTransaction();

		void Enlist(IPageEnlistmentNotification notify);

		Task WriteLogEntryAsync(TransactionLogEntry entry);

		Task<bool> CommitAsync();

		Task<bool> RollbackAsync();
	}
}
