using Zen.Trunk.Storage.Locking;
using Zen.Trunk.Utils;

namespace Zen.Trunk.Storage.Data
{
    /// <summary>
    /// <c>ITransactionContextTaskRequest</c> defines a contract implemented
    /// by a <see cref="TaskRequest{TResult}"/> to support capturing the
    /// transaction context.
    /// </summary>
    public interface ITransactionContextTaskRequest
    {
        /// <summary>
        /// Gets or sets the transaction context.
        /// </summary>
        /// <value>
        /// The transaction context.
        /// </value>
        ITrunkTransaction TransactionContext
        {
            get;
            set;
        }
    }
}