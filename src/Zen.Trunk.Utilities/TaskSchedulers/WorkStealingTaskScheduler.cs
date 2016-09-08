//--------------------------------------------------------------------------
// 
//  Copyright (c) Microsoft Corporation.  All rights reserved. 
// 
//  File: WorkStealingTaskScheduler.cs
//
//--------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Zen.Trunk.Extensions;

namespace Zen.Trunk.TaskSchedulers
{
    /// <summary>Provides a work-stealing scheduler.</summary>
    public class WorkStealingTaskScheduler : TaskScheduler, IDisposable
    {
        private readonly int _concurrencyLevel;
        private readonly Queue<Task> _queue = new Queue<Task>();
        private WorkStealingQueue<Task>[] _wsQueues = new WorkStealingQueue<Task>[Environment.ProcessorCount];
        private readonly Lazy<Thread[]> _threads;
        private int _threadsWaiting;
        private bool _shutdown;
        [ThreadStatic]
        private static WorkStealingQueue<Task> _wsq;

        /// <summary>Initializes a new instance of the WorkStealingTaskScheduler class.</summary>
        /// <remarks>This constructors defaults to using twice as many threads as there are processors.</remarks>
        public WorkStealingTaskScheduler() : this(Environment.ProcessorCount*2)
        {
        }

        /// <summary>Initializes a new instance of the WorkStealingTaskScheduler class.</summary>
        /// <param name="concurrencyLevel">The number of threads to use in the scheduler.</param>
        public WorkStealingTaskScheduler(int concurrencyLevel)
        {
            // Store the concurrency level
            if (concurrencyLevel <= 0) throw new ArgumentOutOfRangeException(nameof(concurrencyLevel));
            _concurrencyLevel = concurrencyLevel;

            // Set up threads
            _threads = new Lazy<Thread[]>(() =>
            {
                var threads = new Thread[_concurrencyLevel];
                for (var i = 0; i < threads.Length; i++)
                {
                    threads[i] = new Thread(DispatchLoop) { IsBackground = true };
                    threads[i].Start();
                }
                return threads;
            });
        }

        /// <summary>Queues a task to the scheduler.</summary>
        /// <param name="task">The task to be scheduled.</param>
        protected override void QueueTask(Task task)
        {
            // Make sure the pool is started, e.g. that all threads have been created.
            _threads.Force();

            // If the task is marked as long-running, give it its own dedicated thread
            // rather than queueing it.
            if ((task.CreationOptions & TaskCreationOptions.LongRunning) != 0)
            {
                new Thread(state => TryExecuteTask((Task)state)) { IsBackground = true }.Start(task);
            }
            else
            {
                // Otherwise, insert the work item into a queue, possibly waking a thread.
                // If there's a local queue and the task does not prefer to be in the global queue,
                // add it to the local queue.
                var wsq = _wsq;
                if (wsq != null && ((task.CreationOptions & TaskCreationOptions.PreferFairness) == 0))
                {
                    // Add to the local queue and notify any waiting threads that work is available.
                    // Races may occur which result in missed event notifications, but they're benign in that
                    // this thread will eventually pick up the work item anyway, as will other threads when another
                    // work item notification is received.
                    wsq.LocalPush(task);
                    if (_threadsWaiting > 0) // OK to read lock-free.
                    {
                        lock (_queue) { Monitor.Pulse(_queue); }
                    }
                }
                // Otherwise, add the work item to the global queue
                else
                {
                    lock (_queue)
                    {
                        _queue.Enqueue(task);
                        if (_threadsWaiting > 0) Monitor.Pulse(_queue);
                    }
                }
            }
        }

        /// <summary>Executes a task on the current thread.</summary>
        /// <param name="task">The task to be executed.</param>
        /// <param name="taskWasPreviouslyQueued">Ignored.</param>
        /// <returns>Whether the task could be executed.</returns>
        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            return TryExecuteTask(task);

            // // Optional replacement: Instead of always trying to execute the task (which could
            // // benignly leave a task in the queue that's already been executed), we
            // // can search the current work-stealing queue and remove the task,
            // // executing it inline only if it's found.
            // WorkStealingQueue<Task> wsq = _wsq;
            // return wsq != null && wsq.TryFindAndPop(task) && TryExecuteTask(task);
        }

        /// <summary>Gets the maximum concurrency level supported by this scheduler.</summary>
        public override int MaximumConcurrencyLevel => _concurrencyLevel;

