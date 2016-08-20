namespace Zen.Trunk.Storage.Locking
{
	using System;

	public abstract class PreparingPageEnlistment : PageEnlistment
	{
		public abstract void ForceRollback();
		public abstract void ForceRollback(Exception error);
		public abstract void Prepared();
	}
}
