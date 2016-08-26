using System;
using System.Diagnostics;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Autofac;

namespace Zen.Trunk.Storage.Locking
{
	/// <summary>
	/// <c>TrunkTransactionContext</c> is a object that tracks the current
	/// transaction for the current execution context.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This object makes it possible for any method in a call chain to update
	/// the transaction with dirty pages.
	/// </para>
	/// <para>
	/// Unfortunately since this object alters the Execution Context this will
	/// make all async work performed slower as the TPL is optimised for scenarios
	/// where the execution context is not modified. We will need to consider
	/// whether it is worthwhile pursuing an alternative strategy for tracking
	/// the current transaction...
	/// </para>
	/// </remarks>
	public static class TrunkTransactionContext
	{
	    private const string LogicalContextName = "TrunkTransactionContext";

	    private class TrunkTransactionScope : IDisposable
		{
			private ITrunkTransaction _oldContext;
			private bool _disposed;

			public TrunkTransactionScope(ITrunkTransaction newContext)
			{
				_oldContext = TrunkTransactionContext.Current;
				TrunkTransactionContext.Current = newContext;

                TraceTransaction("Enter", _oldContext, TrunkTransactionContext.Current);
			}

            public void Dispose()
			{
				DisposeManagedObjects();
			}

			private void DisposeManagedObjects()
			{
				if (!_disposed)
				{
					_disposed = true;

					var prevContext = TrunkTransactionContext.Current;
					TrunkTransactionContext.Current = _oldContext;

				    TraceTransaction("Leave", prevContext, TrunkTransactionContext.Current);
				}

				_oldContext = null;
			}

		    private void TraceTransaction(string action, ITrunkTransaction prev, ITrunkTransaction next)
		    {
		        var threadId = Thread.CurrentThread.ManagedThreadId;
		        var prevTransactionId = prev != null ? prev.TransactionId.ToString() : "N/A";
                var nextTransactionId = next != null ? next.TransactionId.ToString() : "N/A";
		        Trace.TraceInformation(
		            $"{action} transaction scope on thread {threadId} switching transaction from {prevTransactionId} to {nextTransactionId}");
		    }
		}

		public static ITrunkTransaction Current
		{
			get
			{
				var transaction = (ITrunkTransaction)CallContext.LogicalGetData(LogicalContextName);
				if (transaction != null)
				{
					var priv = transaction as ITrunkTransactionPrivate;
					if (priv != null && priv.IsCompleted)
					{
						CallContext.FreeNamedDataSlot(LogicalContextName);
						return null;
					}
				}
				return transaction;
			}
			private set
			{
				if (value != null)
				{
					CallContext.LogicalSetData(LogicalContextName, value);
				}
				else
				{
					CallContext.FreeNamedDataSlot(LogicalContextName);
				}
			}
		}

        internal static TransactionLockOwnerBlock TransactionLocks
        {
            get
            {
                var privTxn = TrunkTransactionContext.Current as ITrunkTransactionPrivate;
                return privTxn?.TransactionLocks;
            }
        }

        public static void BeginTransaction(ILifetimeScope lifetimeScope)
		{
			BeginTransaction(new TrunkTransaction(lifetimeScope));
		}

		public static void BeginTransaction(ILifetimeScope lifetimeScope, TransactionOptions transactionOptions)
		{
			BeginTransaction(new TrunkTransaction(lifetimeScope, transactionOptions));
		}

		public static void BeginTransaction(ILifetimeScope lifetimeScope, TimeSpan timeout)
		{
			BeginTransaction(new TrunkTransaction(lifetimeScope, timeout));
		}

		public static void BeginTransaction(ILifetimeScope lifetimeScope, IsolationLevel isoLevel, TimeSpan timeout)
		{
			BeginTransaction(new TrunkTransaction(lifetimeScope, isoLevel, timeout));
		}

		public static async Task Commit()
		{
			var txn = Current as ITrunkTransactionPrivate;
			if (txn != null)
			{
				var result = await txn.Commit().WithTimeout(txn.Timeout);
				if (result)
				{
					Current = null;
				}
			}
		}

		public static async Task Rollback()
		{
			var txn = Current as ITrunkTransactionPrivate;
			if (txn != null)
			{
				var result = await txn.Rollback().WithTimeout(txn.Timeout);
				if (result)
				{
					Current = null;
				}
			}
		}

		internal static void BeginTransaction(ITrunkTransaction txn)
		{
			if (Current == null)
			{
				Current = txn;
			}
			else
			{
				var priv = Current as ITrunkTransactionPrivate;
			    priv?.BeginNestedTransaction();
			}
		}

		internal static IDisposable SwitchTransactionContext(ITrunkTransaction newContext)
		{
			return new TrunkTransactionScope(newContext);
		}
	}
}
