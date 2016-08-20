namespace Zen.Trunk.Torrent.Client
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading.Tasks;
	using System.Threading.Tasks.Dataflow;
	using Zen.Trunk.Torrent.Common;
	using System.Threading;

	internal class ReverseComparer : IComparer<Priority>
	{
		public int Compare(Priority x, Priority y)
		{
			// High priority will sort to the top of the list
			return ((int)y).CompareTo((int)x);
		}
	}

	public class MainLoop
	{
		private class DelegateTask
		{
			private Task<object> _task;

			public DelegateTask()
			{
			}

			public bool IsRecurring
			{
				get
				{
					return RecurrencePeriod.HasValue && LoopRecurringFunction != null;
				}
			}

			public bool IsBlocking
			{
				get;
				set;
			}

			public TimeSpan? RecurrencePeriod
			{
				get;
				set;
			}

			public Action LoopAction
			{
				get;
				set;
			}

			public Func<object> DelegateFunction
			{
				get;
				set;
			}

			public Func<bool> LoopRecurringFunction
			{
				get;
				set;
			}

			public Task<object> TaskHandle
			{
				get
				{
					return _task;
				}
			}

			public void Initialise()
			{
				RecurrencePeriod = null;
				IsBlocking = false;
				DelegateFunction = null;
				LoopAction = null;
				LoopRecurringFunction = null;
			}

			public Task<object> Execute()
			{
				_task = ExecuteCore();
				return _task;
			}

			private async Task<object> ExecuteCore()
			{
				object result = null;

				bool keepAlive = true;
				while (keepAlive)
				{
					keepAlive = false;
					if (DelegateFunction != null)
					{
						result = DelegateFunction();
					}
					else if (LoopAction != null)
					{
						LoopAction();
					}
					else if (LoopRecurringFunction != null)
					{
						keepAlive = LoopRecurringFunction();
						if (keepAlive && RecurrencePeriod.HasValue)
						{
							await Task.Delay(RecurrencePeriod.Value);
						}
						else
						{
							keepAlive = false;
						}
					}
				}

				return result;
			}
		}

		private class PriorityTransferBlock :
			ISourceBlock<DelegateTask>,
			ITargetBlock<DelegateTask>
		{
			private class TargetLink : IDisposable
			{
				private PriorityTransferBlock _owner;
				private int _remainingMessages;

				public TargetLink(
					PriorityTransferBlock owner,
					ITargetBlock<DelegateTask> target,
					DataflowLinkOptions linkOptions)
				{
					_owner = owner;
					Target = target;
					LinkOptions = linkOptions;
					if (linkOptions.MaxMessages > 0)
					{
						_remainingMessages = linkOptions.MaxMessages;
					}
				}

				public ITargetBlock<DelegateTask> Target
				{
					get;
					private set;
				}

				public DataflowLinkOptions LinkOptions
				{
					get;
					private set;
				}

				public bool HandledLastMessage()
				{
					if (LinkOptions.MaxMessages > 0)
					{
						if (Interlocked.Decrement(ref _remainingMessages) == 0)
						{
							return true;
						}
					}
					return false;
				}

				public void Dispose()
				{
					if (_owner != null)
					{
						_owner.RemoveTarget(this);
						_owner = null;
					}
				}
			}

			private ConcurrentPriorityQueue<int, DelegateTask> _priorityQueue =
				new ConcurrentPriorityQueue<int, DelegateTask>();
			private volatile int _messageId = 0;
			private bool _decliningPermanently;

			private List<TargetLink> _linkedTargets =
				new List<TargetLink>();

			private Task _runTask;
			private AutoResetEvent _dataAvailable;

			public PriorityTransferBlock()
			{
				_dataAvailable = new AutoResetEvent(false);
				_runTask = Task.Run(
					() =>
					{
						while (!_decliningPermanently || _priorityQueue.Count > 0)
						{
							// Wait for data note we check exit state every 100ms
							if (!_decliningPermanently && !_dataAvailable.WaitOne(100))
							{
								continue;
							}

							// We have data, so offer to linked targets
							KeyValuePair<int, DelegateTask> topmostTask;
							while (_linkedTargets.Count > 0 &&
								_priorityQueue.TryPeek(out topmostTask))
							{
								// Create message
								DataflowMessageHeader message =
									new DataflowMessageHeader(
										Interlocked.Increment(ref _messageId));

								// Copy targets to array so we can dispose of
								//	one-shot targets as needed.
								TargetLink[] targets = null;
								lock (_linkedTargets)
								{
									targets = _linkedTargets.ToArray();
								}

								// Walk the list of targets
								foreach (TargetLink link in targets)
								{
									// Offer the message to the target
									// NOTE: Target must consume to accept
									DataflowMessageStatus status =
										link.Target.OfferMessage(message, topmostTask.Value, this, true);
									if (status == DataflowMessageStatus.DecliningPermanently)
									{
										// Target is no longer accepting messages
										//	so remove linkage from our list
										link.Dispose();
										continue;
									}
									if (status == DataflowMessageStatus.Accepted)
									{
										// Message has been accepted

										// Unlink the target if we need to
										if (link.HandledLastMessage())
										{
											link.Dispose();
										}
										break;
									}
								}
							}
						}
					});
			}

			private void RemoveTarget(TargetLink target)
			{
				lock (_linkedTargets)
				{
					_linkedTargets.Remove(target);
				}
			}

			#region IDataflowBlock Members
			public Task Completion
			{
				get
				{
					return _runTask;
				}
			}
			#endregion

			#region ITargetBlock<DelegateTask> Members
			public void Fault(Exception exception)
			{
				// Inform linked blocks...
				if (_linkedTargets != null && _linkedTargets.Any())
				{
					TargetLink[] targets = null;
					lock (_linkedTargets)
					{
						targets = _linkedTargets.ToArray();
					}

					foreach (var target in targets)
					{
						if (target.LinkOptions.PropagateCompletion)
						{
							target.Target.Fault(exception);
						}
					}
				}

				// TODO: Post fault...
				_decliningPermanently = true;
			}

			public void Complete()
			{
				// Inform linked blocks...
				if (_linkedTargets != null && _linkedTargets.Any())
				{
					TargetLink[] targets = null;
					lock (_linkedTargets)
					{
						targets = _linkedTargets.ToArray();
					}

					foreach (var target in targets)
					{
						if (target.LinkOptions.PropagateCompletion)
						{
							target.Target.Complete();
						}
					}
				}
				_decliningPermanently = true;
			}

			public DataflowMessageStatus OfferMessage(
				DataflowMessageHeader message,
				DelegateTask value,
				ISourceBlock<DelegateTask> source,
				bool consumeToAccept)
			{
				if (_decliningPermanently)
				{
					return DataflowMessageStatus.DecliningPermanently;
				}
				if (consumeToAccept)
				{
					bool messageConsumed;
					value = source.ConsumeMessage(message, this, out messageConsumed);
					if (!messageConsumed)
					{
						return DataflowMessageStatus.NotAvailable;
					}
				}
				_priorityQueue.Enqueue(
					new KeyValuePair<int, DelegateTask>(
						(int)Priority.Normal,
						value));
				return DataflowMessageStatus.Accepted;
			}

			public bool Post(DelegateTask item)
			{
				return Post(item, Priority.Normal);
			}

			public bool Post(DelegateTask item, Priority priority)
			{
				if (_decliningPermanently)
				{
					return false;
				}

				_priorityQueue.Enqueue(
					new KeyValuePair<int, DelegateTask>((int)priority, item));
				_dataAvailable.Set();
				return true;
			}
			#endregion

			#region ISourceBlock<DelegateTask> Members
			public DelegateTask ConsumeMessage(
				DataflowMessageHeader message,
				ITargetBlock<DelegateTask> target,
				out bool messageConsumed)
			{
				// If this message is not topmost in the queue then return null
				KeyValuePair<int, DelegateTask> topmostTask;
				if (!_priorityQueue.TryDequeue(out topmostTask))
				{
					messageConsumed = false;
					return null;
				}

				messageConsumed = true;
				return topmostTask.Value;
			}

			public IDisposable LinkTo(ITargetBlock<DelegateTask> target, DataflowLinkOptions linkOptions)
			{
				TargetLink link = new TargetLink(this, target, linkOptions);
				lock (_linkedTargets)
				{
					if (linkOptions.Append)
					{
						_linkedTargets.Add(link);
					}
					else
					{
						_linkedTargets.Insert(0, link);
					}
				}
				return link;
			}

			public bool ReserveMessage(DataflowMessageHeader message, ITargetBlock<DelegateTask> target)
			{
				throw new NotImplementedException();
			}

			public void ReleaseReservation(DataflowMessageHeader message, ITargetBlock<DelegateTask> target)
			{
				throw new NotImplementedException();
			}

			public bool TryReceive(Predicate<DelegateTask> filter, out DelegateTask item)
			{
				item = null;
				lock (_priorityQueue)
				{
					KeyValuePair<int, DelegateTask> task;
					while (true)
					{
						if (!_priorityQueue.TryPeek(out task) ||
							!filter(task.Value))
						{
							return false;
						}
						if (!_priorityQueue.TryDequeue(out task))
						{
							return false;
						}
						item = task.Value;
						return true;
					}
				}
			}

			public bool TryReceiveAll(out IList<DelegateTask> items)
			{
				List<DelegateTask> taskList = null;

				KeyValuePair<int, DelegateTask> task;
				if (_priorityQueue.TryDequeue(out task))
				{
					taskList = new List<DelegateTask>();
					taskList.Add(task.Value);
				}

				items = taskList;
				return (taskList != null);
			}
			#endregion
		}

		private bool _disposed;
		private ITargetBlock<DelegateTask> _taskPort;
		private PriorityTransferBlock _priorityTransferPort;
		private IDisposable _portLink;

		private ObjectPool<DelegateTask> _spareTasks =
			new ObjectPool<DelegateTask>(() => new DelegateTask());

		public MainLoop(string name)
		{
			_taskPort = new ActionBlock<DelegateTask>(
				new Func<DelegateTask, Task>(ExecuteTask),
				new ExecutionDataflowBlockOptions
				{
					TaskScheduler = TaskScheduler.Default,
					MaxDegreeOfParallelism = 4,
					MaxMessagesPerTask = 4
				});
			_priorityTransferPort = new PriorityTransferBlock();
			_portLink = _priorityTransferPort.LinkTo(_taskPort);
		}

		public void Dispose()
		{
			if (!_disposed)
			{
				_disposed = true;
				_priorityTransferPort.Complete();
				_priorityTransferPort.Completion.ContinueWith(
					(task) =>
					{
						// Dispose of our port link object
						_portLink.Dispose();
						_portLink = null;

						// Discard transfer port
						_priorityTransferPort = null;

						// Shutdown the task
						_taskPort.Complete();
						_taskPort.Completion.Wait();
					});
			}
		}

		public Task QueueAsync(Action task)
		{
			return QueueAsync(task, Priority.Normal);
		}

		public Task QueueAsync(Action task, Priority priority)
		{
			DelegateTask dTask = GetSpare();
			dTask.LoopAction = task;
			Queue(dTask, priority);
			return dTask.TaskHandle;
		}

		public Task<object> QueueAsync(Func<object> job)
		{
			return QueueAsync(job, Priority.Normal);
		}

		public Task<object> QueueAsync(Func<object> job, Priority priority)
		{
			DelegateTask dTask = GetSpare();
			dTask.DelegateFunction = job;
			Queue(dTask, priority);
			return dTask.TaskHandle;
		}

		public Task<TResult> QueueAsync<TResult>(Func<object> job)
		{
			return QueueAsync<TResult>(job, Priority.Normal);
		}

		public Task<TResult> QueueAsync<TResult>(Func<object> job, Priority priority)
		{
			DelegateTask dTask = GetSpare();
			dTask.DelegateFunction = job;
			Queue(dTask, priority);
			return dTask.TaskHandle.ContinueWith<TResult>(
				(task) => (TResult)task.Result,
				TaskContinuationOptions.OnlyOnRanToCompletion |
				TaskContinuationOptions.ExecuteSynchronously);
		}

		public void QueueRecurring(TimeSpan period, Func<bool> task)
		{
			QueueRecurring(period, task, Priority.Normal);
		}

		public void QueueRecurring(TimeSpan period, Func<bool> task, Priority priority)
		{
			DelegateTask dTask = GetSpare();
			dTask.RecurrencePeriod = period;
			dTask.LoopRecurringFunction = task;
			Queue(dTask, priority);
		}

		private async Task ExecuteTask(DelegateTask task)
		{
			if (task.IsRecurring)
			{
				// Don't bother to wait for recurring tasks; we simply ensure
				//	the delegate task is reused if and when it completes.
				task.Execute()
					.ContinueWith((result) => AddSpare(task));
			}
			else
			{
				bool canReuse = !task.IsBlocking;
				try
				{
					await task.Execute();
				}
				finally
				{
					if (canReuse)
					{
						AddSpare(task);
					}
				}
			}
		}

		private void Queue(DelegateTask task)
		{
			Queue(task, Priority.Normal);
		}

		private void Queue(DelegateTask task, Priority priority)
		{
			if (!_priorityTransferPort.Post(task, priority))
			{
				throw new ObjectDisposedException(GetType().FullName);
			}
		}

		private void AddSpare(DelegateTask task)
		{
			_spareTasks.PutObject(task);
		}

		private DelegateTask GetSpare()
		{
			DelegateTask task = _spareTasks.GetObject();
			task.Initialise();
			return task;
		}
	}
}