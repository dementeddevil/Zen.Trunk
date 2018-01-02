using System;
using System.Diagnostics;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Autofac;
using Zen.Trunk.Extensions;

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
    /// </remarks>
    public static class TrunkTransactionContext
    {
        private class TrunkTransactionScope : IDisposable
        {
            private ITrunkTransaction _oldContext;
            private bool _disposed;

            public TrunkTransactionScope(ITrunkTransaction newContext)
            {
                _oldContext = Current;
                Current = newContext;

                TraceTransaction("Enter", _oldContext, Current);
            }

            public void Dispose()
            {
                Dispose(true);
            }

            private void Dispose(bool disposing)
            {
                if (!_disposed)
                {
                    _disposed = true;

                    var prevContext = Current;
                    Current = _oldContext;

                    TraceTransaction("Leave", prevContext, Current);
                }

                _oldContext = null;
            }

            private void TraceTransaction(string action, ITrunkTransaction prev, ITrunkTransaction next)
            {
                if (prev?.TransactionId == next?.TransactionId)
                {
                    return;
                }

                var threadId = Thread.CurrentThread.ManagedThreadId;
                var prevTransactionId = prev?.TransactionId.ToString() ?? "N/A";
                var nextTransactionId = next?.TransactionId.ToString() ?? "N/A";
                Trace.TraceInformation(
                    $"{action} transaction scope on thread {threadId} switching transaction from {prevTransactionId} to {nextTransactionId}");
            }
        }

        private const string LogicalContextName = "TrunkTransactionContext";

        /// <summary>
        /// Gets the current trunk transaction.
        /// </summary>
        /// <value>
        /// An instance of <see cref="ITrunkTransaction"/> representing the
        /// current transaction; otherwise <c>null</c> if no transaction is
        /// in progress.
        /// </value>
        public static ITrunkTransaction Current
        {
            get
            {
                var transaction = (ITrunkTransaction)CallContext.LogicalGetData(LogicalContextName);
                var priv = transaction as ITrunkTransactionPrivate;
                if (priv != null && priv.IsCompleted)
                {
                    CallContext.FreeNamedDataSlot(LogicalContextName);
                    return null;
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

        /// <summary>
        /// Begins the transaction.
        /// </summary>
        /// <param name="lifetimeScope">The lifetime scope.</param>
        public static void BeginTransaction(ILifetimeScope lifetimeScope)
        {
            BeginTransaction(new TrunkTransaction(lifetimeScope));
        }

        /// <summary>
        /// Begins the transaction.
        /// </summary>
        /// <param name="lifetimeScope">The lifetime scope.</param>
        /// <param name="transactionOptions">The transaction options.</param>
        public static void BeginTransaction(ILifetimeScope lifetimeScope, TransactionOptions transactionOptions)
        {
            BeginTransaction(new TrunkTransaction(lifetimeScope, transactionOptions));
        }

        /// <summary>
        /// Begins the transaction.
        /// </summary>
        /// <param name="lifetimeScope">The lifetime scope.</param>
        /// <param name="timeout">The timeout.</param>
        public static void BeginTransaction(ILifetimeScope lifetimeScope, TimeSpan timeout)
        {
            BeginTransaction(new TrunkTransaction(lifetimeScope, timeout));
        }

        /// <summary>
        /// Begins the transaction.
        /// </summary>
        /// <param name="lifetimeScope">The lifetime scope.</param>
        /// <param name="isoLevel">The iso level.</param>
        /// <param name="timeout">The timeout.</param>
        public static void BeginTransaction(ILifetimeScope lifetimeScope, IsolationLevel isoLevel, TimeSpan timeout)
        {
            BeginTransaction(new TrunkTransaction(lifetimeScope, isoLevel, timeout));
        }

        /// <summary>
        /// Commits the transaction.
        /// </summary>
        /// <returns></returns>
        public static async Task CommitAsync()
        {
            var txn = Current as ITrunkTransactionPrivate;
            if (txn != null)
            {
                var result = await txn.CommitAsync()
                    .WithTimeout(txn.Timeout)
                    .ConfigureAwait(false);
                if (result)
                {
                    Current = null;
                }
            }
        }

        /// <summary>
        /// Rollbacks the transaction.
        /// </summary>
        /// <returns></returns>
        public static async Task RollbackAsync()
        {
            var transaction = Current as ITrunkTransactionPrivate;
            if (transaction != null)
            {
                var result = await transaction
                    .RollbackAsync()
                    .WithTimeout(transaction.Timeout)
                    .ConfigureAwait(false);
                if (result)
                {
                    Current = null;
                }
            }
        }

        internal static TransactionLockOwnerBlock GetTransactionLockOwnerBlock(IDatabaseLockManager lockManager)
        {
            var privTxn = Current as ITrunkTransactionPrivate;
            return privTxn?.GetTransactionLockOwnerBlock(lockManager);
        }

        internal static IDisposable SwitchTransactionContext(ITrunkTransaction newContext)
        {
            return new TrunkTransactionScope(newContext);
        }

        private static void BeginTransaction(ITrunkTransaction txn)
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
    }
}
