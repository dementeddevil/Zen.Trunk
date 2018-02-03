using System;

namespace Zen.Trunk.Storage.Log
{
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
}