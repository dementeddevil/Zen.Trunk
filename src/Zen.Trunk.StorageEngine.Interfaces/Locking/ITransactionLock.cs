using System;
using System.Threading.Tasks;

namespace Zen.Trunk.Storage.Locking
{
    public interface ITransactionLock<in TLockTypeEnum> : IReferenceLock
        where TLockTypeEnum : struct, IComparable, IConvertible, IFormattable
    {
        /// <summary>
        /// Gets/sets the lock Id.
        /// </summary>
        string Id { get; set; }

        /// <summary>
        /// Determines whether the current transaction has the given lock type.
        /// </summary>
        /// <param name="lockType"></param>
        /// <returns>Boolean. True indicates lock is held.</returns>
        /// <remarks>
        /// This method will throw if the current thread does not have a
        /// transaction context.
        /// </remarks>
        Task<bool> HasLockAsync(TLockTypeEnum lockType);

        /// <summary>
        /// Acquires the specified lock type.
        /// </summary>
        /// <param name="lockType"></param>
        /// <param name="timeout"></param>
        /// <remarks>
        /// <para>
        /// This method will throw if the current thread does not have a
        /// transaction context.
        /// </para>
        /// <para>
        /// This method will throw if the lock is not acquired within the
        /// specified timeout period.
        /// </para>
        /// </remarks>
        Task LockAsync(TLockTypeEnum lockType, TimeSpan timeout);

        /// <summary>
        /// Releases the lock by downgrading to the none lock state
        /// </summary>
        Task UnlockAsync();

        /// <summary>
        /// Releases the lock by downgrading to the given lock type.
        /// </summary>
        /// <param name="newLockType"></param>
        /// <remarks>
        /// The new lock type should be of a lower state than the
        /// current lock type or this method may throw an exception.
        /// The lock timeout period is fixed at 10 seconds so get it
        /// right!
        /// </remarks>
        Task UnlockAsync(TLockTypeEnum newLockType);
    }
}