namespace Zen.Trunk.Storage.Locking
{
	/// <summary>
	/// Interface which exposes reference counting primitives for locking
	/// purposes.
	/// </summary>
	public interface IReferenceLock
	{
        /// <summary>
        /// Adds the reference lock.
        /// </summary>
        void AddRefLock ();

        /// <summary>
        /// Releases the lock.
        /// </summary>
        void ReleaseRefLock ();
	}
}
