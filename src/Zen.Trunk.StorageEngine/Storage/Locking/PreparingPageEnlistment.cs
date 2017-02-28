using System;

namespace Zen.Trunk.Storage.Locking
{
    /// <summary>
    /// <c>PreparingPageEnlistment</c> is passed to objects participating in
    /// the page two-phase commit protocol used when commiting transactions.
    /// </summary>
    /// <seealso cref="Zen.Trunk.Storage.Locking.PageEnlistment" />
    public abstract class PreparingPageEnlistment : PageEnlistment
	{
        /// <summary>
        /// Instructs the transaction manager to rollback the current transaction.
        /// </summary>
        public abstract void ForceRollback();

        /// <summary>
        /// Instructs the transaction manager to rollback the current transaction
        /// passing the specified exception.
        /// </summary>
        /// <param name="error">The error.</param>
        public abstract void ForceRollback(Exception error);
       
        /// <summary>
        /// Instructs the transaction manager that the resource is prepared and
        /// should receive further calls during the commit process.
        /// </summary>
        /// <remarks>
        /// If <see cref="PageEnlistment.Done"/> is called instead of this method
        /// then preparation is considered successful however the resource will
        /// not receive any further calls for this transaction.
        /// </remarks>
        public abstract void Prepared();
	}
}
