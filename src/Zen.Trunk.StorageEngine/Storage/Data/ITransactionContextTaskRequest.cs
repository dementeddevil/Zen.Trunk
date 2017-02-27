using Zen.Trunk.Storage.Locking;
using Zen.Trunk.Utils;

namespace Zen.Trunk.Storage.Data
{
    /// <summary>
    /// <c>ITransactionContextTaskRequest</c> defines a contract implemented
    /// by <see cref="TransactionContextTaskRequest{TResult}"/> to support
    /// capturing the session and transaction contexts.
    /// </summary>
    public interface ITransactionContextTaskRequest
    {
        /// <summary>
        /// Gets or sets the session context.
        /// </summary>
        /// <value>
        /// The session context.
        /// </value>
        ITrunkSession SessionContext { get; set; }

        /// <summary>
        /// Gets or sets the transaction context.
        /// </summary>
        /// <value>
        /// The transaction context.
        /// </value>
        ITrunkTransaction TransactionContext { get; set; }
    }
}