        /// <summary>Gets all of the tasks currently scheduled to this scheduler.</summary>
        /// <returns>An enumerable containing all of the scheduled tasks.</returns>
        protected override IEnumerable<Task> GetScheduledTasks()
        {
            // Keep track of all of the tasks we find
            var tasks = new List<Task>();

            // Get all of the global tasks.  We use TryEnter so as not to hang
            // a debugger if the lock is held by a frozen thread.
            var lockTaken = false;
            try
            {
                Monitor.TryEnter(_queue, ref lockTaken);
                if (lockTaken) tasks.AddRange(_queue.ToArray());
                else throw new NotSupportedException();
            }
            finally
            {
                if (lockTaken) Monitor.Exit(_queue);
            }

            // Now get all of the tasks from the work-stealing queues
            var queues = _wsQueues;
            for (var i = 0; i < queues.Length; i++)
            {
                var wsq = queues[i];
                if (wsq != null) tasks.AddRange(wsq.ToArray());
            }

            // Return to the debugger all of the collected task instances
            return tasks;
        }

        /// <summary>Adds a work-stealing queue to the set of queues.</summary>
        /// <param name="wsq">The queue to be added.</param>
        private void AddWsq(WorkStealingQueue<Task> wsq)
        {
            lock (_wsQueues)
            {
                // Find the next open slot in the array. If we find one,
                // store the queue and we're done.
                int i;
                for (i = 0; i < _wsQueues.Length; i++)
                {
                    if (_wsQueues[i] == null)
                    {
                        _wsQueues[i] = wsq;
                        return;
                    }
                }

                // We couldn't find an open slot, so double the length 
                // of the array by creating a new one, copying over,
                // and storing the new one. Here, i == _wsQueues.Length.
                var queues = new WorkStealingQueue<Task>[i * 2];
                Array.Copy(_wsQueues, queues, i);
                queues[i] = wsq;
                _wsQueues = queues;
            }
        }

        /// <summary>Remove a work-stealing queue from the set of queues.</summary>
        /// <param name="wsq">The work-stealing queue to remove.</param>
        private void RemoveWsq(WorkStealingQueue<Task> wsq)
        {
            lock (_wsQueues)
            {
                // Find the queue, and if/when we find it, null out its array slot
                for (var i = 0; i < _wsQueues.Length; i++)
                {
                    if (_wsQueues[i] == wsq)
                    {
                        _wsQueues[i] = null;
                    }
                }
            }
        }

        /// <summary>
        /// The dispatch loop run by each thread in the scheduler.
        /// </summary>
        private void DispatchLoop()
        {
            // Create a new queue for this thread, store it in TLS for later retrieval,
            // and add it to the set of queues for this scheduler.
            var wsq = new WorkStealingQueue<Task>();
            _wsq = wsq;
            AddWsq(wsq);

            try
            {
                // Until there's no more work to do...
                while (true)
                {
                    Task wi;

                    // Search order: (1) local WSQ, (2) global Q, (3) steals from other queues.
                    if (!wsq.LocalPop(out wi))
                    {
                        // We weren't able to get a task from the local WSQ
                        var searchedForSteals = false;
                        while (true)
                        {
                            lock (_queue)
                            {
                                // If shutdown was requested, exit the thread.
                                if (_shutdown)
                                    return;

                                // (2) try the global queue.
                                if (_queue.Count != 0)
                                {
                                    // We found a work item! Grab it ...
                                    wi = _queue.Dequeue();
                                    break;
                                }
                                else if (searchedForSteals)
                                {
                                    // Note that we're not waiting for work, and then wait
                                    _threadsWaiting++;
                                    try { Monitor.Wait(_queue); }
                                    finally { _threadsWaiting--; }

                                    // If we were signaled due to shutdown, exit the thread.
                                    if (_shutdown)
                                        return;

                                    searchedForSteals = false;
                                    continue;
                                }
                            }

                            // (3) try to steal.
                            var wsQueues = _wsQueues;
                            int i;
                            for (i = 0; i < wsQueues.Length; i++)
                            {
                                var q = wsQueues[i];
                                if (q != null && q != wsq && q.TrySteal(out wi)) break;
                            }

                            if (i != wsQueues.Length) break;

                            searchedForSteals = true;
                        }
                    }

                    // ...and Invoke it.
                    TryExecuteTask(wi);
                }
            }
            finally
            {
                RemoveWsq(wsq);
            }
        }

