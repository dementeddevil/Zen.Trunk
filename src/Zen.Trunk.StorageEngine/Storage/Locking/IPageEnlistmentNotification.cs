namespace Zen.Trunk.Storage.Locking
{
	public interface IPageEnlistmentNotification
	{
		void Prepare(PreparingPageEnlistment prepare);

		void Commit(PageEnlistment enlistment);

		void Rollback(PageEnlistment enlistment);

		void Complete();
	}
}
