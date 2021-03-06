﻿using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Zen.Trunk.Utils
{
    /// <summary>
    /// 
    /// </summary>
    public interface ITargetBlockSet : ITargetBlock<object>
	{
	}

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T0">The type of the 0.</typeparam>
    /// <typeparam name="T1">The type of the 1.</typeparam>
    /// <seealso cref="Zen.Trunk.Utils.ITargetBlockSet" />
    public class TargetBlockSet<T0, T1> : ITargetBlockSet
	{
		private readonly ITargetBlock<T0> _t0;
		private readonly ITargetBlock<T1> _t1;

        /// <summary>
        /// Initializes a new instance of the <see cref="TargetBlockSet{T0, T1}"/> class.
        /// </summary>
        /// <param name="t0">The t0.</param>
        /// <param name="t1">The t1.</param>
        public TargetBlockSet(
			ITargetBlock<T0> t0,
			ITargetBlock<T1> t1)
		{
			_t0 = t0;
			_t1 = t1;
		}

		/// <summary>
		/// Gets a <see cref="T:System.Threading.Tasks.Task">Task</see> that represents the asynchronous operation and completion of the dataflow block.
		/// </summary>
		/// <value></value>
		/// <remarks>
		/// A dataflow block is considered completed when it is not currently processing a message and when it has guaranteed that it will not process
		/// any more messages. The returned <see cref="T:System.Threading.Tasks.Task">Task</see> will transition to a completed state when the
		/// associated block has completed. It will transition to the <see cref="T:System.Threading.Tasks.TaskStatus">RanToCompletion</see> state
		/// when the block completes its processing successfully according to the dataflow block’s defined semantics, it will transition to
		/// the <see cref="T:System.Threading.Tasks.TaskStatus">Faulted</see> state when the dataflow block has completed processing prematurely due to an unhandled exception,
		/// and it will transition to the <see cref="T:System.Threading.Tasks.TaskStatus">Canceled</see> state when the dataflow  block has completed processing
		/// prematurely due to receiving a cancellation request.  If the task completes in the Faulted state, its Exception property will return
		/// an <see cref="T:System.AggregateException"/> containing the one or more exceptions that caused the block to fail.
		/// </remarks>
		public Task Completion => Task.WhenAll(
		    _t0.Completion, 
		    _t1.Completion);

        /// <summary>
        /// Faults the specified exception.
        /// </summary>
        /// <param name="exception">The exception.</param>
        public void Fault(Exception exception)
		{
			_t0.Fault(exception);
			_t1.Fault(exception);
		}

        /// <summary>
        /// Completes this instance.
        /// </summary>
        public void Complete()
		{
			_t0.Complete();
			_t1.Complete();
		}

        /// <summary>
        /// Offers the message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="value">The value.</param>
        /// <param name="source">The source.</param>
        /// <param name="consumeToAccept">if set to <c>true</c> [consume to accept].</param>
        /// <returns></returns>
        public DataflowMessageStatus OfferMessage(DataflowMessageHeader message, object value, ISourceBlock<object> source, bool consumeToAccept)
		{
			if (value is T0)
			{
				return _t0.OfferMessage(
					message,
					(T0)value,
					(ISourceBlock<T0>)source,
					consumeToAccept);
			}
			if (value is T1)
			{
				return _t1.OfferMessage(
					message,
					(T1)value,
					(ISourceBlock<T1>)source,
					consumeToAccept);
			}
			return DataflowMessageStatus.Declined;
		}
	}

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T0">The type of the 0.</typeparam>
    /// <typeparam name="T1">The type of the 1.</typeparam>
    /// <typeparam name="T2">The type of the 2.</typeparam>
    /// <seealso cref="Zen.Trunk.Utils.ITargetBlockSet" />
    public class TargetBlockSet<T0, T1, T2> : ITargetBlockSet
	{
		private readonly ITargetBlock<T0> _t0;
		private readonly ITargetBlock<T1> _t1;
		private readonly ITargetBlock<T2> _t2;

        /// <summary>
        /// Initializes a new instance of the <see cref="TargetBlockSet{T0, T1, T2}"/> class.
        /// </summary>
        /// <param name="t0">The t0.</param>
        /// <param name="t1">The t1.</param>
        /// <param name="t2">The t2.</param>
        public TargetBlockSet(
			ITargetBlock<T0> t0,
			ITargetBlock<T1> t1,
			ITargetBlock<T2> t2)
		{
			_t0 = t0;
			_t1 = t1;
			_t2 = t2;
		}

        /// <summary>
        /// Gets a <see cref="T:System.Threading.Tasks.Task">Task</see> that represents the asynchronous operation and completion of the dataflow block.
        /// </summary>
        /// <value>
        /// The completion.
        /// </value>
        /// <remarks>
        /// A dataflow block is considered completed when it is not currently processing a message and when it has guaranteed that it will not process
        /// any more messages. The returned <see cref="T:System.Threading.Tasks.Task">Task</see> will transition to a completed state when the
        /// associated block has completed. It will transition to the <see cref="T:System.Threading.Tasks.TaskStatus">RanToCompletion</see> state
        /// when the block completes its processing successfully according to the dataflow block’s defined semantics, it will transition to
        /// the <see cref="T:System.Threading.Tasks.TaskStatus">Faulted</see> state when the dataflow block has completed processing prematurely due to an unhandled exception,
        /// and it will transition to the <see cref="T:System.Threading.Tasks.TaskStatus">Canceled</see> state when the dataflow  block has completed processing
        /// prematurely due to receiving a cancellation request.  If the task completes in the Faulted state, its Exception property will return
        /// an <see cref="T:System.AggregateException" /> containing the one or more exceptions that caused the block to fail.
        /// </remarks>
        public Task Completion => Task.WhenAll(
		    _t0.Completion,
		    _t1.Completion,
		    _t2.Completion);

        /// <summary>
        /// Faults the specified exception.
        /// </summary>
        /// <param name="exception">The exception.</param>
        public void Fault(Exception exception)
		{
			_t0.Fault(exception);
			_t1.Fault(exception);
			_t2.Fault(exception);
		}

        /// <summary>
        /// Completes this instance.
        /// </summary>
        public void Complete()
		{
			_t0.Complete();
			_t1.Complete();
			_t2.Complete();
		}

        /// <summary>
        /// Offers the message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="value">The value.</param>
        /// <param name="source">The source.</param>
        /// <param name="consumeToAccept">if set to <c>true</c> [consume to accept].</param>
        /// <returns></returns>
        public DataflowMessageStatus OfferMessage(DataflowMessageHeader message, object value, ISourceBlock<object> source, bool consumeToAccept)
		{
			if (value is T0)
			{
				return _t0.OfferMessage(
					message,
					(T0)value,
					(ISourceBlock<T0>)source,
					consumeToAccept);
			}
			if (value is T1)
			{
				return _t1.OfferMessage(
					message,
					(T1)value,
					(ISourceBlock<T1>)source,
					consumeToAccept);
			}
			if (value is T2)
			{
				return _t2.OfferMessage(
					message,
					(T2)value,
					(ISourceBlock<T2>)source,
					consumeToAccept);
			}
			return DataflowMessageStatus.Declined;
		}
	}

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T0">The type of the 0.</typeparam>
    /// <typeparam name="T1">The type of the 1.</typeparam>
    /// <typeparam name="T2">The type of the 2.</typeparam>
    /// <typeparam name="T3">The type of the 3.</typeparam>
    /// <seealso cref="Zen.Trunk.Utils.ITargetBlockSet" />
    public class TargetBlockSet<T0, T1, T2, T3> : ITargetBlockSet
	{
		private readonly ITargetBlock<T0> _t0;
		private readonly ITargetBlock<T1> _t1;
		private readonly ITargetBlock<T2> _t2;
		private readonly ITargetBlock<T3> _t3;

        /// <summary>
        /// Initializes a new instance of the <see cref="TargetBlockSet{T0, T1, T2, T3}"/> class.
        /// </summary>
        /// <param name="t0">The t0.</param>
        /// <param name="t1">The t1.</param>
        /// <param name="t2">The t2.</param>
        /// <param name="t3">The t3.</param>
        public TargetBlockSet(
			ITargetBlock<T0> t0,
			ITargetBlock<T1> t1,
			ITargetBlock<T2> t2,
			ITargetBlock<T3> t3)
		{
			_t0 = t0;
			_t1 = t1;
			_t2 = t2;
			_t3 = t3;
		}

		/// <summary>
		/// Gets a <see cref="T:System.Threading.Tasks.Task">Task</see> that represents the asynchronous operation and completion of the dataflow block.
		/// </summary>
		/// <value></value>
		/// <remarks>
		/// A dataflow block is considered completed when it is not currently processing a message and when it has guaranteed that it will not process
		/// any more messages. The returned <see cref="T:System.Threading.Tasks.Task">Task</see> will transition to a completed state when the
		/// associated block has completed. It will transition to the <see cref="T:System.Threading.Tasks.TaskStatus">RanToCompletion</see> state
		/// when the block completes its processing successfully according to the dataflow block’s defined semantics, it will transition to
		/// the <see cref="T:System.Threading.Tasks.TaskStatus">Faulted</see> state when the dataflow block has completed processing prematurely due to an unhandled exception,
		/// and it will transition to the <see cref="T:System.Threading.Tasks.TaskStatus">Canceled</see> state when the dataflow  block has completed processing
		/// prematurely due to receiving a cancellation request.  If the task completes in the Faulted state, its Exception property will return
		/// an <see cref="T:System.AggregateException"/> containing the one or more exceptions that caused the block to fail.
		/// </remarks>
		public Task Completion => Task.WhenAll(
		    _t0.Completion,
		    _t1.Completion,
		    _t2.Completion,
		    _t3.Completion);

        /// <summary>
        /// Faults the specified exception.
        /// </summary>
        /// <param name="exception">The exception.</param>
        public void Fault(Exception exception)
		{
			_t0.Fault(exception);
			_t1.Fault(exception);
			_t2.Fault(exception);
			_t3.Fault(exception);
		}

        /// <summary>
        /// Completes this instance.
        /// </summary>
        public void Complete()
		{
			_t0.Complete();
			_t1.Complete();
			_t2.Complete();
			_t3.Complete();
		}

        /// <summary>
        /// Offers the message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="value">The value.</param>
        /// <param name="source">The source.</param>
        /// <param name="consumeToAccept">if set to <c>true</c> [consume to accept].</param>
        /// <returns></returns>
        public DataflowMessageStatus OfferMessage(DataflowMessageHeader message, object value, ISourceBlock<object> source, bool consumeToAccept)
		{
			if (value is T0)
			{
				return _t0.OfferMessage(
					message,
					(T0)value,
					(ISourceBlock<T0>)source,
					consumeToAccept);
			}
			if (value is T1)
			{
				return _t1.OfferMessage(
					message,
					(T1)value,
					(ISourceBlock<T1>)source,
					consumeToAccept);
			}
			if (value is T2)
			{
				return _t2.OfferMessage(
					message,
					(T2)value,
					(ISourceBlock<T2>)source,
					consumeToAccept);
			}
			if (value is T3)
			{
				return _t3.OfferMessage(
					message,
					(T3)value,
					(ISourceBlock<T3>)source,
					consumeToAccept);
			}
			return DataflowMessageStatus.Declined;
		}
	}

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T0">The type of the 0.</typeparam>
    /// <typeparam name="T1">The type of the 1.</typeparam>
    /// <typeparam name="T2">The type of the 2.</typeparam>
    /// <typeparam name="T3">The type of the 3.</typeparam>
    /// <typeparam name="T4">The type of the 4.</typeparam>
    /// <seealso cref="Zen.Trunk.Utils.ITargetBlockSet" />
    public class TargetBlockSet<T0, T1, T2, T3, T4> : ITargetBlockSet
	{
		private readonly ITargetBlock<T0> _t0;
		private readonly ITargetBlock<T1> _t1;
		private readonly ITargetBlock<T2> _t2;
		private readonly ITargetBlock<T3> _t3;
		private readonly ITargetBlock<T4> _t4;

        /// <summary>
        /// Initializes a new instance of the <see cref="TargetBlockSet{T0, T1, T2, T3, T4}"/> class.
        /// </summary>
        /// <param name="t0">The t0.</param>
        /// <param name="t1">The t1.</param>
        /// <param name="t2">The t2.</param>
        /// <param name="t3">The t3.</param>
        /// <param name="t4">The t4.</param>
        public TargetBlockSet(
			ITargetBlock<T0> t0,
			ITargetBlock<T1> t1,
			ITargetBlock<T2> t2,
			ITargetBlock<T3> t3,
			ITargetBlock<T4> t4)
		{
			_t0 = t0;
			_t1 = t1;
			_t2 = t2;
			_t3 = t3;
			_t4 = t4;
		}

		/// <summary>
		/// Gets a <see cref="T:System.Threading.Tasks.Task">Task</see> that represents the asynchronous operation and completion of the dataflow block.
		/// </summary>
		/// <value></value>
		/// <remarks>
		/// A dataflow block is considered completed when it is not currently processing a message and when it has guaranteed that it will not process
		/// any more messages. The returned <see cref="T:System.Threading.Tasks.Task">Task</see> will transition to a completed state when the
		/// associated block has completed. It will transition to the <see cref="T:System.Threading.Tasks.TaskStatus">RanToCompletion</see> state
		/// when the block completes its processing successfully according to the dataflow block’s defined semantics, it will transition to
		/// the <see cref="T:System.Threading.Tasks.TaskStatus">Faulted</see> state when the dataflow block has completed processing prematurely due to an unhandled exception,
		/// and it will transition to the <see cref="T:System.Threading.Tasks.TaskStatus">Canceled</see> state when the dataflow  block has completed processing
		/// prematurely due to receiving a cancellation request.  If the task completes in the Faulted state, its Exception property will return
		/// an <see cref="T:System.AggregateException"/> containing the one or more exceptions that caused the block to fail.
		/// </remarks>
		public Task Completion => Task.WhenAll(
		    _t0.Completion,
		    _t1.Completion,
		    _t2.Completion,
		    _t3.Completion,
		    _t4.Completion);

        /// <summary>
        /// Faults the specified exception.
        /// </summary>
        /// <param name="exception">The exception.</param>
        public void Fault(Exception exception)
		{
			_t0.Fault(exception);
			_t1.Fault(exception);
			_t2.Fault(exception);
			_t3.Fault(exception);
			_t4.Fault(exception);
		}

        /// <summary>
        /// Completes this instance.
        /// </summary>
        public void Complete()
		{
			_t0.Complete();
			_t1.Complete();
			_t2.Complete();
			_t3.Complete();
			_t4.Complete();
		}

        /// <summary>
        /// Offers the message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="value">The value.</param>
        /// <param name="source">The source.</param>
        /// <param name="consumeToAccept">if set to <c>true</c> [consume to accept].</param>
        /// <returns></returns>
        public DataflowMessageStatus OfferMessage(DataflowMessageHeader message, object value, ISourceBlock<object> source, bool consumeToAccept)
		{
			if (value is T0)
			{
				return _t0.OfferMessage(
					message,
					(T0)value,
					(ISourceBlock<T0>)source,
					consumeToAccept);
			}
			if (value is T1)
			{
				return _t1.OfferMessage(
					message,
					(T1)value,
					(ISourceBlock<T1>)source,
					consumeToAccept);
			}
			if (value is T2)
			{
				return _t2.OfferMessage(
					message,
					(T2)value,
					(ISourceBlock<T2>)source,
					consumeToAccept);
			}
			if (value is T3)
			{
				return _t3.OfferMessage(
					message,
					(T3)value,
					(ISourceBlock<T3>)source,
					consumeToAccept);
			}
			if (value is T4)
			{
				return _t4.OfferMessage(
					message,
					(T4)value,
					(ISourceBlock<T4>)source,
					consumeToAccept);
			}
			return DataflowMessageStatus.Declined;
		}
	}

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T0">The type of the 0.</typeparam>
    /// <typeparam name="T1">The type of the 1.</typeparam>
    /// <typeparam name="T2">The type of the 2.</typeparam>
    /// <typeparam name="T3">The type of the 3.</typeparam>
    /// <typeparam name="T4">The type of the 4.</typeparam>
    /// <typeparam name="T5">The type of the 5.</typeparam>
    /// <seealso cref="Zen.Trunk.Utils.ITargetBlockSet" />
    public class TargetBlockSet<T0, T1, T2, T3, T4, T5> : ITargetBlockSet
	{
		private readonly ITargetBlock<T0> _t0;
		private readonly ITargetBlock<T1> _t1;
		private readonly ITargetBlock<T2> _t2;
		private readonly ITargetBlock<T3> _t3;
		private readonly ITargetBlock<T4> _t4;
		private readonly ITargetBlock<T5> _t5;

        /// <summary>
        /// Initializes a new instance of the <see cref="TargetBlockSet{T0, T1, T2, T3, T4, T5}"/> class.
        /// </summary>
        /// <param name="t0">The t0.</param>
        /// <param name="t1">The t1.</param>
        /// <param name="t2">The t2.</param>
        /// <param name="t3">The t3.</param>
        /// <param name="t4">The t4.</param>
        /// <param name="t5">The t5.</param>
        public TargetBlockSet(
			ITargetBlock<T0> t0,
			ITargetBlock<T1> t1,
			ITargetBlock<T2> t2,
			ITargetBlock<T3> t3,
			ITargetBlock<T4> t4,
			ITargetBlock<T5> t5)
		{
			_t0 = t0;
			_t1 = t1;
			_t2 = t2;
			_t3 = t3;
			_t4 = t4;
			_t5 = t5;
		}

		/// <summary>
		/// Gets a <see cref="T:System.Threading.Tasks.Task">Task</see> that represents the asynchronous operation and completion of the dataflow block.
		/// </summary>
		/// <value></value>
		/// <remarks>
		/// A dataflow block is considered completed when it is not currently processing a message and when it has guaranteed that it will not process
		/// any more messages. The returned <see cref="T:System.Threading.Tasks.Task">Task</see> will transition to a completed state when the
		/// associated block has completed. It will transition to the <see cref="T:System.Threading.Tasks.TaskStatus">RanToCompletion</see> state
		/// when the block completes its processing successfully according to the dataflow block’s defined semantics, it will transition to
		/// the <see cref="T:System.Threading.Tasks.TaskStatus">Faulted</see> state when the dataflow block has completed processing prematurely due to an unhandled exception,
		/// and it will transition to the <see cref="T:System.Threading.Tasks.TaskStatus">Canceled</see> state when the dataflow  block has completed processing
		/// prematurely due to receiving a cancellation request.  If the task completes in the Faulted state, its Exception property will return
		/// an <see cref="T:System.AggregateException"/> containing the one or more exceptions that caused the block to fail.
		/// </remarks>
		public Task Completion => Task.WhenAll(
		    _t0.Completion,
		    _t1.Completion,
		    _t2.Completion,
		    _t3.Completion,
		    _t4.Completion,
		    _t5.Completion);

        /// <summary>
        /// Faults the specified exception.
        /// </summary>
        /// <param name="exception">The exception.</param>
        public void Fault(Exception exception)
		{
			_t0.Fault(exception);
			_t1.Fault(exception);
			_t2.Fault(exception);
			_t3.Fault(exception);
			_t4.Fault(exception);
			_t5.Fault(exception);
		}

        /// <summary>
        /// Completes this instance.
        /// </summary>
        public void Complete()
		{
			_t0.Complete();
			_t1.Complete();
			_t2.Complete();
			_t3.Complete();
			_t4.Complete();
			_t5.Complete();
		}

        /// <summary>
        /// Offers the message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="value">The value.</param>
        /// <param name="source">The source.</param>
        /// <param name="consumeToAccept">if set to <c>true</c> [consume to accept].</param>
        /// <returns></returns>
        public DataflowMessageStatus OfferMessage(DataflowMessageHeader message, object value, ISourceBlock<object> source, bool consumeToAccept)
		{
			if (value is T0)
			{
				return _t0.OfferMessage(
					message,
					(T0)value,
					(ISourceBlock<T0>)source,
					consumeToAccept);
			}
			if (value is T1)
			{
				return _t1.OfferMessage(
					message,
					(T1)value,
					(ISourceBlock<T1>)source,
					consumeToAccept);
			}
			if (value is T2)
			{
				return _t2.OfferMessage(
					message,
					(T2)value,
					(ISourceBlock<T2>)source,
					consumeToAccept);
			}
			if (value is T3)
			{
				return _t3.OfferMessage(
					message,
					(T3)value,
					(ISourceBlock<T3>)source,
					consumeToAccept);
			}
			if (value is T4)
			{
				return _t4.OfferMessage(
					message,
					(T4)value,
					(ISourceBlock<T4>)source,
					consumeToAccept);
			}
			if (value is T5)
			{
				return _t5.OfferMessage(
					message,
					(T5)value,
					(ISourceBlock<T5>)source,
					consumeToAccept);
			}
			return DataflowMessageStatus.Declined;
		}
	}

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T0">The type of the 0.</typeparam>
    /// <typeparam name="T1">The type of the 1.</typeparam>
    /// <typeparam name="T2">The type of the 2.</typeparam>
    /// <typeparam name="T3">The type of the 3.</typeparam>
    /// <typeparam name="T4">The type of the 4.</typeparam>
    /// <typeparam name="T5">The type of the 5.</typeparam>
    /// <typeparam name="T6">The type of the 6.</typeparam>
    /// <seealso cref="Zen.Trunk.Utils.ITargetBlockSet" />
    public class TargetBlockSet<T0, T1, T2, T3, T4, T5, T6> : ITargetBlockSet
	{
		private readonly ITargetBlock<T0> _t0;
		private readonly ITargetBlock<T1> _t1;
		private readonly ITargetBlock<T2> _t2;
		private readonly ITargetBlock<T3> _t3;
		private readonly ITargetBlock<T4> _t4;
		private readonly ITargetBlock<T5> _t5;
		private readonly ITargetBlock<T6> _t6;

        /// <summary>
        /// Initializes a new instance of the <see cref="TargetBlockSet{T0, T1, T2, T3, T4, T5, T6}"/> class.
        /// </summary>
        /// <param name="t0">The t0.</param>
        /// <param name="t1">The t1.</param>
        /// <param name="t2">The t2.</param>
        /// <param name="t3">The t3.</param>
        /// <param name="t4">The t4.</param>
        /// <param name="t5">The t5.</param>
        /// <param name="t6">The t6.</param>
        public TargetBlockSet(
			ITargetBlock<T0> t0,
			ITargetBlock<T1> t1,
			ITargetBlock<T2> t2,
			ITargetBlock<T3> t3,
			ITargetBlock<T4> t4,
			ITargetBlock<T5> t5,
			ITargetBlock<T6> t6)
		{
			_t0 = t0;
			_t1 = t1;
			_t2 = t2;
			_t3 = t3;
			_t4 = t4;
			_t5 = t5;
			_t6 = t6;
		}

        /// <summary>
        /// Gets a <see cref="T:System.Threading.Tasks.Task">Task</see> that represents the asynchronous operation and completion of the dataflow block.
        /// </summary>
        /// <value>
        /// The completion.
        /// </value>
        /// <remarks>
        /// A dataflow block is considered completed when it is not currently processing a message and when it has guaranteed that it will not process
        /// any more messages. The returned <see cref="T:System.Threading.Tasks.Task">Task</see> will transition to a completed state when the
        /// associated block has completed. It will transition to the <see cref="T:System.Threading.Tasks.TaskStatus">RanToCompletion</see> state
        /// when the block completes its processing successfully according to the dataflow block’s defined semantics, it will transition to
        /// the <see cref="T:System.Threading.Tasks.TaskStatus">Faulted</see> state when the dataflow block has completed processing prematurely due to an unhandled exception,
        /// and it will transition to the <see cref="T:System.Threading.Tasks.TaskStatus">Canceled</see> state when the dataflow  block has completed processing
        /// prematurely due to receiving a cancellation request.  If the task completes in the Faulted state, its Exception property will return
        /// an <see cref="T:System.AggregateException" /> containing the one or more exceptions that caused the block to fail.
        /// </remarks>
        public Task Completion => Task.WhenAll(
		    _t0.Completion,
		    _t1.Completion,
		    _t2.Completion,
		    _t3.Completion,
		    _t4.Completion,
		    _t5.Completion,
		    _t6.Completion);

        /// <summary>
        /// Faults the specified exception.
        /// </summary>
        /// <param name="exception">The exception.</param>
        public void Fault(Exception exception)
		{
			_t0.Fault(exception);
			_t1.Fault(exception);
			_t2.Fault(exception);
			_t3.Fault(exception);
			_t4.Fault(exception);
			_t5.Fault(exception);
			_t6.Fault(exception);
		}

        /// <summary>
        /// Completes this instance.
        /// </summary>
        public void Complete()
		{
			_t0.Complete();
			_t1.Complete();
			_t2.Complete();
			_t3.Complete();
			_t4.Complete();
			_t5.Complete();
			_t6.Complete();
		}

        /// <summary>
        /// Offers the message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="value">The value.</param>
        /// <param name="source">The source.</param>
        /// <param name="consumeToAccept">if set to <c>true</c> [consume to accept].</param>
        /// <returns></returns>
        public DataflowMessageStatus OfferMessage(DataflowMessageHeader message, object value, ISourceBlock<object> source, bool consumeToAccept)
		{
			if (value is T0)
			{
				return _t0.OfferMessage(
					message,
					(T0)value,
					(ISourceBlock<T0>)source,
					consumeToAccept);
			}
			if (value is T1)
			{
				return _t1.OfferMessage(
					message,
					(T1)value,
					(ISourceBlock<T1>)source,
					consumeToAccept);
			}
			if (value is T2)
			{
				return _t2.OfferMessage(
					message,
					(T2)value,
					(ISourceBlock<T2>)source,
					consumeToAccept);
			}
			if (value is T3)
			{
				return _t3.OfferMessage(
					message,
					(T3)value,
					(ISourceBlock<T3>)source,
					consumeToAccept);
			}
			if (value is T4)
			{
				return _t4.OfferMessage(
					message,
					(T4)value,
					(ISourceBlock<T4>)source,
					consumeToAccept);
			}
			if (value is T5)
			{
				return _t5.OfferMessage(
					message,
					(T5)value,
					(ISourceBlock<T5>)source,
					consumeToAccept);
			}
			if (value is T6)
			{
				return _t6.OfferMessage(
					message,
					(T6)value,
					(ISourceBlock<T6>)source,
					consumeToAccept);
			}
			return DataflowMessageStatus.Declined;
		}
	}

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T0">The type of the 0.</typeparam>
    /// <typeparam name="T1">The type of the 1.</typeparam>
    /// <typeparam name="T2">The type of the 2.</typeparam>
    /// <typeparam name="T3">The type of the 3.</typeparam>
    /// <typeparam name="T4">The type of the 4.</typeparam>
    /// <typeparam name="T5">The type of the 5.</typeparam>
    /// <typeparam name="T6">The type of the 6.</typeparam>
    /// <typeparam name="T7">The type of the 7.</typeparam>
    /// <seealso cref="Zen.Trunk.Utils.ITargetBlockSet" />
    public class TargetBlockSet<T0, T1, T2, T3, T4, T5, T6, T7> : ITargetBlockSet
	{
		private readonly ITargetBlock<T0> _t0;
		private readonly ITargetBlock<T1> _t1;
		private readonly ITargetBlock<T2> _t2;
		private readonly ITargetBlock<T3> _t3;
		private readonly ITargetBlock<T4> _t4;
		private readonly ITargetBlock<T5> _t5;
		private readonly ITargetBlock<T6> _t6;
		private readonly ITargetBlock<T7> _t7;

        /// <summary>
        /// Initializes a new instance of the <see cref="TargetBlockSet{T0, T1, T2, T3, T4, T5, T6, T7}"/> class.
        /// </summary>
        /// <param name="t0">The t0.</param>
        /// <param name="t1">The t1.</param>
        /// <param name="t2">The t2.</param>
        /// <param name="t3">The t3.</param>
        /// <param name="t4">The t4.</param>
        /// <param name="t5">The t5.</param>
        /// <param name="t6">The t6.</param>
        /// <param name="t7">The t7.</param>
        public TargetBlockSet(
			ITargetBlock<T0> t0,
			ITargetBlock<T1> t1,
			ITargetBlock<T2> t2,
			ITargetBlock<T3> t3,
			ITargetBlock<T4> t4,
			ITargetBlock<T5> t5,
			ITargetBlock<T6> t6,
			ITargetBlock<T7> t7)
		{
			_t0 = t0;
			_t1 = t1;
			_t2 = t2;
			_t3 = t3;
			_t4 = t4;
			_t5 = t5;
			_t6 = t6;
			_t7 = t7;
		}

		/// <summary>
		/// Gets a <see cref="T:System.Threading.Tasks.Task">Task</see> that represents the asynchronous operation and completion of the dataflow block.
		/// </summary>
		/// <value></value>
		/// <remarks>
		/// A dataflow block is considered completed when it is not currently processing a message and when it has guaranteed that it will not process
		/// any more messages. The returned <see cref="T:System.Threading.Tasks.Task">Task</see> will transition to a completed state when the
		/// associated block has completed. It will transition to the <see cref="T:System.Threading.Tasks.TaskStatus">RanToCompletion</see> state
		/// when the block completes its processing successfully according to the dataflow block’s defined semantics, it will transition to
		/// the <see cref="T:System.Threading.Tasks.TaskStatus">Faulted</see> state when the dataflow block has completed processing prematurely due to an unhandled exception,
		/// and it will transition to the <see cref="T:System.Threading.Tasks.TaskStatus">Canceled</see> state when the dataflow  block has completed processing
		/// prematurely due to receiving a cancellation request.  If the task completes in the Faulted state, its Exception property will return
		/// an <see cref="T:System.AggregateException"/> containing the one or more exceptions that caused the block to fail.
		/// </remarks>
		public Task Completion => Task.WhenAll(
		    _t0.Completion,
		    _t1.Completion,
		    _t2.Completion,
		    _t3.Completion,
		    _t4.Completion,
		    _t5.Completion,
		    _t6.Completion,
		    _t7.Completion);

        /// <summary>
        /// Faults the specified exception.
        /// </summary>
        /// <param name="exception">The exception.</param>
        public void Fault(Exception exception)
		{
			_t0.Fault(exception);
			_t1.Fault(exception);
			_t2.Fault(exception);
			_t3.Fault(exception);
			_t4.Fault(exception);
			_t5.Fault(exception);
			_t6.Fault(exception);
			_t7.Fault(exception);
		}

        /// <summary>
        /// Completes this instance.
        /// </summary>
        public void Complete()
		{
			_t0.Complete();
			_t1.Complete();
			_t2.Complete();
			_t3.Complete();
			_t4.Complete();
			_t5.Complete();
			_t6.Complete();
			_t7.Complete();
		}

        /// <summary>
        /// Offers the message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="value">The value.</param>
        /// <param name="source">The source.</param>
        /// <param name="consumeToAccept">if set to <c>true</c> [consume to accept].</param>
        /// <returns></returns>
        public DataflowMessageStatus OfferMessage(DataflowMessageHeader message, object value, ISourceBlock<object> source, bool consumeToAccept)
		{
			if (value is T0)
			{
				return _t0.OfferMessage(
					message,
					(T0)value,
					(ISourceBlock<T0>)source,
					consumeToAccept);
			}
			if (value is T1)
			{
				return _t1.OfferMessage(
					message,
					(T1)value,
					(ISourceBlock<T1>)source,
					consumeToAccept);
			}
			if (value is T2)
			{
				return _t2.OfferMessage(
					message,
					(T2)value,
					(ISourceBlock<T2>)source,
					consumeToAccept);
			}
			if (value is T3)
			{
				return _t3.OfferMessage(
					message,
					(T3)value,
					(ISourceBlock<T3>)source,
					consumeToAccept);
			}
			if (value is T4)
			{
				return _t4.OfferMessage(
					message,
					(T4)value,
					(ISourceBlock<T4>)source,
					consumeToAccept);
			}
			if (value is T5)
			{
				return _t5.OfferMessage(
					message,
					(T5)value,
					(ISourceBlock<T5>)source,
					consumeToAccept);
			}
			if (value is T6)
			{
				return _t6.OfferMessage(
					message,
					(T6)value,
					(ISourceBlock<T6>)source,
					consumeToAccept);
			}
			if (value is T7)
			{
				return _t7.OfferMessage(
					message,
					(T7)value,
					(ISourceBlock<T7>)source,
					consumeToAccept);
			}
			return DataflowMessageStatus.Declined;
		}
	}

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T0">The type of the 0.</typeparam>
    /// <typeparam name="T1">The type of the 1.</typeparam>
    /// <typeparam name="T2">The type of the 2.</typeparam>
    /// <typeparam name="T3">The type of the 3.</typeparam>
    /// <typeparam name="T4">The type of the 4.</typeparam>
    /// <typeparam name="T5">The type of the 5.</typeparam>
    /// <typeparam name="T6">The type of the 6.</typeparam>
    /// <typeparam name="T7">The type of the 7.</typeparam>
    /// <typeparam name="T8">The type of the 8.</typeparam>
    /// <seealso cref="Zen.Trunk.Utils.ITargetBlockSet" />
    public class TargetBlockSet<T0, T1, T2, T3, T4, T5, T6, T7, T8> : ITargetBlockSet
	{
		private readonly ITargetBlock<T0> _t0;
		private readonly ITargetBlock<T1> _t1;
		private readonly ITargetBlock<T2> _t2;
		private readonly ITargetBlock<T3> _t3;
		private readonly ITargetBlock<T4> _t4;
		private readonly ITargetBlock<T5> _t5;
		private readonly ITargetBlock<T6> _t6;
		private readonly ITargetBlock<T7> _t7;
		private readonly ITargetBlock<T8> _t8;

        /// <summary>
        /// Initializes a new instance of the <see cref="TargetBlockSet{T0, T1, T2, T3, T4, T5, T6, T7, T8}"/> class.
        /// </summary>
        /// <param name="t0">The t0.</param>
        /// <param name="t1">The t1.</param>
        /// <param name="t2">The t2.</param>
        /// <param name="t3">The t3.</param>
        /// <param name="t4">The t4.</param>
        /// <param name="t5">The t5.</param>
        /// <param name="t6">The t6.</param>
        /// <param name="t7">The t7.</param>
        /// <param name="t8">The t8.</param>
        public TargetBlockSet(
			ITargetBlock<T0> t0,
			ITargetBlock<T1> t1,
			ITargetBlock<T2> t2,
			ITargetBlock<T3> t3,
			ITargetBlock<T4> t4,
			ITargetBlock<T5> t5,
			ITargetBlock<T6> t6,
			ITargetBlock<T7> t7,
			ITargetBlock<T8> t8)
		{
			_t0 = t0;
			_t1 = t1;
			_t2 = t2;
			_t3 = t3;
			_t4 = t4;
			_t5 = t5;
			_t6 = t6;
			_t7 = t7;
			_t8 = t8;
		}

		/// <summary>
		/// Gets a <see cref="T:System.Threading.Tasks.Task">Task</see> that represents the asynchronous operation and completion of the dataflow block.
		/// </summary>
		/// <value></value>
		/// <remarks>
		/// A dataflow block is considered completed when it is not currently processing a message and when it has guaranteed that it will not process
		/// any more messages. The returned <see cref="T:System.Threading.Tasks.Task">Task</see> will transition to a completed state when the
		/// associated block has completed. It will transition to the <see cref="T:System.Threading.Tasks.TaskStatus">RanToCompletion</see> state
		/// when the block completes its processing successfully according to the dataflow block’s defined semantics, it will transition to
		/// the <see cref="T:System.Threading.Tasks.TaskStatus">Faulted</see> state when the dataflow block has completed processing prematurely due to an unhandled exception,
		/// and it will transition to the <see cref="T:System.Threading.Tasks.TaskStatus">Canceled</see> state when the dataflow  block has completed processing
		/// prematurely due to receiving a cancellation request.  If the task completes in the Faulted state, its Exception property will return
		/// an <see cref="T:System.AggregateException"/> containing the one or more exceptions that caused the block to fail.
		/// </remarks>
		public Task Completion => Task.WhenAll(
		    _t0.Completion,
		    _t1.Completion,
		    _t2.Completion,
		    _t3.Completion,
		    _t4.Completion,
		    _t5.Completion,
		    _t6.Completion,
		    _t7.Completion,
		    _t8.Completion);

        /// <summary>
        /// Faults the specified exception.
        /// </summary>
        /// <param name="exception">The exception.</param>
        public void Fault(Exception exception)
		{
			_t0.Fault(exception);
			_t1.Fault(exception);
			_t2.Fault(exception);
			_t3.Fault(exception);
			_t4.Fault(exception);
			_t5.Fault(exception);
			_t6.Fault(exception);
			_t7.Fault(exception);
			_t8.Fault(exception);
		}

        /// <summary>
        /// Completes this instance.
        /// </summary>
        public void Complete()
		{
			_t0.Complete();
			_t1.Complete();
			_t2.Complete();
			_t3.Complete();
			_t4.Complete();
			_t5.Complete();
			_t6.Complete();
			_t7.Complete();
			_t8.Complete();
		}

        /// <summary>
        /// Offers the message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="value">The value.</param>
        /// <param name="source">The source.</param>
        /// <param name="consumeToAccept">if set to <c>true</c> [consume to accept].</param>
        /// <returns></returns>
        public DataflowMessageStatus OfferMessage(DataflowMessageHeader message, object value, ISourceBlock<object> source, bool consumeToAccept)
		{
            if (value is T0)
            {
                return _t0.OfferMessage(
                    message,
                    (T0)value,
                    (ISourceBlock<T0>)source,
                    consumeToAccept);
            }
            if (value is T1)
            {
                return _t1.OfferMessage(
                    message,
                    (T1)value,
                    (ISourceBlock<T1>)source,
                    consumeToAccept);
            }
            if (value is T2)
            {
                return _t2.OfferMessage(
                    message,
                    (T2)value,
                    (ISourceBlock<T2>)source,
                    consumeToAccept);
            }
            if (value is T3)
            {
                return _t3.OfferMessage(
                    message,
                    (T3)value,
                    (ISourceBlock<T3>)source,
                    consumeToAccept);
            }
            if (value is T4)
            {
                return _t4.OfferMessage(
                    message,
                    (T4)value,
                    (ISourceBlock<T4>)source,
                    consumeToAccept);
            }
            if (value is T5)
            {
                return _t5.OfferMessage(
                    message,
                    (T5)value,
                    (ISourceBlock<T5>)source,
                    consumeToAccept);
            }
            if (value is T6)
            {
                return _t6.OfferMessage(
                    message,
                    (T6)value,
                    (ISourceBlock<T6>)source,
                    consumeToAccept);
            }
            if (value is T7)
            {
                return _t7.OfferMessage(
                    message,
                    (T7)value,
                    (ISourceBlock<T7>)source,
                    consumeToAccept);
            }
            if (value is T8)
			{
				return _t8.OfferMessage(
					message,
					(T8)value,
					(ISourceBlock<T8>)source,
					consumeToAccept);
			}
			return DataflowMessageStatus.Declined;
		}
	}

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T0">The type of the 0.</typeparam>
    /// <typeparam name="T1">The type of the 1.</typeparam>
    /// <typeparam name="T2">The type of the 2.</typeparam>
    /// <typeparam name="T3">The type of the 3.</typeparam>
    /// <typeparam name="T4">The type of the 4.</typeparam>
    /// <typeparam name="T5">The type of the 5.</typeparam>
    /// <typeparam name="T6">The type of the 6.</typeparam>
    /// <typeparam name="T7">The type of the 7.</typeparam>
    /// <typeparam name="T8">The type of the 8.</typeparam>
    /// <typeparam name="T9">The type of the 9.</typeparam>
    /// <seealso cref="Zen.Trunk.Utils.ITargetBlockSet" />
    public class TargetBlockSet<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> : ITargetBlockSet
	{
		private readonly ITargetBlock<T0> _t0;
		private readonly ITargetBlock<T1> _t1;
		private readonly ITargetBlock<T2> _t2;
		private readonly ITargetBlock<T3> _t3;
		private readonly ITargetBlock<T4> _t4;
		private readonly ITargetBlock<T5> _t5;
		private readonly ITargetBlock<T6> _t6;
		private readonly ITargetBlock<T7> _t7;
		private readonly ITargetBlock<T8> _t8;
		private readonly ITargetBlock<T9> _t9;

        /// <summary>
        /// Initializes a new instance of the <see cref="TargetBlockSet{T0, T1, T2, T3, T4, T5, T6, T7, T8, T9}"/> class.
        /// </summary>
        /// <param name="t0">The t0.</param>
        /// <param name="t1">The t1.</param>
        /// <param name="t2">The t2.</param>
        /// <param name="t3">The t3.</param>
        /// <param name="t4">The t4.</param>
        /// <param name="t5">The t5.</param>
        /// <param name="t6">The t6.</param>
        /// <param name="t7">The t7.</param>
        /// <param name="t8">The t8.</param>
        /// <param name="t9">The t9.</param>
        public TargetBlockSet(
			ITargetBlock<T0> t0,
			ITargetBlock<T1> t1,
			ITargetBlock<T2> t2,
			ITargetBlock<T3> t3,
			ITargetBlock<T4> t4,
			ITargetBlock<T5> t5,
			ITargetBlock<T6> t6,
			ITargetBlock<T7> t7,
			ITargetBlock<T8> t8,
			ITargetBlock<T9> t9)
		{
			_t0 = t0;
			_t1 = t1;
			_t2 = t2;
			_t3 = t3;
			_t4 = t4;
			_t5 = t5;
			_t6 = t6;
			_t7 = t7;
			_t8 = t8;
			_t9 = t9;
		}

		/// <summary>
		/// Gets a <see cref="T:System.Threading.Tasks.Task">Task</see> that represents the asynchronous operation and completion of the dataflow block.
		/// </summary>
		/// <value></value>
		/// <remarks>
		/// A dataflow block is considered completed when it is not currently processing a message and when it has guaranteed that it will not process
		/// any more messages. The returned <see cref="T:System.Threading.Tasks.Task">Task</see> will transition to a completed state when the
		/// associated block has completed. It will transition to the <see cref="T:System.Threading.Tasks.TaskStatus">RanToCompletion</see> state
		/// when the block completes its processing successfully according to the dataflow block’s defined semantics, it will transition to
		/// the <see cref="T:System.Threading.Tasks.TaskStatus">Faulted</see> state when the dataflow block has completed processing prematurely due to an unhandled exception,
		/// and it will transition to the <see cref="T:System.Threading.Tasks.TaskStatus">Canceled</see> state when the dataflow  block has completed processing
		/// prematurely due to receiving a cancellation request.  If the task completes in the Faulted state, its Exception property will return
		/// an <see cref="T:System.AggregateException"/> containing the one or more exceptions that caused the block to fail.
		/// </remarks>
		public Task Completion => Task.WhenAll(
		    _t0.Completion,
		    _t1.Completion,
		    _t2.Completion,
		    _t3.Completion,
		    _t4.Completion,
		    _t5.Completion,
		    _t6.Completion,
		    _t7.Completion,
		    _t8.Completion,
		    _t9.Completion);

        /// <summary>
        /// Faults the specified exception.
        /// </summary>
        /// <param name="exception">The exception.</param>
        public void Fault(Exception exception)
		{
			_t0.Fault(exception);
			_t1.Fault(exception);
			_t2.Fault(exception);
			_t3.Fault(exception);
			_t4.Fault(exception);
			_t5.Fault(exception);
			_t6.Fault(exception);
			_t7.Fault(exception);
			_t8.Fault(exception);
			_t9.Fault(exception);
		}

        /// <summary>
        /// Completes this instance.
        /// </summary>
        public void Complete()
		{
			_t0.Complete();
			_t1.Complete();
			_t2.Complete();
			_t3.Complete();
			_t4.Complete();
			_t5.Complete();
			_t6.Complete();
			_t7.Complete();
			_t8.Complete();
			_t9.Complete();
		}

        /// <summary>
        /// Offers the message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="value">The value.</param>
        /// <param name="source">The source.</param>
        /// <param name="consumeToAccept">if set to <c>true</c> [consume to accept].</param>
        /// <returns></returns>
        public DataflowMessageStatus OfferMessage(DataflowMessageHeader message, object value, ISourceBlock<object> source, bool consumeToAccept)
		{
            if (value is T0)
            {
                return _t0.OfferMessage(
                    message,
                    (T0)value,
                    (ISourceBlock<T0>)source,
                    consumeToAccept);
            }
            if (value is T1)
            {
                return _t1.OfferMessage(
                    message,
                    (T1)value,
                    (ISourceBlock<T1>)source,
                    consumeToAccept);
            }
            if (value is T2)
            {
                return _t2.OfferMessage(
                    message,
                    (T2)value,
                    (ISourceBlock<T2>)source,
                    consumeToAccept);
            }
            if (value is T3)
            {
                return _t3.OfferMessage(
                    message,
                    (T3)value,
                    (ISourceBlock<T3>)source,
                    consumeToAccept);
            }
            if (value is T4)
            {
                return _t4.OfferMessage(
                    message,
                    (T4)value,
                    (ISourceBlock<T4>)source,
                    consumeToAccept);
            }
            if (value is T5)
            {
                return _t5.OfferMessage(
                    message,
                    (T5)value,
                    (ISourceBlock<T5>)source,
                    consumeToAccept);
            }
            if (value is T6)
            {
                return _t6.OfferMessage(
                    message,
                    (T6)value,
                    (ISourceBlock<T6>)source,
                    consumeToAccept);
            }
            if (value is T7)
            {
                return _t7.OfferMessage(
                    message,
                    (T7)value,
                    (ISourceBlock<T7>)source,
                    consumeToAccept);
            }
            if (value is T8)
			{
				return _t8.OfferMessage(
					message,
					(T8)value,
					(ISourceBlock<T8>)source,
					consumeToAccept);
			}
			if (value is T9)
			{
				return _t9.OfferMessage(
					message,
					(T9)value,
					(ISourceBlock<T9>)source,
					consumeToAccept);
			}
			return DataflowMessageStatus.Declined;
		}
	}

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T0">The type of the 0.</typeparam>
    /// <typeparam name="T1">The type of the 1.</typeparam>
    /// <typeparam name="T2">The type of the 2.</typeparam>
    /// <typeparam name="T3">The type of the 3.</typeparam>
    /// <typeparam name="T4">The type of the 4.</typeparam>
    /// <typeparam name="T5">The type of the 5.</typeparam>
    /// <typeparam name="T6">The type of the 6.</typeparam>
    /// <typeparam name="T7">The type of the 7.</typeparam>
    /// <typeparam name="T8">The type of the 8.</typeparam>
    /// <typeparam name="T9">The type of the 9.</typeparam>
    /// <typeparam name="T10">The type of the 10.</typeparam>
    /// <seealso cref="Zen.Trunk.Utils.ITargetBlockSet" />
    public class TargetBlockSet<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> : ITargetBlockSet
	{
		private readonly ITargetBlock<T0> _t0;
		private readonly ITargetBlock<T1> _t1;
		private readonly ITargetBlock<T2> _t2;
		private readonly ITargetBlock<T3> _t3;
		private readonly ITargetBlock<T4> _t4;
		private readonly ITargetBlock<T5> _t5;
		private readonly ITargetBlock<T6> _t6;
		private readonly ITargetBlock<T7> _t7;
		private readonly ITargetBlock<T8> _t8;
		private readonly ITargetBlock<T9> _t9;
		private readonly ITargetBlock<T10> _t10;

        /// <summary>
        /// Initializes a new instance of the <see cref="TargetBlockSet{T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10}"/> class.
        /// </summary>
        /// <param name="t0">The t0.</param>
        /// <param name="t1">The t1.</param>
        /// <param name="t2">The t2.</param>
        /// <param name="t3">The t3.</param>
        /// <param name="t4">The t4.</param>
        /// <param name="t5">The t5.</param>
        /// <param name="t6">The t6.</param>
        /// <param name="t7">The t7.</param>
        /// <param name="t8">The t8.</param>
        /// <param name="t9">The t9.</param>
        /// <param name="t10">The T10.</param>
        public TargetBlockSet(
			ITargetBlock<T0> t0,
			ITargetBlock<T1> t1,
			ITargetBlock<T2> t2,
			ITargetBlock<T3> t3,
			ITargetBlock<T4> t4,
			ITargetBlock<T5> t5,
			ITargetBlock<T6> t6,
			ITargetBlock<T7> t7,
			ITargetBlock<T8> t8,
			ITargetBlock<T9> t9,
			ITargetBlock<T10> t10)
		{
			_t0 = t0;
			_t1 = t1;
			_t2 = t2;
			_t3 = t3;
			_t4 = t4;
			_t5 = t5;
			_t6 = t6;
			_t7 = t7;
			_t8 = t8;
			_t9 = t9;
			_t10 = t10;
		}

		/// <summary>
		/// Gets a <see cref="T:System.Threading.Tasks.Task">Task</see> that represents the asynchronous operation and completion of the dataflow block.
		/// </summary>
		/// <value></value>
		/// <remarks>
		/// A dataflow block is considered completed when it is not currently processing a message and when it has guaranteed that it will not process
		/// any more messages. The returned <see cref="T:System.Threading.Tasks.Task">Task</see> will transition to a completed state when the
		/// associated block has completed. It will transition to the <see cref="T:System.Threading.Tasks.TaskStatus">RanToCompletion</see> state
		/// when the block completes its processing successfully according to the dataflow block’s defined semantics, it will transition to
		/// the <see cref="T:System.Threading.Tasks.TaskStatus">Faulted</see> state when the dataflow block has completed processing prematurely due to an unhandled exception,
		/// and it will transition to the <see cref="T:System.Threading.Tasks.TaskStatus">Canceled</see> state when the dataflow  block has completed processing
		/// prematurely due to receiving a cancellation request.  If the task completes in the Faulted state, its Exception property will return
		/// an <see cref="T:System.AggregateException"/> containing the one or more exceptions that caused the block to fail.
		/// </remarks>
		public Task Completion => Task.WhenAll(
		    _t0.Completion,
		    _t1.Completion,
		    _t2.Completion,
		    _t3.Completion,
		    _t4.Completion,
		    _t5.Completion,
		    _t6.Completion,
		    _t7.Completion,
		    _t8.Completion,
		    _t9.Completion,
		    _t10.Completion);

        /// <summary>
        /// Faults the specified exception.
        /// </summary>
        /// <param name="exception">The exception.</param>
        public void Fault(Exception exception)
		{
			_t0.Fault(exception);
			_t1.Fault(exception);
			_t2.Fault(exception);
			_t3.Fault(exception);
			_t4.Fault(exception);
			_t5.Fault(exception);
			_t6.Fault(exception);
			_t7.Fault(exception);
			_t8.Fault(exception);
			_t9.Fault(exception);
			_t10.Fault(exception);
		}

        /// <summary>
        /// Completes this instance.
        /// </summary>
        public void Complete()
		{
			_t0.Complete();
			_t1.Complete();
			_t2.Complete();
			_t3.Complete();
			_t4.Complete();
			_t5.Complete();
			_t6.Complete();
			_t7.Complete();
			_t8.Complete();
			_t9.Complete();
			_t10.Complete();
		}

        /// <summary>
        /// Offers the message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="value">The value.</param>
        /// <param name="source">The source.</param>
        /// <param name="consumeToAccept">if set to <c>true</c> [consume to accept].</param>
        /// <returns></returns>
        public DataflowMessageStatus OfferMessage(DataflowMessageHeader message, object value, ISourceBlock<object> source, bool consumeToAccept)
		{
            if (value is T0)
            {
                return _t0.OfferMessage(
                    message,
                    (T0)value,
                    (ISourceBlock<T0>)source,
                    consumeToAccept);
            }
            if (value is T1)
            {
                return _t1.OfferMessage(
                    message,
                    (T1)value,
                    (ISourceBlock<T1>)source,
                    consumeToAccept);
            }
            if (value is T2)
            {
                return _t2.OfferMessage(
                    message,
                    (T2)value,
                    (ISourceBlock<T2>)source,
                    consumeToAccept);
            }
            if (value is T3)
            {
                return _t3.OfferMessage(
                    message,
                    (T3)value,
                    (ISourceBlock<T3>)source,
                    consumeToAccept);
            }
            if (value is T4)
            {
                return _t4.OfferMessage(
                    message,
                    (T4)value,
                    (ISourceBlock<T4>)source,
                    consumeToAccept);
            }
            if (value is T5)
            {
                return _t5.OfferMessage(
                    message,
                    (T5)value,
                    (ISourceBlock<T5>)source,
                    consumeToAccept);
            }
            if (value is T6)
            {
                return _t6.OfferMessage(
                    message,
                    (T6)value,
                    (ISourceBlock<T6>)source,
                    consumeToAccept);
            }
            if (value is T7)
            {
                return _t7.OfferMessage(
                    message,
                    (T7)value,
                    (ISourceBlock<T7>)source,
                    consumeToAccept);
            }
            if (value is T8)
            {
                return _t8.OfferMessage(
                    message,
                    (T8)value,
                    (ISourceBlock<T8>)source,
                    consumeToAccept);
            }
            if (value is T9)
            {
                return _t9.OfferMessage(
                    message,
                    (T9)value,
                    (ISourceBlock<T9>)source,
                    consumeToAccept);
            }
            if (value is T10)
			{
				return _t10.OfferMessage(
					message,
					(T10)value,
					(ISourceBlock<T10>)source,
					consumeToAccept);
			}
			return DataflowMessageStatus.Declined;
		}
	}

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T0">The type of the 0.</typeparam>
    /// <typeparam name="T1">The type of the 1.</typeparam>
    /// <typeparam name="T2">The type of the 2.</typeparam>
    /// <typeparam name="T3">The type of the 3.</typeparam>
    /// <typeparam name="T4">The type of the 4.</typeparam>
    /// <typeparam name="T5">The type of the 5.</typeparam>
    /// <typeparam name="T6">The type of the 6.</typeparam>
    /// <typeparam name="T7">The type of the 7.</typeparam>
    /// <typeparam name="T8">The type of the 8.</typeparam>
    /// <typeparam name="T9">The type of the 9.</typeparam>
    /// <typeparam name="T10">The type of the 10.</typeparam>
    /// <typeparam name="T11">The type of the 11.</typeparam>
    /// <seealso cref="Zen.Trunk.Utils.ITargetBlockSet" />
    public class TargetBlockSet<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> : ITargetBlockSet
	{
		private readonly ITargetBlock<T0> _t0;
		private readonly ITargetBlock<T1> _t1;
		private readonly ITargetBlock<T2> _t2;
		private readonly ITargetBlock<T3> _t3;
		private readonly ITargetBlock<T4> _t4;
		private readonly ITargetBlock<T5> _t5;
		private readonly ITargetBlock<T6> _t6;
		private readonly ITargetBlock<T7> _t7;
		private readonly ITargetBlock<T8> _t8;
		private readonly ITargetBlock<T9> _t9;
		private readonly ITargetBlock<T10> _t10;
		private readonly ITargetBlock<T11> _t11;

        /// <summary>
        /// Initializes a new instance of the <see cref="TargetBlockSet{T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11}"/> class.
        /// </summary>
        /// <param name="t0">The t0.</param>
        /// <param name="t1">The t1.</param>
        /// <param name="t2">The t2.</param>
        /// <param name="t3">The t3.</param>
        /// <param name="t4">The t4.</param>
        /// <param name="t5">The t5.</param>
        /// <param name="t6">The t6.</param>
        /// <param name="t7">The t7.</param>
        /// <param name="t8">The t8.</param>
        /// <param name="t9">The t9.</param>
        /// <param name="t10">The T10.</param>
        /// <param name="t11">The T11.</param>
        public TargetBlockSet(
			ITargetBlock<T0> t0,
			ITargetBlock<T1> t1,
			ITargetBlock<T2> t2,
			ITargetBlock<T3> t3,
			ITargetBlock<T4> t4,
			ITargetBlock<T5> t5,
			ITargetBlock<T6> t6,
			ITargetBlock<T7> t7,
			ITargetBlock<T8> t8,
			ITargetBlock<T9> t9,
			ITargetBlock<T10> t10,
			ITargetBlock<T11> t11)
		{
			_t0 = t0;
			_t1 = t1;
			_t2 = t2;
			_t3 = t3;
			_t4 = t4;
			_t5 = t5;
			_t6 = t6;
			_t7 = t7;
			_t8 = t8;
			_t9 = t9;
			_t10 = t10;
			_t11 = t11;
		}

		/// <summary>
		/// Gets a <see cref="T:System.Threading.Tasks.Task">Task</see> that represents the asynchronous operation and completion of the dataflow block.
		/// </summary>
		/// <value></value>
		/// <remarks>
		/// A dataflow block is considered completed when it is not currently processing a message and when it has guaranteed that it will not process
		/// any more messages. The returned <see cref="T:System.Threading.Tasks.Task">Task</see> will transition to a completed state when the
		/// associated block has completed. It will transition to the <see cref="T:System.Threading.Tasks.TaskStatus">RanToCompletion</see> state
		/// when the block completes its processing successfully according to the dataflow block’s defined semantics, it will transition to
		/// the <see cref="T:System.Threading.Tasks.TaskStatus">Faulted</see> state when the dataflow block has completed processing prematurely due to an unhandled exception,
		/// and it will transition to the <see cref="T:System.Threading.Tasks.TaskStatus">Canceled</see> state when the dataflow  block has completed processing
		/// prematurely due to receiving a cancellation request.  If the task completes in the Faulted state, its Exception property will return
		/// an <see cref="T:System.AggregateException"/> containing the one or more exceptions that caused the block to fail.
		/// </remarks>
		public Task Completion => Task.WhenAll(
		    _t0.Completion,
		    _t1.Completion,
		    _t2.Completion,
		    _t3.Completion,
		    _t4.Completion,
		    _t5.Completion,
		    _t6.Completion,
		    _t7.Completion,
		    _t8.Completion,
		    _t9.Completion,
		    _t10.Completion,
		    _t11.Completion);

        /// <summary>
        /// Faults the specified exception.
        /// </summary>
        /// <param name="exception">The exception.</param>
        public void Fault(Exception exception)
		{
			_t0.Fault(exception);
			_t1.Fault(exception);
			_t2.Fault(exception);
			_t3.Fault(exception);
			_t4.Fault(exception);
			_t5.Fault(exception);
			_t6.Fault(exception);
			_t7.Fault(exception);
			_t8.Fault(exception);
			_t9.Fault(exception);
			_t10.Fault(exception);
			_t11.Fault(exception);
		}

        /// <summary>
        /// Completes this instance.
        /// </summary>
        public void Complete()
		{
			_t0.Complete();
			_t1.Complete();
			_t2.Complete();
			_t3.Complete();
			_t4.Complete();
			_t5.Complete();
			_t6.Complete();
			_t7.Complete();
			_t8.Complete();
			_t9.Complete();
			_t10.Complete();
			_t11.Complete();
		}

        /// <summary>
        /// Offers the message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="value">The value.</param>
        /// <param name="source">The source.</param>
        /// <param name="consumeToAccept">if set to <c>true</c> [consume to accept].</param>
        /// <returns></returns>
        public DataflowMessageStatus OfferMessage(DataflowMessageHeader message, object value, ISourceBlock<object> source, bool consumeToAccept)
		{
            if (value is T0)
            {
                return _t0.OfferMessage(
                    message,
                    (T0)value,
                    (ISourceBlock<T0>)source,
                    consumeToAccept);
            }
            if (value is T1)
            {
                return _t1.OfferMessage(
                    message,
                    (T1)value,
                    (ISourceBlock<T1>)source,
                    consumeToAccept);
            }
            if (value is T2)
            {
                return _t2.OfferMessage(
                    message,
                    (T2)value,
                    (ISourceBlock<T2>)source,
                    consumeToAccept);
            }
            if (value is T3)
            {
                return _t3.OfferMessage(
                    message,
                    (T3)value,
                    (ISourceBlock<T3>)source,
                    consumeToAccept);
            }
            if (value is T4)
            {
                return _t4.OfferMessage(
                    message,
                    (T4)value,
                    (ISourceBlock<T4>)source,
                    consumeToAccept);
            }
            if (value is T5)
            {
                return _t5.OfferMessage(
                    message,
                    (T5)value,
                    (ISourceBlock<T5>)source,
                    consumeToAccept);
            }
            if (value is T6)
            {
                return _t6.OfferMessage(
                    message,
                    (T6)value,
                    (ISourceBlock<T6>)source,
                    consumeToAccept);
            }
            if (value is T7)
            {
                return _t7.OfferMessage(
                    message,
                    (T7)value,
                    (ISourceBlock<T7>)source,
                    consumeToAccept);
            }
            if (value is T8)
            {
                return _t8.OfferMessage(
                    message,
                    (T8)value,
                    (ISourceBlock<T8>)source,
                    consumeToAccept);
            }
            if (value is T9)
            {
                return _t9.OfferMessage(
                    message,
                    (T9)value,
                    (ISourceBlock<T9>)source,
                    consumeToAccept);
            }
            if (value is T10)
			{
				return _t10.OfferMessage(
					message,
					(T10)value,
					(ISourceBlock<T10>)source,
					consumeToAccept);
			}
			if (value is T11)
			{
				return _t11.OfferMessage(
					message,
					(T11)value,
					(ISourceBlock<T11>)source,
					consumeToAccept);
			}
			return DataflowMessageStatus.Declined;
		}
	}
}
