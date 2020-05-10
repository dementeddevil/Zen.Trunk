using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Serilog;

namespace Zen.Trunk.VirtualMemory
{
    public class WriteGatherRequestArray : ScatterGatherRequestArray
    {
        private static readonly ILogger Logger = Log.ForContext<WriteGatherRequestArray>();

        /// <summary>
        /// Initializes a new instance of the <see cref="WriteGatherRequestArray"/> class.
        /// </summary>
        /// <param name="systemClock">Reference clock.</param>
        /// <param name="stream">The <see cref="AdvancedStream"/>.</param>
        /// <param name="request">The request.</param>
        [CLSCompliant(false)]
        public WriteGatherRequestArray(
            ISystemClock systemClock,
            AdvancedStream stream,
            ScatterGatherRequest request)
            : base(systemClock, stream, request)
        {
        }

        /// <summary>
        /// Flushes the request array under the assumption that each element
        /// relates to a pending write to the underlying stream.
        /// </summary>
        /// <returns></returns>
        public override async Task FlushAsync()
        {
            Logger.Debug(
                "Writing {PageCount} memory blocks to disk",
                CallbackInfo.Count);

            // Prepare buffer array
            var buffers = CallbackInfo
                .Select(item => item.Buffer)
                .ToArray();
            var bufferSize = buffers[0].BufferSize;

            await ExecuteIoOperationAsync(
                    () =>
                    {
                        // TODO: We should be able to call Scatter/gather API with an
                        //  overlapped structure set in such a way as to obviate the need
                        //  to do a seek operation (and therefore never needing the
                        //  stream lock synchronisation step)
                        lock (Stream.SyncRoot)
                        {
                            // Adjust the file position and perform scatter/gather
                            //	operation
                            Stream.Seek(StartBlockNo * bufferSize, SeekOrigin.Begin);
                            return Stream.WriteGatherAsync(buffers);
                        }
                    })
                .ConfigureAwait(false);
        }
    }
}