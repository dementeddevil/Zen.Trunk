using System;
using Zen.Trunk.Storage.BufferFields;

namespace Zen.Trunk.Storage.Log
{
    /// <summary>
    /// Serves as a base class for a transaction log entry.
    /// </summary>
    /// <remarks>
    /// Transaction logs are stamped with the transaction Id and this Id is
    /// used to group all logs related to a given transaction to allow a
    /// transaction to be rolled forward or rolled back during recovery.
    /// </remarks>
    [Serializable]
    public class TransactionLogEntry : LogEntry
    {
        #region Private Fields
        private readonly BufferFieldUInt32 _transactionId;
        #endregion

        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionLogEntry"/> class.
        /// </summary>
        /// <param name="logType">Type of the log.</param>
        public TransactionLogEntry(LogEntryType logType)
            : base(logType)
        {
            _transactionId = new BufferFieldUInt32(base.LastField);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionLogEntry"/> class.
        /// </summary>
        /// <param name="logType">Type of the log.</param>
        /// <param name="transactionId">The transaction identifier.</param>
        public TransactionLogEntry(LogEntryType logType, TransactionId transactionId)
            : base(logType)
        {
            _transactionId = new BufferFieldUInt32(base.LastField, transactionId.Value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionLogEntry"/> class.
        /// </summary>
        protected TransactionLogEntry()
        {
            _transactionId = new BufferFieldUInt32(base.LastField);
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the transaction identifier.
        /// </summary>
        /// <value>
        /// The transaction identifier.
        /// </value>
        public TransactionId TransactionId => new TransactionId(_transactionId.Value);
        #endregion

        #region Protected Properties
        /// <summary>
        /// Gets the last buffer field object.
        /// </summary>
        /// <value>
        /// A <see cref="T:BufferField" /> object.
        /// </value>
        protected override BufferField LastField => _transactionId;
        #endregion

        #region Internal Methods
        internal void RewriteTransactionId(TransactionId transactionId)
        {
            _transactionId.Value = transactionId.Value;
        }
        #endregion
    }
}