using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Zen.Trunk.Storage.Locking;
using Zen.Trunk.Utils;

namespace Zen.Trunk.Storage.Data
{
    /// <summary>
    /// TransactionContextTaskRequest is a specialisation of TaskRequest that
    /// flows the active transaction context with the request.
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    [DebuggerStepThrough]
    public class TransactionContextTaskRequest<TResult> :
        TaskRequest<TResult>,
        ITransactionContextTaskRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionContextTaskRequest{TResult}"/> class.
        /// </summary>
        public TransactionContextTaskRequest()
        {
            SessionContext = AmbientSessionContext.Current;
            TransactionContext = TrunkTransactionContext.Current;
        }

        /// <summary>
        /// Gets or sets the session context.
        /// </summary>
        /// <value>
        /// The session context.
        /// </value>
        public IAmbientSession SessionContext { get; set; }

        /// <summary>
        /// Gets or sets the transaction context.
        /// </summary>
        /// <value>
        /// The transaction context.
        /// </value>
        public ITrunkTransaction TransactionContext { get; set; }
    }

    /// <summary>
    /// TransactionContextTaskRequest is a specialisation of TaskRequest that
    /// flows the active transaction context with the request.
    /// </summary>
    [DebuggerStepThrough]
    public class TransactionContextTaskRequest<TMessage, TResult> :
        TaskRequest<TMessage, TResult>,
        ITransactionContextTaskRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionContextTaskRequest{TMessage, TResult}"/> class.
        /// </summary>
        public TransactionContextTaskRequest()
        {
            SessionContext = AmbientSessionContext.Current;
            TransactionContext = TrunkTransactionContext.Current;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionContextTaskRequest{TMessage, TResult}"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        public TransactionContextTaskRequest(TMessage message)
            : base(message)
        {
            SessionContext = AmbientSessionContext.Current;
            TransactionContext = TrunkTransactionContext.Current;
        }

        /// <summary>
        /// Gets or sets the session context.
        /// </summary>
        /// <value>
        /// The session context.
        /// </value>
        public IAmbientSession SessionContext { get; set; }

        /// <summary>
        /// Gets or sets the transaction context.
        /// </summary>
        /// <value>
        /// The transaction context.
        /// </value>
        public ITrunkTransaction TransactionContext { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TRequest">The type of the request.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <seealso cref="Zen.Trunk.Utils.TaskRequestActionBlock{TRequest, TResult}" />
    [DebuggerStepThrough]
    public class TransactionContextActionBlock<TRequest, TResult> : TaskRequestActionBlock<TRequest, TResult>
        where TRequest : TaskRequest<TResult>, ITransactionContextTaskRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionContextActionBlock{TRequest, TResult}"/> class.
        /// </summary>
        /// <param name="action">The action.</param>
        public TransactionContextActionBlock(Func<TRequest, TResult> action)
            : this(action, new ExecutionDataflowBlockOptions())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionContextActionBlock{TRequest, TResult}"/> class.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="dataflowBlockOptions">The dataflow block options.</param>
        public TransactionContextActionBlock(Func<TRequest, TResult> action, ExecutionDataflowBlockOptions dataflowBlockOptions)
            : base(request => request.ExecuteActionWithContext(action), dataflowBlockOptions)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionContextActionBlock{TRequest, TResult}"/> class.
        /// </summary>
        /// <param name="action">The action.</param>
        public TransactionContextActionBlock(Func<TRequest, Task<TResult>> action)
            : this(action, new ExecutionDataflowBlockOptions())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionContextActionBlock{TRequest, TResult}"/> class.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="dataflowBlockOptions">The dataflow block options.</param>
        public TransactionContextActionBlock(Func<TRequest, Task<TResult>> action, ExecutionDataflowBlockOptions dataflowBlockOptions)
            : base(request => request.ExecuteActionWithContextAsync(action), dataflowBlockOptions)
        {
        }

        /// <summary>
        /// Offers the message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="value">The value.</param>
        /// <param name="source">The source.</param>
        /// <param name="consumeToAccept">if set to <c>true</c> [consume to accept].</param>
        /// <returns></returns>
        public override DataflowMessageStatus OfferMessage(DataflowMessageHeader message, TRequest value, ISourceBlock<TRequest> source, bool consumeToAccept)
        {
            EnsureCapturedContext(value);
            return base.OfferMessage(message, value, source, consumeToAccept);
        }

        /// <summary>
        /// Posts the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public override bool Post(TRequest item)
        {
            EnsureCapturedContext(item);
            return base.Post(item);
        }

        private void EnsureCapturedContext(TRequest value)
        {
            var sessionContext = AmbientSessionContext.Current;
            if (value.SessionContext == null && sessionContext != null)
            {
                value.SessionContext = sessionContext;
            }

            var transactionContext = TrunkTransactionContext.Current;
            if (value.TransactionContext == null && transactionContext != null)
            {
                value.TransactionContext = transactionContext;
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TRequest">The type of the request.</typeparam>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <seealso cref="Zen.Trunk.Utils.TaskRequestActionBlock{TRequest, TResult}" />
    [DebuggerStepThrough]
    public class TransactionContextActionBlock<TRequest, TMessage, TResult> : TaskRequestActionBlock<TRequest, TResult>
        where TRequest : TaskRequest<TMessage, TResult>, ITransactionContextTaskRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionContextActionBlock{TRequest, TMessage, TResult}"/> class.
        /// </summary>
        /// <param name="action">The action.</param>
        public TransactionContextActionBlock(Func<TRequest, TResult> action)
            : this(action, new ExecutionDataflowBlockOptions())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionContextActionBlock{TRequest, TMessage, TResult}"/> class.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="dataflowBlockOptions">The dataflow block options.</param>
        public TransactionContextActionBlock(Func<TRequest, TResult> action, ExecutionDataflowBlockOptions dataflowBlockOptions)
            : base(request => request.ExecuteActionWithContext(action), dataflowBlockOptions)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionContextActionBlock{TRequest, TMessage, TResult}"/> class.
        /// </summary>
        /// <param name="action">The action.</param>
        public TransactionContextActionBlock(Func<TRequest, Task<TResult>> action)
            : this(action, new ExecutionDataflowBlockOptions())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionContextActionBlock{TRequest, TMessage, TResult}"/> class.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="dataflowBlockOptions">The dataflow block options.</param>
        public TransactionContextActionBlock(Func<TRequest, Task<TResult>> action, ExecutionDataflowBlockOptions dataflowBlockOptions)
            : base(request => request.ExecuteActionWithContextAsync(action), dataflowBlockOptions)
        {
        }

        /// <summary>
        /// Offers the message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="value">The value.</param>
        /// <param name="source">The source.</param>
        /// <param name="consumeToAccept">if set to <c>true</c> [consume to accept].</param>
        /// <returns></returns>
        public override DataflowMessageStatus OfferMessage(DataflowMessageHeader message, TRequest value, ISourceBlock<TRequest> source, bool consumeToAccept)
        {
            EnsureCapturedContext(value);
            return base.OfferMessage(message, value, source, consumeToAccept);
        }

        /// <summary>
        /// Posts the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public override bool Post(TRequest item)
        {
            EnsureCapturedContext(item);
            return base.Post(item);
        }

        private void EnsureCapturedContext(TRequest value)
        {
            var sessionContext = AmbientSessionContext.Current;
            if (value.SessionContext == null && sessionContext != null)
            {
                value.SessionContext = sessionContext;
            }

            var transactionContext = TrunkTransactionContext.Current;
            if (value.TransactionContext == null && transactionContext != null)
            {
                value.TransactionContext = transactionContext;
            }
        }
    }
}
