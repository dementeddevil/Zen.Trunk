namespace Zen.Trunk.Storage.Locking
{
	using System;
	using System.Threading.Tasks;
	using System.Transactions;
	using Zen.Trunk.Storage.Log;

	public interface ITrunkTransaction : IDisposable
	{
		TransactionId TransactionId
		{
			get;
		}

		IsolationLevel IsolationLevel
		{
			get;
		}

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

		Task WriteLogEntry(TransactionLogEntry entry);

		Task<bool> Commit();

		Task<bool> Rollback();
	}
}
