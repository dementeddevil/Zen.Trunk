using System.Diagnostics;

namespace System.Threading.Tasks.Dataflow
{
    /// <summary>
    /// TaskRequestActionBlock is a helper ActionBlock object that eases writing
    /// handlers for TaskRequest derived messages.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    [DebuggerStepThrough]
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
                request => ExecuteActionAndProcessResult(action, request),
                dataflowBlockOptions);
        }

        public TaskRequestActionBlock(Func<TRequest, Task<TResult>> action, ExecutionDataflowBlockOptions dataflowBlockOptions)
        {
            _innerBlock = new ActionBlock<TRequest>(
                request => ExecuteActionAndProcessResultAsync(action, request),
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
            return _innerBlock.OfferMessage(message, value, source, consumeToAccept);
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
                var result = await action(request).ConfigureAwait(false);
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
                request => ExecuteActionAndProcessResult(action, request),
                dataflowBlockOptions);
        }

        public TaskRequestActionBlock(Func<TRequest, Task<TResult>> action, ExecutionDataflowBlockOptions dataflowBlockOptions)
        {
            _innerBlock = new ActionBlock<TRequest>(
                request => ExecuteActionAndProcessResultAsync(action, request),
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
            return _innerBlock.OfferMessage(message, value, source, consumeToAccept);
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
                var result = await action(request).ConfigureAwait(false);
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
}