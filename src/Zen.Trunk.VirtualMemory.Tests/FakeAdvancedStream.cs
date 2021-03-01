using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Zen.Trunk.VirtualMemory.Tests
{
    public class FakeAdvancedStream : AdvancedStream
    {
        private readonly MemoryStream _innerStream = new MemoryStream();
        private readonly List<string> _buffersRead = new List<string>();
        private readonly List<string> _buffersWritten = new List<string>();

        public override bool CanRead => _innerStream.CanRead;

        public override bool CanSeek => _innerStream.CanSeek;

        public override bool CanWrite => _innerStream.CanWrite;

        public override long Length => _innerStream.Length;

        public override long Position { get; set; }

        public IList<string> BuffersRead => _buffersRead.AsReadOnly();

        public IList<string> BuffersWritten => _buffersWritten.AsReadOnly();

        public override void Flush()
        {
            _innerStream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _innerStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _innerStream.SetLength(value);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _innerStream.Read(buffer, offset, count);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _innerStream.Write(buffer, offset, count);
        }

        public override IAsyncResult BeginReadScatter(IVirtualBuffer[] buffers, AsyncCallback callback, object state)
        {
            var tcs = new TaskCompletionSource<int>(state, TaskCreationOptions.RunContinuationsAsynchronously);
            ReadScatterAsync(buffers)
                .ContinueWith(
                    t =>
                    {
                        callback?.Invoke(tcs.Task);

                        if (t.IsCanceled)
                        {
                            tcs.SetCanceled();
                        }
                        else if (t.IsFaulted)
                        {
                            // ReSharper disable once AssignNullToNotNullAttribute
                            tcs.SetException(t.Exception);
                        }
                        else
                        {
                            tcs.SetResult(t.Result);
                        }
                    });
            return tcs.Task;
        }

        public override int EndReadScatter(IAsyncResult asyncResult)
        {
            return ((Task<int>) asyncResult).GetAwaiter().GetResult();
        }

        public override IAsyncResult BeginWriteGather(IVirtualBuffer[] buffers, AsyncCallback callback, object state)
        {
            var tcs = new TaskCompletionSource<bool>(state, TaskCreationOptions.RunContinuationsAsynchronously);
            WriteGatherAsync(buffers)
                .ContinueWith(
                    t =>
                    {
                        if (t.IsCanceled)
                        {
                            tcs.SetCanceled();
                        }
                        else if (t.IsFaulted)
                        {
                            // ReSharper disable once AssignNullToNotNullAttribute
                            tcs.SetException(t.Exception);
                        }
                        else
                        {
                            tcs.SetResult(true);
                        }

                        callback?.Invoke(tcs.Task);
                    });
            return tcs.Task;
        }

        public override void EndWriteGather(IAsyncResult asyncResult)
        {
            ((Task)asyncResult).GetAwaiter().GetResult();
        }

        private async Task<int> ReadScatterAsync(IVirtualBuffer[] buffers)
        {
            var bytesRead = 0;

            foreach (var buffer in buffers)
            {
                var bufferSize = buffer.BufferSize;
                var block = new byte[bufferSize];
                using (var bufferStream = buffer.GetBufferStream(0, bufferSize, true))
                {
                    await _innerStream.ReadAsync(block, 0, bufferSize).ConfigureAwait(false);
                    await bufferStream.WriteAsync(block, 0, bufferSize).ConfigureAwait(false);
                }

                _buffersRead.Add(buffer.BufferId);
                await Task.Delay(20).ConfigureAwait(false);
                bytesRead += buffer.BufferSize;
            }

            return bytesRead;
        }

        private async Task WriteGatherAsync(IVirtualBuffer[] buffers)
        {
            foreach (var buffer in buffers)
            {
                var bufferSize = buffer.BufferSize;
                var block = new byte[bufferSize];
                using (var bufferStream = buffer.GetBufferStream(0, bufferSize, false))
                {
                    await bufferStream.ReadAsync(block, 0, bufferSize).ConfigureAwait(false);
                    await _innerStream.WriteAsync(block, 0, bufferSize).ConfigureAwait(false);
                }

                _buffersWritten.Add(buffer.BufferId);
                await Task.Delay(20).ConfigureAwait(false);
                Position += buffer.BufferSize;
            }
        }
    }
}
