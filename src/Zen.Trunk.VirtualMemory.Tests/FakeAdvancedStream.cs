using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Zen.Trunk.VirtualMemory.Tests
{
    public class FakeAdvancedStream : AdvancedStream
    {
        private readonly List<string> _buffersRead = new List<string>();
        private readonly List<string> _buffersWritten = new List<string>();

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => true;

        public override long Length => 65536;

        public override long Position { get; set; }

        public IList<string> BuffersRead => _buffersRead.AsReadOnly();

        public IList<string> BuffersWritten => _buffersWritten.AsReadOnly();

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                case SeekOrigin.Current:
                    Position += offset;
                    break;
                case SeekOrigin.End:
                    Position = Length - offset;
                    break;
            }

            return Position;
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            Position += count;
            return count;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Position += count;
        }

        public override IAsyncResult BeginReadScatter(IVirtualBuffer[] buffers, AsyncCallback callback, object state)
        {
            var tcs = new TaskCompletionSource<int>(state);
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
            var tcs = new TaskCompletionSource<bool>(state);
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
                _buffersRead.Add(buffer.BufferId);
                await Task.Delay(20).ConfigureAwait(false);
                Position += buffer.BufferSize;
                bytesRead += buffer.BufferSize;
            }

            return bytesRead;
        }

        private async Task WriteGatherAsync(IVirtualBuffer[] buffers)
        {
            foreach (var buffer in buffers)
            {
                _buffersWritten.Add(buffer.BufferId);
                await Task.Delay(20).ConfigureAwait(false);
                Position += buffer.BufferSize;
            }
        }
    }
}
