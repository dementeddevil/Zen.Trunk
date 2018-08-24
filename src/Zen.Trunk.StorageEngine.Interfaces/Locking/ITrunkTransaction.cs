using System;
using System.Transactions;

namespace Zen.Trunk.Storage.Locking
{
    /// <summary>
    /// <c>ITrunkTransaction</c> defines an in-flight transaction.
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
        TransactionId TransactionId { get; }

        /// <summary>
        /// Gets the isolation level.
        /// </summary>
        /// <value>
        /// The isolation level.
        /// </value>
        IsolationLevel IsolationLevel { get; }

        /// <summary>
        /// Gets the timeout.
        /// </summary>
        /// <value>
        /// The timeout.
        /// </value>
        TimeSpan Timeout { get; }
	}
}
