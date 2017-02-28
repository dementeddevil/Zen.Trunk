namespace Zen.Trunk.Storage.Locking
{
    /// <summary>
    /// <c>PageEnlistment</c> is passed to objects participating in the page
    /// two-phase commit protocol used when commiting transactions.
    /// </summary>
    public abstract class PageEnlistment
	{
        /// <summary>
        /// Instructs the transaction manager that work is done.
        /// </summary>
        public abstract void Done();
	}
}
