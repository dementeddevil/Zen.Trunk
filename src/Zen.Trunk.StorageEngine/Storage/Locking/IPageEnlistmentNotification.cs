namespace Zen.Trunk.Storage.Locking
{
    /// <summary>
    /// <c>IPageEnlistmentNotification</c> is implemented by a resource in
    /// order to participate with a page transaction.
    /// </summary>
    public interface IPageEnlistmentNotification
	{
        /// <summary>
        /// Prepares the specified prepare.
        /// </summary>
        /// <param name="prepare">The prepare.</param>
        void Prepare(PreparingPageEnlistment prepare);

        /// <summary>
        /// Commits the specified enlistment.
        /// </summary>
        /// <param name="enlistment">The enlistment.</param>
        void Commit(PageEnlistment enlistment);

        /// <summary>
        /// Rollbacks the specified enlistment.
        /// </summary>
        /// <param name="enlistment">The enlistment.</param>
        void Rollback(PageEnlistment enlistment);

        /// <summary>
        /// Completes this instance.
        /// </summary>
        void Complete();
	}
}
