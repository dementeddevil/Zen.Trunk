using System;
using System.Threading.Tasks;

namespace System.Threading.Tasks.Dataflow
{
    //[CLSCompliant(false)]
	public class TaskRequest<TResult> : TaskCompletionSource<TResult>, ITaskRequest
	{
		public TaskRequest()
		{
		}

		public TaskRequest(TaskCreationOptions createOptions)
			: base(createOptions)
		{
		}

		Task ITaskRequest.Task => Task;
	}

	//[CLSCompliant(false)]
	public class TaskRequest<TMessage, TResult> : TaskRequest<TResult>
	{
		public TaskRequest()
		{
		}

		public TaskRequest(TaskCreationOptions createOptions)
			: base(createOptions)
		{
		}

		public TaskRequest(TMessage message)
		{
			Message = message;
		}

		public TaskRequest(TMessage message, TaskCreationOptions createOptions)
			: base(createOptions)
		{
			Message = message;
		}

		public TMessage Message
		{
			get;
			set;
		}
	}
}
