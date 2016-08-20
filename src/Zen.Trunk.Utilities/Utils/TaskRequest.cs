namespace System.Threading.Tasks.Dataflow
{
	using System;
	using System.Collections.Generic;
	using System.Threading.Tasks;

	public interface ITaskRequest
	{
		Task Task
		{
			get;
		}
	}

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

	/// <summary>
	/// TaskRequestActionBlock is a helper ActionBlock object that eases writing
	/// handlers for TaskRequest derived messages.
	/// </summary>
	/// <typeparam name="TRequest">The type of the request.</typeparam>
	/// <typeparam name="TResult">The type of the result.</typeparam>
	public class TaskRequestActionBlock<TRequest, TResult> : ITargetBlock<TRequest>
		where TRequest : TaskRequest<TResult>
	{
		private readonly ITargetBlock<TRequest> _innerBlock;

		public TaskRequestActionBlock(Func<TRequest, TResult> action)
			: this(action, new ExecutionDataflowBlockOptions())
		{
		}

		public TaskRequestActionBlock(Func<TRequest, Task<TResult>> action)
			: this(action, new ExecutionDataflowBlockOptions())
		{
		}

		public TaskRequestActionBlock(Func<TRequest, TResult> action, ExecutionDataflowBlockOptions dataflowBlockOptions)
		{
			_innerBlock = new ActionBlock<TRequest>(
				(request) => ExecuteActionAndProcessResult(action, request),
				dataflowBlockOptions);
		}

		public TaskRequestActionBlock(Func<TRequest, Task<TResult>> action, ExecutionDataflowBlockOptions dataflowBlockOptions)
		{
			_innerBlock = new ActionBlock<TRequest>(
				(request) => ExecuteActionAndProcessResultAsync(action, request),
				dataflowBlockOptions);
		}

		public virtual Task Completion => _innerBlock.Completion;

	    public virtual void Fault(Exception exception)
		{
			_innerBlock.Fault(exception);
		}

		public virtual void Complete()
		{
			_innerBlock.Complete();
		}

		public virtual DataflowMessageStatus OfferMessage(DataflowMessageHeader message, TRequest value, ISourceBlock<TRequest> source, bool consumeToAccept)
		{
			return ((ITargetBlock<TRequest>)_innerBlock).OfferMessage(message, value, source, consumeToAccept);
		}

		public virtual bool Post(TRequest item)
		{
			return _innerBlock.Post(item);
		}

		private void ExecuteActionAndProcessResult(Func<TRequest, TResult> action, TRequest request)
		{
			try
			{
				var result = action(request);
				request.TrySetResult(result);
			}
			catch (OperationCanceledException)
			{
				request.TrySetCanceled();
			}
			catch (Exception exception)
			{
				request.TrySetException(exception);
			}
		}

		private async Task ExecuteActionAndProcessResultAsync(Func<TRequest, Task<TResult>> action, TRequest request)
		{
			try
			{
				var result = await action(request);
				request.TrySetResult(result);
			}
			catch (OperationCanceledException)
			{
				request.TrySetCanceled();
			}
			catch (Exception exception)
			{
				request.TrySetException(exception);
			}
		}
	}

	/// <summary>
	/// TaskRequestActionBlock is a helper ActionBlock object that eases writing
	/// handlers for TaskRequest derived messages.
	/// </summary>
	/// <typeparam name="TRequest">The type of the request.</typeparam>
	/// <typeparam name="TMessage">The type of the message.</typeparam>
	/// <typeparam name="TResult">The type of the result.</typeparam>
	public class TaskRequestActionBlock<TRequest, TMessage, TResult> : ITargetBlock<TRequest>
		where TRequest : TaskRequest<TMessage, TResult>
	{
		private readonly ITargetBlock<TRequest> _innerBlock;

		public TaskRequestActionBlock(Func<TRequest, TResult> action)
			: this(action, new ExecutionDataflowBlockOptions())
		{
		}

		public TaskRequestActionBlock(Func<TRequest, Task<TResult>> action)
			: this(action, new ExecutionDataflowBlockOptions())
		{
		}

		public TaskRequestActionBlock(Func<TRequest, TResult> action, ExecutionDataflowBlockOptions dataflowBlockOptions)
		{
			_innerBlock = new ActionBlock<TRequest>(
				(request) => ExecuteActionAndProcessResult(action, request),
				dataflowBlockOptions);
		}

		public TaskRequestActionBlock(Func<TRequest, Task<TResult>> action, ExecutionDataflowBlockOptions dataflowBlockOptions)
		{
			_innerBlock = new ActionBlock<TRequest>(
				(request) => ExecuteActionAndProcessResultAsync(action, request),
				dataflowBlockOptions);
		}

		public virtual Task Completion => _innerBlock.Completion;

	    public virtual void Fault(Exception exception)
		{
			_innerBlock.Fault(exception);
		}

		public virtual void Complete()
		{
			_innerBlock.Complete();
		}

		public virtual DataflowMessageStatus OfferMessage(DataflowMessageHeader message, TRequest value, ISourceBlock<TRequest> source, bool consumeToAccept)
		{
			return ((ITargetBlock<TRequest>)_innerBlock).OfferMessage(message, value, source, consumeToAccept);
		}

		public virtual bool Post(TRequest item)
		{
			return _innerBlock.Post(item);
		}

		private void ExecuteActionAndProcessResult(Func<TRequest, TResult> action, TRequest request)
		{
			try
			{
				var result = action(request);
				request.TrySetResult(result);
			}
			catch (OperationCanceledException)
			{
				request.TrySetCanceled();
			}
			catch (Exception exception)
			{
				request.TrySetException(exception);
			}
		}

		private async Task ExecuteActionAndProcessResultAsync(Func<TRequest, Task<TResult>> action, TRequest request)
		{
			try
			{
				var result = await action(request);
				request.TrySetResult(result);
			}
			catch (OperationCanceledException)
			{
				request.TrySetCanceled();
			}
			catch (Exception exception)
			{
				request.TrySetException(exception);
			}
		}
	}

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
