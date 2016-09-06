using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Zen.Trunk.Utils
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

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskRequestActionBlock{TRequest, TResult}"/> class.
        /// </summary>
        /// <param name="action">The action.</param>
        public TaskRequestActionBlock(Func<TRequest, TResult> action)
            : this(action, new ExecutionDataflowBlockOptions())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskRequestActionBlock{TRequest, TResult}"/> class.
        /// </summary>
        /// <param name="action">The action.</param>
        public TaskRequestActionBlock(Func<TRequest, Task<TResult>> action)
            : this(action, new ExecutionDataflowBlockOptions())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskRequestActionBlock{TRequest, TResult}"/> class.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="dataflowBlockOptions">The dataflow block options.</param>
        public TaskRequestActionBlock(Func<TRequest, TResult> action, ExecutionDataflowBlockOptions dataflowBlockOptions)
        {
            _innerBlock = new ActionBlock<TRequest>(
                request => ExecuteActionAndProcessResult(action, request),
                dataflowBlockOptions);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskRequestActionBlock{TRequest, TResult}"/> class.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="dataflowBlockOptions">The dataflow block options.</param>
        public TaskRequestActionBlock(Func<TRequest, Task<TResult>> action, ExecutionDataflowBlockOptions dataflowBlockOptions)
        {
            _innerBlock = new ActionBlock<TRequest>(
                request => ExecuteActionAndProcessResultAsync(action, request),
                dataflowBlockOptions);
        }

        /// <summary>
        /// Gets a <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation and completion of the dataflow block.
        /// </summary>
        public virtual Task Completion => _innerBlock.Completion;

        /// <summary>
        /// Causes the <see cref="T:System.Threading.Tasks.Dataflow.IDataflowBlock" /> to complete in a <see cref="F:System.Threading.Tasks.TaskStatus.Faulted" /> state.
        /// </summary>
        /// <param name="exception">The <see cref="T:System.Exception" /> that caused the faulting.</param>
        public virtual void Fault(Exception exception)
        {
            _innerBlock.Fault(exception);
        }

        /// <summary>
        /// Signals to the <see cref="T:System.Threading.Tasks.Dataflow.IDataflowBlock" /> that it should not accept nor produce any more messages nor consume any more postponed messages.
        /// </summary>
        public virtual void Complete()
        {
            _innerBlock.Complete();
        }

        /// <summary>
        /// Offers the message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="value">The value.</param>
        /// <param name="source">The source.</param>
        /// <param name="consumeToAccept">if set to <c>true</c> [consume to accept].</param>
        /// <returns></returns>
        public virtual DataflowMessageStatus OfferMessage(DataflowMessageHeader message, TRequest value, ISourceBlock<TRequest> source, bool consumeToAccept)
        {
            return _innerBlock.OfferMessage(message, value, source, consumeToAccept);
        }

        /// <summary>
        /// Posts the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
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

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskRequestActionBlock{TRequest, TMessage, TResult}"/> class.
        /// </summary>
        /// <param name="action">The action.</param>
        public TaskRequestActionBlock(Func<TRequest, TResult> action)
            : this(action, new ExecutionDataflowBlockOptions())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskRequestActionBlock{TRequest, TMessage, TResult}"/> class.
        /// </summary>
        /// <param name="action">The action.</param>
        public TaskRequestActionBlock(Func<TRequest, Task<TResult>> action)
            : this(action, new ExecutionDataflowBlockOptions())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskRequestActionBlock{TRequest, TMessage, TResult}"/> class.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="dataflowBlockOptions">The dataflow block options.</param>
        public TaskRequestActionBlock(Func<TRequest, TResult> action, ExecutionDataflowBlockOptions dataflowBlockOptions)
        {
            _innerBlock = new ActionBlock<TRequest>(
                request => ExecuteActionAndProcessResult(action, request),
                dataflowBlockOptions);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskRequestActionBlock{TRequest, TMessage, TResult}"/> class.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="dataflowBlockOptions">The dataflow block options.</param>
        public TaskRequestActionBlock(Func<TRequest, Task<TResult>> action, ExecutionDataflowBlockOptions dataflowBlockOptions)
        {
            _innerBlock = new ActionBlock<TRequest>(
                request => ExecuteActionAndProcessResultAsync(action, request),
                dataflowBlockOptions);
        }

        /// <summary>
        /// Gets a <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation and completion of the dataflow block.
        /// </summary>
        public virtual Task Completion => _innerBlock.Completion;

        /// <summary>
        /// Causes the <see cref="T:System.Threading.Tasks.Dataflow.IDataflowBlock" /> to complete in a <see cref="F:System.Threading.Tasks.TaskStatus.Faulted" /> state.
        /// </summary>
        /// <param name="exception">The <see cref="T:System.Exception" /> that caused the faulting.</param>
        public virtual void Fault(Exception exception)
        {
            _innerBlock.Fault(exception);
        }

        /// <summary>
        /// Signals to the <see cref="T:System.Threading.Tasks.Dataflow.IDataflowBlock" /> that it should not accept nor produce any more messages nor consume any more postponed messages.
        /// </summary>
        public virtual void Complete()
        {
            _innerBlock.Complete();
        }

        /// <summary>
        /// Offers the message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="value">The value.</param>
        /// <param name="source">The source.</param>
        /// <param name="consumeToAccept">if set to <c>true</c> [consume to accept].</param>
        /// <returns></returns>
        public virtual DataflowMessageStatus OfferMessage(DataflowMessageHeader message, TRequest value, ISourceBlock<TRequest> source, bool consumeToAccept)
        {
            return _innerBlock.OfferMessage(message, value, source, consumeToAccept);
        }

        /// <summary>
        /// Posts the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
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