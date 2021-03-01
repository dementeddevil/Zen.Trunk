using System.Diagnostics;
using System.Threading.Tasks;

namespace Zen.Trunk.Utils
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <seealso cref="System.Threading.Tasks.TaskCompletionSource{TResult}" />
    /// <seealso cref="Zen.Trunk.Utils.ITaskRequest" />
    [DebuggerStepThrough]
    public class TaskRequest<TResult> : TaskCompletionSource<TResult>, ITaskRequest
	{
        /// <summary>
        /// Initializes a new instance of the <see cref="TaskRequest{TResult}"/> class.
        /// </summary>
        /// <param name="creationOptions">The creation options.</param>
        public TaskRequest(TaskCreationOptions creationOptions = TaskCreationOptions.RunContinuationsAsynchronously)
			: base(creationOptions)
		{
		}

		Task ITaskRequest.Task => Task;
	}

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <seealso cref="System.Threading.Tasks.TaskCompletionSource{TResult}" />
    /// <seealso cref="Zen.Trunk.Utils.ITaskRequest" />
    [DebuggerStepThrough]
    public class TaskRequest<TMessage, TResult> : TaskRequest<TResult>
	{
        /// <summary>
        /// Initializes a new instance of the <see cref="TaskRequest{TMessage, TResult}"/> class.
        /// </summary>
        /// <param name="creationOptions">The creation options.</param>
        public TaskRequest(TaskCreationOptions creationOptions = TaskCreationOptions.RunContinuationsAsynchronously)
			: base(creationOptions)
		{
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskRequest{TMessage, TResult}"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="creationOptions">The creation options.</param>
        public TaskRequest(TMessage message, TaskCreationOptions creationOptions = TaskCreationOptions.RunContinuationsAsynchronously)
			: base(creationOptions)
		{
			Message = message;
		}

        /// <summary>
        /// Gets the message.
        /// </summary>
        /// <value>
        /// The message.
        /// </value>
        public TMessage Message { get; }
	}
}
