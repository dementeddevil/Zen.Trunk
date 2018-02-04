using System.IO;
using Xunit;
using Zen.Trunk.Storage.Log;

namespace Zen.Trunk.Storage
{
    /// <summary>
    /// Summary description for VirtualLogStreamTests
    /// </summary>
    [Trait("", "")]
    public class VirtualLogStreamUnitTests
    {
        [Fact(DisplayName = "When stream is initialised the file length is exactly two headers.")]
        public void WhenStreamIsInitialisedTheLengthIsTwoHeaders()
        {
            var mockedLogDevice = new Moq.Mock<ILogPageDevice>();
            var logFileInfo = new VirtualLogFileInfo();

            using (var stream = new MemoryStream())
            {
                using (var sut = new VirtualLogFileStream(mockedLogDevice.Object, stream, logFileInfo))
                {
                    sut.InitNew();
                }

                Assert.Equal(VirtualLogFileStream.TotalHeaderSize, stream.Length);
            }
        }


        [Fact(DisplayName = "Given an initialised stream, When two log records are written, Then the stream length is as expected.")]
        public void CreateStreamAndWriteCheckpoint()
        {
            var mockedLogDevice = new Moq.Mock<ILogPageDevice>();
            var logFileInfo = new VirtualLogFileInfo();

            using (var stream = new MemoryStream())
            {
                var beginCheckpoint = new BeginCheckPointLogEntry();
                var endCheckpoint = new EndCheckPointLogEntry();
                using (var sut = new VirtualLogFileStream(mockedLogDevice.Object, stream, logFileInfo))
                {
                    sut.InitNew();
                    sut.WriteEntry(beginCheckpoint);
                    sut.Flush();

                    Assert.Equal(VirtualLogFileStream.TotalHeaderSize + beginCheckpoint.RawSize, stream.Length);

                    sut.WriteEntry(endCheckpoint);
                    sut.Flush();

                    Assert.Equal(VirtualLogFileStream.TotalHeaderSize + beginCheckpoint.RawSize + endCheckpoint.RawSize, stream.Length);
                }
            }
        }
    }
}
