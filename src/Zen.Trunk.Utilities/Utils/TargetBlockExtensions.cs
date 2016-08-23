using System.Collections.Generic;

namespace System.Threading.Tasks.Dataflow
{
    public static class TargetBlockExtensions
    {
        public static Task PostAndWaitAsync(this ITargetBlockSet port, ITaskRequest request)
        {
            if (!port.Post(request))
            {
                throw new InvalidOperationException("Request port did not accept message.");
            }

            return request.Task;
        }

        public static Task<TTaskResult> PostAndWaitAsync<TTaskResult>(this ITargetBlockSet port, ITaskRequest request)
        {
            if (!port.Post(request))
            {
                throw new InvalidOperationException("Request port did not accept message.");
            }

            return (Task<TTaskResult>)request.Task;
        }

        public static void PostAndCollect(this ITargetBlockSet port, ITaskRequest request, IList<Task> subTasks)
        {
            if (!port.Post(request))
            {
                throw new InvalidOperationException("Request port did not accept message.");
            }

            subTasks.Add(request.Task);
        }

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