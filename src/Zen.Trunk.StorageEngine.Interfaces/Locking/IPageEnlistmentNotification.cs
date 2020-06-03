namespace Zen.Trunk.Storage.Locking
{
    /// <summary>
    /// <c>IPageEnlistmentNotification</c> is implemented by a <see cref="PageBuffer"/> in
    /// order to participate with a two-phase commit page transaction.
    /// </summary>
    public interface IPageEnlistmentNotification
	{
        /// <summary>
        /// Instructs the buffer to prepare for commiting changes.
        /// </summary>
        /// <param name="prepare">
        /// A callback object that contains methods to be called as part of the preparation process.
        /// </param>
        void Prepare(PreparingPageEnlistment prepare);

        /// <summary>
        /// Instructs the buffer to commit changes.
        /// </summary>
        /// <param name="enlistment">
        /// A callback object that contains methods to be called as part of the commit process.
        /// </param>
        void Commit(PageEnlistment enlistment);

        /// <summary>
        /// Instructs the buffer to rollback changes.
        /// </summary>
        /// <param name="enlistment">The enlistment.</param>
        void Rollback(PageEnlistment enlistment);

        /// <summary>
        /// Instructs the buffer that the transaction has completed.
        /// </summary>
        void Complete();
	}
}
