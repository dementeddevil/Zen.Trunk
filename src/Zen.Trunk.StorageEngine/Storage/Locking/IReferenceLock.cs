namespace Zen.Trunk.Storage.Locking
{
	/// <summary>
	/// Interface which exposes reference counting primatives for locking
	/// purposes.
	/// </summary>
	public interface IReferenceLock
	{
		void AddRefLock ();
		void ReleaseLock ();
	}
}
