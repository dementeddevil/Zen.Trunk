﻿namespace Zen.Trunk.Storage.Locking
{
	using System;
	using System.Diagnostics;
	using System.Runtime.Remoting.Messaging;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Transactions;

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
		public class TrunkTransactionScope : IDisposable
		{
			private ITrunkTransaction _oldContext;
			private bool _disposed;

			public TrunkTransactionScope(ITrunkTransaction newContext)
			{
				_oldContext = TrunkTransactionContext.Current;
				TrunkTransactionContext.Current = newContext;

				Trace.TraceInformation("Enter transaction scope on thread {0} switching transaction from {1} to {2}",
					Thread.CurrentThread.ManagedThreadId,
					(_oldContext != null) ? _oldContext.TransactionId.ToString() : "N/A",
					TrunkTransactionContext.Current.TransactionId);
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

					Trace.TraceInformation("Leave transaction scope on thread {0} switching transaction from {1} to {2}",
						Thread.CurrentThread.ManagedThreadId,
						(prevContext != null) ? prevContext.TransactionId.ToString() : "N/A",
						(_oldContext != null) ? _oldContext.TransactionId.ToString() : "N/A");
				}

				_oldContext = null;
			}
		}

		public static ITrunkTransaction Current
		{
			get
			{
				ITrunkTransaction transaction =
					(ITrunkTransaction)CallContext.LogicalGetData("TrunkTransactionContext");
				if (transaction != null)
				{
					var priv = transaction as ITrunkTransactionPrivate;
					if (priv != null && priv.IsCompleted)
					{
						CallContext.FreeNamedDataSlot("TrunkTransactionContext");
						return null;
					}
				}
				return transaction;
			}
			private set
			{
				if (value != null)
				{
					CallContext.LogicalSetData("TrunkTransactionContext", value);
				}
				else
				{
					CallContext.FreeNamedDataSlot("TrunkTransactionContext");
				}
			}
		}

		public static void BeginTransaction(IServiceProvider serviceProvider)
		{
			BeginTransaction(new TrunkTransaction(serviceProvider));
		}

		public static void BeginTransaction(IServiceProvider serviceProvider, TransactionOptions transactionOptions)
		{
			BeginTransaction(new TrunkTransaction(serviceProvider, transactionOptions));
		}

		public static void BeginTransaction(IServiceProvider serviceProvider, TimeSpan timeout)
		{
			BeginTransaction(new TrunkTransaction(serviceProvider, timeout));
		}

		public static void BeginTransaction(IServiceProvider serviceProvider, IsolationLevel isoLevel, TimeSpan timeout)
		{
			BeginTransaction(new TrunkTransaction(serviceProvider, isoLevel, timeout));
		}

		public static async Task Commit()
		{
			ITrunkTransactionPrivate txn =
				Current as ITrunkTransactionPrivate;
			if (txn != null)
			{
				var result = await txn
					.Commit()
					.WithTimeout(txn.Timeout);
				if (result)
				{
					Current = null;
				}
			}
		}

		public static async Task Rollback()
		{
			ITrunkTransactionPrivate txn =
				Current as ITrunkTransactionPrivate;
			if (txn != null)
			{
				var result = await txn
					.Rollback()
					.WithTimeout(txn.Timeout);
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
				ITrunkTransactionPrivate priv = Current as ITrunkTransactionPrivate;
				if (priv != null)
				{
					priv.BeginNestedTransaction();
				}
			}
		}

		internal static IDisposable SwitchTransactionContext(ITrunkTransaction newContext)
		{
			return new TrunkTransactionScope(newContext);
		}
	}
}
