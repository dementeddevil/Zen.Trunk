using System;

namespace Zen.Trunk.Storage.Log
{
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
}