using System.IO;
using Zen.Trunk.IO;

namespace Zen.Trunk.Storage.BufferFields
{
    public static class BufferFieldWrapperExtensions
    {
        public static void ReadFrom(this BufferFieldWrapper wrapper, Stream stream)
        {
            using (var streamManager = new SwitchingBinaryReader(stream, true))
            {
                wrapper.Read(streamManager);
            }
        }
    }
}