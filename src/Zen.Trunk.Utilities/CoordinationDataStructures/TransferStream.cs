//--------------------------------------------------------------------------
// 
//  Copyright (c) Microsoft Corporation.  All rights reserved. 
// 
//  File: TransferStream.cs
//
//--------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;

namespace Zen.Trunk.CoordinationDataStructures
{
	/// <summary>
	/// Writeable stream for using a separate thread in a producer/consumer scenario.
	/// </summary>
	public sealed class TransferStream : AbstractStreamBase
	{
		private readonly Stream _writeableStream;
		private readonly BlockingCollection<byte[]> _chunks;
		private readonly Task _processingTask;

		/// <summary>
		/// Initializes a new instance of the <see cref="TransferStream" /> class.
		/// </summary>
		/// <param name="writeableStream">The underlying stream to which to write.</param>
		/// <exception cref="System.ArgumentNullException">writeableStream</exception>
		/// <exception cref="System.ArgumentException">Target stream is not writeable.</exception>
		public TransferStream(Stream writeableStream)
		{
			// Validate arguments
			if (writeableStream == null)
			{
				throw new ArgumentNullException(nameof(writeableStream));
			}
			if (!writeableStream.CanWrite)
			{
				throw new ArgumentException("Target stream is not writeable.");
			}

			// Set up the producer/consumer relationship, including starting the consumer running
			_writeableStream = writeableStream;
			_chunks = new BlockingCollection<byte[]>();
			_processingTask = Task.Factory.StartNew(
				() =>
				{
					// Write out all chunks to the underlying stream
					foreach (var chunk in _chunks.GetConsumingEnumerable())
					{
						_writeableStream.Write(chunk, 0, chunk.Length);
					}
				},
				TaskCreationOptions.LongRunning);
		}

		/// <summary>
		/// Determines whether data can be written to the stream.
		/// </summary>
		public override bool CanWrite => true;

	    /// <summary>
		/// Writes a sequence of bytes to the stream.
		/// </summary>
		/// <param name="buffer">An array of bytes. Write copies count bytes from buffer to the stream.</param>
		/// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the stream.</param>
		/// <param name="count">The number of bytes to be written to the current stream.</param>
		/// <exception cref="System.ArgumentNullException">buffer</exception>
		/// <exception cref="System.ArgumentOutOfRangeException">offset</exception>
		public override void Write(byte[] buffer, int offset, int count)
		{
			// Validate all arguments
			if (buffer == null)
			{
				throw new ArgumentNullException(nameof(buffer));
			}
			if (offset < 0 || offset >= buffer.Length)
			{
				throw new ArgumentOutOfRangeException(nameof(offset));
			}
			if (count < 0 || offset + count > buffer.Length)
			{
				throw new ArgumentOutOfRangeException(nameof(count));
			}
			if (count == 0)
			{
				return;
			}

			// Store the data to the collection
			var chunk = new byte[count];
			Buffer.BlockCopy(buffer, offset, chunk, 0, count);
			_chunks.Add(chunk);
		}

		/// <summary>
		/// Closes the current stream and releases any resources (such as
		/// sockets and file handles) associated with the current stream.
		/// Instead of calling this method, ensure that the stream is properly
		/// disposed.
		/// </summary>
		public override void Close()
		{
			// Complete the collection and waits for the consumer to process all of the data
			_chunks.CompleteAdding();
			try
			{
				_processingTask.Wait();
			}
			finally
			{
				base.Close();
			}
		}
	}
}