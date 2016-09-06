//--------------------------------------------------------------------------
// 
//  Copyright (c) Microsoft Corporation.  All rights reserved. 
// 
//  File: SpinLockClass.cs
//
//--------------------------------------------------------------------------

using System.Threading.Tasks;

namespace System.Threading
{
    /// <summary>Provides a simple, reference type wrapper for SpinLock.</summary>
    public class SpinLockClass
    {
        private SpinLock _spinLock; // NOTE: must *not* be readonly due to SpinLock being a mutable struct

        /// <summary>Initializes an instance of the SpinLockClass class.</summary>
        public SpinLockClass() : this(true)
        {
        }

        /// <summary>Initializes an instance of the SpinLockClass class.</summary>
        /// <param name="enableThreadOwnerTracking">
        /// Controls whether the SpinLockClass should track
        /// thread-ownership fo the lock.
        /// </param>
        public SpinLockClass(bool enableThreadOwnerTracking)
        {
            _spinLock = new SpinLock(enableThreadOwnerTracking);
        }

        /// <summary>Runs the specified delegate under the lock.</summary>
        /// <param name="runUnderLock">The delegate to be executed while holding the lock.</param>
        public void Execute(Action runUnderLock)
        {
            var lockTaken = false;
            try
            {
                Enter(ref lockTaken);
                runUnderLock();
            }
            finally
            {
                if (lockTaken)
                {
                    Exit();
                }
            }
        }

        /// <summary>
        /// Runs the specified delegate under the lock.
        /// </summary>
        /// <param name="runUnderLock">The delegate to be executed while holding the lock.</param>
        public async Task ExecuteAsync(Func<Task> runUnderLock)
        {
            var lockTaken = false;
            try
            {
                Enter(ref lockTaken);
                await runUnderLock().ConfigureAwait(true);
            }
            finally
            {
                if (lockTaken) Exit();
            }
        }

        /// <summary>
        /// Runs the specified delegate under the lock.
        /// </summary>
        /// <param name="runUnderLock">The delegate to be executed while holding the lock.</param>
        /// <returns>
        /// The value returned by the specified delegate.
        /// </returns>
        public async Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> runUnderLock)
        {
            var lockTaken = false;
            try
            {
                Enter(ref lockTaken);
                return await runUnderLock().ConfigureAwait(true);
            }
            finally
            {
                if (lockTaken)
                {
                    Exit();
                }
            }
        }

        private void Enter(ref bool lockTaken)
        {
            Console.WriteLine($"Enter: ThreadId:{Thread.CurrentThread.ManagedThreadId}, Context:{SynchronizationContext.Current != null}");
            _spinLock.TryEnter(ref lockTaken);
        }

        private void Exit(bool useMemoryBarrier = true)
        {
            Console.WriteLine($"Exit: ThreadId:{Thread.CurrentThread.ManagedThreadId}, Context:{SynchronizationContext.Current != null}");
            _spinLock.Exit(useMemoryBarrier);
        }
    }
}
