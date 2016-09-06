using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Zen.Trunk.Utils
{
    /// <summary>
    /// 
    /// </summary>
    [DebuggerStepThrough]
    public static class TargetBlockExtensions
    {
        /// <summary>
        /// Posts the and wait asynchronous.
        /// </summary>
        /// <param name="port">The port.</param>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Request port did not accept message.</exception>
        public static Task PostAndWaitAsync(this ITargetBlockSet port, ITaskRequest request)
        {
            if (!port.Post(request))
            {
                throw new InvalidOperationException("Request port did not accept message.");
            }

            return request.Task;
        }

        /// <summary>
        /// Posts the and wait asynchronous.
        /// </summary>
        /// <typeparam name="TTaskResult">The type of the task result.</typeparam>
        /// <param name="port">The port.</param>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Request port did not accept message.</exception>
        public static Task<TTaskResult> PostAndWaitAsync<TTaskResult>(this ITargetBlockSet port, ITaskRequest request)
        {
            if (!port.Post(request))
            {
                throw new InvalidOperationException("Request port did not accept message.");
            }

            return (Task<TTaskResult>)request.Task;
        }

        /// <summary>
        /// Posts the and collect.
        /// </summary>
        /// <param name="port">The port.</param>
        /// <param name="request">The request.</param>
        /// <param name="subTasks">The sub tasks.</param>
        /// <exception cref="InvalidOperationException">Request port did not accept message.</exception>
        public static void PostAndCollect(this ITargetBlockSet port, ITaskRequest request, IList<Task> subTasks)
        {
            if (!port.Post(request))
            {
                throw new InvalidOperationException("Request port did not accept message.");
            }

            subTasks.Add(request.Task);
        }

        /// <summary>
        /// Posts the and collect.
        /// </summary>
        /// <typeparam name="TTaskResult">The type of the task result.</typeparam>
        /// <param name="port">The port.</param>
        /// <param name="request">The request.</param>
        /// <param name="subTasks">The sub tasks.</param>
        /// <exception cref="InvalidOperationException">Request port did not accept message.</exception>
        public static void PostAndCollect<TTaskResult>(this ITargetBlockSet port, ITaskRequest request, IList<Task<TTaskResult>> subTasks)
        {
            if (!port.Post(request))
            {
                throw new InvalidOperationException("Request port did not accept message.");
            }

            subTasks.Add((Task<TTaskResult>)request.Task);
        }
    }
}