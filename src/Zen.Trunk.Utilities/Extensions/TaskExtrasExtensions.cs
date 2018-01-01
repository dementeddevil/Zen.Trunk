﻿//--------------------------------------------------------------------------
// 
//  Copyright (c) Microsoft Corporation.  All rights reserved. 
// 
//  File: TaskExtensions.cs
//
//--------------------------------------------------------------------------

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

//using System.Windows.Threading;

namespace Zen.Trunk.Extensions
{
    /// <summary>Extensions methods for Task.</summary>
    public static class TaskExtrasExtensions
    {
        #region ContinueWith accepting TaskFactory
        /// <summary>Creates a continuation task using the specified TaskFactory.</summary>
        /// <param name="task">The antecedent Task.</param>
        /// <param name="continuationAction">The continuation action.</param>
        /// <param name="factory">The TaskFactory.</param>
        /// <returns>A continuation task.</returns>
        public static Task ContinueWith(
            this Task task, Action<Task> continuationAction, TaskFactory factory)
        {
            return task.ContinueWith(continuationAction, factory.CancellationToken, factory.ContinuationOptions, factory.Scheduler);
        }

        /// <summary>Creates a continuation task using the specified TaskFactory.</summary>
        /// <param name="task">The antecedent Task.</param>
        /// <param name="continuationFunction">The continuation function.</param>
        /// <param name="factory">The TaskFactory.</param>
        /// <returns>A continuation task.</returns>
        public static Task<TResult> ContinueWith<TResult>(
            this Task task, Func<Task, TResult> continuationFunction, TaskFactory factory)
        {
            return task.ContinueWith(continuationFunction, factory.CancellationToken, factory.ContinuationOptions, factory.Scheduler);
        }
        #endregion

        #region ContinueWith accepting TaskFactory<TResult>
        /// <summary>Creates a continuation task using the specified TaskFactory.</summary>
        /// <param name="task">The antecedent Task.</param>
        /// <param name="continuationAction">The continuation action.</param>
        /// <param name="factory">The TaskFactory.</param>
        /// <returns>A continuation task.</returns>
        public static Task ContinueWith<TResult>(
            this Task<TResult> task, Action<Task<TResult>> continuationAction, TaskFactory<TResult> factory)
        {
            return task.ContinueWith(continuationAction, factory.CancellationToken, factory.ContinuationOptions, factory.Scheduler);
        }

        /// <summary>Creates a continuation task using the specified TaskFactory.</summary>
        /// <param name="task">The antecedent Task.</param>
        /// <param name="continuationFunction">The continuation function.</param>
        /// <param name="factory">The TaskFactory.</param>
        /// <returns>A continuation task.</returns>
        public static Task<TNewResult> ContinueWith<TResult, TNewResult>(
            this Task<TResult> task, Func<Task<TResult>, TNewResult> continuationFunction, TaskFactory<TResult> factory)
        {
            return task.ContinueWith(continuationFunction, factory.CancellationToken, factory.ContinuationOptions, factory.Scheduler);
        }
        #endregion

        #region ToAsync(AsyncCallback, object)
        /// <summary>
        /// Creates a Task that represents the completion of another Task, and 
        /// that schedules an AsyncCallback to run upon completion.
        /// </summary>
        /// <param name="task">The antecedent Task.</param>
        /// <param name="callback">The AsyncCallback to run.</param>
        /// <param name="state">The object state to use with the AsyncCallback.</param>
        /// <returns>The new task.</returns>
        public static Task ToAsync(this Task task, AsyncCallback callback, object state)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            var tcs = new TaskCompletionSource<object>(state);
            task.ContinueWith(_ =>
            {
                tcs.SetFromTask(task);
                callback?.Invoke(tcs.Task);
            });
            return tcs.Task;
        }

