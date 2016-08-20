namespace Zen.Trunk.Torrent.Common
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Threading.Tasks.Dataflow;
	using Zen.Trunk.Torrent.Client.Encryption;

	public delegate long Operation<T>(T target);

	public class AsyncEventPool : IDisposable
	{
		private abstract class AsyncEvent : TaskCompletionSource<bool>
		{
			protected AsyncEvent()
			{
			}

			internal abstract void Raise();
		}

		private class AsyncEvent<T> : AsyncEvent
		{
			private EventHandler<T> _handler;
			private object _sender;
			private T _eventArgs;

			public AsyncEvent(EventHandler<T> handler, object sender, T args)
			{
				_handler = handler;
				_sender = sender;
				_eventArgs = args;
			}

			internal override void Raise()
			{
				if (_handler != null)
				{
					_handler(_sender, _eventArgs);
				}
				TrySetResult(true);
			}
		}

		private ActionBlock<AsyncEvent> _block;

		public AsyncEventPool()
		{
			_block = new ActionBlock<AsyncEvent>(
				(request) =>
				{
					request.Raise();
				},
				new ExecutionDataflowBlockOptions
				{
					MaxDegreeOfParallelism = 4,
				});
		}

		public Task RaiseAsyncEvent<T>(EventHandler<T> handler, object sender, T args)
		{
			AsyncEvent<T> action = new AsyncEvent<T>(handler, sender, args);
			if (!_block.Post(action))
			{
				throw new InvalidOperationException("Async event not accepted.");
			}
			return action.Task;
		}

		public void Dispose()
		{
			if (_block != null)
			{
				_block.Complete();
				_block.Completion.Wait(10000);
			}
		}
	}

	public static class Toolbox
	{
		private static Random r = new Random();
		private static AsyncEventPool eventPool = new AsyncEventPool();

		public static Task RaiseAsyncEvent<T>(EventHandler<T> e, object sender, T args)
			where T : EventArgs
		{
			if (e == null)
			{
				return CompletedTask.Default;
			}
			if (eventPool == null)
			{
				throw new InvalidOperationException("System is shutting down.");
			}

			return eventPool.RaiseAsyncEvent<T>(e, sender, args);
		}

		public static void Shutdown()
		{
			var pool = Interlocked.Exchange(ref eventPool, null);
			if (pool != null)
			{
				pool.Dispose();
			}
		}

		/// <summary>
		/// Randomizes the contents of the array
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="array"></param>
		public static void Randomize<T>(List<T> array)
		{
			List<T> clone = new List<T>(array);
			array.Clear();

			while (clone.Count > 0)
			{
				int index = r.Next(0, clone.Count);
				array.Add(clone[index]);
				clone.RemoveAt(index);
			}
		}

		/// <summary>
		/// Switches the positions of two elements in an array
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="array"></param>
		/// <param name="first"></param>
		/// <param name="second"></param>
		public static void Switch<T>(IList<T> array, int first, int second)
		{
			T obj = array[first];
			array[first] = array[second];
			array[second] = obj;
		}

		/// <summary>
		/// Creates a hex string from the given byte[]
		/// </summary>
		/// <param name="infoHash">The byte[] to create the hex string from</param>
		/// <returns></returns>
		public static string ToHex(byte[] array)
		{
			StringBuilder sb = new StringBuilder();

			foreach (byte b in array)
			{
				sb.AppendFormat("X2", b);
			}
			return sb.ToString();
		}

		/// <summary>
		/// Checks to see if the contents of two byte arrays are equal
		/// </summary>
		/// <param name="array1">The first array</param>
		/// <param name="array2">The second array</param>
		/// <returns>True if the arrays are equal, false if they aren't</returns>
		public static bool ByteMatch(byte[] array1, byte[] array2)
		{
			if (array1 == null)
				throw new ArgumentNullException("array1");
			if (array2 == null)
				throw new ArgumentNullException("array2");

			if (array1.Length != array2.Length)
				return false;

			return ByteMatch(array1, 0, array2, 0, array1.Length);
		}

		/// <summary>
		/// Checks to see if the contents of two byte arrays are equal
		/// </summary>
		/// <param name="array1">The first array</param>
		/// <param name="array2">The second array</param>
		/// <param name="offset1">The starting index for the first array</param>
		/// <param name="offset2">The starting index for the second array</param>
		/// <param name="count">The number of bytes to check</param>
		/// <returns></returns>
		public static bool ByteMatch(byte[] array1, int offset1, byte[] array2, int offset2, int count)
		{
			if (array1 == null)
			{
				throw new ArgumentNullException("array1");
			}
			if (array2 == null)
			{
				throw new ArgumentNullException("array2");
			}

			// If either of the arrays is too small, they're not equal
			if ((array1.Length - offset1) < count || (array2.Length - offset2) < count)
			{
				return false;
			}

			// Check if any elements are unequal
			for (int i = 0; i < count; i++)
			{
				if (array1[offset1 + i] != array2[offset2 + i])
				{
					return false;
				}
			}

			return true;
		}

		internal static int HashCode(byte[] array)
		{
			int result = 0;
			for (int i = 0; i < array.Length; i++)
			{
				result ^= array[i];
			}

			return result;
		}

		internal static bool HasEncryption(EncryptionTypes available, EncryptionTypes check)
		{
			return (available & check) == check;
		}
	}
}