        /// <summary>Signal the scheduler to shutdown and wait for all threads to finish.</summary>
        public void Dispose()
        {
            _shutdown = true;
            if (_queue != null && _threads.IsValueCreated)
            {
                var threads = _threads.Value;
                lock (_queue) Monitor.PulseAll(_queue);
                for (var i = 0; i < threads.Length; i++) threads[i].Join();
            }
        }
    }

    /// <summary>A work-stealing queue.</summary>
    /// <typeparam name="T">Specifies the type of data stored in the queue.</typeparam>
    internal class WorkStealingQueue<T> where T : class
    {
        // ReSharper disable once InconsistentNaming
        private const int INITIAL_SIZE = 32;
        private T[] _array = new T[INITIAL_SIZE];
        private int _mask = INITIAL_SIZE - 1;
        private volatile int _headIndex;
        private volatile int _tailIndex;

        private readonly object _foreignLock = new object();

        internal void LocalPush(T obj)
        {
            var tail = _tailIndex;

            // When there are at least 2 elements' worth of space, we can take the fast path.
            if (tail < _headIndex + _mask)
            {
                _array[tail & _mask] = obj;
                _tailIndex = tail + 1;
            }
            else
            {
                // We need to contend with foreign pops, so we lock.
                lock (_foreignLock)
                {
                    var head = _headIndex;
                    var count = _tailIndex - _headIndex;

                    // If there is still space (one left), just add the element.
                    if (count >= _mask)
                    {
                        // We're full; expand the queue by doubling its size.
                        var newArray = new T[_array.Length << 1];
                        for (var i = 0; i < _array.Length; i++)
                            newArray[i] = _array[(i + head) & _mask];

                        // Reset the field values, incl. the mask.
                        _array = newArray;
                        _headIndex = 0;
                        _tailIndex = tail = count;
                        _mask = (_mask << 1) | 1;
                    }

                    _array[tail & _mask] = obj;
                    _tailIndex = tail + 1;
                }
            }
        }

        internal bool LocalPop(out T obj)
        {
            while (true)
            {
                // Decrement the tail using a fence to ensure subsequent read doesn't come before.
                var tail = _tailIndex;
                if (_headIndex >= tail)
                {
                    obj = null;
                    return false;
                }

                tail -= 1;
#pragma warning disable 0420
                Interlocked.Exchange(ref _tailIndex, tail);
#pragma warning restore 0420

                // If there is no interaction with a take, we can head down the fast path.
                if (_headIndex <= tail)
                {
                    var idx = tail & _mask;
                    obj = _array[idx];

                    // Check for nulls in the array.
                    if (obj == null) continue;

                    _array[idx] = null;
                    return true;
                }
                else
                {
                    // Interaction with takes: 0 or 1 elements left.
                    lock (_foreignLock)
                    {
                        if (_headIndex <= tail)
                        {
                            // Element still available. Take it.
                            var idx = tail & _mask;
                            obj = _array[idx];

                            // Check for nulls in the array.
                            if (obj == null) continue;

                            _array[idx] = null;
                            return true;
                        }
                        else
                        {
                            // We lost the race, element was stolen, restore the tail.
                            _tailIndex = tail + 1;
                            obj = null;
                            return false;
                        }
                    }
                }
            }
        }

        internal bool TrySteal(out T obj)
        {
            obj = null;

            while (true)
            {
                if (_headIndex >= _tailIndex)
                    return false;

                lock (_foreignLock)
                {
                    // Increment head, and ensure read of tail doesn't move before it (fence).
                    var head = _headIndex;
#pragma warning disable 0420
                    Interlocked.Exchange(ref _headIndex, head + 1);
#pragma warning restore 0420

                    if (head < _tailIndex)
                    {
                        var idx = head & _mask;
                        obj = _array[idx];

                        // Check for nulls in the array.
                        if (obj == null) continue;

                        _array[idx] = null;
                        return true;
                    }
                    else
                    {
                        // Failed, restore head.
                        _headIndex = head;
                        obj = null;
                    }
                }

                return false;
            }
        }

        internal bool TryFindAndPop(T obj)
        {
            // We do an O(N) search for the work item. The theory of work stealing and our
            // inlining logic is that most waits will happen on recently queued work.  And
            // since recently queued work will be close to the tail end (which is where we
            // begin our search), we will likely find it quickly.  In the worst case, we
            // will traverse the whole local queue; this is typically not going to be a
            // problem (although degenerate cases are clearly an issue) because local work
            // queues tend to be somewhat shallow in length, and because if we fail to find
            // the work item, we are about to block anyway (which is very expensive).

            for (var i = _tailIndex - 1; i >= _headIndex; i--)
            {
                if (_array[i & _mask] == obj)
                {
                    // If we found the element, block out steals to avoid interference.
                    lock (_foreignLock)
                    {
                        // If we lost the race, bail.
                        if (_array[i & _mask] == null)
                        {
                            return false;
                        }

                        // Otherwise, null out the element.
                        _array[i & _mask] = null;

                        // And then check to see if we can fix up the indexes (if we're at
                        // the edge).  If we can't, we just leave nulls in the array and they'll
                        // get filtered out eventually (but may lead to superflous resizing).
                        if (i == _tailIndex)
                            _tailIndex -= 1;
                        else if (i == _headIndex)
                            _headIndex += 1;

                        return true;
                    }
                }
            }

            return false;
        }

        internal T[] ToArray()
        {
            var list = new List<T>();
            for (var i = _tailIndex - 1; i >= _headIndex; i--)
            {
                var obj = _array[i & _mask];
                if (obj != null) list.Add(obj);
            }
            return list.ToArray();
        }
    }
}