        /// <summary>
        /// Creates a Task that represents the completion of another Task, and 
        /// that schedules an AsyncCallback to run upon completion.
        /// </summary>
        /// <param name="task">The antecedent Task.</param>
        /// <param name="callback">The AsyncCallback to run.</param>
        /// <param name="state">The object state to use with the AsyncCallback.</param>
        /// <returns>The new task.</returns>
        public static Task<TResult> ToAsync<TResult>(this Task<TResult> task, AsyncCallback callback, object state)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            var tcs = new TaskCompletionSource<TResult>(state);
            task.ContinueWith(_ =>
            {
                tcs.SetFromTask(task);
                callback?.Invoke(tcs.Task);
            });
            return tcs.Task;
        }
        #endregion

        #region Exception Handling
        /// <summary>Suppresses default exception handling of a Task that would otherwise reraise the exception on the finalizer thread.</summary>
        /// <param name="task">The Task to be monitored.</param>
        /// <returns>The original Task.</returns>
        public static Task IgnoreExceptions(this Task task)
        {
            // ReSharper disable once UnusedVariable
            task.ContinueWith(t => { var ignored = t.Exception; },
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default);
            return task;
        }

        /// <summary>Suppresses default exception handling of a Task that would otherwise reraise the exception on the finalizer thread.</summary>
        /// <param name="task">The Task to be monitored.</param>
        /// <returns>The original Task.</returns>
        public static Task<T> IgnoreExceptions<T>(this Task<T> task)
        {
            return (Task<T>)((Task)task).IgnoreExceptions();
        }

        /// <summary>Fails immediately when an exception is encountered.</summary>
        /// <param name="task">The Task to be monitored.</param>
        /// <returns>The original Task.</returns>
        public static Task FailFastOnException(this Task task)
        {
            task.ContinueWith(t => Environment.FailFast("A task faulted.", t.Exception),
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default);
            return task;
        }

        /// <summary>Fails immediately when an exception is encountered.</summary>
        /// <param name="task">The Task to be monitored.</param>
        /// <returns>The original Task.</returns>
        public static Task<T> FailFastOnException<T>(this Task<T> task)
        {
            return (Task<T>)((Task)task).FailFastOnException();
        }

        /// <summary>Propagates any exceptions that occurred on the specified task.</summary>
        /// <param name="task">The Task whose exceptions are to be propagated.</param>
        public static void PropagateExceptions(this Task task)
        {
            if (!task.IsCompleted)
            {
                throw new InvalidOperationException("The task has not completed.");
            }

            if (task.IsFaulted)
            {
                task.Wait();
            }
        }

        /// <summary>Propagates any exceptions that occurred on the specified tasks.</summary>
        /// <param name="tasks">The Tasks whose exceptions are to be propagated.</param>
        public static void PropagateExceptions(this Task[] tasks)
        {
            if (tasks == null)
            {
                throw new ArgumentNullException(nameof(tasks));
            }
            if (tasks.Any(t => t == null))
            {
                throw new ArgumentException("tasks");
            }
            if (tasks.Any(t => !t.IsCompleted))
            {
                throw new InvalidOperationException("A task has not completed.");
            }

            Task.WaitAll(tasks);
        }
        #endregion

        #region Observables
        /// <summary>
        /// Creates an IObservable that represents the completion of a Task.
        /// </summary>
        /// <typeparam name="TResult">Specifies the type of data returned by the Task.</typeparam>
        /// <param name="task">The Task to be represented as an IObservable.</param>
        /// <returns>An IObservable that represents the completion of the Task.</returns>
        public static IObservable<TResult> ToObservable<TResult>(this Task<TResult> task)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            return new TaskObservable<TResult> { Task = task };
        }

        /// <summary>An implementation of IObservable that wraps a Task.</summary>
        /// <typeparam name="TResult">The type of data returned by the task.</typeparam>
        private class TaskObservable<TResult> : IObservable<TResult>
        {
            internal Task<TResult> Task;

            public IDisposable Subscribe(IObserver<TResult> observer)
            {
                // Validate arguments
                if (observer == null)
                {
                    throw new ArgumentNullException(nameof(observer));
                }

                // Support cancelling the continuation if the observer is unsubscribed
                var cts = new CancellationTokenSource();

                // Create a continuation to pass data along to the observer
                Task.ContinueWith(
                    t =>
                    {
                        switch (t.Status)
                        {
                            case TaskStatus.RanToCompletion:
                                observer.OnNext(Task.Result);
                                observer.OnCompleted();
                                break;

                            case TaskStatus.Faulted:
                                // ReSharper disable once AssignNullToNotNullAttribute
                                observer.OnError(Task.Exception);
                                break;

                            case TaskStatus.Canceled:
                                observer.OnError(new TaskCanceledException(t));
                                break;
                        }
                    },
                    cts.Token);

                // Support unsubscribe simply by canceling the continuation if it hasn't yet run
                return new CancelOnDispose { Source = cts };
            }
        }

        /// <summary>Translate a call to IDisposable.Dispose to a CancellationTokenSource.Cancel.</summary>
        private class CancelOnDispose : IDisposable
        {
            internal CancellationTokenSource Source;
            void IDisposable.Dispose() { Source.Cancel(); }
        }
        #endregion

        #region Timeouts
        /// <summary>
        /// Creates a new Task that mirrors the supplied task but that will be
        /// canceled after the specified timeout.
        /// </summary>
        /// <param name="task">The task.</param>
        /// <param name="timeout">The timeout.</param>
        /// <returns>The new Task that may time out.</returns>
        public static Task WithTimeout(this Task task, TimeSpan timeout)
        {
            var result = new TaskCompletionSource<object>(task.AsyncState);
            var timer = new Timer(
                state => ((TaskCompletionSource<object>)state).TrySetCanceled(),
                result,
                timeout,
                TimeSpan.FromMilliseconds(-1));
            task.ContinueWith(
                t =>
                {
                    timer.Dispose();
                    result.TrySetFromTask(t);
                },
                TaskContinuationOptions.ExecuteSynchronously);
            return result.Task;
        }

        /// <summary>
        /// Creates a new Task that mirrors the supplied task but that will be
        /// canceled after the specified timeout.
        /// </summary>
        /// <typeparam name="TResult">Specifies the type of data contained in the task.</typeparam>
        /// <param name="task">The task.</param>
        /// <param name="timeout">The timeout.</param>
        /// <returns>The new Task that may time out.</returns>
        public static Task<TResult> WithTimeout<TResult>(this Task<TResult> task, TimeSpan timeout)
        {
            var result = new TaskCompletionSource<TResult>(task.AsyncState);
            var timer = new Timer(
                state => ((TaskCompletionSource<TResult>)state).TrySetCanceled(),
                result,
                timeout,
                TimeSpan.FromMilliseconds(-1));
            task.ContinueWith(
                t =>
                {
                    timer.Dispose();
                    result.TrySetFromTask(t);
                },
                TaskContinuationOptions.ExecuteSynchronously);
            return result.Task;
        }
        #endregion

        #region Children
        /// <summary>
        /// Ensures that a parent task can't transition into a completed state
        /// until the specified task has also completed, even if it's not
        /// already a child task.
        /// </summary>
        /// <param name="task">The task to attach to the current task as a child.</param>
        public static void AttachToParent(this Task task)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            task.ContinueWith(
                t => t.Wait(),
                CancellationToken.None,
                TaskContinuationOptions.AttachedToParent |
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }
        #endregion

        #region Waiting
        /*/// <summary>Waits for the task to complete execution, pumping in the meantime.</summary>
        /// <param name="task">The task for which to wait.</param>
        /// <remarks>This method is intended for usage with Windows Presentation Foundation.</remarks>
        public static void WaitWithPumping(this Task task)
        {
            if (task == null) throw new ArgumentNullException("task");
            var nestedFrame = new DispatcherFrame();
            task.ContinueWith(_ => nestedFrame.Continue = false);
            Dispatcher.PushFrame(nestedFrame);
            task.Wait();
        }*/

        /// <summary>
        /// Waits for the task to complete execution, returning the task's
        /// final status.
        /// </summary>
        /// <param name="task">The task for which to wait.</param>
        /// <returns>The completion status of the task.</returns>
        /// <remarks>
        /// Unlike Wait, this method will not throw an exception if the task
        /// ends in the Faulted or Canceled state.
        /// </remarks>
        public static TaskStatus WaitForCompletionStatus(this Task task)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            ((IAsyncResult)task).AsyncWaitHandle.WaitOne();
            return task.Status;
        }
        #endregion
    }
}