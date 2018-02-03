using System;

namespace Zen.Trunk.Storage.Log
{
    /// <summary>
    /// Defines the start of a transaction.
    /// </summary>
    /// <remarks>
    /// For a given transaction Id this will be the first record written
    /// using that Id.
    /// </remarks>
    [Serializable]
    public class BeginTransactionLogEntry : TransactionLogEntry
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BeginTransactionLogEntry"/> class.
        /// </summary>
        /// <param name="transactionId">The transaction identifier.</param>
        public BeginTransactionLogEntry(TransactionId transactionId)
            : base(LogEntryType.BeginXact, transactionId)
        {
        }

        internal BeginTransactionLogEntry()
        {
        }
    }
}