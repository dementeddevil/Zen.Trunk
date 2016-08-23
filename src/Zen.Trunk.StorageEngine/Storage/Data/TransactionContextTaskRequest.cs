using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Zen.Trunk.Storage.Locking;

namespace Zen.Trunk.Storage.Data
{
	public interface ITransactionContextTaskRequest
	{
		ITrunkTransaction TransactionContext
		{
			get;
			set;
		}
	}

	/// <summary>
	/// TransactionContextTaskRequest is a specialisation of TaskRequest that
	/// flows the active transaction context with the request.
	/// </summary>
	/// <typeparam name="TResult"></typeparam>
	public class TransactionContextTaskRequest<TResult> :
		TaskRequest<TResult>,
		ITransactionContextTaskRequest
	{
		public TransactionContextTaskRequest()
		{
			TransactionContext = TrunkTransactionContext.Current;
		}

		public ITrunkTransaction TransactionContext
		{
			get;
			set;
		}
	}

	/// <summary>
	/// TransactionContextTaskRequest is a specialisation of TaskRequest that
	/// flows the active transaction context with the request.
	/// </summary>
	public class TransactionContextTaskRequest<TMessage, TResult> :
		TaskRequest<TMessage, TResult>,
		ITransactionContextTaskRequest
	{
		public TransactionContextTaskRequest()
		{
			TransactionContext = TrunkTransactionContext.Current;
		}

		public TransactionContextTaskRequest(TMessage message)
			: base(message)
		{
			TransactionContext = TrunkTransactionContext.Current;
		}

		public ITrunkTransaction TransactionContext
		{
			get;
			set;
		}
	}

	public class TransactionContextActionBlock<TRequest, TResult> : TaskRequestActionBlock<TRequest, TResult>
		where TRequest : TaskRequest<TResult>, ITransactionContextTaskRequest
	{
		public TransactionContextActionBlock(Func<TRequest, TResult> action)
			: this(action, new ExecutionDataflowBlockOptions())
		{
		}

		public TransactionContextActionBlock(Func<TRequest, TResult> action, ExecutionDataflowBlockOptions dataflowBlockOptions)
			: base(request => ExecuteActionWithContext(action, request), dataflowBlockOptions)
		{
		}

		public TransactionContextActionBlock(Func<TRequest, Task<TResult>> action)
			: this(action, new ExecutionDataflowBlockOptions())
		{
		}

		public TransactionContextActionBlock(Func<TRequest, Task<TResult>> action, ExecutionDataflowBlockOptions dataflowBlockOptions)
			: base(request => ExecuteActionWithContextAsync(action, request), dataflowBlockOptions)
		{
		}

		public override DataflowMessageStatus OfferMessage(DataflowMessageHeader message, TRequest value, ISourceBlock<TRequest> source, bool consumeToAccept)
		{
			EnsureTransactionContext(value);
			return base.OfferMessage(message, value, source, consumeToAccept);
		}

		public override bool Post(TRequest item)
		{
			EnsureTransactionContext(item);
			return base.Post(item);
		}

		private void EnsureTransactionContext(TRequest value)
		{
			var context = TrunkTransactionContext.Current;
			if (value.TransactionContext == null && context != null)
			{
				value.TransactionContext = context;
			}
		}

		private static TResult ExecuteActionWithContext(Func<TRequest, TResult> action, TRequest request)
		{
			using (var scope = TrunkTransactionContext.SwitchTransactionContext(request.TransactionContext))
			{
				return action(request);
			}
		}

		private static async Task<TResult> ExecuteActionWithContextAsync(Func<TRequest, Task<TResult>> action, TRequest request)
		{
			var scope = TrunkTransactionContext.SwitchTransactionContext(request.TransactionContext);
			try
			{
				return await action(request).ConfigureAwait(false);
			}
			finally
			{
				scope.Dispose();
			}
		}
	}

	public class TransactionContextActionBlock<TRequest, TMessage, TResult> : TaskRequestActionBlock<TRequest, TResult>
		where TRequest : TaskRequest<TMessage, TResult>, ITransactionContextTaskRequest
	{
		public TransactionContextActionBlock(Func<TRequest, TResult> action)
			: this(action, new ExecutionDataflowBlockOptions())
		{
		}

		public TransactionContextActionBlock(Func<TRequest, TResult> action, ExecutionDataflowBlockOptions dataflowBlockOptions)
			: base(request => ExecuteActionWithContext(action, request), dataflowBlockOptions)
		{
		}

		public TransactionContextActionBlock(Func<TRequest, Task<TResult>> action)
			: this(action, new ExecutionDataflowBlockOptions())
		{
		}

		public TransactionContextActionBlock(Func<TRequest, Task<TResult>> action, ExecutionDataflowBlockOptions dataflowBlockOptions)
			: base(request => ExecuteActionWithContextAsync(action, request), dataflowBlockOptions)
		{
		}

		public override DataflowMessageStatus OfferMessage(DataflowMessageHeader message, TRequest value, ISourceBlock<TRequest> source, bool consumeToAccept)
		{
			EnsureTransactionContext(value);
			return base.OfferMessage(message, value, source, consumeToAccept);
		}

		public override bool Post(TRequest item)
		{
			EnsureTransactionContext(item);
			return base.Post(item);
		}

		private void EnsureTransactionContext(TRequest value)
		{
			var context = TrunkTransactionContext.Current;
			if (value.TransactionContext == null && context != null)
			{
				value.TransactionContext = context;
			}
		}

		private static TResult ExecuteActionWithContext(Func<TRequest, TResult> action, TRequest request)
		{
			using (var scope = TrunkTransactionContext.SwitchTransactionContext(request.TransactionContext))
			{
				return action(request);
			}
		}

		private static async Task<TResult> ExecuteActionWithContextAsync(Func<TRequest, Task<TResult>> action, TRequest request)
		{
			var scope = TrunkTransactionContext.SwitchTransactionContext(request.TransactionContext);
			try
			{
				return await action(request).ConfigureAwait(false);
			}
			finally
			{
				scope.Dispose();
			}
		}
	}
}
